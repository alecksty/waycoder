;; VML FFI 动态库调用扩展库 — Scheme
;; OS 模式专用，需显式 (load "ffi.scm")

(define (dl-open path)
  (asm "SYSCALL 370")
  0)

(define (dl-sym handle name)
  (asm "SYSCALL 371")
  0)

(define (dl-close handle)
  (asm "SYSCALL 372")
  0)

(define (native-call func-id args count flags)
  (asm "SYSCALL 373")
  0)

(define (native-call-f func-id fargs count flags)
  (asm "SYSCALL 375")
  0.0)

(define (get-platform)
  (asm "SYSCALL 374")
  0)
