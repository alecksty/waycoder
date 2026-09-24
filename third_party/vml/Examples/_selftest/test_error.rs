// test_error.rs —— VML 诊断自测用例（**故意含错，不参与编译通过性检查**）
//
// 用途：验证编译器的错误/警告输出、诊断列表、以及编辑器能否显示气泡。
// 预期：1 个错误、**0 个警告**（详见下）。
// 约束：确定性 —— 无随机数、不读文件、不交互、无死循环；错误来自源码本身。
//
// ⚠ `unused_var` 是刻意留的「未使用变量」探针。Rust 前端不报未使用变量
//   （`WarnUnused` 只有 CCompiler 在调），因此本文件只产 1 条错误。
//   与真正的 rustc 相反：本前端**不查未使用、也不查借用**，只查未声明的变量
//   （`Visit(IdentifierNode)` 的最后一条兜底分支 → `ReportUndefined`）。
// ⚠ Rust 是这五门里**唯一**既能定位（文件:行:列）又带诊断码（[CodeGen_UndefinedVariable]）
//   的一门 —— 代码生成期的错误会先收集、再由 `BuildProgram` 一次性抛出。
// ⚠ 未声明的名字**必须出现在普通表达式里**（下面 `let x = undefined_thing;`）才报得出来：
//   写成 `println!("{}", undefined_thing)` 实测**静默编过** —— 格式实参走的是
//   `FormatHelpers` 另一条路，不做标识符查找（那是本前端的一处漏检，不是本文件的错）。
fn main() {
    let unused_var = 5;
    let total = 0;
    println!("{}", total);
    let x = undefined_thing;   // error: 未声明的变量 [CodeGen_UndefinedVariable]
    println!("{}", x);
}
