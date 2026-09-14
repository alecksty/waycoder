#include "math.h"
#include <errno.h>

#define PI      3.141592653589793
#define PI2     6.283185307179586
#define PI_HALF 1.570796326794896

int errno = 0;

static double _sin_taylor(double x);
static double _cos_taylor(double x);
static double atan_approx(double x);

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

double sin(double x)
{
    double sign = 1.0;
    if (x < 0) { x = -x; sign = -1.0; }
    x = x - (double)(int)(x / PI2) * PI2;
    if (x > PI) { x -= PI; sign = -sign; }
    if (x > PI_HALF) x = PI - x;
    return sign * _sin_taylor(x);
}

double cos(double x)
{
    if (x < 0) x = -x;
    x = x - (double)(int)(x / PI2) * PI2;
    if (x > PI) x = PI2 - x;
    if (x > PI_HALF) x = PI - x;
    return _cos_taylor(x);
}

double tan(double x)
{
    double s = sin(x);
    double c = cos(x);
    if (c < 1e-15 && c > -1e-15) return 0.0;
    return s / c;
}

double asin(double x)
{
    if (x < -1.0 || x > 1.0) { errno = EDOM; return 0.0; }
    return atan2(x, sqrt(1.0 - x * x));
}

double acos(double x)
{
    if (x < -1.0 || x > 1.0) { errno = EDOM; return 0.0; }
    return atan2(sqrt(1.0 - x * x), x);
}

double atan(double x)
{
    return atan2(x, 1.0);
}

double atan2(double y, double x)
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

double sinh(double x)
{
    double e = exp(x);
    return (e - 1.0 / e) * 0.5;
}

double cosh(double x)
{
    double e = exp(x);
    return (e + 1.0 / e) * 0.5;
}

double tanh(double x)
{
    double e = exp(x);
    double em1 = 1.0 / e;
    return (e - em1) / (e + em1);
}

double exp(double x)
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

double log(double x)
{
    int e = 0;
    if (x <= 0.0) { errno = EDOM; return 0.0; }
    while (x > 1.5) { x /= 2.718281828459045; e++; }
    while (x < 0.6) { x *= 2.718281828459045; e--; }
    return _log_taylor(x - 1.0) + (double)e;
}

double log10(double x)
{
    return log(x) / 2.302585092994046;
}

double pow(double x, double y)
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

double sqrt(double x)
{
    double guess, prev;
    if (x < 0.0) { errno = EDOM; return 0.0; }
    if (x == 0.0) return 0.0;
    guess = x * 0.5 + 0.5;
    do {
        prev = guess;
        guess = (guess + x / guess) * 0.5;
    } while (guess < prev - 1e-10 || guess > prev + 1e-10);
    return guess;
}

double ceil(double x)
{
    int i = (int)x;
    if ((double)i == x) return x;
    return x > 0.0 ? (double)(i + 1) : (double)i;
}

double floor(double x)
{
    int i = (int)x;
    if ((double)i == x) return x;
    return x > 0.0 ? (double)i : (double)(i - 1);
}

double fabs(double x)
{
    return x < 0.0 ? -x : x;
}

double fmod(double x, double y)
{
    if (y == 0.0) { errno = EDOM; return 0.0; }
    return x - (double)(int)(x / y) * y;
}

double frexp(double value, int *exp)
{
    int e = 0;
    if (value == 0.0) { *exp = 0; return 0.0; }
    if (value < 0.0) return -frexp(-value, exp);
    while (value >= 1.0) { value *= 0.5; e++; }
    while (value < 0.5) { value *= 2.0; e--; }
    *exp = e;
    return value;
}

double ldexp(double x, int exp)
{
    int i;
    if (exp > 0) for (i = 0; i < exp; i++) x *= 2.0;
    else for (i = 0; i < -exp; i++) x *= 0.5;
    return x;
}

double modf(double value, double *iptr)
{
    int i = (int)value;
    *iptr = (double)i;
    return value - *iptr;
}
