# Vector blocks

Record once, then stamp it over and over with rotation and scaling (this is what sprites use).

> For the full list see "[UI development](help:vml/ui)". Each call below comes with a
> one-line usage note and an example; signatures come from `Lib/c/waycoder_ui.h` (the authoritative source).

### `ui_create_block(int w, int h, int color)`
Starts **recording** a block. What gets recorded is drawing commands, not pixels - so a stamp can be rotated and scaled, and looks sharp when enlarged.
Note: record once, stamp forever; **recording again every frame runs out of handles after 128 frames** (the block table is not cleared by `ui_clear`).
```c
bid = ui_create_block(30, 20, 0);   /* a 30x20 block */
```
### `ui_draw_block(int block, int x, int y, int sx, int sy, int rot)`
Stamps a block. **(x,y) is the block's center** and the rotation is around that center; the scale is in **thousandths** (1000 = original size) and the angle is in degrees.
Note: a **negative scale means mirror** (`-1000` = horizontal flip) - so a sprite does not need two recordings for facing left and facing right.
```c
ui_draw_block(bid, 200, 300, 720, 720, 45);     /* scale to 72%, rotate 45 degrees */
ui_draw_block(bid, 200, 300, -1000, 1000, 0);   /* horizontal mirror */
```
### `ui_draw_block_at(int block, int x, int y, int sx, int sy, int rot)`
Same, but **(x,y) is the block's top-left corner**, and the rotation is around that corner.
```c
ui_draw_block_at(bid, 10, 10, 2000, 1000, 0);   /* top-left at (10,10), doubled horizontally */
```
### `ui_end_block(void)`
Ends the recording and returns the block handle (at least 1; 0 = the recording failed).
```c
bid = ui_end_block();
```
