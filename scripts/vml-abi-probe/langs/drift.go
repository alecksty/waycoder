// 栈漂移探针（Go）：循环里反复调外部库函数（ipow）。判据：`DRIFT=126`（2^1+…+2^6）。
// 反序得 91（i^2 之和）、第 2 参丢得 6（ipow(2,0)=1）。
//
// 打印照 corpus/go/skel.go 的既有处置：不用 println("DRIFT=", s)（Go 前端在实参之间插空格），
// 改 print_str + println_int 拼成一行。
//
// ⚠ 审计记「Go 的方法调用把 receiver 压在最前、落在最高地址 ⇒ 被当成最后一个形参」。
//    这条探针是裸函数调用、不走方法那条分支，**照不到它** —— 要覆盖方法调用得另写一条。
package main

func main() {
	s := 0
	for i := 1; i <= 6; i++ {
		s = s + ipow(2, i)
	}
	print_str("DRIFT=")
	println_int(s)
}
