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
渐变的圆（渐变先用 `ui_gradient` 起个名字）。
```c
ui_gradient("ball", 1, 0xFFFFFFFF, 0xFF2E6FC4, 500, 500, 500, 0);
ui_circle_grad(cx, cy, r, "ball");
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
定义一个渐变刷子并**起个名字**（字符串 id）；之后 `ui_rect_grad` / `ui_circle_grad` / `ui_path` 按名字引用它。
几何是**归一化 0..1000 的整数**（千分之一）：线性给 x1,y1,x2,y2；径向给 cx,cy,r（第 4 个忽略）。
⚠ 传像素会让渐变塌成纯色。
```c
ui_gradient("sky", 0, 0xFF2E6FC4, 0xFFBEE3F7, 0, 0, 0, 1000);
ui_rect_grad(0, 0, w, h, "sky", 0);
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
按 **SVG 路径语法**画（`M L H V C S Q T A Z`，大小写区分绝对/相对）。
`stroke` 描边色（0 = 不描边）、`width` 线宽、`fill` 填充色（0 = 不填充）、
`grad` 传渐变名（给了就用它填充）、`cap` 0平/1圆/2方、`dash` 0/1 虚线。
```c
ui_path("M10,50 Q60,0 110,50 T210,50", 0xFFFF00FF, 3, 0, "", 1, 0);
```
### `ui_pixel(int x, int y, int color)`
画一个点。
```c
ui_pixel(10, 20, 0xFFFFFFFF);
```
### `ui_polygon(int* pts, int count, int fill, int stroke, int width, char* grad)`
画多边形（自动闭合）。`pts` 是 int 数组、**每两个 int 一个点**；`count` 是**点数**。
`fill` / `stroke` 都是**颜色**（不是开关），`width` 是描边线宽，`grad` 传渐变名或空串。
⚠ **顶点必须是具名数组** —— `ui_polygon((int[]){…})` 复合字面量在这条前端上不支持、也不报错。
```c
int pts[] = {10,10, 110,10, 60,90};
ui_polygon(pts, 3, 0xFFFF8800, 0xFFFFFFFF, 2, "");
```
### `ui_polyline(int* pts, int count, int stroke, int width, char* grad)`
折线（不闭合）。参数含义同 `ui_polygon`（`stroke` 是颜色、`width` 是线宽）。
```c
int pts[] = {10,10, 50,60, 90,20};
ui_polyline(pts, 3, 0xFFFFFFFF, 2, "");
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
