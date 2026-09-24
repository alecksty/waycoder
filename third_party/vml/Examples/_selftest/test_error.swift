// test_error.swift —— 刻意的诊断用例，**必须编译失败**（不参与编译通过性检查）
//
// 期望 2 条 error（都带 行:列 + [诊断码]）：①② 两处引用了未声明的变量。
// ③ 是刻意留着、当前**不会**报的写法：Swift 前端没有「未使用变量」的警告出口。

func main() {
    let unusedScale = 3              // ③ 定义了但从未使用 ⇒ 不出 warning
    let width = 3
    let height = 4
    let area = width * height
    print("area=", area)

    let bad = area + missingWidth    // ① error [CodeGen_UndefinedVariable]
    let worse = bad * missingHeight  // ② error（验证「一次多报」）
    print("bad=", bad, worse)
}
