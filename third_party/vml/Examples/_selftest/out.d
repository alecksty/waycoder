// out.d —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
// out.d -- VML cross-language "output" probe (exactly three lines, see run-langs.sh)
// 写法照 corpus/d/skel.* —— 共享库同时提供 println_str / println_int。
// Pattern follows corpus/d/skel.* -- the shared library provides both println_str / println_int.
void main() {
    writeln("OUT-STR=abc");
    writeln("OUT-INT=", 42);
    writeln("OUT-PUN=hello, world");
}
