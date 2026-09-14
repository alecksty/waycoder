/* stdarg.h - Variable arguments
 * VML uses 4-byte words, so va_list is int*
 */

#ifndef _STDARG_H
#define _STDARG_H

typedef int* va_list;

/* va_start: initialize va_list to point after the last fixed parameter */
#define va_start(ap, param) __builtin_va_start(ap, param)

/* va_arg: get next argument of specified type */
#define va_arg(ap, type) __builtin_va_arg(ap, sizeof(type))

/* va_end: cleanup va_list (no-op in VML) */
#define va_end(ap) __builtin_va_end(ap)

/* C99 extension: copy va_list */
#define va_copy(dest, src) __builtin_va_copy(dest, src)

#endif /* _STDARG_H */
