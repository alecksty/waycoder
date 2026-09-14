;; VML 调试扩展库 — Scheme
;; 需显式 (load "debug.scm")

(define (debug-print str)
  (asm "SYSCALL 70"))

(define (debug-print-int n)
  (asm "SYSCALL 71"))

(define (assert condition message)
  (asm "SYSCALL 72"))
