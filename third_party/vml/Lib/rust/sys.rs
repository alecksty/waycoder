// VML 系统扩展库 — Rust
// 需显式 use sys

pub mod sys {
    pub fn speaker_beep(freq: i32, duration: i32) {
        unsafe { asm!("SYSCALL 57") }
    }

    pub fn set_rtc(timestamp: i32) -> i32 {
        unsafe { asm!("SYSCALL 58") };
        0
    }
}
