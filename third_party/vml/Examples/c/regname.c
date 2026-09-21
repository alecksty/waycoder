/* regname.c —— 「寄存器一律 @ 开头」那一版的验收样例：**变量/函数名就叫寄存器形**
 * （r1 / f1 / d1 / l1 / R2 / F2 / r99 …）必须照常工作。
 *
 * 为什么单独立一个样例：这类名字从前会被**静默**读成寄存器 ——
 *     `int f1;`   读 f1 → 寄存器 F1（读到 R1 的内容，不是变量的值）
 *     `int d1;`   读 d1 → 寄存器 D1 = 17 ⇒ `registers[17]` 直接越界崩
 *     `int l1;`   读 l1 → 寄存器 L1 = 25 ⇒ 同上
 * 症状是"同一份源码里有的一对一错"，而且**编译全绿、只有运行时才现形**
 * （C 前端给全局标量产出 `[f1]` 这种标签名，形状上就是浮点寄存器 F1）。
 * 修法是「寄存器形的名字，**本文件定义了同名标签的就是标签**」—— 本样例把它钉住。
 *
 * 判据：合计一栏。12 项加起来是定值 831，对不上就说明有变量被当成了寄存器。
 *
 * 跑法（桌面，秒级，别为改一行去打 APK）：
 *     dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/c/regname.c
 * 手机上：命令行页敲 `vml run examples/c/regname.c`
 */

int r1 = 11;                        /* 小写 R bank */
int f1 = 22;                        /* 小写 F bank */
int d1 = 33;                        /* 小写 D bank（D1 曾是寄存器 17，直接崩） */
int l1 = 44;                        /* 小写 L bank（L1 曾是寄存器 25，直接崩） */
int R2 = 55;                        /* 大写：与寄存器名**逐字相同**，只差一个 @ */
int F2 = 66;
int r99 = 88;                       /* 越界形：R99 不是合法寄存器，只能当标签 */
int arr_f3[2];                      /* 数组名同样是寄存器形 */
int fn_l2(void) { return 77; }      /* 函数名 */
int add_d2_f4(int d2, int f4) { return d2 + f4; }   /* 形参也叫寄存器形 */

int main(void) {
    int l3 = 99;                    /* 局部量（走栈偏移，本来就与寄存器无关） */
    int sum;

    arr_f3[0] = 111;
    arr_f3[1] = 222;

    sum = r1 + f1 + d1 + l1 + R2 + F2 + fn_l2() + add_d2_f4(1, 2)
        + arr_f3[0] + arr_f3[1] + r99 + l3;

    print_str("期望合计 = 11+22+33+44+55+66+77+3+111+222+88+99 = 831\n");
    print_str("实测合计 = ");
    println_int(sum);

    if (sum == 831)
        print_str("✅ 通过：寄存器形变量名全部正常\n");
    else
        print_str("❌ 失败：有变量被当成了寄存器（合计对不上）\n");

    return 0;
}
