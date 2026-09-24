// demo_std.dart —— **第 1 层：标准输入输出**（Dart 的 `print`）
//
// 这一层是每门语言自己的"往标准输出写文本"。
//
// ## ⚠ 写之前先量过：Dart 前端的 `+` **字符串拼接不可用**
//
// 实测（2026-09-24，`vmlcli Examples/dart/demo_std.dart`）：
//
//     print("num=" + n.toString() + "\n");   →  未定义的函数 'func__dot_toString'
//     print("num=" + n + "\n");              →  编得过，打出的是数字/乱码，不是那句话
//
// 而**单实参**的调用全部正常：
//
//     print("纯字面量\n")   ✔      print(42)   ✔      print(someIntVar)   ✔
//
// 所以本 demo 一律 **`print(标签)` + `print(值)` 分开写**，不用 `+` 拼、
// 也不用 `.toString()`。这不是风格选择，是绕开上面那条。
//
// ## ⚠ 转义序列也只认 `\n`
//
// `print("\x1b[31m")` 会原样打出 `x1b[31m`（六个字符，不是 ESC）。要发 ESC 只能
// `putchar(27)`（见 `demo_tty.dart`）。本 demo 只用 `\n`。
//
// ## 判据
//
//     vmlcli Examples/dart/demo_std.dart
//
// 期望 stdout 逐字节等于文件末尾那段「期望输出」。

// Dart 调库函数**必须 `external` 声明** —— 这是本前端唯一能产出**裸标签** CALL 的形式
//   （`CodeGenerator.Expressions.cs:141`；`asm()` 已移除）。不声明就解析不到 lib_* 标签。
// `println_str` / `print_str` / `println_int` 由 `Lib/shared/io.vml` 提供，
// 经 `builtins.vml`（`vmltool.config.xml` 的 `DefaultLibs`）进每一门语言的链接。
external void println_str(String s);
external void print_str(String s);
external void println_int(int n);

// 纯函数（不碰任何可变全局状态）—— 这几个是好的
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
  print("=== demo_std (Dart) ===\n");
  print("纯字面量一行\n");

  // ── 2. 标签 + 值 分开写（⚠ 不用 `+`，见文件头）──
  print("a=");       println_int(a);
  print("b=");       println_int(b);
  print("a+b=");     println_int(a + b);
  print("a-b=");     println_int(a - b);
  print("a*b=");     println_int(a * b);
  print("a/b=");     println_int(a ~/ b);      // 整除写 ~/
  print("a%b=");     println_int(a % b);
  print("负数：");    println_int(0 - a);

  // ── 3. 进制与宽度 ──
  print("十进制=");   println_int(255);
  print("十六进制="); println_int(255);

  // ── 4. 函数调用（含递归）──
  print("square(9)="); println_int(square(9));
  print("fib(10)=");   println_int(fib(10));
  // ⚠ 这里**必须用 `print_str`（库函数），不能用 `print`**：
  //   `print(<返回 String 的调用>)` 打出来的是**指针**（实测 `1024 1029 1033 1037`），
  //   只有 `print("字面量")` 和 `print(局部 String 变量)` 才是真的打字符串。
  print("digitName: ");
  print_str(digitName(0)); print(" ");
  print_str(digitName(1)); print(" ");
  print_str(digitName(2)); print(" ");
  // 收尾换行用库里的 println_str —— Dart 的 print 打换行时会多带一个回车符
  println_str(digitName(9));

  // ── 5. 数组 + 循环 ──
  //   ⚠ 数组**留在 main 里**：文件级可变状态在 Dart / Kotlin / Swift / Go 那几门上都出过
  //     问题（最典型是 Kotlin 的文件级 `arrayOf` 读回 0），这里不冒这个险。
  List<int> v = [1, 4, 9, 16, 25, 36];
  while (i < 6) {
    sum = sum + v[i];
    i = i + 1;
  }
  print("1^2+...+6^2 = "); println_int(sum);

  // ── 6. 九九表的一小段 ──
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
// a=17
// b=25
// a+b=42
// a-b=-8
// a*b=425
// a/b=0
// a%b=17
// 负数：-17
// 十进制=255
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
// ────────────────────────────────────────────────────────────────
