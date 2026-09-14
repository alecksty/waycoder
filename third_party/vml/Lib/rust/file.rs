// VML 文件操作扩展库 — Rust
pub mod file {
    pub fn fopen(name: &str, mode: &str) -> i32 { unsafe { asm!("SYSCALL 110") }; 0 }
    pub fn fclose(handle: i32) -> i32 { unsafe { asm!("SYSCALL 111") }; 0 }
    pub fn fread(handle: i32, buf: *mut u8, count: i32) -> i32 { unsafe { asm!("SYSCALL 112") }; 0 }
    pub fn fwrite(handle: i32, buf: *const u8, count: i32) -> i32 { unsafe { asm!("SYSCALL 113") }; 0 }
    pub fn fseek(handle: i32, offset: i32) -> i32 { unsafe { asm!("SYSCALL 114") }; 0 }
    pub fn ftell(handle: i32) -> i32 { unsafe { asm!("SYSCALL 114") }; 0 }
}
