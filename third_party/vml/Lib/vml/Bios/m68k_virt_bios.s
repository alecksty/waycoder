| m68k virt machine BIOS for QEMU
| PL011 UART at 0x9000000
|   DR at +0x000, FR at +0x018 (TXFF=bit5)
| RAM at 0x0
| Registers: %aN (addr), %dN (data), %sp (stack ptr)
        .text
        .globl _start
        .globl sys_write
        .globl sys_putc

_start:
        move.l  #0x4000,%sp
        jmp     main

sys_write:
        move.l  %a0,%a1
1:      move.b  (%a1)+,%d0
        beq     2f
        bsr     uart_putc
        bra     1b
2:      rts

sys_putc:
        | Char value comes in %d0 (VML R0 = D0)
uart_putc:
        move.l  %d0,-(%sp)        | Save char
        move.l  #0x9000018,%a0    | FR
3:      btst    #5,(%a0)          | TXFF? (1 = full)
        bne     3b                | wait if full
        move.l  #0x9000000,%a0    | DR
        move.b  (%sp)+,(%a0)      | Write char (pop from stack)
        rts
