;; VML 设备 I/O + 文件 + 键鼠扩展库 — Scheme
;; 需显式 (load "device.scm")

;; 统一设备接口 (SYSCALL 100-104)
(define (dev-open name) (asm "SYSCALL 100") 0)
(define (dev-close handle) (asm "SYSCALL 101") 0)
(define (dev-read handle buf offset count) (asm "SYSCALL 102") 0)
(define (dev-write handle buf offset count) (asm "SYSCALL 103") 0)
(define (dev-control handle command data length) (asm "SYSCALL 104") 0)

;; 键盘 (SYSCALL 83-84)
(define (kb-hit?) (asm "SYSCALL 83") 0)
(define (kb-getch) (asm "SYSCALL 84") #\nul)

;; 鼠标 (SYSCALL 85-88)
(define (mouse-get-x) (asm "SYSCALL 85") 0)
(define (mouse-get-y) (asm "SYSCALL 86") 0)
(define (mouse-left?) (asm "SYSCALL 87") 0)
(define (mouse-right?) (asm "SYSCALL 88") 0)

