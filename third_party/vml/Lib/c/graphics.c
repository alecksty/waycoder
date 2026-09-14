#include "graphics.h"

extern double cos(double x);
extern double sin(double x);
static double atan_approx(double x);
static double atan2_approx(double y, double x);

int _graph_ok = 0;
int _current_color = WHITE;
int _bg_color = BLACK;
int _fill_pattern = SOLID_FILL;
int _fill_color = WHITE;
int _cp_x = 0;
int _cp_y = 0;

static int _vp_x1 = 0, _vp_y1 = 0, _vp_x2 = 319, _vp_y2 = 239, _vp_clip = 0;

static char _font8x8[96][8] = {
    {0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00},
    {0x7E,0x81,0xA5,0x81,0xBD,0x99,0x81,0x7E},
    {0x7E,0xFF,0xDB,0xFF,0xC3,0xE7,0xFF,0x7E},
    {0x6C,0xFE,0xFE,0xFE,0x7C,0x38,0x10,0x00},
    {0x10,0x38,0x7C,0xFE,0x7C,0x38,0x10,0x00},
    {0x38,0x7C,0x38,0xFE,0xFE,0x10,0x10,0x7C},
    {0x00,0x18,0x3C,0x7E,0xFF,0x7E,0x18,0x7E},
    {0x00,0x00,0x18,0x3C,0x3C,0x18,0x00,0x00},
    {0xFF,0xFF,0xE7,0xC3,0xC3,0xE7,0xFF,0xFF},
    {0x00,0x3C,0x66,0x42,0x42,0x66,0x3C,0x00},
    {0xFF,0xC3,0x99,0xBD,0xBD,0x99,0xC3,0xFF},
    {0x0F,0x07,0x0F,0x7D,0xCC,0xCC,0xCC,0x78},
    {0x3C,0x66,0x66,0x66,0x3C,0x18,0x7E,0x18},
    {0x3F,0x33,0x3F,0x30,0x30,0x70,0xF0,0xE0},
    {0x7F,0x63,0x7F,0x63,0x63,0x67,0xE6,0xC0},
    {0x99,0x5A,0x3C,0xE7,0xE7,0x3C,0x5A,0x99},
    {0x80,0xE0,0xF8,0xFE,0xF8,0xE0,0x80,0x00},
    {0x02,0x0E,0x3E,0xFE,0x3E,0x0E,0x02,0x00},
    {0x18,0x3C,0x7E,0x18,0x18,0x7E,0x3C,0x18},
    {0x66,0x66,0x66,0x66,0x66,0x00,0x66,0x00},
    {0x7F,0xDB,0xDB,0x7B,0x1B,0x1B,0x1B,0x00},
    {0x3E,0x63,0x38,0x6C,0x6C,0x38,0xCC,0x78},
    {0x00,0x00,0x00,0x00,0x7E,0x7E,0x7E,0x00},
    {0x18,0x3C,0x7E,0x18,0x7E,0x3C,0x18,0xFF},
    {0x18,0x3C,0x7E,0x18,0x18,0x18,0x18,0x00},
    {0x18,0x18,0x18,0x18,0x7E,0x3C,0x18,0x00},
    {0x00,0x18,0x0C,0xFE,0x0C,0x18,0x00,0x00},
    {0x00,0x30,0x60,0xFE,0x60,0x30,0x00,0x00},
    {0x00,0x00,0xC0,0xC0,0xC0,0xFE,0x00,0x00},
    {0x00,0x24,0x66,0xFF,0x66,0x24,0x00,0x00},
    {0x00,0x10,0x38,0x7C,0xFE,0xFE,0x00,0x00},
    {0x00,0xFE,0xFE,0x7C,0x38,0x10,0x00,0x00},
    {0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00},
    {0x18,0x3C,0x3C,0x18,0x18,0x00,0x18,0x00},
    {0x6C,0x6C,0x6C,0x00,0x00,0x00,0x00,0x00},
    {0x6C,0x6C,0xFE,0x6C,0xFE,0x6C,0x6C,0x00},
    {0x18,0x7E,0xC0,0x7C,0x06,0xFC,0x18,0x00},
    {0x00,0xC6,0xCC,0x18,0x30,0x66,0xC6,0x00},
    {0x38,0x6C,0x38,0x76,0xDC,0xCC,0x76,0x00},
    {0x30,0x30,0x60,0x00,0x00,0x00,0x00,0x00},
    {0x0C,0x18,0x30,0x30,0x30,0x18,0x0C,0x00},
    {0x30,0x18,0x0C,0x0C,0x0C,0x18,0x30,0x00},
    {0x00,0x66,0x3C,0xFF,0x3C,0x66,0x00,0x00},
    {0x00,0x18,0x18,0x7E,0x18,0x18,0x00,0x00},
    {0x00,0x00,0x00,0x00,0x18,0x18,0x30,0x00},
    {0x00,0x00,0x00,0x7E,0x00,0x00,0x00,0x00},
    {0x00,0x00,0x00,0x00,0x18,0x18,0x00,0x00},
    {0x06,0x0C,0x18,0x30,0x60,0xC0,0x80,0x00},
    {0x7C,0xC6,0xCE,0xD6,0xE6,0xC6,0x7C,0x00},
    {0x18,0x38,0x18,0x18,0x18,0x18,0x7E,0x00},
    {0x7C,0xC6,0x06,0x1C,0x30,0x66,0xFE,0x00},
    {0x7C,0xC6,0x06,0x3C,0x06,0xC6,0x7C,0x00},
    {0x1C,0x3C,0x6C,0xCC,0xFE,0x0C,0x1E,0x00},
    {0xFE,0xC0,0xFC,0x06,0x06,0xC6,0x7C,0x00},
    {0x3C,0x60,0xC0,0xFC,0xC6,0xC6,0x7C,0x00},
    {0xFE,0xC6,0x0C,0x18,0x30,0x30,0x30,0x00},
    {0x7C,0xC6,0xC6,0x7C,0xC6,0xC6,0x7C,0x00},
    {0x7C,0xC6,0xC6,0x7E,0x06,0x0C,0x78,0x00},
    {0x00,0x18,0x18,0x00,0x18,0x18,0x00,0x00},
    {0x00,0x18,0x18,0x00,0x18,0x18,0x30,0x00},
    {0x0C,0x18,0x30,0x60,0x30,0x18,0x0C,0x00},
    {0x00,0x00,0x7E,0x00,0x7E,0x00,0x00,0x00},
    {0x30,0x18,0x0C,0x06,0x0C,0x18,0x30,0x00},
    {0x3C,0x66,0x0C,0x18,0x18,0x00,0x18,0x00},
    {0x7C,0xC6,0xDE,0xDE,0xDE,0xC0,0x78,0x00},
    {0x38,0x6C,0xC6,0xFE,0xC6,0xC6,0xC6,0x00},
    {0xFC,0x66,0x66,0x7C,0x66,0x66,0xFC,0x00},
    {0x3C,0x66,0xC0,0xC0,0xC0,0x66,0x3C,0x00},
    {0xF8,0x6C,0x66,0x66,0x66,0x6C,0xF8,0x00},
    {0xFE,0x62,0x68,0x78,0x68,0x62,0xFE,0x00},
    {0xFE,0x62,0x68,0x78,0x68,0x60,0xF0,0x00},
    {0x3C,0x66,0xC0,0xC0,0xCE,0x66,0x3E,0x00},
    {0xC6,0xC6,0xC6,0xFE,0xC6,0xC6,0xC6,0x00},
    {0x7E,0x18,0x18,0x18,0x18,0x18,0x7E,0x00},
    {0x1E,0x0C,0x0C,0x0C,0xCC,0xCC,0x78,0x00},
    {0xE6,0x66,0x6C,0x78,0x6C,0x66,0xE6,0x00},
    {0xF0,0x60,0x60,0x60,0x62,0x66,0xFE,0x00},
    {0xC6,0xEE,0xFE,0xD6,0xC6,0xC6,0xC6,0x00},
    {0xC6,0xE6,0xF6,0xDE,0xCE,0xC6,0xC6,0x00},
    {0x38,0x6C,0xC6,0xC6,0xC6,0x6C,0x38,0x00},
    {0xFC,0x66,0x66,0x7C,0x60,0x60,0xF0,0x00},
    {0x7C,0xC6,0xC6,0xC6,0xD6,0x7C,0x0E,0x00},
    {0xFC,0x66,0x66,0x7C,0x6C,0x66,0xE6,0x00},
    {0x7C,0xC6,0xE0,0x7C,0x0E,0xC6,0x7C,0x00},
    {0x7E,0x5A,0x18,0x18,0x18,0x18,0x3C,0x00},
    {0xC6,0xC6,0xC6,0xC6,0xC6,0xC6,0x7C,0x00},
    {0xC6,0xC6,0xC6,0xC6,0xC6,0x6C,0x38,0x00},
    {0xC6,0xC6,0xC6,0xD6,0xFE,0xEE,0xC6,0x00},
    {0xC6,0xC6,0x6C,0x38,0x6C,0xC6,0xC6,0x00},
    {0x66,0x66,0x66,0x3C,0x18,0x18,0x3C,0x00},
    {0xFE,0xC6,0x8C,0x18,0x32,0x66,0xFE,0x00},
    {0x3C,0x30,0x30,0x30,0x30,0x30,0x3C,0x00},
    {0xC0,0x60,0x30,0x18,0x0C,0x06,0x02,0x00},
    {0x3C,0x0C,0x0C,0x0C,0x0C,0x0C,0x3C,0x00},
    {0x10,0x38,0x6C,0xC6,0x00,0x00,0x00,0x00},
    {0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xFF},
    {0x30,0x18,0x0C,0x00,0x00,0x00,0x00,0x00},
    {0x00,0x00,0x78,0x0C,0x7C,0xCC,0x76,0x00},
    {0xE0,0x60,0x7C,0x66,0x66,0x66,0xDC,0x00},
    {0x00,0x00,0x7C,0xC6,0xC0,0xC6,0x7C,0x00},
    {0x1C,0x0C,0x7C,0xCC,0xCC,0xCC,0x76,0x00},
    {0x00,0x00,0x7C,0xC6,0xFE,0xC0,0x7C,0x00},
    {0x3C,0x66,0x60,0xF8,0x60,0x60,0xF0,0x00},
    {0x00,0x00,0x76,0xCC,0xCC,0x7C,0x0C,0xF8},
    {0xE0,0x60,0x6C,0x76,0x66,0x66,0xE6,0x00},
    {0x18,0x00,0x38,0x18,0x18,0x18,0x3C,0x00},
    {0x06,0x00,0x0E,0x06,0x06,0x06,0x66,0x3C},
    {0xE0,0x60,0x66,0x6C,0x78,0x6C,0xE6,0x00},
    {0x38,0x18,0x18,0x18,0x18,0x18,0x3C,0x00},
    {0x00,0x00,0xEC,0xFE,0xD6,0xC6,0xC6,0x00},
    {0x00,0x00,0xDC,0x66,0x66,0x66,0x66,0x00},
    {0x00,0x00,0x7C,0xC6,0xC6,0xC6,0x7C,0x00},
    {0x00,0x00,0xDC,0x66,0x66,0x7C,0x60,0xF0},
    {0x00,0x00,0x76,0xCC,0xCC,0x7C,0x0C,0x1E},
    {0x00,0x00,0xDC,0x76,0x60,0x60,0xF0,0x00},
    {0x00,0x00,0x7E,0xC0,0x7C,0x06,0xFC,0x00},
    {0x30,0x30,0xFC,0x30,0x30,0x36,0x1C,0x00},
    {0x00,0x00,0xCC,0xCC,0xCC,0xCC,0x76,0x00},
    {0x00,0x00,0xC6,0xC6,0xC6,0x6C,0x38,0x00},
    {0x00,0x00,0xC6,0xC6,0xD6,0xFE,0x6C,0x00},
    {0x00,0x00,0xC6,0x6C,0x38,0x6C,0xC6,0x00},
    {0x00,0x00,0xC6,0xC6,0xC6,0x7E,0x06,0xFC},
    {0x00,0x00,0xFE,0x8C,0x18,0x32,0xFE,0x00},
    {0x0E,0x18,0x18,0x70,0x18,0x18,0x0E,0x00},
    {0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x00},
    {0x70,0x18,0x18,0x0E,0x18,0x18,0x70,0x00},
    {0x76,0xDC,0x00,0x00,0x00,0x00,0x00,0x00},
};

static void write_pixel(int x, int y, int r, int g, int b)
{
    char *fb = (char *)GFX_BASE;
    int off = (y * GFX_WIDTH + x) * 3;
    if (x < 0 || x >= GFX_WIDTH || y < 0 || y >= GFX_HEIGHT) return;
    fb[off] = (char)r;
    fb[off + 1] = (char)g;
    fb[off + 2] = (char)b;
}

static void read_pixel(int x, int y, int *r, int *g, int *b)
{
    char *fb = (char *)GFX_BASE;
    int off = (y * GFX_WIDTH + x) * 3;
    if (x < 0 || x >= GFX_WIDTH || y < 0 || y >= GFX_HEIGHT) { *r = 0; *g = 0; *b = 0; return; }
    *r = (char)fb[off];
    *g = (char)fb[off + 1];
    *b = (char)fb[off + 2];
}

static void palette_to_rgb(int color, int *r, int *g, int *b)
{
    char *pal = (char *)PAL_BASE;
    if (color < 0 || color > 15)
    {
        *r = 0; *g = 0; *b = 0;
        return;
    }
    *r = pal[color * 3];
    *g = pal[color * 3 + 1];
    *b = pal[color * 3 + 2];
}

static int rgb_to_palette(int r, int g, int b)
{
    char *pal = (char *)PAL_BASE;
    int best = 0;
    int best_dist = 999999;
    int i;
    for (i = 0; i < 16; i++)
    {
        int dr = r - (int)pal[i * 3];
        int dg = g - (int)pal[i * 3 + 1];
        int db = b - (int)pal[i * 3 + 2];
        int dist = dr * dr + dg * dg + db * db;
        if (dist < best_dist) { best_dist = dist; best = i; }
    }
    return best;
}

void initgraph(int *gd, int *gm, const char *path)
{
    int i, j;
    char *fb = (char *)GFX_BASE;
    _graph_ok = 1;
    _current_color = WHITE;
    _bg_color = BLACK;
    _cp_x = 0;
    _cp_y = 0;
    for (i = 0; i < GFX_HEIGHT; i++)
        for (j = 0; j < GFX_WIDTH; j++)
        {
            int off = (i * GFX_WIDTH + j) * 3;
            fb[off] = 0;
            fb[off + 1] = 0;
            fb[off + 2] = 0;
        }
}

void closegraph(void)
{
    _graph_ok = 0;
}

int graphresult(void)
{
    return 0;
}

char *grapherrormsg(int code)
{
    switch (code) {
        case 0: return "No error";
        case -1: return "Graphics not initialized";
        case -2: return "Invalid driver";
        case -3: return "Invalid mode";
        default: return "Unknown error";
    }
}

void setcolor(int color)
{
    _current_color = color;
}

void setbkcolor(int color)
{
    _bg_color = color;
}

int getcolor(void)
{
    return _current_color;
}

int getmaxx(void)
{
    return GFX_WIDTH - 1;
}

int getmaxy(void)
{
    return GFX_HEIGHT - 1;
}

void putpixel(int x, int y, int color)
{
    int r, g, b;
    if (_vp_clip && (x < _vp_x1 || x > _vp_x2 || y < _vp_y1 || y > _vp_y2)) return;
    palette_to_rgb(color, &r, &g, &b);
    write_pixel(x, y, r, g, b);
}

int getpixel(int x, int y)
{
    int r, g, b;
    if (x < 0 || x >= GFX_WIDTH || y < 0 || y >= GFX_HEIGHT) return 0;
    read_pixel(x, y, &r, &g, &b);
    return rgb_to_palette(r, g, b);
}

void moveto(int x, int y)
{
    _cp_x = x;
    _cp_y = y;
}

void moverel(int dx, int dy)
{
    _cp_x += dx;
    _cp_y += dy;
}

static int abs_int(int x)
{
    return x < 0 ? -x : x;
}

static void draw_line_low(int x0, int y0, int x1, int y1, int color)
{
    int dx = x1 - x0;
    int dy = y1 - y0;
    int yi = 1;
    int D, x, y;
    if (dy < 0) { yi = -1; dy = -dy; }
    D = 2 * dy - dx;
    y = y0;
    for (x = x0; x <= x1; x++)
    {
        putpixel(x, y, color);
        if (D > 0) { y += yi; D -= 2 * dx; }
        D += 2 * dy;
    }
}

static void draw_line_high(int x0, int y0, int x1, int y1, int color)
{
    int dx = x1 - x0;
    int dy = y1 - y0;
    int xi = 1;
    int D, x, y;
    if (dx < 0) { xi = -1; dx = -dx; }
    D = 2 * dx - dy;
    x = x0;
    for (y = y0; y <= y1; y++)
    {
        putpixel(x, y, color);
        if (D > 0) { x += xi; D -= 2 * dy; }
        D += 2 * dx;
    }
}

void line(int x1, int y1, int x2, int y2)
{
    int color = _current_color;
    if (x1 == x2 && y1 == y2) { putpixel(x1, y1, color); return; }
    if (abs_int(y2 - y1) < abs_int(x2 - x1))
    {
        if (x1 > x2) { int t; t = x1; x1 = x2; x2 = t; t = y1; y1 = y2; y2 = t; }
        draw_line_low(x1, y1, x2, y2, color);
    }
    else
    {
        if (y1 > y2) { int t; t = x1; x1 = x2; x2 = t; t = y1; y1 = y2; y2 = t; }
        draw_line_high(x1, y1, x2, y2, color);
    }
}

void lineto(int x, int y)
{
    line(_cp_x, _cp_y, x, y);
    _cp_x = x;
    _cp_y = y;
}

void linerel(int dx, int dy)
{
    lineto(_cp_x + dx, _cp_y + dy);
}

void circle(int xc, int yc, int r)
{
    int x = 0, y = r;
    int d = 3 - 2 * r;
    int color = _current_color;
    while (y >= x)
    {
        putpixel(xc + x, yc + y, color);
        putpixel(xc - x, yc + y, color);
        putpixel(xc + x, yc - y, color);
        putpixel(xc - x, yc - y, color);
        putpixel(xc + y, yc + x, color);
        putpixel(xc - y, yc + x, color);
        putpixel(xc + y, yc - x, color);
        putpixel(xc - y, yc - x, color);
        x++;
        if (d > 0) { y--; d = d + 4 * (x - y) + 10; }
        else d = d + 4 * x + 6;
    }
}

void arc(int x, int y, int stangle, int endangle, int radius)
{
    int xp = 0, yp = radius;
    int d = 3 - 2 * radius;
    int xa, ya;
    int color = _current_color;

    while (yp >= xp)
    {
        int pts[8][2] = {
            {xp, yp}, {-xp, yp}, {xp, -yp}, {-xp, -yp},
            {yp, xp}, {-yp, xp}, {yp, -xp}, {-yp, -xp}
        };
        int i;
        for (i = 0; i < 8; i++)
        {
            xa = x + pts[i][0];
            ya = y + pts[i][1];
            double angle;
            if (xa == x)
                angle = (ya > y) ? 90.0 : 270.0;
            else
            {
                angle = atan2_approx(ya - y, xa - x) * 180.0 / 3.14159265;
            }
            if (angle < 0) angle += 360.0;
            if (stangle <= endangle)
            {
                if (angle >= stangle && angle <= endangle)
                    putpixel(xa, ya, color);
            }
            else
            {
                if (angle >= stangle || angle <= endangle)
                    putpixel(xa, ya, color);
            }
        }
        xp++;
        if (d > 0) { yp--; d = d + 4 * (xp - yp) + 10; }
        else d = d + 4 * xp + 6;
    }
}

void ellipse(int xc, int yc, int stangle, int endangle, int xradius, int yradius)
{
    int x = 0, y = yradius;
    int rx = xradius, ry = yradius;
    int color = _current_color;
    double d1, d2;

    d1 = (double)(ry * ry) - (double)(rx * rx) * ry + (double)(rx * rx) * 0.25;

    while (2 * (double)(ry * ry) * x <= 2 * (double)(rx * rx) * y)
    {
        putpixel(xc + x, yc + y, color);
        putpixel(xc - x, yc + y, color);
        putpixel(xc + x, yc - y, color);
        putpixel(xc - x, yc - y, color);
        if (d1 < 0)
        {
            x++;
            d1 += 2 * (double)(ry * ry) * x + (double)(ry * ry);
        }
        else
        {
            x++;
            y--;
            d1 += 2 * (double)(ry * ry) * x - 2 * (double)(rx * rx) * y + (double)(ry * ry);
        }
    }

    d2 = (double)(ry * ry) * (x + 0.5) * (x + 0.5) + (double)(rx * rx) * (y - 1) * (y - 1) - (double)(rx * rx) * (double)(ry * ry);

    while (y >= 0)
    {
        putpixel(xc + x, yc + y, color);
        putpixel(xc - x, yc + y, color);
        putpixel(xc + x, yc - y, color);
        putpixel(xc - x, yc - y, color);
        if (d2 > 0)
        {
            y--;
            d2 += (double)(rx * rx) - 2 * (double)(rx * rx) * y;
        }
        else
        {
            y--;
            x++;
            d2 += 2 * (double)(ry * ry) * x - 2 * (double)(rx * rx) * y + (double)(rx * rx);
        }
    }
}

void rectangle(int left, int top, int right, int bottom)
{
    int color = _current_color;
    line(left, top, right, top);
    line(right, top, right, bottom);
    line(right, bottom, left, bottom);
    line(left, bottom, left, top);
}

void bar(int left, int top, int right, int bottom)
{
    int x, y, color = _fill_color;
    for (y = top; y <= bottom; y++)
        for (x = left; x <= right; x++)
            putpixel(x, y, color);
}

void bar3d(int left, int top, int right, int bottom, int depth, int topflag)
{
    int color = _current_color;
    bar(left, top, right, bottom);
    setcolor(color);
    rectangle(left, top, right, bottom);
    line(right + depth, top - depth, right + depth, bottom - depth);
    line(right + depth, bottom - depth, right, bottom);
    line(right + depth, top - depth, right, top);
    if (topflag)
        line(left, top, left + depth, top - depth);
    setcolor(color);
}

void setfillstyle(int pattern, int color)
{
    _fill_pattern = pattern;
    _fill_color = color;
}

void floodfill(int x, int y, int border)
{
    int old_color = getpixel(x, y);
    if (old_color == border) return;

    {
        int stack[4096];
        int sp = 0;
        int sx, sy, c;

        stack[sp++] = x;
        stack[sp++] = y;

        while (sp > 0)
        {
            sy = stack[--sp];
            sx = stack[--sp];
            if (sx < 0 || sx >= GFX_WIDTH || sy < 0 || sy >= GFX_HEIGHT) continue;
            if (_vp_clip && (sx < _vp_x1 || sx > _vp_x2 || sy < _vp_y1 || sy > _vp_y2)) continue;
            c = getpixel(sx, sy);
            if (c == border || c != old_color) continue;
            putpixel(sx, sy, _fill_color);
            stack[sp++] = sx + 1; stack[sp++] = sy;
            stack[sp++] = sx - 1; stack[sp++] = sy;
            stack[sp++] = sx; stack[sp++] = sy + 1;
            stack[sp++] = sx; stack[sp++] = sy - 1;
        }
    }
}

void setlinestyle(int linestyle, int pattern, int thickness)
{
}

void outtextxy(int x, int y, const char *str)
{
    int color = _current_color;
    int r, g, b;
    palette_to_rgb(color, &r, &g, &b);

    while (*str)
    {
        char c = (char)*str;
        int row, col;
        if (c < 32 || c > 127) { str++; x += 8; continue; }
        c -= 32;
        for (row = 0; row < 8; row++)
        {
            char font_byte = _font8x8[c][row];
            for (col = 0; col < 8; col++)
            {
                if (font_byte & (0x80 >> col))
                {
                    int px = x + col;
                    int py = y + row;
                    if (px >= 0 && px < GFX_WIDTH && py >= 0 && py < GFX_HEIGHT)
                        write_pixel(px, py, r, g, b);
                }
            }
        }
        x += 8;
        str++;
    }
}

void outtext(const char *str)
{
    outtextxy(_cp_x, _cp_y, str);
}

void settextstyle(int font, int direction, int charsize)
{
}

void setviewport(int x1, int y1, int x2, int y2, int clip)
{
    _vp_x1 = x1; _vp_y1 = y1;
    _vp_x2 = x2; _vp_y2 = y2;
    _vp_clip = clip;
}

void clearviewport(void)
{
    int i, j;
    char *fb = (char *)GFX_BASE;
    for (i = _vp_y1; i <= _vp_y2 && i < GFX_HEIGHT; i++)
        for (j = _vp_x1; j <= _vp_x2 && j < GFX_WIDTH; j++)
        {
            int off = (i * GFX_WIDTH + j) * 3;
            fb[off] = 0; fb[off + 1] = 0; fb[off + 2] = 0;
        }
}

int imagesize(int x1, int y1, int x2, int y2)
{
    int w = x2 - x1 + 1;
    int h = y2 - y1 + 1;
    return w * h * 3 + 8;
}

void getimage(int x1, int y1, int x2, int y2, void *buf)
{
    char *fb = (char *)GFX_BASE;
    char *b = (char *)buf;
    int w = x2 - x1 + 1;
    int h = y2 - y1 + 1;
    int x, y;

    *(int *)b = w; b += 4;
    *(int *)b = h; b += 4;

    for (y = y1; y <= y2; y++)
        for (x = x1; x <= x2; x++)
        {
            int off = (y * GFX_WIDTH + x) * 3;
            *b++ = fb[off];
            *b++ = fb[off + 1];
            *b++ = fb[off + 2];
        }
}

void putimage(int x, int y, void *buf, int op)
{
    char *fb = (char *)GFX_BASE;
    char *b = (char *)buf;
    int w = *(int *)b; b += 4;
    int h = *(int *)b; b += 4;
    int px, py;

    for (py = 0; py < h; py++)
        for (px = 0; px < w; px++)
        {
            int fx = x + px;
            int fy = y + py;
            if (fx >= 0 && fx < GFX_WIDTH && fy >= 0 && fy < GFX_HEIGHT)
            {
                int off = (fy * GFX_WIDTH + fx) * 3;
                if (op == COPY_PUT)
                {
                    fb[off] = *b++;
                    fb[off + 1] = *b++;
                    fb[off + 2] = *b++;
                }
                else
                {
                    fb[off] ^= *b++;
                    fb[off + 1] ^= *b++;
                    fb[off + 2] ^= *b++;
                }
            }
            else b += 3;
        }
}

void drawpoly(int numpoints, int *polypoints)
{
    int i;
    int color = _current_color;
    for (i = 0; i < numpoints - 1; i++)
        line(polypoints[i * 2], polypoints[i * 2 + 1],
             polypoints[(i + 1) * 2], polypoints[(i + 1) * 2 + 1]);
    line(polypoints[(numpoints - 1) * 2], polypoints[(numpoints - 1) * 2 + 1],
         polypoints[0], polypoints[1]);
}

void fillpoly(int numpoints, int *polypoints)
{
    int i, j;
    int color = _fill_color;
    int min_y = 9999, max_y = -9999;
    int x, y;

    for (i = 0; i < numpoints; i++)
    {
        int py = polypoints[i * 2 + 1];
        if (py < min_y) min_y = py;
        if (py > max_y) max_y = py;
    }

    for (y = min_y; y <= max_y; y++)
    {
        int inter[100];
        int inter_cnt = 0;

        for (i = 0; i < numpoints; i++)
        {
            int j = (i + 1) % numpoints;
            int x1 = polypoints[i * 2];
            int y1 = polypoints[i * 2 + 1];
            int x2 = polypoints[j * 2];
            int y2 = polypoints[j * 2 + 1];

            if ((y1 <= y && y2 > y) || (y2 <= y && y1 > y))
            {
                int x_inter = x1 + (y - y1) * (x2 - x1) / (y2 - y1);
                if (inter_cnt < 100) inter[inter_cnt++] = x_inter;
            }
        }

        for (i = 0; i < inter_cnt - 1; i++)
            for (j = 0; j < inter_cnt - 1 - i; j++)
                if (inter[j] > inter[j + 1])
                {
                    int t = inter[j];
                    inter[j] = inter[j + 1];
                    inter[j + 1] = t;
                }

        for (i = 0; i + 1 < inter_cnt; i += 2)
            for (x = inter[i]; x <= inter[i + 1]; x++)
                putpixel(x, y, color);
    }

    drawpoly(numpoints, polypoints);
}

void sector(int x, int y, int stangle, int endangle, int xradius, int yradius)
{
    int color = _fill_color;
    int border = _current_color;
    double st = stangle * 3.14159265 / 180.0;
    double en = endangle * 3.14159265 / 180.0;
    double a;
    int px, py;

    if (stangle > endangle) { a = st; st = en; en = a; }

    for (a = st; a <= en; a += 0.01)
    {
        px = x + (int)(xradius * cos(a));
        py = y + (int)(yradius * sin(a));
        line(x, y, px, py);
    }

    setcolor(border);
    for (a = st; a <= en; a += 0.01)
    {
        px = x + (int)(xradius * cos(a));
        py = y + (int)(yradius * sin(a));
        putpixel(px, py, border);
    }
}

void pieslice(int x, int y, int stangle, int endangle, int radius)
{
    sector(x, y, stangle, endangle, radius, radius);
}

static double atan2_approx(double y, double x)
{
    if (x > 0.0) return atan_approx(y / x);
    if (x < 0.0)
    {
        if (y >= 0.0) return atan_approx(y / x) + 3.14159265;
        return atan_approx(y / x) - 3.14159265;
    }
    if (y > 0.0) return 3.14159265 / 2.0;
    if (y < 0.0) return -3.14159265 / 2.0;
    return 0.0;
}

static double atan_approx(double x)
{
    double x2 = x * x;
    return x - x2 * x / 3.0 + x2 * x2 * x / 5.0 - x2 * x2 * x2 * x / 7.0 + x2 * x2 * x2 * x2 * x / 9.0;
}
