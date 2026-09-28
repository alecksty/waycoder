// demo_std.swift —— **标准输出**示范（Swift）
// demo_std.swift — a **standard output** demo (Swift)
//
// 四层示范的第一层：只用这门语言自己的标准输出，**不读输入、不画图、不弹窗**，
// Layer 1 of the four-layer demo: use only this language's own standard output, **no input reading, no drawing, no dialogs**,
// 输出**逐字节确定**，能正常结束。
// with **byte-for-byte deterministic** output, terminating normally.
//
// ◆ 写法（Swift 前端的实测约束）
// ◆ Style (measured constraints of the Swift frontend)
//
//   · `print` 多个实参之间**不加分隔符**（`print("n =", 7)` → `n =7`），
//   · `print` **adds no separator** between multiple arguments (`print("n =", 7)` → `n =7`),
//     所以间距要自己写进字符串里（`print("n = ", 7)` → `n = 7`）。
//     so the spacing must be written into the string yourself (`print("n = ", 7)` → `n = 7`).
//   · 没有 `str(x)` 这种整数转字符串的内置函数（写了报「未定义的函数 'str'」）⇒
//   · There is no `str(x)` builtin for int-to-string (writing one reports "undefined function 'str'") ⇒
//     要拼串得自己写 `numToStr`（见 `snake.swift`）；这一份把标签和值分开传，
//     to build strings you must write your own `numToStr` (see `snake.swift`); this copy passes the label and the value separately,
//     不需要转换。
//     so no conversion is needed.
//   · **状态放数组、常量放函数返回值** —— 这几门前端的模块级标量读出来是垃圾
//   · **State goes in arrays, constants in function return values** — module-level scalars read back as garbage in these frontends
//     （见 `snake.swift` 文件头）。这一份只用 `main` 里的局部量。
//     (see the header of `snake.swift`). This copy uses only locals inside `main`.
//   · `\u{1B}` 转义不支持；控制字符走 `putchar(码)`，见 `demo_tty.swift`。
//   · The `\u{1B}` escape is unsupported; control characters go through `putchar(code)`, see `demo_tty.swift`.
//
// 跑法：命令行页输入  vml run examples/swift/demo_std.swift
// How to run: on the command-line page type  vml run examples/swift/demo_std.swift

print("=== WayCoder demo_std (Swift) ===")

// ① 字符串
// ① Strings
print("[字符串] hello, world")

// ② 整数
// ② Integers
let n = 42
print("[整数] n = ", n)

// ③ 计算结果
// ③ Computed results
print("[计算] 6 * 7 = ", 6 * 7)
let a = 7
let b = 5
print("[计算] a + b = ", a + b)
print("[计算] a * b - 3 = ", a * b - 3)

// ④ 循环里算斐波那契前 10 项
// ④ Compute the first 10 Fibonacci numbers in a loop
print("[循环] 斐波那契前 10 项：")
var x = 0
var y = 1
var i = 0
while i < 10 {
    var z = x + y
    x = y
    y = z
    print("  fib = ", x)
    i = i + 1
}

// ⑤ 累加 1+2+…+100
// ⑤ Sum 1+2+…+100
var sum = 0
var k = 1
while k <= 100 {
    sum = sum + k
    k = k + 1
}
print("[累加] 1+2+...+100 = ", sum)

// ⑥ 阶乘 5!
// ⑥ Factorial 5!
var fact = 1
var m = 1
while m <= 5 {
    fact = fact * m
    m = m + 1
}
print("[阶乘] 5! = ", fact)

print("=== 结束 ===")
