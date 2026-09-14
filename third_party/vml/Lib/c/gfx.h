/* gfx.h - VGA Graphics functions (QBASIC-style)
 * SYSCALL 80-82, 200-203 + software drawing primitives
 */

#ifndef _GFX_H
#define _GFX_H

#param lib("graphics")

/* Basic VGA (SYSCALL-based) */
void vga_clear(void);
void vga_putchar(int x, int y, char c, int color);
void vga_puts(int x, int y, const char *s, int color);

/* Screen mode & info */
int  gfx_screen(int mode);              /* <0:query, >=0:set */
int  gfx_width(void);
int  gfx_height(void);
int  gfx_depth(void);

/* Palette (RGB each 0-63) */
void gfx_palette(int index, int r, int g, int b);
int  gfx_palette_get(int index);

/* Pixel ops */
void gfx_pset(int x, int y, int color);
int  gfx_point(int x, int y);
void gfx_cls(void);
void gfx_cls_color(int color);

/* Drawing primitives */
void gfx_line(int x1, int y1, int x2, int y2, int color);
void gfx_rect(int x1, int y1, int x2, int y2, int color);
void gfx_rect_fill(int x1, int y1, int x2, int y2, int color);
void gfx_circle(int cx, int cy, int r, int color);
void gfx_circle_fill(int cx, int cy, int r, int color);
void gfx_ellipse(int cx, int cy, int rx, int ry, int color);
void gfx_ellipse_fill(int cx, int cy, int rx, int ry, int color);
void gfx_arc(int cx, int cy, int r, int start_angle, int end_angle, int color);
void gfx_sector(int cx, int cy, int r, int start_angle, int end_angle, int color);
void gfx_sector_fill(int cx, int cy, int r, int start_angle, int end_angle, int color);

/* Text with built-in 8x16 bitmap font */
void gfx_print(int x, int y, const char *text, int color);
void gfx_print_scale(int x, int y, const char *text, int color, int scale);

/* Flood fill */
void gfx_flood_fill(int x, int y, int fill_color, int border_color);

/* Advanced graphics (SYSCALL-based) */
int  gfx_screenshot(void);
int  gfx_put_image(int x, int y, int w, int h, const void *data);
int  gfx_get_image(int x, int y, int w, int h, void *buffer);
int  gfx_viewport(int x1, int y1, int x2, int y2);

#endif /* _GFX_H */
