// VML Shared Device Library

// dev_open(name) -> 打开设备
__stdcall int dev_open(const char* name) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #100");
}

// dev_close(handle) -> 关闭设备
__stdcall int dev_close(int handle) {
    asm("SYSCALL #101");
    return 0;
}

// dev_read(handle, buf, offset, count) -> 实际读取字节数
__stdcall int dev_read(int handle, void* buf, int offset, int count) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #102");
}

// dev_write(handle, buf, offset, count) -> 实际写入字节数  
__stdcall int dev_write(int handle, const void* buf, int offset, int count) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #103");
}

// dev_control(handle, cmd, data) -> 结果
__stdcall int dev_control(int handle, int cmd, int data) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #104");
}
