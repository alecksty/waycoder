// demo_std.swift —— **标准输出**示范（Swift）
//
// 四层示范的第一层：只用这门语言自己的标准输出，**不读输入、不画图、不弹窗**，
// 输出**逐字节确定**，能正常结束。
//
// ◆ 写法（Swift 前端的实测约束）
//
//   · `print` 多个实参之间**不加分隔符**（`print("n =", 7)` → `n =7`），
//     所以间距要自己写进字符串里（`print("n = ", 7)` → `n = 7`）。
//   · 没有 `str(x)` 这种整数转字符串的内置函数（写了报「未定义的函数 'str'」）⇒
//     要拼串得自己写 `numToStr`（见 `snake.swift`）；这一份把标签和值分开传，
//     不需要转换。
//   · **状态放数组、常量放函数返回值** —— 这几门前端的模块级标量读出来是垃圾
//     （见 `snake.swift` 文件头）。这一份只用 `main` 里的局部量。
//   · `\u{1B}` 转义不支持；控制字符走 `putchar(码)`，见 `demo_tty.swift`。
//
// 跑法：命令行页输入  vml run examples/swift/demo_std.swift

print("=== WayCoder demo_std (Swift) ===")

// ① 字符串
print("[字符串] hello, world")

// ② 整数
let n = 42
print("[整数] n = ", n)

// ③ 计算结果
print("[计算] 6 * 7 = ", 6 * 7)
let a = 7
let b = 5
print("[计算] a + b = ", a + b)
print("[计算] a * b - 3 = ", a * b - 3)

// ④ 循环里算斐波那契前 10 项
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
var sum = 0
var k = 1
while k <= 100 {
    sum = sum + k
    k = k + 1
}
print("[累加] 1+2+...+100 = ", sum)

// ⑥ 阶乘 5!
var fact = 1
var m = 1
while m <= 5 {
    fact = fact * m
    m = m + 1
}
print("[阶乘] 5! = ", fact)

print("=== 结束 ===")
