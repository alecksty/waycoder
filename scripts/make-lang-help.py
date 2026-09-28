#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
从 VML 源码生成手机端「22 种语言」的三级说明页 —— **中文与英文各一份**。

## 为什么要生成，而不是手写

每个前端编译器目录下**本来就有两份权威文档**：`README.md`（功能/编译模式/完成度）
与 `<语言>_LANGUAGE_SPEC.md`（语言规范：语法、类型、标准库）。
手写一遍等于把它们抄错一遍，而且上游一改就漂。

所以这里的做法是：**上游原文原样嵌入，只在前面加一小段 WayCoder 侧的东西**
（怎么在手机上跑、有哪些现成例子、实测踩过的坑）——
那一小段是上游文档里没有、只有本仓库才知道的信息。

## 两份产物落在哪

    help/zh/**   ← 中文
    help/en/**   ← 英文（**路径与 zh 一一对应**）

`HelpCatalog.AssetPath(id)` 会按当前语言取 `help/{zh|en}/{id}.md`，
所以少一个文件、英文界面上那一页就是空的。

## 英文版的上游正文从哪来

上游文档的中文原件（`README.md` / `<语言>_LANGUAGE_SPEC.md`）**一个字都不动**，
英文版读的是**同目录下的孪生文件**：

    CCompiler/README.md          → CCompiler/README.en.md
    CCompiler/C_LANGUAGE_SPEC.md → CCompiler/C_LANGUAGE_SPEC.en.md

⚠ **没有 `.en.md` 时绝不静默回退中文** —— 那会让英文页里混进中文正文
（用户看到的是「这个 App 一半中文一半英文」），而且漏翻永远没人发现。
这里的做法是：在该页里写一段**显式的英文说明**，并在输出里**打印警告**列出缺哪些。

## 用法

    python3 scripts/make-lang-help.py            # 生成（中文 + 英文）
    python3 scripts/make-lang-help.py --check    # 只核对两份产物，不写（CI/自测用）

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
HELP = os.path.join(ROOT, "WayCoder.Maui", "Resources", "Raw", "help")

#: 要产出的两份 —— 目录名 = `L.ResourceLangDir` 的取值
LOCALES = ("zh", "en")
ZH = LOCALES[0]


def out_dir(loc: str) -> str:
    """语言说明页落地的目录（`languages.md` 落在它的上一级 `vml/`）。"""
    return os.path.join(HELP, loc, "vml", "lang")


def index_path(loc: str) -> str:
    """「22 种语言」一览表 —— 落在各自的 `help/<loc>/vml/` 下。"""
    return os.path.join(HELP, loc, "vml", "languages.md")


def T(zh: str, en: str, loc: str) -> str:
    """按语言取文案。中文侧必须传原文，英文侧传译文。"""
    return zh if loc == ZH else en


# ---------------------------------------------------------------- 每门语言

# key = 帮助页 id 的最后一段 / Examples 子目录名
# dir = VMLPrepares 下的编译器目录名
# ext = 示例源码扩展名（用于拼「怎么跑」那一行）
#
# ⚠ 英文字段（`*_en`）**不设中文兜底** —— 缺了就写占位说明 + 打警告（见 build）。
#   22 门语言的 intro_en / tips_en 必须齐全；pitfalls_en 只在有 pitfalls 时要求。
LANGS = [
    dict(key="c", title="C", dir="CCompiler", ext=".c",
         intro="**最完整的一条路**：绘图、输入、音效、存档、手柄全都用得上。"
               "仓库里那几个完整的游戏（俄罗斯方块、五子棋、吃豆人）都是 C 写的。\n\n"
               "如果你不确定用哪门语言 —— 用 C。",
         intro_en="**The most complete path**: drawing, input, sound, save data and the gamepad "
                  "are all available. The full games in this repo (Tetris, Gomoku, Pac-Man) are "
                  "all written in C.\n\n"
                  "If you are not sure which language to pick: use C.",
         tips=["引 `#include <waycoder_ui.h>` 就有全部接口声明",
               "颜色一律 `0xAARRGGBB`（alpha 在最前）",
               "数组别越界（报「内存错误」十有八九是它）"],
         tips_en=["Include `#include <waycoder_ui.h>` and you get every interface declaration",
                  "Colors are always `0xAARRGGBB` (alpha first)",
                  "Do not run arrays out of bounds (a \"memory error\" is almost always this)"],
         pitfalls="- **编译最慢**：一份带标准库的程序，手机上要**一两分钟**（Python/Lua 那类只要几秒）。\n"
                  "  同一份反复跑的话，先编成 `.vmb` 再跑，快很多。\n"
                  "- 全局数组的初始化器里带**负数**时注意（历史缺陷，已修；写方向表这类东西记得测一下）。",
         pitfalls_en="- **The slowest to compile**: a program that pulls in the standard library takes "
                     "**one to two minutes** on a phone (Python/Lua and friends take seconds).\n"
                     "  If you are going to run the same program over and over, compile it to `.vmb` "
                     "first and run that: it is much faster.\n"
                     "- Watch out for **negative numbers** in a global array's initializer (a historical "
                     "defect, since fixed; test it if you write direction tables that way)."),

    dict(key="cpp", title="C++", dir="CppCompiler", ext=".cpp",
         intro="与 C 同一套接口，可以写类。适合把游戏逻辑整理成对象。",
         intro_en="The same interfaces as C, with classes. Good for organising game logic into objects.",
         tips=["和 C 一样引 `waycoder_ui.h`",
               "支持类与一部分模板；**不要依赖完整的 STL**（标准库是 VML 自己的实现）",
               "全局对象的构造时机与 C++ 标准不一定一致 —— 简单起见把状态放在 `main` 里"],
         tips_en=["Include `waycoder_ui.h` just like C",
                  "Classes and some templates are supported; **do not rely on the full STL** "
                  "(the standard library is VML's own implementation)",
                  "Global object construction order may not match the C++ standard: to keep it "
                  "simple, keep state inside `main`"],
         ),

    dict(key="csharp", title="C#", dir="CSharpCompiler", ext=".cs",
         intro="写得像桌面 C#，入口固定是一个叫 `P` 的类。",
         intro_en="Reads like desktop C#; the entry point is always a class named `P`.",
         tips=["入口是 `class P { static void Main() }`",
               "`System.Console.WriteLine` 能直接用",
               "状态放 `static` 字段或数组里"],
         tips_en=["The entry point is `class P { static void Main() }`",
                  "`System.Console.WriteLine` works directly",
                  "Keep state in `static` fields or arrays"],
         ),

    dict(key="objc", title="Objective-C", dir="ObjCCompiler", ext=".m",
         intro="语法是 C 的超集，UI 接口用法与 C 完全相同。",
         intro_en="The syntax is a superset of C, and the UI interfaces are used exactly as in C.",
         tips=["直接调用 `ui_*` 函数",
               "可以写 C 风格的代码，也可以用 `@interface`"],
         tips_en=["Call the `ui_*` functions directly",
                  "You can write C-style code, or use `@interface`"],
         pitfalls="- ⚠ **这个前端解析不了 `waycoder_ui.h`**（会报 `expected )`）——\n"
                  "  **别引头文件**，直接调用即可（`examples/objc/snake.m` 就是这么写的）。",
         pitfalls_en="- ⚠ **This front end cannot parse `waycoder_ui.h`** (it reports `expected )`) -\n"
                     "  **do not include the header**, just call the functions "
                     "(`examples/objc/snake.m` is written that way)."),

    dict(key="java", title="Java", dir="JavaCompiler", ext=".java",
         intro="写法与桌面 Java 接近，外部函数用 `native` 声明。",
         intro_en="Close to desktop Java; external functions are declared `native`.",
         tips=["入口固定 `public class P { public static void main(String[] a) }`",
               "用到哪个 `ui_*` 就声明哪一行 `static native ...`",
               "`System.out.println` 能直接用"],
         tips_en=["The entry point is always `public class P { public static void main(String[] a) }`",
                  "Declare one `static native ...` line for each `ui_*` you use",
                  "`System.out.println` works directly"],
         ),

    dict(key="kotlin", title="Kotlin", dir="KotlinCompiler", ext=".kt",
         intro="与 Java 同一套写法，语法更短。",
         intro_en="The same approach as Java, with shorter syntax.",
         tips=["入口 `fun main()`",
               "与 Java 一样直接调用 `ui_*`",
               "**状态数组写在 `main` 里面**（见下面那条坑）"],
         tips_en=["The entry point is `fun main()`",
                  "Call `ui_*` directly, as in Java",
                  "**Write state arrays inside `main`** (see the pitfall below)"],
         pitfalls="- ⚠ **文件级（顶层）的 `arrayOf` 读回是 0** —— 状态数组请写在 `main` 内部。\n"
                  "- ⚠ `step` 是前端保留字，别拿它当变量名。",
         pitfalls_en="- ⚠ **A file-level (top-level) `arrayOf` reads back as 0** - keep state arrays "
                     "inside `main`.\n"
                     "- ⚠ `step` is a reserved word in this front end; do not use it as a variable name."),

    dict(key="swift", title="Swift", dir="SwiftCompiler", ext=".swift",
         intro="写法自然，示例里那个飞机大战是完整可玩的。",
         intro_en="Natural to write, and the plane shooter example is a complete, playable game.",
         tips=["直接调用 `ui_*`，不需要声明",
               "注意返回值要显式接住"],
         tips_en=["Call `ui_*` directly; no declaration needed",
                  "Remember to capture return values explicitly"],
         ),

    dict(key="go", title="Go", dir="GoCompiler", ext=".go",
         intro="写法与桌面 Go 接近，示例里的贪吃蛇是完整游戏。",
         intro_en="Close to desktop Go; the snake example is a complete game.",
         tips=["入口 `package main` + `func main()`",
               "直接用 `:=` 声明变量",
               "数组与循环都正常（这一路修得比较完整）"],
         tips_en=["The entry point is `package main` plus `func main()`",
                  "Declare variables with `:=` as usual",
                  "Arrays and loops work normally (this path has been fixed up fairly thoroughly)"],
         ),

    dict(key="rust", title="Rust", dir="RustCompiler", ext=".rs",
         intro="能写，但别指望完整的标准库。",
         intro_en="Usable, but do not expect the full standard library.",
         tips=["入口 `fn main()`",
               "直接调用 `ui_*`",
               "**数组字面量要写在一行**（见下面那条坑）"],
         tips_en=["The entry point is `fn main()`",
                  "Call `ui_*` directly",
                  "**Array literals must be written on one line** (see the pitfall below)"],
         pitfalls="- ⚠ 数组字面量**必须写在一行**（跨行的 `];` 会报非法 token）。\n"
                  "- 部分标准库函数没有 —— 用之前先试。",
         pitfalls_en="- ⚠ An array literal **must be on one line** (a multi-line `];` reports an "
                     "illegal token).\n"
                     "- Some standard library functions are missing: try before you rely on them."),

    dict(key="d", title="D", dir="DCompiler", ext=".d",
         intro="C 风格的语法，写起来比 C 宽松一些。",
         intro_en="C-style syntax, a little more relaxed than C to write.",
         tips=["入口 `void main()`",
               "直接调用 `ui_*`",
               "与 C 一样注意数组越界"],
         tips_en=["The entry point is `void main()`",
                  "Call `ui_*` directly",
                  "Watch array bounds, as in C"],
         ),

    dict(key="dart", title="Dart", dir="DartCompiler", ext=".dart",
         intro="写法接近，外部函数用 `external` 声明。",
         intro_en="Close in style; external functions are declared `external`.",
         tips=["入口 `void main()`",
               "用到哪个 `ui_*` 就 `external` 声明哪一行"],
         tips_en=["The entry point is `void main()`",
                  "Declare one `external` line for each `ui_*` you use"],
         ),

    dict(key="python", title="Python", dir="PythonCompiler", ext=".py",
         intro="**写起来最快的一门**。语法几乎就是桌面 Python，改一行跑一次很舒服。",
         intro_en="**The fastest one to write.** The syntax is almost desktop Python, and changing "
                  "a line and running again is a pleasure.",
         tips=["直接调用 `ui_*`，不需要声明",
               "编译快（几秒），适合反复改",
               "**可变网格（棋盘、地图）用共享库的整数网格**，不要用 Python 列表（见下面那条坑）"],
         tips_en=["Call `ui_*` directly; no declaration needed",
                  "Compiles fast (a few seconds), good for repeated edits",
                  "**Use the shared library's integer grid for mutable grids** (boards, maps), "
                  "not a Python list (see the pitfall below)"],
         pitfalls="- ⚠ **列表「写不生效」**：`b[i] = v` 之后读回来还是 0（嵌套列表也错）。\n"
                  "  棋盘这类请改用共享库的整数网格：\n"
                  "  ```python\n"
                  "  ui_gclear()\n"
                  "  ui_gset(3, 1)      # 第 3 格\n"
                  "  v = ui_gget(3)\n"
                  "  ```\n"
                  "  共 256 个格子，够放 10×20 的棋盘。",
         pitfalls_en="- ⚠ **List writes do not stick**: after `b[i] = v`, reading it back still gives "
                     "0 (nested lists are wrong too).\n"
                     "  For things like a board, use the shared library's integer grid instead:\n"
                     "  ```python\n"
                     "  ui_gclear()\n"
                     "  ui_gset(3, 1)      # cell 3\n"
                     "  v = ui_gget(3)\n"
                     "  ```\n"
                     "  There are 256 cells, enough for a 10x20 board."),

    dict(key="javascript", title="JavaScript", dir="JavaScriptCompiler", ext=".js",
         intro="写法接近，但入口要自己调一次。",
         intro_en="Close in style, but you have to call the entry point yourself.",
         tips=["定义 `function main()` 之后**记得手动调用一次**",
               "直接调用 `ui_*`"],
         tips_en=["After defining `function main()` **remember to call it once**",
                  "Call `ui_*` directly"],
         ),

    dict(key="lua", title="Lua", dir="LuaCompiler", ext=".lua",
         intro="轻快的小语言，写小游戏很舒服，编译也快。",
         intro_en="A light, quick little language: pleasant for small games, and it compiles fast.",
         tips=["直接调用 `ui_*`",
               "编译快，适合反复试"],
         tips_en=["Call `ui_*` directly",
                  "Compiles fast, good for experimenting"],
         ),

    dict(key="ruby", title="Ruby", dir="RubyCompiler", ext=".rb",
         intro="能写，但这个前端支持的特性最少 —— **完全平铺着写**。",
         intro_en="Usable, but this front end supports the fewest features of all: "
                  "**write everything flat**.",
         tips=["全部逻辑写在顶层，不要包函数、不要嵌套",
               "直接调用 `ui_*`"],
         tips_en=["Put all the logic at the top level; do not wrap it in functions and do not nest",
                  "Call `ui_*` directly"],
         pitfalls="- ⚠ **不认 `&&`**（用 `and`，或者分开写 `if`）。\n"
                  "- ⚠ **`def` 会报 `Unexpected token: End`** —— 也就是说这一路**一个函数都写不了**，\n"
                  "  只能平铺（`examples/ruby/catch.rb` 就是这么写的）。",
         pitfalls_en="- ⚠ **`&&` is not recognised** (use `and`, or write separate `if` statements).\n"
                     "- ⚠ **`def` reports `Unexpected token: End`** - in other words **you cannot write "
                     "a single function** on this path,\n"
                     "  only flat code (`examples/ruby/catch.rb` is written that way)."),

    dict(key="r", title="R", dir="RCompiler", ext=".r",
         intro="向量语言，写法与桌面 R 接近。",
         intro_en="A vector language, close to desktop R in style.",
         tips=["直接调用 `ui_*`",
               "状态可以放向量里"],
         tips_en=["Call `ui_*` directly",
                  "State can live in vectors"],
         ),

    dict(key="pascal", title="Pascal", dir="PascalCompiler", ext=".pas",
         intro="结构清楚，写游戏状态机很合适。",
         intro_en="Clearly structured; well suited to writing game state machines.",
         tips=["入口 `program p; begin ... end.`",
               "全局数组 + 无参过程是这一路的强项（`examples/pascal/catch.pas` 就是这个结构）"],
         tips_en=["The entry point is `program p; begin ... end.`",
                  "Global arrays plus parameterless procedures are this path's strength "
                  "(`examples/pascal/catch.pas` is built that way)"],
         pitfalls="- ⚠ **注释是 `{ }` 不是 `//`** —— 用 `//` 的话整行会被当成代码。\n"
                  "- ⚠ **注释里只能写 ASCII**：中文破折号、中文逗号都会报「未知字符」。",
         pitfalls_en="- ⚠ **Comments are `{ }`, not `//`** - with `//` the whole line is treated as code.\n"
                     "- ⚠ **Comments may only contain ASCII**: a non-ASCII dash or comma reports an "
                     "\"unknown character\"."),

    dict(key="fortran", title="Fortran", dir="FortranCompiler", ext=".f90",
         intro="科学计算那套写法在这里也能用。",
         intro_en="The scientific-computing style works here too.",
         tips=["入口 `program p ... end program p`",
               "用 `call` 调无返回值的过程"],
         tips_en=["The entry point is `program p ... end program p`",
                  "Use `call` for procedures with no return value"],
         pitfalls="- `if` 条件里解析不了「紧跟括号的除法」—— 先算进变量再判。",
         pitfalls_en="- An `if` condition cannot parse a division right after a bracket: compute it "
                     "into a variable first."),

    dict(key="basic", title="BASIC", dir="BasicCompiler", ext=".bas",
         intro="老式写法，命令式一行一行往下走。",
         intro_en="The old-fashioned style: imperative, one line after another.",
         tips=["用到的每个 `ui_*` 都要 `NATIVE FUNCTION` / `NATIVE SUB` 声明",
               "无返回值的过程**裸调**（不写括号也可以）",
               "**一屏的 `NATIVE` 声明不用自己抄** —— 写一行 `'$INCLUDE: 'waycoder_ui.bi'` "
               "就把整张 `ui_*` 表拉进来了（见 `examples/basic/waycoder_ui.bi`）",
               "`SCREEN`/`CLS`/`PSET`/`LINE`/`CIRCLE`/`PAINT`/`COLOR`/`PALETTE`/`LOCATE`/"
               "`DRAW`/`GET`/`PUT`/`POINT(x,y)`/`WIDTH` **手机上直接可用**（编译器默认翻译成 "
               "宿主的 `ui_*` 图元）；`SCREEN n` 只决定开多大窗口",
               "`PRINT` 在图形模式下落到**窗口里**（不是控制台），中文与框线正常",
               "浮点可用：`PRINT 3.14` 打 `3.14`、`PRINT 7 / 2` 打 `3.5`（`\\` 才是整除）、"
               "整数不打多余的 `.0`（`PRINT 2.0` 打 `2`）",
               "文件 `OPEN`/`CLOSE`/`PRINT #`/`INPUT #`/`LINE INPUT #`/`FREEFILE` 都能用，"
               "路径落在你自己的工作区里（越界会被拒）"],
         tips_en=["Every `ui_*` you use needs a `NATIVE FUNCTION` / `NATIVE SUB` declaration",
                  "Call parameterless procedures **bare** (no brackets needed)",
                  "**You do not have to copy the screenful of `NATIVE` declarations by hand** - write "
                  "one line, `'$INCLUDE: 'waycoder_ui.bi'`, and the whole `ui_*` table is pulled in "
                  "(see `examples/basic/waycoder_ui.bi`)",
                  "`SCREEN`/`CLS`/`PSET`/`LINE`/`CIRCLE`/`PAINT`/`COLOR`/`PALETTE`/`LOCATE`/"
                  "`DRAW`/`GET`/`PUT`/`POINT(x,y)`/`WIDTH` **work directly on the phone** (the "
                  "compiler translates them to the host's `ui_*` shapes by default); `SCREEN n` only "
                  "decides how big the window is",
                  "`PRINT` in graphics mode lands **in the window** (not the console); box-drawing "
                  "characters render correctly",
                  "Floats work: `PRINT 3.14` prints `3.14`, `PRINT 7 / 2` prints `3.5` (`\\` is "
                  "integer division), and integers do not print a trailing `.0` (`PRINT 2.0` "
                  "prints `2`)",
                  "Files work too: `OPEN`/`CLOSE`/`PRINT #`/`INPUT #`/`LINE INPUT #`/`FREEFILE`, with "
                  "paths inside your own workspace (going outside is refused)"],
         pitfalls="\n".join([
             "- 形参名不能叫 `on`（关键字）。",
             "- **`FOR INPUT` 里的 `INPUT` 是关键字**，不是普通标识符 —— "
             "别的模式名（`OUTPUT`/`APPEND`）随便写就行。",
             "- 字符串拼接是好的，但一行里**同时活着的串有上限**（轮转缓冲 8 块），超过会互相覆盖。",
             "- **`FUNCTION` 的返回值槽是整数** —— 想返回浮点得绕（用 `SUB` + 输出参数）。",
         ]),
         pitfalls_en="\n".join([
             "- A parameter cannot be named `on` (it is a keyword).",
             "- **`INPUT` in `FOR INPUT` is a keyword**, not an ordinary identifier - other mode "
             "names (`OUTPUT`/`APPEND`) can be written freely.",
             "- String concatenation works, but **only a limited number of strings can be live at "
             "once on one line** (a rotating buffer of 8); beyond that they overwrite each other.",
             "- **The return-value slot of `FUNCTION` is an integer** - to return a float you have to "
             "work around it (use `SUB` with an output parameter).",
         ])),

    dict(key="forth", title="Forth", dir="ForthCompiler", ext=".fth",
         intro="栈式语言，写起来完全是另一种思路。",
         intro_en="A stack language; writing in it is a completely different way of thinking.",
         tips=["字符串用 `S\" ...\"` 压栈，函数在后面",
               "参数顺序就是压栈顺序"],
         tips_en=["Push strings with `S\" ...\"`; the function comes after",
                  "Argument order is push order"],
         ),

    dict(key="scheme", title="Scheme", dir="SchemeCompiler", ext=".scm",
         intro="Lisp 方言，括号就是一切。",
         intro_en="A Lisp dialect where brackets are everything.",
         tips=["一切皆表达式：`(函数 参数…)`",
               "**游戏请写成扁平顶层程序**（见下面那条坑）"],
         tips_en=["Everything is an expression: `(function argument...)`",
                  "**Write games as a flat top-level program** (see the pitfall below)"],
         pitfalls="- ⚠ **用户函数看不见顶层变量**（两边的帧指针会互相踩）。\n"
                  "  正解：游戏逻辑全写在顶层，只把「参数全部传进去、不碰全局」的**纯函数**抽出去。",
         pitfalls_en="- ⚠ **User functions cannot see top-level variables** (the two frame pointers "
                     "step on each other).\n"
                     "  The fix: write all game logic at the top level, and only pull out **pure "
                     "functions** that take every parameter and touch no globals."),

    dict(key="ladder", title="Ladder", dir="LadderCompiler", ext=".ld",
         intro="梯形图（PLC）风格的前端 —— 它**做不了手机界面程序**。\n\n"
               "这一路能用的是「编译与运行」本身，UI 那整套接口（开窗/绘图/输入）没有对应语法"
               "（没有「带字符串参数的函数调用」）。",
         intro_en="A ladder-diagram (PLC) style front end - it **cannot build phone UI programs**.\n\n"
                  "What works on this path is compiling and running itself; the whole UI interface set "
                  "(windows, drawing, input) has no corresponding syntax (there is no \"function call "
                  "with a string argument\").",
         tips=["只能写声明与简单逻辑，**不能调 UI 接口**"],
         tips_en=["Only declarations and simple logic; **you cannot call the UI interfaces**"],
         pitfalls="- 想做手机界面程序，换一门语言（C / Python / Lua 都可以）。",
         pitfalls_en="- For a phone UI program, pick another language (C / Python / Lua all work)."),
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

    "basic/sysinfo.bas": "设备信息",
    "basic/demo_ui.bas": "开窗 / 画图 / 消息循环的最小骨架",
    "basic/tetris.bas": "俄罗斯方块（网格 + 手柄）",
    "basic/whack.bas": "打地鼠（随机数 + 定时器 + 振动）",
    "basic/gorilla.bas": "猴子扔香蕉（完整游戏 + 音效）",
    "basic/gorilla_pro.bas": "同上，加了渐变 / 路径 / 图块",
    "basic/blackjack.bas": "21 点（轮询式消息循环 + 振动）",
    "basic/hammurabi.bas": "汉谟拉比（回合制经营）",
    "basic/lander.bas": "登月舱",
    "basic/nibbles.bas": "贪吃蛇",
    "basic/hurkle.bas": "老式打字机式文本程序（内联了 `_tty.bas`）",
    "basic/demo_tty.bas": "文本模式（ANSI 彩色）",
    "basic/demo_bgi.bas": "图形语句逐条演示",
    "basic/gfx_demo.bas": "同上",
    "basic/gfx_modes.bas": "各 `SCREEN` 模式的分辨率",
    "basic/_tty.bas": "不是独立程序，是「用 `ui_*` 重建等宽字符网格」的垫层",

    "forth/parserexp_demo.fs": "调用共享库解析表达式",
    "forth/sysinfo.fth": "设备信息",

    "scheme/catch.scm": "接方块",
    "scheme/sysinfo.scm": "设备信息",

    "ladder/file_io.ld": "空测试（NOP）",
    "ladder/sysinfo.ld": "占位 —— 这一路调不了 JSON 接口",
}

# 示例表说明的英文版。**键与 EXAMPLE_DESC 完全一致**（少一个键 = 英文页里混进中文）。
EXAMPLE_DESC_EN = {
    "c/draw_prims.c": "Shape check-up: one cell drawn per drawing instruction",
    "c/gomoku.c": "Gomoku (tap to place, play against the computer, portrait only with no gamepad area)",
    "c/mario.c": "Side-scrolling platformer",
    "c/pacman.c": "Pac-Man (grid map plus chasing AI)",
    "c/starfall.c": "Vertical bullet-hell",
    "c/sysinfo.c": "Calls `ui_call_json(\"sysinfo\")` to print device information",
    "c/tetris.c": "Tetris (score, levels, restart, sound)",

    "cpp/snake.cpp": "Snake",
    "cpp/sysinfo.cpp": "Device information",

    "csharp/file_io.cs": "Reading and writing files",
    "csharp/racer.cs": "Racing",
    "csharp/snake.cs": "Snake",
    "csharp/sysinfo.cs": "Device information",

    "objc/file_io.m": "Reading and writing files",
    "objc/parserexpf_demo.m": "Calling a shared-library expression parser",
    "objc/snake.m": "Snake (see this one for the style that includes no header)",
    "objc/sysinfo.m": "Device information",

    "java/catch.java": "Catch the blocks (paddle plus ball plus score plus restart on game over)",
    "java/file_io.java": "Reading and writing files",
    "java/sysinfo.java": "Device information",

    "kotlin/catch.kt": "Catch the blocks",
    "kotlin/sysinfo.kt": "Device information",

    "swift/file_io.swift": "Reading and writing files",
    "swift/plane.swift": "Plane combat",
    "swift/snake.swift": "Snake",
    "swift/sysinfo.swift": "Device information",

    "go/snake.go": "Snake",
    "go/sysinfo.go": "Device information",

    "rust/breakout.rs": "Breakout (iterating a 24-brick array plus rectangle collision)",
    "rust/sysinfo.rs": "Device information",

    "d/catch.d": "Catch the blocks",
    "d/sysinfo.d": "Device information",

    "dart/catch.dart": "Catch the blocks",
    "dart/parserexpf_demo.dart": "Calling a shared-library expression parser",
    "dart/sysinfo.dart": "Device information",

    "python/tetris.py": "Tetris",
    "python/sysinfo.py": "Device information",

    "javascript/bench.js": "Benchmark",
    "javascript/catch.js": "Catch the blocks",
    "javascript/file_io.js": "Reading and writing files",
    "javascript/sysinfo.js": "Device information",

    "lua/life.lua": "Game of Life (double buffering plus eight-neighbour sum)",
    "lua/sysinfo.lua": "Device information",

    "ruby/catch.rb": "Catch the blocks (a completely flat version; this path cannot do functions)",
    "ruby/file_io.rb": "Reading and writing files",
    "ruby/sysinfo.rb": "Device information",

    "r/catch.r": "Catch the blocks",
    "r/file_io.r": "Reading and writing files",
    "r/parserexpf_demo.r": "Calling a shared-library expression parser",
    "r/sysinfo.r": "Device information",

    "pascal/catch.pas": "Catch the blocks (global arrays plus parameterless procedures; structurally "
                        "the closest counterpart to the C version)",
    "pascal/sysinfo.pas": "Device information",

    "fortran/bench.f90": "Benchmark",
    "fortran/sokoban.f90": "Sokoban (two-array state plus movement rules plus level-complete check)",
    "fortran/sysinfo.f90": "Device information",

    "basic/sysinfo.bas": "Device information",
    "basic/demo_ui.bas": "The smallest skeleton: open a window, draw, message loop",
    "basic/tetris.bas": "Tetris (grid plus gamepad)",
    "basic/whack.bas": "Whack-a-mole (random numbers plus timers plus vibration)",
    "basic/gorilla.bas": "Gorillas throwing bananas (a complete game plus sound)",
    "basic/gorilla_pro.bas": "The same, with gradients, paths and tiles added",
    "basic/blackjack.bas": "Blackjack (polling message loop plus vibration)",
    "basic/hammurabi.bas": "Hammurabi (turn-based management)",
    "basic/lander.bas": "Lunar lander",
    "basic/nibbles.bas": "Snake",
    "basic/hurkle.bas": "An old-style typewriter text program (with `_tty.bas` inlined)",
    "basic/demo_tty.bas": "Text mode (ANSI color)",
    "basic/demo_bgi.bas": "A demonstration of the graphics statements, one by one",
    "basic/gfx_demo.bas": "The same as above",
    "basic/gfx_modes.bas": "Resolution of each `SCREEN` mode",
    "basic/_tty.bas": "Not a standalone program: the shim that rebuilds a monospaced character grid "
                      "on top of `ui_*`",

    "forth/parserexp_demo.fs": "Calling a shared-library expression parser",
    "forth/sysinfo.fth": "Device information",

    "scheme/catch.scm": "Catch the blocks",
    "scheme/sysinfo.scm": "Device information",

    "ladder/file_io.ld": "Empty test (NOP)",
    "ladder/sysinfo.ld": "Placeholder - this path cannot call the JSON interface",
}

# 全角/半角混着写容易错，标题里统一用半角括号
HEAD_ZH = """# {title}

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

HEAD_EN = """# {title}

{intro}

## How to run it on your phone

```
vml run examples/{key}/{sample}
```

Compiling takes a while (one to two minutes for C, a few seconds for scripting languages).
Once the program is running, **the gamepad is at the bottom of the screen** - the D-pad
and the four action buttons are all there. Tap the back arrow in the top-right corner to
return to the Shell.

## What to know before you write

{tips}
"""

#: 上游正文缺失 `.en.md` 时写进英文页的**显式说明**（绝不回退中文正文）。
NOTRANS = """
> **This reference section is not available in English yet.** The English twin of the upstream
> document (`{twin}`) has not been written, so it is not shown here. The original lives in the
> app's source at `third_party/vml/VMLPrepares/{dir}/{orig}`.
"""

#: 自己手写的那几段（intro / tips / pitfalls / 示例说明）缺英文字段时的显式说明。
NOTRANS_FIELD = """
> **This section is not available in English yet.**
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
    （`Cpp_LANGUAGE_SPEC.md` 但 `CSHARP_LANGUAGE_SPEC.md`）。

    ⚠ 找英文孪生时传 `README\\.en\\.md` 这类模式。`re.fullmatch` 保证
      `README\\.md` **不会**匹配上 `README.en.md`，所以中文侧不受孪生文件影响。
    """
    hits = [p for p in glob.glob(os.path.join(PREPARES, dirname, "*.md"))
            if re.fullmatch(pattern, os.path.basename(p), re.IGNORECASE)]
    return hits[0] if hits else None


def pick(doc, loc: str, field: str, missing: list, owner: str):
    """取手写字段：中文必填；英文缺失 → 记进 missing 并返回 None（**绝不回退中文**）。"""
    if loc == ZH:
        return doc.get(field)
    val = doc.get(field + "_en")
    if val is None:
        missing.append(f"{owner}: {field}_en")
    return val


def upstream(lang, kind: str, loc: str, missing: list):
    """取上游正文，返回 `(路径 or None, 这一段的「英文孪生文件名」)`。"""
    # **中文原件是锚点**：孪生名由它推出来（`X.md` -> `X.en.md`），既不用猜、
    # 也不会因为 glob 的歧义选错文件；缺了孪生时报告里写的也是**那个确定的名字**。
    orig = find_doc(lang["dir"],
                    r"README\.md" if kind == "readme" else r".*_LANGUAGE_SPEC\.md")
    if orig is None:
        return None, ("README.en.md" if kind == "readme"
                      else "<LANG>_LANGUAGE_SPEC.en.md")
    twin = orig[:-3] + ".en.md"
    if loc == ZH:
        return orig, os.path.basename(twin)
    if os.path.exists(twin):
        return twin, os.path.basename(twin)
    missing.append(os.path.relpath(twin, ROOT).replace(os.sep, "/"))
    return None, os.path.basename(twin)


def examples_table(lang, loc: str) -> str:
    d = os.path.join(EXAMPLES, lang["key"])
    files = sorted(f for f in os.listdir(d)
                   if os.path.isfile(os.path.join(d, f))) if os.path.isdir(d) else []
    desc_tbl = EXAMPLE_DESC if loc == ZH else EXAMPLE_DESC_EN
    rows, rest = [], []
    for f in files:
        desc = desc_tbl.get(f"{lang['key']}/{f}")
        if desc:
            rows.append(f"| `{f}` | {desc} |")
        else:
            rest.append(f)
    if not rows and not files:
        return ""
    s = T("## 示例\n\n| 文件 | 演示什么 |\n|---|---|\n",
          "## Examples\n\n| File | What it shows |\n|---|---|\n", loc) + "\n".join(rows) + "\n"
    if rest:
        s += T("\n同目录还有：", "\nAlso in this folder: ", loc) \
            + T("、", ", ", loc).join(f"`{f}`" for f in rest) + "\n"
    return s


def build(lang, loc: str, missing: list) -> str:
    readme, readme_twin = upstream(lang, "readme", loc, missing)
    spec, spec_twin = upstream(lang, "spec", loc, missing)
    if loc == ZH and (not readme or not spec):
        raise SystemExit(f"✘ {lang['key']}: 找不到 README.md 或 LANGUAGE_SPEC.md")

    read = lambda p: open(p, encoding="utf-8").read().replace("\r\n", "\n").strip("\n")

    # 挑一个存在的示例文件来拼「怎么跑」（每个语言都有 sysinfo，兜底用它）
    d = os.path.join(EXAMPLES, lang["key"])
    exts = [f for f in sorted(os.listdir(d))
            if os.path.isfile(os.path.join(d, f)) and f.endswith(lang["ext"])]
    # ⚠ **别直接取 `sorted(...)[0]`**（v0.96.506 修）：下划线开头的文件排在最前，
    #   而 `_` 前缀在本仓的约定里是**脚手架/被内联的库**（`basic/_tty.bas` 就是
    #   "用 ui_* 重建字符网格"的垫层，**不是能独立跑的程序**）——
    #   于是 BASIC 那一页的「在手机上怎么跑」写着 `vml run examples/basic/_tty.bas`，
    #   照着敲只会得到一个什么都不画的东西。优先挑能独立跑的，垫层一律排除。
    runnable = [f for f in exts if not f.startswith("_")]
    exts = runnable or exts
    sample = next((f for f in exts if f.startswith("sysinfo")), None) \
        or next((f for f in exts if f.startswith(("tetris", "demo_ui"))), None) \
        or (exts[0] if exts else "sysinfo" + lang["ext"])

    intro = pick(lang, loc, "intro", missing, lang["key"])
    tips_list = pick(lang, loc, "tips", missing, lang["key"])
    tips = bold_to_markup("\n".join("- " + t for t in tips_list)) if tips_list \
        else NOTRANS_FIELD.strip()
    header = HEAD_ZH if loc == ZH else HEAD_EN
    body = header.format(title=lang["title"],
                         intro=bold_to_markup(intro) if intro else NOTRANS_FIELD.strip(),
                         key=lang["key"], sample=sample, tips=tips)

    ex = examples_table(lang, loc)
    if ex:
        body += "\n" + ex

    if lang.get("pitfalls"):
        pit = pick(lang, loc, "pitfalls", missing, lang["key"])
        body += T("\n## 实测踩过的坑\n\n", "\n## Pitfalls we hit on real devices\n\n", loc) \
            + (bold_to_markup(pit) if pit else NOTRANS_FIELD.strip()) + "\n"

    if loc == ZH:
        body += ("\n---\n\n下面的内容是**从 VML 源码里直接带的**（"
                 f"`third_party/vml/VMLPrepares/{lang['dir']}/`）：\n"
                 "`README` 讲这个前端支持什么、怎么编；`语言规范` 讲语法本身。\n"
                 "上游一改，这里重新生成就是最新的。\n")
    else:
        body += ("\n---\n\nThe rest of this page is taken straight from the VML source "
                 f"(`third_party/vml/VMLPrepares/{lang['dir']}/`):\n"
                 "the `README` covers what this front end supports and how to compile it, and the\n"
                 "language reference covers the syntax itself. When upstream changes, regenerating\n"
                 "this page brings it up to date.\n")

    sections = (
        ("## 语言规范", "## Language reference", "spec", spec, spec_twin),
        ("## 编译器 README", "## Compiler README", "readme", readme, readme_twin))
    for heading_zh, heading_en, kind, path, twin in sections:
        body += "\n" + T(heading_zh, heading_en, loc) + "\n\n"
        if path is None:
            # 英文侧的上游孪生文档还不存在 —— 写显式说明，**不回退中文正文**。
            body += demote(NOTRANS.format(twin=twin, dir=lang["dir"],
                                          orig=twin[:-len(".en.md")] + ".md").lstrip("\n"))


        else:
            body += demote(bold_to_markup(read(path))) + "\n"
    return body


def index_page(loc: str) -> str:
    """
    「22 种语言」这一页 —— **层级靠链接表达**，不写死在代码里。

    原先这棵树是 `HelpCatalog.Topic.Children` 里的一组 C# 数组，后果有两个：
    ① 每加一层都要动代码、动列表页；② "哪些节点是目录、哪些有正文"这个判断漏一处，
    现象是**点下去什么也不发生**（实测就坏过：点「22 种语言」去开一个从不存在的
    `help/vml/languages.md`）。改成正文里写链接之后，**多少级都行，加页面只写 markdown**。
    """
    intro_field = "intro" if loc == ZH else "intro_en"
    rows = "\n".join(
        f"| [{l['title']}](help:vml/lang/{l['key']}) | "
        f"{bold_to_markup(l.get(intro_field) or '').splitlines()[0] if l.get(intro_field) else ''} |"
        for l in LANGS)

    if loc == ZH:
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

    return f"""# 22 languages

One VML interface set, usable from all 22 front ends. **Write in whichever one you know** - what
you compile runs on the same virtual machine.

## One page per language

Open any one of them and you get its **language reference** (syntax, types, standard library),
**what this front end supports**, and **the pitfalls we hit on real devices**. The last two are
taken straight from each compiler's `README.md` / `<language>_LANGUAGE_SPEC.md` in the VML source,
so regenerating this page brings them up to date.

| Language | In one line |
|---|---|
{rows}

## Run one first

```
vml run examples/c/tetris.c        # Tetris
vml run examples/python/tetris.py  # the same game, Python version
vml run examples/c/gomoku.c        # Gomoku
vml run examples/lua/life.lua      # Game of Life
vml run examples/basic/whack.bas   # Whack-a-mole
```

Every language folder also has a `sysinfo.<ext>` that calls `ui_call_json("sysinfo", "")` and
prints your device information - **if you want to know whether this language is usable, run that
first**.
"""


def render(loc: str):
    """渲染一份（loc = `zh` / `en`），返回 `[(path, text)]` 与缺失清单。"""
    missing = []
    targets = [(os.path.join(out_dir(loc), l["key"] + ".md"), build(l, loc, missing))
               for l in LANGS]
    targets.append((index_path(loc), index_page(loc)))
    return targets, missing


def main():
    # ⚠ **先把自己 stdout 的编码钉成 UTF-8**：本脚本的收尾行带 `✔`/`✘`，而中文 Windows 的
    #   控制台默认是 GBK ⇒ 那行 `print` 抛 `UnicodeEncodeError`。**产物已经写完了才崩**，
    #   于是"看起来失败了、其实成功" —— 实测踩到，白查一轮。`reconfigure` 失败也不影响主流程。
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
        sys.stderr.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass

    check = "--check" in sys.argv
    bad, written, all_missing = 0, 0, {}
    for loc in LOCALES:
        if not check:
            os.makedirs(out_dir(loc), exist_ok=True)
        targets, missing = render(loc)
        if missing:
            all_missing[loc] = missing
        for path, text in targets:
            if check:
                old = open(path, encoding="utf-8").read() if os.path.exists(path) else None
                if old != text:
                    print(f"✘ {os.path.relpath(path, ROOT)} 与生成结果不一致（重跑一次本脚本）")
                    bad += 1
                continue
            with open(path, "w", encoding="utf-8", newline="") as f:
                f.write(text)
            written += 1

    for loc, items in all_missing.items():
        print(f"⚠ {loc}: {len(items)} 处还没有英文版 —— 页面里写的是显式说明（不回退中文）：",
              file=sys.stderr)
        for it in items:
            print("   - " + it, file=sys.stderr)

    if check:
        print(("✘ 有 %d 个页面不一致" % bad) if bad else "✔ 全部一致")
    else:
        print("✔ 生成 %d 个说明页 → %s" % (written, os.path.join(HELP, " 与 ".join(LOCALES))))


if __name__ == "__main__":
    main()
