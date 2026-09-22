/* stdlib.h - General utilities
 * ISO C Standard 7.10
 */

#ifndef _STDLIB_H
#define _STDLIB_H

#param lib("convert")
#param lib("builtins")

#include <stddef.h>
/* 兼容层（同 stdio.h）：`__attribute__` 这类扩展关键字按空宏抹掉 */
#include <vml_compat.h>

/* Numeric conversion functions - simplified */
int atoi(const char *nptr);
/* long atol(const char *nptr); - 暂不实现 */
/* double atof(const char *nptr); - 暂不实现 */
/* long strtol(const char *nptr, char **endptr, int base); - 暂不支持指针参数 */
/* unsigned long strtoul(const char *nptr, char **endptr, int base); - 暂不支持指针参数 */
/* double strtod(const char *nptr, char **endptr); - 暂不支持指针参数 */

/* Memory allocation functions - simplified (return NULL) */
void *malloc(int size);
void free(void *ptr);
/* void *calloc(size_t nmemb, size_t size); - 暂不支持 */
void *realloc(void *ptr, size_t size);

/* Program control functions - simplified */
void abort(void);
/* void exit(int status); - 简化，暂不实现 */
/* int atexit(void (*func)(void)); - 暂不支持函数指针 */

/* Environment functions - simplified */
char *getenv(const char *name);
/* int system(const char *command); - 暂不实现 */

/* Search and sort utilities - simplified (no function pointers) */
/* void *bsearch(const void *key, const void *base, */
/*               size_t nmemb, size_t size, */
/*               int (*compar)(const void *, const void *)); */
/* void qsort(void *base, size_t nmemb, size_t size, */
/*            int (*compar)(const void *, const void *)); */

/* Integer arithmetic functions */
int abs(int j);
/* long labs(long j); - 暂不支持long */

/* Random number generation —— 二者都在 `Lib/shared/src/convert.c` 里。
   `srand` 收下种子但不施加（本平台随机源由 VM 播种），详见该文件的说明。 */
int rand(void);
void srand(int seed);

/* Time functions */
int get_datetime(void);

/* Constants */
#define EXIT_SUCCESS 0
#define EXIT_FAILURE 1
#define RAND_MAX 32767

/* Memory allocation failure */
#define NULL ((void *)0)

#endif /* _STDLIB_H */