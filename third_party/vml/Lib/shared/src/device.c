// VML Shared Device Library

// dev_open(name) -> 打开设备
__stdcall int dev_open(const char* name) {
    int h;
    asm("SYSCALL #100");
    return h;
}

// dev_close(handle) -> 关闭设备
__stdcall int dev_close(int handle) {
    asm("SYSCALL #101");
    return 0;
}

// dev_read(handle, buf, offset, count) -> 实际读取字节数
__stdcall int dev_read(int handle, void* buf, int offset, int count) {
    int r;
    asm("SYSCALL #102");
    return r;
}

// dev_write(handle, buf, offset, count) -> 实际写入字节数  
__stdcall int dev_write(int handle, const void* buf, int offset, int count) {
    int r;
    asm("SYSCALL #103");
    return r;
}

// dev_control(handle, cmd, data) -> 结果
__stdcall int dev_control(int handle, int cmd, int data) {
    int r;
    asm("SYSCALL #104");
    return r;
}
