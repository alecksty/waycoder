# Integer grid

For two-dimensional state such as a board or a map.

> For the full list see "[UI development](help:vml/ui)". Each call below comes with a
> one-line usage note and an example; signatures come from `Lib/c/waycoder_ui.h` (the authoritative source).

### `ui_gclear(void)`
Clears the grid. Use it for two-dimensional state such as a board or a map - it is **more reliable than the language's own arrays** (with some front ends a value written into an array does not read back; see the per-language pitfalls under "22 languages").
```c
ui_gclear();
```
### `ui_gget(int idx)`
Reads one cell; returns 0 if it was never written.
```c
int v = ui_gget(y * 10 + x);
```
### `ui_gset(int idx, int val)`
Writes one cell; `i` is a **one-dimensional index** (`row * width + col`).
```c
ui_gset(y * 10 + x, 1);   /* place a stone on a 10-column board at (x,y) */
```
