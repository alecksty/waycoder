// VML 网络 Socket 扩展库 — Rust
// OS 模式专用，需显式 mod net

pub fn create(domain: i32, typ: i32) -> i32 {
    unsafe { asm!("SYSCALL 330") }
    0
}
pub fn bind(fd: i32, port: i32) -> i32 {
    unsafe { asm!("SYSCALL 331") }
    0
}
pub fn listen(fd: i32, backlog: i32) -> i32 {
    unsafe { asm!("SYSCALL 332") }
    0
}
pub fn accept(fd: i32) -> i32 {
    unsafe { asm!("SYSCALL 333") }
    0
}
pub fn connect(host: &str, port: i32) -> i32 {
    unsafe { asm!("SYSCALL 334") }
    0
}
pub fn send(fd: i32, data: &[u8], len: i32) -> i32 {
    unsafe { asm!("SYSCALL 335") }
    0
}
pub fn recv(fd: i32, buf: &mut [u8], max_len: i32) -> i32 {
    unsafe { asm!("SYSCALL 336") }
    0
}
pub fn close(fd: i32) -> i32 {
    unsafe { asm!("SYSCALL 337") }
    0
}
pub fn dns_resolve(hostname: &str) -> i32 {
    unsafe { asm!("SYSCALL 338") }
    0
}
