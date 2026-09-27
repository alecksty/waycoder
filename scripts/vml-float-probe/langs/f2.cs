// f2.cs
class P {
  static void Main() {
    double a = 3.14;
    double b = 2.0;
    long big = 3000000000;
    long add = 1000000000;
    long mul = 123456789;
    long pos = 4294967296;
    System.Console.WriteLine("F-MUL=" + (int)(a * b * 100));
    System.Console.WriteLine("D-MUL=" + (int)(a * b * 100));
    System.Console.WriteLine("F-NEG=" + (int)(-0.5 * 100));
    System.Console.WriteLine("D-NEG=" + (int)(-0.5 * 100));
    System.Console.WriteLine("L-ADD=" + (int)((big + add) / 1000000000));
    System.Console.WriteLine("L-MUL=" + (int)((mul * 1000) / 1000000000));
    System.Console.WriteLine("L-NEG=" + (int)((0 - pos) / 1000000000));
  }
}
