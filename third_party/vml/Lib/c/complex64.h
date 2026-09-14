/* complex64.h - Double-Precision Complex Numbers */

#ifndef _COMPLEX64_H
#define _COMPLEX64_H

#param lib("complex64")

void zadd(double* a, double* b, double* result);
void zsub(double* a, double* b, double* result);
void zmul(double* a, double* b, double* result);
void zdiv(double* a, double* b, double* result);
double zabs(double* z);
double zarg(double* z);
void zconj(double* a, double* result);
void zneg(double* a, double* result);
double zexp_re(double re, double im);
double zexp_im(double re, double im);
void zsqr(double* a, double* result);
void zsqrt(double* z, double* result);

#endif /* _COMPLEX64_H */
