# demo_std.r —— **第 1 层：标准输入输出**（R 的 cat / print）
# demo_std.r -- **layer 1: standard output** (R's cat / print)
#
# 这一层就是语言自己的标准输出：往 stdout 写文本。
# This layer is just the language's own standard output: writing text to stdout.
# 也是最基础的一层，也是**唯一有逐字节确定性判据**的一层 —— 下面的输出
# It is also the most basic layer, and the **only one with a byte-for-byte deterministic criterion** -- the output below
# 跑两遍完全一样（不读输入、不用随机数、不看时间）。
# is identical on two runs (it reads no input, uses no random numbers and looks at no clock).
#
# ## 判据
# ## The criterion
#
#     vmlcli Examples/r/demo_std.r
#
# 期望 stdout 逐字节等于本文件末尾「期望输出」那段。
# The expected stdout is byte-for-byte equal to the "expected output" block at the end of this file.
#
# ## ⚠ 本前端实测的两条限制（写 demo 时避开）
# ## ⚠ Two measured limitations of this frontend (avoid them when writing a demo)
#
#   · **`cat` 只打印第一个实参** —— `cat("a=", a, "\n")` 只打出 `a=`。
#   · **`cat` prints only the first argument** -- `cat("a=", a, "\n")` prints just `a=`.
#     （`Examples/r/catch.r` 的文件头记着同一条。）所以本 demo 一个字一个字地
#     (The header of `Examples/r/catch.r` notes the same thing.) So this demo prints piece
#     `cat`，每个 `cat` 恰好一个实参。
#     by piece with `cat`, each `cat` having exactly one argument.
#   · **没有 `paste`**（`未定义的函数 'func_paste'`）⇒ 数字没法拼进字符串里，
#   · **There is no `paste`** ("undefined function 'func_paste'") => numbers cannot be joined into a string,
#     只能用「先 `cat("a=")`、再 `cat(a)`」这种分次打印。
#     so printing has to be split as "first `cat("a=")`, then `cat(a)`".
#   · `cat` **不补换行**，换行要自己 `cat("\n")`。
#   · `cat` **appends no newline**; a line break needs its own `cat("\n")`.
#   · `\x1b` 在**字符串字面量里是支持的**，所以彩色控制台那一层可以内联 ESC ——
#   · `\x1b` **is supported inside string literals**, so the color console layer can inline ESC --
#     见 demo_tty.r。（本前端不认 `0x` 开头的整数，颜色要写负数十进制。）
#     see demo_tty.r. (This frontend does not accept integers starting with `0x`; colors must be written as negative decimals.)
#
# R 的向量是 **1 基**的，循环也别写 `for (i in 1:4)`（`:` 会被解析成 `CALL func_:`
# R vectors are **1-based**, and loops must not use `for (i in 1:4)` either (`:` is parsed into `CALL func_:`
# 而链接期报未定义）—— 本 demo 一律用 `while`。
# and link time reports it as undefined) -- this demo always uses `while`.

cat("=== demo_std (R) ===\n")
cat("纯字符串一行\n")
cat("转义：制表\t反斜杠\\引号\"\n")

a <- 17
b <- 25

cat("a=")
cat(a)
cat(" b=")
cat(b)
cat("\n")

cat("a+b=")
cat(a + b)
cat(" a-b=")
cat(a - b)
cat(" a*b=")
cat(a * b)
cat("\n")

cat("a/b=")
cat(a / b)
cat(" a%b=")
cat(a %% b)
cat("\n")

cat("负数： ")
cat(0 - a)
cat(" ")
cat(0 - (a * b))
cat("\n")

# 循环算一个结果，证明这一层和语言本身是通的
# Compute one result in a loop, proving this layer and the language itself are wired up
i <- 1
total <- 0
while (i <= 10) {
  total <- total + i * i
  i <- i + 1
}
cat("1^2+...+10^2 = ")
cat(total)
cat("\n")

# 九九表的一小段（多行）
# A short stretch of the multiplication table (several lines)
i <- 1
while (i <= 5) {
  cat(i)
  cat(" x 7 = ")
  cat(i * 7)
  cat("\n")
  i <- i + 1
}

cat("=== 完成 ===\n")

# ── 期望输出（逐字节）────────────────────────────────────────────
# -- Expected output (byte-for-byte; each line below is printed in Chinese, and the line under it is its translation) --
# === demo_std (R) ===
# 纯字符串一行
# A plain string line
# 转义：制表	反斜杠\引号"
# Escapes: tab\tbackslash\\quote"
# a=17 b=25
# a+b=42 a-b=-8 a*b=425
# a/b=0 a%b=17
# 负数： -17 -425
# Negative numbers: -17 -425
# 1^2+...+10^2 = 385
# 1 x 7 = 7
# 2 x 7 = 14
# 3 x 7 = 21
# 4 x 7 = 28
# 5 x 7 = 35
# === 完成 ===
# === Done ===
# ────────────────────────────────────────────────────────────────
