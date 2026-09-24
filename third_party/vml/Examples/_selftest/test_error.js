// test_error.js —— VML 跨语言「诊断」判据（**故意编不过**）
//
// ⚠ 这是**刻意的诊断用例**，不参与任何「编译通过性」检查
//   （`vml-out-probe` 那套逐字节比对跑的是同目录的 out.*，别把本文件混进去）。
// 用途：验证 error / warning 的报出与要素（文件 / 行 / 列 / 诊断码）+ 编辑器气泡。
// 期望产出：1 个 warning + 1 个 error。
//
// ⚠ **warning 刻意用「缺失头文件」造**：预处理器那条 `[Preprocessor_IncludeNotFound]`（1201）
//   是**默认就开**的真诊断，自带 文件:行 + 诊断码；它只产 warning、不产 error ——
//   本文件的 error 仍是下面那句未声明引用。「未使用变量」那类警告本前端产不出
//   （`WarnUnused` 全仓**只有 CCompiler 在调**，CompilerBase/CodeGeneratorBase.cs:130），
//   下面的 unused_var 是**刻意留的探针**：前端哪天补上未使用诊断，它会立刻现形。
//   ⚠ 本前端的这条 warning 报的文件名是 `<unknown>`（不是真实路径），故气泡锚不到路径。
// ⚠ **错误来自未声明的函数，不是变量**：本前端不检查未声明的变量 —— 查不到就当场建一个初值 0
//   的槽继续生成（JavaScriptCompiler/CodeGenerator.Expressions.cs）；错误只能由**调用未声明的
//   函数**触发，由链接器报出，位置只到 `文件:行`（**无列、无诊断码**）。
#include <nosuchheader.h>
native function println_str(s) {}

function main() {
    var unused_var = 5;             // 探针：期望 warning（当前前端产不出）
    println_str("test_error.js");   // 这一句是正常的
    undefined_func(1);              // error：未声明的函数（链接期报出）
}
