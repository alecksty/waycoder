# Piece tiles

Cut one image into small cells and stamp them over and over.

> For the full list see "[UI development](help:vml/ui)". Each call below comes with a
> one-line usage note and an example; signatures come from `Lib/c/waycoder_ui.h` (the authoritative source).

### `ui_piece_cell(int pid, int rot, int which)`
Gets one cell of a piece: `pid` is the piece number, `rot` the rotation and `which` the cell index.
```c
ui_piece_cell(0, 0, 3);   /* piece 0, no rotation, cell 3 */
```
### `ui_piece_init(void)`
Initializes the piece tile table (the no-argument version; see `Lib/shared/src/vmlui.c` for the exact shape).
```c
ui_piece_init();
```
