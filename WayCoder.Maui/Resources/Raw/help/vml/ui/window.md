# 窗口

开窗、问尺寸、关窗、屏幕方向。

> 完整清单见「[UI 开发](help:vml/ui)」。下面每个接口都带一句用法和一个例子；
> 签名取自 `Lib/c/waycoder_ui.h`（权威来源）。

### `ui_orientation(void)`
设备方向：`VML_ORIENT_PORTRAIT`(0) / `VML_ORIENT_LANDSCAPE`(1)。
**别拿 `scr_w > scr_h` 去推** —— 那是可用绘图区（会随手柄收起而变化），方向是设备本身的属性。
```c
if (ui_orientation() == VML_ORIENT_LANDSCAPE) { /* 横排 */ }
```
### `ui_scr_h(void)`
可用绘图区的高。
```c
int h = ui_scr_h();
```
### `ui_scr_w(void)`
可用绘图区的宽（**开窗前也能问**）。
```c
int w = ui_scr_w();
```
### `ui_win_close(void)`
关掉窗口（程序自己结束用）。
```c
ui_win_close();
```
### `ui_win_closed(void)`
用户是不是已经关窗了（主循环的退出条件）。
```c
while (ui_win_closed() == 0) { … }
```
### `ui_win_open(char* title, int w, int h)`
开一个绘图窗口。**先问 `ui_scr_w/h()` 拿可用绘图区，再按它开** —— 写死尺寸在小屏上会溢出。
```c
int w = ui_scr_w(), h = ui_scr_h();
ui_win_open("我的游戏", w, h);
```
### `ui_win_open_ex(char* title, int w, int h, int rotatable, int gamepad)`
同上，另加两个**开窗前就生效**的声明：转屏策略、要不要手柄区。
```c
/* 五子棋：只竖屏 + 不要手柄 */
ui_win_open_ex("五子棋", w, h, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
/* 赛车：锁横屏 + 要手柄 */
ui_win_open_ex("赛车", w, h, VML_WIN_LANDSCAPE, VML_WIN_NEED_GAMEPAD);
```
