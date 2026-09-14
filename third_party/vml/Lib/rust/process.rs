// VML 进程扩展库 — Rust (OS 模式)
// 需显式 use process

pub fn exec(path: &str) -> i32 {
    unsafe { asm!("SYSCALL 320") };
    0
}

pub fn get_pid() -> i32 {
    unsafe { asm!("SYSCALL 322") };
    0
}
