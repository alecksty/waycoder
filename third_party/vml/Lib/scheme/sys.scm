;; VML 系统扩展库 — Scheme
;; 需显式 (load "sys.scm")

(define (speaker-beep freq duration)
  (asm "SYSCALL 57"))

(define (set-rtc timestamp)
  (asm "SYSCALL 58")
  0)
