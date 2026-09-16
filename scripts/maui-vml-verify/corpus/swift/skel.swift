// skel.swift —— VML 骨架程序（Swift）。
//
// 习惯用法抄自 Examples/swift/snake.swift、plane.swift（裸调 ui_*，无 import/声明）。
// 三条实测约束（snake.swift 文件头）：① 模块级标量读出来是垃圾 ⇒ 状态放数组、常量写进 main 的
// 局部变量；② 只开**一个**全局数组（多个全局数组互相踩），这里干脆全放 main 内的局部；
// ③ 颜色写负数十进制（-65536 = 0xFFFF0000）。
// print 逐参输出、**不插分隔符**、末尾只补一次换行（CodeGenerator.cs:759-781）⇒ 两个参数会拼成一行。
// 循环用 for-in（GenerateForEach，支持 0...3 闭区间）；示例里只出现过 while，这条是新写法。
func inc(x: Int) -> Int { return x + 1 }

func main() {
    var a = [1, 2, 3, 4]
    var s = 0
    for i in 0...3 {
        a[i] = inc(a[i])
        s = s + a[i]
    }
    ui_rect(10, 10, 50, 50, -65536, 1, 0, 0)
    ui_present()
    print("SKEL-SUM=", s)
}
