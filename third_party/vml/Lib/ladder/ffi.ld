// VML FFI 动态库调用扩展函数块 — Ladder (IEC 61131-3)
// OS 模式专用，需显式 INCLUDE ffi.ld

// 打开动态库
[DL_OPEN]
SYSCALL 370
RET

// 查找符号
[DL_SYM]
SYSCALL 371
RET

// 关闭动态库
[DL_CLOSE]
SYSCALL 372
RET

// 原生调用 (返回整数)
[NATIVE_CALL]
SYSCALL 373
RET

// 原生调用 (返回浮点)
[NATIVE_CALL_F]
SYSCALL 375
RET

// 获取平台ID
[GET_PLATFORM]
SYSCALL 374
RET
