' ★ SUB 里**写**模块级变量（t4/t7 只覆盖了读 —— 这是个缺口）
DIM counter AS INTEGER
counter = 0
SUB bump()
    counter = counter + 5
END SUB
bump()
bump()
PRINT "counter="
PRINT counter
PRINT "\n"
