(define g 0)
(define (w1) (set! g 5))
(w1)
(print g)                      ; 顶层变量 + 函数里 set!
(define (add2 a b) (+ a b))
(print (add2 3 4))             ; 多参数
(define (len1 s) (strlen s))
(print (len1 "abcd"))          ; 函数内调库
(define (multi x) (set! g x) (+ x 1))
(print (multi 9))              ; 多形式 body
(define (nlp) (let loop ((i 0) (s 0)) (if (< i 5) (loop (+ i 1) (+ s i)) s)))
(print (nlp))                  ; 命名 let
(define (many) (let ((a 1) (b 2) (c 3) (d 4) (e 5)) (+ a b c d e)))
(print (many))                 ; 多局部量
