# 调用宿主

按数字 id 调宿主函数（带类型、零编解码）。

> 完整清单见「[UI 开发](help:vml/ui)」。下面每个接口都带一句用法和一个例子；
> 签名取自 `Lib/c/waycoder_ui.h`（权威来源）。

### `callwithdouble4(double* v)`
同上，参数是 3 个 `double`（走 `D1..D3`），返回覆盖 `D0`。
```c
double v[4];
v[0] = VML_CALL_ECHO_DOUBLE;
v[1] = 1.0; v[2] = 2.0;
double r = callwithdouble4(v);
```
### `callwithfloat8(float* v)`
同上，参数是 7 个 `float`（走 `F1..F7`），返回覆盖 `F0`。
⚠ 调用号也放在 `F0`（`v[0]`）里，所以它必须能被 `float` 精确表示 —— 取小整数就没事。
```c
float v[8];
v[0] = VML_CALL_ECHO_FLOAT;
v[1] = 1.0; v[2] = 2.0;
float r = callwithfloat8(v);
```
### `callwithint8(int* v)`
**按数字 id 调宿主的函数**：`v[0]` 是调用号（见 `VML_CALL_*` 宏）、`v[1..7]` 是参数，
返回值**覆盖 `v[0]`** —— 所以调用完拿不到原来传进去的 `v[0]`，这是设计不是 bug。
比 `ui_call_json` 快得多（参数直接进寄存器、一次调用零编解码），代价是**类型写死在接口上**。
没注册过的 id / 类型对不上都**不崩**，写回负数失败码（-2 参数、-6 没这个号、-9 宿主内部错）。
```c
int v[8];
v[0] = VML_CALL_ECHO_INT;
v[1] = 1; v[2] = 2; v[3] = 3;
int r = callwithint8(v);   /* 1 + 2×10 + 3×100 = 321 */
```
### `callwithlong4(long* v)`
同上，参数是 3 个 `long`（走 `L1..L3`），返回覆盖 `L0`。
64 位值只有长整数寄存器组装得下 —— 通用寄存器是 32 位的。
```c
long v[4];
v[0] = VML_CALL_ECHO_LONG;
v[1] = 1; v[2] = 2;
long r = callwithlong4(v);
```
