# 整数网格

棋盘 / 地图这种二维状态。

> 完整清单见「[UI 开发](help:vml/ui)」。下面每个接口都带一句用法和一个例子；
> 签名取自 `Lib/c/waycoder_ui.h`（权威来源）。

### `ui_gclear(void)`
清空网格。棋盘 / 地图这种二维状态用它 —— **比语言自带的数组可靠**（有的前端数组写入读不回来，见「22 种语言」里各语言的坑）。
```c
ui_gclear();
```
### `ui_gget(int idx)`
读一格，没写过返回 0。
```c
int v = ui_gget(y * 10 + x);
```
### `ui_gset(int idx, int val)`
写一格，`i` 是**一维下标**（`row * 宽 + col`）。
```c
ui_gset(y * 10 + x, 1);   /* 10 列的棋盘，(x,y) 落子 */
```
