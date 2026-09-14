# VML VGA 图形扩展函数块 (QBASIC风格) — Ladder
# 需显式 include gfx.ld

# VGA 清屏
[VGA_CLEAR]
SYSCALL 80
RET

# VGA 显示字符 (X, Y, Char, Color)
[VGA_PUTCHAR]
SYSCALL 81
RET

# VGA 显示字符串 (X, Y, Str, Color)
[VGA_PUTS]
SYSCALL 82
RET

# === 屏幕模式与信息 ===

[GFX_SCREEN]
RET

[GFX_WIDTH]
RET

[GFX_HEIGHT]
RET

[GFX_DEPTH]
RET

# === 调色板 ===

[GFX_PALETTE]
RET

[GFX_PALETTE_GET]
RET

# === 像素操作 ===

[GFX_PSET]
RET

[GFX_POINT]
RET

[GFX_CLS]
RET

[GFX_CLS_COLOR]
RET

# === 绘图原语 ===

[GFX_LINE]
RET

[GFX_RECT]
RET

[GFX_RECT_FILL]
RET

[GFX_CIRCLE]
RET

[GFX_CIRCLE_FILL]
RET

[GFX_ARC]
RET

[GFX_SECTOR]
RET

# === 文字 ===

[GFX_PRINT]
RET

[GFX_PRINT_SCALE]
RET

# === 填充 ===

[GFX_FLOOD_FILL]
RET

# === 高级图形 (SYSCALL) ===

# 调试截图
[GFX_SCREENSHOT]
SYSCALL 200
RET

# 像素写入 (X, Y, W, H, Data)
[GFX_PUT_IMAGE]
SYSCALL 201
RET

# 像素读取 (X, Y, W, H, Buffer)
[GFX_GET_IMAGE]
SYSCALL 202
RET

# 视口裁剪 (X1, Y1, X2, Y2)
[GFX_VIEWPORT]
SYSCALL 203
RET
