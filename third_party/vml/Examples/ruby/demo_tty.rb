# demo_tty.rb —— **第 2 层：彩色控制台**
#
# 这一层解决的问题是「**在一个字符界面上做版面**」：清屏、把光标挪到第几行第几列、
# 设前景/背景色。老 DOS 程序（菜单、表格、状态栏）全靠这几个原语拼出来。
#
# Ruby 这一层**没有 conio/crt 绑定**（`Lib/ruby/console.rb` 那几个 `console_*` 是
# 文档性存根），所以这里**直接输出 ANSI 转义序列** —— 那正是"支持彩色的控制台程序"：
#
#     清屏        ESC [ 2 J          ESC [ H
#     定位光标    ESC [ <行> ; <列> H
#     前景/背景   ESC [ 3x m / 4x m / 9x m / 10x m，256 色 38;5;N，真彩 38;2;r;g;b
#     复位        ESC [ 0 m
#
# ## ⚠ 本前端的三条限制（写 demo 时避开）
#
#   · **不能定义函数** —— 最朴素的 `def f … end` 会报
#     `Unexpected token: End(end)`（`Examples/ruby/catch.rb` 的文件头记着同一条）。
#     所以本份**完全平铺**：ESC 用 `print("\x1b[…")` 内联，一个 `def` 都没有。
#   · **词法器不认 `&&`**（`Unexpected char: &`）⇒ 多条件是嵌套 `if`，不是 `and`/`&&`。
#     本份只有一个等值判断，用 `if k == 0` 就够了。
#   · 字符串插值 `#{...}` **不生效**（原样打出 `#{a}`）⇒ 坐标写成字面量，别想拼字符串。
#
# ✅ 好消息：`\x1b` 在**字符串字面量里是支持的**（Ruby 走 `LexerBase.ReadEscape`，
#    完整支持 `\n \t \r \\ \' \" \xNN \NNN`）⇒ 本份可以直接内联 ESC，
#    不像 Python 那样非得 `putchar(27)`。
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
# 跑法：命令行页输入  vml run examples/ruby/demo_tty.rb

# ── 开场：清屏 + 回左上角 ───────────────────────────────────────
print("\x1b[2J")
print("\x1b[H")

# ── 标题条：蓝底白字（ESC[44;97m）──────────────────────────────
print("\x1b[1;1H")
print("\x1b[44;97m")
print("  demo_tty (Ruby) —— 彩色控制台 / ANSI 转义序列            ")
print("\x1b[0m")
print("\n")

print("\x1b[2;1H")
print("\x1b[90m")
print("清屏 ESC[2J   定位 ESC[r;cH   颜色 ESC[3xm / ESC[4xm   复位 ESC[0m")
print("\x1b[0m")
print("\n")

# ── 标准 8 色前景（30–37）──────────────────────────────────────
print("\x1b[4;1H")
print("\x1b[1;37m")
print("标准 8 色前景：")
print("\x1b[0m")
print("\n")

print("\x1b[5;1H")
print("\x1b[30m")
print(" 30 黑 ")
print("\x1b[0m")
print("\x1b[31m")
print(" 31 红 ")
print("\x1b[0m")
print("\x1b[32m")
print(" 32 绿 ")
print("\x1b[0m")
print("\x1b[33m")
print(" 33 黄 ")
print("\x1b[0m")
print("\x1b[34m")
print(" 34 蓝 ")
print("\x1b[0m")
print("\x1b[35m")
print(" 35 品红 ")
print("\x1b[0m")
print("\x1b[36m")
print(" 36 青 ")
print("\x1b[0m")
print("\x1b[37m")
print(" 37 白 ")
print("\x1b[0m")
print("\n")

# ── 亮色前景（90–97）────────────────────────────────────────────
print("\x1b[6;1H")
print("\x1b[90m")
print(" 90 亮黑(灰) ")
print("\x1b[0m")
print("\x1b[91m")
print(" 91 亮红 ")
print("\x1b[0m")
print("\x1b[92m")
print(" 92 亮绿 ")
print("\x1b[0m")
print("\x1b[93m")
print(" 93 亮黄 ")
print("\x1b[0m")
print("\x1b[94m")
print(" 94 亮蓝 ")
print("\x1b[0m")
print("\x1b[95m")
print(" 95 亮品红 ")
print("\x1b[0m")
print("\x1b[96m")
print(" 96 亮青 ")
print("\x1b[0m")
print("\x1b[97m")
print(" 97 亮白 ")
print("\x1b[0m")
print("\n")

# ── 背景色（40–47 / 100–107）───────────────────────────────────
print("\x1b[8;1H")
print("\x1b[1;37m")
print("背景色：")
print("\x1b[0m")
print("\n")

print("\x1b[9;1H")
print("\x1b[41m")
print(" 红底 ")
print("\x1b[0m")
print("\x1b[42m")
print(" 绿底 ")
print("\x1b[0m")
print("\x1b[44m")
print(" 蓝底 ")
print("\x1b[0m")
print("\x1b[46m")
print(" 青底 ")
print("\x1b[0m")
print("\x1b[103m")
print(" 亮黄底 ")
print("\x1b[0m")
print("\x1b[105m")
print(" 亮品红底 ")
print("\x1b[0m")
print("\n")

# ── 样式（1 粗 / 2 暗 / 3 斜 / 4 下划线 / 9 删除线）─────────────
print("\x1b[11;1H")
print("\x1b[1;37m")
print("样式：")
print("\x1b[0m")
print("\x1b[1m")
print(" 粗体 ")
print("\x1b[0m")
print("\x1b[2m")
print(" 暗淡 ")
print("\x1b[0m")
print("\x1b[3m")
print(" 斜体 ")
print("\x1b[0m")
print("\x1b[4m")
print(" 下划线 ")
print("\x1b[0m")
print("\x1b[9m")
print(" 删除线 ")
print("\x1b[0m")
print("\n")

# ── 256 色（38;5;N）与真彩（38;2;r;g;b）─────────────────────────
print("\x1b[13;1H")
print("\x1b[1;37m")
print("256 色 / 真彩：")
print("\x1b[0m")
print("\x1b[38;5;208m")
print(" 256-208 橙 ")
print("\x1b[0m")
print("\x1b[38;5;46m")
print(" 256-46 亮绿 ")
print("\x1b[0m")
print("\x1b[38;2;255;128;0m")
print(" 真彩橙 ")
print("\x1b[0m")
print("\x1b[48;2;60;0;90m")
print(" 真彩深紫底 ")
print("\x1b[0m")
print("\n")

# ── 循环画一条色带（12 格，颜色在 8 档里循环）──────────────────
# 「算出来的」那一格：取模 + 分支挑字面量色码。
# （数字转字符串在 Ruby 这边本来就没有 —— 没有 `.to_s`。）
print("\x1b[16;1H")
print("\x1b[1;37m")
print("循环画 12 格色带：")
print("\x1b[0m")
print("\n")

print("\x1b[17;1H")
i = 0
while i < 12
  k = i % 8
  if k == 0
    print("\x1b[41m")
  end
  if k == 1
    print("\x1b[42m")
  end
  if k == 2
    print("\x1b[43m")
  end
  if k == 3
    print("\x1b[44m")
  end
  if k == 4
    print("\x1b[45m")
  end
  if k == 5
    print("\x1b[46m")
  end
  if k == 6
    print("\x1b[47m")
  end
  if k == 7
    print("\x1b[100m")
  end
  print("   ")
  print("\x1b[0m")
  i = i + 1
end
print("\n")

# ── 收尾：复位颜色 + 定位到第 20 行写结束语 ─────────────────────
print("\x1b[0m")
print("\x1b[20;1H")
print("\x1b[1;32m")
print("demo_tty 结束 —— 没有按键等待，画完即退出。")
print("\x1b[0m")
print("\n")
