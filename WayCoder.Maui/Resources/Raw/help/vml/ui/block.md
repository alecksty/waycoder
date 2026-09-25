# 矢量图块

录一次、带旋转缩放地反复贴（精灵用这个）。

> 完整清单见「[UI 开发](help:vml/ui)」。下面每个接口都带一句用法和一个例子；
> 签名取自 `Lib/c/waycoder_ui.h`（权威来源）。

### `ui_create_block(int w, int h, int color)`
开始**录制**一个图块。录的是绘图指令、不是像素 —— 贴的时候能旋转缩放，放大也不糊。
⚠ 一次录、一直贴；**每帧重录会在 128 帧后拿不到句柄**（块表不随 `ui_clear` 清）。
```c
bid = ui_create_block(30, 20, 0);   /* 30×20 的块 */
```
### `ui_draw_block(int block, int x, int y, int sx, int sy, int rot)`
贴一个图块。**(x,y) 是块的中心**、绕中心旋转；缩放是**千分比**（1000 = 原尺寸）、角度是度。
⚠ 缩放取**负值就是镜像**（`-1000` = 水平翻转）—— 同一个精灵朝左朝右不必录两份。
```c
ui_draw_block(bid, 200, 300, 720, 720, 45);     /* 缩到 72%、转 45° */
ui_draw_block(bid, 200, 300, -1000, 1000, 0);   /* 水平镜像 */
```
### `ui_draw_block_at(int block, int x, int y, int sx, int sy, int rot)`
同上，但 **(x,y) 是块的左上角**、绕左上角转。
```c
ui_draw_block_at(bid, 10, 10, 2000, 1000, 0);   /* 左上角在(10,10)、横向放大一倍 */
```
### `ui_end_block(void)`
结束录制，返回图块句柄（≥1；0 = 没录成）。
```c
bid = ui_end_block();
```
