// VML FFI 动态库调用扩展库 — C
// OS 模式专用，需显式 #include "ffi.h"

#ifndef FFI_H
#define FFI_H

// FFI 函数通过 SYSCALL 370-375 实现，无需额外库

int dl_open(const char* path);
int dl_sym(int handle, const char* name);
int dl_close(int handle);
int native_call(int func_id, const int* args, int count, int flags);
float native_call_f(int func_id, const float* args, int count, int flags);
int get_platform(void);

#endif // FFI_H
