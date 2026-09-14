# VML VGA 图形扩展库 (QBASIC风格) — Ruby
# 需显式 import gfx

# === Basic VGA (SYSCALL) ===
def vga_clear()
    asm("SYSCALL 80")
end

def vga_putchar(x, y, c, color)
    asm("SYSCALL 81")
end

def vga_puts(x, y, s, color)
    asm("SYSCALL 82")
end

# === Screen Mode & Info ===
def gfx_screen(mode)
    # stub: returns current/set mode
    return 0
end

def gfx_width()
    # stub
    return 0
end

def gfx_height()
    # stub
    return 0
end

def gfx_depth()
    # stub
    return 0
end

# === Palette ===
def gfx_palette(index, r, g, b)
    # stub
    return 0
end

def gfx_palette_get(index)
    # stub
    return 0
end

# === Pixel Ops ===
def gfx_pset(x, y, color)
    # stub
    return 0
end

def gfx_point(x, y)
    # stub
    return 0
end

def gfx_cls()
    # stub
    return 0
end

def gfx_cls_color(color)
    # stub
    return 0
end

# === Drawing Primitives ===
def gfx_line(x1, y1, x2, y2, color)
    # stub
    return 0
end

def gfx_rect(x1, y1, x2, y2, color)
    # stub
    return 0
end

def gfx_rect_fill(x1, y1, x2, y2, color)
    # stub
    return 0
end

def gfx_circle(cx, cy, r, color)
    # stub
    return 0
end

def gfx_circle_fill(cx, cy, r, color)
    # stub
    return 0
end

def gfx_arc(cx, cy, r, start_angle, end_angle, color)
    # stub
    return 0
end

def gfx_sector(cx, cy, r, start_angle, end_angle, color)
    # stub
    return 0
end

# === Text (8x16 bitmap font) ===
def gfx_print(x, y, text, color)
    # stub
    return 0
end

def gfx_print_scale(x, y, text, color, scale)
    # stub
    return 0
end

# === Flood Fill ===
def gfx_flood_fill(x, y, fill_color, border_color)
    # stub
    return 0
end

# === Advanced Graphics (SYSCALL) ===
def gfx_screenshot()
    asm("SYSCALL 200")
end

def gfx_put_image(x, y, w, h, data)
    asm("SYSCALL 201")
end

def gfx_get_image(x, y, w, h, buf)
    asm("SYSCALL 202")
end

def gfx_viewport(x1, y1, x2, y2)
    asm("SYSCALL 203")
end
