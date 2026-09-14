// VML FFI 动态库调用扩展库 — Rust
// OS 模式专用，需显式 mod ffi

pub fn dl_open(path: &str) -> i32 {
    unsafe { asm!("SYSCALL 370") }
    0
}
pub fn dl_sym(handle: i32, name: &str) -> i32 {
    unsafe { asm!("SYSCALL 371") }
    0
}
pub fn dl_close(handle: i32) -> i32 {
    unsafe { asm!("SYSCALL 372") }
    0
}
pub fn native_call(func_id: i32, args: &[i32], count: i32, flags: i32) -> i32 {
    unsafe { asm!("SYSCALL 373") }
    0
}
pub fn native_call_f(func_id: i32, fargs: &[f32], count: i32, flags: i32) -> f32 {
    unsafe { asm!("SYSCALL 375") }
    0.0
}
pub fn get_platform() -> i32 {
    unsafe { asm!("SYSCALL 374") }
    0
}
