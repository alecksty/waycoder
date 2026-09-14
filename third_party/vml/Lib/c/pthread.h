/* pthread.h — VML MCU 安全子集 (线程不可用) */
#ifndef _PTHREAD_H
#define _PTHREAD_H

/* 空桩: MCU 模式不支持多线程 */
typedef int pthread_mutex_t;
#define PTHREAD_MUTEX_INITIALIZER 0

#endif /* _PTHREAD_H */
