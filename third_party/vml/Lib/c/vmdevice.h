#param lib("device")

#include "vmlib.h"

// ============================================
// 新的统一设备接口实现
// ============================================

// 设备操作函数
int dev_open(const char *name);
int dev_close(int handle);
int dev_read(int handle, void *buffer, int offset, int count);
int dev_write(int handle, const void *buffer, int offset, int count);
int dev_control(int handle, int command, const void *data, int length);

// ============================================
// 便捷函数实现
// ============================================
void dev_console_putchar(int c);
int dev_console_getchar();
int dev_console_puts(const char *str);

int dev_vga_draw_char(int x, int y, char c, int color);
int dev_vga_draw_string(int x, int y, const char *str, int color);

int dev_keyboard_check();

int dev_mouse_get_position(int *x, int *y);
int dev_rtc_get_time_string(char *buffer, int buffer_size);