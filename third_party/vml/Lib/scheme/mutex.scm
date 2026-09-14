;; VML 互斥锁扩展库 — Scheme (OS 模式)
(define (mutex-create) (asm "SYSCALL 310") 0)
(define (mutex-lock id) (asm "SYSCALL 311") 0)
(define (mutex-unlock id) (asm "SYSCALL 312") 0)
