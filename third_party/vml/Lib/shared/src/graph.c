#param lib("array")
#param lib("math")

// VML Graph Unit — Turbo Pascal BGI 兼容图形库
// 对 Lib/shared/graphics.c 的 BGI 命名封装
// v1.66.33

// 内部 VGA 函数声明 (来自 graphics.c)
int   gfx_screen(int mode);
int   gfx_width(void);
int   gfx_height(void);
int   gfx_depth(void);
void  gfx_palette(int index, int r, int g, int b);
int   gfx_palette_get(int index);
void  gfx_pset(int x, int y, int color);
int   gfx_point(int x, int y);
void  gfx_cls(void);
void  gfx_cls_color(int color);
void  gfx_line(int x1, int y1, int x2, int y2, int color);
void  gfx_rect(int x1, int y1, int x2, int y2, int color);
void  gfx_rect_fill(int x1, int y1, int x2, int y2, int color);
void  gfx_circle(int cx, int cy, int r, int color);
void  gfx_circle_fill(int cx, int cy, int r, int color);
void  gfx_ellipse(int cx, int cy, int rx, int ry, int color);
void  gfx_ellipse_fill(int cx, int cy, int rx, int ry, int color);
void  gfx_arc(int cx, int cy, int r, int sa, int ea, int color);
void  gfx_sector(int cx, int cy, int r, int sa, int ea, int color);
void  gfx_sector_fill(int cx, int cy, int r, int sa, int ea, int color);
void  gfx_flood_fill(int x, int y, int fc, int bc);
void  gfx_print(int x, int y, const char* text, int color);
void  gfx_print_scale(int x, int y, const char* text, int color, int scale);
int   gfx_screenshot(void);
int   gfx_put_image(int x, int y, int w, int h, const void* data);
int   gfx_get_image(int x, int y, int w, int h, void* buffer);
int   gfx_viewport(int x1, int y1, int x2, int y2);

// ═══════════════════════════════════════════════════════════
// BGI 颜色/样式状态
// ═══════════════════════════════════════════════════════════
static int _bgi_color = 15;      // 默认白色
static int _bgi_bgcolor = 0;     // 默认黑色
static int _bgi_fill_pattern = 1; // 默认实心填充
static int _bgi_fill_color = 15;

// ═══════════════════════════════════════════════════════════
// BGI 常量 (Turbo Pascal 7.0 兼容)
// ═══════════════════════════════════════════════════════════

// 图形驱动
#define BGI_DETECT      0
#define BGI_CGA         1
#define BGI_MCGA        2
#define BGI_EGA         3
#define BGI_EGA64       4
#define BGI_EGAMONO     5
#define BGI_IBM8514     6
#define BGI_HERCMONO    7
#define BGI_ATT400      8
#define BGI_VGA         9
#define BGI_PC3270      10

// 图形模式
#define BGI_VGALO   0   // 640x200x16
#define BGI_VGAMED  1   // 640x350x16
#define BGI_VGAHI   2   // 640x480x16

// 线型
#define BGI_SOLID_LINE   0
#define BGI_DOTTED_LINE  1
#define BGI_CENTER_LINE  2
#define BGI_DASHED_LINE  3

// 填充模式
#define BGI_EMPTY_FILL   0
#define BGI_SOLID_FILL   1
#define BGI_LINE_FILL    2
#define BGI_LTSLASH_FILL 3
#define BGI_SLASH_FILL   4
#define BGI_BKSLASH_FILL 5
#define BGI_LTBKSLASH_FILL 6
#define BGI_HATCH_FILL   7
#define BGI_XHATCH_FILL  8
#define BGI_INTERLEAVE_FILL 9
#define BGI_WIDE_DOT_FILL  10
#define BGI_CLOSE_DOT_FILL 11
#define BGI_USER_FILL    12

// ═══════════════════════════════════════════════════════════
// Turbo Pascal BGI API 封装
// ═══════════════════════════════════════════════════════════

// 初始化图形系统
__stdcall void InitGraph(int* driver, int* mode, const char* path) {
    int d = driver ? *driver : BGI_DETECT;
    int m = mode ? *mode : BGI_VGAHI;
    // 映射到 VML SCREEN 模式
    int vga_mode = 12; // 默认 VGA 640x480x16
    if (d == BGI_DETECT) {
        // 自动检测 → 使用 VGA 最高模式
        vga_mode = 12;
    } else if (d == BGI_VGA) {
        if (m == BGI_VGALO)  vga_mode = 8;   // 640x200x16
        else if (m == BGI_VGAMED) vga_mode = 9;  // 640x350x16
        else vga_mode = 12;  // 640x480x16
    }
    gfx_screen(vga_mode);
    gfx_cls_color(_bgi_bgcolor);
}

// 关闭图形系统, 返回文本模式
__stdcall void CloseGraph(void) {
    gfx_screen(0);  // 返回文本模式
}

// 检测图形驱动
__stdcall void DetectGraph(int* driver, int* mode) {
    if (driver) *driver = BGI_VGA;
    if (mode) *mode = BGI_VGAHI;
}

// 获取最大 X/Y
__stdcall int GetMaxX(void) { return gfx_width() - 1; }
__stdcall int GetMaxY(void) { return gfx_height() - 1; }

// 获取模式名
__stdcall char* GetDriverName(void) { return (char*)"VGA"; }
__stdcall char* GetModeName(int mode) { return (char*)"640x480x16"; }

// 颜色
__stdcall void SetColor(int color) {
    _bgi_color = color;
}

__stdcall int GetColor(void) {
    return _bgi_color;
}

__stdcall void SetBkColor(int color) {
    _bgi_bgcolor = color;
}

__stdcall int GetBkColor(void) {
    return _bgi_bgcolor;
}

__stdcall void SetRGBPalette(int index, int r, int g, int b) {
    gfx_palette(index, r, g, b);
}

// 像素
__stdcall void PutPixel(int x, int y, int color) {
    gfx_pset(x, y, color);
}

__stdcall int GetPixel(int x, int y) {
    return gfx_point(x, y);
}

// 线
__stdcall void Line(int x1, int y1, int x2, int y2) {
    gfx_line(x1, y1, x2, y2, _bgi_color);
}

__stdcall void LineTo(int x, int y) {
    // 简化: 从 (0,0) 画线到 (x,y)
    gfx_line(0, 0, x, y, _bgi_color);
}

__stdcall void LineRel(int dx, int dy) {
    gfx_line(0, 0, dx, dy, _bgi_color);
}

// 矩形
__stdcall void Rectangle(int x1, int y1, int x2, int y2) {
    gfx_rect(x1, y1, x2, y2, _bgi_color);
}

__stdcall void Bar(int x1, int y1, int x2, int y2) {
    gfx_rect_fill(x1, y1, x2, y2, _bgi_fill_color);
}

__stdcall void Bar3D(int x1, int y1, int x2, int y2, int depth, int top) {
    gfx_rect_fill(x1, y1, x2, y2, _bgi_fill_color);
    // 简化: 3D 效果暂不实现
}

// 圆
__stdcall void Circle(int x, int y, int r) {
    gfx_circle(x, y, r, _bgi_color);
}

__stdcall void FillCircle(int x, int y, int r) {
    gfx_circle_fill(x, y, r, _bgi_fill_color);
}

// 椭圆
__stdcall void Ellipse(int x, int y, int sa, int ea, int rx, int ry) {
    // BGI Ellipse: (x,y) center, start_angle, end_angle, rx, ry
    // 简化: 画完整椭圆, 忽略角度
    gfx_ellipse(x, y, rx, ry, _bgi_color);
}

__stdcall void FillEllipse(int x, int y, int rx, int ry) {
    gfx_ellipse_fill(x, y, rx, ry, _bgi_fill_color);
}

// 弧
__stdcall void Arc(int x, int y, int sa, int ea, int r) {
    gfx_arc(x, y, r, sa, ea, _bgi_color);
}

// 扇形
__stdcall void PieSlice(int x, int y, int sa, int ea, int r) {
    gfx_sector_fill(x, y, r, sa, ea, _bgi_fill_color);
}

// 设置填充样式
__stdcall void SetFillStyle(int pattern, int color) {
    _bgi_fill_pattern = pattern;
    _bgi_fill_color = color;
}

// 设置线型
__stdcall void SetLineStyle(int style, int pattern, int thickness) {
    // 简化: 不支持线型
}

// 清屏
__stdcall void ClearDevice(void) {
    gfx_cls_color(_bgi_bgcolor);
}

__stdcall void ClearViewPort(void) {
    gfx_cls_color(_bgi_bgcolor);
}

// 视口
__stdcall void SetViewPort(int x1, int y1, int x2, int y2, int clip) {
    gfx_viewport(x1, y1, x2, y2);
}

// 文本输出 (BGI 使用 8x8 默认字体)
__stdcall void OutText(const char* text) {
    // 在当前光标位置输出文本 (简化: 左上角)
    gfx_print(0, 0, text, _bgi_color);
}

__stdcall void OutTextXY(int x, int y, const char* text) {
    gfx_print(x, y, text, _bgi_color);
}

// 填充
__stdcall void FloodFill(int x, int y, int border) {
    gfx_flood_fill(x, y, _bgi_fill_color, border);
}

// 图像
__stdcall unsigned int ImageSize(int x1, int y1, int x2, int y2) {
    int w = x2 - x1 + 1;
    int h = y2 - y1 + 1;
    return w * h * 4 + 4;  // 每个像素最多4字节 + 头部
}

__stdcall void GetImage(int x1, int y1, int x2, int y2, void* bitmap) {
    gfx_get_image(x1, y1, x2 - x1 + 1, y2 - y1 + 1, bitmap);
}

__stdcall void PutImage(int x, int y, void* bitmap, int op) {
    int* hdr = (int*)bitmap;
    int w = hdr[0];
    int h = hdr[1];
    const char* data = (const char*)(hdr + 1);
    gfx_put_image(x, y, w, h, data);
}

// 获取调色板
__stdcall int GetPaletteSize(void) {
    return 256;
}

__stdcall void GetPalette(void* palette) {
    // 简化: 返回 null
}

// 获取默认调色板
__stdcall void GetDefaultPalette(void* palette) {
}

// 图形错误
__stdcall int GraphResult(void) {
    return 0;  // grOk
}

__stdcall char* GraphErrorMsg(int code) {
    return (char*)"OK";
}

// 切换回文本模式并保留显存
__stdcall void RestoreCrtMode(void) {
    gfx_screen(0);
}
