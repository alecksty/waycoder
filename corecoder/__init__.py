"""CoreCoder - 极简 AI 编程智能体，灵感源自 Claude Code 的架构。"""

__version__ = "0.5.0"

from corecoder.agent import Agent
from corecoder.config import Config
from corecoder.llm import LLM
from corecoder.tools import ALL_TOOLS

__all__ = ["ALL_TOOLS", "LLM", "Agent", "Config", "__version__"]
