// nat.kt —— 测**这门语言自己的**标准输出函数（与 out.kt 的共享库那条路分开）
// ⚠ Kotlin 的 println 只接受**一个**实参 —— 原写法 println("OUT-INT=", 42) 是解析错误
//   （编译期报 `Expected ')' at 4:23, got Comma`）。拆成 print + println，各自单实参。
fun main() {
    println("OUT-STR=abc")
    print("OUT-INT=")
    println(42)
    println("OUT-PUN=hello, world")
}
