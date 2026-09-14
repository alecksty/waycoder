/* console.h - POSIX 标准控制台库 (termios 简化版)
 *
 * OS 模式: 通过 dev_open("console") + dev_control() 与 VmConsoleDevice 通信
 * MCU 模式: tcgetattr 返回默认值, tcsetattr 为 no-op
 */

#ifndef _CONSOLE_H
#define _CONSOLE_H

/* ---- termios 结构 (简化版 POSIX) ---- */
struct termios {
    int  c_iflag;     /* 输入标志 */
    int  c_oflag;     /* 输出标志 */
    int  c_cflag;     /* 控制标志 */
    int  c_lflag;     /* 本地标志 */
    char c_cc[8];     /* 特殊控制字符 */
};

typedef struct termios termios_t;

/* ---- winsize 结构 ---- */
struct winsize {
    int ws_row;       /* 行数 (用 int 替代 unsigned short) */
    int ws_col;       /* 列数 */
};

typedef struct winsize winsize_t;

/* ---- termios 标志位 ---- */
/* c_lflag (本地模式) */
#define ECHO      0x0001   /* 输入回显 */
#define ICANON    0x0002   /* 规范模式 (行缓冲) */
#define ISIG      0x0004   /* 信号处理 (Ctrl+C 等) */
#define ECHOE     0x0008   /* 退格时擦除字符 */
#define ECHOK     0x0010   /* 行删除后回显换行 */
#define ECHONL    0x0020   /* 规范模式下回显 NL */
#define IEXTEN    0x0040   /* 扩展输入处理 */

/* c_iflag (输入模式) */
#define ICRNL     0x0100   /* 将 CR 映射为 NL */
#define IXON      0x0200   /* 输出流控 XON/XOFF */
#define IXOFF     0x0400   /* 输入流控 XON/XOFF */
#define IGNCR     0x0800   /* 忽略 CR */
#define INLCR     0x1000   /* 将 NL 映射为 CR */

/* c_oflag (输出模式) */
#define OPOST     0x0001   /* 输出处理 */
#define ONLCR     0x0002   /* 将 NL 映射为 CR-NL */

/* ---- tcsetattr 动作 ---- */
#define TCSANOW    0        /* 立即生效 */
#define TCSADRAIN  1        /* 等待输出完成 */
#define TCSAFLUSH  2        /* 等待输出完成并刷新输入 */

/* ---- tcflush 队列选择器 ---- */
#define TCIFLUSH   0        /* 刷新输入队列 */
#define TCOFLUSH   1        /* 刷新输出队列 */
#define TCIOFLUSH  2        /* 刷新输入输出队列 */

/* ---- DeviceControl 命令 (与 VmConsoleDevice 同步) ---- */
#define CON_TCGETATTR     4   /* 获取终端属性 */
#define CON_TCSETATTR     5   /* 设置终端属性 */
#define CON_TCFLUSH       6   /* 刷新缓冲区 */
#define CON_GET_TERM_SIZE 7   /* 获取窗口大小 */
#define CON_GET_TTY_NAME  8   /* 获取终端名称 */
#define CON_ISATTY        9   /* 检查是否为终端 */

/* ---- 函数声明 ---- */
int  isatty(int fd);
char* ttyname(int fd);

int  tcgetattr(int fd, termios_t *t);
int  tcsetattr(int fd, int optional_actions, const termios_t *t);
void cfmakeraw(termios_t *t);

int  tcflush(int fd, int queue_selector);
int  tcdrain(int fd);

int  ioctl_console(int fd, int request, void *data);

#endif /* _CONSOLE_H */
