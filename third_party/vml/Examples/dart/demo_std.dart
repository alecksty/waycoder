// demo_std.dart —— **第 1 层：标准输入输出**（Dart 的 `print`）
// demo_std.dart — **Layer 1: standard input/output** (Dart's `print`)
//
// 这一层是每门语言自己的"往标准输出写文本"。
// This layer is each language's own way of "writing text to stdout".
//
// ## ⚠ 写之前先量过：Dart 前端的 `+` **字符串拼接不可用**
// ## ⚠ Measured before writing: the Dart frontend's `+` **cannot concatenate strings**
//
// 实测（2026-09-24，`vmlcli Examples/dart/demo_std.dart`）：
// Measured (2026-09-24, `vmlcli Examples/dart/demo_std.dart`):
//
//     print("num=" + n.toString() + "\n");   →  未定义的函数 'func__dot_toString'
//     print("num=" + n.toString() + "\n");   →  undefined function 'func__dot_toString'
//     print("num=" + n + "\n");              →  编得过，打出的是数字/乱码，不是那句话
//     print("num=" + n + "\n");              →  compiles, but prints a number/garbage, not that sentence
//
// 而**单实参**的调用全部正常：
// while **single-argument** calls are all fine:
//
//     print("纯字面量\n")   ✔      print(42)   ✔      print(someIntVar)   ✔
//     print("a plain literal\n")   ✔      print(42)   ✔      print(someIntVar)   ✔
//
// 所以本 demo 一律 **`print(标签)` + `print(值)` 分开写**，不用 `+` 拼、
// So this demo always writes **`print(label)` + `print(value)` separately**, never concatenating with `+`,
// 也不用 `.toString()`。这不是风格选择，是绕开上面那条。
// and never using `.toString()`. That is not a style choice, it is how the above is worked around.
//
// ## ⚠ 转义序列也只认 `\n`
// ## ⚠ Only `\n` is recognized among escape sequences
//
// `print("\x1b[31m")` 会原样打出 `x1b[31m`（六个字符，不是 ESC）。要发 ESC 只能
// `print("\x1b[31m")` prints `x1b[31m` literally (six characters, not ESC). To emit ESC you can only
// `putchar(27)`（见 `demo_tty.dart`）。本 demo 只用 `\n`。
// use `putchar(27)` (see `demo_tty.dart`). This demo uses only `\n`.
//
// ## 判据
// ## Criteria
//
//     vmlcli Examples/dart/demo_std.dart
//
// 期望 stdout 逐字节等于文件末尾那段「期望输出」。
// Expects stdout to equal the "expected output" block at the end of the file, byte for byte.

// Dart 调库函数**必须 `external` 声明** —— 这是本前端唯一能产出**裸标签** CALL 的形式
// Dart library calls **must be declared `external`** — this is the only form in this frontend that produces a **bare-label** CALL
//   （`CodeGenerator.Expressions.cs:141`；`asm()` 已移除）。不声明就解析不到 lib_* 标签。
//   (`CodeGenerator.Expressions.cs:141`; `asm()` has been removed). Without a declaration the lib_* labels cannot be resolved.
// `println_str` / `print_str` / `println_int` 由 `Lib/shared/io.vml` 提供，
// `println_str` / `print_str` / `println_int` are provided by `Lib/shared/io.vml`,
// 经 `builtins.vml`（`vmltool.config.xml` 的 `DefaultLibs`）进每一门语言的链接。
// They enter every language's link through `builtins.vml` (`DefaultLibs` in `vmltool.config.xml`).
external void println_str(String s);
external void print_str(String s);
external void println_int(int n);

// 纯函数（不碰任何可变全局状态）—— 这几个是好的
// pure functions (touching no mutable global state) — these few are fine
int square(int v) {
  return v * v;
}

int fib(int n) {
  if (n < 2) {
    return n;
  }
  return fib(n - 1) + fib(n - 2);
}

String digitName(int d) {
  if (d == 0) { return "zero"; }
  if (d == 1) { return "one"; }
  if (d == 2) { return "two"; }
  return "many";
}

void main() {
  int a = 17;
  int b = 25;
  int i = 0;
  int sum = 0;

  // ── 1. 字符串 ──
  // ── 1. Strings ──
  print("=== demo_std (Dart) ===\n");
  print("纯字面量一行\n");

  // ── 2. 标签 + 值 分开写（⚠ 不用 `+`，见文件头）──
  // ── 2. Label + value written separately (⚠ don't use `+`, see the file header) ──
  print("a=");       println_int(a);
  print("b=");       println_int(b);
  print("a+b=");     println_int(a + b);
  print("a-b=");     println_int(a - b);
  print("a*b=");     println_int(a * b);
  print("a/b=");     println_int(a ~/ b);      // 整除写 ~/
  // integer division is written ~/
  print("a%b=");     println_int(a % b);
  print("负数：");    println_int(0 - a);

  // ── 3. 进制与宽度 ──
  // ── 3. Number bases and widths ──
  print("十进制=");   println_int(255);
  print("十六进制="); println_int(255);

  // ── 4. 函数调用（含递归）──
  // ── 4. Function calls (including recursion) ──
  print("square(9)="); println_int(square(9));
  print("fib(10)=");   println_int(fib(10));
  // ⚠ 这里**必须用 `print_str`（库函数），不能用 `print`**：
  // ⚠ Here you **must use `print_str` (the library function), not `print`**:
  //   `print(<返回 String 的调用>)` 打出来的是**指针**（实测 `1024 1029 1033 1037`），
  //   `print(<a call returning String>)` prints a **pointer** (measured `1024 1029 1033 1037`),
  //   只有 `print("字面量")` 和 `print(局部 String 变量)` 才是真的打字符串。
  //   only `print("literal")` and `print(a local String variable)` really print the string.
  print("digitName: ");
  print_str(digitName(0)); print(" ");
  print_str(digitName(1)); print(" ");
  print_str(digitName(2)); print(" ");
  // 收尾换行用库里的 println_str —— Dart 的 print 打换行时会多带一个回车符
  // Use the library's println_str for the trailing newline — Dart's print carries an extra carriage return when printing a newline
  println_str(digitName(9));

  // ── 5. 数组 + 循环 ──
  // ── 5. Arrays + loops ──
  //   ⚠ 数组**留在 main 里**：文件级可变状态在 Dart / Kotlin / Swift / Go 那几门上都出过
  //   ⚠ Arrays **stay inside main**: file-level mutable state has gone wrong on Dart / Kotlin / Swift / Go
  //     问题（最典型是 Kotlin 的文件级 `arrayOf` 读回 0），这里不冒这个险。
  //     (most typically Kotlin's file-level `arrayOf` reading back 0), and this doesn't take that risk.
  List<int> v = [1, 4, 9, 16, 25, 36];
  while (i < 6) {
    sum = sum + v[i];
    i = i + 1;
  }
  print("1^2+...+6^2 = "); println_int(sum);

  // ── 6. 九九表的一小段 ──
  // ── 6. A small slice of the multiplication table ──
  i = 1;
  while (i <= 5) {
    print(i);
    print(" x 7 = ");
    println_int(i * 7);
    i = i + 1;
  }

  print("=== 完成 ===\n");
}

// ── 期望输出（逐字节）────────────────────────────────────────────
// === demo_std (Dart) ===
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
// digitName: zero one two many
// 1^2+...+6^2 = 91
// 1 x 7 = 7
// 2 x 7 = 14
// 3 x 7 = 21
// 4 x 7 = 28
// 5 x 7 = 35
// === 完成 ===
// === done ===
// ────────────────────────────────────────────────────────────────
