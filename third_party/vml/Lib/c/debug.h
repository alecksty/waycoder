/* debug.h - Debug output functions
 * SYSCALL 70-72
 */

#ifndef _DEBUG_H
#define _DEBUG_H

// debug_print/assert 通过 SYSCALL 实现，无需额外库

void debug_print(const char *str);
void debug_print_int(int n);
void assert(int condition, const char *message);

#endif /* _DEBUG_H */
