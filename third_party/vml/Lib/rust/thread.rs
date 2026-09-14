// VML 线程扩展库 — Rust (OS 模式)
pub fn thread_create(entry: i32, stack_size: i32) -> i32 { unsafe { asm!("SYSCALL 300") }; 0 }
pub fn thread_exit() { unsafe { asm!("SYSCALL 301") } }
pub fn thread_join(tid: i32) -> i32 { unsafe { asm!("SYSCALL 302") }; 0 }
pub fn thread_yield() -> i32 { unsafe { asm!("SYSCALL 303") }; 0 }
