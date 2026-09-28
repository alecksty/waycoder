// test_error.dart —— VML 诊断自测用例（**故意含错，不参与编译通过性检查**）
// test_error.dart -- VML diagnostic self-test case (**deliberately contains errors, not part of the compile-cleanliness check**)
//
// 用途：验证编译器的错误/警告输出、诊断列表、以及编辑器能否显示气泡。
// Purpose: verify the compiler's error/warning output, the diagnostic list, and whether the editor can show a squiggle.
// 预期：1 个错误、**0 个警告**（dart 前端没有任何警告通道，见下）。
// Expected: 1 error, **0 warnings** (the dart frontend has no warning channel at all, see below).
// 约束：确定性 —— 无随机数、不读文件、不交互、无死循环；错误来自源码本身。
// Constraints: deterministic -- no randomness, no file reads, no interaction, no infinite loops; the error comes from the source itself.
//
// ⚠ `unused` 是刻意留的「未使用变量」探针：C 前端会对它报
// ⚠ `unused` is a deliberately kept "unused variable" probe: the C frontend reports
//   [CodeGen_UnusedVariable]，而 dart 前端既不产警告也不报错
//   [CodeGen_UnusedVariable] for it, while the dart frontend neither warns nor errors
//   （未使用变量检测只在 CCompiler 里实现）。留在这里当回归哨兵 ——
//   (unused-variable detection is implemented only in CCompiler). It stays here as a regression sentinel --
//   哪天 dart 前端补上这项检测，本文件立刻会多出一条 warning。
//   the day the dart frontend adds that detection, this file will immediately gain one more warning.
external void println_str(String s);
external void println_int(int v);
void main() {
  int unused = 5;
  println_str("dart diag probe");
  int x = undefined_value;
  println_int(x);
}
