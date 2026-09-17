// out.java —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
// 写法照 corpus/java/skel.* —— 共享库同时提供 println_str / println_int。
public class P {
  public static void main(String[] a) {
    System.out.println("OUT-STR=abc");
    System.out.print("OUT-INT=");
    System.out.println(42);
    System.out.println("OUT-PUN=hello, world");
  }
}
