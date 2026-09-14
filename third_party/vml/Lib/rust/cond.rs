// VML 条件变量扩展库 — Rust (OS 模式)
pub fn cond_create() -> i32 { unsafe { asm!("SYSCALL 313") }; 0 }
pub fn cond_wait(cond_id: i32, mutex_id: i32) -> i32 { unsafe { asm!("SYSCALL 314") }; 0 }
pub fn cond_signal(cond_id: i32) -> i32 { unsafe { asm!("SYSCALL 315") }; 0 }
pub fn cond_broadcast(cond_id: i32) -> i32 { unsafe { asm!("SYSCALL 316") }; 0 }
