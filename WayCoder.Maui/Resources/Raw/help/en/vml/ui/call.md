# Calling the host

Call host functions by numeric id (typed, with zero encoding or decoding).

> For the full list see "[UI development](help:vml/ui)". Each call below comes with a
> one-line usage note and an example; signatures come from `Lib/c/waycoder_ui.h` (the authoritative source).

### `callwithdouble4(double* v)`
The same calling convention, with 3 `double` arguments (passed in `D1..D3`); the return value overwrites `D0`.
```c
double v[4];
v[0] = VML_CALL_ECHO_DOUBLE;
v[1] = 1.0; v[2] = 2.0;
double r = callwithdouble4(v);
```
### `callwithfloat8(float* v)`
The same, with 7 `float` arguments (passed in `F1..F7`); the return value overwrites `F0`.
Note: the call number also lives in `F0` (`v[0]`), so it must be exactly representable as a `float` - small integers are always safe.
```c
float v[8];
v[0] = VML_CALL_ECHO_FLOAT;
v[1] = 1.0; v[2] = 2.0;
float r = callwithfloat8(v);
```
### `callwithint8(int* v)`
**Calls a host function by numeric id**: `v[0]` is the call number (see the `VML_CALL_*` macros) and `v[1..7]` are the arguments. The return value **overwrites `v[0]`** - so after the call you can no longer read the `v[0]` you passed in. That is by design, not a bug.
It is much faster than `ui_call_json` (the arguments go straight into registers, one call with zero encoding or decoding); the price is that the **types are fixed in the signature**.
An unregistered id or mismatched types **never crash** - they write back a negative error code (-2 arguments, -6 no such call number, -9 internal host error).
```c
int v[8];
v[0] = VML_CALL_ECHO_INT;
v[1] = 1; v[2] = 2; v[3] = 3;
int r = callwithint8(v);   /* 1 + 2x10 + 3x100 = 321 */
```
### `callwithlong4(long* v)`
The same, with 3 `long` arguments (passed in `L1..L3`); the return value overwrites `L0`.
Only the long-integer registers can hold a 64-bit value - the general-purpose registers are 32-bit.
```c
long v[4];
v[0] = VML_CALL_ECHO_LONG;
v[1] = 1; v[2] = 2;
long r = callwithlong4(v);
```
