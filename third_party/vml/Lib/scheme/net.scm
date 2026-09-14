;; VML 网络 Socket 扩展库 — Scheme
;; OS 模式专用，需显式 (load "net.scm")

(define (net-create domain type)
  (asm "SYSCALL 330")
  0)

(define (net-bind fd port)
  (asm "SYSCALL 331")
  0)

(define (net-listen fd backlog)
  (asm "SYSCALL 332")
  0)

(define (net-accept fd)
  (asm "SYSCALL 333")
  0)

(define (net-connect host port)
  (asm "SYSCALL 334")
  0)

(define (net-send fd data len)
  (asm "SYSCALL 335")
  0)

(define (net-recv fd buf max-len)
  (asm "SYSCALL 336")
  0)

(define (net-close fd)
  (asm "SYSCALL 337")
  0)

(define (dns-resolve hostname)
  (asm "SYSCALL 338")
  0)
