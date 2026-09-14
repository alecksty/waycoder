// VML 文件系统扩展库 — Rust (OS 模式)
// 需显式 use fs

pub mod fs {
    pub fn mkdir(path: &str) -> i32 {
        unsafe { asm!("SYSCALL 340") };
        0
    }

    pub fn remove(path: &str) -> i32 {
        unsafe { asm!("SYSCALL 341") };
        0
    }

    pub fn rename(old_path: &str, new_path: &str) -> i32 {
        unsafe { asm!("SYSCALL 342") };
        0
    }

    pub fn readdir(path: &str, buffer: *mut u8) -> i32 {
        unsafe { asm!("SYSCALL 343") };
        0
    }

    pub fn stat(path: &str, info: *mut u8) -> i32 {
        unsafe { asm!("SYSCALL 344") };
        0
    }
}
