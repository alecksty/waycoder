// out.go —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
// out.go -- VML cross-language "output" probe (exactly three lines, see run-langs.sh)
// 写法照 corpus/go/skel.* —— 共享库同时提供 println_str / println_int。
// Pattern follows corpus/go/skel.* -- the shared library provides both println_str / println_int.
package main
func main() {
	println_str("OUT-STR=abc")
	print_str("OUT-INT=")
	println_int(42)
	println_str("OUT-PUN=hello, world")
}
