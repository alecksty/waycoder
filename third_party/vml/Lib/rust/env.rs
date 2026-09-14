// VML 环境变量扩展库 — Rust (OS 模式)
// 需显式 use env

pub mod env {
    pub fn get_env(name: &str) -> &str {
        unsafe { asm!("SYSCALL 360") };
        ""
    }

    pub fn set_env(name: &str, value: &str) -> i32 {
        unsafe { asm!("SYSCALL 361") };
        0
    }

    pub fn get_args(buffer: *mut u8) -> i32 {
        unsafe { asm!("SYSCALL 362") };
        0
    }
}
