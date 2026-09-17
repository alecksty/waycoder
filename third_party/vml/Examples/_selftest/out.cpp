// out.cpp —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
// 写法照 corpus/cpp/skel.* —— 共享库同时提供 println_str / println_int。
void println_str(char* s);
void println_int(int v);
int main() {
    println_str("OUT-STR=abc");
    print_str("OUT-INT=");
    println_int(42);
    println_str("OUT-PUN=hello, world");
    return 0;
}
