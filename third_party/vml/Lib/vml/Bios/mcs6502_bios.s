; 6502 BIOS - VML SYSCALL via C64 KERNAL
; Target: Commodore 64 / Apple II
; Assembler: ca65
        .org    $C000

        .export __bios_init
        .export __syscall_1
        .export __syscall_3
        .export __syscall_4
        .export __syscall_5
        .export __syscall_50

; Zero-page pointers (typically unused by BASIC)
STRPTR   = $FA
RNDSTATE = $FC

; C64 KERNAL entry points
CHROUT   = $FFD2
CHRIN    = $FFCF

;----------------------------------------------------------------------
; __bios_init
;----------------------------------------------------------------------
__bios_init:
        rts

;----------------------------------------------------------------------
; __syscall_1  - OutputString (R0=string address in A/X)
;----------------------------------------------------------------------
__syscall_1:
        sta     STRPTR
        stx     STRPTR+1
        ldy     #0
1:      lda     (STRPTR),Y
        beq     2f
        jsr     CHROUT
        iny
        bne     1b
        inc     STRPTR+1
        bne     1b
2:      rts

;----------------------------------------------------------------------
; __syscall_4  - OutputChar (R0=char in A)
;----------------------------------------------------------------------
__syscall_4:
        jsr     CHROUT
        rts

;----------------------------------------------------------------------
; __syscall_5  - InputChar (R0=result in A)
;----------------------------------------------------------------------
__syscall_5:
        jsr     CHRIN
        rts

;----------------------------------------------------------------------
; __syscall_3  - Exit (halt)
;----------------------------------------------------------------------
__syscall_3:
        sei
1:      brk
        jmp     1b

;----------------------------------------------------------------------
; __syscall_50 - Random (LFSR)
;----------------------------------------------------------------------
__syscall_50:
        lda     RNDSTATE
        asl     a
        bcc     1f
        eor     #$1D
1:      sta     RNDSTATE
        rts
