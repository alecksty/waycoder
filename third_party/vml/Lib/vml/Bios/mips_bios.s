# MIPS BIOS - VML SYSCALL via UART
# Target: PIC32MX170 (UART2 at 0xBF806000)
# Assembler: MIPS GNU AS / xc32-gcc
# Uses UART at 115200 baud

        .global __bios_init
        .global __syscall_1
        .global __syscall_3
        .global __syscall_4
        .global __syscall_5
        .global __syscall_50

        .equ UART_BASE,    0xBF806000
        .equ U_STA,        0x1C
        .equ U_TXR,        0x10
        .equ U_RXR,        0x08
        .equ U_BRG,        0x24
        .equ U_MODE,       0x00
        .equ TX_RDY,       0x0200
        .equ RX_RDY,       0x0001
        .equ PB_CLK,       80000000

        .data
        .align 2
__random_state: .word 1

        .text
        .set noreorder
#----------------------------------------------------------------------
# __bios_init - Init UART2 at 115200 baud
#----------------------------------------------------------------------
__bios_init:
        la t0, UART_BASE
        # Reset UART
        sw zero, U_MODE(t0)
        # Set baud: PB_CLK/(16*115200) = 80000000/1843200 = 43
        li t1, 43
        sw t1, U_BRG(t0)
        # Enable UART: ON=1
        li t1, 0x8000
        sw t1, U_MODE(t0)
        # Enable TX and RX
        li t1, 0x0100       # UTXEN=8
        sw t1, U_STA(t0)
        jr ra
        nop

#----------------------------------------------------------------------
# __syscall_1 - OutputString (R0=string address in a0)
#----------------------------------------------------------------------
__syscall_1:
        mv t2, a0
1:      lbu a0, 0(t2)
        beqz a0, 2f
        nop
        jal __uart_putchar
        nop
        addi t2, t2, 1
        j 1b
        nop
2:      jr ra
        nop

#----------------------------------------------------------------------
# __syscall_4 - OutputChar (R0=char in a0)
#----------------------------------------------------------------------
__syscall_4:
        j __uart_putchar
        nop

#----------------------------------------------------------------------
# __syscall_5 - InputChar (R0=result in a0)
#----------------------------------------------------------------------
__syscall_5:
        j __uart_getchar
        nop

#----------------------------------------------------------------------
# __syscall_3 - Exit (halt)
#----------------------------------------------------------------------
__syscall_3:
        wait
        j __syscall_3
        nop

#----------------------------------------------------------------------
# __syscall_50 - Random (LFSR)
#----------------------------------------------------------------------
__syscall_50:
        la t0, __random_state
        lw t1, 0(t0)
        srl t2, t1, 7
        xor t2, t2, t1
        srl t2, t2, 6
        xor t2, t2, t1
        srl a0, t2, 1
        xor a0, a0, t2
        sw a0, 0(t0)
        jr ra
        nop

#----------------------------------------------------------------------
# UART functions
#----------------------------------------------------------------------
__uart_putchar:
        la t0, UART_BASE
1:      lw t1, U_STA(t0)
        andi t1, t1, TX_RDY
        beqz t1, 1b
        nop
        sw a0, U_TXR(t0)
        jr ra
        nop

__uart_getchar:
        la t0, UART_BASE
1:      lw t1, U_STA(t0)
        andi t1, t1, RX_RDY
        beqz t1, 1b
        nop
        lw a0, U_RXR(t0)
        jr ra
        nop
