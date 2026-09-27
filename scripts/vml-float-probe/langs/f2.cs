// f2.cs —— float/double/int64 运算（分开打印，不用字符串拼接）
class P {
  static void Main() {
    double a = 3.14;
    double b = 2.0;
    long big = 3000000000;
    long add = 1000000000;
    long pos = 4294967296;
    System.Console.WriteLine("F-MUL="); System.Console.WriteLine((int)(a * b * 100));
    System.Console.WriteLine("D-MUL="); System.Console.WriteLine((int)(a * b * 100));
    System.Console.WriteLine("F-NEG="); System.Console.WriteLine((int)(-0.5 * 100));
    System.Console.WriteLine("D-NEG="); System.Console.WriteLine((int)(-0.5 * 100));
    System.Console.WriteLine("L-ADD="); System.Console.WriteLine((int)((big + add) / 1000000000));
    System.Console.WriteLine("L-MUL="); System.Console.WriteLine((int)(((5000000000 / 5) * 2) / 1000000000));
    System.Console.WriteLine("L-NEG="); System.Console.WriteLine((int)((0 - pos) / 1000000000));
  }
}
