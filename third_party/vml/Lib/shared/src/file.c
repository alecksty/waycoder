// VML Shared File Library

__stdcall int fopen(const char* path, int mode) {
    int h;
    asm("SYSCALL #110");
    return h;
}

__stdcall int fclose(int handle) {
    asm("SYSCALL #111");
    return 0;
}

__stdcall int fread(int handle, void* buf, int count) {
    int r;
    asm("SYSCALL #112");
    return r;
}

__stdcall int fwrite(int handle, const void* buf, int count) {
    int r;
    asm("SYSCALL #113");
    return r;
}

__stdcall int fseek(int handle, int offset, int whence) {
    int r;
    asm("SYSCALL #114");
    return r;
}

__stdcall int ftell(int handle) {
    int r;
    asm("SYSCALL #114");
    return r;
}

__stdcall int fsize(int handle) {
    int r;
    asm("SYSCALL #114");
    return r;
}

__stdcall int ftruncate(int handle, int size) {
    int r;
    asm("SYSCALL #114");
    return r;
}
