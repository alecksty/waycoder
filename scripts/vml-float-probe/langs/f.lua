-- f.lua
-- ⚠ 两点与判据有关：
--   ① 判据是**逐行比**（标签自己占一行）⇒ 标签单独 print 一条；
--      `print("x=", v)` 会用制表符接在同一行。
--   ② 写成**顶层语句**：`function main() … end` 只是定义，谁也没调用它
--      （此前这份探针正是因此**零输出**）。
print("F-MUL=")
print(3.14 * 2.0 * 100)
print("F-NEG=")
print(-0.5 * 100)
print("F-HEX=")
print(0x10 * 100)
