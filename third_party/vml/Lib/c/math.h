/* math.h - Mathematics
 * ISO C Standard 7.12
 */

#ifndef _MATH_H
#define _MATH_H

#param lib("math")

/* Trigonometric functions */
double sin(double x);
double cos(double x);
double tan(double x);
double asin(double x);
double acos(double x);
double atan(double x);
double atan2(double y, double x);

/* Hyperbolic functions */
double sinh(double x);
double cosh(double x);
double tanh(double x);

/* Exponential and logarithmic functions */
double exp(double x);
double log(double x);
double log10(double x);

/* Power functions */
double pow(double x, double y);
double sqrt(double x);

/* Nearest integer, absolute value, and remainder functions */
double ceil(double x);
double floor(double x);
double fabs(double x);
double fmod(double x, double y);

/* Decomposition functions */
double frexp(double value, int *exp);
double ldexp(double x, int exp);
double modf(double value, double *iptr);

/* Constants */
#define HUGE_VAL (1.0/0.0)

/* Domain errors (errno = EDOM) */
#define DOMAIN_ERROR 1

/* Range errors (errno = ERANGE) */
#define RANGE_ERROR 2

#endif /* _MATH_H */