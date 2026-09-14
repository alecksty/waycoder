; PIC24HJ256GP610 BIOS - VML SYSCALL via UART1 (115200 baud)
        .linked  "p24Fxxxx.inc"
        .text

        .global __bios_init
        .global __syscall_1
        .global __syscall_3
        .global __syscall_4
        .global __syscall_5
        .global __syscall_50

;----------------------------------------------------------------------
; __bios_init  - UART1 @ 115200 baud (40 MIPS)
;----------------------------------------------------------------------
__bios_init:
        ; Configure UART1
        mov     #0x0000, W0
        mov     W0, U1MODE              ; UARTEN=0 during setup

        ; BRG = (FCY/(16*Baud)) - 1 = 40000000/(16*115200) - 1 = 20.7 -> 21
        mov     #21, W0
        mov     W0, U1BRG

        ; 8N1, enable UART
        mov     #0x8008, W0             ; UARTEN=15, UTXEN=3
        mov     W0, U1MODE

        ; Clear flags
        mov     U1STA, W0
        ret

;----------------------------------------------------------------------
; __syscall_1  - OutputString (R0=string address in W0)
;----------------------------------------------------------------------
__syscall_1:
        push    W4
        mov     W0, W4
1:      mov.b   [W4++], W0
        bra     Z, 2f
        call    __uart_putchar
        bra     1b
2:      pop     W4
        ret

;----------------------------------------------------------------------
; __syscall_4  - OutputChar (R0=char in W0)
;----------------------------------------------------------------------
__syscall_4:
        call    __uart_putchar
        ret

;----------------------------------------------------------------------
; __syscall_5  - InputChar (R0=result in W0)
;----------------------------------------------------------------------
__syscall_5:
        call    __uart_getchar
        ret

;----------------------------------------------------------------------
; __syscall_3  - Exit (halt)
;----------------------------------------------------------------------
__syscall_3:
        disi    #0x3FFF
1:      bra     1b

;----------------------------------------------------------------------
; __syscall_50 - Random (LFSR)
;----------------------------------------------------------------------
__syscall_50:
        push    W4
        mov     __random_state, W4
        sl      W4, #1, W4
        bra     NC, 1f
        xor     W4, #0x001D, W4
1:      mov     W4, __random_state
        mov     W4, W0
        pop     W4
        ret

;----------------------------------------------------------------------
; UART1 polling I/O
;----------------------------------------------------------------------
__uart_putchar:
        push    W4
1:      mov     U1STA, W4
        and     W4, #0x0200, W4         ; UTXBF?
        bra     NZ, 1b
        mov     W0, U1TXREG
        pop     W4
        ret

__uart_getchar:
        push    W4
1:      mov     U1STA, W4
        and     W4, #0x0001, W4         ; URXDA?
        bra     Z, 1b
        mov     U1RXREG, W0
        pop     W4
        ret

        .data
        .align 2
__random_state: .word 1
