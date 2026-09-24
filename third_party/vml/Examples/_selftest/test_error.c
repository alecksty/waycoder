// test_error.c —— VML 跨语言「诊断」判据（**故意编不过**）
//
// ⚠ 这是**刻意的诊断用例**，不参与任何「编译通过性」检查
//   （`vml-out-probe` 那套逐字节比对跑的是同目录的 out.*，别把本文件混进去）。
// 用途：验证前端是否报出 error / warning、诊断是否带
//   `文件:行:列: level: 消息 [诊断码]` 四要素、以及编辑器能否据此画气泡。
// 期望产出：1 个 error（未声明的变量）+ 2 个 warning（未使用的函数 / 局部变量）。
// 写法照同目录 out.c（`__stdcall` 声明共享库函数，不 include 头文件）。
__stdcall void puts(char* s);

static int unused_helper() {     // warning: 定义了但从未使用的函数
    return 1;
}

int main() {
    int unused_var = 5;          // warning: 定义了但从未使用的局部变量
    int total = 0;               // 这个真的在用，不该报警告
    int x = undefined_thing;     // error: 未声明的变量（就是它让编译失败）
    total = total + x;
    puts("hi");
    return total;
}
