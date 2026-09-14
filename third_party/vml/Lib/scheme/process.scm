;; VML 进程扩展库 — Scheme (OS 模式)
(define (exec path) (asm "SYSCALL 320") 0)
(define (get-pid) (asm "SYSCALL 322") 0)
