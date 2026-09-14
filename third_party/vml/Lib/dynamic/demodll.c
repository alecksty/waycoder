// demodll — GenDyn v2 动态库测试

int add(int a, int b) { return a + b; }
int sub(int a, int b) { return a - b; }
int mul(int a, int b) { return a * b; }
int div_op(int a, int b) { return a / b; }

int fact(int n) {
    if (n <= 1) return 1;
    return n * fact(n - 1);
}

int fib(int n) {
    if (n <= 1) return n;
    return fib(n - 1) + fib(n - 2);
}

int negate(int x) { return -x; }

float fadd(float a, float b) { return a + b; }
double dadd(double a, double b) { return a + b; }

const char* greet(const char* name) {
    static char buf[256];
    int i = 0;
    const char* s = "Hello, ";
    while (*s && i < 255) buf[i++] = *s++;
    while (*name && i < 255) buf[i++] = *name++;
    buf[i] = 0;
    return buf;
}
