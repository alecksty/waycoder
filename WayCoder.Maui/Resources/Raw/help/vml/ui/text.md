# 文字

写字、字体、锚点。

> 完整清单见「[UI 开发](help:vml/ui)」。下面每个接口都带一句用法和一个例子；
> 签名取自 `Lib/c/waycoder_ui.h`（权威来源）。

### `ui_set_font(int size, int style, int color, int anchor)`
设一次字体，后面所有 `ui_text_cur` 都用它（省得每次重复传四个参数）。
```c
ui_set_font(18, VML_FONT_BOLD, 0xFFFFFFFF, VML_ANCHOR_CENTER);
ui_text_cur(180, 40, "按方向键退出");
```
### `ui_text(int x, int y, char* s, int color, int size, int anchor)`
在 (x,y) 写一行字。`size` 是字号；`anchor` 决定 (x,y) 指文字的哪一边（`VML_ANCHOR_LEFT` / `CENTER` / `RIGHT`）。
```c
ui_text(180, 40, "得分: 120", 0xFFFFFFFF, 20, VML_ANCHOR_CENTER);
```
### `ui_text_cur(int x, int y, char* s)`
用 `ui_set_font` 设好的字体写字。
```c
ui_text_cur(180, 300, "游戏结束");
```
### `ui_text_styled(int x, int y, char* s, int color, int size, int anchor, int style)`
同上，另加样式（粗体 / 斜体 / 下划线）。
```c
ui_text_styled(10, 10, "标题", 0xFFFFFFFF, 22, VML_ANCHOR_LEFT, VML_FONT_BOLD);
```
