// VML OS Mode Shared Library
// OS模式系统调用(300-381)的C封装
// 仅在 --mode os 下可用

// ============================================================
// 线程 (300-304)
// ============================================================

__stdcall int thread_create(void* fn, int stack_size) {
    int id;
    asm("SYSCALL #300");
    return id;
}

__stdcall void thread_exit(void) {
    asm("SYSCALL #301");
}

__stdcall int thread_join(int tid) {
    int r;
    asm("SYSCALL #302");
    return r;
}

__stdcall void thread_yield(void) {
    asm("SYSCALL #303");
}

__stdcall void thread_sleep(int ms) {
    asm("SYSCALL #304");
}

// ============================================================
// 互斥锁 (310-312)
// ============================================================

__stdcall int mutex_create(void) {
    int id;
    asm("SYSCALL #310");
    return id;
}

__stdcall int mutex_lock(int id) {
    int r;
    asm("SYSCALL #311");
    return r;
}

__stdcall int mutex_unlock(int id) {
    int r;
    asm("SYSCALL #312");
    return r;
}

// ============================================================
// 条件变量 (313-316)
// ============================================================

__stdcall int cond_create(void) {
    int id;
    asm("SYSCALL #313");
    return id;
}

__stdcall int cond_wait(int cond, int mutex) {
    int r;
    asm("SYSCALL #314");
    return r;
}

__stdcall int cond_signal(int cond) {
    int r;
    asm("SYSCALL #315");
    return r;
}

__stdcall int cond_broadcast(int cond) {
    int r;
    asm("SYSCALL #316");
    return r;
}

// ============================================================
// 进程 (320-322)
// ============================================================

int exec(const char* path) {
    int pid;
    asm("SYSCALL #320");
    return pid;
}

void process_exit(int code) {
    asm("SYSCALL #321");
}

__stdcall int get_pid(void) {
    int pid;
    asm("SYSCALL #322");
    return pid;
}

// ============================================================
// 网络 (330-337)
// ============================================================

__stdcall int socket_create(int domain, int type) {
    int fd;
    asm("SYSCALL #330");
    return fd;
}

__stdcall int socket_bind(int fd, int port) {
    int r;
    asm("SYSCALL #331");
    return r;
}

__stdcall int socket_listen(int fd, int backlog) {
    int r;
    asm("SYSCALL #332");
    return r;
}

__stdcall int socket_accept(int fd) {
    int client_fd;
    asm("SYSCALL #333");
    return client_fd;
}

__stdcall int socket_connect(int fd, const char* addr, int port) {
    int r;
    asm("SYSCALL #334");
    return r;
}

__stdcall int socket_send(int fd, const void* buf, int len) {
    int sent;
    asm("SYSCALL #335");
    return sent;
}

__stdcall int socket_recv(int fd, void* buf, int len) {
    int received;
    asm("SYSCALL #336");
    return received;
}

__stdcall int socket_close(int fd) {
    int r;
    asm("SYSCALL #337");
    return r;
}

// ============================================================
// 文件系统 (340-344)
// ============================================================

__stdcall int mkdir(const char* path) {
    int r;
    asm("SYSCALL #340");
    return r;
}

__stdcall int remove_file(const char* path) {
    int r;
    asm("SYSCALL #341");
    return r;
}

__stdcall int rename_file(const char* old, const char* new) {
    int r;
    asm("SYSCALL #342");
    return r;
}

int read_dir(const char* path, void* buf) {
    int count;
    asm("SYSCALL #343");
    return count;
}

int file_stat(const char* path, void* buf) {
    int r;
    asm("SYSCALL #344");
    return r;
}

// ============================================================
// 环境变量 (360-362)
// ============================================================

char* get_env(const char* name) {
    char* val;
    asm("SYSCALL #360");
    return val;
}

__stdcall void set_env(const char* name, const char* val) {
    asm("SYSCALL #361");
}

int get_args(void* buf) {
    int count;
    asm("SYSCALL #362");
    return count;
}

// ============================================================
// 信号 (350)
// ============================================================

__stdcall int signal(int signum, void* handler) {
    int r;
    asm("SYSCALL #350");
    return r;
}

// ============================================================
// 反射 (380-381)
// ============================================================

__stdcall int type_of(void* addr) {
    int type_id;
    asm("SYSCALL #380");
    return type_id;
}

__stdcall int type_name(int type_id, char* buf) {
    int r;
    asm("SYSCALL #381");
    return r;
}
