#include "vmlib.h"

// ============================================
// 新的统一设备接口实现（简化版，使用VML asm语法）
// ============================================

// 设备操作函数
int dev_open(const char *name)
{
    // 将参数name的地址加载到R0，然后调用SYSCALL 100
    // 注意：这里假设name参数在栈帧中的标准位置
    asm("LOAD R0, [R12+12]");  // 第一个参数在R12+12
    asm("SYSCALL #100");
    // 返回值在R0中，但我们需要将其保存
    // 由于asm不支持返回值，我们需要编译器支持
    return 0; // 临时返回值
}

int dev_close(int handle)
{
    asm("LOAD R0, [R12+12]");  // handle参数
    asm("SYSCALL #101");
    return 0;
}

int dev_read(int handle, void *buffer, int offset, int count)
{
    // 参数：handle(R12+12), buffer(R12+16), offset(R12+20), count(R12+24)
    asm("LOAD R0, [R12+12]");  // handle
    asm("LOAD R1, [R12+16]");  // buffer
    asm("LOAD R2, [R12+24]");  // count (跳过offset，因为系统调用可能不需要)
    asm("SYSCALL #102");
    return 0;
}

int dev_write(int handle, const void *buffer, int offset, int count)
{
    asm("LOAD R0, [R12+12]");  // handle
    asm("LOAD R1, [R12+16]");  // buffer
    asm("LOAD R2, [R12+24]");  // count
    asm("SYSCALL #103");
    return 0;
}

int dev_control(int handle, int command, const void *data, int length)
{
    asm("LOAD R0, [R12+12]");  // handle
    asm("LOAD R1, [R12+16]");  // command
    asm("LOAD R2, [R12+20]");  // data
    asm("LOAD R3, [R12+24]");  // length
    asm("SYSCALL #104");
    return 0;
}

// ============================================
// 便捷函数实现
// ============================================

int dev_console_puts(const char *str)
{
    int console = dev_open("console");
    if (console < 0) return -1;
    
    int len = 0;
    while (str[len] != '\0') len++;
    
    int result = dev_write(console, str, 0, len);
    dev_close(console);
    return result;
}

int dev_console_getchar()
{
    int console = dev_open("console");
    if (console < 0) return -1;
    
    char ch;
    int result = dev_read(console, &ch, 0, 1);
    dev_close(console);
    
    if (result == 1) return ch;
    return -1;
}

int dev_vga_draw_char(int x, int y, char c, int color)
{
    int vga = dev_open("vga");
    if (vga < 0) return -1;
    
    unsigned char data[4] = {x, y, c, color};
    int result = dev_control(vga, VGA_CMD_DRAW_CHAR, data, 4);
    dev_close(vga);
    return result;
}

int dev_vga_draw_string(int x, int y, const char *str, int color)
{
    int vga = dev_open("vga");
    if (vga < 0) return -1;
    
    // 准备数据：x, y, color, string...
    int len = 0;
    while (str[len] != '\0') len++;
    
    // 使用栈分配内存，避免malloc
    unsigned char data[256]; // 最大256字节
    if (len > 253) len = 253; // 限制长度
    
    data[0] = x;
    data[1] = y;
    data[2] = color;
    for (int i = 0; i < len; i++) {
        data[3 + i] = str[i];
    }
    
    int result = dev_control(vga, VGA_CMD_DRAW_STRING, data, 3 + len);
    dev_close(vga);
    return result;
}

int dev_keyboard_check()
{
    int keyboard = dev_open("kbd");
    if (keyboard < 0) return -1;
    
    unsigned char status;
    int result = dev_control(keyboard, KEYBOARD_CMD_CHECK_KEY, &status, 1);
    dev_close(keyboard);
    
    if (result == 0) return status;
    return -1;
}

int dev_mouse_get_position(int *x, int *y)
{
    int mouse = dev_open("mouse");
    if (mouse < 0) return -1;
    
    unsigned char data[10];
    int result = dev_control(mouse, MOUSE_CMD_GET_STATUS, data, 10);
    dev_close(mouse);
    
    if (result == 0) {
        *x = *(int *)&data[0];
        *y = *(int *)&data[4];
        return 0;
    }
    return -1;
}

int dev_rtc_get_time_string(char *buffer, int buffer_size)
{
    int rtc = dev_open("rtc");
    if (rtc < 0) return -1;
    
    int result = dev_control(rtc, RTC_CMD_GET_TIME_STRING, buffer, buffer_size);
    dev_close(rtc);
    return result;
}