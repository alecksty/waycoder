// f2.java
public class P {
  public static void main(String[] a2) {
    double a = 3.14, b = 2.0;
    long big = 3000000000L, add = 1000000000L, pos = 4294967296L;
    System.out.println("F-MUL=" + (int)(a * b * 100));
    System.out.println("D-MUL=" + (int)(a * b * 100));
    System.out.println("F-NEG=" + (int)(-0.5 * 100));
    System.out.println("D-NEG=" + (int)(-0.5 * 100));
    System.out.println("L-ADD=" + (int)((big + add) / 1000000000L));
    System.out.println("L-MUL=" + (int)(((5000000000L / 5L) * 2L) / 1000000000L));
    System.out.println("L-NEG=" + (int)((0L - pos) / 1000000000L));
  }
}
