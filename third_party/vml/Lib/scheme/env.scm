;; VML 环境变量扩展库 — Scheme (OS 模式)
;; 需显式 (load "env.scm")

(define (get-env name) (asm "SYSCALL 360") "")
(define (set-env name value) (asm "SYSCALL 361") 0)
(define (get-args buffer) (asm "SYSCALL 362") 0)
