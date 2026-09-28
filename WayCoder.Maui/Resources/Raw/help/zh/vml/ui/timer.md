# 定时器与随机数

重复定时器、取时间、随机数。

> 完整清单见「[UI 开发](help:vml/ui)」。下面每个接口都带一句用法和一个例子；
> 签名取自 `Lib/c/waycoder_ui.h`（权威来源）。

### `ui_rand(int n)`
随机数：`ui_rand(n)` → **0..n-1**（n ≤ 0 时返回 1，不会崩）。
```c
int n = ui_rand(6);      /* 0..5 */
int side = ui_rand(2);   /* 0 或 1 */
```
### `ui_tick(void)`
开机以来的毫秒数（自己算帧间隔、做动画用）。
```c
int now = ui_tick();
```
### `ui_timer_kill(int timerId)`
停掉一个定时器。
⚠ 它是**重复**的 —— 靠「一定会收到 KeyUp」来停是不可靠的，自己要有刹车（换键即接管 / 按了没动两次就停 / 总拍数上限）。
```c
ui_timer_kill(1);
```
### `ui_timer_set(int interval_ms, int tag)`
起一个**重复**定时器，每 N 毫秒发一条 `VML_MSG_TIMER`（`msg[1]` 是你给的 id）。
```c
ui_timer_set(1, 100);   /* 每 100ms 一条，id=1 */
```
