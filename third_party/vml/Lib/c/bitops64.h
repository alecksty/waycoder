/* bitops64.h - 64-bit Bit Operations */

#ifndef _BITOPS64_H
#define _BITOPS64_H

#param lib("bitops64")

/* 基础位运算 */
long lbit_and64(long a, long b);
long lbit_or64(long a, long b);
long lbit_xor64(long a, long b);
long lbit_not64(long a);
long lbit_shl64(long a, long bits);
long lbit_shr64(long a, long bits);
long lrol64(long val, long bits);
long lror64(long val, long bits);
long lbit_set64(long val, long bit);
long lbit_clear64(long val, long bit);
long lbit_toggle64(long val, long bit);
long lbit_test64(long val, long bit);

/* 高级位操作 */
long lcount64(long value);
long lreverse64(long value);
long lrotate_left64(long value, long n);
long lrotate_right64(long value, long n);
long llowest_set64(long value);
long lhighest_set64(long value);
long lmask64(long n);
long lextract64(long value, long start, long len);
long linsert64(long value, long field, long start, long len);
long lis_power_of_two64(long x);
long lnext_power_of_two64(long x);

#endif /* _BITOPS64_H */
