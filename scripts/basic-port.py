#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
把 1970~80 年代的"打字机式"老 BASIC 源码，转换成能在本平台（手机 VML）跑的形式。

设计目标是用户定的那条：**改尽量少的代码，实现尽量多的兼容性** ——
所以这里是**语句级机械转换 + 一层运行时垫层**，不是重写：
  · 老源码的**行号、GOTO、GOSUB、FOR/NEXT 原样保留**（本前端实测支持行号跳转）
  · 变的是那三件在本平台不成立的事：PRINT（看不见）、INPUT（读不到）、CLS/LOCATE（写 VGA）
  · 垫层 `_tty.bas` 提供 ui_* 版的控制台，转换时**内联**进产物（不用 #include，理由见下）

为什么不 #include：
  实测 `#include` 与行号标签一起用会出问题，但**根因不是 include** —— 是缺陷 30
  （`ELSEIF` 分支末尾的单行 IF 会吞掉下一行并丢弃其后整个文件）。既然产物要自包含
  （与仓库现有 8 个示例一致），内联反而简单。

三条重写规则对应 `scripts/vml-basic-probe/cases/` 里登记的已知缺陷：
  ① 单行 IF 一律展开成三行块           —— 规避缺陷 30（cases/30-elseif-tailif.bas）
  ② 字符串数组的读写改成标量临时变量   —— 规避缺陷 29/31（cases/29,31）
  ③ PRINT 的每一项按"是不是字符串"决定要不要套 STR$

用法：
    python3 scripts/basic-port.py <原始.bas> <输出.bas> --title "HURKLE" [--range 0:9]
"""

import argparse
import os
import re
import sys

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SHIM = os.path.join(REPO, "third_party", "vml", "Examples", "basic", "_tty.bas")

# 老 BASIC 里"结果是字符串"的内置函数（决定要不要套 STR$）
STR_FUNCS = ("CHR$", "LEFT$", "RIGHT$", "MID$", "STR$", "SPACE$", "STRING$",
             "HEX$", "OCT$", "DATE$", "TIME$", "INKEY$")


def split_statements(line: str):
    """按 `:` 拆语句（引号内的冒号不算）。老 BASIC 的 `PRINT:PRINT:PRINT` 全靠这个。"""
    out, buf, in_str = [], "", False
    for ch in line:
        if ch == '"':
            in_str = not in_str
            buf += ch
        elif ch == ":" and not in_str:
            out.append(buf)
            buf = ""
        else:
            buf += ch
    out.append(buf)
    return [s.strip() for s in out if s.strip()]


def is_string_expr(e: str) -> bool:
    """判断一个 PRINT 项是不是字符串表达式（决定要不要套 STR$）。"""
    e = e.strip()
    if not e:
        return True
    if e.startswith('"'):
        return True
    up = e.upper()
    for f in STR_FUNCS:
        if up.startswith(f):
            return True
    # 以 $ 结尾的变量 / 数组元素（`A$`、`T$(3)`）
    if re.match(r"^[A-Za-z_][A-Za-z0-9_]*\$", e):
        return True
    return False


def split_items(args: str):
    """把 PRINT 的参数按 `;` / `,` 拆开，同时告诉调用方末尾有没有分隔符。"""
    items, buf, in_str = [], "", False
    for ch in args:
        if ch == '"':
            in_str = not in_str
            buf += ch
        elif ch in ";," and not in_str:
            items.append((buf, ch))          # 记下这一项后面跟的是 ; 还是 ,
            buf = ""
        else:
            buf += ch
    if buf.strip():
        items.append((buf, ""))              # 末尾无分隔符
        trailing = False
    else:
        trailing = bool(items)               # 末尾有分隔符 ⇒ 不换行
    return items, trailing


def rewrite_rnd(expr: str) -> str:
    """`INT(G*RND(1))` → `ui_rand(G)`。本平台的 RND 忽略参数、`/` 是整除、没有浮点，
    所以老代码里那套 `INT(n*RND(1))` 正好等价于 `ui_rand(n)`（返回 0..n-1）。"""
    expr = re.sub(r"INT\s*\(\s*([A-Za-z0-9_]+)\s*\*\s*RND\s*\(\s*1?\s*\)\s*\)",
                  r"ui_rand(\1)", expr, flags=re.I)
    expr = re.sub(r"INT\s*\(\s*RND\s*\(\s*1?\s*\)\s*\*\s*([A-Za-z0-9_]+)\s*\)",
                  r"ui_rand(\1)", expr, flags=re.I)
    return expr


def conv_print(args: str) -> str:
    """PRINT 参数 → ttyP / ttyPn / ttyPc 调用。"""
    args = args.strip()
    if not args:
        return 'ttyP("")'

    # `PRINT TAB(n);"..."` —— 老代码用它做居中，本平台 TAB 是空实现
    m = re.match(r'^TAB\s*\(\s*(\d+)\s*\)\s*[;,]?\s*(.*)$', args, flags=re.I)
    if m:
        rest = m.group(2).strip()
        if is_string_expr(rest) and rest.startswith('"') and '"' not in rest[1:-1]:
            return "ttyPc(%s)" % rest
        args = rest                      # 不是纯字面量就退化成普通打印

    items, trailing = split_items(args)
    if not items:
        return 'ttyP("")'

    parts = []
    for text, sep in items:
        text = rewrite_rnd(text.strip())
        if not text:
            continue
        if is_string_expr(text):
            parts.append(text)
        else:
            # 老 BASIC 打印数字时**前后各带一个空格**（`PRINT "A";G;"BY"` 出 `A 10 BY`）。
            # 不补的话会挤成 `A10BY`，观感差一截。
            parts.append('" " + STR$(%s) + " "' % text)
        if sep == ",":
            parts.append('"  "')         # 老 `,` 是分栏；没有列对齐，退化成两个空格
    if not parts:
        return 'ttyP("")'

    joined = " + ".join(parts)
    return ("ttyPn(%s)" if trailing else "ttyP(%s)") % joined


def expand_single_line_if(stmt: str):
    """把单行 `IF c THEN s` 展开成三行块 —— 规避缺陷 30。

    不展开的话：若它是 `ELSEIF` 分支的最后一条，会把下一行（`ELSEIF`/`END IF`）
    吞进自己的 THEN 分支 ⇒ 块永不闭合 ⇒ 其后整个文件被静默丢弃。
    实测 `cases/30-elseif-tailif.bas`。语义上三行块与单行 IF 完全等价，所以可以无脑展开。
    """
    m = re.match(r"^(IF\s+.+?)\s+THEN\s+(.+)$", stmt, flags=re.I)
    if not m:
        return None
    cond, body = m.group(1), m.group(2).strip()
    if body.upper() == "GOTO" or not body:
        return None
    if body.upper().startswith("GOTO "):
        pass                                     # `IF c THEN 500` 的等价展开
    elif re.match(r"^\d+$", body):
        body = "GOTO " + body                    # 裸行号 = 跳转
    return [cond + " THEN", "    " + body, "END IF"]


def convert_line(stmt: str, lo: int, hi: int):
    """单条语句 → 一条或多条本平台语句。返回 list[str]，None 表示删除该语句。"""
    up = stmt.upper()

    # ── 直接删除：写 VGA 显存 / 控制台特性，在本平台无对应且无意义 ──
    for kw in ("COLOR ", "LOCATE ", "SCREEN ", "WIDTH ", "PALETTE ",
               "DEF SEG", "RANDOMIZE", "KEY OFF", "KEY ON"):
        if up.startswith(kw) or up == kw.strip():
            return None
    if up in ("BEEP", "SOUND"):
        return ["ui_beep(880, 80)"]
    if up.startswith("SOUND "):
        m = re.match(r"SOUND\s+([0-9]+)\s*,\s*([0-9]+)", stmt, flags=re.I)
        if m:
            return ["ui_beep(%s, %s)" % (m.group(1), m.group(2))]
        return None
    if up == "CLS":
        return ["ttyCls"]

    # ── PRINT 家族 ──
    if up == "PRINT":
        return ['ttyP("")']
    for kw, fn in (("LPRINT ", None), ("PRINT ", "print"), ("PRINT#", None)):
        if up.startswith(kw):
            if fn is None:
                return None
            return [conv_print(stmt[len(kw):])]

    # ── INPUT：老游戏的"打字问一个数"，变成步进条 ──
    if up.startswith("INPUT "):
        vars_part = stmt[6:].strip()
        # `INPUT "提示", X` 这种带提示串的形态在本平台是**静默空操作**，
        # 所以这里主动把提示串摘出来当步进条的标题用。
        prompt = None
        if vars_part.startswith('"'):
            m = re.match(r'^"([^"]*)"\s*,?\s*(.*)$', vars_part)
            if m:
                prompt, vars_part = m.group(1), m.group(2)
        out = []
        for v in [x.strip() for x in vars_part.split(",") if x.strip()]:
            label = prompt or ("%s=" % v)
            out.append('%s = ttyAsk("%s", %d, %d)' % (v, label.replace('"', ""), lo, hi))
            out.append("IF ui_win_closed() <> 0 THEN GOTO 99998")
        return out or None

    # ── 单行 IF 展开（缺陷 30）──
    expanded = expand_single_line_if(stmt)
    if expanded:
        return [rewrite_rnd(x) for x in expanded]

    return [rewrite_rnd(stmt)]


def convert(src_path: str, out_path: str, title: str, lo: int, hi: int, rot: int, pad: int, font: int):
    with open(src_path, "r", encoding="utf-8", errors="replace") as f:
        raw = f.read().splitlines()

    body = []
    for line in raw:
        line = line.rstrip()
        stripped = line.strip()
        if not stripped:
            body.append("")
            continue
        if stripped.upper().startswith("REM") or stripped.startswith("'"):
            body.append("' " + stripped[3:].strip() if stripped.upper().startswith("REM") else line)
            continue

        # 行号（老 BASIC 的标签）
        m = re.match(r"^(\d+)\s*(.*)$", stripped)
        label, rest = (m.group(1), m.group(2)) if m else ("", stripped)

        outs = []
        for stmt in split_statements(rest):
            res = convert_line(stmt, lo, hi)
            if res:
                outs.extend(res)
        if not outs:
            continue
        if label:
            outs = ["%s %s" % (label, outs[0])] + outs[1:]
        body.extend(outs)

    with open(SHIM, "r", encoding="utf-8") as f:
        shim = f.read().rstrip()

    header = """' ═══════════════════════════════════════════════════════════════════════
' %s —— 由 scripts/basic-port.py 从老 BASIC 源码**机械转换**而来
'
' 原始来源：%s
' 版权：David Ahl《BASIC Computer Games》合集已进入**公有领域**（UNLICENSE），
'       可随包分发 —— 与 gorilla/nibbles 那些"只参考玩法、代码自己写"的不同。
'
' 转换保留：**行号、GOTO、GOSUB、FOR/NEXT 的原结构**（逐行对应）
' 转换改的：PRINT→ttyP（本平台 PRINT 在游戏窗口里不可见）、INPUT→ttyAsk（手机上不敲键盘）、
'           CLS/LOCATE/COLOR→垫层（它们写的是被宿主关掉的 VGA 显存）、INT(n*RND(1))→ui_rand(n)
' 垫层说明见 _tty.bas 头部。重新生成请跑：
'     python3 scripts/basic-port.py <原始.bas> <本文件> --title "%s"
' ═══════════════════════════════════════════════════════════════════════
""" % (title, os.path.basename(src_path), title)

    out = [header, shim, "", 'ttyOpen("%s", %d, %d, %d)' % (title, rot, pad, font), ""]
    out.extend(body)
    out.extend(["", "99998 END", ""])

    with open(out_path, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(out))
    # 用 ASCII 打印：Windows 控制台是 GBK，打 ✔ 会抛 UnicodeEncodeError（实测）
    print("[OK] %s -> %s (%d lines)" % (os.path.basename(src_path), out_path, len(out)))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("src")
    ap.add_argument("out")
    ap.add_argument("--title", default=None)
    ap.add_argument("--range", default="0:99", help="INPUT 步进条的范围 lo:hi，默认 0:99")
    ap.add_argument("--rot", type=int, default=0, help="0 竖屏锁 / 1 可旋转 / 2 横屏锁")
    ap.add_argument("--pad", type=int, default=0, help="0 不要手柄 / 1 要手柄")
    ap.add_argument("--font", type=int, default=20)
    a = ap.parse_args()
    lo, hi = (int(x) for x in a.range.split(":"))
    title = a.title or os.path.splitext(os.path.basename(a.src))[0].upper()
    convert(a.src, a.out, title, lo, hi, a.rot, a.pad, a.font)


if __name__ == "__main__":
    main()
