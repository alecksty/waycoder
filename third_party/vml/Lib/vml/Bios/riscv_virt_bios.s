# RISC-V virt machine BIOS
# UART (ns16550) at 0x10000000
# ECALL handler: a7=syscall#, a0=arg
#   syscall 1: OutputString(a0 = string address)
#   syscall 3: Exit
#   syscall 4: OutputChar(a0 = character)
.text
.globl _start
_start:
    la sp, _stack_top
    la t0, trap_vector
    csrw mtvec, t0
    jal main
1:  wfi
    j 1b

.align 4
trap_vector:
    addi sp, sp, -16
    sw a0, 0(sp)
    sw a7, 4(sp)
    sw t0, 8(sp)
    sw t1, 12(sp)

    csrr t0, mcause
    li t1, 11
    bne t0, t1, 1f

    li t0, 1
    beq a7, t0, do_puts
    li t0, 3
    beq a7, t0, do_exit
    li t0, 4
    beq a7, t0, do_putc
    j 1f

do_puts:
    mv t1, a0
2:  lbu t0, 0(t1)
    beqz t0, 3f
    li t2, 0x10000000
    sw t0, 0(t2)
    addi t1, t1, 1
    j 2b
3:  j 1f

do_putc:
    li t2, 0x10000000
    sw a0, 0(t2)
    j 1f

do_exit:
1:  wfi
    j 1b

    lw a0, 0(sp)
    lw a7, 4(sp)
    lw t0, 8(sp)
    lw t1, 12(sp)
    addi sp, sp, 16
    mret

    .space 4096
_stack_top:
