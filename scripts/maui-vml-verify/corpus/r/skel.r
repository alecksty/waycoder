# skel.r —— R 前端「能不能写游戏」最小骨架（期望输出恰好一行 SKEL-SUM=14）
#
# 取材 Examples/r/parserexpf_demo.r（`cat(...)`）与 Examples/r/file_io.r；
# 前端实现 VMLPrepares/RCompiler/。
#
# 刻意写法 + 已知缺陷（跑不过=产品缺陷，不是语料写错）：
#  ① 循环**不写** `for (i in 1:4)`：`:` 被解析成 CallNode(":")，GenerateCall 无此分支
#     ⇒ 编出 `CALL func_:`（带冒号的标签，永不解析）—— 这就是「R 循环挂住」的真身。
#     改成 `for (i in c(1,2,3,4))`，循环变量拿到的是**元素值**。
#  ② R 向量是 1-based（GenerateIndex 用 (i-1)*4）⇒ 下标写 1..4。
#  ③ `cat` / `print` 只打印**第一个实参**（CodeGenerator.Expressions.cs:317-323）
#     ⇒ 一行输出必须拆成三次调用，换行也要自己补 "\n"。
#  ④ ui_rect 会被编成 `CALL func_ui_rect`，R 没有 extern/asm，只有靠链接器剥
#     `func_` 前缀解析到 lib_vmlui_ui_rect（VMLAssembler/LibraryLinker.cs:162-182）。
#  ⑤ 词法器不认 0x ⇒ 颜色写 -65536（= 0xFFFF0000）。

inc <- function(x) { return(x + 1) }

a <- c(1, 2, 3, 4)
s <- 0
for (i in c(1, 2, 3, 4)) {
    a[i] <- inc(a[i])
    s <- s + a[i]
}
cat("SKEL-SUM=")
print(s)
cat("\n")
ui_rect(10, 10, 50, 50, -65536, 1, 0, 0)
ui_present()
