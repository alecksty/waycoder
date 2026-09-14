#param lib("builtins")
#param lib("fixed")

// VML Shared Math Library
// 编译: vmltool compile math.c -o ../math.vml --lang c

// abs(x) -> 绝对值
__stdcall int abs(int x) {
    return x < 0 ? -x : x;
}

// min(a, b) -> 最小值
__stdcall int min(int a, int b) {
    return a < b ? a : b;
}

// max(a, b) -> 最大值
__stdcall int max(int a, int b) {
    return a > b ? a : b;
}

// clamp(x, low, high) -> 限制范围
__stdcall int clamp(int x, int low, int high) {
    if (x < low) return low;
    if (x > high) return high;
    return x;
}

// is_even(x) -> 是否为偶数
__stdcall int is_even(int x) {
    return (x & 1) == 0;
}

// is_odd(x) -> 是否为奇数
__stdcall int is_odd(int x) {
    return (x & 1) == 1;
}

// sign(x) -> 返回符号(-1, 0, 1)
__stdcall int sign(int x) {
    if (x > 0) return 1;
    if (x < 0) return -1;
    return 0;
}

// pow10(n) -> 10的n次方
__stdcall int pow10(int n) {
    int r = 1;
    for (int i = 0; i < n; i++) r *= 10;
    return r;
}

__stdcall void randomize(int seed) {
    asm("SYSCALL #51");
}

// ===== 浮点三角函数 (double) =====
// Use const instead of #define to avoid preprocessor macro expansion issues
static const double PI      = 3.141592653589793;
static const double PI2     = 6.283185307179586;
static const double PI_HALF = 1.570796326794896;

// Forward declarations
static double _sin_taylor(double x);
static double _cos_taylor(double x);
static double atan_approx(double x);
static double _log_taylor(double x);
__stdcall double atan2(double y, double x);

static double _sin_taylor(double x)
{
    double x2 = x * x;
    double term = x;
    double sum = x;
    int n;
    for (n = 1; n < 12; n++)
    {
        term *= -x2 / (double)((2 * n) * (2 * n + 1));
        sum += term;
        if (term < 1e-10 && term > -1e-10) break;
    }
    return sum;
}

static double _cos_taylor(double x)
{
    double x2 = x * x;
    double term = 1.0;
    double sum = 1.0;
    int n;
    for (n = 1; n < 12; n++)
    {
        term *= -x2 / (double)((2 * n - 1) * (2 * n));
        sum += term;
        if (term < 1e-10 && term > -1e-10) break;
    }
    return sum;
}

static double atan_approx(double x)
{
    double x2 = x * x;
    double term = x;
    double sum = x;
    int n;
    for (n = 1; n < 15; n++)
    {
        term *= -x2 * (double)(2 * n - 1) / (double)(2 * n + 1);
        sum += term;
        if (term < 1e-10 && term > -1e-10) break;
    }
    return sum;
}

__stdcall double sin(double x)
{
    double sign = 1.0;
    if (x < 0) { x = -x; sign = -1.0; }
    x = x - (double)(int)(x / PI2) * PI2;
    if (x > PI) { x -= PI; sign = -sign; }
    if (x > PI_HALF) x = PI - x;
    return sign * _sin_taylor(x);
}

__stdcall double cos(double x)
{
    if (x < 0) x = -x;
    x = x - (double)(int)(x / PI2) * PI2;
    if (x > PI) x = PI2 - x;
    if (x > PI_HALF) x = PI - x;
    return _cos_taylor(x);
}

__stdcall double tan(double x)
{
    double s = sin(x);
    double c = cos(x);
    if (c < 1e-15 && c > -1e-15) return 0.0;
    return s / c;
}

__stdcall double sqrt(double x)
{
    double guess, prev;
    if (x < 0.0) return 0.0;
    if (x == 0.0) return 0.0;
    guess = x * 0.5 + 0.5;
    do {
        prev = guess;
        guess = (guess + x / guess) * 0.5;
    } while (guess < prev - 1e-10 || guess > prev + 1e-10);
    return guess;
}

__stdcall double asin(double x)
{
    if (x < -1.0 || x > 1.0) return 0.0;
    return atan2(x, sqrt(1.0 - x * x));
}

__stdcall double acos(double x)
{
    if (x < -1.0 || x > 1.0) return 0.0;
    return atan2(sqrt(1.0 - x * x), x);
}

__stdcall double atan(double x)
{
    return atan2(x, 1.0);
}

__stdcall double atan2(double y, double x)
{
    if (x > 0.0) return atan_approx(y / x);
    if (x < 0.0)
    {
        if (y >= 0.0) return atan_approx(y / x) + PI;
        return atan_approx(y / x) - PI;
    }
    if (y > 0.0) return PI_HALF;
    if (y < 0.0) return -PI_HALF;
    return 0.0;
}

__stdcall double exp(double x)
{
    int i, n;
    double term = 1.0;
    double sum = 1.0;
    if (x < 0.0) return 1.0 / exp(-x);
    n = (int)x;
    x -= (double)n;
    for (i = 1; i < 20; i++)
    {
        term *= x / (double)i;
        sum += term;
        if (term < 1e-12) break;
    }
    for (i = 0; i < n; i++) sum *= 2.718281828459045;
    return sum;
}

__stdcall double log(double x)
{
    int e = 0;
    if (x <= 0.0) return 0.0;
    while (x > 1.5) { x /= 2.718281828459045; e++; }
    while (x < 0.6) { x *= 2.718281828459045; e--; }
    return _log_taylor(x - 1.0) + (double)e;
}

static double _log_taylor(double x)
{
    double term = x;
    double sum = x;
    int n;
    for (n = 2; n < 30; n++)
    {
        term *= -x * (double)(n - 1) / (double)n;
        sum += term;
        if (term < 1e-10 && term > -1e-10) break;
    }
    return sum;
}

__stdcall double pow(double x, double y)
{
    int i;
    int int_y;
    if (y == 0.0) return 1.0;
    if (x == 0.0) return 0.0;
    int_y = (int)y;
    if ((double)int_y == y && int_y >= 0)
    {
        double result = 1.0;
        for (i = 0; i < int_y; i++) result *= x;
        return result;
    }
    return exp(y * log(x));
}

// Integer power: base^exp (positive integer exponent)
__stdcall int ipow(int base, int exp) {
    if (exp <= 0) return 1;
    int result = 1;
    while (exp > 0) {
        result *= base;
        exp--;
    }
    return result;
}

// ===== 高精度浮点三角函数 (360度查表, 1度精度, float) =====
// Pre-computed sin(0°..360°) lookup table — float precision
static const float sin_table[361] = {
    0.000000f, 0.017452f, 0.034899f, 0.052336f, 0.069756f, 0.087156f, 0.104528f, 0.121869f, 0.139173f, 0.156434f,
    0.173648f, 0.190809f, 0.207912f, 0.224951f, 0.241922f, 0.258819f, 0.275637f, 0.292372f, 0.309017f, 0.325568f,
    0.342020f, 0.358368f, 0.374607f, 0.390731f, 0.406737f, 0.422618f, 0.438371f, 0.453990f, 0.469472f, 0.484810f,
    0.500000f, 0.515038f, 0.529919f, 0.544639f, 0.559193f, 0.573576f, 0.587785f, 0.601815f, 0.615661f, 0.629320f,
    0.642788f, 0.656059f, 0.669131f, 0.681998f, 0.694658f, 0.707107f, 0.719340f, 0.731354f, 0.743145f, 0.754710f,
    0.766044f, 0.777146f, 0.788011f, 0.798636f, 0.809017f, 0.819152f, 0.829038f, 0.838671f, 0.848048f, 0.857167f,
    0.866025f, 0.874620f, 0.882948f, 0.891007f, 0.898794f, 0.906308f, 0.913545f, 0.920505f, 0.927184f, 0.933580f,
    0.939693f, 0.945519f, 0.951057f, 0.956305f, 0.961262f, 0.965926f, 0.970296f, 0.974370f, 0.978148f, 0.981627f,
    0.984808f, 0.987688f, 0.990268f, 0.992546f, 0.994522f, 0.996195f, 0.997564f, 0.998630f, 0.999391f, 0.999848f,
    1.000000f, 0.999848f, 0.999391f, 0.998630f, 0.997564f, 0.996195f, 0.994522f, 0.992546f, 0.990268f, 0.987688f,
    0.984808f, 0.981627f, 0.978148f, 0.974370f, 0.970296f, 0.965926f, 0.961262f, 0.956305f, 0.951057f, 0.945519f,
    0.939693f, 0.933580f, 0.927184f, 0.920505f, 0.913545f, 0.906308f, 0.898794f, 0.891007f, 0.882948f, 0.874620f,
    0.866025f, 0.857167f, 0.848048f, 0.838671f, 0.829038f, 0.819152f, 0.809017f, 0.798636f, 0.788011f, 0.777146f,
    0.766044f, 0.754710f, 0.743145f, 0.731354f, 0.719340f, 0.707107f, 0.694658f, 0.681998f, 0.669131f, 0.656059f,
    0.642788f, 0.629320f, 0.615661f, 0.601815f, 0.587785f, 0.573576f, 0.559193f, 0.544639f, 0.529919f, 0.515038f,
    0.500000f, 0.484810f, 0.469472f, 0.453990f, 0.438371f, 0.422618f, 0.406737f, 0.390731f, 0.374607f, 0.358368f,
    0.342020f, 0.325568f, 0.309017f, 0.292372f, 0.275637f, 0.258819f, 0.241922f, 0.224951f, 0.207912f, 0.190809f,
    0.173648f, 0.156434f, 0.139173f, 0.121869f, 0.104528f, 0.087156f, 0.069756f, 0.052336f, 0.034899f, 0.017452f,
    0.000000f, -0.017452f, -0.034899f, -0.052336f, -0.069756f, -0.087156f, -0.104528f, -0.121869f, -0.139173f, -0.156434f,
    -0.173648f, -0.190809f, -0.207912f, -0.224951f, -0.241922f, -0.258819f, -0.275637f, -0.292372f, -0.309017f, -0.325568f,
    -0.342020f, -0.358368f, -0.374607f, -0.390731f, -0.406737f, -0.422618f, -0.438371f, -0.453990f, -0.469472f, -0.484810f,
    -0.500000f, -0.515038f, -0.529919f, -0.544639f, -0.559193f, -0.573576f, -0.587785f, -0.601815f, -0.615661f, -0.629320f,
    -0.642788f, -0.656059f, -0.669131f, -0.681998f, -0.694658f, -0.707107f, -0.719340f, -0.731354f, -0.743145f, -0.754710f,
    -0.766044f, -0.777146f, -0.788011f, -0.798636f, -0.809017f, -0.819152f, -0.829038f, -0.838671f, -0.848048f, -0.857167f,
    -0.866025f, -0.874620f, -0.882948f, -0.891007f, -0.898794f, -0.906308f, -0.913545f, -0.920505f, -0.927184f, -0.933580f,
    -0.939693f, -0.945519f, -0.951057f, -0.956305f, -0.961262f, -0.965926f, -0.970296f, -0.974370f, -0.978148f, -0.981627f,
    -0.984808f, -0.987688f, -0.990268f, -0.992546f, -0.994522f, -0.996195f, -0.997564f, -0.998630f, -0.999391f, -0.999848f,
    -1.000000f, -0.999848f, -0.999391f, -0.998630f, -0.997564f, -0.996195f, -0.994522f, -0.992546f, -0.990268f, -0.987688f,
    -0.984808f, -0.981627f, -0.978148f, -0.974370f, -0.970296f, -0.965926f, -0.961262f, -0.956305f, -0.951057f, -0.945519f,
    -0.939693f, -0.933580f, -0.927184f, -0.920505f, -0.913545f, -0.906308f, -0.898794f, -0.891007f, -0.882948f, -0.874620f,
    -0.866025f, -0.857167f, -0.848048f, -0.838671f, -0.829038f, -0.819152f, -0.809017f, -0.798636f, -0.788011f, -0.777146f,
    -0.766044f, -0.754710f, -0.743145f, -0.731354f, -0.719340f, -0.707107f, -0.694658f, -0.681998f, -0.669131f, -0.656059f,
    -0.642788f, -0.629320f, -0.615661f, -0.601815f, -0.587785f, -0.573576f, -0.559193f, -0.544639f, -0.529919f, -0.515038f,
    -0.500000f, -0.484810f, -0.469472f, -0.453990f, -0.438371f, -0.422618f, -0.406737f, -0.390731f, -0.374607f, -0.358368f,
    -0.342020f, -0.325568f, -0.309017f, -0.292372f, -0.275637f, -0.258819f, -0.241922f, -0.224951f, -0.207912f, -0.190809f,
    -0.173648f, -0.156434f, -0.139173f, -0.121869f, -0.104528f, -0.087156f, -0.069756f, -0.052336f, -0.034899f, -0.017452f,
    0.000000f
};

// sin(degrees): degrees (float), returns sin as float
// Uses 360-entry lookup table with 1-degree precision + linear interpolation
__stdcall float sin_deg(float degrees) {
    // Normalize to 0-359
    int idx = (int)degrees;
    while (idx < 0) idx += 360;
    idx = idx % 360;
    // Linear interpolation between table[idx] and table[idx+1]
    float frac = degrees - (float)(int)degrees;
    if (frac < 0.0f) frac = 0.0f;
    if (frac > 0.999f) frac = 0.999f;
    float v0 = sin_table[idx];
    float v1 = sin_table[idx + 1];
    return v0 + (v1 - v0) * frac;
}

// cos(degrees): cos(x) = sin(x + 90)
__stdcall float cos_deg(float degrees) {
    return sin_deg(degrees + 90.0f);
}

// tan(degrees): tan = sin/cos
__stdcall float tan_deg(float degrees) {
    float s = sin_deg(degrees);
    float c = cos_deg(degrees);
    if (c < 1e-6f && c > -1e-6f) return 0.0f;
    return s / c;
}

// ===== 整数工具函数 =====

// Integer log2 approximation (for positive integers)
__stdcall int ilog2(int x) {
    int result = 0;
    while (x > 1) { x >>= 1; result++; }
    return result;
}

// ===== 通用数学工具 =====

// ceil_div: 向上取整除法 (a + b - 1) / b
__stdcall int ceil_div(int a, int b) {
    if (b == 0) return 0;
    return (a + b - 1) / b;
}

// round_div: 四舍五入除法 (a + b/2) / b
__stdcall int round_div(int a, int b) {
    if (b == 0) return 0;
    if (a >= 0) return (a + b / 2) / b;
    return (a - b / 2) / b;
}

// gcd: 最大公约数 (欧几里得算法)
__stdcall int gcd(int a, int b) {
    int t;
    if (a < 0) a = -a;
    if (b < 0) b = -b;
    while (b != 0) { t = b; b = a % b; a = t; }
    return a;
}

// lcm: 最小公倍数
__stdcall int lcm(int a, int b) {
    if (a == 0 || b == 0) return 0;
    int g = gcd(a, b);
    return (a / g) * b;
}

// is_prime: 判断质数 (简单试除法, 适合32位以内)
__stdcall int is_prime(int n) {
    int i;
    if (n < 2) return 0;
    if (n == 2 || n == 3) return 1;
    if (n % 2 == 0) return 0;
    for (i = 3; i * i <= n; i += 2)
        if (n % i == 0) return 0;
    return 1;
}

// factorial: 阶乘 n! (n <= 12, 超出返回0)
__stdcall int factorial(int n) {
    int result = 1;
    int i;
    if (n < 0) return 0;
    if (n > 12) return 0; // 13! exceeds 32-bit int
    for (i = 2; i <= n; i++) result *= i;
    return result;
}

// random_range: 生成 [min, max] 范围内的伪随机数
// 使用线性同余生成器: X_{n+1} = (1103515245 * X_n + 12345) & 0x7FFFFFFF
__stdcall int random_range(int min, int max) {
    static int seed = 1;
    if (max < min) { int t = min; min = max; max = t; }
    seed = (1103515245 * seed + 12345) & 0x7FFFFFFF;
    return min + (seed % (max - min + 1));
}

// random_set_seed: 设置随机数种子
__stdcall void random_set_seed(int new_seed) {
    // C static variable access: use inline approach
    // This is a stub — actual seed management through data section
    asm("SYSCALL #51"); // randomize via VML syscall
}

// lerp: 线性插值 a + (b - a) * t (定点缩放, t 为 0..1000 表示 0.0..1.0)
__stdcall int lerp(int a, int b, int t) {
    return a + ((b - a) * t) / 1000;
}

// map_range: 将值 x 从 [in_min, in_max] 映射到 [out_min, out_max]
__stdcall int map_range(int x, int in_min, int in_max, int out_min, int out_max) {
    if (in_max == in_min) return out_min;
    return out_min + ((x - in_min) * (out_max - out_min)) / (in_max - in_min);
}

// clamp_int: 限制值在 [low, high] 范围内 (另一命名版本)
__stdcall int clamp_int(int x, int low, int high) {
    if (x < low) return low;
    if (x > high) return high;
    return x;
}

// hypot: sqrt(a*a + b*b) (直角三角形斜边, 整数版)
__stdcall int hypot_int(int a, int b) {
    if (a < 0) a = -a;
    if (b < 0) b = -b;
    int s = a*a + b*b;
    if (s <= 0) return 0;
    int g = s / 2 + 1;
    int i;
    for (i = 0; i < 10; i++) g = (g + s / (g > 0 ? g : 1)) / 2;
    return g;
}

// isqrt: 整数平方根 (Newton法)
__stdcall int isqrt(int n) {
    if (n <= 0) return 0;
    if (n == 1) return 1;
    int g = n / 2;
    if (g < 1) g = 1;
    int i;
    for (i = 0; i < 10; i++) {
        int ng = (g + n / g) / 2;
        if (ng >= g) return g;
        g = ng;
    }
    return g;
}
