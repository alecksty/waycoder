;; VML VGA 图形扩展库 (QBASIC风格) — Scheme
;; 需显式 (load "gfx.scm")

;; Basic VGA
(define (vga-clear) (asm "SYSCALL 80"))
(define (vga-putchar x y c color) (asm "SYSCALL 81"))
(define (vga-puts x y s color) (asm "SYSCALL 82"))

;; Screen mode & info
(define (gfx-screen mode) 0)
(define (gfx-width) 0)
(define (gfx-height) 0)
(define (gfx-depth) 0)

;; Palette
(define (gfx-palette idx r g b) #f)
(define (gfx-palette-get idx) 0)

;; Pixel ops
(define (gfx-pset x y color) #f)
(define (gfx-point x y) 0)
(define (gfx-cls) #f)
(define (gfx-cls-color color) #f)

;; Drawing
(define (gfx-line x1 y1 x2 y2 color) #f)
(define (gfx-rect x1 y1 x2 y2 color) #f)
(define (gfx-rect-fill x1 y1 x2 y2 color) #f)
(define (gfx-circle cx cy r color) #f)
(define (gfx-circle-fill cx cy r color) #f)
(define (gfx-arc cx cy r sa ea color) #f)
(define (gfx-sector cx cy r sa ea color) #f)

;; Text
(define (gfx-print x y text color) #f)
(define (gfx-print-scale x y text color scale) #f)

;; Fill
(define (gfx-flood-fill x y fc bc) #f)

;; Advanced (SYSCALL)
(define (gfx-screenshot) (asm "SYSCALL 200") 0)
(define (gfx-put-image x y w h data) (asm "SYSCALL 201") 0)
(define (gfx-get-image x y w h buf) (asm "SYSCALL 202") 0)
(define (gfx-viewport x1 y1 x2 y2) (asm "SYSCALL 203") 0)
