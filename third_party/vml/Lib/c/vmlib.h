#ifndef VMLIB_H
#define VMLIB_H

// 默认最小集已包含: io, string, builtins, memory, sysinfo, float
// 以下按需引入额外模块
#param lib("math")
#param lib("ctype")
#param lib("file")
#param lib("graphics")
#param lib("network")
#param lib("os")
#param lib("device")
#param lib("convert")
#param lib("bitops")
#param lib("util")
#param lib("softfloat")
#param lib("softint64")
#param lib("softdouble")
#param lib("readline")
#param lib("vga_text")

// 基本类型定义（避免包含问题）
typedef unsigned int size_t;

// 可变参数支持
typedef char *va_list;
#define va_start(ap, param) ((ap) = (va_list)&(param) + sizeof(param))
#define va_arg(ap, type) (*(type *)((ap) += sizeof(type), (ap) - sizeof(type)))
#define va_end(ap) ((ap) = (va_list)0)

// 标准输入输出函数声明
int printf(const char *format, ...);
int putchar(int c);
int puts(const char *s);
int getchar(void);

// 字符串处理函数
size_t strlen(const char *str);
char *strcpy(char *dest, const char *src);
char *strncpy(char *dest, const char *src, size_t n);
char *strcat(char *dest, const char *src);
char *strncat(char *dest, const char *src, size_t n);
int strcmp(const char *str1, const char *str2);
int strncmp(const char *str1, const char *str2, size_t n);
char *strchr(const char *str, int c);
char *strstr(const char *haystack, const char *needle);

// 内存操作函数
void *memset(void *ptr, int value, size_t num);
void *memcpy(void *dest, const void *src, size_t num);
void *memmove(void *dest, const void *src, size_t num);
int memcmp(const void *ptr1, const void *ptr2, size_t num);

// 字符分类函数
int isalpha(int c);
int isdigit(int c);
int isalnum(int c);
int isspace(int c);
int isupper(int c);
int islower(int c);
int toupper(int c);
int tolower(int c);

// 数学函数
int abs(int n);
long labs(long n);
double fabs(double x);
double sqrt(double x);
double sin(double x);
double cos(double x);
double tan(double x);
double atan2(double y, double x);
double pow(double x, double y);
double exp(double x);
double log(double x);
double log10(double x);

// 工具函数
int atoi(const char *str);
long atol(const char *str);
double atof(const char *str);
void qsort(void *base, size_t num, size_t size, void* compar);
void *bsearch(void *key, void *base, size_t num, size_t size, void* compar);
int rand(void);
void srand(unsigned int seed);

// ============================================
// 传统接口（向后兼容）
// ============================================

// 显示屏接口
void vga_clear();
void vga_putchar(int x, int y, char c, int color);
void vga_puts(int x, int y, const char *str, int color);

// 键盘接口
int kb_hit();
char kb_getch();

// 鼠标接口
int mouse_get_x();
int mouse_get_y();
int mouse_left_button();
int mouse_right_button();

// ============================================
// 新的统一设备接口
// ============================================

// 设备类型定义
#define DEVICE_CONSOLE   0
#define DEVICE_VGA       1
#define DEVICE_KEYBOARD  2
#define DEVICE_MOUSE     3
#define DEVICE_TIMER     4
#define DEVICE_RTC       5
#define DEVICE_FILESYSTEM 6

// 设备操作函数
int dev_open(const char *name);
int dev_close(int handle);
int dev_read(int handle, void *buffer, int offset, int count);
int dev_write(int handle, const void *buffer, int offset, int count);
int dev_control(int handle, int command, const void *data, int length);

// 设备控制命令
// 控制台设备命令
#define CONSOLE_CMD_GET_INFO     0
#define CONSOLE_CMD_CLEAR_SCREEN 1
#define CONSOLE_CMD_SET_CURSOR   2
#define CONSOLE_CMD_GET_CURSOR   3

// VGA设备命令
#define VGA_CMD_GET_INFO         0
#define VGA_CMD_CLEAR_SCREEN     1
#define VGA_CMD_SET_PIXEL        2
#define VGA_CMD_DRAW_CHAR        3
#define VGA_CMD_DRAW_STRING      4
#define VGA_CMD_SET_MODE         5
#define VGA_CMD_GET_MEMORY       6

// 键盘设备命令
#define KEYBOARD_CMD_GET_STATUS  0
#define KEYBOARD_CMD_CHECK_KEY   1
#define KEYBOARD_CMD_CLEAR_BUFFER 2
#define KEYBOARD_CMD_SET_LEDS    3

// 鼠标设备命令
#define MOUSE_CMD_GET_STATUS     0
#define MOUSE_CMD_SET_POSITION   1
#define MOUSE_CMD_SET_BUTTONS    2
#define MOUSE_CMD_SET_WHEEL      3
#define MOUSE_CMD_SIMULATE_MOVE  4
#define MOUSE_CMD_SIMULATE_CLICK 5

// 定时器设备命令
#define TIMER_CMD_GET_STATUS     0
#define TIMER_CMD_START          1
#define TIMER_CMD_STOP           2
#define TIMER_CMD_SET_INTERVAL   3
#define TIMER_CMD_RESET          4
#define TIMER_CMD_CHECK          5

// RTC设备命令
#define RTC_CMD_GET_FULL_INFO    0
#define RTC_CMD_SET_FORMAT       1
#define RTC_CMD_GET_TIMESTAMP    2
#define RTC_CMD_SET_ALARM        3
#define RTC_CMD_CLEAR_ALARM      4
#define RTC_CMD_GET_DATE_STRING  5
#define RTC_CMD_GET_TIME_STRING  6

// 文件系统设备命令
#define FS_CMD_OPEN_FILE         0
#define FS_CMD_CLOSE_FILE        1
#define FS_CMD_READ_FILE         2
#define FS_CMD_WRITE_FILE        3
#define FS_CMD_SEEK_FILE         4
#define FS_CMD_GET_FILE_INFO     5
#define FS_CMD_DELETE_FILE       6
#define FS_CMD_CREATE_DIR        7
#define FS_CMD_DELETE_DIR        8
#define FS_CMD_LIST_DIR          9

// 便捷函数（基于新设备接口）
int dev_console_puts(const char *str);
int dev_console_getchar();

int dev_vga_draw_char(int x, int y, char c, int color);
int dev_vga_draw_string(int x, int y, const char *str, int color);

int dev_keyboard_check();

int dev_mouse_get_position(int *x, int *y);
int dev_rtc_get_time_string(char *buffer, int buffer_size);

// ============================================
// 新增浮点输入输出函数
// ============================================

// 输出浮点数
void putfloat(float value);

// 输入浮点数
float getfloat(void);

// 输出十六进制整数
void puthex(int value);

// 浮点数转字符串
int float_to_str(float value, char *buffer, int buffer_size);

// 字符串转浮点数
float str_to_float(const char *str);

// ============================================
// 新增文件操作函数
// ============================================

// 打开文件
int fopen(const char *filename, const char *mode);

// 关闭文件
int fclose(int handle);

// 读取文件
int fread(int handle, void *buffer, int size);

// 写入文件
int fwrite(int handle, const void *data, int size);

// 设置文件位置
int fseek(int handle, int offset);

// 获取文件位置
int ftell(int handle);

// 获取文件大小
int fsize(int handle);

// 截断文件
int ftruncate(int handle, int size);

#endif // VMLIB_H