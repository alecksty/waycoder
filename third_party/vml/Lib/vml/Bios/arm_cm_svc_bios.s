@ ARM Cortex-M GAS BIOS + SVC Handler (cortex-m3)
@ Supports: SYSCALL 1 (OutputString), 3 (Exit), 4 (OutputChar)
@ Uses USART1 for serial output (mapped to QEMU stdio)
.syntax unified
.thumb
.cpu cortex-m3

.section .vectors, "ax", %progbits
.align 2
.global _vectors
_vectors:
.word __stack_top
.word Reset_Handler
.word Default_Handler      @ NMI
.word Default_Handler      @ HardFault
.word Default_Handler      @ MemManage
.word Default_Handler      @ BusFault
.word Default_Handler      @ UsageFault
.word 0
.word 0
.word 0
.word 0
.word SVC_Handler          @ SVCall (SYSCALL)
.word Default_Handler      @ DebugMon
.word 0
.word Default_Handler      @ PendSV
.word Default_Handler      @ SysTick

.text
.align 2
Default_Handler:
    b .

.global Reset_Handler
.thumb_func
Reset_Handler:
    ldr sp, =__stack_top
    bl __uart_init
    bl main
    b .

@ Initialize USART1 for serial output (STM32F100)
__uart_init:
    push {r0, r1, r2, lr}
    ldr r0, =0x40021018      @ RCC_APB2ENR
    ldr r1, [r0]
    ldr r2, =0x4004
    orrs r1, r1, r2          @ bit 14 (USART1) + bit 2 (GPIOA)
    str r1, [r0]
    ldr r0, =0x40010804      @ GPIOA_CRH (PA8-PA15)
    ldr r1, [r0]
    ldr r2, =0xFFFFFF0F
    ands r1, r1, r2          @ Clear PA9 config
    ldr r2, =0xB0
    orrs r1, r1, r2          @ PA9 = AF push-pull 50MHz (0xB)
    str r1, [r0]
    ldr r0, =0x40013808      @ USART1_BRR
    ldr r1, =0x1D4C          @ 9600 @ 8MHz
    str r1, [r0]
    ldr r0, =0x4001380C      @ USART1_CR1
    movs r1, #0x0C
    movs r2, #0x20
    lsl r2, r2, #8
    orrs r1, r1, r2          @ UE=1, TE=1 (0x200C)
    str r1, [r0]
    pop {r0, r1, r2, pc}

@ Output char via USART1 (blocking, poll TXE)
__uart_putc:
    push {r0, r1, r2, lr}
    ldr r1, =0x40013800      @ USART1_SR
wait_txe:
    ldr r2, [r1]
    tst r2, #0x80            @ TXE flag (bit 7)
    beq wait_txe
    ldr r1, =0x40013804      @ USART1_DR
    str r0, [r1]
    pop {r0, r1, r2, pc}

@ Output null-terminated string via USART1
__uart_puts:
    push {r0, r1, r2, lr}
    mov r1, r0
puts_loop:
    ldrb r0, [r1]
    cmp r0, #0
    beq puts_done
    push {r1}
    bl __uart_putc
    pop {r1}
    adds r1, r1, #1
    b puts_loop
puts_done:
    pop {r0, r1, r2, pc}

@ SVC Handler (cortex-m3)
@ SVC number is in the instruction: svc #N = 0xDF00 | N
@ Exception frame (auto-pushed by HW): R0,R1,R2,R3,R12,LR,PC,xPSR
@ After push {r4,r5,lr}: SP points to r4; frame starts at SP+12
.thumb_func
.global SVC_Handler
SVC_Handler:
    push {r4, r5, lr}
    mov r4, r0               @ Save R0 (SYSCALL arg1)
    mov r5, r1               @ Save R1 (SYSCALL arg2)
    mov r0, sp
    adds r0, r0, #12         @ r0 = start of exception frame
    ldr r0, [r0, #24]        @ PC at offset 24 from frame start
    subs r0, r0, #2          @ Back to SVC instruction
    ldrh r0, [r0]            @ SVC = 0xDFxx
    uxtb r0, r0              @ Extract SVC number
    cmp r0, #1
    beq do_output_string
    cmp r0, #3
    beq do_exit
    cmp r0, #4
    beq do_output_char
    pop {r4, r5, pc}         @ Unknown SVC: return

do_output_string:
    mov r0, r4               @ R0 = string address
    bl __uart_puts
    pop {r4, r5, pc}

do_exit:
    wfe
    b do_exit

do_output_char:
    mov r0, r4               @ R0 = character
    bl __uart_putc
    pop {r4, r5, pc}
