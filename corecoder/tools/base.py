"""所有工具的基类。"""

from abc import ABC, abstractmethod


class Tool(ABC):
    """最小化的工具接口。继承此类即可添加新能力。"""

    name: str
    description: str
    parameters: dict  # 函数参数的 JSON Schema

    @abstractmethod
    def execute(self, **kwargs) -> str:
        """运行工具并返回文本结果。"""
        ...

    def schema(self) -> dict:
        """OpenAI 函数调用格式的 schema。"""
        return {
            "type": "function",
            "function": {
                "name": self.name,
                "description": self.description,
                "parameters": self.parameters,
            },
        }
