/* statistics64.h - 64-bit Statistics */

#ifndef _STATISTICS64_H
#define _STATISTICS64_H

#param lib("statistics64")

long lmean64(long* arr);
long lsum64(long* arr);
long lmin_arr64(long* arr);
long lmax_arr64(long* arr);
long lmedian64(long* arr);
long lrange64(long* arr);
long lcount_gt64(long* arr, long threshold);
long lcount_lt64(long* arr, long threshold);
long lcount_eq64(long* arr, long value);
long lvariance64(long* arr);
long lstd_dev64(long* arr);
long llinreg_slope64(long* x, long* y);
long llinreg_intercept64(long* x, long* y);

#endif /* _STATISTICS64_H */
