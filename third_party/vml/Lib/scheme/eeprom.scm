;; VML EEPROM 扩展库 — Scheme
;; 需显式 (load "eeprom.scm")

(define (eeprom-read offset buffer count)
  (asm "SYSCALL 106")
  0)

(define (eeprom-write offset data count)
  (asm "SYSCALL 107")
  0)
