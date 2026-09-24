// demo_std.go —— **标准输出**示范（Go）
//
// 四层示范的第一层：只用这门语言自己的标准输出，**不读输入、不画图、不弹窗**，
// 输出**逐字节确定**，能正常结束。
//
// ◆ 写法（Go 前端的两条实测约束）
//
//   · `println` 多个实参之间**自动补一个空格**（`println("a=", 7)` → `a= 7`），
//     所以拼"标签 + 值"时标签末尾不要多写空格。
//   · **字符串拼接在本前端编出来是空串**（`"a" + "b"` 渲染出来一个字都没有，
//     见 `snake.go` 文件头）⇒ 这一份全程不拼字符串，要拼的地方直接并列成多个实参。
//   · `\x1b` 转义**不解析**（原样打出 `x1b`）⇒ 控制字符走 `putchar(码)`，见 `demo_tty.go`。
//
// 跑法：命令行页输入  vml run examples/go/demo_std.go

package main

func main() {
	println("=== WayCoder demo_std (Go) ===")

	// ① 字符串
	println("[字符串] hello, world")

	// ② 整数
	n := 42
	println("[整数] n =", n)

	// ③ 计算结果
	println("[计算] 6 * 7 =", 6*7)
	a := 7
	b := 5
	println("[计算] a + b =", a+b)
	println("[计算] a * b - 3 =", a*b-3)

	// ④ 循环里算斐波那契前 10 项
	println("[循环] 斐波那契前 10 项：")
	x := 0
	y := 1
	i := 0
	for i < 10 {
		z := x + y
		x = y
		y = z
		println("  fib =", x)
		i = i + 1
	}

	// ⑤ 累加 1+2+…+100
	sum := 0
	k := 1
	for k <= 100 {
		sum = sum + k
		k = k + 1
	}
	println("[累加] 1+2+...+100 =", sum)

	// ⑥ 阶乘 5!
	fact := 1
	m := 1
	for m <= 5 {
		fact = fact * m
		m = m + 1
	}
	println("[阶乘] 5! =", fact)

	println("=== 结束 ===")
}
