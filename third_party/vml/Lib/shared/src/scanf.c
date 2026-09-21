#param lib("util")

/* scanf / sscanf — C 源码实现 (cdecl 变参, 仅 C/C++ 自动链接)
 * 使用 __builtin_va_start/va_arg 正确处理变参
 * 编译: dotnet run --project VMLTool -- Lib/shared/src/scanf.c -o Lib/shared/scanf.vml --no-link
 */

#include "stdarg.h"

/* ---- 前向声明 ---- */
int getchar();

/* 统计格式串中变参个数 —— **唯一实现在 `printf.c`**（`format_arg_count`）。
   ⚠ 这里原先自带一份 static 的。两份的后果不是"代码重复"这么轻：数错一个，
   变参表就**整体错位一格**，`scanf("%d %s", &n, buf)` 会把整数当地址写 ——
   而两份判据只要有一处改动没同步就会这样，且**没有任何编译期提示**。 */
int format_arg_count(const char *format);

/* 判断字符是否为空白 */
static int _isspace(int c)
{
    return c == ' ' || c == '\t' || c == '\n' || c == '\r';
}

/* 判断字符是否为数字 */
static int _isdigit(int c)
{
    return c >= '0' && c <= '9';
}

/* 基础字符串扫描 — 从 str 中按 format 解析 */
int vsscanf(char *str, char *format, int *args, int nargs)
{
    if (!str || !format || !args) return -1;

    char *s = str;
    int arg_idx = 0;
    int count = 0;
    char *p = format;
    int val, sign, got;
    char *dst;
    int *dest;
    int n;

    while (*p && *s) {
        if (*p == ' ' || *p == '\t' || *p == '\n' || *p == '\r') {
            while (*s && (*s == ' ' || *s == '\t' || *s == '\n' || *s == '\r'))
                s++;
            p++;
            continue;
        }

        if (*p != '%') {
            if (*p == *s) { p++; s++; }
            else break;
            continue;
        }

        p++;
        if (*p == 0) break;
        if (arg_idx >= nargs) break;

        switch (*p) {
            case 'd':
            case 'i':
                val = 0; sign = 1; got = 0;
                while (*s == ' ' || *s == '\t') s++;
                if (*s == '-') { sign = -1; s++; }
                else if (*s == '+') { s++; }
                if (*s == '0') {
                    s++;
                    if (*s == 'x' || *s == 'X') {
                        s++;
                        while (*s) {
                            if (*s >= '0' && *s <= '9')
                                val = val * 16 + (*s - '0');
                            else if (*s >= 'a' && *s <= 'f')
                                val = val * 16 + (*s - 'a' + 10);
                            else if (*s >= 'A' && *s <= 'F')
                                val = val * 16 + (*s - 'A' + 10);
                            else break;
                            got = 1; s++;
                        }
                    } else {
                        while (*s >= '0' && *s <= '7')
                            { val = val * 8 + (*s - '0'); got = 1; s++; }
                    }
                } else {
                    while (*s >= '0' && *s <= '9')
                        { val = val * 10 + (*s - '0'); got = 1; s++; }
                }
                if (got) { dest = (int *)args[arg_idx]; *dest = sign * val; count++; }
                arg_idx++;
                break;
            case 'x':
            case 'X':
                val = 0; got = 0;
                while (*s == ' ' || *s == '\t') s++;
                while (*s) {
                    if (*s >= '0' && *s <= '9')
                        val = val * 16 + (*s - '0');
                    else if (*s >= 'a' && *s <= 'f')
                        val = val * 16 + (*s - 'a' + 10);
                    else if (*s >= 'A' && *s <= 'F')
                        val = val * 16 + (*s - 'A' + 10);
                    else break;
                    got = 1; s++;
                }
                if (got) { dest = (int *)args[arg_idx]; *dest = val; count++; }
                arg_idx++;
                break;
            case 's':
                dst = (char *)args[arg_idx];
                while (*s == ' ' || *s == '\t') s++;
                n = 0;
                while (*s && *s != ' ' && *s != '\t' && *s != '\n' && n < 255)
                    { *dst = *s; dst++; s++; n++; }
                *dst = 0;
                count++; arg_idx++;
                break;
            case 'c':
                dst = (char *)args[arg_idx];
                *dst = *s; dst++; *dst = 0; s++;
                count++; arg_idx++;
                break;
        }
        p++;
    }

    return count;
}

int scanf(const char *format, ...)
{
    va_list ap;
    va_start(ap, format);

    int nargs = format_arg_count(format);
    int args[12];
    int i;
    for (i = 0; i < nargs && i < 12; i++)
        args[i] = va_arg(ap, int);

    va_end(ap);

    char buf[256];
    int i2 = 0;
    int c;
    while (i2 < 254) {
        c = getchar();
        if (c <= 0 || c == '\n') break;
        buf[i2] = (char)c;
        i2++;
    }
    buf[i2] = 0;

    return vsscanf(buf, format, args, nargs);
}

int sscanf(const char *str, const char *format, ...)
{
    va_list ap;
    va_start(ap, format);

    int nargs = format_arg_count(format);
    int args[12];
    int i;
    for (i = 0; i < nargs && i < 12; i++)
        args[i] = va_arg(ap, int);

    va_end(ap);

    return vsscanf(str, format, args, nargs);
}
