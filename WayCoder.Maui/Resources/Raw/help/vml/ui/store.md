# 本地存档

存最高分这类小数据。

> 完整清单见「[UI 开发](help:vml/ui)」。下面每个接口都带一句用法和一个例子；
> 签名取自 `Lib/c/waycoder_ui.h`（权威来源）。

### `ui_store_get(char* key, char* buf, int cap)`
读一个值，没存过返回 0。
```c
int best = ui_store_get("high");
```
### `ui_store_set(char* key, char* value)`
存一个值（键会自动加前缀，不会和 App 自己的设置打架）。
```c
ui_store_set("high", 1200);
```
