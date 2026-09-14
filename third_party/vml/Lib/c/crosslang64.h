/* crosslang64.h - 64-bit Cross-Language Arithmetic Bridge */

#ifndef _CROSSLANG64_H
#define _CROSSLANG64_H

#param lib("crosslang64")

long ladd64(long a, long b);
long lsub64(long a, long b);
long lmul64(long a, long b);
long ldiv64(long a, long b);
long lmod64(long a, long b);
long lneg64(long x);
long larr_sum64(long* arr);
long larr_avg64(long* arr);
long larr_max64(long* arr);
long larr_min64(long* arr);

#endif /* _CROSSLANG64_H */
