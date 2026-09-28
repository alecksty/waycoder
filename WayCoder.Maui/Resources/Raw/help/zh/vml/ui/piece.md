# 方块贴图

把一张图切成小格反复贴。

> 完整清单见「[UI 开发](help:vml/ui)」。下面每个接口都带一句用法和一个例子；
> 签名取自 `Lib/c/waycoder_ui.h`（权威来源）。

### `ui_piece_cell(int pid, int rot, int which)`
取某个棋子的某一格：`pid` 棋子号、`rot` 旋转、`which` 第几格。
```c
ui_piece_cell(0, 0, 3);   /* 0 号棋子、不旋转、第 3 格 */
```
### `ui_piece_init(void)`
初始化棋子贴图表（无参版本；具体形态见 `Lib/shared/src/vmlui.c`）。
```c
ui_piece_init();
```
