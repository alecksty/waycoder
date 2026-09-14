// VML 线程管理扩展库 — Objective-C
// SYSCALL 300-303 (OS mode)

int thread_create(void *entry, int stack_size) {
    asm("SYSCALL 300");
    return 0;
}

void thread_exit(void) {
    asm("SYSCALL 301");
}

int thread_join(int thread_id) {
    asm("SYSCALL 302");
    return 0;
}

int thread_yield(void) {
    asm("SYSCALL 303");
    return 0;
}
