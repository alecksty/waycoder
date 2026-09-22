// **`wbkgdset` 的背景属性要真的落到格子上** —— 老程序"用空格 + 底色画图"全靠它
//
// ## 症状
//
// `tty-clock` 的**表盘整个是黑的**（不是"颜色不好看"，是一个字都没有）：
// 它的数字是这么画的 ——
//
//     wbkgdset(win, COLOR_PAIR(1));      // 这一格的底色
//     mvwaddch(win, y, x, ' ');          // 画一个"没有字形"的色块
//
// 而 `wbkgdset` 此前是 `(void)w; (void)ch; return 0;`（注释还写着"背景字符：忽略"）
// ⇒ 每一格都按默认属性落位，`refresh` 只发 `[37m[40m`，屏幕上什么都没有。
// 链接、运行、`waddch` 的返回值**全都正常**，"能编译能跑就是没画面"。
//
// ## 两条一起坏才看不出来
//
// ① `wbkgdset` 不记属性（`WINDOW.bg` 字段 + `sc_write_attr` 合并）；
// ② 即便记了，`sc_sgr` 也认不出**默认色**：它拿 `pair_fg[pair] >= 0` 当
//    "这个颜色对登记过没有"，而 `use_default_colors()` 之后 `-1` 是**合法颜色值**
//    （"用终端默认色"）—— `init_pair(1, -1, COLOR_GREEN)`（tty-clock 的表盘数字
//    正是"默认前景 + 绿底"）会被判成"没登记"，整块退回白字黑底。
//    ⇒ 现在多一张 `pair_set[]`，并把 `-1` 发成 `39`/`49`。
//
// ## 判据形态：这一条**必须**看字节流
//
// `wbkgdset` 的作用面**只有**"发出去的 SGR"，没有可读回的状态
// （本实现没有 `inch()`；`stdscr->bg` 只能证明"记下了"，证明不了"落上去了"）。
// 为了把期望串压到可写，三行都只碰**一行 80 格**：
//   A：只改当前属性、不让任何行变脏 ⇒ `refresh` 只发出属性那一段（最短的一条）；
//   B：`wbkgdset(COLOR_PAIR(2))` + 79 个 `waddch` ⇒ 前 79 格是绿字默认底、第 80 格仍是默认
//      （两段 SGR 的交界正好落在列 80）；
//   C：**反例** —— `wbkgdset(w, 0)` 不许凭空注入颜色（整行一段 SGR）。
// STDIN:
// EXPECT: A=\e[0m\e[39m\e[42m\e[1;1H|B=\e[24;1H\e[0m\e[32m\e[49mGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG\e[24;80H\e[0m\e[39m\e[49m \e[0m\e[39m\e[49m\e[24;80H|C=\e[23;1H\e[0m\e[39m\e[49mEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE \e[0m\e[39m\e[49m\e[23;80H
#include <stdio.h>
#include <curses.h>

int main()
{
    int i;

    initscr();
    start_color();
    use_default_colors();               /* 之后 -1 = 终端默认色 */
    init_pair(1, -1, COLOR_GREEN);      /* 默认前景 + 绿底 —— tty-clock 的表盘数字 */
    init_pair(2, COLOR_GREEN, -1);      /* 绿字 + 默认底 */

    clear();
    printf("\n");                       /* 让整屏那一发独占一行 */
    refresh();
    printf("\n");

    /* ── A：只改当前属性（没有脏行）⇒ refresh 只发属性那一段 ── */
    attron(COLOR_PAIR(1));
    printf("\nA=");
    refresh();
    printf("\n");
    attroff(COLOR_PAIR(1));

    /* ── B：窗口背景经 waddch 落到格子上（用倒数第二行，回绕不脏别的行）── */
    wbkgdset(stdscr, COLOR_PAIR(2));
    move(23, 0);
    for (i = 0; i < 79; i++) waddch(stdscr, 'G');
    printf("\nB=");
    refresh();
    printf("\n");

    /* ── C：反例 —— 背景为 0 时不许注入颜色 ── */
    wbkgdset(stdscr, 0);
    move(22, 0);
    for (i = 0; i < 79; i++) waddch(stdscr, 'E');
    printf("\nC=");
    refresh();
    printf("\n");

    /* ⚠ 这里**不**读 `stdscr->bg` 回来对账：program 侧 `指针->字段` 的成员偏移另有问题
       （实测 `stdscr->cols` 读出来是 `attr` 的值、经 `wbkgdset` 写的 `bg` 读回恒 0，
       撤掉本轮全部改动后现象一字不差 ⇒ **既有缺陷**，与 `wbkgdset` 无关）。
       本条的判据只取**发出去的 SGR**（A/B/C），那才是 `wbkgdset` 唯一的作用面。 */

    /* `endwin()` 会把光标放回左下角（发 `ESC[25;1H`）—— 不先断行会粘在上一条判据的尾巴上 */
    printf("\n");
    endwin();
    return 0;
}
