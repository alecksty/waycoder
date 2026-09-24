# demo_tty.py —— **第 2 层：彩色控制台**
#
# 这一层解决的问题是「**在一个字符界面上做版面**」：清屏、把光标挪到第几行第几列、
# 设前景/背景色。老 DOS 程序（菜单、表格、状态栏）全靠这几个原语拼出来。
#
# Python 这一层**没有 conio/crt 绑定**（`Lib/python/gfx.py` 里那些 `*_print` 是文档性
# 存根，`conio.vml` 的绑定也不是给 Python 调的），所以这里**直接输出 ANSI 转义序列**
# —— 那正是"支持彩色的控制台程序"：
#
#     清屏        ESC [ 2 J          ESC [ H
#     定位光标    ESC [ <行> ; <列> H
#     前景/背景   ESC [ 3x m / 4x m / 9x m / 10x m，256 色 38;5;N，真彩 38;2;r;g;b
#     复位        ESC [ 0 m
#
# ## ⚠ 本前端的坑：字符串字面量里**没有 `\xNN` 转义**
#
# `PythonCompiler/Lexer.cs` 的转义表只有 `\n \t \r \\ \' \"`，其余是
# 「丢掉反斜杠、留原字符」⇒ `"\x1b[31m"` 打出的是字面量 `x1b[31m`。
# 所以 `ansi()` 用 `putchar(27)` **直接写 ESC 那个字节**，再把后半截当普通字符串打。
# `print_str`/`putchar` 都**不补换行**，换行要自己 `print_str("\n")`。
#
# ## ⚠ 另一条踩过的坑：别在用户函数里连着调两次 `int_to_str`
#
# 想「算出坐标再拼进转义串」会踩到 `Lib/` 那两套栈清理约定（见 CLAUDE.md ⑨）：
# 实测
#
#     def at(row, col):
#         print_str(int_to_str(row)); print_str(";"); print_str(int_to_str(col))
#     at(1, 1)   # 期望 1;1H，实得 1;22818H（第二个 int_to_str 读到的实参漂了）
#
# 所以本 demo 的坐标**写成字面量**（`ansi("5;1H")`），一个 `int_to_str` 都不用。
#
# ## 在哪儿能看到什么
#
# · **真终端**（`vmlcli … | cat -v`）：整套转义都生效。
# · **手机"命令行"页**：那一层把 stdout 里的 ANSI 转成仓库统一的 `«»` 中间格式
#   （`UI/Shared/AnsiMarkup.cs`），**只认 SGR（颜色/样式）**；光标定位（`ESC[…H`）
#   与清屏（`ESC[2J`）会被**吃掉** —— 这是刻意的有限子集（见
#   `Examples/c/ansi_colors.c` 的说明）。所以每行末尾都补了换行，
#   定位被吃掉时输出仍然一行一句、读得通。
#
# 跑法：命令行页输入  vml run examples/python/demo_tty.py

# ── ANSI 小工具 ────────────────────────────────────────────────
# `ansi("31m")` = ESC[31m；定位也走同一个口子（`ansi("5;1H")` = ESC[5;1H）。
def ansi(code):
    putchar(27)
    print_str("[")
    print_str(code)

def text(s):
    print_str(s)

# ── 开场：清屏 + 回左上角 ───────────────────────────────────────
ansi("2J")
ansi("H")

# ── 标题条：蓝底白字（ESC[44;97m）──────────────────────────────
ansi("1;1H")
ansi("44;97m")
text("  demo_tty (Python) —— 彩色控制台 / ANSI 转义序列          ")
ansi("0m")
text("\n")

ansi("2;1H")
ansi("90m")
text("清屏 ESC[2J   定位 ESC[r;cH   颜色 ESC[3xm / ESC[4xm   复位 ESC[0m")
ansi("0m")
text("\n")

# ── 标准 8 色前景（30–37）──────────────────────────────────────
ansi("4;1H")
ansi("1;37m")
text("标准 8 色前景：")
ansi("0m")
text("\n")

ansi("5;1H")
ansi("30m")
text(" 30 黑 ")
ansi("0m")
ansi("31m")
text(" 31 红 ")
ansi("0m")
ansi("32m")
text(" 32 绿 ")
ansi("0m")
ansi("33m")
text(" 33 黄 ")
ansi("0m")
ansi("34m")
text(" 34 蓝 ")
ansi("0m")
ansi("35m")
text(" 35 品红 ")
ansi("0m")
ansi("36m")
text(" 36 青 ")
ansi("0m")
ansi("37m")
text(" 37 白 ")
ansi("0m")
text("\n")

# ── 亮色前景（90–97）────────────────────────────────────────────
ansi("6;1H")
ansi("90m")
text(" 90 亮黑(灰) ")
ansi("0m")
ansi("91m")
text(" 91 亮红 ")
ansi("0m")
ansi("92m")
text(" 92 亮绿 ")
ansi("0m")
ansi("93m")
text(" 93 亮黄 ")
ansi("0m")
ansi("94m")
text(" 94 亮蓝 ")
ansi("0m")
ansi("95m")
text(" 95 亮品红 ")
ansi("0m")
ansi("96m")
text(" 96 亮青 ")
ansi("0m")
ansi("97m")
text(" 97 亮白 ")
ansi("0m")
text("\n")

# ── 背景色（40–47 / 100–107）───────────────────────────────────
ansi("8;1H")
ansi("1;37m")
text("背景色：")
ansi("0m")
text("\n")

ansi("9;1H")
ansi("41m")
text(" 红底 ")
ansi("0m")
ansi("42m")
text(" 绿底 ")
ansi("0m")
ansi("44m")
text(" 蓝底 ")
ansi("0m")
ansi("46m")
text(" 青底 ")
ansi("0m")
ansi("103m")
text(" 亮黄底 ")
ansi("0m")
ansi("105m")
text(" 亮品红底 ")
ansi("0m")
text("\n")

# ── 样式（1 粗 / 2 暗 / 3 斜 / 4 下划线 / 9 删除线）─────────────
ansi("11;1H")
ansi("1;37m")
text("样式：")
ansi("0m")
ansi("1m")
text(" 粗体 ")
ansi("0m")
ansi("2m")
text(" 暗淡 ")
ansi("0m")
ansi("3m")
text(" 斜体 ")
ansi("0m")
ansi("4m")
text(" 下划线 ")
ansi("0m")
ansi("9m")
text(" 删除线 ")
ansi("0m")
text("\n")

# ── 256 色（38;5;N）与真彩（38;2;r;g;b）─────────────────────────
ansi("13;1H")
ansi("1;37m")
text("256 色 / 真彩：")
ansi("0m")
ansi("38;5;208m")
text(" 256-208 橙 ")
ansi("0m")
ansi("38;5;46m")
text(" 256-46 亮绿 ")
ansi("0m")
ansi("38;2;255;128;0m")
text(" 真彩橙 ")
ansi("0m")
ansi("48;2;60;0;90m")
text(" 真彩深紫底 ")
ansi("0m")
text("\n")

# ── 循环画一条色带（12 格，颜色在 8 档里循环）──────────────────
# 「算出来的」那一格：用取模 + 分支挑出字面量色码。这不是绕远 ——
# 数字转字符串这条路在多数前端上要么缺、要么像上面那样会漂，
# 所以「色码写字面量、循环只算用哪一个」是所有语言都能照做的写法。
ansi("16;1H")
ansi("1;37m")
text("循环画 12 格色带：")
ansi("0m")
text("\n")

ansi("17;1H")
i = 0
while i < 12:
    k = i % 8
    if k == 0:
        ansi("41m")
    if k == 1:
        ansi("42m")
    if k == 2:
        ansi("43m")
    if k == 3:
        ansi("44m")
    if k == 4:
        ansi("45m")
    if k == 5:
        ansi("46m")
    if k == 6:
        ansi("47m")
    if k == 7:
        ansi("100m")
    text("   ")
    ansi("0m")
    i = i + 1
text("\n")

# ── 收尾：复位颜色 + 定位到第 20 行写结束语 ─────────────────────
ansi("0m")
ansi("20;1H")
ansi("1;32m")
text("demo_tty 结束 —— 没有按键等待，画完即退出。")
ansi("0m")
text("\n")
