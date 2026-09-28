# lang_probe.py —— 非 C 语言侧的同一条判据：ui_get_language() 经 Lib/python/shared.py
# 的绑定（asm("CALL ui_get_language")）调到宿主 HOST_LANG(#568)。
#
# 期望（本机中文系统）：LANG=0；`LANG=en_US.UTF-8 DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` 下 LANG=1。
# 注释前缀必须是 `#`（写成 `//` 会编译期直接抛）。
print_str("LANG=")
println_int(ui_get_language())
