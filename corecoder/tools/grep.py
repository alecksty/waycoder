"""支持正则表达式的内容搜索。"""

import re
from pathlib import Path

from .base import Tool

# 跳过这些目录以减少噪音
_SKIP_DIRS = {".git", "node_modules", "__pycache__", ".venv", "venv", ".tox", "dist", "build"}


class GrepTool(Tool):
    name = "grep"
    description = (
        "使用正则表达式搜索文件内容。"
        "返回匹配行，包含文件路径和行号。"
    )
    parameters = {
        "type": "object",
        "properties": {
            "pattern": {
                "type": "string",
                "description": "要搜索的正则表达式模式",
            },
            "path": {
                "type": "string",
                "description": "要搜索的文件或目录（默认：当前工作目录）",
            },
            "include": {
                "type": "string",
                "description": "仅搜索匹配此 glob 模式的文件（如 '*.py'）",
            },
        },
        "required": ["pattern"],
    }

    def execute(self, pattern: str, path: str = ".", include: str | None = None) -> str:
        try:
            regex = re.compile(pattern)
        except re.error as e:
            return f"无效的正则表达式：{e}"

        base = Path(path).expanduser().resolve()
        if not base.exists():
            return f"错误：{path} 未找到"

        if base.is_file():
            files = [base]
        else:
            files = self._walk(base, include)

        matches = []
        for fp in files:
            try:
                text = fp.read_text(encoding="utf-8", errors="ignore")
            except OSError:
                continue
            for lineno, line in enumerate(text.splitlines(), 1):
                if regex.search(line):
                    matches.append(f"{fp}:{lineno}: {line.rstrip()}")
                    if len(matches) >= 200:
                        matches.append("...（已达到 200 条匹配上限）")
                        return "\n".join(matches)

        return "\n".join(matches) if matches else "未找到匹配项。"

    @staticmethod
    def _walk(root: Path, include: str | None) -> list[Path]:
        """遍历目录树，跳过垃圾目录。"""
        results = []
        for item in root.rglob(include or "*"):
            # 跳过搜索根目录*内部*的垃圾目录——匹配 item.parts
            # 也会命中名为 "build" 的祖先目录并隐藏整个树
            if any(part in _SKIP_DIRS for part in item.relative_to(root).parts):
                continue
            if item.is_file():
                results.append(item)
            if len(results) >= 5000:
                break
        return results
