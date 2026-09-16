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

function inc(x)
    return x + 1
end

function main()
    local a = {1, 2, 3, 4}
    local s = 0
    local i = 0
    for i = 0, 3 do
        a[i] = inc(a[i])
        s = s + a[i]
    end
    print("SKEL-SUM=", s)
    ui_rect(10, 10, 50, 50, -65536, 1, 0, 0)
    ui_present()
end

main()
