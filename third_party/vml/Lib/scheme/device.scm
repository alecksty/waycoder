⚠ SYSCALL 审计（2026-09-27）：本文件用到的号（83–88，键盘/鼠标查询）在本 VM 里**从未实现** ——
全仓只有本文件在用它们，宿主与 VM 都不认 ⇒ **调了什么都不会发生**（静默 no-op）。
要真用得走库函数那条路（如文件操作用 `Lib/shared/file.vml` 的 file_*），别再照抄这里的写法。
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

