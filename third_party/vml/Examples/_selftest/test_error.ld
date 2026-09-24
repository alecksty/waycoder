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
 * ⚠ 这里的 0 警告是「**源码级**」的：`.ld` 只有在**编译成功**时，链接器才会另打一条
 *   `警告: 库代码里有 21 个未解析标签`（库档固定噪音，不是本文件的诊断 —— 本文件在
 *   代码生成阶段就失败了，根本走不到链接器；别把那条当成本用例的期望产出）。
 * ⚠ 注释只能用 IEC 61131-3 的 (* ... *)：本前端**不认** COM / // / '。
 * ⚠ 与 js / lua 不同，本前端**会**检查未声明的变量 —— 走 CompilerBase 的共用诊断通道
 *   `ReportUndefined`（LadderCompiler/CodeGenerator.Core.cs:615），故诊断码与列号都齐。
 *)
BEGIN
PRINT_STR "test_error.ld"
PRINT_INT undefinedValue
END_PROGRAM
