-- out.lua —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
-- 写法照 corpus/lua/skel.* —— 共享库同时提供 println_str / println_int。
function main()
    print("OUT-STR=abc")
    print("OUT-INT=", 42)
    print("OUT-PUN=hello, world")
end
main()
