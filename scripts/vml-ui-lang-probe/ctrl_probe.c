// ctrl_probe.c —— `lang_probe.c` 的**对照组**：同号段里一个**没实现**的号（#599）
// 到底会怎样？这决定了 "LANG=0" 是不是一句真话。
//
//   · 若 #599 报错/中止 ⇒ 任何走通的号都是宿主**认领了的** ⇒ #568 返回的 0 是真答案；
//   · 若 #599 静默返回 0 ⇒ "0" 可能只是"没人写 R0"，那 lang_probe 的判据不成立，要换判据。
__stdcall void print_str(char* s);
__stdcall void println_int(int v);
int ui_scr_w(void);

int probe_unknown(void) { return asm("SYSCALL #599"); }

int main() {
    print_str("W=");
    println_int(ui_scr_w());
    print_str("UNKNOWN599=");
    println_int(probe_unknown());
    print_str("W2=");
    println_int(ui_scr_w());
    return 0;
}
