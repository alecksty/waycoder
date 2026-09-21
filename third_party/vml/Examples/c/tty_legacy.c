// 老 TTY 程序**依赖的终端行为**体检 —— 命令行页能不能跑它们，看这个。
//
// 为什么单写一个探针而不是拿真程序试：真程序（`sl`/`cmatrix`/`htop` 那类）几乎都依赖
// **ncurses**，没有它连编都编不过；而 ncurses 底下真正依赖的终端行为其实就这么几条。
// 先把这几条量清楚，"哪些老程序能跑"才有答案，而不是一个个试运气。
//
// 每一条都先打一行带标签的说明，再打实际的序列 —— 看屏幕就知道哪条成立。
//
// 跑法：命令行页输入  vml run examples/c/tty_legacy.c

int main()
{
    int i;

    // ── ① 回车覆写（`\r`）：**进度条就靠它** ──
    // 真终端里 `\r` 把光标移回行首，后面的输出盖掉同一行 ⇒ 屏幕上只有一行在变。
    // 若实现只是把 `\r` 丢掉或当换行，这里会看到多行（或一行挤在一起）。
    puts("[1] CR-overwrite (progress bar) ->");
    for (i = 1; i <= 5; i++) {
        printf("\r    progress [%d/5]", i);
    }
    puts("");

    // ── ② 制表符（`\t`）：**表格对齐**靠它 ──
    // 真终端按 8 列制表位（或可配）跳到下一个制表位。若被当普通空格，
    // 下面的列就不会对齐（`ls`/`df`/`ps` 的表格都会歪）。
    puts("[2] TAB columns (should align) ->");
    puts("a\tb\tc");
    puts("aaaa\tbbbb\tcccc");
    puts("aa\tbb\tcc");

    // ── ③ 退格叠打（`\b`）：**粗体/下划线**靠它 ──
    // 老手册页的粗体就是 `X\bX`（打两遍，中间退一格）—— 真终端叠出不加粗，
    // 但至少**不该把 `\b` 显示成可见字符**。
    puts("[3] backspace overstrike ->");
    puts("A\bB\bC    <- 期望看到 ABC，且没有可见的退格符");

    // ── ④ 光标定位与清屏（CSI 序列）：**全屏程序**靠它 ──
    // 我们的命令行页会**吃掉**这类序列（不落到正文），所以下面应当只看到两行文字，
    // 没有 `[2J`/`[H` 这种字面量漏出来。
    puts("[4] cursor/clear sequences (should be eaten, not shown) ->");
    printf("\x1b[2J\x1b[H    after clear+home\n");
    printf("\x1b[31m    red line via SGR\x1b[0m\n");

    // ── ⑤ 亮色 / 256 色 / 真彩 ──
    // 三档都在这里：16 色亮色不带分号，256 色（`38;5;N`）与真彩（`38;2;r;g;b`）**必须带分号**。
    // 后者此前被一个汇编器缺陷吃掉（`.string` 里的 `;` 被当行内注释截断 ⇒ 转义残废、
    // 颜色静默不对），修好之前这一节只能测 16 色。见 `scripts/vml-c-probe/cases/10-semi-in-string.c`。
    puts("[5] colors: bright / 256-color / truecolor ->");
    printf("\x1b[91m    bright red\x1b[0m\n");
    printf("\x1b[104m    bright blue background\x1b[0m\n");
    printf("\x1b[38;5;208m    256-color orange (38;5;208)\x1b[0m\n");
    printf("\x1b[38;2;255;105;180m    truecolor hot pink (38;2;255;105;180)\x1b[0m\n");
    printf("\x1b[48;5;24m    256-color background\x1b[0m\n");

    // ── ⑥ 宽字符（CJK）—— 列宽必须按 2 格算 ──
    puts("[6] wide chars (CJK = 2 cells) ->");
    puts("    中文宽字符|ascii|中文");
    return 0;
}
