# demo_std.rb —— **第 1 层：标准输入输出**（Ruby 的 print / puts）
#
# 这一层就是语言自己的标准输出：往 stdout 写文本。
# 也是最基础的一层，也是**唯一有逐字节确定性判据**的一层 —— 下面的输出
# 跑两遍完全一样（不读输入、不用随机数、不看时间）。
#
# ## 判据
#
#     vmlcli Examples/ruby/demo_std.rb
#
# 期望 stdout 逐字节等于本文件末尾「期望输出」那段。
#
# ## ⚠ 本前端实测的四条限制（写 demo 时避开）
#
#   · **字符串插值 `#{...}` 不生效** —— 原样打出 `#{a}`（实测）。
#     要「文字 + 数字」只能用 `print("a=")` + `print(a)` 这样拆开写，
#     好在 `print` 打印数字是对的、且不补换行。
#   · `puts("a", "b")` 把多个实参**直接连起来**（`multiargs`），不插分隔符。
#   · **没有 `.to_s`**（`(a + b).to_s` 报 `expected )（得到 Dot）`）⇒ 数字没法转字符串。
#   · `\x1b` 在**字符串字面量里是支持的**（Ruby 走的是 `LexerBase.ReadEscape`），
#     所以彩色控制台那一层可以内联 ESC —— 见 demo_tty.rb。
#
# 本份用 `print` 单值输出 + `puts` 整行，避开上面全部四条。

puts("=== demo_std (Ruby) ===")
puts("纯字符串一行")
puts("转义：制表\t反斜杠\\引号\"")

a = 17
b = 25

print("a=")
print(a)
print(" b=")
print(b)
print("\n")

print("a+b=")
print(a + b)
print(" a-b=")
print(a - b)
print(" a*b=")
print(a * b)
print("\n")

print("a/b=")
print(a / b)
print(" a%b=")
print(a % b)
print("\n")

print("负数： ")
print(0 - a)
print(" ")
print(0 - (a * b))
print("\n")

# 循环算一个结果，证明这一层和语言本身是通的
i = 1
total = 0
while i <= 10
  total = total + i * i
  i = i + 1
end
print("1^2+...+10^2 = ")
print(total)
print("\n")

# 九九表的一小段（多行）
i = 1
while i <= 5
  print(i)
  print(" x 7 = ")
  print(i * 7)
  print("\n")
  i = i + 1
end

puts("=== 完成 ===")

# ── 期望输出（逐字节）────────────────────────────────────────────
# === demo_std (Ruby) ===
# 纯字符串一行
# 转义：制表	反斜杠\引号"
# a=17 b=25
# a+b=42 a-b=-8 a*b=425
# a/b=0 a%b=17
# 负数： -17 -425
# 1^2+...+10^2 = 385
# 1 x 7 = 7
# 2 x 7 = 14
# 3 x 7 = 21
# 4 x 7 = 28
# 5 x 7 = 35
# === 完成 ===
# ────────────────────────────────────────────────────────────────
