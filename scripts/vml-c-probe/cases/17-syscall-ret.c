// 库函数**从 syscall 取返回值**这条路的判据。
//
// ## 为什么单独一条
//
// C 前端里 `int c; asm("SYSCALL #N"); return c;` 这种写法**会把返回值整个丢掉**
// （`c` 永远是未初始化的垃圾），**且不报错**。正确写法是把 asm 当**表达式**用：
// `return asm("SYSCALL #N");` —— 规则写在 `Lib/shared/src/vmlui.c` 头部。
//
// 实测（指令级 trace）：`syscall` 执行完 `registers[0]` 确实是 65，但下一条 `MOVE`
// 读的是**另一个槽**（`move [@R12-4] @R0` 存、`move @R0 [@R12-8]` 读）—— 存读不同源。
//
// ⚠ 波及面：`Lib/shared/src/` 下 **12 个模块、上百处**都是这个写法
// （`vmlsys.c` / `os.c` / `file.c` / `console.c` / `graphics.c` …）。
// 本用例是清理那批时的**判据**：改一处、跑一次，不用逐个人肉推理。
//
// ## 判据怎么取：**同一件事的两种写法必须给同一个答案**
//
// `vmlui.c` 一直用的是正确写法，所以 `ui_tick()` / `ui_rand()` 是现成的**对照组**：
//
//   · `get_tick()`（老写法） vs `ui_tick()`（新写法）—— 都是"自启动以来的毫秒数"。
//     判据取 **> 0**（不去比两者是否相等：那不是同一个时刻取的值）。
//   · `random()`（老写法） vs `ui_rand()`（新写法）—— 连取多次求和，
//     "8 次全是 0" 的概率可忽略 ⇒ 判据取**非零**。
//
// 两个对照都**不依赖具体数值**，所以不会被运行时波动干扰。
#include <stdio.h>

extern int get_tick(void);      /* 写法①：语句形式，**不搬运** ⇒ 坏 */
extern int random(void);        /* 写法① */
extern int ui_tick(void);       /* 写法②：asm 当表达式 ⇒ 好（对照组） */
extern int ui_rand(int n);      /* 写法②（对照组） */
extern int vml_get_tick(void);  /* 写法③：显式 `asm("MOVE [全局], R0")` 搬运 ⇒ 也要验 */
extern int vml_random(void);    /* 写法③ */

int main()
{
    int i;
    int sum;

    /* ① 时刻：两种写法都该 > 0 */
    printf("\nA=%d", get_tick() > 0);
    printf("\nB=%d", ui_tick() > 0);

    /* ② 随机数：连取 8 次求和，"全 0" 直接说明返回值丢了 */
    sum = 0;
    for (i = 0; i < 8; i++) sum = sum + random();
    printf("\nC=%d", sum != 0);

    sum = 0;
    for (i = 0; i < 8; i++) sum = sum + ui_rand(1000);
    printf("\nD=%d", sum != 0);

    /* ③ `vmlsys.c` 那套"显式搬运"的写法（`asm("MOVE [_vml_result_int], R0")`）。
       它与①②都不同：结果经一个**全局中转变量**返回。本用例顺带把它钉住 ——
       它是给**非 C 语言**用的 syscall 接口（见 vmlsys.c 头部），坏了影响的是别的语言。 */
    printf("\nE=%d", vml_get_tick() > 0);
    sum = 0;
    for (i = 0; i < 8; i++) sum = sum + vml_random();
    printf("\nF=%d", sum != 0);

    return 0;
}
// EXPECT: A=1|B=1|C=1|D=1|E=1|F=1
