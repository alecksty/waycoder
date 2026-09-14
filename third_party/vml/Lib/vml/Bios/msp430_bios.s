; MSP430G2553 BIOS - VML SYSCALL via UART (9600 baud)
        .cdecls C,LIST,"msp430.h"
        .text

        .global __bios_init
        .global __syscall_1
        .global __syscall_3
        .global __syscall_4
        .global __syscall_5
        .global __syscall_50

;----------------------------------------------------------------------
; __bios_init
;----------------------------------------------------------------------
__bios_init:
        ; Configure eUSCI_A0 UART @ 9600 baud, 1MHz DCO
        mov.b   #0x80, &UCA0CTL1        ; SWRST=1
        mov.b   #0x68, &UCA0BR0         ; 1000000/9600 = 104 (0x68)
        mov.b   #0x00, &UCA0BR1
        mov.b   #0x02, &UCA0MCTL        ; UCBRF=1
        bic.b   #0x01, &IFG2            ; Clear UCA0TXIFG
        bic.b   #0x02, &IFG2            ; Clear UCA0RXIFG
        bic.b   #0x80, &UCA0CTL1        ; SWRST=0
        ret

;----------------------------------------------------------------------
; __syscall_1  - OutputString (R0=string address)
;----------------------------------------------------------------------
__syscall_1:
        push.w  R4
        mov.w   R0, R4
1:      mov.b   @R4+, R0
        tst.b   R0
        jeq     2f
        call    #__uart_putchar
        jmp     1b
2:      pop.w   R4
        ret

;----------------------------------------------------------------------
; __syscall_4  - OutputChar (R0=char)
;----------------------------------------------------------------------
__syscall_4:
        call    #__uart_putchar
        ret

;----------------------------------------------------------------------
; __syscall_5  - InputChar (R0=result)
;----------------------------------------------------------------------
__syscall_5:
        call    #__uart_getchar
        ret

;----------------------------------------------------------------------
; __syscall_3  - Exit (halt)
;----------------------------------------------------------------------
__syscall_3:
        dint
1:      nop
        jmp     1b

;----------------------------------------------------------------------
; __syscall_50 - Random (LFSR: x[n+1] = (x[n]<<1) ^ (C ? 0x1D : 0))
;----------------------------------------------------------------------
__syscall_50:
        push.w  R4
        mov.w   &__random_state, R4
        rla.w   R4
        jnc     1f
        xor.w   #0x001D, R4
1:      mov.w   R4, &__random_state
        mov.w   R4, R0
        pop.w   R4
        ret

;----------------------------------------------------------------------
; UART polling I/O
;----------------------------------------------------------------------
__uart_putchar:
        bit.b   #0x01, &IFG2            ; UCA0TXIFG?
        jz      __uart_putchar
        mov.b   R0, &UCA0TXBUF
        ret

__uart_getchar:
        bit.b   #0x02, &IFG2            ; UCA0RXIFG?
        jz      __uart_getchar
        mov.b   &UCA0RXBUF, R0
        ret

        .data
        .align 2
__random_state: .word 1
