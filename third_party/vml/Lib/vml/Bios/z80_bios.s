; Z80 BIOS - VML SYSCALL via ZX Spectrum ROM
; Target: ZX Spectrum 48K / MSX
; Assembler: sjasm / z80asm

        org     $8000

        public  __bios_init
        public  __syscall_1
        public  __syscall_3
        public  __syscall_4
        public  __syscall_5
        public  __syscall_50

; ZX Spectrum ROM entry points
CHAN_OPEN equ $1601      ; Channel selection
PRINT_CHR equ $0010      ; RST 10 - print A
KEY_INPUT equ $15DE      ; Keyboard repeat handler

;----------------------------------------------------------------------
; __bios_init
;----------------------------------------------------------------------
__bios_init:
        ; Open channel 2 (screen) for output
        ld      a, 2
        call    CHAN_OPEN
        ret

;----------------------------------------------------------------------
; __syscall_1  - OutputString (R0=string address in HL)
;----------------------------------------------------------------------
__syscall_1:
        ex      de, hl          ; string addr in DE
1:      ld      a, (de)
        or      a
        ret     z
        rst     $10
        inc     de
        jr      1b

;----------------------------------------------------------------------
; __syscall_4  - OutputChar (R0=char in A)
;----------------------------------------------------------------------
__syscall_4:
        rst     $10
        ret

;----------------------------------------------------------------------
; __syscall_5  - InputChar (R0=result in A)
;----------------------------------------------------------------------
__syscall_5:
        call    KEY_INPUT
        or      a
        jr      z, __syscall_5
        ret

;----------------------------------------------------------------------
; __syscall_3  - Exit (halt)
;----------------------------------------------------------------------
__syscall_3:
        di
1:      halt
        jr      1b

;----------------------------------------------------------------------
; __syscall_50 - Random (LFSR: HL = (HL << 1) ^ (C ? 0x1D : 0))
;----------------------------------------------------------------------
__syscall_50:
        push    hl
        ld      hl, (__random_seed)
        add     hl, hl
        jr      nc, 1f
        ld      a, $1D
        xor     h
        ld      h, a
1:      ld      (__random_seed), hl
        ld      a, l
        pop     hl
        ret

;----------------------------------------------------------------------
; Data
;----------------------------------------------------------------------
__random_seed:
        dw      1
