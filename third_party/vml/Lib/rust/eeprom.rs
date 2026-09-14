// VML EEPROM 扩展库 — Rust
// 需显式 use eeprom

pub mod eeprom {
    pub fn eeprom_read(offset: i32, buffer: *mut u8, count: i32) -> i32 {
        unsafe { asm!("SYSCALL 106") };
        0
    }

    pub fn eeprom_write(offset: i32, data: *const u8, count: i32) -> i32 {
        unsafe { asm!("SYSCALL 107") };
        0
    }
}
