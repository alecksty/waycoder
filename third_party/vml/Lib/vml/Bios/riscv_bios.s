# RISC-V BIOS - VML SYSCALL via memory-mapped UART
# Target: SiFive HiFive1 / FE310-G000 (UART0 at 0x10013000)
# Assembler: RISC-V GNU AS
# Uses 16550-compatible UART at 115200 baud

        .global __bios_init
        .global __syscall_1
        .global __syscall_3
        .global __syscall_4
        .global __syscall_5
        .global __syscall_50

        .equ UART_BASE, 0x10013000
        .equ UART_TXFIFO, 0x00
        .equ UART_RXFIFO, 0x04
        .equ UART_TXCTRL, 0x08
        .equ UART_RXCTRL, 0x0C
        .equ UART_IE,    0x10

        .data
        .align 2
__random_state: .word 1

        .text
#----------------------------------------------------------------------
# __bios_init - Enable UART TX/RX
#----------------------------------------------------------------------
__bios_init:
        li t0, UART_BASE
        li t1, 1            # Enable TX
        sw t1, UART_TXCTRL(t0)
        sw t1, UART_RXCTRL(t0)  # Enable RX
        ret

#----------------------------------------------------------------------
# __syscall_1 - OutputString (R0=string address in a0)
#----------------------------------------------------------------------
__syscall_1:
        mv t2, a0
1:      lbu a0, 0(t2)
        beqz a0, 2f
        call __uart_putchar
        addi t2, t2, 1
        j 1b
2:      ret

#----------------------------------------------------------------------
# __syscall_4 - OutputChar (R0=char in a0)
#----------------------------------------------------------------------
__syscall_4:
        call __uart_putchar
        ret

#----------------------------------------------------------------------
# __syscall_5 - InputChar (R0=result in a0)
#----------------------------------------------------------------------
__syscall_5:
        call __uart_getchar
        ret

#----------------------------------------------------------------------
# __syscall_3 - Exit (halt)
#----------------------------------------------------------------------
__syscall_3:
        wfi
        j __syscall_3

#----------------------------------------------------------------------
# __syscall_50 - Random (LFSR)
#----------------------------------------------------------------------
__syscall_50:
        la t0, __random_state
        lw a0, 0(t0)
        srli t1, a0, 7
        xor t1, t1, a0
        srli t1, t1, 6
        xor t1, t1, a0
        srli t2, t1, 1
        xor a0, t1, t2
        sw a0, 0(t0)
        ret

#----------------------------------------------------------------------
# UART functions
#----------------------------------------------------------------------
__uart_putchar:
        li t0, UART_BASE
1:      lw t1, UART_TXFIFO(t0)
        bgez t1, 1b         # TXFIFO full?
        sw a0, UART_TXFIFO(t0)
        ret

__uart_getchar:
        li t0, UART_BASE
1:      lw a0, UART_RXFIFO(t0)
        bltz a0, 1b         # RXFIFO empty?
        ret
