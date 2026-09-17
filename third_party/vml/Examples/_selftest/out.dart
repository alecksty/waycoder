// out.dart —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
// 写法照 corpus/dart/skel.* —— 共享库同时提供 println_str / println_int。
external void println_str(String s);
external void println_int(int v);
void main() {
  println_str("OUT-STR=abc");
  print("OUT-INT=", "");
  println_int(42);
  println_str("OUT-PUN=hello, world");
}
