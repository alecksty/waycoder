// nat.kt —— 测**这门语言自己的**标准输出函数（与 out.kt 的共享库那条路分开）
// ⚠ Kotlin 的 println 只接受**一个**实参 —— 原写法 println("OUT-INT=", 42) 是解析错误
//   （编译期报 `Expected ')' at 4:23, got Comma`）。拆成 print + println，各自单实参。
//
// 三个值刻意放在**顶层属性**里：`val` 写在所有 `fun` 外面时，此前解析器的兜底分支
// 会把它一个 token 一个 token 地吃掉，函数里读到的**恒为 0**（台账那条「顶层 arrayOf 读回是 0」
// 的真身，其实标量也一样）。判据只写字面量就照不出来。
//
// `step` 那段同批钉住：它在 Kotlin 里是**软关键字**，此前被当硬关键字 ⇒ `var step = 5` 编不过；
// 而放宽之后又要保证 `for (.. step n)` 的步长**真的生效**（此前静默按 ±1 走）。
val hello = "OUT-STR=abc"
val tail = "OUT-PUN=hello, world"

fun main() {
    println(hello)
    print("OUT-INT=")
    // 0+2+4+6+8+10+12 = 42（**步长不生效时**是 0+1+…+12 = 78，一眼分得出来）
    var acc = 0
    for (i in 0..12 step 2) {
        acc = acc + i
    }
    println(acc)
    println(tail)
}
