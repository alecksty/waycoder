# 本地存档

存最高分这类小数据。

> 完整清单见「[UI 开发](help:vml/ui)」。下面每个接口都带一句用法和一个例子；
> 签名取自 `Lib/c/waycoder_ui.h`（权威来源）。

### `ui_store_get(char* key, char* buf, int cap)`
读一个值：**写进你给的缓冲区**、返回长度（没有这条键返回 -1）。
⚠ 它不是「返回那个整数」—— 字符串读回来，要数字自己转。
```c
char buf[16];
if (ui_store_get("high", buf, 16) > 0) best = atoi(buf);
```
### `ui_store_set(char* key, char* value)`
存一个值。**值也是字符串** —— 存数字要先自己转成字符串（这里没有 sprintf 可用）。
键会自动加前缀，不会和 App 自己的设置打架。
```c
ui_store_set("high", "1200");
```
