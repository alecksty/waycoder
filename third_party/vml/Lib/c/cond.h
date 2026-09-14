/* cond.h - Condition variable (OS mode)
 * SYSCALL 313-316
 */

#ifndef _COND_H
#define _COND_H

#param lib("os")

int cond_create(void);
int cond_wait(int cond_id, int mutex_id);
int cond_signal(int cond_id);
int cond_broadcast(int cond_id);

#endif /* _COND_H */
