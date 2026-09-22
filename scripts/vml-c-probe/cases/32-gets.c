// `gets()` —— 老 C **最典型的那句读循环** `while (gets(buf))`
//
// ## 为什么单立一条
//
// `gets` 是「**同名、签名不同**」撞车的受害者：
//   · `Lib/shared/src/console.c` 里原来那个 `__stdcall char* gets(void)` 是**零参数**
//     的内部助手（`SYSCALL #2`，读进**内部**缓冲区并返回那个指针）；
//   · 而 `Lib/c/stdio.h:52` 声明的是标准的 `char *gets(char *s)`（要一个缓冲区）。
//
// 老程序写 `while (gets(buf))` 时：`buf` 被当参数压进去、**被实现彻底忽略**（它没有形参）
// ⇒ **`buf` 永远填不上**，程序读到自己的旧内容 —— **不报错、结果错**。
// 本判据钉的就是这条（`A=`/`B=` 必须真是喂进去的那两行）。
//
// ## 第二个钉子：**EOF 必须返回 NULL**
//
// 老程序的 `while (gets(buf))` 是靠 NULL 收尾的。而这条要求 `gets` 逐字符走
// `SYSCALL #14`（输入耗尽给 **-1**），**不能**用现成的 `read_line` ——
// 它走 `#5`，而 `#5` 在耗尽时返回 `0x0A`（那是给 `conio.getch()` 的单键读定的），
// **区分不出"空行"与"EOF"**，两者都长得像"读到一个换行"。
// 所以 `C=` 必须是 `NULL`：若实现退回 `#5`，第三次调用会读到"空行"而返回 `""`，
// 本判据当场变红。
//
// STDIN: hello\nworld\n
// EXPECT: A=hello|B=world|C=NULL|OK=1

#include <stdio.h>

int main() {
    char buf[64];
    int ok = 1;

    if (gets(buf)) printf("\nA=%s", buf); else { ok = 0; printf("\nA=NULL"); }
    if (gets(buf)) printf("\nB=%s", buf); else { ok = 0; printf("\nB=NULL"); }
    /* 第三次：输入已耗尽 ⇒ 必须是 NULL（不是空行） */
    if (gets(buf)) { ok = 0; printf("\nC=%s", buf); } else printf("\nC=NULL");
    printf("\nOK=%d", ok);
    return 0;
}
