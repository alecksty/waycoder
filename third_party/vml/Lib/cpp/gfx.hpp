// VML VGA 图形扩展库 (QBASIC风格) — C++
// 需显式 #include "gfx.hpp"
#ifndef VML_GFX_HPP
#define VML_GFX_HPP

namespace vml::gfx {

// Basic VGA (SYSCALL)
inline void clear()                                    { asm("SYSCALL 80"); }
inline void putchar(int x, int y, char c, int color)   { asm("SYSCALL 81"); }
inline void puts(int x, int y, const char* s, int color) { asm("SYSCALL 82"); }

// Screen mode & info
int  screen(int mode);
int  width();
int  height();
int  depth();

// Palette
void palette(int idx, int r, int g, int b);
int  palette_get(int idx);

// Pixel ops
void pset(int x, int y, int color);
int  point(int x, int y);
void cls();
void cls_color(int color);

// Drawing
void line(int x1,int y1,int x2,int y2,int color);
void rect(int x1,int y1,int x2,int y2,int color);
void rect_fill(int x1,int y1,int x2,int y2,int color);
void circle(int cx,int cy,int r,int color);
void circle_fill(int cx,int cy,int r,int color);
void arc(int cx,int cy,int r,int sa,int ea,int color);
void sector(int cx,int cy,int r,int sa,int ea,int color);

// Text (8x16 font)
void print(int x,int y,const char* text,int color);
void print_scale(int x,int y,const char* text,int color,int scale);

// Flood fill
void flood_fill(int x,int y,int fc,int bc);

// Advanced (SYSCALL)
inline int screenshot()                                  { asm("SYSCALL 200"); return 0; }
inline int put_image(int x, int y, int w, int h, const void* data) { asm("SYSCALL 201"); return 0; }
inline int get_image(int x, int y, int w, int h, void* buf) { asm("SYSCALL 202"); return 0; }
inline int viewport(int x1, int y1, int x2, int y2)      { asm("SYSCALL 203"); return 0; }

} // namespace vml::gfx
#endif
