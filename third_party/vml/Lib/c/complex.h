/* C99 complex.h — VML 复数运算标准库
 * 实现 C99 7.3 Complex arithmetic <complex.h> 子集。
 * 复数存储为连续的 float64 对 (real, imag)。
 */

#ifndef _COMPLEX_H
#define _COMPLEX_H

/* 复数类型 (MCU 模式: 双精度浮点) */
typedef double _Complex[2];

/* 虚数单位 */
#define _Complex_I (__builtin_complex(0.0, 1.0))
#define I _Complex_I

/* 基本运算 */
double creal(_Complex z);      /* 实部 */
double cimag(_Complex z);      /* 虚部 */
_Complex conj(_Complex z);     /* 共轭 */
double cabs(_Complex z);       /* 模 */
double carg(_Complex z);       /* 辐角 */

/* 算术运算 */
_Complex cadd(_Complex a, _Complex b);   /* a + b */
_Complex csub(_Complex a, _Complex b);   /* a - b */
_Complex cmul(_Complex a, _Complex b);   /* a * b */
_Complex cdiv(_Complex a, _Complex b);   /* a / b */

/* 指数和对数 */
_Complex cexp(_Complex z);     /* e^z */
_Complex clog(_Complex z);     /* ln(z) */

/* 幂和根 */
_Complex csqrt(_Complex z);    /* sqrt(z) */

/* 三角函数 */
_Complex csin(_Complex z);
_Complex ccos(_Complex z);

#endif /* _COMPLEX_H */
