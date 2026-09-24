#!/usr/bin/env python3
"""从 C 头文件 `Lib/c/waycoder_ui.h` 生成 BASIC 头文件 `Examples/basic/waycoder_ui.bi`。

为什么要生成而不是手写：那 89 个 `ui_*` 接口的**真源是 C 头文件**（宿主侧实现、
`Lib/c/waycoder_ui.h` 的注释、`docs/VML宿主接口.md` 都对着它）。手抄一份 BASIC 声明
= 手抄一份平行表，而本仓头号坑就是「同一规则两处实现只修了一处」——
接口一改（加参数 / 改名 / 换类型），BASIC 那边就会静默地按旧签名发调用。

用法：
    python3 scripts/make-basic-ui-bi.py            # 就地重写 .bi

⚠ 改接口的顺序：**先改 `Lib/c/waycoder_ui.h`，再重跑本脚本**。

判据：`scripts/vml-basic-probe/run.sh 52`（`'$INCLUDE` 的元命令判据 ——
元命令认得出、相对路径以包含者目录为基准、嵌套包含也成立）。
⚠ 生成物的**可编译性**另由 `Examples/basic/waycoder_ui.bi` 自身保证：
它随包发到手机，任何一条声明写错都会在使用它的程序上现形 ——
而"生成脚本跑过一遍"只能证明脚本没崩，证明不了声明对（别把这两件事混起来）。
"""

import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)
HEADER = os.path.join(REPO, "third_party/vml/Lib/c/waycoder_ui.h")
LEXER = os.path.join(REPO, "third_party/vml/VMLPrepares/BasicCompiler/Lexer.cs")
OUT = os.path.join(REPO, "third_party/vml/Examples/basic/waycoder_ui.bi")


def basic_keywords():
    """从 BASIC 前端的词法器里**读出**关键字表（不另抄一份）。

    形参名撞关键字会把 NATIVE 声明弄坏（例子文件里记着实测：形参不能叫 `on`）——
    而"哪些词是关键字"只有词法器知道，手列一张表迟早漂。
    """
    src = open(LEXER, encoding="utf-8").read()
    return {m.group(1).lower() for m in re.finditer(r'\{\s*"([^"]+)"\s*,\s*TokenType\.', src)}


def parse_params(text):
    """`char* title, int w, int h` → [("char*", "title"), ("int", "w"), ("int", "h")]"""
    text = " ".join(text.split())
    if not text or text == "void":
        return []
    out = []
    for part in text.split(","):
        part = part.strip()
        m = re.match(r"^(.*?[\*\s])([A-Za-z_][A-Za-z0-9_]*)$", part)
        if not m:
            raise SystemExit("认不出的形参：%r" % part)
        out.append((" ".join(m.group(1).split()), m.group(2)))
    return out


BASIC_TYPE = {
    "char*": "STRING",
    "char *": "STRING",
    "int": "INTEGER",
    "int*": "INTEGER",
    "int *": "INTEGER",
    "long": "INTEGER",
    "float": "SINGLE",
    "double": "DOUBLE",
}


def main():
    src = open(HEADER, encoding="utf-8").read()
    src = re.sub(r"/\*.*?\*/", "", src, flags=re.S)      # 块注释
    src = re.sub(r"//[^\n]*", "", src)                    # 行注释

    decls = re.findall(
        r"\b(int|void|char\s*\*|float|double|long)\s+(ui_[A-Za-z0-9_]+)\s*\(([^;]*?)\)\s*;",
        src, flags=re.S)
    if not decls:
        raise SystemExit("一个声明都没解析出来 —— 头文件格式变了？")

    kw = basic_keywords()
    lines = []
    for ret, name, params in decls:
        args = []
        for i, (ctype, pname) in enumerate(parse_params(params)):
            btype = BASIC_TYPE.get(ctype)
            if btype is None:
                raise SystemExit("%s 的形参类型 %r 还没有映射（补 BASIC_TYPE）" % (name, ctype))
            # ⚠ 形参名撞 BASIC 关键字会把整条 NATIVE 声明弄坏（实测 `on`）——
            #   改名成**位置名**。形参在 ABI 上就是位置，名字纯给人看。
            safe = pname.lower() not in kw and not pname.lower().startswith("fn")
            arg = "%s AS %s" % (pname if safe else "p%d" % i, btype)
            args.append(arg)

        if ret == "void":
            lines.append("NATIVE SUB %s(%s)" % (name, ", ".join(args)))
        else:
            rtype = BASIC_TYPE.get(ret, "INTEGER")
            lines.append("NATIVE FUNCTION %s(%s) AS %s" % (name, ", ".join(args), rtype))

    head = """' ═══════════════════════════════════════════════════════════════════════════
' WayCoder UI 接口 —— BASIC 侧的 `NATIVE` 声明表
'
' 用法（**一行即可**，写新版 UI 程序不用再抄一屏声明）：
'
'     '$INCLUDE: 'waycoder_ui.bi'
'
'     r = ui_win_open("演示", 360, 620)
'     DO WHILE ui_win_closed() = 0
'       ui_clear(0)
'       ui_text 20, 30, "你好", 15, 24, 0
'       ui_present
'     LOOP
'
' ⚠ 本文件是**生成物**：`python3 scripts/make-basic-ui-bi.py`
'   （真源 = `Lib/c/waycoder_ui.h`）。接口有改动请改那个头文件再重跑，
'   别在这里手改 —— 手改出来的是**第二份签名表**，迟早与宿主不一致。
'
' ⚠ `NATIVE SUB/FUNCTION` = 「这是外部的符号，别加 sub_/func_ 前缀」，
'   **不需要**再写 `END SUB` / `END FUNCTION`（NATIVE 是纯声明，本来就没有体）。
' ⚠ 形参**默认按值传递**（外部函数的约定；BASIC 自己的 SUB/FUNCTION 才是默认按引用）。
'   所以字符串实参传的是**首地址**、整数实参传的是**值**。
' ⚠ 参数类型说明：`char*` → `STRING`，`int`/`int*` → `INTEGER`，`float` → `SINGLE`，
'   `double` → `DOUBLE`。完整语义（每个号干什么）见 `docs/VML宿主接口.md`。
' ═══════════════════════════════════════════════════════════════════════════

"""

    with open(OUT, "w", encoding="utf-8", newline="\n") as f:
        f.write(head + "\n".join(lines) + "\n")
    print("写出 %d 条声明 -> %s" % (len(lines), OUT))
    return 0


if __name__ == "__main__":
    sys.exit(main())
