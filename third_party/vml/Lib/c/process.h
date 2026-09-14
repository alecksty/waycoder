/* process.h - Process management (OS mode)
 * SYSCALL 320, 322
 */

#ifndef _PROCESS_H
#define _PROCESS_H

#param lib("os")

int exec(const char *path);
int get_pid(void);

#endif /* _PROCESS_H */
