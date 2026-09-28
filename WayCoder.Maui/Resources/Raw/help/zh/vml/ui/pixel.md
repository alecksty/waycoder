# 像素读回

读像素、灌色、抓图贴图、截屏。

> 完整清单见「[UI 开发](help:vml/ui)」。下面每个接口都带一句用法和一个例子；
> 签名取自 `Lib/c/waycoder_ui.h`（权威来源）。

### `ui_flood_fill(int x, int y, int color, int border)`
从 (x,y) **灌色**，碰到 border 色就停（四连通）。返回落笔的矩形条数，0 = 没填
（种子点本身就在边界色上时是这样 —— 老程序“点在线上”很常见，不是错误）。
```c
ui_flood_fill(50, 50, 0xFF00FF00, 0xFFFF0000);   /* 在红框里灌绿 */
```
### `ui_get_image(int x, int y, int w, int h)`
存一块画面 → **句柄**（≥1），失败 0。老程序的 `malloc(imagesize(...))` 照写不误，只是那块内存我们不用。
```c
int img = ui_get_image(0, 0, 32, 32);   /* 抓一块 32×32 */
```
### `ui_get_pixel(int x, int y)`
读一个像素的颜色（`0xRRGGBB`；越界 -1）。
⚠ 场景是**保留模式**的，每次调用宿主都要**当场光栅化一次** —— 别放进密集大循环。
```c
int c = ui_get_pixel(100, 100);
if (c == 0xFFFFFF) { /* 那里是白的 */ }
```
### `ui_put_image(int x, int y, int handle, int mode)`
把句柄那块贴到 (x,y)。`mode`：0 = COPY 直接贴 / 1 = XOR 异或（异或要先读目的像素，慢一些）。
```c
ui_put_image(200, 100, img, 0);    /* 贴过去 */
ui_put_image(200, 100, img, 1);    /* 再异或一次 = 擦掉（精灵动画的老做法）*/
```
### `ui_screenshot(char* path)`
把当前窗口存成 PNG（路径相对工作区；不给路径就自动取名落在 `shot/` 下）。返回 1 成功。
```c
ui_screenshot("shot/win.png");
```
