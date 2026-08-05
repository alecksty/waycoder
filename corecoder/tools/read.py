"""带行号的文件读取。"""

from pathlib import Path

from .base import Tool


class ReadFileTool(Tool):
    name = "read_file"
    description = (
        "读取文件内容并显示行号。"
        "修改文件之前始终先读取它。"
    )
    parameters = {
        "type": "object",
        "properties": {
            "file_path": {
                "type": "string",
                "description": "文件路径",
            },
            "offset": {
                "type": "integer",
                "description": "起始行（从 1 开始）。默认 1。",
            },
            "limit": {
                "type": "integer",
                "description": "最大读取行数。默认 2000。",
            },
        },
        "required": ["file_path"],
    }

    def execute(self, file_path: str, offset: int = 1, limit: int = 2000) -> str:
        try:
            p = Path(file_path).expanduser().resolve()
            if not p.exists():
                return f"错误：{file_path} 未找到"
            if not p.is_file():
                return f"错误：{file_path} 是目录，不是文件"

            text = p.read_text(encoding="utf-8", errors="replace")
            lines = text.splitlines()
            total = len(lines)

            start = max(0, offset - 1)
            chunk = lines[start : start + limit]
            numbered = [f"{start + i + 1}\t{ln}" for i, ln in enumerate(chunk)]
            result = "\n".join(numbered)

            if total > start + limit:
                result += f"\n...（共 {total} 行，显示第 {start+1}-{start+len(chunk)} 行）"
            return result or "（空文件）"
        except Exception as e:
            return f"错误：{e}"
