// test_error.go —— VML 诊断自测用例（**故意含错，不参与编译通过性检查**）
// test_error.go -- VML diagnostic self-test case (**deliberately contains errors, not part of the compile-cleanliness check**)
//
// 用途：验证编译器的错误/警告输出、诊断列表、以及编辑器能否显示气泡。
// Purpose: verify the compiler's error/warning output, the diagnostic list, and whether the editor can show a squiggle.
// 预期：1 个错误、**0 个警告**（详见下）。
// Expected: 1 error, **0 warnings** (see below).
// 约束：确定性 —— 无随机数、不读文件、不交互、无死循环；错误来自源码本身。
// Constraints: deterministic -- no randomness, no file reads, no interaction, no infinite loops; the error comes from the source itself.
//
// ⚠ `unused` 是刻意留的「未使用变量」探针。go 前端不报未使用变量
// ⚠ `unused` is a deliberately kept "unused variable" probe. The go frontend does not report unused variables
//   （该检测只在 CCompiler 里实现），因此本文件只产 1 条错误。
//   (that detection is implemented only in CCompiler), so this file produces only 1 error.
//   go 前端确实有「MCU 模式: chan/select/go 被忽略」一族警告，但走的是
//   The go frontend does have the "MCU mode: chan/select/go ignored" family of warnings, but they go through
//   VMLPlugins.WarningEmitter，而它被 WarningLevel 门控、默认 0 ⇒ 本仓的
//   VMLPlugins.WarningEmitter, which is gated by WarningLevel and defaults to 0 => this repo's
//   vmlcli / 手机端都不开启，实测 chan/select/go 全静默。
//   vmlcli / mobile client never enables it, and measurement shows chan/select/go are entirely silent.
package main
func main() {
	println_str("go diag probe")
	unused := 5
	x := undefinedValue
	println_int(x)
}
