/* stddef.h - Standard type definitions
 * ISO C Standard 7.1.6
 */

#ifndef _STDDEF_H
#define _STDDEF_H

/* Types */
typedef unsigned int size_t;
typedef int ptrdiff_t;
typedef unsigned int wchar_t;

/* Macros */
#define NULL 0
/* offsetof macro — compile-time offset calculation */
#define offsetof(type, member) ((size_t)&((type *)0)->member)

#endif /* _STDDEF_H */