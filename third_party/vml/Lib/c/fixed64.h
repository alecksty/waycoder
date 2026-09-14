/* fixed64.h - Q31.32 Fixed-Point Math (64-bit) */

#ifndef _FIXED64_H
#define _FIXED64_H

#param lib("fixed64")

long lfixed_mul64(long a, long b);
long lfixed_div64(long a, long b);
long lfixed_from_int64(long value);
long lfixed_to_int64(long value);
long lfixed_to_int_round64(long value);
long lfixed_from_double(double f);
double lfixed_to_double(long value);

#endif /* _FIXED64_H */
