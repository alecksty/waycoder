/* mutex.h - Mutex synchronization (OS mode)
 * SYSCALL 310-312
 */

#ifndef _MUTEX_H
#define _MUTEX_H

#param lib("os")

int mutex_create(void);
int mutex_lock(int mutex_id);
int mutex_unlock(int mutex_id);

#endif /* _MUTEX_H */
