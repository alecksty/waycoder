/* math64.h - 64-bit Integer Mathematics
 * VML Extended Math Library for Long (Int64) Operations
 * Uses L0-L7 registers (R24-R31) for 64-bit values
 */

#ifndef _MATH64_H
#define _MATH64_H

#param lib("math64")

/* 基础 64位整数函数 */
long labs64(long x);
long lmin64(long a, long b);
long lmax64(long a, long b);
long lclamp64(long x, long low, long high);
long lsign64(long x);

/* 64位整数算术 */
long lgcd64(long a, long b);
long llcm64(long a, long b);
long lisqrt64(long n);
long lpow64(long base, long exp);
long lfactorial64(long n);

/* 64位除法工具 */
long lceil_div64(long a, long b);
long lround_div64(long a, long b);

/* 64位数论 */
long lis_prime64(long n);

/* 64位随机数 */
long lrandom64(long min, long max);

/* 64位位运算 */
long lclz64(long x);
long lctz64(long x);
long lpopcnt64(long x);

/* 64位插值与映射 */
long llerp64(long a, long b, long t);
long lmap_range64(long x, long in_min, long in_max, long out_min, long out_max);

/* 补充 64位函数 */
long lpow10_64(long n);
long lhypot64(long a, long b);
long lilog2_64(long x);
long lis_even64(long x);
long lis_odd64(long x);

/* 64位双精度角度三角函数 */
double sin_deg_d(double degrees);
double cos_deg_d(double degrees);
double tan_deg_d(double degrees);

#endif /* _MATH64_H */
