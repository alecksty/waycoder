// f2.java —— float/double/int64 运算（与 f.java 同风格：**分开打印**，不用字符串拼接）
public class P {
  public static void main(String[] a2) {
    double a = 3.14;
    double b = 2.0;
    long big = 3000000000L;
    long add = 1000000000L;
    long pos = 4294967296L;
    System.out.print("F-MUL="); System.out.println((int)(a * b * 100));
    System.out.print("D-MUL="); System.out.println((int)(a * b * 100));
    System.out.print("F-NEG="); System.out.println((int)(-0.5 * 100));
    System.out.print("D-NEG="); System.out.println((int)(-0.5 * 100));
    System.out.print("L-ADD="); System.out.println((int)((big + add) / 1000000000L));
    System.out.print("L-MUL="); System.out.println((int)(((5000000000L / 5L) * 2L) / 1000000000L));
    System.out.print("L-NEG="); System.out.println((int)((0L - pos) / 1000000000L));
  }
}
