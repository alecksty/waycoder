/*
 * VML syscall 共享库 — 为非 C-like 语言提供类型安全的 SYSCALL 接口
 * 所有语言通过链接此库调用 VML 系统功能
 * 编译: dotnet run --project VMLTool -- Lib/c/vmlsys.c -o vmlsys.vml
 *
 * 设计原则:
 *   1. 每个 SYSCALL 一个 C 函数，参数类型明确
 *   2. 返回数据通过全局变量 _vml_result_* 中转
 *   3. 函数参数按 VML 调用约定已在 R0/R1/R2/R3 中
 *   4. 仅 C/ObjC/C++ 语言可使用 asm()，其他语言通过链接此库使用系统功能
 */

/* 全局存储 — asm 结果通过此变量中转返回 */
int _vml_result_int = 0;
const char* _vml_result_str = 0;
float _vml_result_float = 0.0f;

/* ═══════════════════════════════════════════
 * 基本 I/O (SYSCALL #1-10, #391-394)
 * ═══════════════════════════════════════════ */

void vml_print_str(const char* s);
void vml_print_str(const char* s) {
    asm("SYSCALL 1");
}

void vml_print_int(int n);
void vml_print_int(int n) {
    asm("SYSCALL 6");
}

void vml_print_char(char c);
void vml_print_char(char c) {
    asm("SYSCALL 4");
}

void vml_print_float(float f);
void vml_print_float(float f) {
    asm("SYSCALL 8");
}

void vml_print_hex(int n);
void vml_print_hex(int n) {
    asm("SYSCALL 10");
}

void vml_newline(void);
void vml_newline(void) {
    asm("MOVE R0, #10\nSYSCALL 4");
}

/* ====== 宽字符输出 (SYSCALL #391-394) ====== */
void vml_print_wstr(const char* s);
void vml_print_wstr(const char* s) {
    asm("SYSCALL 391");
}

void vml_print_ustr(const char* s);
void vml_print_ustr(const char* s) {
    asm("SYSCALL 393");
}

/* ═══════════════════════════════════════════
 * 内存管理 (SYSCALL #11-13, #40-41)
 * ═══════════════════════════════════════════ */

int vml_alloc(int size);
int vml_alloc(int size) {
    asm("SYSCALL 40");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

void vml_free(int addr);
void vml_free(int addr) {
    asm("SYSCALL 41");
}

int vml_mem_copy(int src, int dst, int len);
int vml_mem_copy(int src, int dst, int len) {
    asm("SYSCALL 11");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_mem_fill(int addr, int value, int len);
int vml_mem_fill(int addr, int value, int len) {
    asm("SYSCALL 12");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_mem_compare(int addr1, int addr2, int len);
int vml_mem_compare(int addr1, int addr2, int len) {
    asm("SYSCALL 13");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

/* ═══════════════════════════════════════════
 * 随机数 / 时间 (SYSCALL #50-58)
 * ═══════════════════════════════════════════ */

int vml_random(void);
int vml_random(void) {
    asm("SYSCALL 50");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

void vml_seed(int s);
void vml_seed(int s) {
    asm("SYSCALL 51");
}

void vml_sleep_ms(int ms);
void vml_sleep_ms(int ms) {
    asm("SYSCALL 52");
}

int vml_get_tick(void);
int vml_get_tick(void) {
    asm("SYSCALL 53");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_get_timestamp(void);
int vml_get_timestamp(void) {
    asm("SYSCALL 54");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

const char* vml_get_date(void);
const char* vml_get_date(void) {
    asm("SYSCALL 55");
    asm("MOVE [_vml_result_str], R0");
    return _vml_result_str;
}

const char* vml_get_time(void);
const char* vml_get_time(void) {
    asm("SYSCALL 56");
    asm("MOVE [_vml_result_str], R0");
    return _vml_result_str;
}

void vml_beep(int freq_hz, int duration_ms);
void vml_beep(int freq_hz, int duration_ms) {
    asm("SYSCALL 57");
}

/* ═══════════════════════════════════════════
 * 系统信息 (SYSCALL #59-60, #374)
 * ═══════════════════════════════════════════ */

int vml_get_config(int type);
int vml_get_config(int type) {
    asm("SYSCALL 60");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

const char* vml_get_info(int typeId);
const char* vml_get_info(int typeId) {
    asm("SYSCALL 59");
    asm("MOVE [_vml_result_str], R0");
    return _vml_result_str;
}

int vml_get_platform(void);
int vml_get_platform(void) {
    asm("SYSCALL 374");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

/* ═══════════════════════════════════════════
 * 调试 (SYSCALL #70-72)
 * ═══════════════════════════════════════════ */

void vml_debug_print(const char* s);
void vml_debug_print(const char* s) {
    asm("SYSCALL 70");
}

void vml_debug_int(int n);
void vml_debug_int(int n) {
    asm("SYSCALL 71");
}

void vml_assert(int cond, const char* msg);
void vml_assert(int cond, const char* msg) {
    asm("SYSCALL 72");
}

/* ═══════════════════════════════════════════
 * 设备操作 (SYSCALL #100-107)
 * ═══════════════════════════════════════════ */

int vml_device_open(const char* name);
int vml_device_open(const char* name) {
    asm("SYSCALL 100");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_device_close(int handle);
int vml_device_close(int handle) {
    asm("SYSCALL 101");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_device_read(int handle, int buf, int len);
int vml_device_read(int handle, int buf, int len) {
    asm("SYSCALL 102");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_device_write(int handle, int buf, int len);
int vml_device_write(int handle, int buf, int len) {
    asm("SYSCALL 103");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_device_control(int handle, int cmd, int data, int len);
int vml_device_control(int handle, int cmd, int data, int len) {
    asm("SYSCALL 104");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

/* ═══════════════════════════════════════════
 * 文件操作 (SYSCALL #110-114)
 * ═══════════════════════════════════════════ */

int vml_file_open(const char* path, int mode);
int vml_file_open(const char* path, int mode) {
    asm("SYSCALL 110");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_file_close(int handle);
int vml_file_close(int handle) {
    asm("SYSCALL 111");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_file_read(int handle, int buf, int len);
int vml_file_read(int handle, int buf, int len) {
    asm("SYSCALL 112");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_file_write(int handle, int buf, int len);
int vml_file_write(int handle, int buf, int len) {
    asm("SYSCALL 113");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_file_size(int handle);
int vml_file_size(int handle) {
    int cmd = 0;
    asm("SYSCALL 114");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_file_seek(int handle, int pos);
int vml_file_seek(int handle, int pos) {
    int cmd = 1;
    asm("SYSCALL 114");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_file_tell(int handle);
int vml_file_tell(int handle) {
    int cmd = 2;
    asm("SYSCALL 114");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

/* ═══════════════════════════════════════════
 * 文件系统 OS (SYSCALL #340-344)
 * ═══════════════════════════════════════════ */

int vml_mkdir(const char* path);
int vml_mkdir(const char* path) {
    asm("SYSCALL 340");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_remove(const char* path);
int vml_remove(const char* path) {
    asm("SYSCALL 341");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_rename(const char* oldpath, const char* newpath);
int vml_rename(const char* oldpath, const char* newpath) {
    asm("SYSCALL 342");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_readdir(const char* path, int buf);
int vml_readdir(const char* path, int buf) {
    asm("SYSCALL 343");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_stat(const char* path, int buf);
int vml_stat(const char* path, int buf) {
    asm("SYSCALL 344");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

/* ═══════════════════════════════════════════
 * 环境变量 (SYSCALL #360-362)
 * ═══════════════════════════════════════════ */

const char* vml_get_env(const char* name);
const char* vml_get_env(const char* name) {
    asm("SYSCALL 360");
    asm("MOVE [_vml_result_str], R0");
    return _vml_result_str;
}

int vml_set_env(const char* name, const char* value);
int vml_set_env(const char* name, const char* value) {
    asm("SYSCALL 361");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

/* ═══════════════════════════════════════════
 * 进程 / OS 线程 (SYSCALL #3, #300-303, #310-316, #322)
 * ═══════════════════════════════════════════ */

void vml_exit(int code);
void vml_exit(int code) {
    asm("SYSCALL 3");
}

int vml_get_pid(void);
int vml_get_pid(void) {
    asm("SYSCALL 322");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_thread_create(int fn, int stack_size);
int vml_thread_create(int fn, int stack_size) {
    asm("SYSCALL 300");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

void vml_thread_exit(void);
void vml_thread_exit(void) {
    asm("SYSCALL 301");
}

void vml_thread_yield(void);
void vml_thread_yield(void) {
    asm("SYSCALL 303");
}

int vml_mutex_create(void);
int vml_mutex_create(void) {
    asm("SYSCALL 310");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_mutex_lock(int id);
int vml_mutex_lock(int id) {
    asm("SYSCALL 311");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_mutex_unlock(int id);
int vml_mutex_unlock(int id) {
    asm("SYSCALL 312");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

/* ═══════════════════════════════════════════
 * 条件变量 (SYSCALL #313-316)
 * ═══════════════════════════════════════════ */

int vml_cond_create(void);
int vml_cond_create(void) {
    asm("SYSCALL 313");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_cond_wait(int cond_id, int mutex_id);
int vml_cond_wait(int cond_id, int mutex_id) {
    asm("SYSCALL 314");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_cond_signal(int cond_id);
int vml_cond_signal(int cond_id) {
    asm("SYSCALL 315");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_cond_broadcast(int cond_id);
int vml_cond_broadcast(int cond_id) {
    asm("SYSCALL 316");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

/* ═══════════════════════════════════════════
 * 进程 (SYSCALL #320)
 * ═══════════════════════════════════════════ */

int vml_exec(const char* path);
int vml_exec(const char* path) {
    asm("SYSCALL 320");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

/* ═══════════════════════════════════════════
 * 网络 Socket (SYSCALL #330-338)
 * ═══════════════════════════════════════════ */

int vml_socket_create(int domain, int type);
int vml_socket_create(int domain, int type) {
    asm("SYSCALL 330");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_socket_connect(int fd, const char* ip, int port);
int vml_socket_connect(int fd, const char* ip, int port) {
    asm("SYSCALL 334");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_socket_send(int fd, int buf, int len);
int vml_socket_send(int fd, int buf, int len) {
    asm("SYSCALL 335");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_socket_recv(int fd, int buf, int len);
int vml_socket_recv(int fd, int buf, int len) {
    asm("SYSCALL 336");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

int vml_socket_close(int fd);
int vml_socket_close(int fd) {
    asm("SYSCALL 337");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}

const char* vml_dns_resolve(const char* hostname);
const char* vml_dns_resolve(const char* hostname) {
    asm("SYSCALL 338");
    asm("MOVE [_vml_result_str], R0");
    return _vml_result_str;
}

/* ═══════════════════════════════════════════
 * 截屏 (SYSCALL #200)
 * ═══════════════════════════════════════════ */

void vml_screenshot(void);
void vml_screenshot(void) {
    asm("SYSCALL 200");
}

/* ═══════════════════════════════════════════
 * SYS — 通用系统调用
 * ═══════════════════════════════════════════ */

/* 直接设置 VML 寄存器 R0-R4 并执行任意 SYSCALL */
int vml_syscall5(int num, int a0, int a1, int a2, int a3, int a4);
int vml_syscall5(int num, int a0, int a1, int a2, int a3, int a4) {
    /* 参数在寄存器中: num=R0, a0-a4 需要移动
       注: 调用时 C 编译器会将参数依次放入 R0-R4 (caller-saved)
       但我们需要: R0=a0, R1=a1, R2=a2, R3=a3, R4=a4
       然后 MOVE R5, num / SYSCALL R5

       简化: 由于调用约定已把 a0-a4 放入 R0-R4,
       我们需要临时保存 num (在 R0), 恢复 a0 到 R0
       用全局变量做中转 */
    asm("MOVE [_vml_result_int], R0");
    /* 现在 R0 已保存。调用约定已把 a1=R1, a2=R2, a3=R3, a4=R4 */
    /* 恢复 num 到 R5: 需要手动 load */
    /* 实际使用: 把 num 放入 R5, 恢复 a0 到 R0 */
    _vml_result_int = num;
    asm("MOVE R5, [_vml_result_int]");
    _vml_result_int = a0;
    asm("MOVE R0, [_vml_result_int]");
    asm("SYSCALL R5");
    asm("MOVE [_vml_result_int], R0");
    return _vml_result_int;
}
