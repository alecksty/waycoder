/* sys/ioctl.h —— 终端 ioctl。
   ⚠ 本平台**没有真终端**（界面是一个固定 80×25 的字符网格），所以：
   · `TIOCGWINSZ`（问窗口尺寸）**必须能答** —— 老程序拿它做布局，答不出来就是
     "画到屏幕外"或者死循环。这里恒返回 80×25。
   · 其它请求一律返回 0（成功但什么都不做）：返回 -1 会让不少程序直接退出。 */
#ifndef _SYS_IOCTL_H
#define _SYS_IOCTL_H

#param lib("util")

struct winsize {
    unsigned short ws_row;
    unsigned short ws_col;
    unsigned short ws_xpixel;
    unsigned short ws_ypixel;
};

#define TIOCGWINSZ 0x5413
#define TIOCSWINSZ 0x5414
#define TIOCNOTTY  0x5422
#define FIONREAD   0x541B

/* ⚠ `request` 用 **`int`**，不是 POSIX 的 `unsigned long`。
   本平台 `long` 占**两个参数槽**（8 字节），而实现在 `Lib/shared/src/util.c`
   里是 `int` —— 两边不一致时**参数会错开一个槽**：`arg` 读成 0、
   `TIOCGWINSZ` 分支被整个跳过，而函数**返回 0**（成功）。
   症状是"程序拿到的窗口尺寸恒为 0×0，且看不出任何异常"。
   改这里就要同时改实现，反之亦然 —— 判据是 `cases/24-termios-ioctl.c`。 */
int ioctl(int fd, int request, void *arg);

#endif /* _SYS_IOCTL_H */
