/* regname.c —— 「寄存器一律 @ 开头」那一版的验收样例：**变量/函数名就叫寄存器形**
 * regname.c — acceptance sample for the "registers always start with @" version: **variables/functions named like registers**
 * （r1 / f1 / d1 / l1 / R2 / F2 / r99 …）必须照常工作。
 * (r1 / f1 / d1 / l1 / R2 / F2 / r99 ...) must keep working as usual.
 *
 * 为什么单独立一个样例：这类名字从前会被**静默**读成寄存器 ——
 * Why a separate sample: these names used to be **silently** read as registers —
 *     `int f1;`   读 f1 → 寄存器 F1（读到 R1 的内容，不是变量的值）
 *     `int f1;`   reading f1 -> register F1 (you got the contents of R1, not the variable's value)
 *     `int d1;`   读 d1 → 寄存器 D1 = 17 ⇒ `registers[17]` 直接越界崩
 *     `int d1;`   reading d1 -> register D1 = 17 => `registers[17]` goes out of bounds and crashes
 *     `int l1;`   读 l1 → 寄存器 L1 = 25 ⇒ 同上
 *     `int l1;`   reading l1 -> register L1 = 25 => same as above
 * 症状是"同一份源码里有的一对一错"，而且**编译全绿、只有运行时才现形**
 * The symptom is "some work and some do not in the same source file", and **the build is all green, it only shows at runtime**
 * （C 前端给全局标量产出 `[f1]` 这种标签名，形状上就是浮点寄存器 F1）。
 * (the C frontend emits label names like `[f1]` for global scalars, which by shape look like the float register F1).
 * 修法是「寄存器形的名字，**本文件定义了同名标签的就是标签**」—— 本样例把它钉住。
 * The fix is "for a register-shaped name, **if this file defines a label with that name it is a label**" — this sample pins it down.
 *
 * 判据：合计一栏。12 项加起来是定值 831，对不上就说明有变量被当成了寄存器。
 * Criterion: the total line. The 12 items add up to the fixed value 831; a mismatch means some variable was taken for a register.
 *
 * 跑法（桌面，秒级，别为改一行去打 APK）：
 * How to run (desktop, takes seconds — do not rebuild the APK just to change one line):
 *     dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/c/regname.c
 * 手机上：命令行页敲 `vml run examples/c/regname.c`
 * On mobile: type `vml run examples/c/regname.c` in the command line page
 */

#include <waycoder_ui.h>            /* 只为 ui_get_language（界面语言）：头文件只有声明与常量，不会让程序变大 */
/* Only for ui_get_language (UI language): the header holds declarations and constants only, it does not grow the program */

int r1 = 11;                        /* 小写 R bank */
                                    /* lowercase R bank */
int f1 = 22;                        /* 小写 F bank */
                                    /* lowercase F bank */
int d1 = 33;                        /* 小写 D bank（D1 曾是寄存器 17，直接崩） */
                                    /* lowercase D bank (D1 used to be register 17, instant crash) */
int l1 = 44;                        /* 小写 L bank（L1 曾是寄存器 25，直接崩） */
                                    /* lowercase L bank (L1 used to be register 25, instant crash) */
int R2 = 55;                        /* 大写：与寄存器名**逐字相同**，只差一个 @ */
                                    /* Uppercase: **character-for-character identical** to the register name, off by one @ */
int F2 = 66;
int r99 = 88;                       /* 越界形：R99 不是合法寄存器，只能当标签 */
                                    /* Out-of-range shape: R99 is not a legal register, it can only be a label */
int arr_f3[2];                      /* 数组名同样是寄存器形 */
                                    /* An array name is register-shaped too */
int fn_l2(void) { return 77; }      /* 函数名 */
                                    /* A function name */
int add_d2_f4(int d2, int f4) { return d2 + f4; }   /* 形参也叫寄存器形 */
                                                    /* The parameters are register-shaped too */

int main(void) {
    int l3 = 99;                    /* 局部量（走栈偏移，本来就与寄存器无关） */
                                    /* A local (addressed by stack offset, unrelated to registers anyway) */
    int sum;
    int lang;                       /* 界面语言：开局查一次（ui_get_language 是 syscall，别每帧调） */
                                    /* UI language: queried once at start (ui_get_language is a syscall, do not call it every frame) */

    lang = ui_get_language();

    arr_f3[0] = 111;
    arr_f3[1] = 222;

    sum = r1 + f1 + d1 + l1 + R2 + F2 + fn_l2() + add_d2_f4(1, 2)
        + arr_f3[0] + arr_f3[1] + r99 + l3;

    print_str(lang == 0 ? "期望合计 = 11+22+33+44+55+66+77+3+111+222+88+99 = 831\n"
                        : "expected total = 11+22+33+44+55+66+77+3+111+222+88+99 = 831\n");
    print_str(lang == 0 ? "实测合计 = " : "actual total = ");
    println_int(sum);

    if (sum == 831)
        print_str(lang == 0 ? "✅ 通过：寄存器形变量名全部正常\n" : "PASS: register-shaped variable names all work\n");
    else
        print_str(lang == 0 ? "❌ 失败：有变量被当成了寄存器（合计对不上）\n" : "FAIL: a variable was taken for a register (total mismatch)\n");

    return 0;
}
