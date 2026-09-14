/* encoding64.h - 64-bit Encoding Operations */

#ifndef _ENCODING64_H
#define _ENCODING64_H

#param lib("encoding64")

long lutf8_strlen(const char* s);
long lutf8_decode(unsigned char* src, long len);
long lutf8_encode(long codepoint, unsigned char* dst, long max_bytes);

#endif /* _ENCODING64_H */
