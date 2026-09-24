// test_error.go —— VML 诊断自测用例（**故意含错，不参与编译通过性检查**）
//
// 用途：验证编译器的错误/警告输出、诊断列表、以及编辑器能否显示气泡。
// 预期：1 个错误、**0 个警告**（详见下）。
// 约束：确定性 —— 无随机数、不读文件、不交互、无死循环；错误来自源码本身。
//
// ⚠ `unused` 是刻意留的「未使用变量」探针。go 前端不报未使用变量
//   （该检测只在 CCompiler 里实现），因此本文件只产 1 条错误。
//   go 前端确实有「MCU 模式: chan/select/go 被忽略」一族警告，但走的是
//   VMLPlugins.WarningEmitter，而它被 WarningLevel 门控、默认 0 ⇒ 本仓的
//   vmlcli / 手机端都不开启，实测 chan/select/go 全静默。
package main
func main() {
	println_str("go diag probe")
	unused := 5
	x := undefinedValue
	println_int(x)
}
