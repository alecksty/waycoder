// VML 网络 Socket 扩展函数块 — Ladder (IEC 61131-3)
// OS 模式专用，需显式 INCLUDE net.ld

// Socket 创建
[NET_CREATE]
SYSCALL 330
RET

// Socket 绑定
[NET_BIND]
SYSCALL 331
RET

// Socket 监听
[NET_LISTEN]
SYSCALL 332
RET

// Socket 接受
[NET_ACCEPT]
SYSCALL 333
RET

// Socket 连接
[NET_CONNECT]
SYSCALL 334
RET

// Socket 发送
[NET_SEND]
SYSCALL 335
RET

// Socket 接收
[NET_RECV]
SYSCALL 336
RET

// Socket 关闭
[NET_CLOSE]
SYSCALL 337
RET

// DNS 解析
[DNS_RESOLVE]
SYSCALL 338
RET
