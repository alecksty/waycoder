# MIPS Malta QEMU BIOS
# Status: CODE EXECUTES but UART output pending
# The Malta board uses the GT-64120 southbridge with SuperIO UART
# UART registers are accessible but QEMU doesn't route writes to serial chardev
# without proper PCI/YAMON initialization.
#
# This BIOS provides sys_write/sys_putc that attempt UART output.
# For verification, use `-d in_asm` to trace execution.

.text
.globl sys_write
sys_write:
    move $t0, $a0
1:  lbu $t1, 0($t0)
    beqz $t1, 2f
    nop
    # Try SuperIO UART (Malta COM1)
    lui $t2, 0xbf00
    ori $t2, $t2, 0x0900
    sb $t1, 0($t2)
    # Also try PCI I/O window
    lui $t2, 0x1e00
    ori $t2, $t2, 0x03f8
    sb $t1, 0($t2)
    nop
    addiu $t0, $t0, 1
    j 1b
    nop
2:  jr $ra
    nop

.globl sys_putc
sys_putc:
    lui $t2, 0xbf00
    ori $t2, $t2, 0x0900
    sb $a0, 0($t2)
    lui $t2, 0x1e00
    ori $t2, $t2, 0x03f8
    sb $a0, 0($t2)
    jr $ra
    nop
