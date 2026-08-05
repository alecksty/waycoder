"""子智能体生成（灵感源自 Claude Code 的 AgentTool，1397 行）。

核心思想：对于复杂的子任务，生成一个独立的智能体，
拥有自己的对话历史和工具访问权限。这让主智能体可以委派工作，
如"去研究这个代码库并报告结果"，而不会污染自己的上下文窗口。

子智能体运行至完成，返回文本摘要。
"""

from .base import Tool


class AgentTool(Tool):
    name = "agent"
    description = (
        "生成一个子智能体来独立处理复杂的子任务。"
        "子智能体拥有自己的上下文和工具访问权限。适用于："
        "研究代码库、独立实现多步骤变更，"
        "或任何能从全新上下文中获益的任务。"
    )
    parameters = {
        "type": "object",
        "properties": {
            "task": {
                "type": "string",
                "description": "子智能体应完成的任务",
            },
        },
        "required": ["task"],
    }

    # 由 Agent.__init__ 在构造后设置
    _parent_agent = None

    def execute(self, task: str) -> str:
        if self._parent_agent is None:
            return "错误：agent 工具未初始化（没有父智能体）"

        # 在此导入以避免循环依赖
        from ..agent import Agent

        parent = self._parent_agent
        sub = Agent(
            llm=parent.llm,
            tools=[t for t in parent.tools if t.name != "agent"],  # 禁止递归生成子智能体
            max_context_tokens=parent.context.max_tokens,
            max_rounds=20,
        )

        try:
            result = sub.chat(task)
            # 截断过长结果，避免撑爆父智能体的上下文
            if len(result) > 5000:
                result = result[:4500] + "\n...（子智能体输出已截断）"
            return f"[子智能体已完成]\n{result}"
        except Exception as e:
            return f"子智能体错误：{e}"
