;; VML 文件系统扩展库 — Scheme (OS 模式)
;; 需显式 (load "fs.scm")

(define (fs-mkdir path) (asm "SYSCALL 340") 0)
(define (fs-remove path) (asm "SYSCALL 341") 0)
(define (fs-rename old-path new-path) (asm "SYSCALL 342") 0)
(define (fs-readdir path buffer) (asm "SYSCALL 343") 0)
(define (fs-stat path info) (asm "SYSCALL 344") 0)
