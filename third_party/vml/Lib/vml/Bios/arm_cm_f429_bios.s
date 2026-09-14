@ STM32F429 ARM Cortex-M BIOS (GNU AS syntax)
@ 76-entry vector table + handlers + calls main
@ All instructions use .byte/.word to avoid assembler reordering bugs

.syntax unified
.thumb

@ ======== Vector Table (16 + 60 = 76 entries, 304 bytes) ========
.word 0x20000748         @ 0: SP (__initial_sp)
.word Reset_Handler      @ 1: Reset (Thumb bit added by assembler)
.word Default_Handler    @ 2: NMI
.word Default_Handler    @ 3: HardFault
.word Default_Handler    @ 4: MemManage
.word Default_Handler    @ 5: BusFault
.word Default_Handler    @ 6: UsageFault
.word 0
.word 0
.word 0
.word 0
.word Default_Handler    @ 11: SVCall
.word Default_Handler    @ 12: DebugMon
.word 0
.word Default_Handler    @ 14: PendSV
.word Default_Handler    @ 15: SysTick
@ External interrupts 0..59
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0
.word 0

@ ======== Handlers (raw bytes to avoid assembler Pass1/Pass2 ordering bugs) ========
Default_Handler:
    .byte 0xFE, 0xE7     @ B .

.global Reset_Handler
.thumb_func
Reset_Handler:
    .byte 0x48, 0x00     @ ldr r0, [pc, #0]
    .byte 0x01, 0xE0     @ b #4
    .word 0x20000748     @ SP literal
    .byte 0x85, 0x46     @ mov sp, r0
    bl main
    .byte 0xFE, 0xE7     @ B .
