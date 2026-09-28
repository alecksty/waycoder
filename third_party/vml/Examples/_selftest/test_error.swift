// test_error.swift —— 刻意的诊断用例，**必须编译失败**（不参与编译通过性检查）
// test_error.swift -- a deliberate diagnostic case, **must fail to compile** (not part of the compile-cleanliness check)
//
// 期望 2 条 error（都带 行:列 + [诊断码]）：①② 两处引用了未声明的变量。
// Expected: 2 errors (both with line:col + [diag-code]): cases (1)(2) reference an undeclared variable.
// ③ 是刻意留着、当前**不会**报的写法：Swift 前端没有「未使用变量」的警告出口。
// (3) is a deliberately kept form that currently **does not** report: the Swift frontend has no warning outlet for "unused variable".

func main() {
    let unusedScale = 3              // ③ 定义了但从未使用 ⇒ 不出 warning
    // (3) defined but never used => no warning is produced
    let width = 3
    let height = 4
    let area = width * height
    print("area=", area)

    let bad = area + missingWidth    // ① error [CodeGen_UndefinedVariable]
    let worse = bad * missingHeight  // ② error（验证「一次多报」）
    // (2) error (verifies "several reported at once")
    print("bad=", bad, worse)
}
