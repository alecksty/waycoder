# Multi-touch and device

Multi-finger queries, held-key tests, orientation lock, full screen.

> For the full list see "[UI development](help:vml/ui)". Each call below comes with a
> one-line usage note and an example; signatures come from `Lib/c/waycoder_ui.h` (the authoritative source).

### `ui_audio_playing(void)`
Is the background BGM still playing? -> 1/0 (never played, already finished and stopped all give 0).
Typical use: wait for one track to finish before moving to the next segment.
```c
ui_audio_play("bgm.mp3", 0);
while (ui_audio_playing()) { ui_wait(msg, 200); }
```
### `ui_immersive(int on)`
Hides / restores the status bar and navigation bar (for full-screen games).
```c
ui_immersive(1);   /* entering the game: full screen */
ui_immersive(0);   /* restore before quitting */
```
### `ui_key_down(int key)`
Is a key **currently** held down (so "hold left to keep moving left" does not need your own state table).
Note: it **does not cure a lost KeyUp** - a finger sliding off the key, or the system eating a CANCEL, can mean the KeyUp never arrives, and the query then reports "held" forever. **Auto-repeat logic still needs its own brake** (stop after two ticks with no movement / a cap on the total number of ticks).
```c
if (ui_key_down(VML_KEY_LEFT)) { x = x - 2; }
```
### `ui_orient_lock(int mode)`
Locks the screen orientation: `VML_LOCK_PORTRAIT`(0) / `VML_LOCK_LANDSCAPE`(1) / `VML_LOCK_AUTO`(2).
Note: this is a **different thing** from `ui_win_open_ex`'s `rotatable`: that one governs "does the window follow a rotation", this one governs "**does the system allow rotation at all**".
```c
ui_orient_lock(VML_LOCK_LANDSCAPE);   /* lock to landscape while running */
```
### `ui_touch(int slot, int* out)`
Queries finger number slot (0..9): it writes `out[0]=x out[1]=y out[2]=pressed` and returns 1 when valid (and 0 when the slot is out of range).
**Two players on one screen / virtual joysticks / pinch zoom / chords** rely on it. Note: this is **polling**, so you have to check it yourself every frame.
```c
int t[3];
if (ui_touch(0, t) && t[2]) { /* finger 0 is pressing at (t[0],t[1]) */ }
```
Note: **touch event messages are only posted for slot 0** - a touch message received by `ui_wait` always describes finger 0 only.
So **any program that handles multiple fingers must do its press/release detection by polling** (set a 16-33ms timer and do your own edge detection against the pressed state from the previous tick). If you rely only on event messages, everything works with one finger and **as soon as a second finger goes down the second one is completely invisible** - which is exactly where oddities like "the other finger holding a piano key cannot tap a button" come from. See `examples/c/piano.c` for a ready-made pattern.
### `ui_touch_down(void)`
The "pressed" value (1/0) from the cache of the last `ui_touch_query`.
```c
if (ui_touch_down()) { /* ... */ }
```
### `ui_touch_query(int slot)`
For languages that cannot get a pointer: query once and cache the result, then the three calls below read the cache (the same split of labour as `ui_wait_msg` + `ui_msg_a/b`).
```c
ui_touch_query(0);
if (ui_touch_down()) { x = ui_touch_x(); y = ui_touch_y(); }
```
### `ui_touch_x(void)`
The x from the cache of the last `ui_touch_query`.
```c
int x = ui_touch_x();
```
### `ui_touch_y(void)`
The y from the cache of the last `ui_touch_query`.
```c
int y = ui_touch_y();
```

## Sensors (tilt control)

### `ui_sensor(int kind, int* out)`
Reads the **latest** reading into `out[0..2]`. Returns **1 = valid**, **0 = this device has no such sensor**.

`kind` is one of three - **use the first for tilt control**:

| kind | What it is | Unit |
|---|---|---|
| `VML_SENS_ACCEL`(0) | **Acceleration** (including gravity) | milli-g (`1000` = 1g) |
| `VML_SENS_GYRO`(1) | Angular velocity (how fast it is turning) | thousandths of a degree per second |
| `VML_SENS_ROTATION`(2) | Fused attitude (pitch / roll / azimuth) | thousandths of a degree |

```c
int a[3];
if (ui_sensor(VML_SENS_ACCEL, a)) {
    /* lying flat, a is about (0, 0, 1000); tilted 30 degrees to the right, a[0] is about 500 */
    move_x = 0 - a[0] / 40;     /* note the minus: the ball rolls downhill */
}
```

Note: **why tilt uses the accelerometer and not the gyroscope**. The gyroscope gives **angular velocity**, so getting "the current angle" means integrating it yourself - and **integration drifts** (within tens of seconds it is outside the usable range). At rest the accelerometer reads the **direction of gravity**, which is the tilt itself, and it **does not drift**.

Note: **both axes need a minus sign**. The accelerometer reads "**which axis points up**" - lift the right side and `x` **grows**, while things should slide toward the **low** side. Get it backwards and the symptom is "tilt this way and things run that way", which looks exactly like a sensor mounted upside down when the only mistake is that one line.
(This has **been confirmed on a real device**: raise the right side and the ball rolls left.)

Note: **no reading is not the same as a reading of 0**: only a return of 0 means "there is no such sensor". Mix the two up and the program thinks "the phone is lying flat".

### `ui_sensor_available(int kind)`
Does this device have that sensor? -> 1/0. **You can ask before opening a window.**

### `ui_sensor_rate(int kind, int ms)`
Sampling interval (milliseconds, 0 = the platform default). A sensor draws power as soon as it is on, so a longer interval saves more.

### `ui_sensor_calibrate(int kind)`
Treats the **current attitude** as the zero point ("level calibration"). When a player is lounging on the sofa, "level" is wrong, and quitting and coming back does not fix it - so give them a "tap to calibrate" entry point.

### `ui_sensor_query(int kind)` / `ui_sensor_x/y/z(void)`
For languages that cannot get a pointer (structured like `ui_touch_query`):
call `ui_sensor_query(0)` once to query and cache, then read `ui_sensor_x()` and so on.
```c
ok = ui_sensor_query(0);
if (ok) { x = ui_sensor_x(); y = ui_sensor_y(); z = ui_sensor_z(); }
```

A ready-made example: `examples/c/tilt.c` (a spirit level plus a small ball, with all three sensors drawn on screen).

## Battery and power saving

Running out of power halfway through is the most common "accident" in phone games, and a program can guard against it on its own - draw less when the battery is low, turn off sound effects and remind the player to save. **No permissions are needed.**

### `ui_battery(int* out)`
Reads the battery level and charging state: `out[0]` = level 0-100, `out[1]` = charging(1)/not charging(0).
Returns **1 = valid**, **0 = this device has no battery** (a desktop, or a machine that is always plugged in).

```c
int b[2];
if (ui_battery(b) && b[0] < 15 && b[1] == 0) {
    low_power_mode();      /* not charging and low on power means it is time to economize */
}
```

Note: **"no battery" and "0% battery" are two different things**: only a return of 0 means the former. Faking it with 100 or 0 makes the program think "there is plenty of power" or "the power is gone", when the truth is "this question is meaningless here".

Note: **the battery level changes very slowly** (a few minutes per notch), so glancing at it in logic that already runs every tick is enough - **there is no need to check it every frame**.

### `ui_power_saver(void)`
Is the system's **power-saving mode** on? -> 1/0.

Note: this is **not the same thing** as "the battery is low": it is the **user explicitly asking to save power** (and they may have turned it on manually at 40%). Lowering the frame rate because of it is doing what the user asked, rather than a guess on your part. The two are **independent** - the battery can be full with power saving on.

### `ui_battery_query(void)` / `ui_battery_level(void)` / `ui_battery_charging(void)`
For languages that cannot get a pointer (structured like `ui_touch_query`):
call `ui_battery_query()` once to query and cache, then read `ui_battery_level()` and so on.

A ready-made example: the battery line is drawn along the bottom of `examples/c/tilt.c`.

## Clipboard and sharing

Standard fare for a phone game: **sharing a score**, and **save codes** (encoding a whole round into a short piece of text that the player keeps in their notes and pastes back on another device). **Neither needs permissions.**

```c
char code[64];
ui_clipboard_set("SAVE-1a2b3c");                 /* hand the save code to the player */
if (ui_clipboard_get(code, 64) > 0) { /* read it back and carry on */ }
ui_share_text("I turned the screw 8 times!", "Share score");
```

### `ui_clipboard_set(char* text)` / `ui_clipboard_get(char* buf, int cap)`
Writes / reads the system clipboard. `set` returns 1 = handed to the system, 0 = this platform has no clipboard;
`get` returns the **number of UTF-8 bytes written**, and **-1 = the clipboard is empty**.

Note: **a return of 1 only means "it was handed to the system"**, not "the player has saved it" - writing the clipboard is asynchronous on the platform, and the host does not wait for the result.
Note: when the buffer is too small, `get` returns the **number of bytes actually written** (not the original length) - **compare it with `strlen` to tell whether it was truncated**.
Note: passing an empty string to `set` **does not clear the clipboard** (it returns 0) - that is "could not work it out", not the "clear" you wanted.

### `ui_share_text(char* text, char* title)`
Opens the system share sheet (an empty title string = no title). 1 = shown, 0 = this platform has no share sheet.
Note: it **does not block** - it only brings the sheet up, and the program keeps running.

### `ui_open_url(char* url)`
Opens a link in the system browser. **Only `http://` and `https://` are accepted**; any other scheme returns 0
(allowing any scheme would let a program use the system to open anything).

Note: it **leaves this app** - save your state before calling it. But **do not expect the host to pause for you**: that would be a policy, not a mechanism.

Note: **a return of 0 means "this platform does not have that ability", not failure** - a desktop has no share sheet. The game should carry on as usual (the same handling as "the device has no gyroscope").
