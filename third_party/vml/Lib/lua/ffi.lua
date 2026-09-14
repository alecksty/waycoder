-- VML FFI 动态库调用扩展库 — Lua
-- OS 模式专用，需显式 require("ffi")

ffi = {}
function ffi.dlopen(path)
    asm("SYSCALL 370")
    return 0
end
function ffi.dlsym(handle, name)
    asm("SYSCALL 371")
    return 0
end
function ffi.dlclose(handle)
    asm("SYSCALL 372")
    return 0
end
function ffi.native_call(func_id, args, count, flags)
    asm("SYSCALL 373")
    return 0
end
function ffi.native_call_f(func_id, fargs, count, flags)
    asm("SYSCALL 375")
    return 0.0
end
function ffi.get_platform()
    asm("SYSCALL 374")
    return 0
end
