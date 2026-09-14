! SPARC BIOS - VML SYSCALL via APB UART
! Target: LEON3/LEON4 (GRLIB APB UART at 0x80000100)
! Assembler: SPARC GNU AS
! Uses APB UART at 38400 baud (typical LEON3)

        .global __bios_init
        .global __syscall_1
        .global __syscall_3
        .global __syscall_4
        .global __syscall_5
        .global __syscall_50

        .equ UART_BASE, 0x80000100
        .equ UART_DATA, 0x00
        .equ UART_STAT, 0x04
        .equ UART_CTRL, 0x08
        .equ UART_SCAL, 0x0C
        .equ TX_READY,  0x02
        .equ RX_READY,  0x01

        .section .data
        .align 4
__random_state: .word 1

        .section .text
        .align 4
!----------------------------------------------------------------------
! __bios_init - Init APB UART at 38400 baud (50MHz/(16*38400)=81)
!----------------------------------------------------------------------
__bios_init:
        set UART_BASE, %l0
        ! Set baud scaler: 81 for 38400 @ 50MHz
        mov 81, %l1
        st %l1, [%l0 + UART_SCAL]
        ! Enable TX and RX
        mov 3, %l1
        st %l1, [%l0 + UART_CTRL]
        jmpl %o7 + 8, %g0
        nop

!----------------------------------------------------------------------
! __syscall_1 - OutputString (R0=string address in %o0)
!----------------------------------------------------------------------
__syscall_1:
        mov %o0, %l2
1:      ldub [%l2], %o0
        tst %o0
        be 2f
        nop
        call __uart_putchar
        nop
        add %l2, 1, %l2
        ba 1b
        nop
2:      jmpl %o7 + 8, %g0
        nop

!----------------------------------------------------------------------
! __syscall_4 - OutputChar (R0=char in %o0)
!----------------------------------------------------------------------
__syscall_4:
        ba __uart_putchar
        nop

!----------------------------------------------------------------------
! __syscall_5 - InputChar (R0=result in %o0)
!----------------------------------------------------------------------
__syscall_5:
        ba __uart_getchar
        nop

!----------------------------------------------------------------------
! __syscall_3 - Exit (halt)
!----------------------------------------------------------------------
__syscall_3:
        mov 0x0F, %l0      ! Disable interrupts
        ba __syscall_3
        nop

!----------------------------------------------------------------------
! __syscall_50 - Random (LFSR)
!----------------------------------------------------------------------
__syscall_50:
        set __random_state, %l0
        ld [%l0], %l1
        srl %l1, 7, %l2
        xor %l2, %l1, %l2
        srl %l2, 6, %l2
        xor %l2, %l1, %l2
        srl %l2, 1, %o0
        xor %o0, %l2, %o0
        st %o0, [%l0]
        jmpl %o7 + 8, %g0
        nop

!----------------------------------------------------------------------
! UART functions
!----------------------------------------------------------------------
__uart_putchar:
        set UART_BASE, %l0
1:      ld [%l0 + UART_STAT], %l1
        and %l1, TX_READY, %l1
        cmp %l1, 0
        be 1b
        nop
        st %o0, [%l0 + UART_DATA]
        jmpl %o7 + 8, %g0
        nop

__uart_getchar:
        set UART_BASE, %l0
1:      ld [%l0 + UART_STAT], %l1
        and %l1, RX_READY, %l1
        cmp %l1, 0
        be 1b
        nop
        ld [%l0 + UART_DATA], %o0
        jmpl %o7 + 8, %g0
        nop
