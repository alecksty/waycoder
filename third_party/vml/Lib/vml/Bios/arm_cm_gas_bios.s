@ ARM Cortex-M GAS BIOS Template
@ For use with arm-none-eabi-as + linker
.syntax unified
.thumb

.section .vectors, "ax", %progbits
.align 2
.global _vectors
_vectors:
.word __stack_top          @ SP
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
.word Default_Handler
.word Default_Handler
.word 0
.word Default_Handler
.word Default_Handler

Default_Handler:
    b .

.global Reset_Handler
.thumb_func
Reset_Handler:
    ldr sp, =__stack_top
    bl main
    b .

@ User code follows (entry: main)
