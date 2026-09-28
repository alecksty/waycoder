// test_error.cpp —— VML 跨语言「诊断」判据（**故意编不过**）
// test_error.cpp -- VML cross-language "diagnostics" probe (**deliberately does not compile**)
//
// ⚠ 这是**刻意的诊断用例**，不参与任何「编译通过性」检查
// ⚠ This is a **deliberate diagnostic case**; it takes part in no "compile-cleanliness" check
//   （`vml-out-probe` 那套逐字节比对跑的是同目录的 out.*，别把本文件混进去）。
//   (the byte-for-byte comparison in `vml-out-probe` runs against the out.* files in this same directory -- do not mix this file into it).
// 用途：验证前端是否报出 error / warning、诊断是否带
// Purpose: verify whether the frontend reports error / warning, whether a diagnostic carries
//   `文件:行:列: level: 消息 [诊断码]` 四要素、以及编辑器能否据此画气泡。
//   the four fields `file:line:col: level: message [diag-code]`, and whether the editor can draw a squiggle from it.
// 期望产出：1 个 error（未声明的变量）、**0 个 warning** —— C++ 前端没有可达的
// Expected output: 1 error (undeclared variable), **0 warnings** -- the C++ frontend has no reachable
//   警告通道（它唯一的 WarningEmitter 只在 `WarningLevel > 0` 时才说话，而默认是 0）。
//   warning channel (its only WarningEmitter speaks only when `WarningLevel > 0`, and the default is 0).
// 写法照同目录 out.cpp（声明共享库函数，不 include 头文件）。
// Written like out.cpp in this same directory (it declares the shared-library functions, no header is included).
void println_str(char* s);

int main() {
    int unused_var = 5;          // 不报 warning：C++ 前端没有「未使用」检查
    // no warning: the C++ frontend has no "unused" check
    int total = 0;               // 这个真的在用，不该报警告
    // this one is really used, it should not raise a warning
    int x = undefined_thing;     // error: 未声明的变量（就是它让编译失败）
    // error: undeclared variable (this is the one that makes the compile fail)
    total = total + x;
    println_str("hi");
    return total;
}
