// `char **` 的双下标 `p[i][j]` / 双重解引用 `**p` —— **已修**（v0.96.346）。
//
// 钉住它是因为**症状极其隐蔽**：编译全绿、运行不报错、`%s` 打印还"看着对"
// （`%s` 走的是地址、不经过元素类型推断），只有拿 `%d`/`%c` 把**字符当成数**读出来
// 才会露馅。而它砸掉的是一整类写法：
//
//   · `int main(int argc, char **argv)` —— 老程序的入口
//   · `getopt(argc, argv, "ab:c")` —— POSIX 命令行解析（见 `22-getopt.c`）
//   · `nyan_show(char **frames)` —— 本探针套件诞生的那个案例
//   · 任何"字符串表"（`char *names[]` + `names[i][0]`）
//
// ## 两处独立缺陷，症状都是"读出来是 4 个字节拼成的数"
//
// ① **多级下标按级数解引用**（`CodeGenerator.Statements.cs` 的 `InferExpressionType`）
//    解析器把 `p[i][j]` 收成**一个** `ArrayAccess` + `Indices=[i,j]`
//    （不是嵌套节点 —— 只有 `(p[i])[j]` 那种带括号的才分两层）。
//    那条分支**看不到"外层下标"**，只按"元素是指针"返回 `arrType` ⇒
//    `pp[0][0]` 被判成 `CharPtr`，最后一跳按 **4 字节**读。
//    实测 `167789121` = `0x0A004241`，正好是 `"AB\0\n"` 四个字节。
//    **地址那侧是对的**（步长 4 再 1），只有取值宽度错。
//
// ② **`*p` 的返回类型**（同一函数里 `UnaryOp "*"` 那一支）
//    `ExprType` 这一档**表达不了"指针的指针"**（`char **` 也归成 `CharPtr`），
//    照 `case CharPtr: return Char` 走，`*p` 被判成 `char` ⇒ 按字节加载。
//    判据回到**声明原文的星号数**（`ElementIsPointer`，那个函数本就记着"三处共用"）。
//
// 判据：`(int)x == (int)&x` 之类的关系式在这里没用 —— 直接用**独立推导的字符值**：
// `arr[0]="AB"` ⇒ `[0]` 是 `'A'`(65)、`[1]` 是 `'B'`(66)；`arr[1]="CD"` ⇒ `'C'`(67)。
//
// ⚠ **`argfirst` 这个函数名是改过的**。原先叫 `peek`，而 `builtins` 库里**也有**一个
//   `peek` ⇒ `call peek` 被链接器改写成 `lib_builtins_peek`，**用户那个函数根本没人调**，
//   于是 F/G 一路红 —— 那是个**独立缺陷**（同名符号劫持），不是形参形态的问题。
//   改用不冲突的名字，本用例才测得到它本来要测的东西；
//   劫持本身另立一条：`cases/28-name-shadowed-by-lib.c`。
#include <stdio.h>

/* 形参形状（`getopt` 就是这个签名）—— 与局部变量分开测，两条路都要对 */
int argfirst(char **av, int i)
{
    return av[i][0];
}

int main()
{
    char  *arr[2];
    char **pp;
    char  *q;

    arr[0] = "AB";
    arr[1] = "CD";
    pp = arr;

    /* ① 对照：先把 `p[i]` 取到标量再下标 —— 一直是**对的**，证明用例本身不空 */
    q = pp[0];
    printf("\nA=%d", q[0]);

    /* ② 缺陷：不加括号的双下标 */
    printf("\nB=%d", pp[0][0]);
    printf("\nC=%d", pp[0][1]);

    /* ③ 缺陷：双重解引用 */
    printf("\nD=%d", **pp);

    /* ④ 对照：加一层括号就**对了** —— 这条是"问题在语法形态"的判据 */
    printf("\nE=%d", (pp[0])[0]);

    /* ⑤ 形参形状（getopt 走的就是这条） */
    printf("\nF=%d", argfirst(arr, 0));
    printf("\nG=%d", argfirst(arr, 1));

    return 0;
}
// EXPECT: A=65|B=65|C=66|D=65|E=65|F=65|G=67
