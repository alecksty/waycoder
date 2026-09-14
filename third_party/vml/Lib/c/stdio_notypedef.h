/* stdio.h - Standard input/output (no typedef version)
 * ISO C Standard 7.9
 */

#ifndef _STDIO_H
#define _STDIO_H

// 默认最小集已包含 printf/putchar/getchar

// 不使用typedef，直接使用基本类型
// size_t 已经在vmlib.h中定义

// 可变参数支持 - 使用宏而不是typedef
#define va_list char*
#define va_start(ap, param) ((ap) = (va_list)&(param) + sizeof(param))
#define va_arg(ap, type) (*(type *)((ap) += sizeof(type), (ap) - sizeof(type)))
#define va_end(ap) ((ap) = (va_list)0)

// 标准输入输出函数声明
int printf(const char *format, ...);
int putchar(int c);
int puts(const char *s);
int getchar(void);

// EOF常量
#define EOF (-1)

#endif /* _STDIO_H */