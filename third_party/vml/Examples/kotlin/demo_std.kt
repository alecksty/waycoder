// demo_std.kt —— **第 1 层：标准输入输出**（Kotlin 的 `print` / `println`）
// demo_std.kt — **Layer 1: standard input/output** (Kotlin's `print` / `println`)
//
// 这一层是每门语言自己的"往标准输出写文本"。
// This layer is each language's own way of "writing text to stdout".
//
// ## ⚠ 写之前先量过：Kotlin 前端的 `+` **字符串拼接不可用**
// ## ⚠ Measured before writing: the Kotlin frontend's `+` **cannot concatenate strings**
//
// 实测（2026-09-24，`vmlcli Examples/kotlin/demo_std.kt`）：
// Measured (2026-09-24, `vmlcli Examples/kotlin/demo_std.kt`):
//
//     print("num=" + n + " str=" + s + "\n")     // n=7, s="hello"
//     实际输出：1057701067102410730107542010850   ← 一串数字，不是那句话
//     actual output: 1057701067102410730107542010850   ← a run of digits, not that sentence
//
// 而**单实参**的调用全部正常：
// while **single-argument** calls are all fine:
//
//     print("纯字面量\n")   ✔      print(42)   ✔      println(someIntVar)   ✔
//     print("a plain literal\n")   ✔      print(42)   ✔      println(someIntVar)   ✔
//
// 所以本 demo 一律 **`print(标签)` + `print(值)` 分开写**，不用 `+` 拼。
// So this demo always writes **`print(label)` + `print(value)` separately**, never concatenating with `+`.
// 这不是风格选择，是绕开上面那条。
// This is not a style choice; it is how the above is worked around.
//
// ## ⚠ 转义序列也只认 `\n`
// ## ⚠ Only `\n` is recognized among escape sequences
//
// `print("\u001b[31m")` 会原样打出 `u001b[31m`；`\t` 同样不可靠。
// `print("\u001b[31m")` prints `u001b[31m` literally; `\t` is just as unreliable.
// 要发 ESC 只能 `putchar(27)`（见 `demo_tty.kt`）。本 demo 只用 `\n`。
// The only way to emit ESC is `putchar(27)` (see `demo_tty.kt`). This demo uses only `\n`.
//
// ## 判据
// ## Criteria
//
//     vmlcli Examples/kotlin/demo_std.kt
//
// 期望 stdout 逐字节等于文件末尾那段「期望输出」。
// Expects stdout to equal the "expected output" block at the end of the file, byte for byte.

// Kotlin 调库函数**不用声明**（对不认识的函数名发裸标签 CALL、实参右到左）——
// Kotlin library calls **need no declaration** (unknown function names become bare-label CALLs with arguments right-to-left) —
// 与 Java（必须 `static native`）/ Dart（必须 `external`）都不一样。
// Unlike Java (which requires `static native`) / Dart (which requires `external`).
// `println_str` / `println_int` 这些由 `Lib/shared/io.vml` 提供，
// `println_str` / `println_int` and the like are provided by `Lib/shared/io.vml`,
// 经 `builtins.vml`（`vmltool.config.xml` 的 `DefaultLibs`）进每一门语言的链接。
// They enter every language's link through `builtins.vml` (`DefaultLibs` in `vmltool.config.xml`).

fun main() {
    var a = 17
    var b = 25
    var i = 0
    var sum = 0

    // ── 1. 字符串 ──
    // ── 1. Strings ──
    print("=== demo_std (Kotlin) ===\n")
    print("纯字面量一行\n")

    // ── 2. 标签 + 值 分开写（⚠ 不用 `+`，见文件头）──
    // ── 2. Label + value written separately (⚠ don't use `+`, see the file header) ──
    print("a=");       println(a)
    print("b=");       println(b)
    print("a+b=");     println(a + b)
    print("a-b=");     println(a - b)
    print("a*b=");     println(a * b)
    print("a/b=");     println(a / b)
    print("a%b=");     println(a % b)
    print("负数：");    println(0 - a)

    // ── 3. 进制与宽度（这里只演示值；格式化宽度在 Kotlin 侧没有等价物）──
    // ── 3. Number bases and widths (only values are demonstrated here; Kotlin has no equivalent of formatted width) ──
    print("十进制=");   println(255)
    print("十六进制="); println(255)

    // ── 4. 局部函数 + 递归 ──
    // ── 4. Local functions + recursion ──
    print("square(9)="); println(square(9))
    print("fib(10)=");   println(fib(10))

    // ── 5. 数组 + 循环 ──
    // ── 5. Arrays + loops ──
    //   ⚠ 数组**必须留在 main 里**（`catch.kt` 记的实测：文件级 `arrayOf` 读回 0），
    //   ⚠ Arrays **must stay inside main** (the measurement recorded in `catch.kt`: a file-level `arrayOf` reads back 0),
    //     所以本 demo 的状态与逻辑全在 main 内，不拆到别的函数去。
    //     so this demo's state and logic are all inside main, not split into other functions.
    var v = arrayOf(1, 4, 9, 16, 25, 36)
    while (i < 6) {
        sum = sum + v[i]
        i = i + 1
    }
    print("1^2+...+6^2 = "); println(sum)

    // ── 6. 九九表的一小段 ──
    // ── 6. A small slice of the multiplication table ──
    i = 1
    while (i <= 5) {
        print(i)
        print(" x 7 = ")
        println(i * 7)
        i = i + 1
    }

    print("=== 完成 ===\n")
}

// 纯函数可以抽出去（不碰任何数组/全局状态）
// Pure functions can be pulled out (they touch no arrays/global state)
fun square(v: Int): Int {
    return v * v
}

fun fib(n: Int): Int {
    if (n < 2) {
        return n
    }
    return fib(n - 1) + fib(n - 2)
}

// ── 期望输出（逐字节）────────────────────────────────────────────
// === demo_std (Kotlin) ===
// 纯字面量一行
// A plain literal, one line
// a=17
// b=25
// a+b=42
// a-b=-8
// a*b=425
// a/b=0
// a%b=17
// 负数：-17
// Negative: -17
// 十进制=255
// Decimal=255
// 十六进制=255
// square(9)=81
// fib(10)=55
// 1^2+...+6^2 = 91
// 1 x 7 = 7
// 2 x 7 = 14
// 3 x 7 = 21
// 4 x 7 = 28
// 5 x 7 = 35
// === 完成 ===
// === done ===
// ────────────────────────────────────────────────────────────────
