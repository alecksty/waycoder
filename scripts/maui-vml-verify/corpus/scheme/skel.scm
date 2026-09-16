;; skel.scm —— Scheme 前端「能不能写游戏」最小骨架（期望输出恰好一行 SKEL-SUM=14）
;;
;; 取材 Examples/scheme/math/main.scm（`(define (f x) …)`）与
;;      Examples/scheme/sicp/1.2.5-greatest_common_divisors_gcd.scm（顶层调用求值）；
;; 前端实现 VMLPrepares/SchemeCompiler/。
;;
;; 故意踩的已知缺陷（跑不过=产品缺陷，不是语料写错）：
;;  ① `(vector 1 2 3 4)` 的**元素写入方向反了** —— CodeGenerator.Expressions.cs:503 发的是
;;     `MOVE R1,[R1+…]`（读），元素值全丢；`(vector-set! …)` 同病（:587 也是读）
;;     ⇒ `a[i] = inc(a[i])` 不可能成立。这是标准 Scheme，失败即产品缺陷。
;;  ② display/print 只取第一个实参且**不补换行**（CodeGenerator.cs:83-88、
;;     Expressions.cs:593-596）⇒ 一行输出拆三次调用，换行自己补。
;;  ③ Scheme 是全部前端里唯一把「裸标签调用」做对的：GenCall 直发 `CALL ui_rect`，
;;     实参逆序压栈、调用方清 32 字节，正好合 vmlui 的 [R12+12] 约定（Expressions.cs:50）。
;;  ④ 词法器只认十进制（Lexer.cs:35），`0x…` 会被切成 0 + 符号 ⇒ 颜色写 -65536。
;;  ⑤ 顶层的第一个变量绑定落在 R12+ 侧（返回地址槽），所以循环留在顶层、别塞进函数。

(define (inc x) (+ x 1))

(define a (vector 1 2 3 4))
(define s 0)

(do ((i 0 (+ i 1)))
    ((= i 4))
  (vector-set! a i (inc (vector-ref a i)))
  (set! s (+ s (vector-ref a i))))

(display "SKEL-SUM=")
(display s)
(newline)
(ui_rect 10 10 50 50 -65536 1 0 0)
(ui_present)
