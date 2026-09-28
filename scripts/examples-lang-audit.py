#!/usr/bin/env python3
"""例程语言化审计 —— 找出「用户可见、但没有按语言分支」的中文字符串字面量。

判据（唯一裁判，改例程的人与验收的人共用它）：
  **注释里的中文不算**（注释的多语化是另一个工作面），
  **字符串字面量里、且会显示给用户的中文**才算 —— 这类必须是
  `lang == 0 ? "中文" : "English"`（各语言写成各自的条件表达式）。

用法：
    python scripts/examples-lang-audit.py                 # 全部打包进包的例程
    python scripts/examples-lang-audit.py c/ gomoku       # 只看匹配的路径（子串过滤）
    python scripts/examples-lang-audit.py --list-stale    # 只列还有问题的文件

退出码：0 = 干净；1 = 还有未分支的用户可见中文。

⚠ **这条判据要求「中文字面量与语言条件在同一行」**（`lang == 0` / `lang != 0` / `if lang`，忽略大小写）。
这是**刻意的保守**（宁可漏判"已分支"，也不放过"看起来分支了其实没有"），但它会**逼着**某些语言
改写法：多行 `if/else` 把中文放在单独一行是**完全正确**的代码，却过不了这条 —— Fortran 那种就得
写成同一行（或加行内条件）。**若某语言只有多行写法可用，就按语言的正确写法写，并在报告里说明该处
过不了脚本**；别为了过脚本把代码写成这个语言里别扭的样子（那是判据倒过来指挥实现）。

已知的语言侧约束（改之前先看，都是实测出来的，不是猜的）：
    · **Lua**：`and`/`or` 不能当三元用 —— 前端收尾就是 `MOVE R0, 0/1`，返回**布尔**而非操作数
      （`(lang == 0) and "中文" or "English"` 会把窗口标题画成 `"VML"`）。要 `if … then … else … end`。
    · **Go**：包级标量坏了 —— `var lang int` 在函数里赋值，读回来恒 0。用状态数组（如 `A[110]`）。
    · **Python**：模块级全局在函数里不可见 —— 模块级 `LANG = ui_get_language()` 在函数里读出恒 0。走参数传。
    · **ObjC**：没有三元运算符（解析器未实现）⇒ `if/else` 把调用写两遍。
    · **Swift**：不解码转义 —— `\"` 会渲染成 `\"`。英文里的引号用 `'No'` 绕开。
    · **BASIC**：单行 `IF … THEN <带实参的调用> ELSE <带实参的调用>` 会**静默打乱实参**
      （坐标/颜色/串指针全变垃圾）；赋值与 `PRINT` 用单行 IF 是对的，块式 `IF … END IF` 也是对的。
      要进调用实参的文字先赋给字符串变量再调用。
"""
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "third_party", "vml", "Examples")
CJK = re.compile(r"[一-鿿]")

# 每种扩展名的行注释 / 块注释标记（**按语言来**；不按语言会误把代码当注释或反之）
LINE_COMMENT = {
    ".c": ["//"], ".h": ["//"], ".cpp": ["//"], ".hpp": ["//"], ".cs": ["//"],
    ".java": ["//"], ".js": ["//"], ".go": ["//"], ".rs": ["//"], ".swift": ["//"],
    ".m": ["//"], ".mm": ["//"], ".kt": ["//"], ".dart": ["//"], ".d": ["//"],
    ".vml": [";"], ".ld": ["#"], ".py": ["#"], ".rb": ["#"], ".r": ["#"],
    ".lua": ["--"], ".f90": ["!"], ".fth": ["\\"], ".scm": [";"], ".fs": ["\\"],
    ".bas": ["'", "REM", "rem"], ".pas": ["//"], ".swift ": ["//"],
}
BLOCK_COMMENT = {  # (开, 闭)
    ".c": [("/*", "*/")], ".h": [("/*", "*/")], ".cpp": [("/*", "*/")], ".hpp": [("/*", "*/")],
    ".cs": [("/*", "*/")], ".java": [("/*", "*/")], ".js": [("/*", "*/")], ".go": [("/*", "*/")],
    ".rs": [("/*", "*/")], ".swift": [("/*", "*/")], ".m": [("/*", "*/")], ".mm": [("/*", "*/")],
    ".kt": [("/*", "*/")], ".dart": [("/*", "*/")], ".d": [("/*", "*/")],
    ".pas": [("{", "}"), ("(*", "*)")],
    ".fth": [("(", ")")],
    # ⚠ `.ld`（Ladder）**两种注释都有**：行注释 `#` + 块注释 `(* … *)`。
    #   只登记了前者 ⇒ 文件头那段 `(* … *)` 里的中文被当成"用户可见字符串"，
    #   实测 `ladder/demo_std.ld` 误报 4 处（行 4、6、13、13）。
    ".ld": [("(*", "*)")],
    ".scm": [("#|", "|#")],
}


def strip_comments(text, ext):
    """把注释换成等长空白（**保行号**）—— 行号能对上才方便人工核对。"""
    out = list(text)
    for a, b in BLOCK_COMMENT.get(ext, []):
        i = 0
        while True:
            s = "".join(out).find(a, i)
            if s < 0:
                break
            e = "".join(out).find(b, s + len(a))
            e = len(out) if e < 0 else e + len(b)
            for k in range(s, e):
                if out[k] != "\n":
                    out[k] = " "
            i = e
    marks = LINE_COMMENT.get(ext, [])
    lines = "".join(out).split("\n")
    for n, line in enumerate(lines):
        cut = None
        for m in marks:
            # BASIC 的 `REM` 只算行首（否则 `REMARK` 之类会误伤）；`'` 直接找
            if m.isalpha():
                if re.match(r"\s*" + m + r"\b", line):
                    cut = 0 if cut is None else min(cut, 0)
            else:
                p = line.find(m)
                if p >= 0 and (cut is None or p < cut):
                    cut = p
        if cut is not None:
            lines[n] = line[:cut] + " " * (len(line) - cut)
    return "\n".join(lines)


# 例程里「引号字符串」的形态：双引号、单引号（跳过 Fortran 的 .and. 之类不是引号的情形）
STR = re.compile(r'"([^"\n]*)"' + r"|'([^'\n]*)'")


def audit(path, ext):
    try:
        text = open(path, encoding="utf-8", errors="ignore").read()
    except Exception:
        return []
    code = strip_comments(text, ext)
    hits = []
    for i, line in enumerate(code.split("\n"), 1):
        for m in STR.finditer(line):
            lit = m.group(1) if m.group(1) is not None else m.group(2)
            if lit and CJK.search(lit):
                # 已经是「按语言分支」的写法就不再算问题
                if re.search(r"lang\s*[=!]=\s*0|LANG\s*=\s*0|\bif\s+lang\b"
                             r"|LANG\s*@\s*0=", line, re.I):
                    continue
                hits.append((i, lit))
    return hits


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    list_stale = "--list-stale" in sys.argv
    total = 0
    files = 0
    for root, dirs, names in os.walk(ROOT):
        dirs[:] = [d for d in dirs if d != "_selftest"]
        for name in sorted(names):
            ext = os.path.splitext(name)[1].lower()
            if ext not in LINE_COMMENT and ext not in BLOCK_COMMENT:
                continue
            p = os.path.join(root, name)
            rel = os.path.relpath(p, ROOT).replace("\\", "/")
            if args and not any(a in rel for a in args):
                continue
            hits = audit(p, ext)
            if not hits:
                continue
            files += 1
            total += len(hits)
            print(f"{len(hits):>3}  {rel}")
            if not list_stale:
                for i, lit in hits[:6]:
                    print(f'         {i}: "{lit}"')
                if len(hits) > 6:
                    print(f"         … 还有 {len(hits) - 6} 处")
    print(f"\n合计 {total} 处未按语言分支的用户可见中文，分布在 {files} 个例程")
    print("（注释里的中文**不计** —— 注释多语化是另一个工作面）")
    return 1 if total else 0


if __name__ == "__main__":
    sys.exit(main())
