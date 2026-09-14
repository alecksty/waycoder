@ ARM Cortex-M3 BIOS for LM3S6965 (QEMU)
@ UART0 at 0x4000C000 (no init needed, write-only)
@ Exception return via BX LR (LR = EXC_RETURN on entry)
.syntax unified
.thumb
.cpu cortex-m3

.section .vectors, "ax", %progbits
.align 2
.global _vectors
_vectors:
.word __stack_top
.word Reset_Handler
.word Default_Handler
.word Default_Handler
.word Default_Handler
.word Default_Handler
.word Default_Handler
.word 0
.word 0
.word 0
.word 0
.word SVC_Handler
.word Default_Handler
.word 0
.word Default_Handler
.word Default_Handler

.text
Default_Handler:
    b .

.global Reset_Handler
.thumb_func
Reset_Handler:
    ldr r0, =__stack_top
    mov sp, r0
    bl main
    b .
.ltorg

.global SVC_Handler
.thumb_func
SVC_Handler:
    push {r4, r5, lr}        @ Save r4, r5 + EXC_RETURN
    mov r4, r0               @ Save R0 (SYSCALL arg)
    mov r5, r1
    mov r0, sp
    adds r0, r0, #12         @ Start of hw exception frame
    ldr r0, [r0, #24]        @ Saved PC from frame
    subs r0, r0, #2          @ Back to SVC instruction
    ldrh r0, [r0]            @ SVC = 0xDFxx
    uxtb r0, r0              @ SVC number
    cmp r0, #1
    beq do_puts
    cmp r0, #3
    beq do_halt
    cmp r0, #4
    beq do_putc
    pop {r4, r5, lr}
    bx lr

do_puts:
    mov r0, r4
    push {r4, r5}
    mov r5, r0
1:  ldrb r4, [r5]
    cmp r4, #0
    beq 2f
    ldr r0, =0x4000C000
    str r4, [r0]
    adds r5, r5, #1
    b 1b
2:  pop {r4, r5}
    pop {r4, r5, lr}
    bx lr

do_putc:
    mov r0, r4
    ldr r1, =0x4000C000
    str r0, [r1]
    pop {r4, r5, lr}
    bx lr

do_halt:
    wfe
    b do_halt
