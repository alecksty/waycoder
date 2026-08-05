"""搜索替换式文件编辑（Claude Code 的关键创新）。

核心思想：不是发送整个文件重写或行号补丁，而是让 LLM 指定一个
*精确*的子串来查找及其替换内容。该子串必须在文件中恰好出现一次，
从而消除歧义，使编辑安全且可审查。
"""

import difflib
from pathlib import Path

from .base import Tool

# 跟踪本次会话中修改的文件，供 /diff 使用
_changed_files: set[str] = set()


class EditFileTool(Tool):
    name = "edit_file"
    description = (
        "通过替换精确匹配的字符串来编辑文件。"
        "为安全起见，old_string 必须在文件中恰好出现一次。"
        "包含足够的上下文以确保唯一性。"
    )
    parameters = {
        "type": "object",
        "properties": {
            "file_path": {
                "type": "string",
                "description": "要编辑的文件路径",
            },
            "old_string": {
                "type": "string",
                "description": "要查找的精确文本（必须在文件中唯一）",
            },
            "new_string": {
                "type": "string",
                "description": "替换文本",
            },
        },
        "required": ["file_path", "old_string", "new_string"],
    }

    def execute(self, file_path: str, old_string: str, new_string: str) -> str:
        try:
            p = Path(file_path).expanduser().resolve()
            if not p.exists():
                return f"错误：{file_path} 未找到"

            try:
                content = p.read_text(encoding="utf-8")
            except UnicodeDecodeError:
                return f"错误：{file_path} 不是 UTF-8 文本文件（edit_file 只能编辑文本文件）"
            occurrences = content.count(old_string)

            if occurrences == 0:
                preview = content[:500] + ("..." if len(content) > 500 else "")
                return (
                    f"错误：在 {file_path} 中未找到 old_string。\n"
                    f"文件开头内容：\n{preview}"
                )
            if occurrences > 1:
                return (
                    f"错误：old_string 在 {file_path} 中出现了 {occurrences} 次。"
                    f"请包含更多上下文行以确保唯一性。"
                )

            new_content = content.replace(old_string, new_string, 1)
            p.write_text(new_content, encoding="utf-8")
            _changed_files.add(str(p))

            # 生成 unified diff，以便用户/LLM 准确看到变更内容
            diff = _unified_diff(content, new_content, str(p))
            return f"已编辑 {file_path}\n{diff}"
        except Exception as e:
            return f"错误：{e}"


def _unified_diff(old: str, new: str, filename: str, context: int = 3) -> str:
    """生成新旧文件内容之间的紧凑 unified diff。"""
    old_lines = old.splitlines(keepends=True)
    new_lines = new.splitlines(keepends=True)
    diff = difflib.unified_diff(
        old_lines, new_lines,
        fromfile=f"a/{filename}", tofile=f"b/{filename}",
        n=context,
    )
    result = "".join(diff)
    # 截断过大的 diff
    if len(result) > 3000:
        result = result[:2500] + "\n...（diff 已截断）\n"
    return result
