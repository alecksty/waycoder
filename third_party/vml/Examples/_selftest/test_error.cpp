// test_error.cpp —— VML 跨语言「诊断」判据（**故意编不过**）
//
// ⚠ 这是**刻意的诊断用例**，不参与任何「编译通过性」检查
//   （`vml-out-probe` 那套逐字节比对跑的是同目录的 out.*，别把本文件混进去）。
// 用途：验证前端是否报出 error / warning、诊断是否带
//   `文件:行:列: level: 消息 [诊断码]` 四要素、以及编辑器能否据此画气泡。
// 期望产出：1 个 error（未声明的变量）、**0 个 warning** —— C++ 前端没有可达的
//   警告通道（它唯一的 WarningEmitter 只在 `WarningLevel > 0` 时才说话，而默认是 0）。
// 写法照同目录 out.cpp（声明共享库函数，不 include 头文件）。
void println_str(char* s);

int main() {
    int unused_var = 5;          // 不报 warning：C++ 前端没有「未使用」检查
    int total = 0;               // 这个真的在用，不该报警告
    int x = undefined_thing;     // error: 未声明的变量（就是它让编译失败）
    total = total + x;
    println_str("hi");
    return total;
}
