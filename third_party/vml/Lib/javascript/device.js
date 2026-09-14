// VML 设备 I/O + 文件 + 键鼠扩展库 — JavaScript
// 需显式 import device

var device = {
    // 键盘
    kbHit: function() {
        asm("SYSCALL #83");
        return 0;
    },

    kbGetch: function() {
        asm("SYSCALL #84");
        return '';
    },

    // 鼠标
    mouseGetX: function() {
        asm("SYSCALL #85");
        return 0;
    },

    mouseGetY: function() {
        asm("SYSCALL #86");
        return 0;
    },

    mouseLeftButton: function() {
        asm("SYSCALL #87");
        return 0;
    },

    mouseRightButton: function() {
        asm("SYSCALL #88");
        return 0;
    },

    // 统一设备接口
    devOpen: function(name) {
        asm("SYSCALL #100");
        return 0;
    },

    devClose: function(handle) {
        asm("SYSCALL #101");
        return 0;
    },

    devRead: function(handle, buf, offset, count) {
        asm("SYSCALL #102");
        return 0;
    },

    devWrite: function(handle, buf, offset, count) {
        asm("SYSCALL #103");
        return 0;
    },

    devControl: function(handle, command, data, length) {
        asm("SYSCALL #104");
        return 0;
    },

};
