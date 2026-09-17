-- abi.lua —— 实参顺序探针（Lua）。判据：`ABI=8`（反序得 9）。
function main()
    local r = ipow(2, 3)
    print("ABI=", r)
end
main()
