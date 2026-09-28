# 命令行参数

程序自己的 -l 这类开关。

> 完整清单见「[UI 开发](help:vml/ui)」。下面每个接口都带一句用法和一个例子；
> 签名取自 `Lib/c/waycoder_ui.h`（权威来源）。

### `ui_arg(int i, char* buf, int cap)`
把第 i 个参数拷进 buf，返回长度（越界 -1）。`argv[0]` 是程序名 ⇒ 用户给的第一个是 `ui_arg(1,…)`。
```c
char buf[64];
if (ui_arg(1, buf, sizeof(buf)) > 0) { /* 用了 -l 这类开关 */ }
```
### `ui_argc(void)`
参数个数（含程序名，恒 ≥ 1）。
⚠ `int main(int argc, char **argv)` 的程序**不需要**它们（入口帧直接给）—— 这两个是给
`int main(void)` 的程序和非 C 语言绑定用的。
```c
int n = ui_argc();   /* 手机：vml run prog.c -l ⇒ n = 2 */
```
