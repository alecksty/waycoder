// test_error.dart —— VML 诊断自测用例（**故意含错，不参与编译通过性检查**）
//
// 用途：验证编译器的错误/警告输出、诊断列表、以及编辑器能否显示气泡。
// 预期：1 个错误、**0 个警告**（dart 前端没有任何警告通道，见下）。
// 约束：确定性 —— 无随机数、不读文件、不交互、无死循环；错误来自源码本身。
//
// ⚠ `unused` 是刻意留的「未使用变量」探针：C 前端会对它报
//   [CodeGen_UnusedVariable]，而 dart 前端既不产警告也不报错
//   （未使用变量检测只在 CCompiler 里实现）。留在这里当回归哨兵 ——
//   哪天 dart 前端补上这项检测，本文件立刻会多出一条 warning。
external void println_str(String s);
external void println_int(int v);
void main() {
  int unused = 5;
  println_str("dart diag probe");
  int x = undefined_value;
  println_int(x);
}
