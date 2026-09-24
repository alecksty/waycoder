// test_error.m —— VML 跨语言「诊断」判据（**故意编不过**）
//
// ⚠ 这是**刻意的诊断用例**，不参与任何「编译通过性」检查
//   （`vml-out-probe` 那套逐字节比对跑的是同目录的 out.*，别把本文件混进去）。
// 用途：验证 error / warning 的报出与要素（文件 / 行 / 列 / 诊断码）+ 编辑器气泡。
// 期望产出：1 个 warning + 1 个 error，两者要素齐全。
//
// ⚠ **warning 刻意用「缺失头文件」造**：预处理器那条 `[Preprocessor_IncludeNotFound]`（1201）
//   是**默认就开**的真诊断，自带 文件:行 + 诊断码；它只产 warning、不产 error ——
//   本文件的 error 仍是下面那句未声明引用。「未使用变量」那类警告本前端产不出
//   （`WarnUnused` 全仓**只有 CCompiler 在调**，CompilerBase/CodeGeneratorBase.cs:130）；
//   它另有 @try/@catch/@throw 三条 MCU 模式警告，但被 `WarningLevel`（默认 0，
//   CompilerBase/CompilerPluginBase.cs:77）挡死 ⇒ 实测静默。unusedCount 是刻意留的探针。
// ⚠ 与 js / lua 不同，本前端**会**检查未声明的变量（**读**侧）—— 走共用诊断通道
//   `ReportUndefined`（ObjCCompiler/CodeGenerator.cs:87），故诊断码与列号都齐。
#include <nosuchheader.h>
int main() {
    int unusedCount = 5;             // 探针：期望 warning（当前前端产不出）
    printf("test_error.m");          // 这一句是正常的
    printf("%d", undefinedValue);    // error：未声明的变量
    return 0;
}
