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
//   ④ **真根因找到了：C 前端不认 `extern` 关键字**。
//      链接产物里**同时**有两个符号：
//        77:  stdscr: .word 0                        ← **主程序自己生成的**
//        126: lib_curses_stdscr: .word lib_curses_sc_win   ← 库里真正的定义
//      `curses.h` 里写的是 `extern WINDOW *stdscr;`（**声明**），
//      而 `ASTNode` 只有 `IsStatic`、**没有 `IsExtern`**，
//      `Parser.Statements.cs` 里**全无 `extern` 的处理** ⇒
//      **前端把它当普通全局变量定义**，于是在**每个使用者的数据段**里
//      都生成一个 `.word 0`；测试程序读 `[stdscr]` 解析到的正是**这个空的**。
//
//      探针实测（`VML_TRACE_DATA=1`，已删）：
//        `[DATA] key=lib_curses_stdscr ref=lib_curses_sc_win found=1 val=18768`
//      —— 库那边的**值是对的**（18768），问题纯粹在"读到了另一个符号"。
//
//      **修法**（下一轮）：AST 加 `IsExtern` → 解析器认 `extern` →
//      全局变量生成时**跳过 extern 声明的**（不占数据段槽位）。
//      影响面远超 curses：**所有用 `extern` 声明跨模块全局变量的 C 代码**。
//      ⚠ 这条已**独立于 curses**，值得单独一条判据（`extern int x;` 之后
//      由别处定义、两处读到的必须是同一个）。
// EXPECT: A=1|B=1|C=1|D=0|E=0|F=0|G=0|H=0|I=0|J=0|K=0|L=0|M=0|N=0|O=0|P=0|Q=0|R=0|S=25,80|T=0,0|U=-1,-1|S2=1|S3=0|S4=0|V=0,4|W=-1
