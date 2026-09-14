@ STM32F429 ARM Cortex-M 启动代码 (仿 HAL startup_stm32f429xx.s)
@ 正确向量表 + 最小 Reset_Handler → bl main → VML 用户代码

.syntax unified
.thumb

@ ===== Stack 定义 =====
.set __stack_top, 0x20010000

@ ===== Vector Table (仿 HAL: DCD 指令) =====
.section .vectors
.vectors:
.word 0x20010000         @ 0: SP
.word Reset_Handler      @ 1: Reset
.word Default_Handler    @ 2: NMI
.word Default_Handler    @ 3: HardFault
.word Default_Handler    @ 4: MemManage
.word Default_Handler    @ 5: BusFault
.word Default_Handler    @ 6: UsageFault
.word 0                  @ 7-10: Reserved
.word 0
.word 0
.word 0
.word Default_Handler    @ 11: SVCall
.word Default_Handler    @ 12: DebugMon
.word 0                  @ 13: Reserved
.word Default_Handler    @ 14: PendSV
.word Default_Handler    @ 15: SysTick

@ ===== Default Handler (死循环) =====
Default_Handler:
    b .

@ ===== Reset Handler =====
.global Reset_Handler
.thumb_func
Reset_Handler:
    ldr sp, =0x20010000
    bl main
    b .

@ VML 用户代码由 translator 追加 (entry: main)
