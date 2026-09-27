// f.cs
class P {
  static void Main() {
    double a = 3.14, b = 2.0, h = 0x10, d = -0.5;
    System.Console.Write("F-MUL="); System.Console.WriteLine((int)(a * b * 100));
    System.Console.Write("F-NEG="); System.Console.WriteLine((int)(d * 100));
    System.Console.Write("F-HEX="); System.Console.WriteLine((int)(h * 100));
  }
}
