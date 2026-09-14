// VML 网络 Socket 扩展库 — C++
// OS 模式专用，需显式 #include "net.hpp"

#ifndef NET_HPP
#define NET_HPP

namespace vml {
namespace net {

inline int create(int domain, int type)              { asm("SYSCALL 330"); return 0; }
inline int bind(int fd, int port)                    { asm("SYSCALL 331"); return 0; }
inline int listen(int fd, int backlog)               { asm("SYSCALL 332"); return 0; }
inline int accept(int fd)                            { asm("SYSCALL 333"); return 0; }
inline int connect(const char* host, int port)       { asm("SYSCALL 334"); return 0; }
inline int send(int fd, const void* data, int len)   { asm("SYSCALL 335"); return 0; }
inline int recv(int fd, void* buf, int max_len)      { asm("SYSCALL 336"); return 0; }
inline int close(int fd)                             { asm("SYSCALL 337"); return 0; }
inline int dns_resolve(const char* hostname)         { asm("SYSCALL 338"); return 0; }

} // namespace net
} // namespace vml

#endif // NET_HPP
