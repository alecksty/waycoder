# 绘图

点线面、多边形、路径、渐变、贴图。

> 完整清单见「[UI 开发](help:vml/ui)」。下面每个接口都带一句用法和一个例子；
> 签名取自 `Lib/c/waycoder_ui.h`（权威来源）。

### `ui_circle(int cx, int cy, int r, int color, int fill, int lw)`
画圆，`fill` 非 0 填充。
```c
ui_circle(100, 100, 30, 0xFF00FF00, 1, 2);
```
### `ui_circle_grad(int cx, int cy, int r, char* grad)`
带渐变的圆。
```c
ui_circle_grad(100, 100, 60, g, 1, 2);
```
### `ui_clear(int color)`
整屏填一个色（每帧开头调）。颜色一律 `0xAARRGGBB`。
```c
ui_clear(0xFF101020);   /* 深蓝底 */
```
### `ui_ellipse(int cx, int cy, int rx, int ry, int color, int fill, int lw)`
画椭圆（`rx` / `ry` 两个半径）。
```c
ui_ellipse(100, 100, 50, 30, 0xFFFFFF00, 1, 2);
```
### `ui_gradient(char* id, int radial, int color_a, int color_b, int a1, int a2, int a3, int a4)`
定义一个渐变并返回 id（之后 `ui_rect_grad` / `ui_circle_grad` 用）。
坐标是**相对这个图形自己的包围盒**的 0..1，不是屏幕坐标。
```c
int g = ui_gradient(0, 0, 0, 1, 0xFF0000FF, 0xFFFF0000, 0xFFFF00FF);
```
### `ui_icon(int x, int y, char* name, int size, int color)`
画一个内置图标（按名字取，省得自己画）。
```c
ui_icon(10, 10, "star", 24, 0xFFFFD700);
```
### `ui_image(int x, int y, char* path, int w, int h)`
在指定位置画一张图（PNG / JPG / BMP），`w` / `h` 传 0 按原尺寸。
```c
ui_image(20, 20, "~/pics/logo.png", 0, 0);
```
### `ui_line(int x1, int y1, int x2, int y2, int color, int lw)`
画线，`lw` 是线宽。
```c
ui_line(0, 0, 100, 100, 0xFFFF0000, 2);
```
### `ui_path(char* d, int stroke, int width, int fill, char* grad, int cap, int dash)`
按 **SVG 路径语法**画（`M`/`L`/`Q`/`C`/`A`/`Z` 都支持，曲线自动分段）。想画圆角、弧线、曲线图形用它，比拿直线拼省事。
```c
ui_path("M10,50 Q60,0 110,50 T210,50", 0xFFFF00FF, 3, 1);
```
### `ui_pixel(int x, int y, int color)`
画一个点。
```c
ui_pixel(10, 20, 0xFFFFFFFF);
```
### `ui_polygon(int* pts, int count, int fill, int stroke, int width, char* grad)`
画多边形，点用 `int pts[] = {x1,y1, x2,y2, …}` 给，`count` 是**点数**（不是坐标个数）。
```c
int pts[] = {10,10, 110,10, 60,90};
ui_polygon(pts, 3, 0xFFFF8800, 1, 0xFFFFFFFF, 2, -1);
```
### `ui_polyline(int* pts, int count, int stroke, int width, char* grad)`
折线（不闭合）。
```c
int pts[] = {10,10, 50,60, 90,20};
ui_polyline(pts, 3, 0xFFFFFFFF, 2, -1);
```
### `ui_present(void)`
**这一帧画完了**。整个循环里最关键的一句 —— 不调它屏幕不更新。
（宿主也是按它判断「可以出图了」，所以要放在每帧末尾、全部图元画完之后。）
```c
ui_clear(0xFF000000);
ui_text(10, 10, "一帧", 0xFFFFFFFF, 16, 0);
ui_present();
```
### `ui_rect(int x, int y, int w, int h, int color, int fill, int lw, int radius)`
画矩形。`fill` 非 0 填充、`radius` 是圆角半径。
```c
ui_rect(20, 30, 120, 60, 0xFF3366FF, 1, 2, 8);   /* 圆角填充 */
```
### `ui_rect_grad(int x, int y, int w, int h, char* grad, int radius)`
带渐变的矩形（渐变先用 `ui_gradient` 定义）。
```c
ui_rect_grad(0, 0, 200, 100, g, 8);
```
