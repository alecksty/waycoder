# 收消息

触摸 / 按键 / 定时器消息怎么收。

> 完整清单见「[UI 开发](help:vml/ui)」。下面每个接口都带一句用法和一个例子；
> 签名取自 `Lib/c/waycoder_ui.h`（权威来源）。

### `ui_msg_a(void)`
当前消息的**第一个参数**（触摸的 x、按键的键码、定时器的 id…）。
```c
int x = ui_msg_a();
```
### `ui_msg_b(void)`
当前消息的**第二个参数**（触摸的 y…）。
```c
int y = ui_msg_b();
```
### `ui_msg_clear(void)`
清空消息队列（切场景 / 重开一局时用，免得把上一局的按键吃进来）。
```c
ui_msg_clear();
```
### `ui_msg_count(void)`
队列里还积着几条（想丢掉积压时可以看一眼）。
```c
if (ui_msg_count() > 8) ui_msg_clear();
```
### `ui_msg_type(void)`
当前消息的类型（省得把 `msg[0]` 记在脑子里）。
```c
if (ui_msg_type() == VML_MSG_TOUCHDOWN) { /* 处理 */ }
```
### `ui_poll(int* msg)`
**不等**，没有就返回 `VML_MSG_NONE`。连续动画用这个（配自己的节拍）；事件驱动的用 `ui_wait`（常态省电）。
```c
int m[4];
while (ui_win_closed() == 0) {
    if (ui_poll(m, 0) == VML_MSG_TOUCHDOWN) { /* 处理 */ }
    /* 画一帧 */
    ui_present();
}
```
### `ui_poll_ex(int* msg, int keep)`
`ui_poll` 的带「读完后留不留」版本。
```c
int m[4];
ui_poll_ex(m, 0, VML_MSG_CONSUME);
```
### `ui_poll_msg(void)`
只取指定类型，没有就返回 `VML_MSG_NONE`。
```c
int m[4];
if (ui_poll_msg(m, VML_MSG_KEYDOWN, 0)) { /* 处理 */ }
```
### `ui_wait(int* msg, int timeout_ms)`
**等**一条消息，参数是 `int msg[4]`。返回消息类型（见下表）；`timeout=0` 表示**一直等**。
```c
int m[4];
int t = ui_wait(m, 0);   /* 一直等 */
```
### `ui_wait_ex(int* msg, int timeout_ms, int keep)`
同上，第三个参数决定读完之后**留不留**这条消息（`VML_MSG_KEEP` / `VML_MSG_CONSUME`）。
```c
int m[4];
ui_wait_ex(m, 0, VML_MSG_KEEP);   /* 读完不弹掉 */
```
### `ui_wait_msg(int timeout_ms)`
只等**指定类型**的消息（其余留在队列里）。
```c
int m[4];
ui_wait_msg(m, VML_MSG_TOUCHDOWN, 0);
```
