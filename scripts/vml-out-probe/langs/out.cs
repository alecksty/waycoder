// out.cs —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
// 写法照 corpus/cs/skel.* —— 共享库同时提供 println_str / println_int。
class P {
  static void Main() {
    System.Console.WriteLine("OUT-STR=abc");
    System.Console.Write("OUT-INT=");
    System.Console.WriteLine(42);
    System.Console.WriteLine("OUT-PUN=hello, world");
  }
}
