// out.go —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
package main

func main() {
	print_str("OUT-STR=abc")
	println_int(0)
	println_int(42)
	print_str("OUT-PUN=hello, world")
	println_int(0)
}
