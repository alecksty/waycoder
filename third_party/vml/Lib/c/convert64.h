/* convert64.h - 64-bit Numeric Conversion */

#ifndef _CONVERT64_H
#define _CONVERT64_H

#param lib("convert64")

int ltoa(long value, char* dst);
long atol(const char* s);
int ltoa_hex(long value, char* dst);
long atol_hex(const char* s);
int dtoa(double value, int precision, char* dst);

#endif /* _CONVERT64_H */
