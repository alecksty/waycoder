// **匿名嵌套结构体当"成员"时，中间那个成员名从来没被登记**
//
// ## 症状
//
// `tty-clock` 的画面**全空**：`ttyclock.option.*` 的读写全落在结构体开头那几个
// 字段上（`s.option.color` 算成偏移 0），于是 `nsdelay = 0` 那一句把 `running`
// 冲成 0 ⇒ 主循环一次都不进 ⇒ 一帧都不画。
//
// ## 根因：内层成员被"拍平"，中间那个成员名被丢掉
//
// `struct { … } opt;` 这一段，解析器把内层成员**拍平**进外层成员表
// （偏移按外层的绝对值算），而中间的 `opt` 成员**从没登记** ——
// 于是 `s.opt.a` 找不到 `opt`、基址从 0 起算。
// （C 里 `struct { … } opt;` 的成员**本就不提升**，只有 C11 的
//  `struct { … };`（没有成员名）才提升 —— 这两条路必须分开。）
//
// 顺带：`union { … } u;` 是同一个分支的另一种形态（外层只加一条成员、
// 整块登记成一张 union 类型），`struct { … };` 那条**提升**路径也得跟着一起保住。
//
// ⚠ 判据刻意只看**稳定量**（成员间的字节偏移、往返读写的值）——
//   绝对地址随布局漂，不能进判据。
// STDIN:
// EXPECT: TAG=1|A=11|B=22|X=33|Y=44|TL=55|O1=4|O2=12|O3=20|IB=4|SZ=24|P=1|Q=7|R=8|S2=9|OQ=4|OR=8|OS=12|SZ2=16|PRE=1|U1=7|U2=7|POST=9|OU=4|SZU=12
#include <stdio.h>

/* ① 具名成员 + 两个匿名嵌套结构体（tty-clock 的形状） */
typedef struct {
    int tag;
    struct { int a; int b; } opt;      /* 匿名 struct 当**具名成员** */
    struct { int x, y; } geo;          /* 逗号列表 */
    int tail;
} S;

/* ② C11 匿名成员（没有成员名）—— 成员**提升**进外层，这条路不能一起改坏 */
typedef struct {
    int p;
    struct { int q; int r; };          /* 注意：`};` 之后没有成员名 */
    int s2;
} T;

/* ③ 匿名 union 当具名成员（同一个分支的另一形态） */
typedef struct {
    int pre;
    union { int u1; int u2; } u;
    int post;
} U;

int main()
{
    S s;
    T t;
    U u;

    s.tag = 1; s.opt.a = 11; s.opt.b = 22; s.geo.x = 33; s.geo.y = 44; s.tail = 55;
    printf("TAG=%d\n", s.tag);
    printf("A=%d\n", s.opt.a);
    printf("B=%d\n", s.opt.b);
    printf("X=%d\n", s.geo.x);
    printf("Y=%d\n", s.geo.y);
    printf("TL=%d\n", s.tail);

    /* 偏移：int 4 字节、贪心排布 ⇒ tag0 / opt4 / geo12 / tail20 / 共 24 */
    printf("O1=%d\n", (int)&s.opt - (int)&s);
    printf("O2=%d\n", (int)&s.geo - (int)&s);
    printf("O3=%d\n", (int)&s.tail - (int)&s);
    printf("IB=%d\n", (int)&s.opt.b - (int)&s.opt);
    printf("SZ=%d\n", (int)sizeof(S));

    /* ② 提升：q/r 直接挂在外层 */
    t.p = 1; t.q = 7; t.r = 8; t.s2 = 9;
    printf("P=%d\n", t.p);
    printf("Q=%d\n", t.q);
    printf("R=%d\n", t.r);
    printf("S2=%d\n", t.s2);
    printf("OQ=%d\n", (int)&t.q - (int)&t);
    printf("OR=%d\n", (int)&t.r - (int)&t);
    printf("OS=%d\n", (int)&t.s2 - (int)&t);
    printf("SZ2=%d\n", (int)sizeof(T));

    /* ③ union 当成员：u 在 4、整块 4 字节，u1/u2 互叠 */
    u.pre = 1; u.u.u1 = 7; u.post = 9;
    printf("PRE=%d\n", u.pre);
    printf("U1=%d\n", u.u.u1);
    printf("U2=%d\n", u.u.u2);
    printf("POST=%d\n", u.post);
    printf("OU=%d\n", (int)&u.u - (int)&u);
    printf("SZU=%d\n", (int)sizeof(U));
    return 0;
}
