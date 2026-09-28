# 绘图

点线面、多边形、路径、渐变、贴图。

> 完整清单见「[UI 开发](help:vml/ui)」。下面每个接口都带一句用法和一个例子；
> 签名取自 `Lib/c/waycoder_ui.h`（权威来源）。

### `ui_brush_linear(int color_a, int color_b, int x1, int y1, int x2, int y2)`
造一个**线性渐变刷子**。几何是**千分之一**（0..1000），相对**形状自己的包围盒**：`0,0,1000,0` = 从左到右。
```c
int b = ui_brush_linear(0xFFFF3020, 0xFF2050FF, 0, 0, 1000, 0);
ui_set_fill(b);
```
### `ui_brush_named(char* gradId)`
按**名字**引用一个已经 `ui_gradient` 定义过的渐变 → 句柄。
⚠ 它是**引用型**：自己不定义渐变，所以依赖程序当帧先调过 `ui_gradient`，而 `ui_clear` 会把那条定义清掉。
```c
ui_gradient("sky", 0, 0xFF2E6FC4, 0xFFBEE3F7, 0, 0, 0, 1000);
int b = ui_brush_named("sky");
ui_set_fill(b);
```
### `ui_brush_radial(int color_a, int color_b, int cx, int cy, int r)`
造一个**径向渐变刷子**（中心 → 四周）。`cx cy r` 同样千分之一，`500,500,500` = 居中。
```c
int b = ui_brush_radial(0xFFFFE060, 0xFF204020, 500, 500, 500);
ui_set_fill(b);
```
### `ui_brush_solid(int color)`
造一个**纯色刷子**。返回句柄（≥1；0 = 失败），交给 `ui_set_fill` / `ui_set_pen` 用。
⚠ 其实**直接传颜色**也行（颜色 = 只有一个色标的刷子），这个函数是给「先造一批刷子、之后换着用」的场合。
```c
int b = ui_brush_solid(0xFF2A3346);
ui_set_fill(b);
```
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
### `ui_draw_circle(int cx, int cy, int r)`
用当前刷子画圆。
```c
ui_draw_circle(100, 100, 40);
```
### `ui_draw_ellipse(int cx, int cy, int rx, int ry)`
用当前刷子画椭圆。
```c
ui_draw_ellipse(100, 100, 50, 30);
```
### `ui_draw_heart(int cx, int cy, int size)`
用当前刷子画心形。
```c
ui_set_fill(0xFFFF4D6D);
ui_draw_heart(100, 100, 60);
```
### `ui_draw_line(int x1, int y1, int x2, int y2)`
用当前**画笔**画直线。
```c
ui_set_pen(0xFFE06C50, 3, 0, 0, 0);
ui_draw_line(10, 10, 120, 60);
```
### `ui_draw_path(char* d)`
用当前刷子画 SVG 路径（`M L C Q A Z`，大小写区分绝对/相对；多子路径按奇偶规则挖洞）。
```c
ui_set_fill(0xFF4ADE80);
ui_draw_path("M 20 80 L 60 20 L 100 80 Z");
```
### `ui_draw_pie(int cx, int cy, int r, int a0, int a1)`
用当前刷子画扇形：半径 / 起始角 / 结束角（度）。
```c
ui_set_fill(0xFFE06C50);
ui_draw_pie(100, 100, 50, 0, 120);
```
### `ui_draw_poly(int* pts, int count, int close)`
用当前刷子画多边形（`close=1` 自动闭合）或折线（`close=0`）。`pts` 每两个 int 一个点，`count` 是**点数**。
```c
int tri[6];
tri[0]=60; tri[1]=10; tri[2]=90; tri[3]=70; tri[4]=30; tri[5]=70;
ui_set_fill(0xFF4ADE80);
ui_draw_poly(tri, 3, 1);
```
### `ui_draw_rect(int x, int y, int w, int h, int radius)`
用**当前刷子**画矩形（`radius > 0` 即圆角）。
```c
ui_set_fill(0xFF4ADE80);
ui_draw_rect(20, 20, 120, 60, 8);
```
### `ui_draw_regular(int cx, int cy, int r, int n, int rot)`
用当前刷子画正多边形：半径 / 边数 / 旋转角(度)。
```c
ui_set_fill(0xFF4ADE80);
ui_draw_regular(100, 100, 50, 6, 0);
```
### `ui_draw_ring(int cx, int cy, int r_out, int r_in)`
用当前刷子画圆环（外半径 / 内半径，中间的洞靠奇偶规则挖）。
```c
ui_set_fill(0xFF4A90D9);
ui_draw_ring(100, 100, 50, 30);
```
### `ui_draw_star(int cx, int cy, int r_out, int r_in, int points, int rot)`
用当前刷子画星形：外半径 / 内半径 / 角数 / 旋转角(度)。
```c
ui_set_fill(0xFFFFD700);
ui_draw_star(100, 100, 50, 22, 5, 0);
```
### `ui_draw_text(int x, int y, char* s)`
用**当前文字属性**（`ui_set_font`）画一行字。
```c
ui_set_font(24, 0, 0xFFFFFFFF, VML_ANCHOR_CENTER);
ui_draw_text(100, 40, "你好");
```
### `ui_ellipse(int cx, int cy, int rx, int ry, int color, int fill, int lw)`
画椭圆（`rx` / `ry` 两个半径）。
```c
ui_ellipse(100, 100, 50, 30, 0xFFFFFF00, 1, 2);
```
### `ui_ellipse_grad(int cx, int cy, int rx, int ry, char* grad)`
渐变填充的椭圆。与 `ui_rect_grad` / `ui_circle_grad` 是一组（那批接口当时**漏了椭圆**）。
```c
ui_gradient("ball", 1, 0xFFFFFFFF, 0xFF2E6FC4, 500, 500, 500, 0);
ui_ellipse_grad(cx, cy, rx, ry, "ball");
```
### `ui_gradient(char* gradId, int radial, int color_a, int color_b, int a1, int a2, int a3, int a4)`
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
### `ui_set_fill(int brush)`
设置**填充刷子**。传刷子句柄或颜色都行；传 0 = 不填充（空心）。
```c
ui_set_fill(0xFF2A3346);        /* 直接给颜色 */
ui_draw_rect(20, 20, 120, 60, 8);
```
### `ui_set_pen(int brush, int width, int cap, int dash, int arrow)`
设置**画笔**（描边）= 刷子 + 线宽 + 线帽 + 虚线 + 箭头。传 0 = 不描边。
线帽用 `VML_CAP_BUTT/ROUND/SQUARE`，箭头用 `VML_ARROW_*`。
⚠ 本批**画笔只支持纯色**（渐变描边还没落地，给渐变句柄会记一次警告并退回它的起始色）。
```c
ui_set_fill(0xFF2A3346);
ui_set_pen(0xFFF2F6FA, 3, VML_CAP_ROUND, 0, VML_ARROW_NONE);
ui_draw_rect(20, 20, 120, 60, 8);
```
### `ui_set_text_brush(int brush)`
设置**文字刷子**（配合 `ui_set_font` + `ui_draw_text`）。传 0 = 回到 `ui_set_font` 给的颜色。
```c
ui_set_text_brush(0xFFFFD700);
ui_set_font(24, VML_FONT_BOLD, 0, VML_ANCHOR_CENTER);
ui_draw_text(100, 40, "标题");
```
