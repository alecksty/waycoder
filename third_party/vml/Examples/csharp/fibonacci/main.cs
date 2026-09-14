class Fib {
  static int Calc(int n) {
    if (n <= 1) return n;
    return Calc(n - 1) + Calc(n - 2);
  }
}
