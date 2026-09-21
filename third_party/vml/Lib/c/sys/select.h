/* sys/select.h —— "等一组 fd 可读"那套。本平台没有真正的 fd 多路复用，
   但老程序（tty-clock 等）拿它做"等按键或超时"，得让它**能编、能跑**。
   语义近似：`select` 一律"超时"，让程序自己进入下一轮。 */
#ifndef _SYS_SELECT_H
#define _SYS_SELECT_H

#param lib("util")

#define FD_SETSIZE 64

typedef struct { int _bits[FD_SETSIZE / 32 + 1]; } fd_set;

#define FD_ZERO(p)      _vml_fd_zero(p)
#define FD_SET(n, p)    _vml_fd_set(n, p)
#define FD_CLR(n, p)    _vml_fd_clr(n, p)
#define FD_ISSET(n, p)  _vml_fd_isset(n, p)

void _vml_fd_zero(void *p);
void _vml_fd_set(int n, void *p);
void _vml_fd_clr(int n, void *p);
int  _vml_fd_isset(int n, void *p);

struct timeval { int tv_sec; int tv_usec; };

int select(int nfds, fd_set *r, fd_set *w, fd_set *e, struct timeval *timeout);
int pselect(int nfds, fd_set *r, fd_set *w, fd_set *e, struct timeval *timeout, void *mask);

#endif /* _SYS_SELECT_H */
