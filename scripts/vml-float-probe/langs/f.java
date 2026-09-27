// f.java
//
// ⚠ 判据格式（与 `f2.*` 一致、也是 `run-langs.sh` 头部写死的那种）：
//   **标签自己占一行**、值另起一行 —— 用 `println` 打标签。
//   此前这里写的是 `print("F-MUL=")`（标签与值同行），于是**值全对**也会判红：
//   「判据写错会把前端的对盖成错」，探针的格式必须与判据一致。
public class P {
  public static void main(String[] a2) {
    double a = 3.14, b = 2.0, h = 0x10, d = -0.5;
    System.out.println("F-MUL="); System.out.println((int)(a * b * 100));
    System.out.println("F-NEG="); System.out.println((int)(d * 100));
    System.out.println("F-HEX="); System.out.println((int)(h * 100));
  }
}
