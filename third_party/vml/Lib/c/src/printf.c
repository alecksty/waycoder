/* printf / scanf 格式化输入输出 — C 源码实现
 * 使用 __builtin_va_start/va_arg 正确处理变参
 * 编译: dotnet run --project VMLTool -- Lib/c/src/printf.c -o Lib/c/stdio_funcs.vml --no-link
 */

#include "stdarg.h"

/* ---- 前向声明 ---- */
int putchar(int c);
int getchar();
int puts(const char *s);

/* 输出一个字符到缓冲区或终端 */
int _put(int c, char *buf, int *pos)
{
    if (buf) {
        buf[*pos] = (char)c;
        *pos = *pos + 1;
    } else {
        putchar(c);
    }
    return c;
}

/* 输出字符串 */
int _puts(const char *s, char *buf, int *pos)
{
    int count = 0;
    while (*s) {
        _put(*s, buf, pos);
        count++;
        s++;
    }
    return count;
}

/* 输出有符号整数 */
int _put_int(int n, char *buf, int *pos)
{
    int count = 0;
    if (n < 0) {
        _put('-', buf, pos);
        count++;
        n = -n;
    }
    if (n == 0) {
        _put('0', buf, pos);
        count++;
        return count;
    }

    char tmp[12];
    int i = 0;
    while (n > 0) {
        tmp[i] = (char)('0' + (n % 10));
        i++;
        n = n / 10;
    }
    count = count + i;
    while (i > 0) {
        i--;
        _put(tmp[i], buf, pos);
    }
    return count;
}

/* 输出无符号整数 (十进制) */
int _put_uint(unsigned int n, char *buf, int *pos)
{
    int count = 0;
    if (n == 0) {
        _put('0', buf, pos);
        return 1;
    }

    char tmp[12];
    int i = 0;
    while (n > 0) {
        tmp[i] = (char)('0' + (n % 10));
        i++;
        n = n / 10;
    }
    count = i;
    while (i > 0) {
        i--;
        _put(tmp[i], buf, pos);
    }
    return count;
}

/* 输出十六进制 (小写) */
int _put_hex(unsigned int n, char *buf, int *pos)
{
    int count = 0;
    if (n == 0) {
        _put('0', buf, pos);
        return 1;
    }

    char tmp[10];
    int i = 0;
    while (n > 0) {
        int d = n & 0xF;
        if (d < 10)
            tmp[i] = (char)('0' + d);
        else
            tmp[i] = (char)('a' + d - 10);
        i++;
        n = n >> 4;
    }
    count = i;
    while (i > 0) {
        i--;
        _put(tmp[i], buf, pos);
    }
    return count;
}

/* 输出十六进制 (大写) */
int _put_HEX(unsigned int n, char *buf, int *pos)
{
    int count = 0;
    if (n == 0) {
        _put('0', buf, pos);
        return 1;
    }

    char tmp[10];
    int i = 0;
    while (n > 0) {
        int d = n & 0xF;
        if (d < 10)
            tmp[i] = (char)('0' + d);
        else
            tmp[i] = (char)('A' + d - 10);
        i++;
        n = n >> 4;
    }
    count = i;
    while (i > 0) {
        i--;
        _put(tmp[i], buf, pos);
    }
    return count;
}

/* 输出八进制 */
int _put_oct(unsigned int n, char *buf, int *pos)
{
    int count = 0;
    if (n == 0) {
        _put('0', buf, pos);
        return 1;
    }

    char tmp[12];
    int i = 0;
    while (n > 0) {
        tmp[i] = (char)('0' + (n & 0x7));
        i++;
        n = n >> 3;
    }
    count = i;
    while (i > 0) {
        i--;
        _put(tmp[i], buf, pos);
    }
    return count;
}

/* 统计格式串中变参个数 */
static int _count_args(const char *format)
{
    int n = 0;
    const char *p = format;
    while (*p) {
        if (*p == '%') {
            p++;
            if (*p == '%' || *p == 0) { }
            else { n++; }
        }
        p++;
    }
    return n;
}

/* 核心格式化引擎。
 * buf=NULL, size=0 → 输出到终端 (printf)
 * buf!=NULL, size=0 → 写入缓冲区无限制 (sprintf)
 * buf!=NULL, size>0 → 最多写入 size-1 字符 (snprintf) */
int _vformat_buf(const char *format, const int *args, int nargs,
                 char *buf, int size)
{
    int pos = 0;
    int count = 0;
    int arg_idx = 0;
    const char *p = format;
    char *out_buf = buf;
    int out_size = size;
    int val;
    char *sp;
    int sc;

    while (*p) {
        if (*p != '%') {
            count++;
            if (out_size == 0 || pos < out_size - 1) {
                _put(*p, out_buf, &pos);
            }
            p++;
            continue;
        }

        p++; /* 跳过 % */

        if (*p == '%') {
            count++;
            if (out_size == 0 || pos < out_size - 1) {
                _put('%', out_buf, &pos);
            }
            p++;
            continue;
        }

        if (*p == 0) break;

        /* 获取参数 */
        val = 0;
        if (arg_idx < nargs) {
            val = args[arg_idx];
            arg_idx++;
        }

        switch (*p) {
            case 'c':
                count++;
                if (out_size == 0 || pos < out_size - 1)
                    _put(val, out_buf, &pos);
                break;
            case 's':
                if (val != 0) {
                    sp = (char *)val;
                    sc = _puts(sp, out_buf, &pos);
                    count = count + sc;
                } else {
                    count = count + _puts("(null)", out_buf, &pos);
                }
                break;
            case 'd':
            case 'i':
                count = count + _put_int(val, out_buf, &pos);
                break;
            case 'u':
                count = count + _put_uint(val, out_buf, &pos);
                break;
            case 'x':
                count = count + _put_hex(val, out_buf, &pos);
                break;
            case 'X':
                count = count + _put_HEX(val, out_buf, &pos);
                break;
            case 'o':
                count = count + _put_oct(val, out_buf, &pos);
                break;
            case 'p':
                count = count + _puts("0x", out_buf, &pos);
                count = count + _put_hex(val, out_buf, &pos);
                break;
            default:
                count++;
                if (out_size == 0 || pos < out_size - 1)
                    _put('%', out_buf, &pos);
                count++;
                if (out_size == 0 || pos < out_size - 1)
                    _put(*p, out_buf, &pos);
                break;
        }
        p++;
    }

    /* 空终止 */
    if (out_buf) {
        if (out_size > 0) {
            if (pos < out_size)
                out_buf[pos] = 0;
            else
                out_buf[out_size - 1] = 0;
        } else {
            out_buf[pos] = 0;
        }
    }

    return count;
}

/* shared_vsnprintf — 被 vmlib.c 等调用的共享格式化函数 */
int shared_vsnprintf(char *buf, const char *format, const int *args, int nargs)
{
    if (!buf || !format) return -1;
    return _vformat_buf(format, args, nargs, buf, 0);
}

/* ================================================================
 *  printf / sprintf / snprintf
 *  使用标准 va_start/va_arg/va_end 提取变参，然后调用 shared_vsnprintf
 * ================================================================ */

int printf(const char *format, ...)
{
    va_list ap;
    va_start(ap, format);

    int nargs = _count_args(format);
    int args[12];
    int i;
    for (i = 0; i < nargs && i < 12; i++)
        args[i] = va_arg(ap, int);

    va_end(ap);

    char buf[512];
    int ret = shared_vsnprintf(buf, format, args, nargs);
    puts(buf);
    return ret;
}

int sprintf(char *str, const char *format, ...)
{
    va_list ap;
    va_start(ap, format);

    int nargs = _count_args(format);
    int args[12];
    int i;
    for (i = 0; i < nargs && i < 12; i++)
        args[i] = va_arg(ap, int);

    va_end(ap);

    return shared_vsnprintf(str, format, args, nargs);
}

int snprintf(char *str, int size, const char *format, ...)
{
    va_list ap;
    va_start(ap, format);

    int nargs = _count_args(format);
    int args[12];
    int i;
    for (i = 0; i < nargs && i < 12; i++)
        args[i] = va_arg(ap, int);

    va_end(ap);

    return _vformat_buf(format, args, nargs, str, size);
}

/* scanf / sscanf — 已拆分至 Lib/shared/src/scanf.c */
