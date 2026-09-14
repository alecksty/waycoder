// VML VGA Text Mode Extension Library
// 写入字符到 VGA 文本显存 0xB8000，管理光标位置
// BASIC 编译器默认链接此库，其他语言按需链接

// 内存地址常量
// 0x6FF0: SCREEN 模式 (0=文本, >0=图形)
// 0x6FF4: 光标行 (0-24)
// 0x6FF8: 光标列 (0-79)
// 0xB8000: VGA 文本帧缓冲基址 (PC标准)

__stdcall void vga_text_putchar(int c) {
    char* mode_ptr = (char*)0x6FF0;
    char* row_ptr = (char*)0x6FF4;
    char* col_ptr = (char*)0x6FF8;
    char* attr_ptr = (char*)0x6FFC;

    // 仅在文本模式下工作 (mode == 0)
    if (*mode_ptr != 0) return;

    // 换行处理
    if (c == 10) {
        *row_ptr = *row_ptr + 1;
        *col_ptr = 0;
        return;
    }

    // 仅输出可打印字符
    if (c < 32 || c >= 127) return;

    char row = *row_ptr;
    char col = *col_ptr;
    char attr = *attr_ptr;

    // 边界检查
    if (row >= 25) return;
    if (col >= 80) return;

    // 计算 VGA 地址: 0xB8000 + (row * 80 + col) * 2
    int offset = (row * 80 + col) * 2;
    char* vga = (char*)(0xB8000 + offset);

    // 写入字符字节 + 属性字节
    *vga = (char)c;
    vga = vga + 1;
    *vga = attr;

    // 推进光标列，支持列回绕
    col = col + 1;
    if (col >= 80) {
        col = 0;
        row = row + 1;
        *row_ptr = row;
    }
    *col_ptr = col;
}

__stdcall void vga_text_newline(void) {
    char* mode_ptr = (char*)0x6FF0;
    char* row_ptr = (char*)0x6FF4;
    char* col_ptr = (char*)0x6FF8;

    if (*mode_ptr != 0) return;

    *row_ptr = *row_ptr + 1;
    *col_ptr = 0;
}
