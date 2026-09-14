; 8051 BIOS - VML SYSCALL implementations via UART
; Uses UART at 9600 baud, mode 1

        .org 0x0000
        ljmp __bios_init

; UART constants
UART_SFR EQU 0x87  ; PCON
TMOD_VAL EQU 0x20  ; Timer 1, mode 2 (8-bit auto-reload)
TH1_VAL  EQU 0xFD  ; 9600 baud @ 11.0592MHz

__bios_init:
        ; Set up UART
        mov TMOD, #TMOD_VAL
        mov TH1, #TH1_VAL
        mov TL1, #TH1_VAL
        setb TR1
        mov SCON, #0x50  ; Mode 1, REN enabled
        ret

; SYSCALL handlers
__syscall_1:  ; OutputString
        push 0
        push 1
        mov r0, dpl
        mov r1, dph
1:      mov dpl, r0
        mov dph, r1
        clr a
        movc a, @a+dptr
        jz 2f
        lcall __uart_putchar
        inc dptr
        mov r0, dpl
        mov r1, dph
        sjmp 1b
2:      pop 1
        pop 0
        ret

__syscall_4:  ; OutputChar
        push 0
        mov r0, dpl
        lcall __uart_putchar
        pop 0
        ret

__syscall_3:  ; Exit
        clr EA
1:      sjmp 1b

__syscall_50: ; Random
        push 0
        push 1
        mov r0, __random_state
        mov r1, __random_state+1
        mov a, r0
        rl a
        mov r0, a
        mov a, r1
        rlc a
        mov r1, a
        mov a, r0
        xrl a, #0x1D
        jnb acc.0, 1f
        xrl a, #0x1D
1:      mov __random_state, a
        mov __random_state+1, r1
        mov dpl, a
        pop 1
        pop 0
        ret

__uart_putchar:
        jnb TI, $
        clr TI
        mov SBUF, a
        ret

__random_state: .byte 0x01, 0x00
