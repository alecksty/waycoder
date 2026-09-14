// VML Shared Network Library
// Network operations using SYSCALL 330-337
// Compile: dotnet run --project VMLTool -- Lib/shared/src/network.c -o Lib/shared/network.vml

__stdcall int net_connect(const char* host, int port) {
    int fd;
    int result;

    // Create socket: AF_INET=2, SOCK_STREAM=1
    asm("LOAD R0 #2");
    asm("LOAD R1 #1");
    asm("SYSCALL #330");
    asm("MOVE fd, R0");
    if (fd < 0) return -1;

    // Connect
    asm("MOVE R0, fd");
    asm("MOVE R1, host");
    asm("MOVE R2, port");
    asm("SYSCALL #334");
    asm("MOVE result, R0");
    if (result < 0) {
        asm("MOVE R0, fd");
        asm("SYSCALL #337");
        return -1;
    }
    return fd;
}

__stdcall int net_send(int fd, void* data, int len) {
    int result;
    asm("SYSCALL #335");
    asm("MOVE result, R0");
    return result;
}

__stdcall int net_recv(int fd, void* buf, int max_len) {
    int result;
    asm("SYSCALL #336");
    asm("MOVE result, R0");
    return result;
}

__stdcall void net_close(int fd) {
    asm("SYSCALL #337");
}

__stdcall int net_listen(int port) {
    int fd;
    int result;

    // Create socket: AF_INET=2, SOCK_STREAM=1
    asm("LOAD R0 #2");
    asm("LOAD R1 #1");
    asm("SYSCALL #330");
    asm("MOVE fd, R0");
    if (fd < 0) return -1;

    // Bind
    asm("MOVE R0, fd");
    asm("MOVE R1, port");
    asm("SYSCALL #331");
    asm("MOVE result, R0");
    if (result < 0) {
        asm("MOVE R0, fd");
        asm("SYSCALL #337");
        return -1;
    }

    // Listen: backlog=5
    asm("MOVE R0, fd");
    asm("LOAD R1 #5");
    asm("SYSCALL #332");
    asm("MOVE result, R0");
    if (result < 0) {
        asm("MOVE R0, fd");
        asm("SYSCALL #337");
        return -1;
    }

    return fd;
}
