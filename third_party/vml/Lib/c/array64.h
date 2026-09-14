/* array64.h - 64-bit Integer Array Operations */

#ifndef _ARRAY64_H
#define _ARRAY64_H

#param lib("array64")

long llen64(long* arr);
long lget64(long* arr, long index);
void lset64(long* arr, long index, long value);
void lsort_bubble64(long* arr);
long lindexof64(long* arr, long value);
long llast_indexof64(long* arr, long value);
long lcontains64(long* arr, long value);
void lreverse64(long* arr);
void lfill64(long* arr, long value);
void lcopy64(long* src, long* dst);
void lslice64(long* src, long start, long count, long* dst);
void lconcat64(long* a, long* b, long* dst);
void lpush64(long* arr, long value);
long lpop64(long* arr);
long lshift64(long* arr);
void lunshift64(long* arr, long value);
void linsert64(long* arr, long index, long value);
long lremove64(long* arr, long index);
long lmin_val64(long* arr);
long lmax_val64(long* arr);
long lsum_val64(long* arr);
long lstartswith64(long* a, long* b);
long lendswith64(long* a, long* b);

#endif /* _ARRAY64_H */
