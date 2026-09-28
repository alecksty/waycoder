# 全能接口

两个字符串进、一个 JSON 出。

> 完整清单见「[UI 开发](help:vml/ui)」。下面每个接口都带一句用法和一个例子；
> 签名取自 `Lib/c/waycoder_ui.h`（权威来源）。

### `ui_call_json(char* fn, char* args_json, char* out_buf, int cap)`
**两个字符串进、一个 JSON 字符串出** —— 查设备信息、调宿主的杂项能力都走它，
不必为每个小功能占一个 syscall 号。结果写进你给的缓冲区。
⚠ **性能敏感的调用别走这里**（一次要过两趟 JSON + 一次内存拷贝）：绘图、输入仍旧走专用接口。
```c
char buf[512];
ui_call_json("sysinfo", "", buf, 512);
puts(buf);   /* {"ok":true,"result":{…}} */
```
### `ui_call_json_at(int i)`
取上一次结果的第 i 个字节。
```c
char c = (char)ui_call_json_at(i);
```
### `ui_call_json_len(void)`
上一次 `ui_call_json_s` 的结果有多长。
```c
int n = ui_call_json_len();
```
### `ui_call_json_print(void)`
把**上一次** `ui_call_json_s` 的结果整份打到 stdout（末尾补换行）—— 调试最省事，
各语言的自测也都靠它把结果原样打出来。⚠ 无参：打印的是上一次的结果，不是带参数再调一次。
```c
ui_call_json_s("version", "");
ui_call_json_print();
```
### `ui_call_json_s(char* fn, char* args_json)`
同上，但**不用给缓冲区**（适合拿不到指针的语言），配 `_len` / `_at` 读结果。
```c
int n = ui_call_json_s("version", "");
for (int i = 0; i < n; i++) putchar(ui_call_json_at(i));
```
