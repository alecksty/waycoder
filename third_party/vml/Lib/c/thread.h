/* thread.h - Thread management (OS mode)
 * SYSCALL 300-303
 */

#ifndef _THREAD_H
#define _THREAD_H

#param lib("os")

int thread_create(void *entry, int stack_size);
void thread_exit(void);
int thread_join(int thread_id);
int thread_yield(void);

#endif /* _THREAD_H */
