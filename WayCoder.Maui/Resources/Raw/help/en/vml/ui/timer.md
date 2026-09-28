# Timers and random numbers

Repeating timers, reading the clock, random numbers.

> For the full list see "[UI development](help:vml/ui)". Each call below comes with a
> one-line usage note and an example; signatures come from `Lib/c/waycoder_ui.h` (the authoritative source).

### `ui_rand(int n)`
A random number: `ui_rand(n)` gives **0..n-1** (for n <= 0 it returns 1, so it never crashes).
```c
int n = ui_rand(6);      /* 0..5 */
int side = ui_rand(2);   /* 0 or 1 */
```
### `ui_tick(void)`
Milliseconds since boot (for measuring frame intervals yourself, and for animation).
```c
int now = ui_tick();
```
### `ui_timer_kill(int timerId)`
Stops a timer.
Note: the timer is **repeating** - relying on "a KeyUp will always arrive" to stop it is not reliable, so build in your own brake (a new key takes over / stop after two ticks with no movement / a cap on the total number of ticks).
```c
ui_timer_kill(1);
```
### `ui_timer_set(int interval_ms, int tag)`
Starts a **repeating** timer that posts one `VML_MSG_TIMER` every N milliseconds (`msg[1]` is the id you passed in).
```c
ui_timer_set(1, 100);   /* one every 100ms, id=1 */
```
