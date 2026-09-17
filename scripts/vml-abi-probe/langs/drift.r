# 栈漂移探针（R）：print/cat 那条路径「压一个实参」，而被调方已改成裸 `ret` ——
# 压了必须由调用方清（2026-09-17 已补 `ADD R13 #4`）。
#
# 判据：`DRIFT=7`（循环 c(3,4) 两轮都要跑到）。3 次 print 先制造 12 字节漂移，
# 若漂移爬进循环游标，循环就会只跑一轮 ⇒ 得 3。
print(1)
print(2)
print(3)
s <- 0
for (j in c(3, 4)) {
    s <- s + j
}
cat("\nDRIFT=")
print(s)
cat("\n")
