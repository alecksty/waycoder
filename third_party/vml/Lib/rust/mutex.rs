// VML 互斥锁扩展库 — Rust (OS 模式)
pub fn mutex_create() -> i32 { unsafe { asm!("SYSCALL 310") }; 0 }
pub fn mutex_lock(id: i32) -> i32 { unsafe { asm!("SYSCALL 311") }; 0 }
pub fn mutex_unlock(id: i32) -> i32 { unsafe { asm!("SYSCALL 312") }; 0 }
