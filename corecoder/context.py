"""多层上下文压缩。

Claude Code 使用 4 层策略：
  1. HISTORY_SNIP   - 将旧的工具输出裁剪为一行摘要
  2. Microcompact   - LLM 驱动的旧对话摘要（可缓存）
  3. CONTEXT_COLLAPSE - 接近硬限制时的激进压缩
  4. Autocompact    - 周期性后台压缩

CoreCoder 以 3 层实现相同的理念：
  第 1 层（tool_snip）   - 用截断版本替换冗长的工具结果
  第 2 层（summarize）   - LLM 驱动的旧对话摘要
  第 3 层（hard_collapse） - 最后手段：仅保留摘要 + 最近消息
"""

from __future__ import annotations

from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from .llm import LLM


def _approx_tokens(text: str) -> int:
    """粗略的 token 计数，混合中英文内容约每 3 个字符对应 1 个 token。"""
    return len(text) // 3


def estimate_tokens(messages: list[dict]) -> int:
    total = 0
    for m in messages:
        if m.get("content"):
            total += _approx_tokens(m["content"])
        if m.get("tool_calls"):
            total += _approx_tokens(str(m["tool_calls"]))
    return total


class ContextManager:
    def __init__(self, max_tokens: int = 128_000):
        self.max_tokens = max_tokens
        # 各层阈值（max_tokens 的比例）
        self._snip_at = int(max_tokens * 0.50)     # 50% -> 裁剪工具输出
        self._summarize_at = int(max_tokens * 0.70)  # 70% -> LLM 摘要
        self._collapse_at = int(max_tokens * 0.90)   # 90% -> 硬折叠

    def maybe_compress(self, messages: list[dict], llm: LLM | None = None) -> bool:
        """按需应用压缩层。返回是否发生了压缩。"""
        current = estimate_tokens(messages)
        compressed = False

        # 第 1 层：裁剪冗长的工具输出
        if current > self._snip_at:
            if self._snip_tool_outputs(messages):
                compressed = True
                current = estimate_tokens(messages)

        # 第 2 层：LLM 驱动的旧对话摘要
        if current > self._summarize_at and len(messages) > 10:
            if self._summarize_old(messages, llm, keep_recent=8):
                compressed = True
                current = estimate_tokens(messages)

        # 第 3 层：硬折叠——最后手段
        if current > self._collapse_at and len(messages) > 4:
            self._hard_collapse(messages, llm)
            compressed = True

        return compressed

    @staticmethod
    def _snip_tool_outputs(messages: list[dict]) -> bool:
        """第 1 层：将超过 1500 字符的工具结果裁剪为首尾几行。

        这对应 Claude Code 的 HISTORY_SNIP，它用一行摘要替换旧的工具输出，
        以回收上下文空间。
        """
        changed = False
        for m in messages:
            if m.get("role") != "tool":
                continue
            content = m.get("content", "")
            if len(content) <= 1500:
                continue
            lines = content.splitlines()
            if len(lines) <= 6:
                continue
            # 保留前 3 行 + 后 3 行
            snipped = (
                "\n".join(lines[:3])
                + f"\n...（共 {len(lines)} 行，已裁剪以节省上下文）...\n"
                + "\n".join(lines[-3:])
            )
            m["content"] = snipped
            changed = True
        return changed

    @staticmethod
    def _safe_split(messages: list[dict], keep_recent: int) -> int:
        """计算保留尾部应从哪个索引开始。

        将边界向后移动，确保 'tool' 结果永远不会与产生它的（包含
        tool_calls 的）assistant 消息分离——孤立的工具消息没有前置的
        tool_calls，OpenAI 兼容 API 会拒绝此类请求。
        """
        split = max(0, len(messages) - keep_recent)
        while split > 0 and messages[split].get("role") == "tool":
            split -= 1
        return split

    def _summarize_old(self, messages: list[dict], llm: LLM | None,
                       keep_recent: int = 8) -> bool:
        """第 2 层：摘要旧对话，保持最近消息不变。"""
        if len(messages) <= keep_recent:
            return False

        split = self._safe_split(messages, keep_recent)
        old = messages[:split]
        tail = messages[split:]

        summary = self._get_summary(old, llm)

        messages.clear()
        messages.append({
            "role": "user",
            "content": f"[上下文已压缩 - 对话摘要]\n{summary}",
        })
        messages.append({
            "role": "assistant",
            "content": "收到，我已了解之前对话的上下文。",
        })
        messages.extend(tail)
        return True

    def _hard_collapse(self, messages: list[dict], llm: LLM | None):
        """第 3 层：紧急压缩。仅保留最后 4 条消息 + 摘要。"""
        split = self._safe_split(messages, 4 if len(messages) > 4 else 2)
        tail = messages[split:]
        summary = self._get_summary(messages[:split], llm)

        messages.clear()
        messages.append({
            "role": "user",
            "content": f"[硬重置上下文]\n{summary}",
        })
        messages.append({
            "role": "assistant",
            "content": "上下文已恢复。从之前中断的地方继续。",
        })
        messages.extend(tail)

    def _get_summary(self, messages: list[dict], llm: LLM | None) -> str:
        """通过 LLM 生成摘要，或回退到提取关键信息。"""
        flat = self._flatten(messages)

        if llm:
            try:
                resp = llm.chat(
                    messages=[
                        {
                            "role": "system",
                            "content": (
                                "将此对话压缩为简要摘要。"
                                "保留：已编辑的文件路径、已做出的关键决策、"
                                "遇到的错误、当前任务状态。"
                                "丢弃：冗长的命令输出、代码清单、"
                                "重复的来回对话。"
                            ),
                        },
                        {"role": "user", "content": flat[:15000]},
                    ],
                )
                return resp.content
            except Exception:
                pass

        # 回退：提取关键行
        return self._extract_key_info(messages)

    @staticmethod
    def _flatten(messages: list[dict]) -> str:
        parts = []
        for m in messages:
            role = m.get("role", "?")
            text = m.get("content", "") or ""
            if text:
                parts.append(f"[{role}] {text[:400]}")
        return "\n".join(parts)

    @staticmethod
    def _extract_key_info(messages: list[dict]) -> str:
        """回退方案：无需 LLM，提取文件路径、错误和决策。"""
        import re
        files_seen = set()
        errors = []

        for m in messages:
            text = m.get("content", "") or ""
            # 提取文件路径
            for match in re.finditer(r'[\w./\-]+\.\w{1,5}', text):
                files_seen.add(match.group())
            # 提取错误行
            for line in text.splitlines():
                if "error" in line.lower():
                    errors.append(line.strip()[:150])

        parts = []
        if files_seen:
            parts.append(f"涉及的文件：{', '.join(sorted(files_seen)[:20])}")
        if errors:
            parts.append(f"遇到的错误：{'；'.join(errors[:5])}")
        return "\n".join(parts) or "（无可提取的上下文）"
