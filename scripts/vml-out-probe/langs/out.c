// out.c —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
__stdcall void print_str(char* s);
__stdcall void print_int(int v);
__stdcall void newline(void);

int main() {
    print_str("OUT-STR=abc");
    newline();
    print_int(42);
    newline();
    print_str("OUT-PUN=hello, world");
    newline();
    return 0;
}
