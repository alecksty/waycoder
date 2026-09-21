// `curses.h` **新增接口**的判据：把这一批一次钉住，别再靠"从现象反推"。
//
// ## 为什么单独一条、而且是"一次一条"
//
// 老程序兼容那条线里一口气往 `curses.c` 补了二十来个接口
// （`leaveok`/`scrollok`/`timeout`/`napms`/`addwstr`/`wnoutrefresh`…），
// **但除了 `addwstr` 一个判据都没有**。后果是 `addwstr` 出事时：
// 手上没有"它本来该输出什么"的基线，只能从现象反推 ——
// 而反推又反复被自己的探针/判据偏移误导，一个坑转了十几轮。
//
// **有判据的话，那种事五分钟就能定位。** 所以这个用例的原则是：
// **每个接口至少一条判据**，且期望值**独立推导**（不是"跑出来是什么就写什么"）。
//
// ## 判据怎么取：沿用 19 号的做法 —— **读状态，不比对整屏字节流**
//
// `refresh()` 会重发整屏（两千多字符 + 一堆 ANSI），当期望串又长又脆。
// 所以这里测**返回值**与**可读回的状态**，只有 `addwstr` 一条要看到输出
// （它是"写缓冲"的语义，没有别的观察面）。
//
// ## 特别重要的一条：`timeout(0)` 之后的 `wgetch` 必须返回 ERR
//
// 那是 `cmatrix` / `tty-clock` 这类**动画老程序主循环的节拍器**：
//
//     timeout(0);
//     while (1) { …画…; if ((ch = wgetch(stdscr)) != ERR) {…} }
//
// 返回的不是 ERR ⇒ 代码会以为"用户按了键"，进而走进按键处理分支。
// （实测踩过：`kbhit` 恒真时 `wgetch` 永不返回 ERR，整类程序**一帧都画不出来**。）
#include <stdio.h>
#include <curses.h>

int main()
{
    WINDOW *w;

    w = initscr();

    /* ── ① 能力查询：恒为真（我们是重画整屏，从调用方看结果一样） ── */
    printf("\nA=%d", has_ic());
    printf("\nB=%d", has_il());
    printf("\nC=%d", has_colors());

    /* ── ② "收下即成功"的一批：**返回 OK(0)**，不能返回 ERR ──
       返回 -1 会让老程序当成"终端不支持"而走进降级分支（甚至直接退出）。 */
    printf("\nD=%d", beep());
    printf("\nE=%d", flash());
    printf("\nF=%d", nonl());
    printf("\nG=%d", savetty());
    printf("\nH=%d", resetty());
    printf("\nI=%d", raw());
    printf("\nJ=%d", noraw());
    printf("\nK=%d", doupdate());

    /* ── ③ 窗口选项：收下 + 返回 OK ── */
    printf("\nL=%d", leaveok(w, TRUE));
    printf("\nM=%d", scrollok(w, TRUE));
    printf("\nN=%d", idlok(w, TRUE));

    /* ── ④ 属性简写：转调 attron/attrset，真的改当前属性 ── */
    printf("\nO=%d", standout());
    printf("\nP=%d", standend());

    /* ── ⑤ `napms`：返回 OK；真睡没睡另用 tick 判（见 18 号 dos 用例） ── */
    printf("\nQ=%d", napms(0));
    printf("\nR=%d", napms(5));

    /* ── ⑥ 几何宏：`getmaxyx` / `getbegyx` / `getparyx` ──
       单窗口退化实现下：尺寸 25×80、起点 (0,0)、不是子窗口 ⇒ (-1,-1)。 */
    {
        int my = -9, mx = -9, by = -9, bx = -9, py = -9, px = -9;
        getmaxyx(w, my, mx);
        getbegyx(w, by, bx);
        getparyx(w, py, px);
        printf("\nS=%d,%d", my, mx);
        printf("\nT=%d,%d", by, bx);
        printf("\nU=%d,%d", py, px);
        /* 诊断：`stdscr` 到底指向哪儿 —— `S` 读成 0,0 说明它的成员全是零 */
        printf("\nS2=%d", (int)stdscr != 0);
        printf("\nS3=%d", (int)w != (int)stdscr);
        printf("\nS4=%d", (int)stdscr);
    }

    /* ── ⑦ `addwstr`：**宽字符**，逐码点编码成 UTF-8 落进缓冲 ──
       两个码点：ASCII 的 'A'（1 字节）与 U+4E2D「中」（**3 字节**）。

       ⚠ 判据用**读回光标位置**而不是比对输出字节流：`refresh()` 会在字符前
       插一串 ESC（定位 + 颜色），期望串会又长又脆（19 号那条注释里的理由）。
       而"光标推进了多少"恰好就是这个接口的可观察契约：

           'A'  → 1 个字节 → 推进 1
           '中' → 3 个字节 → 推进 3
           合计 **4 列**

       （`sc_ch` 是**字节**缓冲，`refresh` 原样发这一行的字节、终端再按 UTF-8
        合成字形 —— 所以"一个字节占一格"是自洽的。早先按"一个码点占一列"
        写，结果是后一个字节覆盖前一个，实测只发出 `AD`。） */
    {
        wchar_t ws[3];
        ws[0] = 'A';
        ws[1] = 0x4E2D;
        ws[2] = 0;
        clear();
        move(0, 0);
        addwstr(ws);
        printf("\nV=%d,%d", getcury(w), getcurx(w));
    }

    /* ── ⑧ **节拍器**：`timeout(0)` 之后 `wgetch` 没键必须返回 ERR(-1) ──
       `-1` 不是"随便一个负数"，它**就是 `ERR` 的值**（`curses.h` 里
       `#define ERR (-1)`）—— 老程序写的是 `!= ERR`，值错了分支就走错。
       ⚠ 这一条要**放在最后**：它会改 `nodelay` 状态，影响后续所有读键。 */
    timeout(0);
    printf("\nW=%d", wgetch(stdscr));

    /* ⚠ `endwin()` 会把真实光标放回左下角（发一条 `ESC[25;1H`）——
       不先断行的话它会**粘在最后一条判据的尾巴上**（19 号同一个坑）。 */
    printf("\n");
    endwin();
    return 0;
}
// KNOWN-RED —— 19 条里 **17 条已绿**，卡住的是 ⑥ 那组的 `S`/`S2`/`S3`：
// `stdscr` 在**使用者那边**读到 NULL（`S2=0`），所以 `getmaxyx` 从它取不到尺寸。
//
// 已经查清的链路（三段，前两段已修）：
//   ① `WINDOW *stdscr = &sc_win;` 的**初始化器**本来就没生成代码
//      （`CodeGenerator.Functions.cs` 的初始化器分支没有"取址"这一支）
//      ⇒ 已加，现在 `curses.vml` 里是 `stdscr: .word sc_win` ✓
//   ② `VmlProgram.Load` 解析 `.word <非数字>` 时**什么都不做**（槽位直接丢失）
//      ⇒ 已加 `LabelRef` 分支 ✓
//   ③ **还差**：链接器走的是 `VmlAssembler.AssembleWithIncludes`（不是 ②那条路），
//      而它的 `ParseValue` 对"不是数字"的输入**原样返回字符串** ⇒ 落成 `.string "sc_win"`。
//      试过在那里改成 `LabelRef`，**但引出了回归**（`02-ptr-ptr.c` 的
//      `char *rows[] = {"abc","def"}` 全变 NULL），已回退 —— 说明"裸标识符=标签引用"
//      这个判据**太宽**，把"数组初始化器里的字符串标签"也圈进去了。
//   ④ **还差**（当前卡点）：链接后的**文本已经全对**
//      （`lib_curses_stdscr: .word lib_curses_sc_win`），
//      但运行时 `stdscr` 本身读到 **0**（`S4=0`）——
//      即那个槽位**被填了 0**。两种可能，下一轮先分清：
//        (a) `labelAddresses["lib_curses_sc_win"]` 查不到（标签不在表里）
//        (b) 查得到但**时机不对** —— 数据段是**按 `DataSection` 遍历序**逐个
//            分配内存的（循环末尾才 `labelAddresses[data.Key] = address`），
//            若 `stdscr` 排在 `sc_win` **之前**处理，解析 `LabelRef` 时
//            后者还没登记 ⇒ 取到 0。`curses.vml` 里两者顺序是对的
//            （`sc_win` 在前），但 `DataSection` 是字典，**不能依赖遍历序**。
//      正解大概率是**两趟**：先把所有槽位的地址登记齐，再填内容。
//      判据 `S4`（`(int)stdscr`）就是为分清这两种可能加的。
// EXPECT: A=1|B=1|C=1|D=0|E=0|F=0|G=0|H=0|I=0|J=0|K=0|L=0|M=0|N=0|O=0|P=0|Q=0|R=0|S=25,80|T=0,0|U=-1,-1|S2=1|S3=0|S4=0|V=0,4|W=-1
