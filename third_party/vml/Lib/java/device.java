// VML 设备 I/O + 文件 + 键鼠扩展库 — Java
// 需显式 import device

public class Device {
    // 键盘
    public static int kbHit() {
        asm("SYSCALL #83");
        return 0;
    }

    public static char kbGetch() {
        asm("SYSCALL #84");
        return '\0';
    }

    // 鼠标
    public static int mouseGetX() {
        asm("SYSCALL #85");
        return 0;
    }

    public static int mouseGetY() {
        asm("SYSCALL #86");
        return 0;
    }

    public static int mouseLeftButton() {
        asm("SYSCALL #87");
        return 0;
    }

    public static int mouseRightButton() {
        asm("SYSCALL #88");
        return 0;
    }

    // 统一设备接口
    public static int devOpen(String name) {
        asm("SYSCALL #100");
        return 0;
    }

    public static int devClose(int handle) {
        asm("SYSCALL #101");
        return 0;
    }

    public static int devRead(int handle, String buf, int offset, int count) {
        asm("SYSCALL #102");
        return 0;
    }

    public static int devWrite(int handle, String buf, int offset, int count) {
        asm("SYSCALL #103");
        return 0;
    }

    public static int devControl(int handle, int command, String data, int length) {
        asm("SYSCALL #104");
        return 0;
    }

}
