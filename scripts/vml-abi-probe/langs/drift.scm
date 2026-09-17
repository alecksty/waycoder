;; 栈漂移探针（Scheme）：循环里反复调外部库函数（ipow）。判据：`DRIFT=126`（2^1+…+2^6）。
;; 反序得 91（i^2 之和）、第 2 参丢得 6（ipow(2,0)=1）。
;;
;; 照 corpus/scheme/skel.scm：循环留在**顶层**（顶层第一个变量绑定落在 R12+ 侧 = 返回地址槽，
;; 塞进函数会被踩）；display 只取第一个实参且不补换行 ⇒ 三次调用 + 自己 (newline)。
;; Scheme 是全部前端里唯一把「裸标签调用」做对的（GenCall 直发 CALL ipow、实参逆序压栈、
;; 调用方清栈），所以这一条理论上不该红 —— 它在这里同时是**对照项**。

(define s 0)

(do ((i 1 (+ i 1)))
    ((> i 6))
  (set! s (+ s (ipow 2 i))))

(display "DRIFT=")
(display s)
(newline)
