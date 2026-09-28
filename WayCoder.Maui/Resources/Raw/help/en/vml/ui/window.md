# Window

Opening a window, asking for the size, closing it, screen orientation.

> For the full list see "[UI development](help:vml/ui)". Each call below comes with a
> one-line usage note and an example; signatures come from `Lib/c/waycoder_ui.h` (the authoritative source).

### `ui_orientation(void)`
The device orientation: `VML_ORIENT_PORTRAIT`(0) / `VML_ORIENT_LANDSCAPE`(1).
**Do not infer it from `scr_w > scr_h`** - that is the available drawing area (which changes when the gamepad is collapsed); the orientation is a property of the device itself.
```c
if (ui_orientation() == VML_ORIENT_LANDSCAPE) { /* lay out horizontally */ }
```
### `ui_scr_h(void)`
The height of the available drawing area.
```c
int h = ui_scr_h();
```
### `ui_scr_w(void)`
The width of the available drawing area (**you can ask this before opening a window**).
```c
int w = ui_scr_w();
```
### `ui_win_close(void)`
Closes the window (for a program that ends on its own).
```c
ui_win_close();
```
### `ui_win_closed(void)`
Whether the user has already closed the window (the exit condition of your main loop).
```c
while (ui_win_closed() == 0) { ... }
```
### `ui_win_open(char* title, int w, int h)`
Opens a drawing window. **Ask `ui_scr_w/h()` for the available drawing area first, then open with it** - a hard-coded size overflows on a small screen.
```c
int w = ui_scr_w(), h = ui_scr_h();
ui_win_open("My game", w, h);
```
### `ui_win_open_ex(char* title, int w, int h, int rotatable, int gamepad)`
The same, plus two declarations that **take effect before the window opens**: the rotation policy and whether you want a gamepad area.
```c
/* Gomoku: portrait only + no gamepad */
ui_win_open_ex("Gomoku", w, h, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
/* Racing: locked to landscape + gamepad wanted */
ui_win_open_ex("Racing", w, h, VML_WIN_LANDSCAPE, VML_WIN_NEED_GAMEPAD);
```
### `ui_win_open_pc(char* title, int w, int h, int rotatable, int keyboard)`
Opens a **"PC screen" window**: the "character grid + mouse" model of an old DOS/BGI program (it wants a keyboard and mouse, not the phone gamepad).
```c
ui_win_open_pc("Old program", 640, 400, VML_WIN_ROTATABLE, VML_WIN_NEED_KEYBOARD);
```
