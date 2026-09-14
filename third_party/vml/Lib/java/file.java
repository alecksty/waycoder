// VML 文件操作扩展库 — Java
public class FileIO {
    public static int fopen(String name, String mode) { asm("SYSCALL 110"); return 0; }
    public static int fclose(int handle) { asm("SYSCALL 111"); return 0; }
    public static int fread(int handle, String buf, int count) { asm("SYSCALL 112"); return 0; }
    public static int fwrite(int handle, String buf, int count) { asm("SYSCALL 113"); return 0; }
    public static int fseek(int handle, int offset) { asm("SYSCALL 114"); return 0; }
    public static int ftell(int handle) { asm("SYSCALL 114"); return 0; }
}
