"""文件创建 / 覆写。"""

from pathlib import Path

from .base import Tool
from .edit import _changed_files


class WriteFileTool(Tool):
    name = "write_file"
    description = (
        "创建新文件或完全覆写已有文件。"
        "对于已有文件的小改动，优先使用 edit_file。"
    )
    parameters = {
        "type": "object",
        "properties": {
            "file_path": {
                "type": "string",
                "description": "文件路径",
            },
            "content": {
                "type": "string",
                "description": "要写入的完整文件内容",
            },
        },
        "required": ["file_path", "content"],
    }

    def execute(self, file_path: str, content: str) -> str:
        try:
            p = Path(file_path).expanduser().resolve()
            p.parent.mkdir(parents=True, exist_ok=True)
            p.write_text(content, encoding="utf-8")
            _changed_files.add(str(p))
            n_lines = content.count("\n") + (1 if content and not content.endswith("\n") else 0)
            return f"已写入 {n_lines} 行到 {file_path}"
        except Exception as e:
            return f"错误：{e}"
