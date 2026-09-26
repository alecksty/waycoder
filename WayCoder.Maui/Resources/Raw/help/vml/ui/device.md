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
**双人同屏 / 虚拟摇杆 / 双指缩放 / 按和弦**靠它。⚠ 这是**轮询**，要自己每帧查。
```c
int t[3];
if (ui_touch(0, t) && t[2]) { /* 第 0 根手指按在 (t[0],t[1]) */ }
```
⚠⚠ **触摸的「事件消息」只有槽位 0 会投** —— `ui_wait` 收到的触摸消息永远只描述第 0 根手指。
所以**凡是多指的程序，按下/抬起的判定必须走轮询**（挂个 16~33ms 的定时器，
自己用上一拍的按下状态做边沿检测）。只靠事件消息的话，程序在单指下一切正常、
**一旦同时按第二根手指，第二根就完全看不见** —— 这也正是"按住琴键的另一根手指
点不动按钮"那类怪现象的来源。现成写法见 `examples/c/piano.c`。
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

## 传感器（歪手机控制）

### `ui_sensor(int kind, int* out)`
读**最新**读数，写进 `out[0..2]`。返回 **1 = 有效**、**0 = 这台设备没有该传感器**。

`kind` 三选一 —— **做倾斜控制用第一个**：

| kind | 是什么 | 单位 |
|---|---|---|
| `VML_SENS_ACCEL`(0) | **加速度**（含重力） | 毫克（`1000` = 1g） |
| `VML_SENS_GYRO`(1) | 角速度（转得多快） | 千分之一度/秒 |
| `VML_SENS_ROTATION`(2) | 融合姿态（俯仰/翻滚/方位） | 千分之一度 |

```c
int a[3];
if (ui_sensor(VML_SENS_ACCEL, a)) {
    /* 平放时 a ≈ (0, 0, 1000)；往右歪 30° 时 a[0] ≈ 500 */
    move_x = 0 - a[0] / 40;     /* ⚠ 取负：球往低处滚 */
}
```

⚠⚠ **为什么倾斜用加速度计而不是陀螺仪**：陀螺仪给的是**角速度**，要"当前角度"得自己
积分，而**积分会漂**（几十秒就偏出可用范围）。加速度计静止时读到的就是**重力方向**，
也就是倾斜本身，**不漂**。

⚠⚠ **两个轴都要取负号**。加速度计读的是"**哪条轴朝上**"—— 把右边抬起来 `x` 会**变大**，
而东西该往**低**的那边滑。写反的症状是"歪这边、东西往那边跑"，
看起来完全像传感器装反了，其实就错在这一行。

⚠ **读不到 ≠ 读到 0**：返回 0 才是"没有这个传感器"。混在一起程序会以为"手机放平了"。

### `ui_sensor_available(int kind)`
这台设备有没有该传感器 → 1/0。**开窗之前就能问**。

### `ui_sensor_rate(int kind, int ms)`
采样间隔（毫秒，0 = 平台默认）。传感器一开就在耗电，间隔越大越省。

### `ui_sensor_calibrate(int kind)`
把**当前姿态**当成零点（"校准水平"）。玩家躺在沙发上玩时"水平"是错的，
退出重进也还是错的 ⇒ 给一个"点一下校准"的入口。

### `ui_sensor_query(int kind)` / `ui_sensor_x/y/z(void)`
拿不到指针的语言用这一套（与 `ui_touch_query` 同构）：
先 `ui_sensor_query(0)` 查一次并缓存，再读 `ui_sensor_x()` 等。
```c
ok = ui_sensor_query(0);
if (ok) { x = ui_sensor_x(); y = ui_sensor_y(); z = ui_sensor_z(); }
```

现成例子：`examples/c/tilt.c`（水平仪 + 小球，三个传感器的读数都画在屏幕上）。
