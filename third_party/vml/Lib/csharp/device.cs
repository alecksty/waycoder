// VML 设备 I/O + 文件 + 键鼠扩展库 — C#
// 需显式 using device

static class Device {
    // 键盘
    public static int KbHit() {
        asm("SYSCALL #83");
        return 0;
    }

    public static char KbGetch() {
        asm("SYSCALL #84");
        return '\0';
    }

    // 鼠标
    public static int MouseGetX() {
        asm("SYSCALL #85");
        return 0;
    }

    public static int MouseGetY() {
        asm("SYSCALL #86");
        return 0;
    }

    public static int MouseLeftButton() {
        asm("SYSCALL #87");
        return 0;
    }

    public static int MouseRightButton() {
        asm("SYSCALL #88");
        return 0;
    }

    // 统一设备接口
    public static int DevOpen(string name) {
        asm("SYSCALL #100");
        return 0;
    }

    public static int DevClose(int handle) {
        asm("SYSCALL #101");
        return 0;
    }

    public static int DevRead(int handle, byte[] buf, int offset, int count) {
        asm("SYSCALL #102");
        return 0;
    }

    public static int DevWrite(int handle, byte[] buf, int offset, int count) {
        asm("SYSCALL #103");
        return 0;
    }

    public static int DevControl(int handle, int command, byte[] data, int length) {
        asm("SYSCALL #104");
        return 0;
    }

}
