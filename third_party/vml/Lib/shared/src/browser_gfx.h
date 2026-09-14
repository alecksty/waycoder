/* browser_gfx.h - VML Browser Graphics Library
 * Dedicated Canvas2D drawing via command-buffer protocol.
 * Programs call these functions. JS reads the buffer and executes Canvas2D ops.
 */

#ifndef BROWSER_GFX_H
#define BROWSER_GFX_H

void browser_clear(int r, int g, int b);
void browser_color(int r, int g, int b);
void browser_fill_polygon(int count, int* points);
void browser_draw_polygon(int count, int* points);
void browser_flush(void);

#endif
