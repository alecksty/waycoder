// skel.kt —— VML 骨架程序（Kotlin）。
//
// 习惯用法抄自 Examples/kotlin/fibonacci/main.kt、geometry/main.kt（fun 定义；入口是 fun main，
// 见 KotlinCompiler/CodeGenerator.cs:107）。示例文件都是纯函数文件，没有循环/数组可抄。
//
// 外部库函数：KotlinCompiler 对**任何**不认识的函数名都发裸标签 CALL、实参从右到左压栈
// （CodeGenerator.cs:736-762）⇒ 与库包装的 C 约定一致，ui_rect / ui_present 可以直接裸调。
//
// ⚠ 打印不用 println()：println/print 在 Parser.cs:136-149 是**关键字形式**、只吃一个实参，
//   且 println 自带换行 ⇒ 拼不出 "SKEL-SUM=14" 一行。改用库函数 print_str（不换行）
//   + println_int（含换行）。
// ⚠ 不确定：`a[i] = v` 这种**下标赋值**在 Kotlin 的 Parser 里没有对应分支
//   （赋值只认裸名与成员访问），按源码看会解析失败 —— 若真编不过，缺的是前端这个语法，
//   不是本骨架写错。这正是要测的一条，所以照自然写法写、不做规避。
// 数组用 arrayOf(...)（docs/前端游戏能力评估.md §4.2C 用的就是它）；颜色写负数十进制。
fun plus1(x: Int): Int { return x + 1 }

fun main() {
    val a = arrayOf(1, 2, 3, 4)
    var s = 0
    for (i in 0..3) {
        a[i] = plus1(a[i])
        s = s + a[i]
    }
    ui_rect(10, 10, 50, 50, -65536, 1, 0, 0)
    ui_present()
    print_str("SKEL-SUM=")
    println_int(s)
}
