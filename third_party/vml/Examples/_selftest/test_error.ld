(* test_error.ld —— VML 跨语言「诊断」判据（**故意编不过**）
 *
 * ⚠ 这是**刻意的诊断用例**，不参与任何「编译通过性」检查
 *   （`vml-out-probe` 那套逐字节比对跑的是同目录的 out.*，别把本文件混进去）。
 * 用途：验证前端是否报出 error / warning、诊断是否带齐
 *   `文件:行:列: level: 消息 [诊断码]` 五要素、以及编辑器能否据此画气泡。
 * 期望产出：1 个 error（未声明的变量，行:列与诊断码齐全）、0 个 warning。
 *
 * ⚠ **0 个警告不是漏写**：`WarnUnused` 全仓**只有 CCompiler 在调**
 *   （CompilerBase/CodeGeneratorBase.cs:130），ladder 前端没有任何警告出口。
 * ⚠ 注释只能用 IEC 61131-3 的 (* ... *)：本前端**不认** COM / // / '。
 * ⚠ 与 js / lua 不同，本前端**会**检查未声明的变量 —— 走 CompilerBase 的共用诊断通道
 *   `ReportUndefined`（LadderCompiler/CodeGenerator.Core.cs:615），故诊断码与列号都齐。
 *)
BEGIN
PRINT_STR "test_error.ld"
PRINT_INT undefinedValue
END_PROGRAM
