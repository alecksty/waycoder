-- 栈漂移探针（Lua）：ipow 属「callee cleans」注释那批 builtin。
-- 判据：`DRIFT=126`（2^1+…+2^6）。⚠ 2026-09-17 实测仍为 0 —— 见 README「已知失败」。
function main()
    local s = 0
    for i = 1, 6 do
        s = s + ipow(2, i)
    end
    print("DRIFT=", s)
end
main()
