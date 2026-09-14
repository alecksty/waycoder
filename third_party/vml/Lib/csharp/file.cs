// VML 文件操作扩展库 — C#
static class FileIO {
    public static int Fopen(string name, string mode) { asm("SYSCALL 110"); return 0; }
    public static int Fclose(int handle) { asm("SYSCALL 111"); return 0; }
    public static int Fread(int handle, string buf, int count) { asm("SYSCALL 112"); return 0; }
    public static int Fwrite(int handle, string buf, int count) { asm("SYSCALL 113"); return 0; }
    public static int Fseek(int handle, int offset) { asm("SYSCALL 114"); return 0; }
    public static int Ftell(int handle) { asm("SYSCALL 114"); return 0; }
}
