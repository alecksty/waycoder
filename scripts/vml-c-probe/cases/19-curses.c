// `curses.h` 的判据：**坐标语义**与输出推进。
//
// ## 为什么先钉坐标
//
// `curses` 与 `conio` 长得很像（都在画字符网格、都在发 ANSI），但**坐标原点相反**：
//
//     conio  : gotoxy(1,1) 是左上角   —— **1 起**
//     curses : move(0,0)   是左上角   —— **0 起**
//
// 这是"一起用过的代码最容易出错"的地方，而且错了**不报错** —— 整个界面平移一格
// （菜单框少一条边、标题压到状态栏上）。
//
// ## 判据怎么取：**读回缓冲光标**，不比对整屏字节流
//
// `refresh()` 会把**整屏 25×80** 重发一遍（两千多个字符 + 一堆 ANSI），拿它当期望串
// 又长又脆。而 `getcury()`/`getcurx()` 能把缓冲光标**读回来** —— 而"光标在哪"
// 正是坐标系那件事本身。所以本用例**故意不调 `refresh()`**：
//
//   · 测的是 `move`/`addstr`/越界判定的**状态机**（这才是语义所在）
//   · 输出流那条路已经由 `13`/`14`/`15` 三个 conio 用例压过了（同一套 ANSI 出口）
//
#include <stdio.h>
#include <curses.h>

int main()
{
    WINDOW *w;

    w = initscr();

    /* ① 原点就是 (0,0) —— **不是 (1,1)** */
    printf("\nO=%d,%d", getcury(w), getcurx(w));

    /* ② `move` 是 0 起：move(2,3) ⇒ 读回 2,3（不是 3,4） */
    move(2, 3);
    printf("\nP=%d,%d", getcury(w), getcurx(w));

    /* ③ 输出推进列；`addch('\n')` 回到**第 0 列**（curses 的换行语义） */
    move(5, 5);
    addstr("abc");
    printf("\nQ=%d,%d", getcury(w), getcurx(w));
    addch(10);
    printf("\nR=%d,%d", getcury(w), getcurx(w));

    /* ④ 越界要**拒绝**（返回 ERR = -1），不是静默钳制 —— 与 conio 的 gotoxy
          "钳到边界"**相反**，老程序靠返回值判"画不下了" */
    printf("\nS=%d", move(99, 99));
    printf("\nT=%d", move(-1, 0));

    /* ⑤ 换行后越界的那一档：写满最后一行也不该跑到 25 行去 */
    move(24, 79);
    addch('Z');
    printf("\nU=%d,%d", getcury(w), getcurx(w));

    /* ⚠ `endwin()` 会把真实光标放回左下角（发一条 `ESC[25;1H`）——
       不先断行的话它会**粘在最后一条判据的尾巴上**（14/15 同一个坑）。 */
    printf("\n");
    endwin();
    return 0;
}
// EXPECT: O=0,0|P=2,3|Q=5,8|R=6,0|S=-1|T=-1|U=24,0
