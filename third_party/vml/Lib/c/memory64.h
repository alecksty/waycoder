/* memory64.h - 64-bit Memory Operations */

#ifndef _MEMORY64_H
#define _MEMORY64_H

#param lib("memory64")

void* lmemcpy(void* dst, const void* src, long n);
void* lmemset(void* ptr, int val, long n);
void* lmemmove(void* dst, const void* src, long n);
int lmemcmp(const void* a, const void* b, long n);

#endif /* _MEMORY64_H */
