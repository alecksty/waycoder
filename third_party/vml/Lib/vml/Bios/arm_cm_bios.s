@ Simple BIOS for ARM Cortex-M
.syntax unified
.thumb
.section .vectors,"ax"
.align 2
.word 0x20010000
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
@ 60 reserved vectors
.word 0; .word 0; .word 0; .word 0; .word 0; .word 0; .word 0; .word 0; .word 0; .word 0
.word 0; .word 0; .word 0; .word 0; .word 0; .word 0; .word 0; .word 0; .word 0; .word 0
.word 0; .word 0; .word 0; .word 0; .word 0; .word 0; .word 0; .word 0; .word 0; .word 0
.word 0; .word 0; .word 0; .word 0; .word 0; .word 0; .word 0; .word 0; .word 0; .word 0
.word 0; .word 0; .word 0; .word 0; .word 0; .word 0; .word 0; .word 0; .word 0; .word 0
.word 0; .word 0; .word 0; .word 0; .word 0; .word 0; .word 0; .word 0; .word 0; .word 0
.text
.thumb_func
Default_Handler: bx lr  @ 返回异常 (跳过未处理中断,避免HardFault升级)
.thumb_func
lcd_write_cmd: bx lr
.thumb_func
lcd_write_data: bx lr
.thumb_func
lcd_init: bx lr
.global Reset_Handler
.thumb_func
Reset_Handler:
    ldr r0, =0x20010000
    mov sp, r0
    bl lcd_init
    bl main
    b .
    .ltorg
