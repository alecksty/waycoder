# nat.rb —— 测**这门语言自己的** puts（与 out.rb 走的共享库 println_str/println_int 是两套实现）
#
# 为什么值要放进**变量**再打印：`puts("字面量")` 一直是对的，而 `puts(变量)` 曾经
# 走整数那条实现，把**字符串的地址**打了出来（实测 1024）。判据只写字面量就照不出来。
#
# 为什么带一个 `&&`：Ruby 词法器此前**完全没有** `&` / `|` 的 case，一写就
# `Unexpected char: &` ⇒ 多个条件只能写成嵌套 if。多带一行逻辑运算，这条就顺带钉住了。
s = "OUT-STR=abc"
p = "OUT-PUN=hello, world"
n = 1
m = 0
if n == 1 && m == 0
  puts(s)
end
print("OUT-INT=")
puts(42)
puts(p)
