@ ARM Cortex-M3 (STM32F103) BIOS - VML SYSCALL implementations via USART1
@ Uses USART1 at 115200 baud, 8MHz HSE -> 72MHz PLL

.syntax unified
.thumb
.global __bios_init
.thumb_func
__bios_init:
    @ Enable USART1 and GPIOA clocks (RCC_APB2ENR)
    ldr r0, =0x40021018
    ldr r1, [r0]
    orr r1, r1, #0x4004       @ USART1EN=14, GPIOAEN=2
    str r1, [r0]

    @ GPIOA.9=TX (AF push-pull, 50MHz), GPIOA.10=RX (input floating)
    ldr r0, =0x40010804       @ GPIOA_CRH
    ldr r1, [r0]
    bic r1, r1, #0xFF0        @ Clear CNF9/MODE9, CNF10/MODE10
    orr r1, r1, #0x4B0        @ TX=AFPP 50MHz(0xB), RX=Input float(0x4)
    str r1, [r0]

    @ USART1 init: 115200 baud @ 72MHz PCLK2
    ldr r0, =0x40013800       @ USART1 base
    mov r1, #0x27             @ 72000000/(16*115200) = 39.06 -> 39
    str r1, [r0, #0x08]       @ USART_BRR

    @ Enable USART1, TX, RX
    ldr r1, =0x200C           @ UE=13, TE=3, RE=2
    str r1, [r0, #0x0C]       @ USART_CR1

    bx lr

.global __syscall_1
.thumb_func
__syscall_1:  @ OutputString: R0=string address
    push {r4, lr}
    mov r4, r0
1:  ldrb r0, [r4], #1
    cmp r0, #0
    beq 2f
    bl __uart_putchar
    b 1b
2:  pop {r4, pc}

.global __syscall_4
.thumb_func
__syscall_4:  @ OutputChar: R0=char
    push {lr}
    bl __uart_putchar
    pop {pc}

.global __syscall_5
.thumb_func
__syscall_5:  @ InputChar: R0=result
    push {lr}
    bl __uart_getchar
    pop {pc}

.global __syscall_3
.thumb_func
__syscall_3:  @ Exit
    cpsid i
1:  wfi
    b 1b

.global __syscall_50
.thumb_func
__syscall_50: @ Random: simple LFSR
    push {r1, r2}
    ldr r1, =__random_state
    ldr r0, [r1]
    lsl r2, r0, #1
    tst r0, #0x80
    it ne
    eorne r2, #0x1D
    str r2, [r1]
    pop {r1, r2}
    bx lr

.global __syscall_52
.thumb_func
__syscall_52: @ Sleep: R0=ms, approximate delay
    push {r1, r2}
1:  ldr r1, =4000
2:  sub r1, #1
    bne 2b
    sub r0, #1
    bne 1b
    pop {r1, r2}
    bx lr

__uart_putchar:
    ldr r1, =0x40013800
1:  ldr r2, [r1, #0x00]     @ USART_SR
    lsl r2, r2, #27          @ TXE bit (7)
    bpl 1b
    str r0, [r1, #0x04]      @ USART_DR
    bx lr

__uart_getchar:
    ldr r1, =0x40013800
1:  ldr r2, [r1, #0x00]     @ USART_SR
    lsl r2, r2, #24          @ RXNE bit (5)
    bpl 1b
    ldr r0, [r1, #0x04]      @ USART_DR
    bx lr

.data
.align 2
__random_state: .word 1

@ ======== Stack (BIOS manages stack allocation) ========
.section .bss
.align 3
__stack_bottom:
.space 1024                    @ 1KB stack
__stack_top:
