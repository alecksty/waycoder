# Drawing

Points, lines, surfaces, polygons, paths, gradients, images.

> For the full list see "[UI development](help:vml/ui)". Each call below comes with a
> one-line usage note and an example; signatures come from `Lib/c/waycoder_ui.h` (the authoritative source).

### `ui_brush_linear(int color_a, int color_b, int x1, int y1, int x2, int y2)`
Builds a **linear gradient brush**. The geometry is in **thousandths** (0..1000) and is relative to **the shape's own bounding box**: `0,0,1000,0` = left to right.
```c
int b = ui_brush_linear(0xFFFF3020, 0xFF2050FF, 0, 0, 1000, 0);
ui_set_fill(b);
```
### `ui_brush_named(char* gradId)`
Looks up a gradient already defined with `ui_gradient` **by name** and returns a handle.
Note: it is a **reference**: it does not define anything itself, so it depends on the program having called `ui_gradient` in the same frame - and `ui_clear` clears that definition.
```c
ui_gradient("sky", 0, 0xFF2E6FC4, 0xFFBEE3F7, 0, 0, 0, 1000);
int b = ui_brush_named("sky");
ui_set_fill(b);
```
### `ui_brush_radial(int color_a, int color_b, int cx, int cy, int r)`
Builds a **radial gradient brush** (center outward). `cx cy r` are in thousandths as well; `500,500,500` = centered.
```c
int b = ui_brush_radial(0xFFFFE060, 0xFF204020, 500, 500, 500);
ui_set_fill(b);
```
### `ui_brush_solid(int color)`
Builds a **solid-color brush**. Returns a handle (at least 1; 0 = failure) to hand to `ui_set_fill` / `ui_set_pen`.
Note: passing a color **directly** works too (a color is just a brush with a single stop); this function is for the case where you "build a batch of brushes up front and swap between them later".
```c
int b = ui_brush_solid(0xFF2A3346);
ui_set_fill(b);
```
### `ui_circle(int cx, int cy, int r, int color, int fill, int lw)`
Draws a circle; a non-zero `fill` fills it.
```c
ui_circle(100, 100, 30, 0xFF00FF00, 1, 2);
```
### `ui_circle_grad(int cx, int cy, int r, char* grad)`
A gradient circle (give the gradient a name with `ui_gradient` first).
```c
ui_gradient("ball", 1, 0xFFFFFFFF, 0xFF2E6FC4, 500, 500, 500, 0);
ui_circle_grad(cx, cy, r, "ball");
```
### `ui_clear(int color)`
Fills the whole screen with one color (call it at the start of each frame). Colors are always `0xAARRGGBB`.
```c
ui_clear(0xFF101020);   /* dark blue background */
```
### `ui_draw_circle(int cx, int cy, int r)`
Draws a circle with the current brush.
```c
ui_draw_circle(100, 100, 40);
```
### `ui_draw_ellipse(int cx, int cy, int rx, int ry)`
Draws an ellipse with the current brush.
```c
ui_draw_ellipse(100, 100, 50, 30);
```
### `ui_draw_heart(int cx, int cy, int size)`
Draws a heart with the current brush.
```c
ui_set_fill(0xFFFF4D6D);
ui_draw_heart(100, 100, 60);
```
### `ui_draw_line(int x1, int y1, int x2, int y2)`
Draws a straight line with the current **pen**.
```c
ui_set_pen(0xFFE06C50, 3, 0, 0, 0);
ui_draw_line(10, 10, 120, 60);
```
### `ui_draw_path(char* d)`
Draws an SVG path with the current brush (`M L C Q A Z`; upper case is absolute, lower case is relative; multiple subpaths punch holes by the even-odd rule).
```c
ui_set_fill(0xFF4ADE80);
ui_draw_path("M 20 80 L 60 20 L 100 80 Z");
```
### `ui_draw_pie(int cx, int cy, int r, int a0, int a1)`
Draws a pie slice with the current brush: radius / start angle / end angle (degrees).
```c
ui_set_fill(0xFFE06C50);
ui_draw_pie(100, 100, 50, 0, 120);
```
### `ui_draw_poly(int* pts, int count, int close)`
Draws a polygon (`close=1`, closed automatically) or a polyline (`close=0`) with the current brush. `pts` holds one point per two ints; `count` is the **number of points**.
```c
int tri[6];
tri[0]=60; tri[1]=10; tri[2]=90; tri[3]=70; tri[4]=30; tri[5]=70;
ui_set_fill(0xFF4ADE80);
ui_draw_poly(tri, 3, 1);
```
### `ui_draw_rect(int x, int y, int w, int h, int radius)`
Draws a rectangle with the **current brush** (`radius > 0` gives rounded corners).
```c
ui_set_fill(0xFF4ADE80);
ui_draw_rect(20, 20, 120, 60, 8);
```
### `ui_draw_regular(int cx, int cy, int r, int n, int rot)`
Draws a regular polygon with the current brush: radius / number of sides / rotation (degrees).
```c
ui_set_fill(0xFF4ADE80);
ui_draw_regular(100, 100, 50, 6, 0);
```
### `ui_draw_ring(int cx, int cy, int r_out, int r_in)`
Draws a ring with the current brush (outer radius / inner radius; the hole in the middle is punched by the even-odd rule).
```c
ui_set_fill(0xFF4A90D9);
ui_draw_ring(100, 100, 50, 30);
```
### `ui_draw_star(int cx, int cy, int r_out, int r_in, int points, int rot)`
Draws a star with the current brush: outer radius / inner radius / number of points / rotation (degrees).
```c
ui_set_fill(0xFFFFD700);
ui_draw_star(100, 100, 50, 22, 5, 0);
```
### `ui_draw_text(int x, int y, char* s)`
Draws one line of text with the **current text attributes** (`ui_set_font`).
```c
ui_set_font(24, 0, 0xFFFFFFFF, VML_ANCHOR_CENTER);
ui_draw_text(100, 40, "Hello");
```
### `ui_ellipse(int cx, int cy, int rx, int ry, int color, int fill, int lw)`
Draws an ellipse (`rx` / `ry` are the two radii).
```c
ui_ellipse(100, 100, 50, 30, 0xFFFFFF00, 1, 2);
```
### `ui_ellipse_grad(int cx, int cy, int rx, int ry, char* grad)`
An ellipse filled with a gradient. It belongs with `ui_rect_grad` / `ui_circle_grad` (that batch **missed the ellipse** at the time).
```c
ui_gradient("ball", 1, 0xFFFFFFFF, 0xFF2E6FC4, 500, 500, 500, 0);
ui_ellipse_grad(cx, cy, rx, ry, "ball");
```
### `ui_gradient(char* gradId, int radial, int color_a, int color_b, int a1, int a2, int a3, int a4)`
Defines a gradient brush under a **name** (a string id); `ui_rect_grad` / `ui_circle_grad` / `ui_path` then reference it by that name.
The geometry is a **normalized integer 0..1000** (thousandths): for a linear gradient pass x1,y1,x2,y2; for a radial one pass cx,cy,r (the fourth is ignored).
Note: passing pixels collapses the gradient into a solid color.
```c
ui_gradient("sky", 0, 0xFF2E6FC4, 0xFFBEE3F7, 0, 0, 0, 1000);
ui_rect_grad(0, 0, w, h, "sky", 0);
```
### `ui_icon(int x, int y, char* name, int size, int color)`
Draws one of the built-in icons (looked up by name, so you do not have to draw it yourself).
```c
ui_icon(10, 10, "star", 24, 0xFFFFD700);
```
### `ui_image(int x, int y, char* path, int w, int h)`
Draws an image (PNG / JPG / BMP) at the given position; pass 0 for `w` / `h` to use the original size.
```c
ui_image(20, 20, "~/pics/logo.png", 0, 0);
```
### `ui_line(int x1, int y1, int x2, int y2, int color, int lw)`
Draws a line; `lw` is the line width.
```c
ui_line(0, 0, 100, 100, 0xFFFF0000, 2);
```
### `ui_path(char* d, int stroke, int width, int fill, char* grad, int cap, int dash)`
Draws using **SVG path syntax** (`M L H V C S Q T A Z`; upper case is absolute, lower case is relative).
`stroke` is the outline color (0 = no outline), `width` the line width, `fill` the fill color (0 = no fill),
`grad` the gradient name (if given, it is used for the fill), `cap` 0 butt / 1 round / 2 square, `dash` 0/1 dashed.
```c
ui_path("M10,50 Q60,0 110,50 T210,50", 0xFFFF00FF, 3, 0, "", 1, 0);
```
### `ui_pixel(int x, int y, int color)`
Draws a single point.
```c
ui_pixel(10, 20, 0xFFFFFFFF);
```
### `ui_polygon(int* pts, int count, int fill, int stroke, int width, char* grad)`
Draws a polygon (closed automatically). `pts` is an int array with **one point per two ints**; `count` is the **number of points**.
`fill` / `stroke` are both **colors** (not on/off switches), `width` is the outline width and `grad` takes a gradient name or an empty string.
Note: **the vertices must be a named array** - a `ui_polygon((int[]){...})` compound literal is not supported on this front end, and it does not report an error either.
```c
int pts[] = {10,10, 110,10, 60,90};
ui_polygon(pts, 3, 0xFFFF8800, 0xFFFFFFFF, 2, "");
```
### `ui_polyline(int* pts, int count, int stroke, int width, char* grad)`
A polyline (not closed). The arguments mean the same as in `ui_polygon` (`stroke` is a color, `width` the line width).
```c
int pts[] = {10,10, 50,60, 90,20};
ui_polyline(pts, 3, 0xFFFFFFFF, 2, "");
```
### `ui_present(void)`
**This frame is finished.** The single most important call in the whole loop - without it the screen never updates.
(The host also uses it to decide "it is time to put a frame on screen", so it must go at the end of every frame, after all shapes have been drawn.)
```c
ui_clear(0xFF000000);
ui_text(10, 10, "One frame", 0xFFFFFFFF, 16, 0);
ui_present();
```
### `ui_rect(int x, int y, int w, int h, int color, int fill, int lw, int radius)`
Draws a rectangle. A non-zero `fill` fills it; `radius` is the corner radius.
```c
ui_rect(20, 30, 120, 60, 0xFF3366FF, 1, 2, 8);   /* filled, rounded corners */
```
### `ui_rect_grad(int x, int y, int w, int h, char* grad, int radius)`
A rectangle with a gradient (define the gradient with `ui_gradient` first).
```c
ui_rect_grad(0, 0, 200, 100, g, 8);
```
### `ui_set_fill(int brush)`
Sets the **fill brush**. You can pass either a brush handle or a color; 0 = no fill (hollow).
```c
ui_set_fill(0xFF2A3346);        /* pass a color directly */
ui_draw_rect(20, 20, 120, 60, 8);
```
### `ui_set_pen(int brush, int width, int cap, int dash, int arrow)`
Sets the **pen** (the outline) = brush + line width + cap + dash + arrow. Pass 0 = no outline.
Use `VML_CAP_BUTT/ROUND/SQUARE` for the cap and `VML_ARROW_*` for the arrow.
Note: for now **the pen only supports solid colors** (gradient outlines are not implemented yet; passing a gradient handle logs one warning and falls back to its first color).
```c
ui_set_fill(0xFF2A3346);
ui_set_pen(0xFFF2F6FA, 3, VML_CAP_ROUND, 0, VML_ARROW_NONE);
ui_draw_rect(20, 20, 120, 60, 8);
```
### `ui_set_text_brush(int brush)`
Sets the **text brush** (used together with `ui_set_font` + `ui_draw_text`). Pass 0 = go back to the color given to `ui_set_font`.
```c
ui_set_text_brush(0xFFFFD700);
ui_set_font(24, VML_FONT_BOLD, 0, VML_ANCHOR_CENTER);
ui_draw_text(100, 40, "Title");
```
