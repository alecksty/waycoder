// 栈漂移探针（Kotlin）：循环里反复调外部库函数（ipow）。判据：`DRIFT=126`（2^1+…+2^6）。
// 反序得 91（i^2 之和）、第 2 参丢得 6（ipow(2,0)=1）。
//
// 外部库函数：KotlinCompiler 对任何不认识的函数名都发裸标签 CALL、实参从右到左压栈
// （CodeGenerator.cs:736-762），与库包装的 C 约定一致 ⇒ ipow 可以直接裸调。
// 打印照 corpus/kotlin/skel.kt：println() 只吃一个实参且自带换行 ⇒ 改用 print_str + println_int。
//
// ⚠ 审计记「Kotlin 的被调方取参公式（CodeGenerator.cs:90-96）与调用点的压栈方向相反」。
//    若成立，第 2 个实参（循环变量 i）会读成别的东西 —— 这条正是照它的。
fun main() {
    var s = 0
    for (i in 1..6) {
        s = s + ipow(2, i)
    }
    print_str("DRIFT=")
    println_int(s)
}
