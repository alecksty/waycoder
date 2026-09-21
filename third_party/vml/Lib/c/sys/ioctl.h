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

int ioctl(int fd, unsigned long request, void *arg);

#endif /* _SYS_IOCTL_H */
