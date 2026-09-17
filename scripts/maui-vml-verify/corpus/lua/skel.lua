-- skel.lua —— Lua 前端「能不能写游戏」最小骨架（期望输出恰好一行 SKEL-SUM=14）
--
-- 取材 Examples/lua/stm32/f103/stm32f103_i2c.lua（`function main() ... end` + 顶层 `main()`）；
-- 前端实现 VMLPrepares/LuaCompiler/。
--
-- 故意踩的已知缺陷（跑不过=产品缺陷，不是语料写错）：
--  ① `a[i]` / `a[i] = v` 是表索引，前端编成 `CALL lua_table_get` / `CALL lua_table_set`，
--     而这两个标签在 Lib/ 下**根本不存在**（CodeGenerator.Statements_B.cs:1034、
--     Statements_A.cs:206）⇒ 链接只报「未解析标签」警告，运行期抛 未找到标签。
--  ② 函数调用只有前 4 个实参进 R0-R3 且**不压栈**（Statements_B.cs:443-471），
--     ui_rect 的 8 个实参到不了包装函数读的 [R12+12]…[R12+40]。
--  ③ `0x…` 字面量在 Parser.cs:662 会 Int32 溢出，颜色写 -65536（= 0xFFFF0000）。
--  ④ print 多实参不加分隔符、末尾自动补换行（Statements_B.cs:476-502）。
--
-- ⚠ 下标基准：Lua 的表是 **1-based**（真 Lua 如此，本前端亦如此 ——
--    `LUA_LANGUAGE_SPEC.md` 明写 `local arr = {10,20,30,40}; print(arr[1]) -- 10（Lua索引从1开始）`，
--    `GenerateTableConstructor` 给位置字段的键也是 `slotIndex + 1`）。
--    本文件原先写的是 `for i = 0, 3` —— **那是语料作者的笔误**（他按 0-based 语言的习惯套过来了；
--    对照 Fortran(1-based) 写的是 `a(1)…a(4)`，Pascal/BASIC/JS/Python(0-based) 写的是 `0..3`）。
--    0-based 取 `a[0..3]` 时 `a[0]` 是新键（nil→0），`s` 只得 10。
--    **修的是语料，不是前端** —— 把前端改成 0-based 会同时推翻真 Lua 语义与它自己的规格文档。

function inc(x)
    return x + 1
end

function main()
    local a = {1, 2, 3, 4}
    local s = 0
    local i = 0
    for i = 1, 4 do
        a[i] = inc(a[i])
        s = s + a[i]
    end
    print("SKEL-SUM=", s)
    ui_rect(10, 10, 50, 50, -65536, 1, 0, 0)
    ui_present()
end

main()
