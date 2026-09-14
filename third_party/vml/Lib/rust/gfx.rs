// VML VGA 图形扩展库 (QBASIC风格) — Rust
// 需显式 mod gfx

// Basic VGA
pub fn vga_clear() {
    unsafe { asm!("SYSCALL 80") }
}

pub fn vga_putchar(x: i32, y: i32, c: char, color: i32) {
    unsafe { asm!("SYSCALL 81") }
}

pub fn vga_puts(x: i32, y: i32, s: &str, color: i32) {
    unsafe { asm!("SYSCALL 82") }
}

// Screen mode & info
pub fn gfx_screen(mode: i32) -> i32 { 0 }
pub fn gfx_width() -> i32 { 0 }
pub fn gfx_height() -> i32 { 0 }
pub fn gfx_depth() -> i32 { 0 }

// Palette
pub fn gfx_palette(idx: i32, r: i32, g: i32, b: i32) {}
pub fn gfx_palette_get(idx: i32) -> i32 { 0 }

// Pixel ops
pub fn gfx_pset(x: i32, y: i32, color: i32) {}
pub fn gfx_point(x: i32, y: i32) -> i32 { 0 }
pub fn gfx_cls() {}
pub fn gfx_cls_color(color: i32) {}

// Drawing
pub fn gfx_line(x1: i32, y1: i32, x2: i32, y2: i32, color: i32) {}
pub fn gfx_rect(x1: i32, y1: i32, x2: i32, y2: i32, color: i32) {}
pub fn gfx_rect_fill(x1: i32, y1: i32, x2: i32, y2: i32, color: i32) {}
pub fn gfx_circle(cx: i32, cy: i32, r: i32, color: i32) {}
pub fn gfx_circle_fill(cx: i32, cy: i32, r: i32, color: i32) {}
pub fn gfx_arc(cx: i32, cy: i32, r: i32, sa: i32, ea: i32, color: i32) {}
pub fn gfx_sector(cx: i32, cy: i32, r: i32, sa: i32, ea: i32, color: i32) {}

// Text
pub fn gfx_print(x: i32, y: i32, text: &str, color: i32) {}
pub fn gfx_print_scale(x: i32, y: i32, text: &str, color: i32, scale: i32) {}

// Fill
pub fn gfx_flood_fill(x: i32, y: i32, fc: i32, bc: i32) {}

// Advanced (SYSCALL)
pub fn gfx_screenshot() -> i32 {
    unsafe { asm!("SYSCALL 200") };
    0
}

pub fn gfx_put_image(x: i32, y: i32, w: i32, h: i32, data: *const u8) -> i32 {
    unsafe { asm!("SYSCALL 201") };
    0
}

pub fn gfx_get_image(x: i32, y: i32, w: i32, h: i32, buf: *mut u8) -> i32 {
    unsafe { asm!("SYSCALL 202") };
    0
}

pub fn gfx_viewport(x1: i32, y1: i32, x2: i32, y2: i32) -> i32 {
    unsafe { asm!("SYSCALL 203") };
    0
}
