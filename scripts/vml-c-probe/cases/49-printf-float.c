// EXPECT: OK49=1
#include <stdio.h>

// `printf` 的浮点转换（账本 OPEN #9）——**变参占几个槽**这条规则的两半都要对
//
// 修的是**两个各自独立**的东西，少一半都还是错的：
//
// ① **库侧**：`printf` 家族拿 `format_arg_count(fmt)`（**转换个数**）当 `nargs`，
//    而 `vsnprintf` 内部是按**槽**索引 `args[]` 的 ⇒ `%f` 只算 1 个 ⇒
//    `va_arg(ap,int)` 只抄走一个槽 ⇒ double 的高半字丢掉 ⇒ 打 `0.000000`。
//    新增 `format_slot_count()`（`%f/%e/%g` 与带 `l` 的转换算 2 槽）。
//    ⚠ 它**不是** `format_arg_count` 的"更正确版本"，**两个问题不同**：
//    `scanf("%f", &d)` 的变参是**指针**（1 槽），那里必须是转换个数。
//
// ② **前端侧**：只把实参标成"占 2 槽"**不够**，值也得转上去 ——
//    `float` 变量在 R0 里是 **float 的位模式**，而压栈那条 `MOVED` 照样写 8 字节
//    ⇒ 高半字是**栈上残留的别的值**。实测 `float f = 2.25; printf("%f", f)`
//    打出 **`1.500000`**（低半字 2.25 的 float 位模式 + 高半字上一次调用 double 1.5
//    留下的位模式）—— **它甚至不是 0，是个"看着像对"的数**，最难查。
//    修法：压栈前补一次 `F2D`。
//
// ⚠ 判据走 `snprintf` 写进缓冲区再**逐字节比**，不走 stdout ——
//   本平台的 `puts`/`cout` 在多字面量程序里会串行/重复（记过一次），
//   而 `snprintf` 测的正好也是库那条路。
int main(void) {
    char buf[64];
    double d = 1.5;
    float  f = 2.25;
    int ok = 1;

    snprintf(buf, sizeof(buf), "%f", d);
    if (buf[0] != '1' || buf[1] != '.' || buf[2] != '5' || buf[3] != '0') ok = 0;

    snprintf(buf, sizeof(buf), "%f", f);
    if (buf[0] != '2' || buf[1] != '.' || buf[2] != '2' || buf[3] != '5') ok = 0;

    snprintf(buf, sizeof(buf), "%.2f", 3.14159);
    if (buf[0] != '3' || buf[1] != '.' || buf[2] != '1' || buf[3] != '4') ok = 0;

    snprintf(buf, sizeof(buf), "%d/%s/%f", 7, "hi", 0.5);
    if (buf[0] != '7' || buf[1] != '/' || buf[2] != 'h' || buf[3] != 'i') ok = 0;
    if (buf[4] != '/' || buf[5] != '0' || buf[6] != '.' || buf[7] != '5') ok = 0;

    printf("OK49=%d\n", ok);
    return 0;
}
