// test_error.kt —— VML 跨语言「诊断」判据（**故意编不过**）
//
// ⚠ 这是**刻意的诊断用例**，不参与任何「编译通过性」检查
//   （`vml-out-probe` 那套逐字节比对跑的是同目录的 out.*，别把本文件混进去）。
// 用途：验证前端是否报出 error / warning、诊断是否带齐
//   `文件:行:列: level: 消息 [诊断码]` 五要素、以及编辑器能否据此画气泡。
// 期望产出：1 个 error（未声明的变量，行:列与诊断码齐全）、0 个 warning。
//
// ⚠ **0 个警告不是漏写**：`WarnUnused` 全仓**只有 CCompiler 在调**
//   （CompilerBase/CodeGeneratorBase.cs:130），kotlin 前端没有任何未使用诊断的出口。
//   下面的 unusedCount 是**刻意留的探针**：前端哪天补上未使用诊断，它会立刻现形。
// ⚠ 与 js / lua 不同，本前端**会**检查未声明的变量 —— 走 CompilerBase 的共用诊断通道
//   `ReportUndefined`（KotlinCompiler/CodeGenerator.cs:319），故诊断码与列号都齐。
fun main() {
    val unusedCount = 5            // 探针：期望 warning（当前前端产不出）
    println_str("test_error.kt")   // 这一句是正常的
    println_int(undefinedValue)    // error：未声明的变量
}
