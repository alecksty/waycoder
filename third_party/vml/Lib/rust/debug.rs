// VML 调试扩展库 — Rust
// 需显式 use debug

pub mod debug {
    pub fn debug_print(s: &str) {
        unsafe { asm!("SYSCALL 70") }
    }

    pub fn debug_print_int(n: i32) {
        unsafe { asm!("SYSCALL 71") }
    }

    pub fn assert(condition: i32, message: &str) {
        unsafe { asm!("SYSCALL 72") }
    }
}
