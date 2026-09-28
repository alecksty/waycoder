/* callports.c —— 通用宿主调用口（syscall 577–580）自检 / 用法示例
 * callports.c — generic host call ports (syscall 577-580) self-test / usage example
 *
 * 这四个接口让程序**按数字 id 调宿主的函数**：参数直接躺在寄存器里、返回值直接写回
 * These four ports let a program **call a host function by numeric id**: the arguments lie right in the registers
 * 第 0 号寄存器，**一次调用零编解码**（对比 ui_call_json —— 那条要过两趟 JSON）。
 * and the return value is written straight back to register 0 — **one call, zero codec** (compare ui_call_json, which goes through two rounds of JSON).
 *
 *     int    callwithint8   (int*    v);   v[0]=id, v[1..7] → R0..R7；返回覆盖 R0
 *     int    callwithint8   (int*    v);   v[0]=id, v[1..7] → R0..R7; the return value overwrites R0
 *     float  callwithfloat8 (float*  v);   v[0]=id, v[1..7] → F0..F7；返回覆盖 F0
 *     float  callwithfloat8 (float*  v);   v[0]=id, v[1..7] → F0..F7; the return value overwrites F0
 *     long   callwithlong4  (long*   v);   v[0]=id, v[1..3] → L0..L3；返回覆盖 L0
 *     long   callwithlong4  (long*   v);   v[0]=id, v[1..3] → L0..L3; the return value overwrites L0
 *     double callwithdouble4(double* v);   v[0]=id, v[1..3] → D0..D3；返回覆盖 D0
 *     double callwithdouble4(double* v);   v[0]=id, v[1..3] → D0..D3; the return value overwrites D0
 *
 * ⚠ **第 0 个槽既是调用号（进）又是返回值（出）** —— 调完拿不到原来的 v[0]，这是 ABI。
 * ⚠ **slot 0 is both the call id (in) and the return value (out)** — v[0] is gone after the call; that is the ABI.
 *
 * ## 本批注册的是「自检族」（见 Lib/c/waycoder_ui.h 的 VML_CALL_*）
 * ## What is registered here is the "self-test family" (see VML_CALL_* in Lib/c/waycoder_ui.h)
 *
 * 它的返回值是**每个参数按位权拼出来的可分辨数**：传 1,2,3 得 321。
 * Its return value is **a distinguishable number assembled from each argument by positional weight**: pass 1,2,3 and you get 321.
 * 哪个参数没到、参数串没串位、返回值有没有回到程序，一眼就能看出来。
 * Which argument never arrived, whether the arguments got shifted, whether the return value came back to the program — all visible at a glance.
 *
 * ## ⚠ 为什么 float/long/double 那三个是"把位模式摆进 int 数组"再传
 * ## ⚠ Why the float/long/double three pass "the bit pattern laid out in an int array"
 *
 * 不是接口要求这么写 —— 是**当前 C 前端的三个缺陷**逼的（已复现，属编译器侧）：
 * Not because the interface demands it — it is forced by **three defects in the current C front end** (all reproduced, they live on the compiler side):
 *   ① `float[]` / `double[]` 数组元素的**存**到不了内存（标量正常）：
 *   ① a **store** to a `float[]` / `double[]` array element never reaches memory (scalars are fine):
 *      `float v[8]; v[1] = 7.0;` 之后那块内存里是 0；
 *      after `float v[8]; v[1] = 7.0;` that memory holds 0;
 *   ② 全局 `long` / `double` 标量按 32 位存（截断）；
 *   ② global `long` / `double` scalars are stored as 32-bit (truncated);
 *   ③ 同一个 `long[]` 连续存多个元素会丢（实测 4 条只有前 2 条落地）。
 *   ③ storing several elements of one `long[]` in a row loses them (measured: only the first 2 of 4 landed).
 * 所以这里用一个 `int` 数组**手工摆出** float / long / double 的位模式，再把它的地址
 * So here a single `int` array is used to **hand-assemble** the bit patterns of float / long / double, and its address is
 * 当 `float*` / `long*` / `double*` 传进去 —— 这条路是通的（`callwithint8` 那条也顺带
 * passed as `float*` / `long*` / `double*` — that path does work (and the `callwithint8` call incidentally
 * 证明了 int 数组的读写是好的）。等前端那三条修好之后，直接写 `v[1] = 7.0;` 即可。
 * proves that reading and writing an int array is fine). Once those three front-end defects are fixed, just write `v[1] = 7.0;` directly.
 */

#include <waycoder_ui.h>

/* 一块 32 字节的"寄存器内容"缓冲区：int / float / long / double 都从这里摆。
   A 32-byte "register contents" buffer: int / float / long / double are all laid out from here.
   ⚠ 必须是**全局**且是 int 数组 —— 见文件头那三条前端缺陷。
   ⚠ It must be **global** and an int array — see the three front-end defects in the file header. */
int v[8];

int main(void) {
    int r;

    /* ── ① int8：最自然的一种，直接按 int 填 ── */
    /* ── (1) int8: the most natural one, fill it in directly as int ── */
    v[0] = VML_CALL_ECHO_INT;
    v[1] = 1; v[2] = 2; v[3] = 3; v[4] = 4; v[5] = 5; v[6] = 6; v[7] = 7;
    r = callwithint8(v);
    printf("int8      = %d\n", r);          /* 7654321 = 1 + 20 + 300 + … + 7000000 */
    /* 7654321 = 1 + 20 + 300 + ... + 7000000 */

    /* ── ② float8：8 个 float 的位模式（1.0=1065353216, 2.0=1073741824, …）── */
    /* ── (2) float8: the bit patterns of 8 floats (1.0=1065353216, 2.0=1073741824, ...) ── */
    v[0] = 1073741824;                       /* v[0] = 2.0f = 调用号 ECHO_FLOAT */
    /* v[0] = 2.0f = the call id ECHO_FLOAT */
    v[1] = 1088421888;                       /* 7.0f */
    v[2] = 1086324736;                       /* 6.0f */
    v[3] = 1084227584;                       /* 5.0f */
    v[4] = 1082130432;                       /* 4.0f */
    v[5] = 1077936128;                       /* 3.0f */
    v[6] = 1073741824;                       /* 2.0f */
    v[7] = 1065353216;                       /* 1.0f */
    printf("float8    = %d\n", (int)callwithfloat8((float*)v));   /* 1234567 */

    /* ── ③ long4：3 个 long（每个 64 位占两个 int 槽，低半在前）── */
    /* ── (3) long4: 3 longs (each 64-bit value takes two int slots, low half first) ── */
    v[0] = 3; v[1] = 0;                      /* v[0] = 3 = 调用号 ECHO_LONG */
    /* v[0] = 3 = the call id ECHO_LONG */
    v[2] = 1; v[3] = 0;                      /* v[1] = 1 */
    v[4] = 2; v[5] = 0;                      /* v[2] = 2 */
    v[6] = 3; v[7] = 0;                      /* v[3] = 3 */
    printf("long4     = %d\n", (int)callwithlong4((long*)v));     /* 3002001 */

    /* ── ④ double4：3 个 double 的位模式（1.0=0x3FF0000000000000 → 低半 0、高半 1072693248）── */
    /* ── (4) double4: the bit patterns of 3 doubles (1.0=0x3FF0000000000000 → low half 0, high half 1072693248) ── */
    v[0] = 0; v[1] = 1074790400;             /* v[0] = 4.0 = 调用号 ECHO_DOUBLE */
    /* v[0] = 4.0 = the call id ECHO_DOUBLE */
    v[2] = 0; v[3] = 1072693248;             /* v[1] = 1.0 */
    v[4] = 0; v[5] = 1073741824;             /* v[2] = 2.0 */
    v[6] = 0; v[7] = 1074266112;             /* v[3] = 3.0 */
    printf("double4   = %d\n", (int)callwithdouble4((double*)v)); /* 3002001 */

    /* ── ⑤ 宿主信息：1 = 桌面脚手架、2 = 手机 App ── */
    /* ── (5) host info: 1 = desktop scaffolding, 2 = the phone App ── */
    v[0] = VML_CALL_HOST_INFO;
    printf("宿主      = %d\n", callwithint8(v));

    /* ── ⑥ 失败路径：**一个都不许崩**，写回负数失败码（正数才是正常返回）── */
    /* ── (6) failure paths: **not one of them may crash**; a negative failure code is written back (only positive is a normal return) ── */
    v[0] = 9999;                             /* 没注册过这个号 */
    /* this id was never registered */
    printf("未注册    = %d\n", callwithint8(v));   /* -6 */
    /* expected: -6, an id that was never registered */
    v[0] = VML_CALL_ECHO_FLOAT;              /* 这个号注册给 float8 了，却用 int8 口调 */
    /* this id is registered for float8, yet it is called through the int8 port */
    printf("类型不符  = %d\n", callwithint8(v));   /* -2 */
    /* expected: -2, the id belongs to another port */
    v[0] = -5;                               /* 负数调用号 */
    /* a negative call id */
    printf("负数号    = %d\n", callwithint8(v));   /* -2 */
    /* expected: -2, a negative call id */
    return 0;
}
