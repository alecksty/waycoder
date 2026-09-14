// VML 网络 Socket 扩展库 — C
// OS 模式专用，需显式 #include "net.h"

#ifndef NET_H
#define NET_H

#param lib("network")

int net_create(int domain, int type);
int net_bind(int fd, int port);
int net_listen(int fd, int backlog);
int net_accept(int fd);
int net_connect(const char* host, int port);
int net_send(int fd, const void* data, int len);
int net_recv(int fd, void* buf, int max_len);
int net_close(int fd);
int dns_resolve(const char* hostname);

#endif // NET_H
