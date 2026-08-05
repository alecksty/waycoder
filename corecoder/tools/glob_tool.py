"""文件模式匹配。"""

from pathlib import Path

from .base import Tool


class GlobTool(Tool):
    name = "glob"
    description = (
        "查找匹配 glob 模式的文件。"
        "支持 ** 进行递归匹配（如 '**/*.py'）。"
    )
    parameters = {
        "type": "object",
        "properties": {
            "pattern": {
                "type": "string",
                "description": "Glob 模式，如 '**/*.py' 或 'src/**/*.ts'",
            },
            "path": {
                "type": "string",
                "description": "搜索目录（默认：当前工作目录）",
            },
        },
        "required": ["pattern"],
    }

    def execute(self, pattern: str, path: str = ".") -> str:
        try:
            base = Path(path).expanduser().resolve()
            if not base.is_dir():
                return f"错误：{path} 不是目录"

            hits = list(base.glob(pattern))
            # 按修改时间排序，最新的在前
            hits.sort(key=lambda p: p.stat().st_mtime if p.exists() else 0, reverse=True)

            total = len(hits)
            shown = hits[:100]
            lines = [str(h) for h in shown]
            result = "\n".join(lines)

            if total > 100:
                result += f"\n...（共 {total} 个匹配，仅显示前 100 个）"
            return result or "没有匹配的文件。"
        except Exception as e:
            return f"错误：{e}"
