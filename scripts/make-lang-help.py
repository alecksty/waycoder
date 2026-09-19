#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
从 VML 源码生成手机端「22 种语言」的三级说明页。

## 为什么要生成，而不是手写

每个前端编译器目录下**本来就有两份权威文档**：`README.md`（功能/编译模式/完成度）
与 `<语言>_LANGUAGE_SPEC.md`（语言规范：语法、类型、标准库）。
手写一遍等于把它们抄错一遍，而且上游一改就漂。

所以这里的做法是：**上游原文原样嵌入，只在前面加一小段 WayCoder 侧的东西**
（怎么在手机上跑、有哪些现成例子、实测踩过的坑）——
那一小段是上游文档里没有、只有本仓库才知道的信息。

## 用法

    python3 scripts/make-lang-help.py            # 生成
    python3 scripts/make-lang-help.py --check    # 只核对，不写（CI/自测用）

⚠ 上游文档的标题要**降两级**再嵌（页面自己占一级、章节占二级）。
   降级时**必须跳过围栏代码块** —— 否则 C 代码里的 `#include` 会被当成标题。
"""

import os
import re
import sys
import glob

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PREPARES = os.path.join(ROOT, "third_party", "vml", "VMLPrepares")
EXAMPLES = os.path.join(ROOT, "third_party", "vml", "Examples")
OUT = os.path.join(ROOT, "WayCoder.Maui", "Resources", "Raw", "help", "vml", "lang")

# ---------------------------------------------------------------- 每门语言

# key = 帮助页 id 的最后一段 / Examples 子目录名
# dir = VMLPrepares 下的编译器目录名
# ext = 示例源码扩展名（用于拼「怎么跑」那一行）
LANGS = [
    dict(key="c", title="C", dir="CCompiler", ext=".c",
         intro="**最完整的一条路**：绘图、输入、音效、存档、手柄全都用得上。"
               "仓库里那几个完整的游戏（俄罗斯方块、五子棋、吃豆人）都是 C 写的。\n\n"
               "如果你不确定用哪门语言 —— 用 C。",
         tips=["引 `#include <waycoder_ui.h>` 就有全部接口声明",
               "颜色一律 `0xAARRGGBB`（alpha 在最前）",
               "数组别越界（报「内存错误」十有八九是它）"],
         pitfalls="- **编译最慢**：一份带标准库的程序，手机上要**一两分钟**（Python/Lua 那类只要几秒）。\n"
                  "  同一份反复跑的话，先编成 `.vmb` 再跑，快很多。\n"
                  "- 全局数组的初始化器里带**负数**时注意（历史缺陷，已修；写方向表这类东西记得测一下）。"),

    dict(key="cpp", title="C++", dir="CppCompiler", ext=".cpp",
         intro="与 C 同一套接口，可以写类。适合把游戏逻辑整理成对象。",
         tips=["和 C 一样引 `waycoder_ui.h`",
               "支持类与一部分模板；**不要依赖完整的 STL**（标准库是 VML 自己的实现）",
               "全局对象的构造时机与 C++ 标准不一定一致 —— 简单起见把状态放在 `main` 里"],
         ),

    dict(key="csharp", title="C#", dir="CSharpCompiler", ext=".cs",
         intro="写得像桌面 C#，入口固定是一个叫 `P` 的类。",
         tips=["入口是 `class P { static void Main() }`",
               "`System.Console.WriteLine` 能直接用",
               "状态放 `static` 字段或数组里"],
         ),

    dict(key="objc", title="Objective-C", dir="ObjCCompiler", ext=".m",
         intro="语法是 C 的超集，UI 接口用法与 C 完全相同。",
         tips=["直接调用 `ui_*` 函数",
               "可以写 C 风格的代码，也可以用 `@interface`"],
         pitfalls="- ⚠ **这个前端解析不了 `waycoder_ui.h`**（会报 `expected )`）——\n"
                  "  **别引头文件**，直接调用即可（`examples/objc/snake.m` 就是这么写的）。"),

    dict(key="java", title="Java", dir="JavaCompiler", ext=".java",
         intro="写法与桌面 Java 接近，外部函数用 `native` 声明。",
         tips=["入口固定 `public class P { public static void main(String[] a) }`",
               "用到哪个 `ui_*` 就声明哪一行 `static native ...`",
               "`System.out.println` 能直接用"],
         ),

    dict(key="kotlin", title="Kotlin", dir="KotlinCompiler", ext=".kt",
         intro="与 Java 同一套写法，语法更短。",
         tips=["入口 `fun main()`",
               "与 Java 一样直接调用 `ui_*`",
               "**状态数组写在 `main` 里面**（见下面那条坑）"],
         pitfalls="- ⚠ **文件级（顶层）的 `arrayOf` 读回是 0** —— 状态数组请写在 `main` 内部。\n"
                  "- ⚠ `step` 是前端保留字，别拿它当变量名。"),

    dict(key="swift", title="Swift", dir="SwiftCompiler", ext=".swift",
         intro="写法自然，示例里那个飞机大战是完整可玩的。",
         tips=["直接调用 `ui_*`，不需要声明",
               "注意返回值要显式接住"],
         ),

    dict(key="go", title="Go", dir="GoCompiler", ext=".go",
         intro="写法与桌面 Go 接近，示例里的贪吃蛇是完整游戏。",
         tips=["入口 `package main` + `func main()`",
               "直接用 `:=` 声明变量",
               "数组与循环都正常（这一路修得比较完整）"],
         ),

    dict(key="rust", title="Rust", dir="RustCompiler", ext=".rs",
         intro="能写，但别指望完整的标准库。",
         tips=["入口 `fn main()`",
               "直接调用 `ui_*`",
               "**数组字面量要写在一行**（见下面那条坑）"],
         pitfalls="- ⚠ 数组字面量**必须写在一行**（跨行的 `];` 会报非法 token）。\n"
                  "- 部分标准库函数没有 —— 用之前先试。"),

    dict(key="d", title="D", dir="DCompiler", ext=".d",
         intro="C 风格的语法，写起来比 C 宽松一些。",
         tips=["入口 `void main()`",
               "直接调用 `ui_*`",
               "与 C 一样注意数组越界"],
         ),

    dict(key="dart", title="Dart", dir="DartCompiler", ext=".dart",
         intro="写法接近，外部函数用 `external` 声明。",
         tips=["入口 `void main()`",
               "用到哪个 `ui_*` 就 `external` 声明哪一行"],
         ),

    dict(key="python", title="Python", dir="PythonCompiler", ext=".py",
         intro="**写起来最快的一门**。语法几乎就是桌面 Python，改一行跑一次很舒服。",
         tips=["直接调用 `ui_*`，不需要声明",
               "编译快（几秒），适合反复改",
               "**可变网格（棋盘、地图）用共享库的整数网格**，不要用 Python 列表（见下面那条坑）"],
         pitfalls="- ⚠ **列表「写不生效」**：`b[i] = v` 之后读回来还是 0（嵌套列表也错）。\n"
                  "  棋盘这类请改用共享库的整数网格：\n"
                  "  ```python\n"
                  "  ui_gclear()\n"
                  "  ui_gset(3, 1)      # 第 3 格\n"
                  "  v = ui_gget(3)\n"
                  "  ```\n"
                  "  共 256 个格子，够放 10×20 的棋盘。"),

    dict(key="javascript", title="JavaScript", dir="JavaScriptCompiler", ext=".js",
         intro="写法接近，但入口要自己调一次。",
         tips=["定义 `function main()` 之后**记得手动调用一次**",
               "直接调用 `ui_*`"],
         ),

    dict(key="lua", title="Lua", dir="LuaCompiler", ext=".lua",
         intro="轻快的小语言，写小游戏很舒服，编译也快。",
         tips=["直接调用 `ui_*`",
               "编译快，适合反复试"],
         ),

    dict(key="ruby", title="Ruby", dir="RubyCompiler", ext=".rb",
         intro="能写，但这个前端支持的特性最少 —— **完全平铺着写**。",
         tips=["全部逻辑写在顶层，不要包函数、不要嵌套",
               "直接调用 `ui_*`"],
         pitfalls="- ⚠ **不认 `&&`**（用 `and`，或者分开写 `if`）。\n"
                  "- ⚠ **`def` 会报 `Unexpected token: End`** —— 也就是说这一路**一个函数都写不了**，\n"
                  "  只能平铺（`examples/ruby/catch.rb` 就是这么写的）。"),

    dict(key="r", title="R", dir="RCompiler", ext=".r",
         intro="向量语言，写法与桌面 R 接近。",
         tips=["直接调用 `ui_*`",
               "状态可以放向量里"],
         ),

    dict(key="pascal", title="Pascal", dir="PascalCompiler", ext=".pas",
         intro="结构清楚，写游戏状态机很合适。",
         tips=["入口 `program p; begin ... end.`",
               "全局数组 + 无参过程是这一路的强项（`examples/pascal/catch.pas` 就是这个结构）"],
         pitfalls="- ⚠ **注释是 `{ }` 不是 `//`** —— 用 `//` 的话整行会被当成代码。\n"
                  "- ⚠ **注释里只能写 ASCII**：中文破折号、中文逗号都会报「未知字符」。"),

    dict(key="fortran", title="Fortran", dir="FortranCompiler", ext=".f90",
         intro="科学计算那套写法在这里也能用。",
         tips=["入口 `program p ... end program p`",
               "用 `call` 调无返回值的过程"],
         pitfalls="- `if` 条件里解析不了「紧跟括号的除法」—— 先算进变量再判。"),

    dict(key="basic", title="BASIC", dir="BasicCompiler", ext=".bas",
         intro="老式写法，命令式一行一行往下走。",
         tips=["用到的每个 `ui_*` 都要 `NATIVE FUNCTION` / `NATIVE SUB` 声明",
               "无返回值的过程**裸调**（不写括号也可以）"],
         pitfalls="- 形参名不能叫 `on`（关键字）。"),

    dict(key="forth", title="Forth", dir="ForthCompiler", ext=".fth",
         intro="栈式语言，写起来完全是另一种思路。",
         tips=["字符串用 `S\" ...\"` 压栈，函数在后面",
               "参数顺序就是压栈顺序"],
         ),

    dict(key="scheme", title="Scheme", dir="SchemeCompiler", ext=".scm",
         intro="Lisp 方言，括号就是一切。",
         tips=["一切皆表达式：`(函数 参数…)`",
               "**游戏请写成扁平顶层程序**（见下面那条坑）"],
         pitfalls="- ⚠ **用户函数看不见顶层变量**（两边的帧指针会互相踩）。\n"
                  "  正解：游戏逻辑全写在顶层，只把「参数全部传进去、不碰全局」的**纯函数**抽出去。"),

    dict(key="ladder", title="Ladder", dir="LadderCompiler", ext=".ld",
         intro="梯形图（PLC）风格的前端 —— 它**做不了手机界面程序**。\n\n"
               "这一路能用的是「编译与运行」本身，UI 那整套接口（开窗/绘图/输入）没有对应语法"
               "（没有「带字符串参数的函数调用」）。",
         tips=["只能写声明与简单逻辑，**不能调 UI 接口**"],
         pitfalls="- 想做手机界面程序，换一门语言（C / Python / Lua 都可以）。"),
]

# 示例文件的说明（键 = 「语言/文件名」）。没列到的文件在表尾合并成一行。
EXAMPLE_DESC = {
    "c/draw_prims.c": "图元体检：每种绘图指令各画一格",
    "c/gomoku.c": "五子棋（触摸落子、人机对战、只竖屏 + 不要手柄区）",
    "c/mario.c": "横版跳跃",
    "c/pacman.c": "吃豆人（网格地图 + 追击 AI）",
    "c/starfall.c": "竖版弹幕",
    "c/sysinfo.c": "调 `ui_call_json(\"sysinfo\")` 打印设备信息",
    "c/tetris.c": "俄罗斯方块（计分、等级、重开、音效）",

    "cpp/snake.cpp": "贪吃蛇",
    "cpp/sysinfo.cpp": "设备信息",

    "csharp/file_io.cs": "读写文件",
    "csharp/racer.cs": "赛车",
    "csharp/snake.cs": "贪吃蛇",
    "csharp/sysinfo.cs": "设备信息",

    "objc/file_io.m": "读写文件",
    "objc/parserexpf_demo.m": "调用共享库解析表达式",
    "objc/snake.m": "贪吃蛇（**不引头文件**的写法看这份）",
    "objc/sysinfo.m": "设备信息",

    "java/catch.java": "接方块（挡板 + 球 + 计分 + 结束重开）",
    "java/file_io.java": "读写文件",
    "java/sysinfo.java": "设备信息",

    "kotlin/catch.kt": "接方块",
    "kotlin/sysinfo.kt": "设备信息",

    "swift/file_io.swift": "读写文件",
    "swift/plane.swift": "飞机空战",
    "swift/snake.swift": "贪吃蛇",
    "swift/sysinfo.swift": "设备信息",

    "go/snake.go": "贪吃蛇",
    "go/sysinfo.go": "设备信息",

    "rust/breakout.rs": "打砖块（24 块砖的数组遍历 + 矩形碰撞）",
    "rust/sysinfo.rs": "设备信息",

    "d/catch.d": "接方块",
    "d/sysinfo.d": "设备信息",

    "dart/catch.dart": "接方块",
    "dart/parserexpf_demo.dart": "调用共享库解析表达式",
    "dart/sysinfo.dart": "设备信息",

    "python/tetris.py": "俄罗斯方块",
    "python/sysinfo.py": "设备信息",

    "javascript/bench.js": "跑分",
    "javascript/catch.js": "接方块",
    "javascript/file_io.js": "读写文件",
    "javascript/sysinfo.js": "设备信息",

    "lua/life.lua": "生命游戏（双缓冲 + 八邻域求和）",
    "lua/sysinfo.lua": "设备信息",

    "ruby/catch.rb": "接方块（完全平铺的一份，这一路写不了函数）",
    "ruby/file_io.rb": "读写文件",
    "ruby/sysinfo.rb": "设备信息",

    "r/catch.r": "接方块",
    "r/file_io.r": "读写文件",
    "r/parserexpf_demo.r": "调用共享库解析表达式",
    "r/sysinfo.r": "设备信息",

    "pascal/catch.pas": "接方块（全局数组 + 无参过程，结构上最像 C 版的对照）",
    "pascal/sysinfo.pas": "设备信息",

    "fortran/bench.f90": "跑分",
    "fortran/sokoban.f90": "推箱子（双数组状态 + 移动规则 + 过关判定）",
    "fortran/sysinfo.f90": "设备信息",

    "basic/tetris.bas": "俄罗斯方块",
    "basic/whack.bas": "打地鼠（随机数 + 离散光标 + 秒级倒计时）",
    "basic/sysinfo.bas": "设备信息",

    "forth/parserexp_demo.fs": "调用共享库解析表达式",
    "forth/sysinfo.fth": "设备信息",

    "scheme/catch.scm": "接方块",
    "scheme/sysinfo.scm": "设备信息",

    "ladder/file_io.ld": "空测试（NOP）",
    "ladder/sysinfo.ld": "占位 —— 这一路调不了 JSON 接口",
}

# 全角/半角混着写容易错，标题里统一用半角括号
HEAD = """# {title}

{intro}

## 在手机上怎么跑

```
vml run examples/{key}/{sample}
```

编译要等一会儿（C 那种要一两分钟，脚本类语言几秒）。程序跑起来后**屏幕底部就是手柄**，
方向键 + 四个动作键都在；点右上角返回可以回到命令行。

## 写法要点

{tips}
"""



def bold_to_markup(md: str) -> str:
    """
    把 markdown 的 `**粗体**` 转成仓库的 `«bold»…«/»` 中间格式。

    渲染端（`MarkupToFormattedString.RenderInline`）**只认 «» 标记**，不解析 markdown 的
    `**` —— 不转的话，正文里满屏都是字面量星号（上游那两份文档几乎每段都有）。
    只做 `**`，不做 `*斜体*`：单个星号在正文里可能是乘号或列表符号，误伤的代价更大。

    ⚠ 同样要跳过围栏代码块 —— 代码里的 `**` 是乘方或指针，动了就改了代码。
    """
    out, in_fence = [], False
    for line in md.split("\n"):
        if line.lstrip().startswith("```"):
            in_fence = not in_fence
            out.append(line)
            continue
        out.append(line if in_fence else re.sub(r"\*\*(.+?)\*\*", r"«bold»\1«/»", line))
    return "\n".join(out)

def demote(md: str, levels: int = 2) -> str:
    """
    把 markdown 的标题整体降级。**必须跳过围栏代码块**。

    C 代码里满是 `#include`、`#define`，那不是标题；不跳围栏的话它们会被连升两级，
    整段代码就散架了。
    """
    out = []
    in_fence = False
    for line in md.split("\n"):
        if line.lstrip().startswith("```"):
            in_fence = not in_fence
            out.append(line)
            continue
        if in_fence:
            out.append(line)
            continue
        m = re.match(r"^(#{1,6})(\s+)(.*)$", line)
        if m:
            lvl = min(len(m.group(1)) + levels, 6)
            out.append("#" * lvl + m.group(2) + m.group(3))
        else:
            out.append(line)
    return "\n".join(out)


def find_doc(dirname: str, pattern: str):
    """按**不区分大小写**找文档 —— 各编译器的文件名大小写并不统一
    （`Cpp_LANGUAGE_SPEC.md` 但 `CSHARP_LANGUAGE_SPEC.md`）。"""
    hits = [p for p in glob.glob(os.path.join(PREPARES, dirname, "*.md"))
            if re.fullmatch(pattern, os.path.basename(p), re.IGNORECASE)]
    return hits[0] if hits else None


def examples_table(lang) -> str:
    d = os.path.join(EXAMPLES, lang["key"])
    files = sorted(f for f in os.listdir(d)
                   if os.path.isfile(os.path.join(d, f))) if os.path.isdir(d) else []
    rows, rest = [], []
    for f in files:
        desc = EXAMPLE_DESC.get(f"{lang['key']}/{f}")
        if desc:
            rows.append(f"| `{f}` | {desc} |")
        else:
            rest.append(f)
    if not rows and not files:
        return ""
    s = "## 示例\n\n| 文件 | 演示什么 |\n|---|---|\n" + "\n".join(rows) + "\n"
    if rest:
        s += "\n同目录还有：" + "、".join(f"`{f}`" for f in rest) + "\n"
    return s


def build(lang) -> str:
    readme = find_doc(lang["dir"], r"README\.md")
    spec = find_doc(lang["dir"], r".*_LANGUAGE_SPEC\.md")
    if not readme or not spec:
        raise SystemExit(f"✘ {lang['key']}: 找不到 README.md 或 LANGUAGE_SPEC.md")

    read = lambda p: open(p, encoding="utf-8").read().replace("\r\n", "\n").strip("\n")

    # 挑一个存在的示例文件来拼「怎么跑」（每个语言都有 sysinfo，兜底用它）
    d = os.path.join(EXAMPLES, lang["key"])
    exts = [f for f in sorted(os.listdir(d))
            if os.path.isfile(os.path.join(d, f)) and f.endswith(lang["ext"])]
    sample = exts[0] if exts else "sysinfo" + lang["ext"]

    tips = bold_to_markup("\n".join("- " + t for t in lang["tips"]))
    body = HEAD.format(title=lang["title"], intro=bold_to_markup(lang["intro"]), key=lang["key"],
                       sample=sample, tips=tips)

    ex = examples_table(lang)
    if ex:
        body += "\n" + ex

    if lang.get("pitfalls"):
        body += "\n## 实测踩过的坑\n\n" + bold_to_markup(lang["pitfalls"]) + "\n"

    body += ("\n---\n\n下面的内容是**从 VML 源码里直接带的**（"
             f"`third_party/vml/VMLPrepares/{lang['dir']}/`）：\n"
             "`README` 讲这个前端支持什么、怎么编；`语言规范` 讲语法本身。\n"
             "上游一改，这里重新生成就是最新的。\n")

    body += "\n## 语言规范\n\n" + demote(bold_to_markup(read(spec))) + "\n"
    body += "\n## 编译器 README\n\n" + demote(bold_to_markup(read(readme))) + "\n"
    return body


def index_page() -> str:
    """
    「22 种语言」这一页 —— **层级靠链接表达**，不写死在代码里。

    原先这棵树是 `HelpCatalog.Topic.Children` 里的一组 C# 数组，后果有两个：
    ① 每加一层都要动代码、动列表页；② "哪些节点是目录、哪些有正文"这个判断漏一处，
    现象是**点下去什么也不发生**（实测就坏过：点「22 种语言」去开一个从不存在的
    `help/vml/languages.md`）。改成正文里写链接之后，**多少级都行，加页面只写 markdown**。
    """
    rows = "\n".join(f"| [{l['title']}](help:vml/lang/{l['key']}) | {bold_to_markup(l['intro']).splitlines()[0]} |"
                     for l in LANGS)
    return f"""# 22 种语言

同一套 VML 接口，22 个前端都能用。**你熟悉哪门就用哪门写**，编出来的东西跑在同一台虚拟机上。

## 每种语言一份说明

点进任意一门，里面有它的**语言规范**（语法/类型/标准库）、**这个前端支持什么**、
以及**实测踩过的坑** —— 后两类是直接从 VML 源码各编译器的 `README.md` /
`<语言>_LANGUAGE_SPEC.md` 带过来的，上游一改、这里重新生成就是最新的。

| 语言 | 一句话 |
|---|---|
{rows}

## 先跑一个看看

```
vml run examples/c/tetris.c        # 俄罗斯方块
vml run examples/python/tetris.py  # 同一个游戏，Python 版
vml run examples/c/gomoku.c        # 五子棋
vml run examples/lua/life.lua      # 生命游戏
vml run examples/basic/whack.bas   # 打地鼠
```

每门语言目录下还有一个 `sysinfo.<扩展名>`，它调用 `ui_call_json("sysinfo", "")`
把设备信息打出来 —— **想知道这门语言能不能用，先跑它**。
"""


def main():
    check = "--check" in sys.argv
    os.makedirs(OUT, exist_ok=True)
    n = 0
    targets = [(l["key"] + ".md", build(l)) for l in LANGS]
    targets.append(("languages.md", index_page()))   # 上一级那一页（落在 vml/ 下，见下面）
    for name, text in targets:
        path = os.path.join(OUT, name) if name != "languages.md" else \
            os.path.join(os.path.dirname(OUT), "languages.md")
        if check:
            old = open(path, encoding="utf-8").read() if os.path.exists(path) else None
            if old != text:
                print(f"✘ {name} 与生成结果不一致（重跑一次本脚本）")
                n += 1
            continue
        with open(path, "w", encoding="utf-8", newline="") as f:
            f.write(text)
        n += 1
    print(("✘ 有 %d 个页面不一致" % n) if check and n else
          ("✔ 全部一致" if check else "✔ 生成 %d 个说明页 → %s" % (n, OUT)))


if __name__ == "__main__":
    main()
