-- demo_std.lua —— **第 1 层：标准输入输出**（Lua 的 print）
-- demo_std.lua — **Layer 1: standard input/output** (Lua's print)
--
-- 这一层就是语言自己的标准输出：往 stdout 写文本。
-- This layer is the language's own standard output: writing text to stdout.
-- 也是最基础的一层，也是**唯一有逐字节确定性判据**的一层 —— 下面的输出
-- It is also the most basic layer, and the **only one with a byte-exact deterministic criterion** — the output below
-- 跑两遍完全一样（不读输入、不用随机数、不看时间）。
-- is exactly the same when run twice (no input read, no random numbers, no clock).
--
-- ## 判据
-- ## Criterion
--
--     vmlcli Examples/lua/demo_std.lua
--
-- 期望 stdout 逐字节等于本文件末尾「期望输出」那段。
-- stdout is expected to be byte-for-byte identical to the "expected output" block at the end of this file.
--
-- ## ⚠ 本前端实测的三条限制（写 demo 时避开）
-- ## ⚠ Three limits of this frontend measured in practice (avoid them when writing demos)
--
--   · **`..` 字符串拼接编不过** —— 链到 `lua_concat`，而库里没有这个标签
--   · **`..` string concatenation does not compile** — it links to `lua_concat`, and the library has no such label
--     （「未定义的函数 'lua_concat'」）。所以本 demo 一个 `..` 都不用，
--     ("undefined function 'lua_concat'"). So this demo does not use a single `..`;
--     全部靠 `print` 的**多实参直接相连**（`print("a=", a)` → `a=17`）。
--     everything relies on `print` **concatenating multiple arguments directly** (`print("a=", a)` → `a=17`).
--   · `print` **不插分隔符**：`print("line", i)` 打出 `line0`，不是 `line 0`。
--   · `print` **inserts no separator**: `print("line", i)` prints `line0`, not `line 0`.
--     （与 Python/Ruby 那些「多实参用空格连」的语言不同，写期望值时要注意。）
--     (Unlike Python/Ruby, where multiple arguments are joined by a space — be careful when writing expected output.)
--   · `putchar(n)` 打出来的不是那个字节 —— 实测**单条连着调**时是对的
--   · `putchar(n)` does not print that byte — measured: **a straight run of calls** is correct
--     （`putchar(65) putchar(66) putchar(27)` → `A B ESC`，逐字节核过），
--     (`putchar(65) putchar(66) putchar(27)` → `A B ESC`, verified byte by byte),
--     但**中间夹一次别的库调用**它就读错实参了（`putchar(65)` `print_str("\n")` `putchar(27)`
--     but **wedging another library call in between** makes it read the wrong argument (`putchar(65)` `print_str("\n")` `putchar(27)`
--     → 第三个字节是 `A7 AD` 而不是 `1B`）。这跟 `Lib/` 那两套栈清理约定同源
--     → the third byte is `A7 AD` instead of `1B`). This has the same root cause as the two stack-cleanup conventions in `Lib/`
--     （见 CLAUDE.md ⑨ 与本仓 `int_to_str` 的同类现象）⇒ 彩色控制台那一层走
--     (see CLAUDE.md ⑨ and the same class of symptom in this repo's `int_to_str`) ⇒ the color-console layer goes with
--     **字符串字面量里的 `\x1b`**（Lua 支持），见 demo_tty.lua。
--     **`\x1b` inside string literals** (Lua supports it), see demo_tty.lua.
--
-- 另一条写法要求（沿用 `Examples/lua/life.lua`）：**循环变量先 `local` 声明**
-- One more writing requirement (following `Examples/lua/life.lua`): **declare the loop variable with `local` first**
-- 再进循环，否则循环体一次都不执行（v0.96.202 修的就是这条）。
-- before entering the loop, otherwise the loop body never runs even once (this is exactly what v0.96.202 fixed).

print("=== demo_std (Lua) ===")
print("纯字符串一行")
print("转义：制表\t反斜杠\\引号\"")

local a = 17
local b = 25

print("a=", a, " b=", b)
print("a+b=", a + b, " a-b=", a - b, " a*b=", a * b)
print("a/b=", a / b, " a%b=", a % b)
print("负数： ", 0 - a, " ", 0 - (a * b))

-- 循环算一个结果，证明这一层和语言本身是通的
-- A loop computes a result, proving this layer and the language itself are wired up
local i = 0
local total = 0
i = 1
while i <= 10 do
  total = total + i * i
  i = i + 1
end
print("1^2+...+10^2 = ", total)

-- 九九表的一小段（多行）
-- A short slice of the multiplication table (multiple lines)
i = 1
while i <= 5 do
  print(i, " x 7 = ", i * 7)
  i = i + 1
end

print("=== 完成 ===")

-- ── 期望输出（逐字节）────────────────────────────────────────────
-- === demo_std (Lua) ===
-- 纯字符串一行
-- A pure string on one line
-- 转义：制表	反斜杠\引号"
-- escapes: tab	backslash\quote"
-- a=17 b=25
-- a+b=42 a-b=-8 a*b=425
-- a/b=0 a%b=17
-- 负数： -17 -425
-- negatives: -17 -425
-- 1^2+...+10^2 = 385
-- 1 x 7 = 7
-- 2 x 7 = 14
-- 3 x 7 = 21
-- 4 x 7 = 28
-- 5 x 7 = 35
-- === 完成 ===
-- === done ===
-- ────────────────────────────────────────────────────────────────
