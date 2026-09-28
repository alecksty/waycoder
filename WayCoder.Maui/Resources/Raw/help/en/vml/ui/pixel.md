# Pixel readback

Read pixels, flood fill, grab and stamp images, take screenshots.

> For the full list see "[UI development](help:vml/ui)". Each call below comes with a
> one-line usage note and an example; signatures come from `Lib/c/waycoder_ui.h` (the authoritative source).

### `ui_flood_fill(int x, int y, int color, int border)`
**Flood fills** from (x,y), stopping at the border color (four-connected). Returns the number of rectangles written; 0 means nothing was filled (that happens when the seed point itself sits on the border color - very common with old programs that "click on the line", and not an error).
```c
ui_flood_fill(50, 50, 0xFF00FF00, 0xFFFF0000);   /* fill green inside a red frame */
```
### `ui_get_image(int x, int y, int w, int h)`
Saves a region of the picture and returns a **handle** (at least 1), or 0 on failure. An old program's `malloc(imagesize(...))` still works fine - we just do not use that memory.
```c
int img = ui_get_image(0, 0, 32, 32);   /* grab a 32x32 region */
```
### `ui_get_pixel(int x, int y)`
Reads the color of one pixel (`0xRRGGBB`; -1 if out of range).
Note: the scene is **retained-mode**, so every call makes the host **rasterize on the spot** - do not put it in a tight loop.
```c
int c = ui_get_pixel(100, 100);
if (c == 0xFFFFFF) { /* it is white there */ }
```
### `ui_put_image(int x, int y, int handle, int mode)`
Stamps the handle's region at (x,y). `mode`: 0 = COPY, stamp directly / 1 = XOR (XOR has to read the destination pixel first, so it is slower).
```c
ui_put_image(200, 100, img, 0);    /* stamp it there */
ui_put_image(200, 100, img, 1);    /* XOR once more = erase it (the old way of animating sprites) */
```
### `ui_screenshot(char* path)`
Saves the current window as a PNG (the path is relative to the workspace; with no path it picks a name automatically under `shot/`). Returns 1 on success.
```c
ui_screenshot("shot/win.png");
```
