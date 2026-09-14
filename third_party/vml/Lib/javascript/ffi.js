// VML FFI 动态库调用扩展库 — JavaScript
// OS 模式专用，需显式 require("ffi")

var ffi = {
    dlopen: function(path) {
        asm("SYSCALL #370");
        return 0;
    },
    dlsym: function(handle, name) {
        asm("SYSCALL #371");
        return 0;
    },
    dlclose: function(handle) {
        asm("SYSCALL #372");
        return 0;
    },
    nativeCall: function(funcId, args, count, flags) {
        asm("SYSCALL #373");
        return 0;
    },
    nativeCallF: function(funcId, fargs, count, flags) {
        asm("SYSCALL #375");
        return 0.0;
    },
    getPlatform: function() {
        asm("SYSCALL #374");
        return 0;
    }
};
