; test_error.scm —— 刻意的诊断用例，**必须编译失败**（不参与编译通过性检查）
;
; 期望 2 条 error（都带 文件:行:列 + [诊断码]）：下面 ①② 两处少给参数的调用。
; ③④ 是刻意留着、当前**不会**报的写法，用来钉住行为（见各自注释）。

(define (rect-area w h)
  (* w h))

(define unused-scale 3)          ; ③ 定义未使用：Scheme 无警告出口 ⇒ 不出 warning

(display "rect=")
(display (rect-area 3 4))
(newline)

(display "bad1=")
(display (+ 1))                  ; ① error [CodeGen_InvalidOperand]
(newline)

(display "bad2=")
(display (< 7))                  ; ② error [CodeGen_InvalidOperand]（验证「一次多报」）
(newline)

(display (undefined-helper 1))   ; ④ 未定义函数：被 ①② 挡住，链接期才判 ⇒ 不出现
(newline)
