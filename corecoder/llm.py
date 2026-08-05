"""LLM 提供商层 - OpenAI 兼容 API 的薄封装。

由于大多数提供商（DeepSeek、Qwen、Kimi、GLM、Ollama 等）都暴露
OpenAI 兼容的接口，我们直接使用 openai SDK。切换提供商只需修改
OPENAI_BASE_URL + OPENAI_API_KEY。就这么简单。

对于不兼容 OpenAI 的提供商（AWS Bedrock、Google Vertex 等），
使用 LiteLLM 后端，它通过统一接口路由到 100+ 个提供商。
设置 CORECODER_PROVIDER=litellm。
"""

import json
import time
from dataclasses import dataclass, field

from openai import APIConnectionError, APIError, APITimeoutError, BadRequestError, OpenAI, RateLimitError


@dataclass
class ToolCall:
    id: str
    name: str
    arguments: dict


@dataclass
class LLMResponse:
    content: str = ""
    tool_calls: list[ToolCall] = field(default_factory=list)
    prompt_tokens: int = 0
    completion_tokens: int = 0

    @property
    def message(self) -> dict:
        """转换为 OpenAI 消息格式，用于追加到历史记录。"""
        msg: dict = {"role": "assistant", "content": self.content or None}
        if self.tool_calls:
            msg["tool_calls"] = [
                {
                    "id": tc.id,
                    "type": "function",
                    "function": {
                        "name": tc.name,
                        "arguments": json.dumps(tc.arguments),
                    },
                }
                for tc in self.tool_calls
            ]
        return msg


# 每百万 token 的定价：（输入，输出）
# 来源：openai.com/api/pricing、api-docs.deepseek.com、platform.claude.com、
#       platform.moonshot.ai、alibabacloud.com/help/en/model-studio
_PRICING = {
    # OpenAI - 当前旗舰
    "gpt-5.5": (5, 30),
    "gpt-5.4": (2.5, 15),
    "gpt-5.4-mini": (0.75, 4.5),
    "gpt-5.4-nano": (0.2, 1.25),
    "o4-mini": (1.1, 4.4),
    # OpenAI - 上一代（仍被广泛使用）
    "gpt-4.1": (2, 8),
    "gpt-4.1-mini": (0.4, 1.6),
    "gpt-4.1-nano": (0.1, 0.4),
    "gpt-4o": (2.5, 10),
    "gpt-4o-mini": (0.15, 0.6),
    # DeepSeek V4
    "deepseek-v4-flash": (0.14, 0.28),
    "deepseek-v4-pro": (0.435, 0.87),
    # DeepSeek V3 旧版（即将废弃，保留兼容）
    "deepseek-chat": (0.27, 1.10),
    "deepseek-reasoner": (0.55, 2.19),
    # Anthropic Claude
    "claude-opus-4-6": (5, 25),
    "claude-sonnet-4-6": (3, 15),
    "claude-haiku-4-5": (1, 5),
    # 阿里 Qwen
    "qwen3-max": (0.78, 3.9),
    "qwen3-plus": (0.26, 0.78),
    "qwen-max": (0.78, 3.9),
    # 月之暗面 Kimi
    "kimi-k2.5": (0.6, 3),
}


class LLM:
    def __init__(
        self,
        model: str,
        api_key: str,
        base_url: str | None = None,
        **kwargs,
    ):
        self.model = model
        self.client = OpenAI(api_key=api_key, base_url=base_url)
        self.extra = kwargs  # temperature、max_tokens 等
        self.total_prompt_tokens = 0
        self.total_completion_tokens = 0

    @property
    def estimated_cost(self) -> float | None:
        """粗略的美元成本估算。模型不在定价表中时返回 None。"""
        pricing = _PRICING.get(self.model)
        if not pricing:
            return None
        input_rate, output_rate = pricing
        return (
            self.total_prompt_tokens * input_rate / 1_000_000
            + self.total_completion_tokens * output_rate / 1_000_000
        )

    def chat(
        self,
        messages: list[dict],
        tools: list[dict] | None = None,
        on_token=None,
    ) -> LLMResponse:
        """发送消息，流式返回响应，处理工具调用。"""
        params: dict = {
            "model": self.model,
            "messages": messages,
            "stream": True,
            **self.extra,
        }
        if tools:
            params["tools"] = tools

        # stream_options 是 OpenAI 扩展；仅在提供商拒绝该参数（400 BadRequest）
        # 时回退，而不是在 _call_with_retry 已耗尽的瞬态错误上回退——
        # 否则重试次数会翻倍
        params["stream_options"] = {"include_usage": True}
        try:
            stream = self._call_with_retry(params)
        except BadRequestError:
            params.pop("stream_options", None)
            stream = self._call_with_retry(params)

        content_parts: list[str] = []
        tc_map: dict[int, dict] = {}  # 索引 -> {id, name, arguments_str}
        prompt_tok = 0
        completion_tok = 0

        for chunk in stream:
            # usage 信息在最后一个分片中
            if chunk.usage:
                # 部分提供商的 usage 字段可能为 null；强制转换为 0
                # 以免后续累加时报 int + None 错误
                prompt_tok = chunk.usage.prompt_tokens or 0
                completion_tok = chunk.usage.completion_tokens or 0

            if not chunk.choices:
                continue
            delta = chunk.choices[0].delta

            # 累积文本
            if delta.content:
                content_parts.append(delta.content)
                if on_token:
                    on_token(delta.content)

            # 跨分片累积工具调用
            if delta.tool_calls:
                for tc_delta in delta.tool_calls:
                    idx = tc_delta.index
                    if idx not in tc_map:
                        tc_map[idx] = {"id": "", "name": "", "args": ""}
                    if tc_delta.id:
                        tc_map[idx]["id"] = tc_delta.id
                    if tc_delta.function:
                        if tc_delta.function.name:
                            tc_map[idx]["name"] = tc_delta.function.name
                        if tc_delta.function.arguments:
                            tc_map[idx]["args"] += tc_delta.function.arguments

        # 解析累积的工具调用
        parsed: list[ToolCall] = []
        for idx in sorted(tc_map):
            raw = tc_map[idx]
            try:
                args = json.loads(raw["args"])
            except (json.JSONDecodeError, KeyError):
                args = {}
            parsed.append(ToolCall(id=raw["id"], name=raw["name"], arguments=args))

        self.total_prompt_tokens += prompt_tok
        self.total_completion_tokens += completion_tok

        return LLMResponse(
            content="".join(content_parts),
            tool_calls=parsed,
            prompt_tokens=prompt_tok,
            completion_tokens=completion_tok,
        )

    def _call_with_retry(self, params: dict, max_retries: int = 3):
        """在瞬态错误时使用指数退避重试。"""
        for attempt in range(max_retries):
            try:
                return self.client.chat.completions.create(**params)
            except (RateLimitError, APITimeoutError, APIConnectionError):
                if attempt == max_retries - 1:
                    raise
                wait = 2 ** attempt
                time.sleep(wait)
            except APIError as e:
                # 重试 5xx 服务器错误，但不重试 4xx；基础 APIError 没有
                # status_code 属性，所以防御性地读取它
                status_code = getattr(e, "status_code", None)
                if status_code and status_code >= 500 and attempt < max_retries - 1:
                    time.sleep(2 ** attempt)
                else:
                    raise


class LiteLLM(LLM):
    """通过 LiteLLM 的 LLM 后端，支持 100+ 个提供商。

    当目标提供商不兼容 OpenAI 时使用此后端（AWS Bedrock、Google Vertex、
    Cohere 等），或者当你想通过修改模型字符串来切换任意提供商时使用。

    设置 CORECODER_PROVIDER=litellm，使用 LiteLLM 模型字符串，
    如 ``anthropic/claude-3-haiku``、``bedrock/anthropic.claude-v2``、
    ``vertex_ai/gemini-pro`` 等。
    """

    def __init__(
        self,
        model: str,
        api_key: str | None = None,
        base_url: str | None = None,
        **kwargs,
    ):
        # 跳过 LLM.__init__，它创建了 OpenAI 客户端
        self.model = model
        self.api_key = api_key
        self.base_url = base_url
        self.extra = kwargs
        self.total_prompt_tokens = 0
        self.total_completion_tokens = 0

    def chat(
        self,
        messages: list[dict],
        tools: list[dict] | None = None,
        on_token=None,
    ) -> LLMResponse:
        """通过 litellm 发送消息，流式返回响应，处理工具调用。"""
        params: dict = {
            "model": self.model,
            "messages": messages,
            "stream": True,
            **self.extra,
        }
        if tools:
            params["tools"] = tools

        # 请求在最后一个分片中包含 usage 统计；litellm 会对不支持此功能的
        # 提供商自动丢弃该参数（drop_params），所以始终请求是安全的
        params["stream_options"] = {"include_usage": True}
        stream = self._call_with_retry(params)

        content_parts: list[str] = []
        tc_map: dict[int, dict] = {}
        prompt_tok = 0
        completion_tok = 0

        for chunk in stream:
            usage = getattr(chunk, "usage", None)
            if usage:
                prompt_tok = getattr(usage, "prompt_tokens", 0) or 0
                completion_tok = getattr(usage, "completion_tokens", 0) or 0

            if not getattr(chunk, "choices", None):
                continue
            delta = chunk.choices[0].delta

            if getattr(delta, "content", None):
                content_parts.append(delta.content)
                if on_token:
                    on_token(delta.content)

            if getattr(delta, "tool_calls", None):
                for tc_delta in delta.tool_calls:
                    idx = tc_delta.index
                    if idx not in tc_map:
                        tc_map[idx] = {"id": "", "name": "", "args": ""}
                    if tc_delta.id:
                        tc_map[idx]["id"] = tc_delta.id
                    if tc_delta.function:
                        if tc_delta.function.name:
                            tc_map[idx]["name"] = tc_delta.function.name
                        if tc_delta.function.arguments:
                            tc_map[idx]["args"] += tc_delta.function.arguments

        parsed: list[ToolCall] = []
        for idx in sorted(tc_map):
            raw = tc_map[idx]
            try:
                args = json.loads(raw["args"])
            except (json.JSONDecodeError, KeyError):
                args = {}
            parsed.append(ToolCall(id=raw["id"], name=raw["name"], arguments=args))

        self.total_prompt_tokens += prompt_tok
        self.total_completion_tokens += completion_tok

        return LLMResponse(
            content="".join(content_parts),
            tool_calls=parsed,
            prompt_tokens=prompt_tok,
            completion_tokens=completion_tok,
        )

    def _call_with_retry(self, params: dict, max_retries: int = 3):
        """在瞬态错误时通过 litellm 使用指数退避重试。"""
        import litellm

        params["drop_params"] = True
        if self.api_key:
            params["api_key"] = self.api_key
        if self.base_url:
            params["api_base"] = self.base_url

        for attempt in range(max_retries):
            try:
                return litellm.completion(**params)
            except Exception as e:
                err = str(e).lower()
                is_transient = any(
                    kw in err
                    for kw in ["rate_limit", "timeout", "connection", "502", "503", "529"]
                )
                is_server = any(kw in err for kw in ["500", "502", "503", "504"])
                if (is_transient or is_server) and attempt < max_retries - 1:
                    time.sleep(2 ** attempt)
                else:
                    raise
