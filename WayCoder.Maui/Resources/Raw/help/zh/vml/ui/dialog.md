# 对话框

提示、单选、多选、输入。

> 完整清单见「[UI 开发](help:vml/ui)」。下面每个接口都带一句用法和一个例子；
> 签名取自 `Lib/c/waycoder_ui.h`（权威来源）。

### `ui_dlg_input(char* title, char* prompt, char* buf, int cap)`
要一行文字输入。结果写进你给的缓冲区。
```c
char buf[64];
ui_dlg_input("改名", "新名字：", buf, 64);
```
### `ui_dlg_msg(char* title, char* body, int style)`
弹一个提示框（`style` 传 0 即可）。**会阻塞到用户点掉** —— 游戏结束时报个结果正好。
```c
ui_dlg_msg("游戏结束", "得分 120", 0);
```
### `ui_dlg_multi(char* title, char* body, char* opts, int n)`
多选对话框：`opts` 选项串、`n` 选项个数。返回选中的个数。
```c
int n = ui_dlg_multi("设置", "开哪些？", opts, 3);
```
### `ui_dlg_select(char* title, char* body, char* opts, int n, int def)`
单选对话框：`opts` 是**选项串**、`n` 是选项个数、`def` 是默认选中项。返回选中下标（-1 = 取消）。
```c
int r = ui_dlg_select("游戏结束", "再来一局？", opts, 2, 0);
```
