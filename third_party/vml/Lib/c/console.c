/* console.c - POSIX 控制台库实现
 *
 * OS 模式: dev_open("console") + dev_control() → VmConsoleDevice
 * MCU 模式: 每个函数返回默认值
 */

#include "console.h"

/* 前向声明 device 操作 (链接时解析) */
int  dev_open(const char *name);
int  dev_close(int handle);
int  dev_control(int handle, int command, const void *data, int length);

/* 内存复制 (本地实现, 避免依赖 string.h) */
static void _memcpy(char *dst, const char *src, int n)
{
    int i;
    for (i = 0; i < n; i++)
        dst[i] = src[i];
}

/* ttyname 缓冲区 (全局, 避免 static 局部变量) */
char _ttyname_buf[16];

/* ================================================================
 *  isatty — 检查 fd 是否为终端
 * ================================================================ */
int isatty(int fd)
{
    /* 标准 fd 0/1/2 是终端 */
    if (fd == 0 || fd == 1 || fd == 2) {
        int h, result;
        h = dev_open("console");
        if (h < 0) return 1; /* MCU 模式: 总是终端 */
        result = dev_control(h, 9, (void *)0, 0);
        dev_close(h);
        return (result >= 0) ? 1 : 0;
    }
    return 0;
}

/* ================================================================
 *  ttyname — 获取终端名称
 * ================================================================ */
char* ttyname(int fd)
{
    int i;
    for (i = 0; i < 16; i++) _ttyname_buf[i] = 0;

    if (fd == 0 || fd == 1 || fd == 2) {
        int h;
        h = dev_open("console");
        if (h < 0) {
            _ttyname_buf[0] = '/'; _ttyname_buf[1] = 'd'; _ttyname_buf[2] = 'e';
            _ttyname_buf[3] = 'v'; _ttyname_buf[4] = '/'; _ttyname_buf[5] = 'c';
            _ttyname_buf[6] = 'o'; _ttyname_buf[7] = 'n'; _ttyname_buf[8] = 's';
            _ttyname_buf[9] = 'o'; _ttyname_buf[10] = 'l'; _ttyname_buf[11] = 'e';
            return _ttyname_buf;
        }
        dev_control(h, 8, _ttyname_buf, 16);
        dev_close(h);
        return _ttyname_buf;
    }
    return (char *)0;
}

/* ================================================================
 *  tcgetattr — 获取终端属性
 * ================================================================ */
int tcgetattr(int fd, termios_t *t)
{
    int h, result;
    if (!t) return -1;

    h = dev_open("console");
    if (h < 0) {
        /* MCU 模式: 返回默认值 */
        t->c_iflag = ICRNL;
        t->c_oflag = OPOST | ONLCR;
        t->c_cflag = 0;
        t->c_lflag = ECHO | ICANON | ISIG;
        t->c_cc[0] = 0;
        return 0;
    }

    result = dev_control(h, 4, (void *)t, 24);
    dev_close(h);
    return (result >= 0) ? 0 : -1;
}

/* ================================================================
 *  tcsetattr — 设置终端属性
 *  data 布局: [4 字节 action] [24 字节 termios]
 * ================================================================ */
int tcsetattr(int fd, int optional_actions, const termios_t *t)
{
    int h, result;
    char data[28];
    int i;

    if (!t) return -1;

    h = dev_open("console");
    if (h < 0) return 0; /* MCU 模式: no-op */

    /* 编码: action (4 bytes) + termios (24 bytes) */
    data[0] = (char)(optional_actions & 0xFF);
    data[1] = (char)((optional_actions >> 8) & 0xFF);
    data[2] = (char)((optional_actions >> 16) & 0xFF);
    data[3] = (char)((optional_actions >> 24) & 0xFF);

    /* 复制 termios 结构: 4 个 int + 8 个 char */
    _memcpy(data + 4, (char *)t, 24);

    result = dev_control(h, 5, data, 28);
    dev_close(h);
    return (result >= 0) ? 0 : -1;
}

/* ================================================================
 *  cfmakeraw — 将 termios 设置为原始模式
 * ================================================================ */
void cfmakeraw(termios_t *t)
{
    if (!t) return;

    /* 清除所有标志 */
    t->c_iflag = 0;
    t->c_oflag = 0;
    t->c_cflag = 0;
    t->c_lflag = 0;

    /* 原始模式: 无回显, 无规范模式, 无信号处理 */
}

/* ================================================================
 *  tcflush — 刷新终端输入/输出队列
 * ================================================================ */
int tcflush(int fd, int queue_selector)
{
    int h, result;
    int sel;

    h = dev_open("console");
    if (h < 0) return 0; /* MCU 模式: no-op */

    sel = queue_selector;
    result = dev_control(h, 6, (void *)&sel, 4);
    dev_close(h);
    return (result >= 0) ? 0 : -1;
}

/* ================================================================
 *  tcdrain — 等待输出完成
 * ================================================================ */
int tcdrain(int fd)
{
    /* OS 模式下通过 sleep(1ms) 模拟输出排空 */
    int h;
    h = dev_open("console");
    if (h < 0) return 0; /* MCU 模式 */
    dev_close(h);
    /* sleep(1) 会在链接时解析 */
    return 0;
}

/* ================================================================
 *  ioctl_console — 控制台 ioctl (窗口大小等)
 * ================================================================ */
int ioctl_console(int fd, int request, void *data)
{
    int h, result;
    char *buf;
    int val;

    h = dev_open("console");
    if (h < 0) {
        /* MCU 模式: 返回默认 80x25 */
        if (request == 7 && data) {
            buf = (char *)data;
            val = 25;  /* ws_row */
            buf[0] = (char)(val & 0xFF);
            buf[1] = (char)((val >> 8) & 0xFF);
            buf[2] = (char)((val >> 16) & 0xFF);
            buf[3] = (char)((val >> 24) & 0xFF);
            val = 80;  /* ws_col */
            buf[4] = (char)(val & 0xFF);
            buf[5] = (char)((val >> 8) & 0xFF);
            buf[6] = (char)((val >> 16) & 0xFF);
            buf[7] = (char)((val >> 24) & 0xFF);
            return 0;
        }
        return -1;
    }

    result = dev_control(h, request, data, 8);
    dev_close(h);
    return (result >= 0) ? 0 : -1;
}
