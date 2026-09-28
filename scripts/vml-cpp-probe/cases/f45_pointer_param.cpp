// F45：用户函数的**指针形参**被按「被指类型」的宽度读（`char *s` → `MOVEB`）
//
// 症状极具误导性：**直呼库函数完全正常**（`puts("字面量")` 好得很），
// 而**经用户函数转发就什么都拿不到** —— 于是第一反应是"实参没传进去"。
// 实测判据：
//   · `say("lit-arg")` / `say(g)` / `say2("…")` 全打空行，**只有 `sayI(42)` 是好的**
//     ⇒ 与「传参」无关，只与**形参是不是指针**有关；
//   · **同一份源码用 C 前端跑全对** ⇒ 病灶在 C++ 前端的**读变量**那条路
//     （C 那条路本来就恒用 32 位 `MOVE`，所以照不出来）。
//
// 根因：局部变量读取用 `GetTypeLoadInfo(声明类型)` 选指令宽度，而那个函数答的是
// **「被指类型多大」**（它开头就把 `*` 剥掉）—— `char *s` 于是算成 **1 字节** ⇒ `MOVEB`
// ⇒ 读出指针的**最低位那一个字节**当地址。
// `SizeOfType`（`sizeof(T)`）与 `ArgStackBytesForType`（实参占几字节）**各自都先显式判了
// `Contains("*")`**，只有读变量这一处漏了 ⇒ 现补 `GetVarLoadInfo`。
//
// ⚠ **本用例刻意只覆盖 `char*` / `char[]` / `int*` 三种**（4 字节元素）。
//   `float*` / `double*` / `long*` **不在此列，而且是故意的**：那一档现在**编不过**
//   （解引用把目标寄存器写死成 `R0`，而 `MOVEF`/`MOVED`/`MOVEL` 要 F/D/L 类，
//   寄存器类闸当场拦下）。**试过改成类正确的寄存器，只解决一半** —— 能编过但**值仍然是错的**
//   （`double dv=2.5; double *p=&dv; (int)(*p*2.0)` 应 5 得 **2**；`long*` 应 5 得 **1048532**）。
//   真正的缺口在「取址/解引用的宽度」那一段。**编不过比静默算错好**（本仓规矩），
//   所以那一处**有意保持现状**，等宽度那条路一起修好再放开，并把上面这两条数值断言补进来。
//
// 循环带界，防读数跑飞时把探针挂住。
#include <waycoder_ui.h>

static int slen(char *s) { int n = 0; while (n < 64 && s[n] != 0) n++; return n; }
static int alen(char s[]) { int n = 0; while (n < 64 && s[n] != 0) n++; return n; }
static int ilen(int *p)   { return *p; }

int main() {
    char buf[] = "hello";
    int  iv = 7;

    if (slen("abcd") != 4) { cout << "字面量经 char* 形参丢失" << endl; return 1; }
    if (slen(buf) != 5)    { cout << "数组经 char* 形参丢失" << endl; return 1; }
    if (alen("xyz") != 3)  { cout << "字面量经 char[] 形参丢失" << endl; return 1; }
    if (ilen(&iv) != 7)    { cout << "int* 形参读错" << endl; return 1; }

    cout << "ALL PASS" << endl;
    return 0;
}
