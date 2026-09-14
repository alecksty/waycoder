# VML VGA 图形扩展库 (QBASIC风格) — Python
# 需显式 import gfx

# === Basic VGA (SYSCALL) ===
def vga_clear():
    asm("SYSCALL 80")

def vga_putchar(x, y, c, color):
    asm("SYSCALL 81")

def vga_puts(x, y, s, color):
    asm("SYSCALL 82")

# === Screen Mode & Info ===
def gfx_screen(mode=-1):
    pass  # returns current/set mode

def gfx_width():
    pass

def gfx_height():
    pass

def gfx_depth():
    pass

# === Palette ===
def gfx_palette(index, r, g, b):
    pass

def gfx_palette_get(index):
    pass

# === Pixel Ops ===
def gfx_pset(x, y, color):
    pass

def gfx_point(x, y):
    pass

def gfx_cls():
    pass

def gfx_cls_color(color):
    pass

# === Drawing Primitives ===
def gfx_line(x1, y1, x2, y2, color):
    pass

def gfx_rect(x1, y1, x2, y2, color):
    pass

def gfx_rect_fill(x1, y1, x2, y2, color):
    pass

def gfx_circle(cx, cy, r, color):
    pass

def gfx_circle_fill(cx, cy, r, color):
    pass

def gfx_arc(cx, cy, r, start_angle, end_angle, color):
    pass

def gfx_sector(cx, cy, r, start_angle, end_angle, color):
    pass

# === Text (8x16 bitmap font) ===
def gfx_print(x, y, text, color):
    pass

def gfx_print_scale(x, y, text, color, scale=1):
    pass

# === Flood Fill ===
def gfx_flood_fill(x, y, fill_color, border_color):
    pass

# === Advanced Graphics (SYSCALL) ===
def gfx_screenshot():
    asm("SYSCALL 200")

def gfx_put_image(x, y, w, h, data):
    asm("SYSCALL 201")

def gfx_get_image(x, y, w, h, buf):
    asm("SYSCALL 202")

def gfx_viewport(x1, y1, x2, y2):
    asm("SYSCALL 203")
