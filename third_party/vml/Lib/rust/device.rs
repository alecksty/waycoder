// VML 设备 I/O + 文件 + 键鼠扩展库 — Rust
// 需显式 mod device

// 键盘
pub fn kb_hit() -> bool { false }
pub fn kb_getch() -> Option<char> { None }

// 鼠标
pub fn mouse_get_x() -> i32 { 0 }
pub fn mouse_get_y() -> i32 { 0 }
pub fn mouse_left() -> bool { false }
pub fn mouse_right() -> bool { false }

// 统一设备接口
pub fn dev_open(name: &str) -> i32 { 0 }
pub fn dev_close(handle: i32) -> i32 { 0 }
pub fn dev_read(handle: i32, buf: &mut [u8], offset: i32, count: i32) -> i32 { 0 }
pub fn dev_write(handle: i32, buf: &[u8], offset: i32, count: i32) -> i32 { 0 }
pub fn dev_control(handle: i32, command: i32, data: &[u8], length: i32) -> i32 { 0 }

