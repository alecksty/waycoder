# Drawing state

Clipping, masks (with boolean operations), transparency, resource counts.

> For the full list see "[UI development](help:vml/ui)". Each call below comes with a
> one-line usage note and an example; signatures come from `Lib/c/waycoder_ui.h` (the authoritative source).

### `ui_alpha(int v)`
Global transparency 0..255; it applies to shapes drawn **afterwards**.
Note: it only affects a **shape's fill and outline** - gradients and images do not go through this path and are not affected.
```c
ui_alpha(120);
ui_rect(10, 10, 100, 60, 0xFF000000, 1, 0, 0);   /* semi-transparent black */
ui_alpha(255);
```
### `ui_brush_reset(void)`
Releases **all brushes / gradient definitions** accumulated by this window (it can only be reset as a whole - a handle is an index into the table, so deleting one entry would shift everything after it).
Programs that build gradients on demand should call it once at the start of each frame, otherwise the count keeps climbing to the limit.
```c
while (ui_win_closed() == 0) {
    if (ui_res_count(3) > 200) { ui_brush_reset(); }
    /* ... */
}
```
### `ui_clip_pop(void)`
Pops one level of clipping (popping an empty stack does nothing).
```c
ui_clip_pop();
```
### `ui_clip_push(int x, int y, int w, int h)`
Pushes one level of **rectangular clipping**: everything drawn afterwards is visible only inside this rectangle; levels can be **nested** (intersecting with the level above).
Note: every push must be matched by a pop - if you `return` in the middle and forget to pop, nothing after it gets drawn, and the reason is invisible.
```c
ui_clip_push(20, 20, 200, 100);
ui_rect(0, 0, 400, 400, 0xFFFF0000, 1, 0, 0);  /* only the part inside the box appears */
ui_clip_pop();
```
### `ui_clip_reset(void)`
**Empties the whole clipping stack** (it does not pop one level) - use it for "recovering from an error": call it once when you enter the main loop to get back to a clean, full-screen state.
It pairs with `ui_alpha(255)` as a "reset".
```c
while (ui_win_closed() == 0) {
    ui_clip_reset();   /* whoever forgot to pop last frame, this frame is unaffected */
    ui_alpha(255);
    /* ...draw this frame... */
}
```
### `ui_layer_begin(void)`
Starts **layer collection**: shapes drawn during it land on a temporary canvas first, and `ui_layer_end` composites the whole layer at once.
The difference from `ui_alpha`: `ui_alpha` makes "each shape semi-transparent on its own" (**overlaps inside the group show through each other**), while a layer is "draw one whole sheet first, then press that sheet down" (however the group overlaps, it **shows through only once**).
```c
ui_layer_begin();
ui_circle(200, 200, 80, 0xFFFF0000, 1, 0);
ui_circle(240, 200, 80, 0xFFFF0000, 1, 0);
ui_layer_end(120);   /* the whole group semi-transparent, overlaps no darker */
```
### `ui_layer_end(int alpha)`
Ends the collection and composites the **whole layer** with `alpha` (0..255).
Note the **cost**: each layer means one temporary canvas the size of the window plus one per-pixel composite - do not open too many.
Note: a frame that uses layers **falls back to rasterization** on the phone (the vector back end has no general way to composite off-screen).
```c
ui_layer_end(255);   /* opaque (equivalent to not opening a layer) */
ui_layer_end(0);     /* fully transparent (the layer is not drawn, and the canvas is saved too) */
```
### `ui_mask_begin(void)`
Starts collecting mask shapes: shapes drawn during it **are not put on screen** and serve only as the mask. Finish with `ui_mask_end` / `ui_mask_end2`.
```c
ui_mask_begin();
ui_circle(200, 200, 120, 0xFFFFFFFF, 1, 0);   /* this circle is only a mask, not drawn */
ui_mask_end(1);
```
### `ui_mask_clear(void)`
Cancels the mask: every shape drawn afterwards is **fully visible**.
```c
ui_mask_clear();
```
### `ui_mask_end(int inside)`
Accepts the mask and **replaces** the current one. `inside`: 1 = draw only inside the shapes / 0 = draw only outside them.
```c
ui_mask_end(1);   /* draw only inside */
/* or */
ui_mask_end(0);   /* draw only outside (equivalent to cutting the shapes out) */
```
### `ui_mask_end2(int op)`
Accepts the mask and combines it with the **current mask** using the boolean `op` - the old way of making a "ring" took two passes, now one is enough.
`op` is `VML_MASK_REPLACE`(0) / `VML_MASK_UNION`(1) / `VML_MASK_INTERSECT`(2) /
`VML_MASK_SUBTRACT`(3) / `VML_MASK_XOR`(4).
Note: it **does not touch `inside`** (that value is kept) - `inside` is a final overall inversion, which is a different thing from how shapes combine.
```c
/* A donut: big circle minus small circle */
ui_mask_begin();
ui_circle(200, 200, 120, 0xFFFFFFFF, 1, 0);
ui_mask_end(1);
ui_mask_begin();
ui_circle(200, 200, 60, 0xFFFFFFFF, 1, 0);
ui_mask_end2(VML_MASK_SUBTRACT);
```
### `ui_mask_path(int seg, int idx, char* buf, int cap)`
Exports shape idx of segment seg as an **SVG path** into buf - a program can use it to **outline the edge of a hole**, or for an outer glow.
A circle is exported as a **true arc**. Returns -1 if it does not fit or the index is out of range.
```c
char buf[128];
int i;
for (i = 0; i < ui_mask_shape_count(1); i++) {
    if (ui_mask_path(1, i, buf, sizeof(buf)) > 0) {
        ui_path(buf, 0x80000000, 2, 0, "", 1, 0);   /* outline the hole */
    }
}
```
### `ui_mask_seg_count(void)`
How many segments the current mask has (0 when there is no mask).
```c
int n = ui_mask_seg_count();
```
### `ui_mask_seg_op(int seg)`
The operator used by segment seg (-1 if out of range).
Use it to recognize **which segment is a cut-out hole** (`VML_MASK_SUBTRACT`) - the shape list alone cannot tell the base from the holes.
```c
if (ui_mask_seg_op(1) == VML_MASK_SUBTRACT) { /* segment 1 is a hole */ }
```
### `ui_mask_shape_count(int seg)`
How many shapes segment seg holds (0 if out of range).
```c
int n = ui_mask_shape_count(1);
```
### `ui_mask_test(int x, int y)`
**Use the mask as a collider**: is this point inside the current mask (1/0).
After a building layer "blows a hole where it is destroyed", you do not have to maintain your own table of hole coordinates - the holes are what is **visible** (bullets can pass through them).
Note: with no mask it is **always 1** (visible everywhere); the coordinates are scene coordinates; **do not call it per pixel** (each call walks the shape table once).
```c
if (ui_mask_test(ban_x, ban_y) == 0) { /* hit a wall */ }
```
### `ui_res_count(int what)`
Checks current usage: `0` shapes / `1` images / `2` vector blocks / `3` brushes and gradients; returns -1 for an unknown category.
Every category has a hard limit, and **reaching the limit silently drops things** - so you need to be able to see "it is nearly full".
```c
if (ui_res_count(0) > 10000) { ui_clear(0xFF000000); }   /* nearly out of shape slots */
```
