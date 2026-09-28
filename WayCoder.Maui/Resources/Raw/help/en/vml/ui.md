# UI development

A VML program is not limited to printing text - it has a complete set of **phone UI interfaces**:
open a window, draw, receive touch and key input, play sound, vibrate, save data. This page lists
**every interface**, each with a one-line description of how to use it and an example.

## The smallest program

```c
#include <waycoder_ui.h>

int main(void) {
    int w = ui_scr_w(), h = ui_scr_h();     /* (1) ask for the usable drawing area first */
    ui_win_open("Demo", w, h);              /* (2) then open the window at that size */
    ui_clear(0xFF101020);
    ui_text(20, 40, "Hello", 0xFFFFFFFF, 20, VML_ANCHOR_CENTER);
    ui_present();                           /* (3) this frame is finished */

    int m[4];
    while (ui_win_closed() == 0) {          /* (4) main loop: receive -> handle -> redraw */
        if (ui_wait(m, 0) == VML_MSG_TOUCHDOWN) { /* handle m[1], m[2] */ }
    }
    return 0;
}
```

**Four bones**: ask for the size -> open the window -> main loop (receive / handle / `ui_present`)
-> the user closes the window and you exit. Below, every interface is listed by category; **the way
you call them is the same in every language**, only the syntax differs (see "22 languages").

> The authoritative source for the signatures is `Lib/c/waycoder_ui.h`, and the implementation is
> `Lib/shared/src/vmlui.c` - **all 22 front ends share one implementation**. The signatures on this
> page come from that header; change an interface and re-running the generator is enough.

## How the interfaces are layered

They fall into four layers by **purpose**. When you are writing a program, finding an interface from
this diagram is faster than scanning the list:

```text
┌─ Window / device ────────────────────────────────────────────────┐
│  open a window  ui_win_open(_ex / _pc)   ask size  ui_scr_w/h    │
│  orientation  ui_orientation   lock  ui_orient_lock              │
│  immersive  ui_immersive   keep awake  ui_keep_on                │
│  sensors  ui_sensor(_available/_rate / _calibrate)               │
│  battery  ui_battery   power saving  ui_power_saver              │
└───────────────────────────▼──────────────────────────────────────┘
                            │ after the window is open
┌─ Input ──────────────────────────────────────────────────────────┐
│  event-driven (main loop)  ui_wait / ui_poll + ui_msg_a/b        │
│  polling (feel)  ui_touch_query / ui_key_down                    │
└───────────────────────────▼──────────────────────────────────────┘
                            │ input arrives -> decide what to draw
┌─ Drawing ────────────────────────────────────────────────────────┐
│  shapes  ui_rect / ui_circle / ui_path / ui_text ...             │
│  drawing state  ui_clip_* / ui_mask_* / ui_alpha                 │
│  resources  ui_create_block (sprites) / ui_gradient / ui_brush_* │
│  pixels  ui_get_pixel / ui_flood_fill / ui_screenshot            │
└───────────────────────────▼──────────────────────────────────────┘
                            │ used together
┌─ System ─────────────────────────────────────────────────────────┐
│  dialogs  ui_dlg_*    sound & vibration  ui_beep / ui_vibrate    │
│  save data  ui_store_*    arguments  ui_argc / ui_arg            │
│  clipboard  ui_clipboard_*   share  ui_share_text / ui_open_url  │
└──────────────────────────────────────────────────────────────────┘
```

**A one-line mnemonic**: **Window** decides how large you can draw, **Input** tells you what the user
did, **Drawing** puts things on the screen, and **System** covers everything outside the game itself
(dialogs, sound, save data).

### Why sprites should be "vector blocks"

Anything that moves (the player, a bird, a banana, ...) **is best made into a block**
(`ui_create_block` -> `ui_draw_block`):

- The shape is written once, and it can be **rotated, scaled and mirrored** when pasted (a negative
  scale is a mirror);
- Per frame, "a dozen drawing calls" collapse into "one block paste". Measured with 300 sprites x 60
  frames: drawing directly **3531ms** -> pasting blocks **2945ms** (a **17%** gain), and the gap
  grows the more sprites there are.

## Which layer these interfaces live in (where to look when something breaks)

```text
   your program (C / BASIC / Python / ... 22 languages)
        │  #include <waycoder_ui.h>
        ▼
   Lib/c/waycoder_ui.h          <- the authoritative source of the signatures
   Lib/shared/src/vmlui.c       <- the C wrapper: one line of asm("SYSCALL #5xx")
        │  syscall 500-599
        ▼
   VmlHostRuntime (UI/Shared)   <- **all of the logic lives in this one file**
        │   the phone and the desktop **compile the same file**: coordinate clamping,
        │   the message queue, the drawing state machine, ...
        │  IVmlHost (only the dozen or so things that really differ per platform)
        ▼
   phone  MauiVmlHost        │  desktop  CliVmlHost (scripts/vmlcli)
   native dialogs / save     │  scripted input / write PNG
   data / audio              │
```

**What is this good for**:

- **The two platforms behave identically** (one implementation) => what you verified on the desktop
  is exactly what you get on the phone;
- If "right on the phone, wrong on the desktop" ever happens, it is certainly in the **`IVmlHost`**
  layer (the bottom cell), and there is no need to dig through the drawing code;
- The desktop `scripts/vmlcli` is an **acceptance harness** - run a change there first (it takes
  seconds); do not jump straight to building an APK (a compile on the phone takes over a minute).

## Window

Opening, asking the size, closing, screen orientation - [`help:vml/ui/window`](help:vml/ui/window)

| Interface | In one line |
|---|---|
| [ui_orientation](help:vml/ui/window) | Device orientation: `VML_ORIENT_PORTRAIT`(0) / `VML_ORIENT_LANDSCAPE`(1). |
| [ui_scr_h](help:vml/ui/window) | The height of the usable drawing area. |
| [ui_scr_w](help:vml/ui/window) | The width of the usable drawing area (you can ask before opening a window too). |
| [ui_win_close](help:vml/ui/window) | Close the window (for a program that ends on its own). |
| [ui_win_closed](help:vml/ui/window) | Whether the user has already closed the window (the exit condition of the main loop). |
| [ui_win_open](help:vml/ui/window) | Open a drawing window. Ask `ui_scr_w/h()` for the usable drawing area first and open at that size - a hard-coded size overflows on a small screen. |
| [ui_win_open_ex](help:vml/ui/window) | Same as above, plus two declarations that take effect before the window opens: the rotation policy, and whether you want a gamepad area. |
| [ui_win_open_pc](help:vml/ui/window) | Open a "computer screen" window: the old DOS/BGI model of "character grid + mouse" (it wants a keyboard and a mouse, not the phone gamepad). |

```c
/* Example: ui_orientation */
if (ui_orientation() == VML_ORIENT_LANDSCAPE) { /* lay it out sideways */ }
```

## Receiving messages

How touch / key / timer messages are received - [`help:vml/ui/messages`](help:vml/ui/messages)

| Interface | In one line |
|---|---|
| [ui_msg_a](help:vml/ui/messages) | The first argument of the current message (the x of a touch, the key code of a key, the id of a timer, ...). |
| [ui_msg_b](help:vml/ui/messages) | The second argument of the current message (the y of a touch, ...). |
| [ui_msg_clear](help:vml/ui/messages) | Clear the message queue (use it when switching scenes or starting a new round, so a keypress from the previous round is not swallowed). |
| [ui_msg_count](help:vml/ui/messages) | How many messages are still queued up (worth a glance when you want to drop a backlog). |
| [ui_msg_type](help:vml/ui/messages) | The type of the current message (so you do not have to keep `msg[0]` in your head). |
| [ui_poll](help:vml/ui/messages) | Does not wait: returns `VML_MSG_NONE` when there is no message. Use this for continuous animation (together with your own pacing); |
| [ui_poll_ex](help:vml/ui/messages) | The version of `ui_poll` that takes "keep it after reading or not". |
| [ui_poll_msg](help:vml/ui/messages) | The pointer-free way to fetch a message (for languages that cannot take an array pointer): returns `VML_MSG_NONE` when there is none, |
| [ui_wait](help:vml/ui/messages) | Wait for one message; the argument is `int msg[4]`. Returns the message type (see the table below); `timeout=0` means wait forever. |
| [ui_wait_ex](help:vml/ui/messages) | Same as above, but the third argument decides whether the message stays in the queue after it is read (`VML_MSG_KEEP` / `VML_MSG_CONSUME`). |
| [ui_wait_msg](help:vml/ui/messages) | The pointer-free way to wait for a message: returns the type when one arrives, `VML_MSG_NONE` on timeout. |

```c
/* Example: ui_msg_a */
int x = ui_msg_a();
```

## Multi-touch and the device

Multi-finger queries, hold detection, orientation lock, full screen - [`help:vml/ui/device`](help:vml/ui/device)

| Interface | In one line |
|---|---|
| [ui_audio_playing](help:vml/ui/device) | Is the background BGM still playing -> 1/0 (never played, finished playing and stopped all give 0). |
| [ui_immersive](help:vml/ui/device) | Hide / restore the status bar and navigation bar (full-screen games). |
| [ui_key_down](help:vml/ui/device) | Whether a given key is held down right now ("hold left to keep moving" without maintaining your own state table). |
| [ui_orient_lock](help:vml/ui/device) | Lock the screen orientation: `VML_LOCK_PORTRAIT`(0) / `VML_LOCK_LANDSCAPE`(1) / `VML_LOCK_AUTO`(2). |
| [ui_touch](help:vml/ui/device) | Query finger number slot (0..9): writes `out[0]=x out[1]=y out[2]=pressed` and returns 1 when valid (0 when the slot is out of range). |
| [ui_touch_down](help:vml/ui/device) | The "pressed" value (1/0) cached by the last `ui_touch_query`. |
| [ui_touch_query](help:vml/ui/device) | For languages that cannot take a pointer: query once and cache it; the three below read that cache (the same division of labor as `ui_wait_msg` + `ui_msg_a/b`). |
| [ui_touch_x](help:vml/ui/device) | The x cached by the last `ui_touch_query`. |
| [ui_touch_y](help:vml/ui/device) | The y cached by the last `ui_touch_query`. |
| [ui_sensor](help:vml/ui/device) | **Sensors**: read the accelerometer / gyroscope / fused attitude into `out[0..2]`. **For tilt controls use the accelerometer**. |
| [ui_sensor_available](help:vml/ui/device) | Whether this device has that sensor -> 1/0 (**you can ask before opening a window**). |
| [ui_sensor_rate](help:vml/ui/device) | Set the sampling interval in milliseconds - a sensor draws battery the moment it is on. |
| [ui_sensor_calibrate](help:vml/ui/device) | Treat the **current attitude** as zero (when the player is lying down, "level" is wrong). |
| [ui_battery](help:vml/ui/device) | **Battery and charging**: `out[0]` = 0-100, `out[1]` = charging. Returning 0 means **this device has no battery**. |
| [ui_power_saver](help:vml/ui/device) | Is the system power-saving mode on -> 1/0 (**not the same thing as a low battery**). |

```c
/* Example: ui_audio_playing */
ui_audio_play("bgm.mp3", 0);
while (ui_audio_playing()) { ui_wait(msg, 200); }
```

## Timers and random numbers

Repeating timers, reading the time, random numbers - [`help:vml/ui/timer`](help:vml/ui/timer)

| Interface | In one line |
|---|---|
| [ui_rand](help:vml/ui/timer) | Random number: `ui_rand(n)` -> 0..n-1 (returns 1 when n <= 0; it does not crash). |
| [ui_tick](help:vml/ui/timer) | Milliseconds since boot (for computing frame intervals and driving animation yourself). |
| [ui_timer_kill](help:vml/ui/timer) | Stop a timer. |
| [ui_timer_set](help:vml/ui/timer) | Start a repeating timer that sends a `VML_MSG_TIMER` every N milliseconds (`msg[1]` is the id you gave). |

```c
/* Example: ui_rand */
int n = ui_rand(6);      /* 0..5 */
int side = ui_rand(2);   /* 0 or 1 */
```

## Drawing

Points, lines, shapes, polygons, paths, gradients, images - [`help:vml/ui/draw`](help:vml/ui/draw)

| Interface | In one line |
|---|---|
| [ui_brush_linear](help:vml/ui/draw) | Build a linear gradient brush. The geometry is in thousandths (0..1000) relative to the shape's own bounding box: `0,0,1000,0` = left to right. |
| [ui_brush_named](help:vml/ui/draw) | Get a handle to a gradient already defined with `ui_gradient`, looked up by name. |
| [ui_brush_radial](help:vml/ui/draw) | Build a radial gradient brush (center -> edges). `cx cy r` are in thousandths too; `500,500,500` = centered. |
| [ui_brush_solid](help:vml/ui/draw) | Build a solid-color brush. Returns a handle (>= 1; 0 = failure) for `ui_set_fill` / `ui_set_pen` to use. |
| [ui_circle](help:vml/ui/draw) | Draw a circle; a non-zero `fill` fills it. |
| [ui_circle_grad](help:vml/ui/draw) | A gradient circle (define the gradient with `ui_gradient` first). |
| [ui_clear](help:vml/ui/draw) | Fill the whole screen with one color (call it at the start of each frame). Colors are always `0xAARRGGBB`. |
| [ui_draw_circle](help:vml/ui/draw) | Draw a circle with the current brush. |
| [ui_draw_ellipse](help:vml/ui/draw) | Draw an ellipse with the current brush. |
| [ui_draw_heart](help:vml/ui/draw) | Draw a heart with the current brush. |
| [ui_draw_line](help:vml/ui/draw) | Draw a straight line with the current pen. |
| [ui_draw_path](help:vml/ui/draw) | Draw an SVG path with the current brush (`M L C Q A Z`; upper case is absolute and lower case relative; multiple subpaths punch holes by the even-odd rule). |
| [ui_draw_pie](help:vml/ui/draw) | Draw a pie wedge with the current brush: radius / start angle / end angle in degrees. |
| [ui_draw_poly](help:vml/ui/draw) | Draw a polygon with the current brush (`close=1` closes it automatically) or a polyline (`close=0`). `pts` holds one point per two ints and `count` is the number of points. |
| [ui_draw_rect](help:vml/ui/draw) | Draw a rectangle with the current brush (`radius > 0` gives rounded corners). |
| [ui_draw_regular](help:vml/ui/draw) | Draw a regular polygon with the current brush: radius / number of sides / rotation in degrees. |
| [ui_draw_ring](help:vml/ui/draw) | Draw a ring with the current brush (outer radius / inner radius; the hole in the middle is punched by the even-odd rule). |
| [ui_draw_star](help:vml/ui/draw) | Draw a star with the current brush: outer radius / inner radius / number of points / rotation in degrees. |
| [ui_draw_text](help:vml/ui/draw) | Draw one line of text with the current text attributes (`ui_set_font`). |
| [ui_ellipse](help:vml/ui/draw) | Draw an ellipse (`rx` / `ry` are the two radii). |
| [ui_ellipse_grad](help:vml/ui/draw) | An ellipse with a gradient fill. It belongs with `ui_rect_grad` / `ui_circle_grad` (that batch of interfaces missed the ellipse at the time). |
| [ui_gradient](help:vml/ui/draw) | Define a gradient brush under a name (a string id); afterwards `ui_rect_grad` / `ui_circle_grad` / `ui_path` refer to it by that name. |
| [ui_icon](help:vml/ui/draw) | Draw a built-in icon (looked up by name, so you do not have to draw it yourself). |
| [ui_image](help:vml/ui/draw) | Draw an image (PNG / JPG / BMP) at the given position; pass 0 for `w` / `h` to use the original size. |
| [ui_line](help:vml/ui/draw) | Draw a line; `lw` is the line width. |
| [ui_path](help:vml/ui/draw) | Draw using SVG path syntax (`M L H V C S Q T A Z`; upper case is absolute and lower case relative). |
| [ui_pixel](help:vml/ui/draw) | Draw a single point. |
| [ui_polygon](help:vml/ui/draw) | Draw a polygon (closed automatically). `pts` is an int array with one point per two ints; `count` is the number of points. |
| [ui_polyline](help:vml/ui/draw) | A polyline (not closed). The arguments mean the same as `ui_polygon` (`stroke` is the color, `width` the line width). |
| [ui_present](help:vml/ui/draw) | This frame is finished. The single most important call in the whole loop - nothing on screen updates without it. |
| [ui_rect](help:vml/ui/draw) | Draw a rectangle. A non-zero `fill` fills it; `radius` is the corner radius. |
| [ui_rect_grad](help:vml/ui/draw) | A rectangle with a gradient (define the gradient with `ui_gradient` first). |
| [ui_set_fill](help:vml/ui/draw) | Set the fill brush. You can pass a brush handle or a color; 0 = do not fill (hollow). |
| [ui_set_pen](help:vml/ui/draw) | Set the pen (the stroke) = brush + width + line cap + dash + arrow. 0 = do not stroke. |
| [ui_set_text_brush](help:vml/ui/draw) | Set the text brush (together with `ui_set_font` + `ui_draw_text`). 0 = go back to the color given to `ui_set_font`. |

```c
/* Example: ui_brush_linear */
int b = ui_brush_linear(0xFFFF3020, 0xFF2050FF, 0, 0, 1000, 0);
ui_set_fill(b);
```

## Drawing state

Clipping, masks (with boolean operations), alpha, resource counts - [`help:vml/ui/gfx`](help:vml/ui/gfx)

| Interface | In one line |
|---|---|
| [ui_alpha](help:vml/ui/gfx) | Global alpha 0..255, in effect for shapes drawn after it. |
| [ui_brush_reset](help:vml/ui/gfx) | Release every brush / gradient definition this window has accumulated (it can only reset as a whole - a handle is an index into the table, so deleting one entry shifts every entry after it). |
| [ui_clip_pop](help:vml/ui/gfx) | Pop one level of clipping (popping an empty stack does nothing). |
| [ui_clip_push](help:vml/ui/gfx) | Push one level of rectangular clipping: from then on, what you draw is only visible inside that rectangle; levels can nest (intersected with the one above). |
| [ui_clip_reset](help:vml/ui/gfx) | Clear the entire clipping stack (not pop one level) - for "recovering from an error": call it once on entering the main loop and you are back to a clean full-screen state. |
| [ui_layer_begin](help:vml/ui/gfx) | Start collecting a layer: shapes drawn meanwhile land on a temporary canvas, and the whole layer is composited when `ui_layer_end` arrives. |
| [ui_layer_end](help:vml/ui/gfx) | Finish collecting and composite the whole layer with `alpha` (0..255). |
| [ui_mask_begin](help:vml/ui/gfx) | Start collecting mask shapes: shapes drawn meanwhile do not go to the screen, they only serve as the mask. Finish with `ui_mask_end` / `ui_mask_end2`. |
| [ui_mask_clear](help:vml/ui/gfx) | Cancel the mask: everything drawn afterwards is visible. |
| [ui_mask_end](help:vml/ui/gfx) | Accept the mask and make it the current one. `inside`: 1 = draw only inside the shape / 0 = only outside it. |
| [ui_mask_end2](help:vml/ui/gfx) | Accept the mask and combine it with the current one by the boolean `op` - the old way needed two passes for a "ring", now one is enough. |
| [ui_mask_path](help:vml/ui/gfx) | Export shape number idx of segment seg as an SVG path into buf - a program can use it to stroke the rim of a hole or to draw an outer glow. |
| [ui_mask_seg_count](help:vml/ui/gfx) | How many segments the current mask has (0 when there is no mask). |
| [ui_mask_seg_op](help:vml/ui/gfx) | The operator used by segment seg (-1 when out of range). |
| [ui_mask_shape_count](help:vml/ui/gfx) | How many shapes there are in segment seg (0 when out of range). |
| [ui_mask_test](help:vml/ui/gfx) | Use the mask as a collider: is this point inside the current mask (1/0). |
| [ui_res_count](help:vml/ui/gfx) | Query current usage: `0` shapes / `1` images / `2` vector blocks / `3` brushes and gradients; -1 when unknown. |

```c
/* Example: ui_alpha */
ui_alpha(120);
ui_rect(10, 10, 100, 60, 0xFF000000, 1, 0, 0);   /* semi-transparent black */
ui_alpha(255);
```

## Reading pixels back

Read pixels, flood fill, capture and paste images, screenshots - [`help:vml/ui/pixel`](help:vml/ui/pixel)

| Interface | In one line |
|---|---|
| [ui_flood_fill](help:vml/ui/pixel) | Flood from (x,y) and stop at the border color (four-connected). Returns the number of rectangle runs written; 0 = nothing filled |
| [ui_get_image](help:vml/ui/pixel) | Save a piece of the screen -> a handle (>= 1), 0 on failure. An old program's `malloc(imagesize(...))` can still be written as before; we just do not use that memory. |
| [ui_get_pixel](help:vml/ui/pixel) | Read the color of one pixel (`0xRRGGBB`; -1 when out of range). |
| [ui_put_image](help:vml/ui/pixel) | Paste the handle's content at (x,y). `mode`: 0 = COPY, paste directly / 1 = XOR (XOR has to read the destination pixels first, so it is slower). |
| [ui_screenshot](help:vml/ui/pixel) | Save the current window as a PNG (the path is relative to the workspace; without a path a name is generated under `shot/`). Returns 1 on success. |

```c
/* Example: ui_flood_fill */
ui_flood_fill(50, 50, 0xFF00FF00, 0xFFFF0000);   /* flood green inside a red frame */
```

## Vector blocks

Record once, paste over and over with rotation and scaling (this is what sprites use) - [`help:vml/ui/block`](help:vml/ui/block)

| Interface | In one line |
|---|---|
| [ui_create_block](help:vml/ui/block) | Start recording a block. What is recorded is drawing commands, not pixels - so it can be rotated and scaled when pasted, and does not blur when enlarged. |
| [ui_draw_block](help:vml/ui/block) | Paste a block. (x,y) is the center of the block and it rotates about that center; the scale is in thousandths (1000 = original size) and the angle is in degrees. |
| [ui_draw_block_at](help:vml/ui/block) | Same as above, but (x,y) is the block's top-left corner and it rotates about that corner. |
| [ui_end_block](help:vml/ui/block) | Finish recording and return the block handle (>= 1; 0 = nothing was recorded). |

```c
/* Example: ui_create_block */
bid = ui_create_block(30, 20, 0);   /* a 30x20 block */
```

## Text

Writing, fonts, anchors - [`help:vml/ui/text`](help:vml/ui/text)

| Interface | In one line |
|---|---|
| [ui_set_font](help:vml/ui/text) | Set the font once; every later `ui_text_cur` uses it (so you do not repeat four arguments every time). |
| [ui_set_valign](help:vml/ui/text) | Set the default vertical alignment (in effect for all text after it). |
| [ui_text](help:vml/ui/text) | Write one line of text at (x,y). `size` is the font size; `anchor` decides which side of the text (x,y) refers to (`VML_ANCHOR_LEFT` / `CENTER` / `RIGHT`). |
| [ui_text_cur](help:vml/ui/text) | Write text with the font set by `ui_set_font`. |
| [ui_text_styled](help:vml/ui/text) | Same as above, plus styles (bold / italic / underline). |
| [ui_text_v](help:vml/ui/text) | Text with vertical alignment (`VML_VALIGN_*`) - the y of `ui_text` is the baseline, while this one can align to top / middle / bottom. |

```c
/* Example: ui_set_font */
ui_set_font(18, VML_FONT_BOLD, 0xFFFFFFFF, VML_ANCHOR_CENTER);
ui_text_cur(180, 40, "Press a direction key to exit");
```

## Piece tiles

Cut an image into cells and paste them over and over - [`help:vml/ui/piece`](help:vml/ui/piece)

| Interface | In one line |
|---|---|
| [ui_piece_cell](help:vml/ui/piece) | Get one cell of a piece: `pid` is the piece number, `rot` the rotation, `which` which cell. |
| [ui_piece_init](help:vml/ui/piece) | Initialize the piece tile table (the no-argument version; for the actual shapes see `Lib/shared/src/vmlui.c`). |

```c
/* Example: ui_piece_cell */
ui_piece_cell(0, 0, 3);   /* piece 0, no rotation, cell 3 */
```

## Integer grid

Two-dimensional state such as a board or a map - [`help:vml/ui/grid`](help:vml/ui/grid)

| Interface | In one line |
|---|---|
| [ui_gclear](help:vml/ui/grid) | Clear the grid. Use it for two-dimensional state such as a board or a map - it is more reliable than the language's own arrays (some front ends cannot read back what they wrote; see the per-language pitfalls in "22 languages"). |
| [ui_gget](help:vml/ui/grid) | Read one cell; returns 0 if it was never written. |
| [ui_gset](help:vml/ui/grid) | Write one cell; `i` is a one-dimensional index (`row * width + col`). |

```c
/* Example: ui_gclear */
ui_gclear();
```

## Dialogs

Messages, single choice, multiple choice, text input - [`help:vml/ui/dialog`](help:vml/ui/dialog)

| Interface | In one line |
|---|---|
| [ui_dlg_input](help:vml/ui/dialog) | Ask for one line of text input. The result is written into the buffer you pass. |
| [ui_dlg_msg](help:vml/ui/dialog) | Show a message box (pass 0 for `style`). It blocks until the user dismisses it - just right for reporting a result at the end of a game. |
| [ui_dlg_multi](help:vml/ui/dialog) | A multiple-choice dialog: `opts` is a string of options, `n` the number of options. Returns the number selected. |
| [ui_dlg_select](help:vml/ui/dialog) | A single-choice dialog: `opts` is a string of options, `n` the number of options, `def` the default selection. Returns the selected index (-1 = cancelled). |

```c
/* Example: ui_dlg_input */
char buf[64];
ui_dlg_input("Rename", "New name:", buf, 64);
```

## Sound and haptics

Synthesized tones, audio files, vibration, keeping the screen awake - [`help:vml/ui/feel`](help:vml/ui/feel)

| Interface | In one line |
|---|---|
| [ui_beep](help:vml/ui/feel) | Synthesize a tone on the spot (no audio file needed): `freq` in hertz, `ms` in milliseconds. **Single channel**, so firing several in a row leaves only the last one audible - **and that is exactly why it never crackles**: an event always has exactly one tone sounding. **Use this for game sound effects**. |
| [ui_tone_on](help:vml/ui/feel) | **Polyphonic**: start a note (note number 0-127, A4 = 69). Start several channels at once and you get a chord. |
| [ui_tone_off](help:vml/ui/feel) | Stop one note (`note` = -1 stops every note on that channel). |
| [ui_tone_all_off](help:vml/ui/feel) | Stop everything (with a fade-out). |
| [ui_tone_voices](help:vml/ui/feel) | How many voices are sounding right now - use it to check whether your chords are stacking up. |
| [ui_tone_panic](help:vml/ui/feel) | Stop **immediately** (no fade-out) - for a mute button or a forced stop. |
| [ui_tone_wave](help:vml/ui/feel) | Set the waveform of a channel (sine / square / saw / triangle) - give the bass a triangle wave in a chord and it stops sounding muddy. |
| [ui_tone_max_voices](help:vml/ui/feel) | The maximum number of simultaneous voices (1-32, default 32). |
| [ui_sfx_add](help:vml/ui/feel) | **Sequencer**: enqueue a note (it sounds after `delay` beats and lasts `dur` beats). Warning: **do not use this for game sound effects** (stacked voices and long tails **crackle on real devices**; all 28 games in this repository have been moved back to `ui_beep`) - see [which sound effect to use](help:vml/ui/feel). |
| [ui_sfx_tick](help:vml/ui/feel) | Call once per frame; it advances by real elapsed time (**put it in the main loop**). |
| [ui_sfx_panic](help:vml/ui/feel) | Mute immediately and clear the table (on exit or when starting a new round). |
| [ui_sfx_active](help:vml/ui/feel) | How many slots are still occupied - **after a round finishes this should be back to 0**, so use it as a self-check. |
| [ui_audio_play](help:vml/ui/feel) | Play an audio **file** (mp3/wav/...); a non-zero `loop` repeats it (for BGM). |
| [ui_audio_stop](help:vml/ui/feel) | Stop the audio that is playing. |
| [ui_audio_volume](help:vml/ui/feel) | Overall volume 0-100 (in effect for audio played **after** it). |
| [ui_audio_playing](help:vml/ui/device) | Is the background BGM still playing -> 1/0. |
| [ui_keep_on](help:vml/ui/feel) | Screen-awake switch (every game should turn this on). |
| [ui_vibrate](help:vml/ui/feel) | Vibrate: `ms` milliseconds, `strength` intensity. |
| [ui_vibrate_pattern](help:vml/ui/feel) | Vibrate in a rhythm (an int array: odd indices are silent, even indices are on). |

```c
/* Example: ui_beep */
ui_beep(880, 80);      /* one line cleared */
ui_beep(1568, 160);    /* four lines cleared, a higher pitch */
```

## Clipboard and sharing

Share a score, **store and load a save code** (encode a whole game state into a short piece of text
so the player can keep it in their notes and continue on another device) - [`help:vml/ui/device`](help:vml/ui/device)

| Interface | In one line |
|---|---|
| [ui_clipboard_set](help:vml/ui/device) | Write the system clipboard: 1 = handed to the system / 0 = this platform has no clipboard. |
| [ui_clipboard_get](help:vml/ui/device) | Read the clipboard: returns **the number of bytes written** / -1 = empty or unsupported. |
| [ui_share_text](help:vml/ui/device) | Show the system **share sheet** (an empty title string means none). Warning: it does not block. |
| [ui_open_url](help:vml/ui/device) | Open a link in the system browser. **Only http/https**. |

```c
char code[64];
ui_clipboard_set("SAVE-1a2b3c");              /* hand the save code to the player */
if (ui_clipboard_get(code, 64) > 0) { /* read it back and carry on playing */ }
ui_share_text("I scored 8 points in the game!", "Share my score");
```

Warning: **returning 0 means "this platform does not have that capability"** (the desktop has no
share sheet), **not that it failed** - the game should carry on as usual. Both writing the clipboard
and showing the share sheet are asynchronous on the platform, and the host does not wait for a result.

## Local save data

Small data such as a high score - [`help:vml/ui/store`](help:vml/ui/store)

| Interface | In one line |
|---|---|
| [ui_store_get](help:vml/ui/store) | Read a value: it is written into the buffer you pass and the length is returned (-1 when the key does not exist). |
| [ui_store_set](help:vml/ui/store) | Store a value. The value is a string too - to store a number, convert it to a string yourself (there is no sprintf available here). |
| [ui_store_del](help:vml/ui/store) | Delete one entry of save data (for example, "clear the high score"). |

```c
/* Example: ui_store_get */
char buf[16];
if (ui_store_get("high", buf, 16) > 0) best = atoi(buf);
```

## The all-purpose interface

Two strings in, one JSON out - [`help:vml/ui/json`](help:vml/ui/json)

| Interface | In one line |
|---|---|
| [ui_call_json](help:vml/ui/json) | Two strings in, one JSON string out - use it to query device information and to reach the host's miscellaneous capabilities, |
| [ui_call_json_at](help:vml/ui/json) | Get byte number i of the previous result. |
| [ui_call_json_len](help:vml/ui/json) | How long the result of the last `ui_call_json_s` is. |
| [ui_call_json_print](help:vml/ui/json) | Print the whole result of the last `ui_call_json_s` to stdout (with a newline at the end) - the least hassle for debugging, |
| [ui_call_json_s](help:vml/ui/json) | Same as above, but without a buffer (suits languages that cannot take a pointer); read the result with `_len` / `_at`. |

```c
/* Example: ui_call_json */
char buf[512];
ui_call_json("sysinfo", "", buf, 512);
puts(buf);   /* {"ok":true,"result":{...}} */
```

## Calling the host

Call a host function by numeric id (typed, with no encode/decode) - [`help:vml/ui/call`](help:vml/ui/call)

| Interface | In one line |
|---|---|
| [callwithdouble4](help:vml/ui/call) | Same as above, with 3 `double` arguments (passed in `D1..D3`); the return covers `D0`. |
| [callwithfloat8](help:vml/ui/call) | Same as above, with 7 `float` arguments (passed in `F1..F7`); the return covers `F0`. |
| [callwithint8](help:vml/ui/call) | Call a host function by numeric id: `v[0]` is the call number (see the `VML_CALL_*` macros), `v[1..7]` are the arguments, |
| [callwithlong4](help:vml/ui/call) | Same as above, with 3 `long` arguments (passed in `L1..L3`); the return covers `L0`. |

```c
/* Example: callwithdouble4 */
double v[4];
v[0] = VML_CALL_ECHO_DOUBLE;
v[1] = 1.0; v[2] = 2.0;
double r = callwithdouble4(v);
```

## Command-line arguments

The program's own switches, such as -l - [`help:vml/ui/args`](help:vml/ui/args)

| Interface | In one line |
|---|---|
| [ui_arg](help:vml/ui/args) | Copy argument i into buf and return its length (-1 when out of range). `argv[0]` is the program name, so the user's first argument is `ui_arg(1,...)`. |
| [ui_argc](help:vml/ui/args) | The number of arguments (including the program name; always >= 1). |

```c
/* Example: ui_arg */
char buf[64];
if (ui_arg(1, buf, sizeof(buf)) > 0) { /* a -l style switch was used */ }
```
