"""工具注册表。"""

from .agent import AgentTool
from .bash import BashTool
from .edit import EditFileTool
from .glob_tool import GlobTool
from .grep import GrepTool
from .read import ReadFileTool
from .write import WriteFileTool

ALL_TOOLS = [
    BashTool(),
    ReadFileTool(),
    WriteFileTool(),
    EditFileTool(),
    GlobTool(),
    GrepTool(),
    AgentTool(),
]


def get_tool(name: str):
    """按名称查找工具。"""
    for t in ALL_TOOLS:
        if t.name == name:
            return t
    return None
