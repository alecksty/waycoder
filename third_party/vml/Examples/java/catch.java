// 接方块 —— 用 **Java** 写的手机游戏
//
// 玩法：左右方向键移动底部挡板，把落下来的球弹回去；没接住就结束。每接住一次 +10 分。
// 比贪吃蛇短一截（无增长、无自撞、无环形回绕），刻意选它当 Java 的第一份例程 ——
// 把「前端能不能写游戏」这件事与「游戏逻辑本身有多复杂」解耦。
//
// ◆ 手机那套 UI
//
// 开窗 / 绘图 / 输入 / 定时器是 C 写的（`Lib/shared/src/vmlui.c` → `vmlui.vml`），
// 由 `vmltool.config.xml` 的 `<Language Name="java" Libs="vmlui.vml">` 挂上来。
//
// ◆ 两条写法要求（不是偏好）
//
//   ① 调库函数必须声明成 `static native` —— `asm()` 已从 Java 前端移除
//      （`CodeGenerator.Expressions.cs:447` 注明「仅限 C/ObjC/C++」），
//      不声明就发 `CALL method_ui_rect`、链接期找不到标签。
//   ② 颜色写**负数十进制**（`-65536` = `0xFFFF0000`），避开十六进制字面量在词法层的解读差异。
//
// ⚠ `corpus/java/skel.java` 的文件头记着「8 参的 ui_rect 参数会整体错位一格」——
//   本次写例程时读了生成汇编，`call lib_vmlui_ui_rect` 之前是**从右到左**逐参压栈、
//   与 C 前端同形（`10` 最后压 = arg0 在栈顶），**没看到错位**。该注记疑为调用约定统一前的旧观察。
//   真机上的画面是否正确，以 `scripts/maui-vml-verify/corpus.tsv` 的 `game-java-catch` 为准。
class Catch {

    static native int ui_scr_w();
    static native int ui_scr_h();
    static native int ui_win_open(String t, int w, int h);
    static native int ui_win_closed();
    static native int ui_win_close();
    static native void ui_clear(int c);
    static native void ui_rect(int x, int y, int w, int h, int c, int fill, int lw, int r);
    static native void ui_circle(int cx, int cy, int r, int c, int fill, int lw);
    static native void ui_text(int x, int y, String s, int c, int size, int anchor);
    static native void ui_present();
    static native int ui_timer_set(int ms, int tag);
    static native void ui_timer_kill(int id);
    static native int ui_wait_msg(int timeout);
    static native int ui_msg_a();
    static native void ui_beep(int freq, int ms);
    static native void ui_keep_on(int on);
    static native void ui_dlg_msg(String title, String body, int style);

    // 状态：0=挡板x 1=球x 2=球y 3=球dx 4=球dy 5=分数 6=最高 7=存活 8=屏宽 9=屏高
    static int[] A = new int[10];

    static void resetGame() {
        A[0] = A[8] / 2 - 40;
        A[1] = A[8] / 2;
        A[2] = 70;
        A[3] = 3;
        A[4] = 5;
        A[5] = 0;
        A[7] = 1;
    }

    static void draw() {
        ui_clear(-15724520);
        ui_text(8, 8, "得分", -6643536, 13, 0);
        ui_rect(58, 11, A[5], 10, -11409298, 1, 0, 0);
        ui_text(A[8] / 2, 8, "最高", -6643536, 13, 1);
        ui_rect(A[8] / 2 + 46, 11, A[6], 10, -63488, 1, 0, 0);
        ui_rect(A[0], A[9] - 40, 80, 12, -63488, 1, 0, 6);
        ui_circle(A[1], A[2], 9, -131246, 1, 0);
        if (A[7] == 0) {
            ui_text(A[8] / 2, A[9] / 2, "按回车重开", -131246, 16, 1);
        }
        ui_present();
    }

    static void step() {
        if (A[7] == 0) {
            return;
        }
        A[1] = A[1] + A[3];
        A[2] = A[2] + A[4];
        if (A[1] < 10) { A[1] = 10; A[3] = 0 - A[3]; }
        if (A[1] > A[8] - 10) { A[1] = A[8] - 10; A[3] = 0 - A[3]; }
        if (A[2] < 30) { A[2] = 30; A[4] = 0 - A[4]; }
        // 接住：球落到挡板带上、且横向落在挡板范围内
        if (A[2] > A[9] - 52 && A[2] < A[9] - 30 && A[1] > A[0] - 9 && A[1] < A[0] + 89) {
            A[4] = 0 - A[4];
            A[2] = A[9] - 52;
            A[5] = A[5] + 10;
            if (A[5] > A[6]) { A[6] = A[5]; }
            ui_beep(880, 30);
        }
        if (A[2] > A[9]) {
            A[7] = 0;
            ui_beep(220, 260);
            draw();
            if (ui_dlg_msg("接方块", "没接住，这一局结束。\n再来一局？（选「否」退出）", 0) != 0) { ui_win_close(); return; }
            resetGame();
        }
    }

    public static void main(String[] args) {
        int w = ui_scr_w();
        int h = ui_scr_h();
        if (w <= 0) { w = 360; }
        if (h <= 0) { h = 620; }
        A[8] = w;
        A[9] = h;
        ui_win_open("接方块", w, h);
        ui_keep_on(1);
        resetGame();
        int tid = ui_timer_set(40, 0);

        while (ui_win_closed() == 0) {
            draw();
            int t = ui_wait_msg(0);
            if (t == 10) { break; }
            if (t == 9) { step(); }
            if (t == 1) {
                int k = ui_msg_a();
                if (k == 27) { break; }
                if (k == 37) { A[0] = A[0] - 20; if (A[0] < 4) { A[0] = 4; } }
                if (k == 39) { A[0] = A[0] + 20; if (A[0] > w - 84) { A[0] = w - 84; } }
                if (k == 13) { resetGame(); }
            }
        }
        ui_timer_kill(tid);
        ui_keep_on(0);
        ui_win_close();
    }
}
