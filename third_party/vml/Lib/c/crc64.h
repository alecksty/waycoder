/* crc64.h - 64-bit CRC & Hash Functions */

#ifndef _CRC64_H
#define _CRC64_H

#param lib("crc64")

long crc64(const char* data, long len);
long fnv1a_hash64(const char* str);
long djb2_hash64(const char* str);
long sdbm_hash64(const char* str);
int crc32_64l(const char* data, long len);

#endif /* _CRC64_H */
