// out.m —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
// 写法照 corpus/m/skel.* —— 共享库同时提供 println_str / println_int。
int main() {
    printf("OUT-STR=abc
");
    printf("OUT-INT=%d
", 42);
    printf("OUT-PUN=hello, world
");
    return 0;
}
