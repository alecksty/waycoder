/* vmlinfo.h - VML System Info 共享库接口
 * 所有语言通过此库获取 VML 运行时信息
 * 标准用法: #include "vmlinfo.h"
 */

#ifndef _VMLINFO_H
#define _VMLINFO_H

// 默认最小集已包含 sysinfo (print_str/print_int/exit/random...)

#ifdef __cplusplus
extern "C" {
#endif

/* ---- 字符串输出 ---- */

/* 输出字符串 (R0 = 字符串地址) SYSCALL 1 */
void vml_print_str(const char* s);

/* 输出整数 (R0 = 整数值) SYSCALL 6 */
void vml_print_int(int n);

/* 输出十六进制 (R0 = 整数值) SYSCALL 10 */
void vml_print_hex(int n);

/* 输出字符 (R0 = 字符) SYSCALL 4 */
void vml_putchar(char c);

/* 输出新行 */
void vml_newline(void);

/* ---- 配置信息 ---- */

/* GetConfig(type) -> 返回值 SYSCALL 60 */
int vml_getconfig(int type);

/* ---- 随机数 ---- */

/* Random() -> 随机整数 SYSCALL 50 */
int vml_random(void);

/* ---- 日期时间 ---- */

/* GetDateString() -> 日期字符串地址 SYSCALL 55 */
const char* vml_get_date(void);

/* GetTimeString() -> 时间字符串地址 SYSCALL 56 */
const char* vml_get_time(void);

/* ---- 退出 ---- */

/* Exit(code) SYSCALL 3 */
void vml_exit(int code);

#ifdef __cplusplus
}
#endif

#endif /* _VMLINFO_H */
