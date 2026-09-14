#param lib("fixed")

// VML Shared Math64 Library — 64-bit Integer Math
// 编译: dotnet run --project VMLTool -- Lib/shared/src/math64.c -o Lib/shared/math64.vml --lang c

// ===== 基础 64位整数函数 =====

// labs64(x) — 64位绝对值
__stdcall long labs64(long x) {
    return x < 0 ? -x : x;
}

// lmin64(a, b) — 64位最小值
__stdcall long lmin64(long a, long b) {
    return a < b ? a : b;
}

// lmax64(a, b) — 64位最大值
__stdcall long lmax64(long a, long b) {
    return a > b ? a : b;
}

// lclamp64(x, low, high) — 64位范围限制
__stdcall long lclamp64(long x, long low, long high) {
    if (x < low) return low;
    if (x > high) return high;
    return x;
}

// lsign64(x) — 返回符号(-1, 0, 1)
__stdcall long lsign64(long x) {
    if (x > 0) return 1;
    if (x < 0) return -1;
    return 0;
}

// ===== 64位整数算术 =====

// lgcd64(a, b) — 64位最大公约数 (欧几里得算法)
__stdcall long lgcd64(long a, long b) {
    long t;
    if (a < 0) a = -a;
    if (b < 0) b = -b;
    while (b != 0) { t = b; b = a % b; a = t; }
    return a;
}

// llcm64(a, b) — 64位最小公倍数
__stdcall long llcm64(long a, long b) {
    if (a == 0 || b == 0) return 0;
    long g = lgcd64(a, b);
    return (a / g) * b;
}

// lisqrt64(n) — 64位整数平方根 (Newton法)
__stdcall long lisqrt64(long n) {
    if (n <= 0) return 0;
    if (n == 1) return 1;
    long g = n / 2;
    if (g < 1) g = 1;
    long i;
    for (i = 0; i < 20; i++) {
        long ng = (g + n / g) / 2;
        if (ng >= g) return g;
        g = ng;
    }
    return g;
}

// lpow64(base, exp) — 64位整数幂 (正整数指数)
__stdcall long lpow64(long base, long exp) {
    long result = 1;
    if (exp < 0) return 0; // 负指数不支持整数
    while (exp > 0) {
        if (exp & 1) result *= base;
        base *= base;
        exp >>= 1;
    }
    return result;
}

// lfactorial64(n) — 64位阶乘 n! (n <= 20)
__stdcall long lfactorial64(long n) {
    long result = 1;
    long i;
    if (n < 0) return 0;
    if (n > 20) return 0; // 21! exceeds 64-bit
    for (i = 2; i <= n; i++) result *= i;
    return result;
}

// ===== 64位除法工具 =====

// lceil_div64(a, b) — 64位向上取整除法
__stdcall long lceil_div64(long a, long b) {
    if (b == 0) return 0;
    return (a + b - 1) / b;
}

// lround_div64(a, b) — 64位四舍五入除法
__stdcall long lround_div64(long a, long b) {
    if (b == 0) return 0;
    if (a >= 0) return (a + b / 2) / b;
    return (a - b / 2) / b;
}

// ===== 64位数论 =====

// lis_prime64(n) — 判断质数 (试除法, 64位范围)
__stdcall long lis_prime64(long n) {
    long i;
    if (n < 2) return 0;
    if (n == 2 || n == 3) return 1;
    if (n % 2 == 0) return 0;
    long limit = lisqrt64(n);
    for (i = 3; i <= limit; i += 2)
        if (n % i == 0) return 0;
    return 1;
}

// ===== 64位随机数 =====

// lrandom64(min, max) — 生成 [min, max] 范围内的64位伪随机数
// 使用64位线性同余生成器
__stdcall long lrandom64(long min, long max) {
    static long seed = 1;
    if (max < min) { long t = min; min = max; max = t; }
    seed = (6364136223846793005L * seed + 1442695040888963407L) & 0x7FFFFFFFFFFFFFFFL;
    return min + (seed % (max - min + 1));
}

// ===== 64位位运算 =====

// lclz64(x) — Count Leading Zeros
__stdcall long lclz64(long x) {
    if (x == 0) return 64;
    long count = 0;
    long mask = 1L << 63;
    while ((x & mask) == 0 && mask != 0) {
        count++;
        mask >>= 1;  // unsigned shift: use >>>
        // VML C compiler uses arithmetic shift for >>
        // Use explicit mask: (unsigned long)mask >> 1
        mask = (long)((unsigned long)mask >> 1);
    }
    return count;
}

// lctz64(x) — Count Trailing Zeros
__stdcall long lctz64(long x) {
    if (x == 0) return 64;
    long count = 0;
    while ((x & 1) == 0) {
        count++;
        x >>= 1;  // >>>
        x = (long)((unsigned long)x);
    }
    return count;
}

// lpopcnt64(x) — Population Count (1-bits)
__stdcall long lpopcnt64(long x) {
    long count = 0;
    unsigned long ux = (unsigned long)x;
    while (ux != 0) {
        count += (long)(ux & 1);
        ux >>= 1;
    }
    return count;
}

// ===== 64位插值与映射 =====

// llerp64(a, b, t) — 64位线性插值 (t 为 0..1000 表示 0.0..1.0)
__stdcall long llerp64(long a, long b, long t) {
    return a + ((b - a) * t) / 1000;
}

// lmap_range64(x, in_min, in_max, out_min, out_max) — 值域映射
__stdcall long lmap_range64(long x, long in_min, long in_max, long out_min, long out_max) {
    if (in_max == in_min) return out_min;
    return out_min + ((x - in_min) * (out_max - out_min)) / (in_max - in_min);
}

// ===== 补充 64位函数 =====

// lpow10_64(n) — 10的n次方 (64位)
__stdcall long lpow10_64(long n) {
    long r = 1;
    long i;
    if (n < 0) return 0;
    if (n > 18) return 0; // 10^19 exceeds 64-bit
    for (i = 0; i < n; i++) r *= 10;
    return r;
}

// lhypot64(a, b) — sqrt(a*a + b*b) 64位整数版
__stdcall long lhypot64(long a, long b) {
    if (a < 0) a = -a;
    if (b < 0) b = -b;
    // Use larger of two for scaling to avoid overflow in intermediate a*a
    if (a == 0 && b == 0) return 0;
    if (a > b) {
        long ratio = b * 1000 / (a > 0 ? a : 1);
        return lisqrt64(a * a + (b * b > 0 ? b * b : 0));
    }
    return lisqrt64(a * a + b * b);
}

// lilog2_64(x) — 64位整数 log2 近似
__stdcall long lilog2_64(long x) {
    long result = 0;
    if (x <= 0) return -1;
    while (x > 1) { x >>= 1; result++; }
    return result;
}

// lis_even64(x) — 64位偶数检测
__stdcall long lis_even64(long x) {
    return (x & 1) == 0;
}

// lis_odd64(x) — 64位奇数检测
__stdcall long lis_odd64(long x) {
    return (x & 1) == 1;
}

// ===== 64位双精度角度三角函数 =====

static const double PI64_D = 3.14159265358979323846;
static const double PI2_D  = 6.28318530717958647692;
static const double PI_HALF_D = 1.57079632679489661923;

static double _sin_taylor64(double x) {
    double x2 = x * x;
    double term = x;
    double sum = x;
    int n;
    for (n = 1; n < 15; n++) {
        term *= -x2 / (double)((2 * n) * (2 * n + 1));
        sum += term;
        if (term < 1e-15 && term > -1e-15) break;
    }
    return sum;
}

static double _cos_taylor64(double x) {
    double x2 = x * x;
    double term = 1.0;
    double sum = 1.0;
    int n;
    for (n = 1; n < 15; n++) {
        term *= -x2 / (double)((2 * n - 1) * (2 * n));
        sum += term;
        if (term < 1e-15 && term > -1e-15) break;
    }
    return sum;
}

// sin_deg_d(degrees) — 双精度度数正弦 (Taylor级数)
__stdcall double sin_deg_d(double degrees) {
    double sign = 1.0;
    double rad = degrees * PI64_D / 180.0;
    if (rad < 0) { rad = -rad; sign = -1.0; }
    rad = rad - (double)(long)(rad / PI2_D) * PI2_D;
    if (rad > PI64_D) { rad -= PI64_D; sign = -sign; }
    if (rad > PI_HALF_D) rad = PI64_D - rad;
    return sign * _sin_taylor64(rad);
}

// cos_deg_d(degrees) — 双精度度数余弦
__stdcall double cos_deg_d(double degrees) {
    double rad = degrees * PI64_D / 180.0;
    if (rad < 0) rad = -rad;
    rad = rad - (double)(long)(rad / PI2_D) * PI2_D;
    if (rad > PI64_D) rad = PI2_D - rad;
    if (rad > PI_HALF_D) rad = PI64_D - rad;
    return _cos_taylor64(rad);
}

// tan_deg_d(degrees) — 双精度度数正切
__stdcall double tan_deg_d(double degrees) {
    double s = sin_deg_d(degrees);
    double c = cos_deg_d(degrees);
    if (c < 1e-15 && c > -1e-15) return 0.0;
    return s / c;
}
