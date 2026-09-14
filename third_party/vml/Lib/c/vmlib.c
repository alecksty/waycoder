#include "vmlib.h"

// VGA内存地址
#define VGA_BASE 0xB8000
#define VGA_WIDTH 80
#define VGA_HEIGHT 25

// 显示屏接口
void vga_clear()
{
    int i;
    char *vga = (char *)VGA_BASE;
    for (i = 0; i < VGA_WIDTH * VGA_HEIGHT * 2; i += 2)
    {
        vga[i] = ' ';
        vga[i + 1] = 0x07; // 黑底白字
    }
}

void vga_putchar(int x, int y, char c, int color)
{
    if (x >= 0 && x < VGA_WIDTH && y >= 0 && y < VGA_HEIGHT)
    {
        char *vga = (char *)VGA_BASE;
        int offset = (y * VGA_WIDTH + x) * 2;
        vga[offset] = c;
        vga[offset + 1] = color;
    }
}

void vga_puts(int x, int y, const char *str, int color)
{
    int i = 0;
    while (str[i] != '\0')
    {
        vga_putchar(x + i, y, str[i], color);
        i++;
    }
}

// 键盘接口
int kb_hit()
{
    // 这里应该调用虚拟机的键盘状态检查函数
    // 暂时返回0，表示没有按键
    return 0;
}

char kb_getch()
{
    // 这里应该调用虚拟机的键盘读取函数
    // 暂时返回0
    return 0;
}

// 鼠标接口
int mouse_get_x()
{
    // 这里应该调用虚拟机的鼠标X坐标获取函数
    // 暂时返回0
    return 0;
}

int mouse_get_y()
{
    // 这里应该调用虚拟机的鼠标Y坐标获取函数
    // 暂时返回0
    return 0;
}

int mouse_left_button()
{
    // 这里应该调用虚拟机的鼠标左键状态检查函数
    // 暂时返回0，表示未按下
    return 0;
}

int mouse_right_button()
{
    // 这里应该调用虚拟机的鼠标右键状态检查函数
    // 暂时返回0，表示未按下
    return 0;
}

// 标准输入输出函数
int getchar()
{
    asm("SYSCALL #5");
    return 0;
}

int putchar(int c)
{
    asm("LOAD R0, [R12+12]");
    asm("SYSCALL #4");
    return c;
}

int puts(const char *s)
{
    asm("LOAD R0, [R12+12]");
    asm("SYSCALL #1");
    asm("LOAD R0, #10");
    asm("SYSCALL #4");
    return 0;
}

// 声明共享库格式化函数 (链接时解析)
int shared_vsnprintf(char *buf, const char *fmt, const int *args, int nargs);

int printf(const char *format, ...)
{
    va_list ap;
    va_start(ap, format);

    // 统计变参个数 (遍历格式串)
    int nargs = 0;
    const char *pf = format;
    while (*pf) {
        if (*pf == '%') {
            pf++;
            if (*pf == '%' || *pf == 0) { }
            else { nargs++; }
        }
        pf++;
    }

    int args[10];
    int i;
    for (i = 0; i < nargs && i < 10; i++)
        args[i] = va_arg(ap, int);

    va_end(ap);

    // 格式化到栈缓冲区, 输出
    char buf[256];
    int ret = shared_vsnprintf(buf, format, args, nargs);
    puts(buf);
    return ret;
}

int sprintf(char *str, const char *format, ...)
{
    va_list ap;
    va_start(ap, format);

    int nargs = 0;
    const char *pf = format;
    while (*pf) {
        if (*pf == '%') {
            pf++;
            if (*pf == '%' || *pf == 0) { }
            else { nargs++; }
        }
        pf++;
    }

    int args[10];
    int i;
    for (i = 0; i < nargs && i < 10; i++)
        args[i] = va_arg(ap, int);

    va_end(ap);
    return shared_vsnprintf(str, format, args, nargs);
}

int snprintf(char *str, int size, const char *format, ...)
{
    va_list ap;
    va_start(ap, format);

    int nargs = 0;
    const char *pf = format;
    while (*pf) {
        if (*pf == '%') {
            pf++;
            if (*pf == '%' || *pf == 0) { }
            else { nargs++; }
        }
        pf++;
    }

    int args[10];
    int i;
    for (i = 0; i < nargs && i < 10; i++)
        args[i] = va_arg(ap, int);

    va_end(ap);
    return shared_vsnprintf(str, format, args, nargs);
}

// 字符串函数
int strlen(const char *s)
{
    int len = 0;
    while (*s)
    {
        len++;
        s++;
    }
    return len;
}

char *strcpy(char *dest, const char *src)
{
    char *p = dest;
    while (*src)
    {
        *p++ = *src++;
    }
    *p = '\0';
    return dest;
}

int strcmp(const char *s1, const char *s2)
{
    while (*s1 && *s2)
    {
        if (*s1 != *s2)
        {
            return *s1 - *s2;
        }
        s1++;
        s2++;
    }
    return *s1 - *s2;
}
