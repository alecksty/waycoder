"""核心智能体循环。

这是 CoreCoder 的心脏。模式很简单：

    用户消息 -> LLM（带工具）-> 有工具调用？-> 执行 -> 循环
                                 -> 文本回复？-> 返回给用户

它会持续循环，直到 LLM 回复纯文本（没有工具调用），
这意味着它已完成工作并准备报告结果。
"""

import concurrent.futures
import inspect

from .context import ContextManager
from .llm import LLM
from .prompt import system_prompt
from .tools import ALL_TOOLS
from .tools.agent import AgentTool
from .tools.base import Tool


class Agent:
    def __init__(
        self,
        llm: LLM,
        tools: list[Tool] | None = None,
        max_context_tokens: int = 128_000,
        max_rounds: int = 50,
    ):
        self.llm = llm
        self.tools = tools if tools is not None else ALL_TOOLS
        self._tool_by_name = {t.name: t for t in self.tools}
        self.messages: list[dict] = []
        self.context = ContextManager(max_tokens=max_context_tokens)
        self.max_rounds = max_rounds
        self._system = system_prompt(self.tools)

        # 连接子智能体能力
        for t in self.tools:
            if isinstance(t, AgentTool):
                t._parent_agent = self

    def _full_messages(self) -> list[dict]:
        return [{"role": "system", "content": self._system}] + self.messages

    def _tool_schemas(self) -> list[dict]:
        return [t.schema() for t in self.tools]

    def chat(self, user_input: str, on_token=None, on_tool=None) -> str:
        """处理一条用户消息。可能涉及多轮 LLM/工具交互。"""
        self.messages.append({"role": "user", "content": user_input})
        self.context.maybe_compress(self.messages, self.llm)

        for _ in range(self.max_rounds):
            resp = self.llm.chat(
                messages=self._full_messages(),
                tools=self._tool_schemas(),
                on_token=on_token,
            )

            # 没有工具调用 -> LLM 完成，返回文本
            if not resp.tool_calls:
                self.messages.append(resp.message)
                return resp.content

            # 有工具调用 -> 执行（多个时并行，类似 Claude Code 的
            # StreamingToolExecutor，它并发运行独立的工具）
            self.messages.append(resp.message)

            try:
                if len(resp.tool_calls) == 1:
                    tc = resp.tool_calls[0]
                    if on_tool:
                        on_tool(tc.name, tc.arguments)
                    result = self._exec_tool(tc)
                    self.messages.append({
                        "role": "tool",
                        "tool_call_id": tc.id,
                        "content": result,
                    })
                else:
                    # 多个工具调用时并行执行
                    results = self._exec_tools_parallel(resp.tool_calls, on_tool)
                    for tc, result in zip(resp.tool_calls, results):
                        self.messages.append({
                            "role": "tool",
                            "tool_call_id": tc.id,
                            "content": result,
                        })
            except KeyboardInterrupt:
                # 执行中途按 Ctrl+C 会导致 assistant 的 tool_calls 消息
                # 没有对应的工具回复，污染下一次请求；回填缺失的回复
                self._answer_pending_tool_calls(resp.tool_calls)
                raise

            # 如果工具输出太大则压缩上下文
            self.context.maybe_compress(self.messages, self.llm)

        return "（已达到最大工具调用轮次）"

    def _exec_tool(self, tc) -> str:
        """执行单个工具调用，返回结果字符串。"""
        tool = self._tool_by_name.get(tc.name)
        if tool is None:
            return f"错误：未知工具 '{tc.name}'"
        # 先验证参数，这样工具内部抛出的 TypeError 不会被
        # 误标为调用者的参数错误
        try:
            inspect.signature(tool.execute).bind(**tc.arguments)
        except TypeError as e:
            return f"错误：{tc.name} 的参数有误：{e}"
        try:
            return tool.execute(**tc.arguments)
        except Exception as e:
            return f"执行 {tc.name} 时出错：{e}"

    def _exec_tools_parallel(self, tool_calls, on_tool=None) -> list[str]:
        """使用线程并发运行多个工具调用。

        灵感源自 Claude Code 的 StreamingToolExecutor，它在模型还在生成时
        就开始执行工具。我们简化为：当模型一次返回 N 个工具调用时，并行运行它们。
        """
        for tc in tool_calls:
            if on_tool:
                on_tool(tc.name, tc.arguments)

        with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
            futures = [pool.submit(self._exec_tool, tc) for tc in tool_calls]
            return [f.result() for f in futures]

    def _answer_pending_tool_calls(self, tool_calls):
        """为每个未收到回复的工具调用回填一条工具回复。

        OpenAI 兼容 API 会拒绝包含 tool_calls 但没有对应 tool 回复的
        assistant 消息，因此在执行被中断时，这能保持历史记录有效。
        """
        answered = {m.get("tool_call_id") for m in self.messages if m.get("role") == "tool"}
        for tc in tool_calls:
            if tc.id not in answered:
                self.messages.append({
                    "role": "tool",
                    "tool_call_id": tc.id,
                    "content": "[已中断]",
                })

    def reset(self):
        """清空对话历史。"""
        self.messages.clear()
