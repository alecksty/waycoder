# PowerPC BIOS - VML SYSCALL via NS16550 UART
# Target: PowerPC 405 / Xilinx ML403 (UART at 0xEF600300)
# Assembler: PowerPC GNU AS
# Uses 16550 UART at 115200 baud

        .global __bios_init
        .global __syscall_1
        .global __syscall_3
        .global __syscall_4
        .global __syscall_5
        .global __syscall_50

        .equ UART_BASE, 0xEF600300
        .equ UART_THR,  0x00    # Transmit Holding
        .equ UART_RBR,  0x00    # Receive Buffer
        .equ UART_LSR,  0x14    # Line Status
        .equ UART_LCR,  0x0C    # Line Control
        .equ UART_DLL,  0x00    # Divisor Latch Low
        .equ UART_DLM,  0x04    # Divisor Latch High
        .equ TX_EMPTY,  0x20    # LSR bit 5
        .equ RX_READY,  0x01    # LSR bit 0

        .data
        .align 2
__random_state: .word 1

        .text
#----------------------------------------------------------------------
# __bios_init - Init UART at 115200 baud (100MHz/(16*115200)=54)
#----------------------------------------------------------------------
__bios_init:
        lis r4, UART_BASE@h
        ori r4, r4, UART_BASE@l
        # Set DLAB=1 to access divisor latches
        li r5, 0x83
        stb r5, UART_LCR(r4)
        # Set divisor: 54 for 115200 @ 100MHz
        li r5, 54
        stb r5, UART_DLL(r4)
        li r5, 0
        stb r5, UART_DLM(r4)
        # 8N1, DLAB=0
        li r5, 0x03
        stb r5, UART_LCR(r4)
        blr

#----------------------------------------------------------------------
# __syscall_1 - OutputString (R0=string address in r3)
#----------------------------------------------------------------------
__syscall_1:
        mr r6, r3
1:      lbz r3, 0(r6)
        cmpwi r3, 0
        beq 2f
        bl __uart_putchar
        addi r6, r6, 1
        b 1b
2:      blr

#----------------------------------------------------------------------
# __syscall_4 - OutputChar (R0=char in r3)
#----------------------------------------------------------------------
__syscall_4:
        b __uart_putchar

#----------------------------------------------------------------------
# __syscall_5 - InputChar (R0=result in r3)
#----------------------------------------------------------------------
__syscall_5:
        b __uart_getchar

#----------------------------------------------------------------------
# __syscall_3 - Exit (halt)
#----------------------------------------------------------------------
__syscall_3:
        msrclr r3, 0x8000  # Disable interrupts
1:      nop
        b 1b

#----------------------------------------------------------------------
# __syscall_50 - Random (LFSR)
#----------------------------------------------------------------------
__syscall_50:
        lis r4, __random_state@h
        ori r4, r4, __random_state@l
        lwz r5, 0(r4)
        srwi r6, r5, 7
        xor r6, r6, r5
        srwi r6, r6, 6
        xor r6, r6, r5
        srwi r3, r6, 1
        xor r3, r3, r6
        stw r3, 0(r4)
        blr

#----------------------------------------------------------------------
# UART functions
#----------------------------------------------------------------------
__uart_putchar:
        lis r4, UART_BASE@h
        ori r4, r4, UART_BASE@l
1:      lbz r5, UART_LSR(r4)
        andi. r5, r5, TX_EMPTY
        beq 1b
        stb r3, UART_THR(r4)
        blr

__uart_getchar:
        lis r4, UART_BASE@h
        ori r4, r4, UART_BASE@l
1:      lbz r5, UART_LSR(r4)
        andi. r5, r5, RX_READY
        beq 1b
        lbz r3, UART_RBR(r4)
        blr
