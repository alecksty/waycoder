# Getting messages

How to receive touch / key / timer messages.

> For the full list see "[UI development](help:vml/ui)". Each call below comes with a
> one-line usage note and an example; signatures come from `Lib/c/waycoder_ui.h` (the authoritative source).

### `ui_msg_a(void)`
The **first argument** of the current message (the touch x, the key code, the timer id...).
```c
int x = ui_msg_a();
```
### `ui_msg_b(void)`
The **second argument** of the current message (the touch y...).
```c
int y = ui_msg_b();
```
### `ui_msg_clear(void)`
Empties the message queue (use it when switching scenes or starting a new round, so keystrokes from the previous round are not eaten by this one).
```c
ui_msg_clear();
```
### `ui_msg_count(void)`
How many messages are still queued (a glance at it tells you when to drop a backlog).
```c
if (ui_msg_count() > 8) ui_msg_clear();
```
### `ui_msg_type(void)`
The type of the current message (so you do not have to keep `msg[0]` in your head).
```c
if (ui_msg_type() == VML_MSG_TOUCHDOWN) { /* handle it */ }
```
### `ui_poll(int* msg)`
**Does not wait**: returns `VML_MSG_NONE` when there is no message. Use this for continuous animation (with your own pacing); event-driven programs use `ui_wait` (which saves power the rest of the time).
Note: it takes only `int* msg` - there is **no timeout**, so do not write it the way you write `ui_wait`.
```c
int m[4];
while (ui_win_closed() == 0) {
    if (ui_poll(m) == VML_MSG_TOUCHDOWN) { /* handle it */ }
    /* draw a frame */
    ui_present();
}
```
### `ui_poll_ex(int* msg, int keep)`
The version of `ui_poll` that lets you choose whether the message is **kept or consumed after reading**.
```c
int m[4];
ui_poll_ex(m, VML_MSG_CONSUME);
```
### `ui_poll_msg(void)`
The **pointer-free** version (for languages that cannot get an array pointer): returns `VML_MSG_NONE` when there is nothing, and the arguments are read with `ui_msg_a()` / `ui_msg_b()`.
```c
if (ui_poll_msg() == VML_MSG_TOUCHDOWN) { int x = ui_msg_a(); int y = ui_msg_b(); }
```
### `ui_wait(int* msg, int timeout_ms)`
**Waits** for one message; the argument is `int msg[4]`. Returns the message type (see the table below); `timeout=0` means **wait forever**.
```c
int m[4];
int t = ui_wait(m, 0);   /* wait forever */
```
### `ui_wait_ex(int* msg, int timeout_ms, int keep)`
The same; the third argument decides whether the message is **kept or consumed** after it is read (`VML_MSG_KEEP` / `VML_MSG_CONSUME`).
```c
int m[4];
ui_wait_ex(m, 0, VML_MSG_KEEP);   /* do not pop it after reading */
```
### `ui_wait_msg(int timeout_ms)`
The **pointer-free** waiting version: returns the type when one arrives, or `VML_MSG_NONE` on timeout.
```c
if (ui_wait_msg(500) == VML_MSG_TIMER) { /* one tick has passed */ }
```
