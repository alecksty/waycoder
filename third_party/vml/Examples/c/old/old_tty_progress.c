/* old_tty_progress.c —— 进度条 / 旋转动画（彩色的 tty）
 *
 * 类别：tty
 * 兼容面：**回车 `\r` 原地重绘**（不换行、不滚屏 —— 老程序做进度条的标准手法）、
 *         SGR 颜色、`fflush(stdout)` 立刻出字、退格 `\b`
 * 出处：自写，仿 90 年代安装程序 / 压缩工具的进度显示。
 */
#include <stdio.h>

#define ESC "\033"

int main(void)
{
    int i;
    char spin[4] = { '|', '/', '-', '\\' };

    printf(ESC "[2J");

    /* ① 百分比进度条：`\r` 回到行首，整行重画 */
    printf("安装中：");
    for (i = 0; i <= 100; i += 5) {
        int j;
        printf("\r" ESC "[36m安装中：[" ESC "[33m");
        for (j = 0; j < 20; j++) printf(j < i / 5 ? "█" : "░");
        printf(ESC "[36m] %3d%%" ESC "[0m", i);
        fflush(stdout);
    }
    printf("\n");

    /* ② 旋转光标（用 `\b` 退回一格重画）—— 老程序在"不知道进度"时就用它 */
    printf("处理中：");
    for (i = 0; i < 12; i++) {
        printf("%c\b", spin[i % 4]);
        fflush(stdout);
    }
    printf("完成\n");

    /* ③ 反白高亮当前项 */
    printf(ESC "[7m当前选中" ESC "[0m / 未选中\n");
    return 0;
}
