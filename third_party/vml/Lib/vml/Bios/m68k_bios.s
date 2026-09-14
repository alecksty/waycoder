; 68000 BIOS - VML SYSCALL via MC68681 DUART
; Target: Motorola 68000 / MC68681 DUART at 0x100000
; Assembler: vasm / GNU AS
; Uses DUART at 9600 baud

        .cpu 68000

        .global __bios_init
        .global __syscall_1
        .global __syscall_3
        .global __syscall_4
        .global __syscall_5
        .global __syscall_50

DUART   equ $100000
MR1A    equ 0          ; Mode Register A (write)
MR2A    equ 0          ; Mode Register A (write, after MR1)
SRA     equ 1          ; Status Register A (read)
CSRA    equ 1          ; Clock Select A (write)
CRA     equ 2          ; Command Register A (write)
TBA     equ 3          ; Transmit Buffer A (write)
RBA     equ 3          ; Receive Buffer A (read)
ACR     equ 4          ; Auxiliary Control
IMR     equ 5          ; Interrupt Mask
ISR     equ 5          ; Interrupt Status (read)

        .data
        .even
__random_state: dc.w 1

        .text
;----------------------------------------------------------------------
; __bios_init - Init DUART at 9600 baud, 8N1, 8MHz clock
;----------------------------------------------------------------------
__bios_init:
        move.l #DUART, a0
        ; Reset receiver and transmitter
        move.b #$10, CRA(a0)   ; Reset MR pointer
        move.b #$13, MR1A(a0)  ; No parity, 8 bits, 1 stop, RX RTS
        move.b #$07, MR2A(a0)  ; TX RTS, 1 stop, normal
        move.b #$BB, CSRA(a0)  ; 8MHz/(16*9600)=52 -> timer=BB?
        move.b #$05, CRA(a0)   ; Enable TX, RX
        rts

;----------------------------------------------------------------------
; __syscall_1 - OutputString (R0=string address in d0)
;----------------------------------------------------------------------
__syscall_1:
        move.l d0, a1
1:      move.b (a1)+, d0
        tst.b d0
        beq 2f
        bsr __uart_putchar
        bra 1b
2:      rts

;----------------------------------------------------------------------
; __syscall_4 - OutputChar (R0=char in d0)
;----------------------------------------------------------------------
__syscall_4:
        bsr __uart_putchar
        rts

;----------------------------------------------------------------------
; __syscall_5 - InputChar (R0=result in d0)
;----------------------------------------------------------------------
__syscall_5:
        bsr __uart_getchar
        rts

;----------------------------------------------------------------------
; __syscall_3 - Exit (halt)
;----------------------------------------------------------------------
__syscall_3:
        move.w #$2700, sr    ; Disable interrupts
1:      stop #$2700
        bra 1b

;----------------------------------------------------------------------
; __syscall_50 - Random (LFSR)
;----------------------------------------------------------------------
__syscall_50:
        move.w __random_state, d0
        lsl.w #1, d0
        bcc 1f
        eor.w #$1D, d0
1:      move.w d0, __random_state
        rts

;----------------------------------------------------------------------
; UART functions
;----------------------------------------------------------------------
__uart_putchar:
        move.l #DUART, a0
1:      btst #2, SRA(a0)    ; TXRDY bit?
        beq 1b
        move.b d0, TBA(a0)
        rts

__uart_getchar:
        move.l #DUART, a0
1:      btst #0, SRA(a0)    ; RXRDY bit?
        beq 1b
        move.b RBA(a0), d0
        rts
