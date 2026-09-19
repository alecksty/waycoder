# 方块贴图

把一张图切成小格反复贴。

> 完整清单见「[UI 开发](help:vml/ui)」。下面每个接口都带一句用法和一个例子；
> 签名取自 `Lib/c/waycoder_ui.h`（权威来源）。

### `ui_piece_cell(int pid, int rot, int which)`
把某个棋子的第 (列,行) 格贴到屏幕 (x,y)。
```c
ui_piece_cell(0, 0, 0, 40, 60);   /* 棋子 0 的 (0,0) 格 → 屏幕 (40,60) */
```
### `ui_piece_init(void)`
把一块小位图注册成「棋子」，之后用 `ui_piece_cell` 按格子取 —— 方块类游戏用它省掉逐格画。
```c
ui_piece_init(0, "~/pics/tile.png", 4, 4);   /* 第 0 号，切成 4x4 格 */
```
