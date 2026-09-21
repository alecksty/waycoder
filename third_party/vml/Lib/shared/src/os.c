// VML OS Mode Shared Library
// OS模式系统调用(300-381)的C封装
// 仅在 --mode os 下可用

// ============================================================
// 线程 (300-304)
// ============================================================

__stdcall int thread_create(void* fn, int stack_size) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #300");
}

__stdcall void thread_exit(void) {
    asm("SYSCALL #301");
}

__stdcall int thread_join(int tid) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #302");
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
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #310");
}

__stdcall int mutex_lock(int id) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #311");
}

__stdcall int mutex_unlock(int id) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #312");
}

// ============================================================
// 条件变量 (313-316)
// ============================================================

__stdcall int cond_create(void) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #313");
}

__stdcall int cond_wait(int cond, int mutex) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #314");
}

__stdcall int cond_signal(int cond) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #315");
}

__stdcall int cond_broadcast(int cond) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #316");
}

// ============================================================
// 进程 (320-322)
// ============================================================

int exec(const char* path) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #320");
}

void process_exit(int code) {
    asm("SYSCALL #321");
}

__stdcall int get_pid(void) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #322");
}

// ============================================================
// 网络 (330-337)
// ============================================================

__stdcall int socket_create(int domain, int type) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #330");
}

__stdcall int socket_bind(int fd, int port) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #331");
}

__stdcall int socket_listen(int fd, int backlog) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #332");
}

__stdcall int socket_accept(int fd) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #333");
}

__stdcall int socket_connect(int fd, const char* addr, int port) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #334");
}

__stdcall int socket_send(int fd, const void* buf, int len) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #335");
}

__stdcall int socket_recv(int fd, void* buf, int len) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #336");
}

__stdcall int socket_close(int fd) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #337");
}

// ============================================================
// 文件系统 (340-344)
// ============================================================

__stdcall int mkdir(const char* path) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #340");
}

__stdcall int remove_file(const char* path) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #341");
}

__stdcall int rename_file(const char* old, const char* new) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #342");
}

int read_dir(const char* path, void* buf) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #343");
}

int file_stat(const char* path, void* buf) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #344");
}

// ============================================================
// 环境变量 (360-362)
// ============================================================

char* get_env(const char* name) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #360");
}

__stdcall void set_env(const char* name, const char* val) {
    asm("SYSCALL #361");
}

int get_args(void* buf) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #362");
}

// ============================================================
// 信号 (350)
// ============================================================

/* ⚠ **`signal` 曾经在这里也有一份**，`SYSCALL #350` —— 已删，理由有三条：
   · `#350` 对**用户程序恒被拒**（`VmlRuntime.Syscall.cs` 的 `case 350` 先过
     `PrivilegeDenied`，返回 `SYSCALL_PERMISSION_DENIED = -102`；即便放行，
     下一句也是 `NOT_SUPPORTED`）⇒ 这一份**永远不可能返回 0**。
   · 而 `Lib/c/signal.h:26` 写的是 **`#param lib("util")`** —— 头文件的**本意**就是
     `util.c` 那份（`return 0`，收下即成功、处理函数永不触发）。
   · 两份同名时**谁赢取决于链接顺序**（`globalLabelMapping` 按模块名建表），
     实测赢的是 `os` ⇒ 老程序的 `signal(SIGINT, h)` 拿到 **-102**，
     而头文件承诺的是 0。**同一份数据两个实现必然漂移**，删掉多的那份。

   判据：`scripts/vml-c-probe/cases/26-signal-shadowed.c`。
   搬回 `util.c` 那份的实现见 `Lib/shared/src/util.c` 的 `signal`。 */


// ============================================================
// 反射 (380-381)
// ============================================================

__stdcall int type_of(void* addr) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #380");
}

__stdcall int type_name(int type_id, char* buf) {
    /* asm 必须是表达式：写成语句 + return 局部变量会把返回值丢掉
       （局部变量是未初始化的垃圾），且不报错。见 vmlui.c 头部。 */
    return asm("SYSCALL #381");
}
