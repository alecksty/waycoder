;; VML 文件操作扩展库 — Scheme
(define (fopen name mode) (asm "SYSCALL 110") 0)
(define (fclose handle) (asm "SYSCALL 111") 0)
(define (fread handle buf count) (asm "SYSCALL 112") 0)
(define (fwrite handle buf count) (asm "SYSCALL 113") 0)
(define (fseek handle offset) (asm "SYSCALL 114") 0)
(define (ftell handle) (asm "SYSCALL 114") 0)
