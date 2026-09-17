// out.cpp —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
void print_str(char* s);
void newline(void);

int main() {
    print_str("OUT-STR=abc");
    newline();
    print_int(42);
    newline();
    print_str("OUT-PUN=hello, world");
    newline();
    return 0;
}
