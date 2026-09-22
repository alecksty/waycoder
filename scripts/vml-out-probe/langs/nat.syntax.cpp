// nat.syntax.cpp —— C++ 前端的**语言构造**专项探针。
//
// ## 为什么单独立一条
//
// 下面这几条**全是"C 支持、C++ 不支持"**（本仓 C++ 前端比 C 弱是老问题），
// 而 `nat.cpp` 只打字面量、`nat.typedef.cpp` 只压 typedef ⇒ 它们坏了很久，
// 跨语言探针一直全绿。**探针太窄，窄到下不了结论**——这是本文件存在的理由。
//
// ## 每一条的来历（都不是编出来的，是**老 BGI 游戏**撞出来的）
//
//   ① 逗号表达式 —— Turbo C 时代把几条语句挤一行的写法：
//        `sprintf(buf,"%d",t), settextstyle(3,0,4), outtextxy(305,200,buf);`
//      **左边必须真的求值**（副作用），值取右边；语句级与**括号内**是两条解析路径，
//      漏一条就只修好一半（实测先只修了语句级，`x = (a, b);` 照旧报错）。
//   ② `union U a, b;` —— 「已定义类型的**声明**」，不是定义。
//      老程序里是 `union REGS in, out;`（Turbo C 的 DOS 中断结构体）。
//   ③ `true` / `false` —— C++ 关键字，词法器不认识 ⇒ 被当变量名报"未声明"。
//   ④ 双关键字类型 `long int` / `short int` / `unsigned int`。
//      ⚠ 宽度必须仍是 **4**（本平台 `long` 就是 4 字节）—— 拼接成 "long int"
//        会让后端那条 `Contains("int")` 判成 8 字节，**悄悄变宽**。
//   ⑤ `sizeof(变量)` / `sizeof(数组)` —— 原先**编译器直接崩**（NullReferenceException，
//      报错里连行列都没有）。数组按 C 语义给**总字节数**。
#include <stdio.h>

union U { int a; int b; };
union U u1, u2;                 // ← ② 声明（不是定义）

int g[6];                       // ← ⑤ 全局数组
long int gl = 7;                // ← ④

int main()
{
    int i, n;
    long int a = 100000;        // ← ④
    unsigned int b = 7;
    short int c = 3;
    long double ld = 1.5;       // ← ④（表里没有 long double，按 double 算）
    double d = 2.5;
    int arr[4];
    bool t = true;
    bool f = false;             // ← ③

    /* ① 逗号表达式：**左边真的求值**（`n` 必须变成 3），值取右边（30） */
    n = 0;
    i = (n = 1, n = n + 2, n * 10);
    printf("SY-COMMA=%d,%d\n", i, n);

    /* ② union 声明：**只压"能解析 + 单变量读写自洽"**。
          ⚠ **已知缺口（本探针不压、但必须记着）**：C++ 代码生成里**根本没有 union 类型**
          —— `UnionDecl` 解析出来了却从没被用过，成员访问退化成全局标签
          ⇒ `u1.a = 5; u2.a = 7;` 之后读 `u1.a` 得到的是 **7**（两个变量共用一处存储）。
          C 前端是好的（同样两行得 12）。修它等于在 C++ 端新做一个 union 类型，
          不是顺手的事 —— 留在这里当**记录**，别让下一个人以为它是好的。 */
    u1.a = 42;
    printf("SY-UNION=%d\n", u1.a);

    /* ③ true / false */
    printf("SY-BOOL=%d,%d\n", t ? 1 : 0, f ? 1 : 0);

    /* ④ 双关键字类型：值对 + **宽度对**（long 与 long int 都是 4） */
    printf("SY-TYPE=%d,%d,%d,%d,%d\n", a, b, c, (int)sizeof(long int), (int)sizeof(gl));

    /* ⑤ sizeof：变量给元素宽度、数组给**总字节数** */
    printf("SY-SIZEOF=%d,%d,%d,%d,%d\n",
           (int)sizeof(a), (int)sizeof(d), (int)sizeof(ld), (int)sizeof(arr), (int)sizeof(g));
    return 0;
}
