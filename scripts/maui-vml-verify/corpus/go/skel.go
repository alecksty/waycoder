// skel.go —— VML 骨架程序（Go）。
//
// 习惯用法抄自 Examples/go/snake.go：package main + func main，数组在**函数内**声明
// （模块级数组的基址有问题，示例只用一个包级数组；局部数组走 AllocateVmlArray 是对的），
// 颜色写负数十进制（示例里没有一处 0x 字面量）。
//
// 打印**不用** println("SKEL-SUM=", s)：Go 前端的 println 在实参之间插一个空格
// （GeneratePrintln，CodeGenerator.Expressions.cs:763），输出会是 "SKEL-SUM= 14"，
// 而字符串拼接在本前端编出来是空串（snake.go 文件头已记）。所以直接调库函数
// print_str（无换行）+ println_int（含换行），一次拼成一行。
package main

func inc(x int) int { return x + 1 }

func main() {
	var a [4]int
	a[0] = 1
	a[1] = 2
	a[2] = 3
	a[3] = 4
	s := 0
	for i := 0; i < 4; i++ {
		a[i] = inc(a[i])
		s = s + a[i]
	}
	ui_rect(10, 10, 50, 50, -65536, 1, 0, 0)
	ui_present()
	print_str("SKEL-SUM=")
	println_int(s)
}
