# 多点触控与设备

多指查询、按住判断、方向锁、全屏。

> 完整清单见「[UI 开发](help:vml/ui)」。下面每个接口都带一句用法和一个例子；
> 签名取自 `Lib/c/waycoder_ui.h`（权威来源）。

### `ui_audio_playing(void)`
后台 BGM 还在放吗 → 1/0（没放过、已放完、被停掉都是 0）。
典型用法：等一首放完再进下一段。
```c
ui_audio_play("bgm.mp3", 0);
while (ui_audio_playing()) { ui_wait(msg, 200); }
```
### `ui_immersive(int on)`
隐藏 / 恢复状态栏与导航栏（全屏游戏）。
```c
ui_immersive(1);   /* 进游戏：全屏 */
ui_immersive(0);   /* 退出前恢复 */
```
### `ui_key_down(int key)`
某个键**此刻**按住没有（“持续按住左移”不必自己维护状态表）。
⚠ 它**治不了丢 KeyUp** —— 手指划出按键范围、系统吃掉 CANCEL 都会让 KeyUp 永远不来，
而查询会一直报“按着”。**连发逻辑仍然要自带刹车**（按了没动两次就停 / 总拍数上限）。
```c
if (ui_key_down(VML_KEY_LEFT)) { x = x - 2; }
```
### `ui_orient_lock(int mode)`
锁定屏幕方向：`VML_LOCK_PORTRAIT`(0) / `VML_LOCK_LANDSCAPE`(1) / `VML_LOCK_AUTO`(2)。
⚠ 与 `ui_win_open_ex` 的 `rotatable` 是**两件事**：那个管“转屏时窗口跟不跟”，这个管“**系统让不让转**”。
```c
ui_orient_lock(VML_LOCK_LANDSCAPE);   /* 运行中锁成横屏 */
```
### `ui_touch(int slot, int* out)`
查第 slot 根手指（0..9）：写进 `out[0]=x out[1]=y out[2]=按下`，返回 1 有效（槽位越界 0）。
**双人同屏 / 虚拟摇杆 / 双指缩放**靠它。⚠ 这是**轮询**，要自己每帧查。
```c
int t[3];
if (ui_touch(0, t) && t[2]) { /* 第 0 根手指按在 (t[0],t[1]) */ }
```
### `ui_touch_down(void)`
上一次 `ui_touch_query` 缓存里的“按下”（1/0）。
```c
if (ui_touch_down()) { /* … */ }
```
### `ui_touch_query(int slot)`
拿不到指针的语言用这个：查一次并缓存，下面三个读缓存（与 `ui_wait_msg` + `ui_msg_a/b` 同一套分工）。
```c
ui_touch_query(0);
if (ui_touch_down()) { x = ui_touch_x(); y = ui_touch_y(); }
```
### `ui_touch_x(void)`
上一次 `ui_touch_query` 缓存里的 x。
```c
int x = ui_touch_x();
```
### `ui_touch_y(void)`
上一次 `ui_touch_query` 缓存里的 y。
```c
int y = ui_touch_y();
```
