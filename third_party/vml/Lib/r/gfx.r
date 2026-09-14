# VML VGA 图形扩展库 (QBASIC风格) — R

# === Basic VGA (SYSCALL) ===
vga_clear <- function() {
    asm("SYSCALL 80")
}

vga_putchar <- function(x, y, c, color) {
    asm("SYSCALL 81")
}

vga_puts <- function(x, y, s, color) {
    asm("SYSCALL 82")
}

# === Screen Mode & Info (stub — calls shared/graphics.vml) ===
gfx_screen <- function(mode) {
    0
}

gfx_width <- function() {
    0
}

gfx_height <- function() {
    0
}

gfx_depth <- function() {
    0
}

# === Palette (stub) ===
gfx_palette <- function(index, r, g, b) {
}

gfx_palette_get <- function(index) {
    0
}

# === Pixel Ops (stub) ===
gfx_pset <- function(x, y, color) {
}

gfx_point <- function(x, y) {
    0
}

gfx_cls <- function() {
}

gfx_cls_color <- function(color) {
}

# === Drawing Primitives (stub) ===
gfx_line <- function(x1, y1, x2, y2, color) {
}

gfx_rect <- function(x1, y1, x2, y2, color) {
}

gfx_rect_fill <- function(x1, y1, x2, y2, color) {
}

gfx_circle <- function(cx, cy, r, color) {
}

gfx_circle_fill <- function(cx, cy, r, color) {
}

gfx_arc <- function(cx, cy, r, start_angle, end_angle, color) {
}

gfx_sector <- function(cx, cy, r, start_angle, end_angle, color) {
}

gfx_sector_fill <- function(cx, cy, r, start_angle, end_angle, color) {
}

gfx_ellipse <- function(cx, cy, rx, ry, color) {
}

gfx_ellipse_fill <- function(cx, cy, rx, ry, color) {
}

# === Text (stub) ===
gfx_print <- function(x, y, text, color) {
}

gfx_print_scale <- function(x, y, text, color, scale) {
}

# === Flood Fill (stub) ===
gfx_flood_fill <- function(x, y, fill_color, border_color) {
}

# === Advanced Graphics (SYSCALL) ===
gfx_screenshot <- function() {
    asm("SYSCALL 200")
    0
}

gfx_put_image <- function(x, y, w, h, data) {
    asm("SYSCALL 201")
    0
}

gfx_get_image <- function(x, y, w, h, buf) {
    asm("SYSCALL 202")
    0
}

gfx_viewport <- function(x1, y1, x2, y2) {
    asm("SYSCALL 203")
    0
}
