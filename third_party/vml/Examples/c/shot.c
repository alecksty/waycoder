/* shot.c —— 主动截屏（`ui_screenshot` / syscall #588）的示例与自检程序。
 *
 * ## 它演示什么
 *
 * 程序**自己**把当前画布截下来存成 PNG（不需要宿主帮忙、不需要用户按截屏键）。
 * 存的是**你画的那张画面**（不含屏幕手柄与标题栏）—— 拿来当"战绩图 / 通关留念"正好。
 * 实现走共享光栅器，四端同一份代码、不需要任何权限。
 *
 * ## 三条要知道的
 *
 * 1. **路径是相对沙箱的**：空串用 `shot.png`、没扩展名自动补 `.png`、子目录会自动建。
 *    ⚠ 含 `..`/`.` 段或盘符（`C:\`、`http:`）的路径**直接失败**，什么都写不出来 ——
 *    那是防写到你沙箱外面的唯一一道闸，故意不宽容。
 * 2. **会阻塞几十毫秒**（光栅化 + 编码 PNG）⇒ **别放进每帧循环**，
 *    放在"这一局结束""按了分享键"这种一拍一次的地方。
 * 3. **找不到文件别以为程序坏了**：文件落在**源文件旁边**（桌面端）或
 *    **工作区根**（手机端，App 的「文件」页能看到）。桌面先用仓库里的 vmlcli 跑：
 *
 *        dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/c/shot.c
 *
 * ## C 前端的三条硬约束（与 mario.c / tetris.c 同源，踩过才写的）
 *
 * 1. ⚠ `${}` 占位符只认局部变量 ⇒ 调 syscall 一律走 `waycoder_ui.h` 的包装函数。
 * 2. 文件级全局变量是好的，可以随便用。
 * 3. ⚠ `#define` 不支持反斜杠续行 ⇒ 宏一行写完。
 */

#include <waycoder_ui.h>
#include <stdlib.h>

#define BG_COLOR 0xFF102030
#define FRAME_COLOR 0xFFFF0000
#define DISC_COLOR 0xFF00FF00
#define TEXT_COLOR 0xFFFFFFFF

int main(void)
{
    int w;
    int h;
    int n;

    w = ui_scr_w();
    h = ui_scr_h();

    /* 不要手柄区：把画布让给自己（手柄会占掉一百多 dp） */
    if (ui_win_open_ex("shot", w, h, VML_WIN_ROTATABLE, VML_WIN_NO_GAMEPAD) < 0) {
        ui_dlg_msg("失败", "开窗失败", VML_DLG_ERROR);
        return 1;
    }

    ui_clear(BG_COLOR);
    ui_rect(10, 10, w - 20, h - 20, FRAME_COLOR, 0, 6, 0);   /* 描边方框 */
    ui_circle(w / 2, h / 2, 60, DISC_COLOR, 1, 0);           /* 实心圆 */
    ui_text(20, 40, "SCREENSHOT DEMO", TEXT_COLOR, 20, VML_ANCHOR_LEFT);
    ui_text(20, 70, "ui_screenshot(\"\")", TEXT_COLOR, 14, VML_ANCHOR_LEFT);
    ui_present();

    /* ① **推荐用法：传空串** ⇒ 自动命名成 shot/<窗口标题>_<日期>_<时间>.png
     *   （如 shot/shot_20260924_102801.png）。连存几张不会互相覆盖。 */
    n = ui_screenshot("");

    /* ② 非法路径（回退段）⇒ -1，且盘上不该多出任何东西。
     *    这一条是"防逃逸"的现场证据：注释掉上面那句、只留这句，程序应当什么也不产出。 */
    ui_screenshot("../escape.png");

    /* 报数不用 printf —— 本环境的 puts/printf 在字面量多的程序里会串行/重复，
     * 会把 stdio 的毛病看成代码的毛病（见 docs/VML游戏开发指南.md §7）。
     * 用 ui_dlg_msg 直接把返回值写出来看。 */
    if (n > 0) {
        ui_dlg_msg("截屏成功", "已自动命名存进 shot/ 目录（手机：工作区根；桌面：源文件旁边）", VML_DLG_INFO);
    } else {
        ui_dlg_msg("截屏失败", "ui_screenshot 返回 -1：路径非法 / 还没有画布 / 写不进去", VML_DLG_ERROR);
    }

    ui_win_close();
    return 0;
}
