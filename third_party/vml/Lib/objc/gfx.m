// VML VGA 图形扩展库 — Objective-C
// SYSCALL 80-82 (文本), 200-203 (高级图形), 60 (配置查询)

// ---- VGA 文本 (SYSCALL 80-82) ----

void vga_clear(void) {
    asm("SYSCALL 80");
}

void vga_putchar(int x, int y, char c, int color) {
    asm("SYSCALL 81");
}

void vga_puts(int x, int y, const char *s, int color) {
    asm("SYSCALL 82");
}

// ---- 屏幕模式与信息 (SYSCALL 60) ----

int gfx_screen(int mode) {
    asm("SYSCALL 80");
    return 0;
}

int gfx_width(void) {
    asm("SYSCALL 60");
    return 0;
}

int gfx_height(void) {
    asm("SYSCALL 60");
    return 0;
}

int gfx_depth(void) {
    asm("SYSCALL 60");
    return 0;
}

// ---- 调色板 ----

void gfx_palette(int index, int r, int g, int b) {
    asm("SYSCALL 104");
}

int gfx_palette_get(int index) {
    asm("SYSCALL 60");
    return 0;
}

// ---- 像素操作 ----

void gfx_pset(int x, int y, int color) {
    // 通过设备控制写像素
    asm("SYSCALL 104");
}

int gfx_point(int x, int y) {
    // 从帧缓冲读像素
    asm("SYSCALL 60");
    return 0;
}

void gfx_cls(void) {
    asm("SYSCALL 80");
}

void gfx_cls_color(int color) {
    // 填充屏幕为指定颜色
    asm("SYSCALL 104");
}

// ---- 绘图基元 (软件实现存根) ----

void gfx_line(int x1, int y1, int x2, int y2, int color) {
    // 软件 Bresenham 直线 — 存根
}

void gfx_rect(int x1, int y1, int x2, int y2, int color) {
    // 软件矩形边框 — 存根
}

void gfx_rect_fill(int x1, int y1, int x2, int y2, int color) {
    // 软件矩形填充 — 存根
}

void gfx_circle(int cx, int cy, int r, int color) {
    // 软件 Bresenham 圆 — 存根
}

void gfx_circle_fill(int cx, int cy, int r, int color) {
    // 软件填充圆 — 存根
}

void gfx_ellipse(int cx, int cy, int rx, int ry, int color) {
    // 软件椭圆 — 存根
}

void gfx_ellipse_fill(int cx, int cy, int rx, int ry, int color) {
    // 软件填充椭圆 — 存根
}

void gfx_arc(int cx, int cy, int r, int start_angle, int end_angle, int color) {
    // 软件圆弧 — 存根
}

void gfx_sector(int cx, int cy, int r, int start_angle, int end_angle, int color) {
    // 软件扇形 — 存根
}

void gfx_sector_fill(int cx, int cy, int r, int start_angle, int end_angle, int color) {
    // 软件填充扇形 — 存根
}

// ---- 位图文本 ----

void gfx_print(int x, int y, const char *text, int color) {
    // 8x16 位图字体 — 存根
    asm("SYSCALL 82");
}

void gfx_print_scale(int x, int y, const char *text, int color, int scale) {
    // 缩放位图文本 — 存根
}

// ---- 洪水填充 ----

void gfx_flood_fill(int x, int y, int fill_color, int border_color) {
    // 软件洪水填充 — 存根
}

// ---- 高级图形 (SYSCALL 200-203) ----

int gfx_screenshot(void) {
    asm("SYSCALL 200");
    return 0;
}

int gfx_put_image(int x, int y, int w, int h, const void *data) {
    asm("SYSCALL 201");
    return 0;
}

int gfx_get_image(int x, int y, int w, int h, void *buffer) {
    asm("SYSCALL 202");
    return 0;
}

int gfx_viewport(int x1, int y1, int x2, int y2) {
    asm("SYSCALL 203");
    return 0;
}
