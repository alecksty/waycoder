\ STM32F103C8T6设备定义 - Forth文件
\ 生成自: STMicroelectronics/STM32/STM32F103C8T6
\ 版本: 1.0
\ 日期: 2026-04-16
\ 作者: VML Team
\ 描述: 32-bit ARM Cortex-M3 MCU with 64KB Flash, 20KB RAM, 72MHz
\ CPU架构: ARM-Cortex-M3
\ 位宽: 32位
\ 时钟频率: 72000000 Hz

\ =========================================
\ STM32F103C8T6设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" STM32F103C8T6" ;
: MANUFACTURER  S" STMicroelectronics" ;
: FAMILY        S" STM32" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" ARM-Cortex-M3" ;
32 CONSTANT BITS
72000000 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0x00 CONSTANT R0  \ General Purpose Register 0
0x04 CONSTANT R1  \ General Purpose Register 1
0x08 CONSTANT R2  \ General Purpose Register 2
0x0C CONSTANT R3  \ General Purpose Register 3
0x10 CONSTANT R4  \ General Purpose Register 4
0x14 CONSTANT R5  \ General Purpose Register 5
0x18 CONSTANT R6  \ General Purpose Register 6
0x1C CONSTANT R7  \ General Purpose Register 7
0x20 CONSTANT R8  \ General Purpose Register 8
0x24 CONSTANT R9  \ General Purpose Register 9
0x28 CONSTANT R10  \ General Purpose Register 10
0x2C CONSTANT R11  \ General Purpose Register 11
0x30 CONSTANT R12  \ General Purpose Register 12
0x34 CONSTANT SP  \ Stack Pointer
0x38 CONSTANT LR  \ Link Register
0x3C CONSTANT PC  \ Program Counter
0x40 CONSTANT XPSR  \ Program Status Register

\ 内存段定义
0x08000000 CONSTANT FLASH-START
0x0800FFFF CONSTANT FLASH-END
65536 CONSTANT FLASH-SIZE  \ Main Flash Memory (64KB)
0x1FFFF000 CONSTANT SYSTEM_MEMORY-START
0x1FFFF7FF CONSTANT SYSTEM_MEMORY-END
2048 CONSTANT SYSTEM_MEMORY-SIZE  \ System Memory (2KB)
0x20000000 CONSTANT SRAM-START
0x20004FFF CONSTANT SRAM-END
20480 CONSTANT SRAM-SIZE  \ SRAM (20KB)
0x40000000 CONSTANT PERIPHERAL-START
0x40023FFF CONSTANT PERIPHERAL-END
143360 CONSTANT PERIPHERAL-SIZE  \ Peripheral Registers
0xE0000000 CONSTANT CORTEX_M-START
0xE00FFFFF CONSTANT CORTEX_M-END
1048576 CONSTANT CORTEX_M-SIZE  \ Core Peripheral Registers

\ 外设定义
\ Reset and Clock Control
0x40021000 CONSTANT RCC-BASE
0x00 CONSTANT RCC-CR
0x04 CONSTANT RCC-CFGR
0x18 CONSTANT RCC-APB2ENR
0x1C CONSTANT RCC-APB1ENR
\ GPIO Port A
0x40010800 CONSTANT GPIOA-BASE
0x00 CONSTANT GPIOA-CRL
0x04 CONSTANT GPIOA-CRH
0x08 CONSTANT GPIOA-IDR
0x0C CONSTANT GPIOA-ODR
0x10 CONSTANT GPIOA-BSRR
0x14 CONSTANT GPIOA-BRR
0x18 CONSTANT GPIOA-LCKR
\ GPIO Port B
0x40010C00 CONSTANT GPIOB-BASE
0x00 CONSTANT GPIOB-CRL
0x04 CONSTANT GPIOB-CRH
0x08 CONSTANT GPIOB-IDR
0x0C CONSTANT GPIOB-ODR
0x10 CONSTANT GPIOB-BSRR
0x14 CONSTANT GPIOB-BRR
\ GPIO Port C
0x40011000 CONSTANT GPIOC-BASE
0x00 CONSTANT GPIOC-CRL
0x04 CONSTANT GPIOC-CRH
0x08 CONSTANT GPIOC-IDR
0x0C CONSTANT GPIOC-ODR
0x10 CONSTANT GPIOC-BSRR
\ USART 1
0x40013800 CONSTANT USART1-BASE
0x00 CONSTANT USART1-SR
0x04 CONSTANT USART1-DR
0x08 CONSTANT USART1-BRR
0x0C CONSTANT USART1-CR1
0x10 CONSTANT USART1-CR2
0x14 CONSTANT USART1-CR3
0x18 CONSTANT USART1-GTPR
\ USART 2
0x40004400 CONSTANT USART2-BASE
0x00 CONSTANT USART2-SR
0x04 CONSTANT USART2-DR
0x08 CONSTANT USART2-BRR
0x0C CONSTANT USART2-CR1
\ SPI 1
0x40013000 CONSTANT SPI1-BASE
0x00 CONSTANT SPI1-CR1
0x04 CONSTANT SPI1-CR2
0x08 CONSTANT SPI1-SR
0x0C CONSTANT SPI1-DR
\ SPI 2
0x40003800 CONSTANT SPI2-BASE
0x00 CONSTANT SPI2-CR1
0x04 CONSTANT SPI2-CR2
0x08 CONSTANT SPI2-SR
0x0C CONSTANT SPI2-DR
\ I2C 1
0x40005400 CONSTANT I2C1-BASE
0x00 CONSTANT I2C1-CR1
0x04 CONSTANT I2C1-CR2
0x08 CONSTANT I2C1-SR1
0x0C CONSTANT I2C1-SR2
0x10 CONSTANT I2C1-DR
0x14 CONSTANT I2C1-CCR
0x18 CONSTANT I2C1-TRISE
\ I2C 2
0x40005800 CONSTANT I2C2-BASE
0x00 CONSTANT I2C2-CR1
0x04 CONSTANT I2C2-CR2
0x08 CONSTANT I2C2-SR1
0x0C CONSTANT I2C2-SR2
0x10 CONSTANT I2C2-DR
0x14 CONSTANT I2C2-CCR
\ Advanced Timer 1
0x40012C00 CONSTANT TIM1-BASE
0x00 CONSTANT TIM1-CR1
0x04 CONSTANT TIM1-CR2
0x08 CONSTANT TIM1-SMCR
0x0C CONSTANT TIM1-DIER
0x10 CONSTANT TIM1-SR
0x14 CONSTANT TIM1-EGR
0x18 CONSTANT TIM1-CCMR1
0x1C CONSTANT TIM1-CCMR2
0x20 CONSTANT TIM1-CCER
0x24 CONSTANT TIM1-CNT
0x28 CONSTANT TIM1-PSC
0x2C CONSTANT TIM1-ARR
0x30 CONSTANT TIM1-RCR
0x34 CONSTANT TIM1-CCR1
0x38 CONSTANT TIM1-CCR2
0x3C CONSTANT TIM1-CCR3
0x40 CONSTANT TIM1-CCR4
0x44 CONSTANT TIM1-BDTR
\ General Purpose Timer 2
0x40000400 CONSTANT TIM2-BASE
0x00 CONSTANT TIM2-CR1
0x24 CONSTANT TIM2-CNT
0x28 CONSTANT TIM2-PSC
0x2C CONSTANT TIM2-ARR
0x34 CONSTANT TIM2-CCR1
0x38 CONSTANT TIM2-CCR2
0x3C CONSTANT TIM2-CCR3
0x40 CONSTANT TIM2-CCR4
\ General Purpose Timer 3
0x40000400 CONSTANT TIM3-BASE
0x00 CONSTANT TIM3-CR1
0x24 CONSTANT TIM3-CNT
0x2C CONSTANT TIM3-ARR
0x34 CONSTANT TIM3-CCR1
0x38 CONSTANT TIM3-CCR2
0x3C CONSTANT TIM3-CCR3
0x40 CONSTANT TIM3-CCR4
\ General Purpose Timer 4
0x40000800 CONSTANT TIM4-BASE
0x00 CONSTANT TIM4-CR1
0x24 CONSTANT TIM4-CNT
0x2C CONSTANT TIM4-ARR
0x34 CONSTANT TIM4-CCR1
0x38 CONSTANT TIM4-CCR2
0x3C CONSTANT TIM4-CCR3
0x40 CONSTANT TIM4-CCR4
\ ADC 1
0x40012400 CONSTANT ADC1-BASE
0x00 CONSTANT ADC1-SR
0x04 CONSTANT ADC1-CR1
0x08 CONSTANT ADC1-CR2
0x0C CONSTANT ADC1-SMPR1
0x10 CONSTANT ADC1-SMPR2
0x14 CONSTANT ADC1-JOFR1
0x18 CONSTANT ADC1-JOFR2
0x1C CONSTANT ADC1-JOFR3
0x20 CONSTANT ADC1-JOFR4
0x24 CONSTANT ADC1-HTR
0x28 CONSTANT ADC1-LTR
0x2C CONSTANT ADC1-SQRT1
0x30 CONSTANT ADC1-SQRT2
0x34 CONSTANT ADC1-SQRT3
0x38 CONSTANT ADC1-JSQR
0x3C CONSTANT ADC1-JDR1
0x40 CONSTANT ADC1-JDR2
0x44 CONSTANT ADC1-JDR3
0x48 CONSTANT ADC1-JDR4
0x4C CONSTANT ADC1-DR
\ DMA Controller 1
0x40020000 CONSTANT DMA1-BASE
0x00 CONSTANT DMA1-ISR
0x04 CONSTANT DMA1-IFCR
0x08 CONSTANT DMA1-CCR1
0x0C CONSTANT DMA1-CNDTR1
0x10 CONSTANT DMA1-CPAR1
0x14 CONSTANT DMA1-CMAR1
0x1C CONSTANT DMA1-CCR2
0x20 CONSTANT DMA1-CNDTR2
0x24 CONSTANT DMA1-CPAR2
0x28 CONSTANT DMA1-CMAR2
\ Power Control
0x40007000 CONSTANT PWR-BASE
0x00 CONSTANT PWR-CR
0x04 CONSTANT PWR-CSR
\ Backup Registers
0x40006C00 CONSTANT BKP-BASE
0x04 CONSTANT BKP-DR1
0x08 CONSTANT BKP-DR2
0x2C CONSTANT BKP-CSR
\ Window Watchdog
0x40002C00 CONSTANT WWDG-BASE
0x00 CONSTANT WWDG-CR
0x04 CONSTANT WWDG-CFR
0x08 CONSTANT WWDG-SR
\ Independent Watchdog
0x40003000 CONSTANT IWDG-BASE
0x00 CONSTANT IWDG-KR
0x04 CONSTANT IWDG-PR
0x08 CONSTANT IWDG-RLR
\ External Interrupt/Event Controller
0x40010400 CONSTANT EXTI-BASE
0x00 CONSTANT EXTI-IMR
0x04 CONSTANT EXTI-EMR
0x08 CONSTANT EXTI-RTSR
0x0C CONSTANT EXTI-FTSR
0x10 CONSTANT EXTI-SWIER
0x14 CONSTANT EXTI-PR
\ Alternate Function IO
0x40010000 CONSTANT AFIO-BASE
0x00 CONSTANT AFIO-EVCR
0x04 CONSTANT AFIO-MAPR
0x08 CONSTANT AFIO-EXTICR1
0x0C CONSTANT AFIO-EXTICR2
0x10 CONSTANT AFIO-EXTICR3
0x1C CONSTANT AFIO-MAPR2

\ 中断向量定义
0 CONSTANT INT-WWDG  \ Window Watchdog Interrupt
1 CONSTANT INT-PVD  \ PVD through EXTI Line detection
2 CONSTANT INT-TAMPER  \ Tamper Interrupt
3 CONSTANT INT-RTC  \ RTC Global Interrupt
4 CONSTANT INT-FLASH  \ FLASH Global Interrupt
5 CONSTANT INT-RCC  \ RCC Global Interrupt
6 CONSTANT INT-EXTI0  \ EXTI Line 0 Interrupt
7 CONSTANT INT-EXTI1  \ EXTI Line 1 Interrupt
8 CONSTANT INT-EXTI2  \ EXTI Line 2 Interrupt
9 CONSTANT INT-EXTI3  \ EXTI Line 3 Interrupt
10 CONSTANT INT-EXTI4  \ EXTI Line 4 Interrupt
11 CONSTANT INT-DMA1_CHANNEL1  \ DMA1 Channel 1 Interrupt
12 CONSTANT INT-DMA1_CHANNEL2  \ DMA1 Channel 2 Interrupt
13 CONSTANT INT-DMA1_CHANNEL3  \ DMA1 Channel 3 Interrupt
14 CONSTANT INT-DMA1_CHANNEL4  \ DMA1 Channel 4 Interrupt
15 CONSTANT INT-DMA1_CHANNEL5  \ DMA1 Channel 5 Interrupt
16 CONSTANT INT-DMA1_CHANNEL6  \ DMA1 Channel 6 Interrupt
17 CONSTANT INT-DMA1_CHANNEL7  \ DMA1 Channel 7 Interrupt
18 CONSTANT INT-ADC1_2  \ ADC1 and ADC2 Global Interrupt
19 CONSTANT INT-USB_HP_CAN_TX  \ USB HP/CAN TX Interrupts
20 CONSTANT INT-USB_LP_CAN_RX0  \ USB LP/CAN RX0 Interrupt
21 CONSTANT INT-CAN_RX1  \ CAN RX1 Interrupt
22 CONSTANT INT-CAN_SCE  \ CAN SCE Interrupt
23 CONSTANT INT-EXTI9_5  \ EXTI Line 9..5 Interrupt
25 CONSTANT INT-TIM1_BRK  \ TIM1 Break Interrupt
26 CONSTANT INT-TIM1_UP  \ TIM1 Update Interrupt
27 CONSTANT INT-TIM1_TRG_COM  \ TIM1 Trigger and Commutation
28 CONSTANT INT-TIM1_CC  \ TIM1 Capture Compare Interrupt
29 CONSTANT INT-TIM2  \ TIM2 Global Interrupt
30 CONSTANT INT-TIM3  \ TIM3 Global Interrupt
31 CONSTANT INT-TIM4  \ TIM4 Global Interrupt
32 CONSTANT INT-I2C1_EV  \ I2C1 Event Interrupt
33 CONSTANT INT-I2C1_ER  \ I2C1 Error Interrupt
34 CONSTANT INT-I2C2_EV  \ I2C2 Event Interrupt
35 CONSTANT INT-I2C2_ER  \ I2C2 Error Interrupt
35 CONSTANT INT-SPI1  \ SPI1 Global Interrupt
36 CONSTANT INT-SPI2  \ SPI2 Global Interrupt
37 CONSTANT INT-USART1  \ USART1 Global Interrupt
38 CONSTANT INT-USART2  \ USART2 Global Interrupt
39 CONSTANT INT-USART3  \ USART3 Global Interrupt
40 CONSTANT INT-EXTI15_10  \ EXTI Line 15..10 Interrupt
41 CONSTANT INT-RTCALARM  \ RTC Alarm through EXTI
42 CONSTANT INT-USBWAKEUP  \ USB Wakeup from suspend

\ 引脚定义
1 CONSTANT PIN-VBAT  \ Battery Supply
2 CONSTANT PIN-PC13  \ GPIO Port C Pin 13
3 CONSTANT PIN-PC14  \ GPIO Port C Pin 14
4 CONSTANT PIN-PC15  \ GPIO Port C Pin 15
5 CONSTANT PIN-PD0  \ GPIO Port D Pin 0
6 CONSTANT PIN-PD1  \ GPIO Port D Pin 1
7 CONSTANT PIN-NRST  \ Reset
8 CONSTANT PIN-VSSA  \ Analog Ground
9 CONSTANT PIN-VDDA  \ Analog Supply
10 CONSTANT PIN-PA0  \ GPIO Port A Pin 0 / ADC1_IN0
11 CONSTANT PIN-PA1  \ GPIO Port A Pin 1 / ADC1_IN1
12 CONSTANT PIN-PA2  \ GPIO Port A Pin 2 / ADC1_IN2 / USART2_TX
13 CONSTANT PIN-PA3  \ GPIO Port A Pin 3 / ADC1_IN3 / USART2_RX
14 CONSTANT PIN-PA4  \ GPIO Port A Pin 4 / DAC_OUT1 / SPI1_NSS
15 CONSTANT PIN-PA5  \ GPIO Port A Pin 5 / DAC_OUT2 / SPI1_SCK
16 CONSTANT PIN-PA6  \ GPIO Port A Pin 6 / ADC1_IN6 / SPI1_MISO / TIM3_CH1
17 CONSTANT PIN-PA7  \ GPIO Port A Pin 7 / ADC1_IN7 / SPI1_MOSI / TIM3_CH2
18 CONSTANT PIN-PB0  \ GPIO Port B Pin 0 / ADC1_IN8 / TIM3_CH3
19 CONSTANT PIN-PB1  \ GPIO Port B Pin 1 / ADC1_IN9 / TIM3_CH4
20 CONSTANT PIN-PB2  \ GPIO Port B Pin 2
21 CONSTANT PIN-PB10  \ GPIO Port B Pin 10 / I2C2_SCL / USART3_TX
22 CONSTANT PIN-PB11  \ GPIO Port B Pin 11 / I2C2_SDA / USART3_RX
23 CONSTANT PIN-VSS  \ Ground
24 CONSTANT PIN-VDD  \ Digital Supply
25 CONSTANT PIN-PB12  \ GPIO Port B Pin 12 / SPI2_NSS / I2C2_SMBA
26 CONSTANT PIN-PB13  \ GPIO Port B Pin 13 / SPI2_SCK / USART3_CK
27 CONSTANT PIN-PB14  \ GPIO Port B Pin 14 / SPI2_MISO / USART3_RTS
28 CONSTANT PIN-PB15  \ GPIO Port B Pin 15 / SPI2_MOSI / USART3_CTS
29 CONSTANT PIN-PA8  \ GPIO Port A Pin 8 / USART1_CK / TIM1_CH1 / MCO
30 CONSTANT PIN-PA9  \ GPIO Port A Pin 9 / USART1_TX / TIM1_CH2
31 CONSTANT PIN-PA10  \ GPIO Port A Pin 10 / USART1_RX / TIM1_CH3
32 CONSTANT PIN-PA11  \ GPIO Port A Pin 11 / USART1_CT / TIM1_CH4 / CAN_RX
33 CONSTANT PIN-PA12  \ GPIO Port A Pin 12 / USART1_RT / TIM1_ETR / CAN_TX
34 CONSTANT PIN-PA13  \ JTMS/SWDIO
37 CONSTANT PIN-PA14  \ JTCK/SWCLK
38 CONSTANT PIN-PA15  \ GPIO Port A Pin 15 / JTDI / TIM2_CH1_ETR / SPI1_NSS
39 CONSTANT PIN-PB3  \ GPIO Port B Pin 3 / JTDO / TIM2_CH2 / SPI1_SCK
40 CONSTANT PIN-PB4  \ GPIO Port B Pin 4 / JNTRST / TIM3_CH1 / SPI1_MISO
41 CONSTANT PIN-PB5  \ GPIO Port B Pin 5 / TIM3_CH2 / SPI1_MOSI / I2C1_SMBA
42 CONSTANT PIN-PB6  \ GPIO Port B Pin 6 / TIM4_CH1 / I2C1_SCL / USART1_TX
43 CONSTANT PIN-PB7  \ GPIO Port B Pin 7 / TIM4_CH2 / I2C1_SDA / USART1_RX
44 CONSTANT PIN-BOOT0  \ Boot Selection
45 CONSTANT PIN-PB8  \ GPIO Port B Pin 8 / TIM4_CH3 / I2C1_SCL / CAN_RX
46 CONSTANT PIN-PB9  \ GPIO Port B Pin 9 / TIM4_CH4 / I2C1_SDA / CAN_TX
47 CONSTANT PIN-VSS  \ Ground
48 CONSTANT PIN-VDD  \ Digital Supply

\ =========================================
\ 寄存器访问字
\ =========================================

\ 通用寄存器访问
: R0@ ( -- n ) R0 L@ ;
: R0! ( n -- ) R0 L! ;

: R1@ ( -- n ) R1 L@ ;
: R1! ( n -- ) R1 L! ;

: R2@ ( -- n ) R2 L@ ;
: R2! ( n -- ) R2 L! ;

: R3@ ( -- n ) R3 L@ ;
: R3! ( n -- ) R3 L! ;

: R4@ ( -- n ) R4 L@ ;
: R4! ( n -- ) R4 L! ;

: R5@ ( -- n ) R5 L@ ;
: R5! ( n -- ) R5 L! ;

: R6@ ( -- n ) R6 L@ ;
: R6! ( n -- ) R6 L! ;

: R7@ ( -- n ) R7 L@ ;
: R7! ( n -- ) R7 L! ;

: R8@ ( -- n ) R8 L@ ;
: R8! ( n -- ) R8 L! ;

: R9@ ( -- n ) R9 L@ ;
: R9! ( n -- ) R9 L! ;

: R10@ ( -- n ) R10 L@ ;
: R10! ( n -- ) R10 L! ;

: R11@ ( -- n ) R11 L@ ;
: R11! ( n -- ) R11 L! ;

: R12@ ( -- n ) R12 L@ ;
: R12! ( n -- ) R12 L! ;

: SP@ ( -- n ) SP L@ ;
: SP! ( n -- ) SP L! ;

: LR@ ( -- n ) LR L@ ;
: LR! ( n -- ) LR L! ;

: PC@ ( -- n ) PC L@ ;
: PC! ( n -- ) PC L! ;

: XPSR@ ( -- n ) XPSR L@ ;
: XPSR! ( n -- ) XPSR L! ;

\ 外设访问
\ RCC外设
: RCC-CR@ ( -- n ) RCC-CR L@ ;
: RCC-CR! ( n -- ) RCC-CR L! ;
: RCC-CFGR@ ( -- n ) RCC-CFGR L@ ;
: RCC-CFGR! ( n -- ) RCC-CFGR L! ;
: RCC-APB2ENR@ ( -- n ) RCC-APB2ENR L@ ;
: RCC-APB2ENR! ( n -- ) RCC-APB2ENR L! ;
: RCC-APB1ENR@ ( -- n ) RCC-APB1ENR L@ ;
: RCC-APB1ENR! ( n -- ) RCC-APB1ENR L! ;

\ GPIOA外设
: GPIOA-CRL@ ( -- n ) GPIOA-CRL L@ ;
: GPIOA-CRL! ( n -- ) GPIOA-CRL L! ;
: GPIOA-CRH@ ( -- n ) GPIOA-CRH L@ ;
: GPIOA-CRH! ( n -- ) GPIOA-CRH L! ;
: GPIOA-IDR@ ( -- n ) GPIOA-IDR L@ ;
: GPIOA-IDR! ( n -- ) GPIOA-IDR L! ;
: GPIOA-ODR@ ( -- n ) GPIOA-ODR L@ ;
: GPIOA-ODR! ( n -- ) GPIOA-ODR L! ;
: GPIOA-BSRR@ ( -- n ) GPIOA-BSRR L@ ;
: GPIOA-BSRR! ( n -- ) GPIOA-BSRR L! ;
: GPIOA-BRR@ ( -- n ) GPIOA-BRR L@ ;
: GPIOA-BRR! ( n -- ) GPIOA-BRR L! ;
: GPIOA-LCKR@ ( -- n ) GPIOA-LCKR L@ ;
: GPIOA-LCKR! ( n -- ) GPIOA-LCKR L! ;

\ GPIOB外设
: GPIOB-CRL@ ( -- n ) GPIOB-CRL L@ ;
: GPIOB-CRL! ( n -- ) GPIOB-CRL L! ;
: GPIOB-CRH@ ( -- n ) GPIOB-CRH L@ ;
: GPIOB-CRH! ( n -- ) GPIOB-CRH L! ;
: GPIOB-IDR@ ( -- n ) GPIOB-IDR L@ ;
: GPIOB-IDR! ( n -- ) GPIOB-IDR L! ;
: GPIOB-ODR@ ( -- n ) GPIOB-ODR L@ ;
: GPIOB-ODR! ( n -- ) GPIOB-ODR L! ;
: GPIOB-BSRR@ ( -- n ) GPIOB-BSRR L@ ;
: GPIOB-BSRR! ( n -- ) GPIOB-BSRR L! ;
: GPIOB-BRR@ ( -- n ) GPIOB-BRR L@ ;
: GPIOB-BRR! ( n -- ) GPIOB-BRR L! ;

\ GPIOC外设
: GPIOC-CRL@ ( -- n ) GPIOC-CRL L@ ;
: GPIOC-CRL! ( n -- ) GPIOC-CRL L! ;
: GPIOC-CRH@ ( -- n ) GPIOC-CRH L@ ;
: GPIOC-CRH! ( n -- ) GPIOC-CRH L! ;
: GPIOC-IDR@ ( -- n ) GPIOC-IDR L@ ;
: GPIOC-IDR! ( n -- ) GPIOC-IDR L! ;
: GPIOC-ODR@ ( -- n ) GPIOC-ODR L@ ;
: GPIOC-ODR! ( n -- ) GPIOC-ODR L! ;
: GPIOC-BSRR@ ( -- n ) GPIOC-BSRR L@ ;
: GPIOC-BSRR! ( n -- ) GPIOC-BSRR L! ;

\ USART1外设
: USART1-SR@ ( -- n ) USART1-SR L@ ;
: USART1-SR! ( n -- ) USART1-SR L! ;
: USART1-DR@ ( -- n ) USART1-DR L@ ;
: USART1-DR! ( n -- ) USART1-DR L! ;
: USART1-BRR@ ( -- n ) USART1-BRR L@ ;
: USART1-BRR! ( n -- ) USART1-BRR L! ;
: USART1-CR1@ ( -- n ) USART1-CR1 L@ ;
: USART1-CR1! ( n -- ) USART1-CR1 L! ;
: USART1-CR2@ ( -- n ) USART1-CR2 L@ ;
: USART1-CR2! ( n -- ) USART1-CR2 L! ;
: USART1-CR3@ ( -- n ) USART1-CR3 L@ ;
: USART1-CR3! ( n -- ) USART1-CR3 L! ;
: USART1-GTPR@ ( -- n ) USART1-GTPR L@ ;
: USART1-GTPR! ( n -- ) USART1-GTPR L! ;

\ USART2外设
: USART2-SR@ ( -- n ) USART2-SR L@ ;
: USART2-SR! ( n -- ) USART2-SR L! ;
: USART2-DR@ ( -- n ) USART2-DR L@ ;
: USART2-DR! ( n -- ) USART2-DR L! ;
: USART2-BRR@ ( -- n ) USART2-BRR L@ ;
: USART2-BRR! ( n -- ) USART2-BRR L! ;
: USART2-CR1@ ( -- n ) USART2-CR1 L@ ;
: USART2-CR1! ( n -- ) USART2-CR1 L! ;

\ SPI1外设
: SPI1-CR1@ ( -- n ) SPI1-CR1 L@ ;
: SPI1-CR1! ( n -- ) SPI1-CR1 L! ;
: SPI1-CR2@ ( -- n ) SPI1-CR2 L@ ;
: SPI1-CR2! ( n -- ) SPI1-CR2 L! ;
: SPI1-SR@ ( -- n ) SPI1-SR L@ ;
: SPI1-SR! ( n -- ) SPI1-SR L! ;
: SPI1-DR@ ( -- n ) SPI1-DR L@ ;
: SPI1-DR! ( n -- ) SPI1-DR L! ;

\ SPI2外设
: SPI2-CR1@ ( -- n ) SPI2-CR1 L@ ;
: SPI2-CR1! ( n -- ) SPI2-CR1 L! ;
: SPI2-CR2@ ( -- n ) SPI2-CR2 L@ ;
: SPI2-CR2! ( n -- ) SPI2-CR2 L! ;
: SPI2-SR@ ( -- n ) SPI2-SR L@ ;
: SPI2-SR! ( n -- ) SPI2-SR L! ;
: SPI2-DR@ ( -- n ) SPI2-DR L@ ;
: SPI2-DR! ( n -- ) SPI2-DR L! ;

\ I2C1外设
: I2C1-CR1@ ( -- n ) I2C1-CR1 L@ ;
: I2C1-CR1! ( n -- ) I2C1-CR1 L! ;
: I2C1-CR2@ ( -- n ) I2C1-CR2 L@ ;
: I2C1-CR2! ( n -- ) I2C1-CR2 L! ;
: I2C1-SR1@ ( -- n ) I2C1-SR1 L@ ;
: I2C1-SR1! ( n -- ) I2C1-SR1 L! ;
: I2C1-SR2@ ( -- n ) I2C1-SR2 L@ ;
: I2C1-SR2! ( n -- ) I2C1-SR2 L! ;
: I2C1-DR@ ( -- n ) I2C1-DR L@ ;
: I2C1-DR! ( n -- ) I2C1-DR L! ;
: I2C1-CCR@ ( -- n ) I2C1-CCR L@ ;
: I2C1-CCR! ( n -- ) I2C1-CCR L! ;
: I2C1-TRISE@ ( -- n ) I2C1-TRISE L@ ;
: I2C1-TRISE! ( n -- ) I2C1-TRISE L! ;

\ I2C2外设
: I2C2-CR1@ ( -- n ) I2C2-CR1 L@ ;
: I2C2-CR1! ( n -- ) I2C2-CR1 L! ;
: I2C2-CR2@ ( -- n ) I2C2-CR2 L@ ;
: I2C2-CR2! ( n -- ) I2C2-CR2 L! ;
: I2C2-SR1@ ( -- n ) I2C2-SR1 L@ ;
: I2C2-SR1! ( n -- ) I2C2-SR1 L! ;
: I2C2-SR2@ ( -- n ) I2C2-SR2 L@ ;
: I2C2-SR2! ( n -- ) I2C2-SR2 L! ;
: I2C2-DR@ ( -- n ) I2C2-DR L@ ;
: I2C2-DR! ( n -- ) I2C2-DR L! ;
: I2C2-CCR@ ( -- n ) I2C2-CCR L@ ;
: I2C2-CCR! ( n -- ) I2C2-CCR L! ;

\ TIM1外设
: TIM1-CR1@ ( -- n ) TIM1-CR1 L@ ;
: TIM1-CR1! ( n -- ) TIM1-CR1 L! ;
: TIM1-CR2@ ( -- n ) TIM1-CR2 L@ ;
: TIM1-CR2! ( n -- ) TIM1-CR2 L! ;
: TIM1-SMCR@ ( -- n ) TIM1-SMCR L@ ;
: TIM1-SMCR! ( n -- ) TIM1-SMCR L! ;
: TIM1-DIER@ ( -- n ) TIM1-DIER L@ ;
: TIM1-DIER! ( n -- ) TIM1-DIER L! ;
: TIM1-SR@ ( -- n ) TIM1-SR L@ ;
: TIM1-SR! ( n -- ) TIM1-SR L! ;
: TIM1-EGR@ ( -- n ) TIM1-EGR L@ ;
: TIM1-EGR! ( n -- ) TIM1-EGR L! ;
: TIM1-CCMR1@ ( -- n ) TIM1-CCMR1 L@ ;
: TIM1-CCMR1! ( n -- ) TIM1-CCMR1 L! ;
: TIM1-CCMR2@ ( -- n ) TIM1-CCMR2 L@ ;
: TIM1-CCMR2! ( n -- ) TIM1-CCMR2 L! ;
: TIM1-CCER@ ( -- n ) TIM1-CCER L@ ;
: TIM1-CCER! ( n -- ) TIM1-CCER L! ;
: TIM1-CNT@ ( -- n ) TIM1-CNT L@ ;
: TIM1-CNT! ( n -- ) TIM1-CNT L! ;
: TIM1-PSC@ ( -- n ) TIM1-PSC L@ ;
: TIM1-PSC! ( n -- ) TIM1-PSC L! ;
: TIM1-ARR@ ( -- n ) TIM1-ARR L@ ;
: TIM1-ARR! ( n -- ) TIM1-ARR L! ;
: TIM1-RCR@ ( -- n ) TIM1-RCR L@ ;
: TIM1-RCR! ( n -- ) TIM1-RCR L! ;
: TIM1-CCR1@ ( -- n ) TIM1-CCR1 L@ ;
: TIM1-CCR1! ( n -- ) TIM1-CCR1 L! ;
: TIM1-CCR2@ ( -- n ) TIM1-CCR2 L@ ;
: TIM1-CCR2! ( n -- ) TIM1-CCR2 L! ;
: TIM1-CCR3@ ( -- n ) TIM1-CCR3 L@ ;
: TIM1-CCR3! ( n -- ) TIM1-CCR3 L! ;
: TIM1-CCR4@ ( -- n ) TIM1-CCR4 L@ ;
: TIM1-CCR4! ( n -- ) TIM1-CCR4 L! ;
: TIM1-BDTR@ ( -- n ) TIM1-BDTR L@ ;
: TIM1-BDTR! ( n -- ) TIM1-BDTR L! ;

\ TIM2外设
: TIM2-CR1@ ( -- n ) TIM2-CR1 L@ ;
: TIM2-CR1! ( n -- ) TIM2-CR1 L! ;
: TIM2-CNT@ ( -- n ) TIM2-CNT L@ ;
: TIM2-CNT! ( n -- ) TIM2-CNT L! ;
: TIM2-PSC@ ( -- n ) TIM2-PSC L@ ;
: TIM2-PSC! ( n -- ) TIM2-PSC L! ;
: TIM2-ARR@ ( -- n ) TIM2-ARR L@ ;
: TIM2-ARR! ( n -- ) TIM2-ARR L! ;
: TIM2-CCR1@ ( -- n ) TIM2-CCR1 L@ ;
: TIM2-CCR1! ( n -- ) TIM2-CCR1 L! ;
: TIM2-CCR2@ ( -- n ) TIM2-CCR2 L@ ;
: TIM2-CCR2! ( n -- ) TIM2-CCR2 L! ;
: TIM2-CCR3@ ( -- n ) TIM2-CCR3 L@ ;
: TIM2-CCR3! ( n -- ) TIM2-CCR3 L! ;
: TIM2-CCR4@ ( -- n ) TIM2-CCR4 L@ ;
: TIM2-CCR4! ( n -- ) TIM2-CCR4 L! ;

\ TIM3外设
: TIM3-CR1@ ( -- n ) TIM3-CR1 L@ ;
: TIM3-CR1! ( n -- ) TIM3-CR1 L! ;
: TIM3-CNT@ ( -- n ) TIM3-CNT L@ ;
: TIM3-CNT! ( n -- ) TIM3-CNT L! ;
: TIM3-ARR@ ( -- n ) TIM3-ARR L@ ;
: TIM3-ARR! ( n -- ) TIM3-ARR L! ;
: TIM3-CCR1@ ( -- n ) TIM3-CCR1 L@ ;
: TIM3-CCR1! ( n -- ) TIM3-CCR1 L! ;
: TIM3-CCR2@ ( -- n ) TIM3-CCR2 L@ ;
: TIM3-CCR2! ( n -- ) TIM3-CCR2 L! ;
: TIM3-CCR3@ ( -- n ) TIM3-CCR3 L@ ;
: TIM3-CCR3! ( n -- ) TIM3-CCR3 L! ;
: TIM3-CCR4@ ( -- n ) TIM3-CCR4 L@ ;
: TIM3-CCR4! ( n -- ) TIM3-CCR4 L! ;

\ TIM4外设
: TIM4-CR1@ ( -- n ) TIM4-CR1 L@ ;
: TIM4-CR1! ( n -- ) TIM4-CR1 L! ;
: TIM4-CNT@ ( -- n ) TIM4-CNT L@ ;
: TIM4-CNT! ( n -- ) TIM4-CNT L! ;
: TIM4-ARR@ ( -- n ) TIM4-ARR L@ ;
: TIM4-ARR! ( n -- ) TIM4-ARR L! ;
: TIM4-CCR1@ ( -- n ) TIM4-CCR1 L@ ;
: TIM4-CCR1! ( n -- ) TIM4-CCR1 L! ;
: TIM4-CCR2@ ( -- n ) TIM4-CCR2 L@ ;
: TIM4-CCR2! ( n -- ) TIM4-CCR2 L! ;
: TIM4-CCR3@ ( -- n ) TIM4-CCR3 L@ ;
: TIM4-CCR3! ( n -- ) TIM4-CCR3 L! ;
: TIM4-CCR4@ ( -- n ) TIM4-CCR4 L@ ;
: TIM4-CCR4! ( n -- ) TIM4-CCR4 L! ;

\ ADC1外设
: ADC1-SR@ ( -- n ) ADC1-SR L@ ;
: ADC1-SR! ( n -- ) ADC1-SR L! ;
: ADC1-CR1@ ( -- n ) ADC1-CR1 L@ ;
: ADC1-CR1! ( n -- ) ADC1-CR1 L! ;
: ADC1-CR2@ ( -- n ) ADC1-CR2 L@ ;
: ADC1-CR2! ( n -- ) ADC1-CR2 L! ;
: ADC1-SMPR1@ ( -- n ) ADC1-SMPR1 L@ ;
: ADC1-SMPR1! ( n -- ) ADC1-SMPR1 L! ;
: ADC1-SMPR2@ ( -- n ) ADC1-SMPR2 L@ ;
: ADC1-SMPR2! ( n -- ) ADC1-SMPR2 L! ;
: ADC1-JOFR1@ ( -- n ) ADC1-JOFR1 L@ ;
: ADC1-JOFR1! ( n -- ) ADC1-JOFR1 L! ;
: ADC1-JOFR2@ ( -- n ) ADC1-JOFR2 L@ ;
: ADC1-JOFR2! ( n -- ) ADC1-JOFR2 L! ;
: ADC1-JOFR3@ ( -- n ) ADC1-JOFR3 L@ ;
: ADC1-JOFR3! ( n -- ) ADC1-JOFR3 L! ;
: ADC1-JOFR4@ ( -- n ) ADC1-JOFR4 L@ ;
: ADC1-JOFR4! ( n -- ) ADC1-JOFR4 L! ;
: ADC1-HTR@ ( -- n ) ADC1-HTR L@ ;
: ADC1-HTR! ( n -- ) ADC1-HTR L! ;
: ADC1-LTR@ ( -- n ) ADC1-LTR L@ ;
: ADC1-LTR! ( n -- ) ADC1-LTR L! ;
: ADC1-SQRT1@ ( -- n ) ADC1-SQRT1 L@ ;
: ADC1-SQRT1! ( n -- ) ADC1-SQRT1 L! ;
: ADC1-SQRT2@ ( -- n ) ADC1-SQRT2 L@ ;
: ADC1-SQRT2! ( n -- ) ADC1-SQRT2 L! ;
: ADC1-SQRT3@ ( -- n ) ADC1-SQRT3 L@ ;
: ADC1-SQRT3! ( n -- ) ADC1-SQRT3 L! ;
: ADC1-JSQR@ ( -- n ) ADC1-JSQR L@ ;
: ADC1-JSQR! ( n -- ) ADC1-JSQR L! ;
: ADC1-JDR1@ ( -- n ) ADC1-JDR1 L@ ;
: ADC1-JDR1! ( n -- ) ADC1-JDR1 L! ;
: ADC1-JDR2@ ( -- n ) ADC1-JDR2 L@ ;
: ADC1-JDR2! ( n -- ) ADC1-JDR2 L! ;
: ADC1-JDR3@ ( -- n ) ADC1-JDR3 L@ ;
: ADC1-JDR3! ( n -- ) ADC1-JDR3 L! ;
: ADC1-JDR4@ ( -- n ) ADC1-JDR4 L@ ;
: ADC1-JDR4! ( n -- ) ADC1-JDR4 L! ;
: ADC1-DR@ ( -- n ) ADC1-DR L@ ;
: ADC1-DR! ( n -- ) ADC1-DR L! ;

\ DMA1外设
: DMA1-ISR@ ( -- n ) DMA1-ISR L@ ;
: DMA1-ISR! ( n -- ) DMA1-ISR L! ;
: DMA1-IFCR@ ( -- n ) DMA1-IFCR L@ ;
: DMA1-IFCR! ( n -- ) DMA1-IFCR L! ;
: DMA1-CCR1@ ( -- n ) DMA1-CCR1 L@ ;
: DMA1-CCR1! ( n -- ) DMA1-CCR1 L! ;
: DMA1-CNDTR1@ ( -- n ) DMA1-CNDTR1 L@ ;
: DMA1-CNDTR1! ( n -- ) DMA1-CNDTR1 L! ;
: DMA1-CPAR1@ ( -- n ) DMA1-CPAR1 L@ ;
: DMA1-CPAR1! ( n -- ) DMA1-CPAR1 L! ;
: DMA1-CMAR1@ ( -- n ) DMA1-CMAR1 L@ ;
: DMA1-CMAR1! ( n -- ) DMA1-CMAR1 L! ;
: DMA1-CCR2@ ( -- n ) DMA1-CCR2 L@ ;
: DMA1-CCR2! ( n -- ) DMA1-CCR2 L! ;
: DMA1-CNDTR2@ ( -- n ) DMA1-CNDTR2 L@ ;
: DMA1-CNDTR2! ( n -- ) DMA1-CNDTR2 L! ;
: DMA1-CPAR2@ ( -- n ) DMA1-CPAR2 L@ ;
: DMA1-CPAR2! ( n -- ) DMA1-CPAR2 L! ;
: DMA1-CMAR2@ ( -- n ) DMA1-CMAR2 L@ ;
: DMA1-CMAR2! ( n -- ) DMA1-CMAR2 L! ;

\ PWR外设
: PWR-CR@ ( -- n ) PWR-CR L@ ;
: PWR-CR! ( n -- ) PWR-CR L! ;
: PWR-CSR@ ( -- n ) PWR-CSR L@ ;
: PWR-CSR! ( n -- ) PWR-CSR L! ;

\ BKP外设
: BKP-DR1@ ( -- n ) BKP-DR1 L@ ;
: BKP-DR1! ( n -- ) BKP-DR1 L! ;
: BKP-DR2@ ( -- n ) BKP-DR2 L@ ;
: BKP-DR2! ( n -- ) BKP-DR2 L! ;
: BKP-CSR@ ( -- n ) BKP-CSR L@ ;
: BKP-CSR! ( n -- ) BKP-CSR L! ;

\ WWDG外设
: WWDG-CR@ ( -- n ) WWDG-CR L@ ;
: WWDG-CR! ( n -- ) WWDG-CR L! ;
: WWDG-CFR@ ( -- n ) WWDG-CFR L@ ;
: WWDG-CFR! ( n -- ) WWDG-CFR L! ;
: WWDG-SR@ ( -- n ) WWDG-SR L@ ;
: WWDG-SR! ( n -- ) WWDG-SR L! ;

\ IWDG外设
: IWDG-KR@ ( -- n ) IWDG-KR L@ ;
: IWDG-KR! ( n -- ) IWDG-KR L! ;
: IWDG-PR@ ( -- n ) IWDG-PR L@ ;
: IWDG-PR! ( n -- ) IWDG-PR L! ;
: IWDG-RLR@ ( -- n ) IWDG-RLR L@ ;
: IWDG-RLR! ( n -- ) IWDG-RLR L! ;

\ EXTI外设
: EXTI-IMR@ ( -- n ) EXTI-IMR L@ ;
: EXTI-IMR! ( n -- ) EXTI-IMR L! ;
: EXTI-EMR@ ( -- n ) EXTI-EMR L@ ;
: EXTI-EMR! ( n -- ) EXTI-EMR L! ;
: EXTI-RTSR@ ( -- n ) EXTI-RTSR L@ ;
: EXTI-RTSR! ( n -- ) EXTI-RTSR L! ;
: EXTI-FTSR@ ( -- n ) EXTI-FTSR L@ ;
: EXTI-FTSR! ( n -- ) EXTI-FTSR L! ;
: EXTI-SWIER@ ( -- n ) EXTI-SWIER L@ ;
: EXTI-SWIER! ( n -- ) EXTI-SWIER L! ;
: EXTI-PR@ ( -- n ) EXTI-PR L@ ;
: EXTI-PR! ( n -- ) EXTI-PR L! ;

\ AFIO外设
: AFIO-EVCR@ ( -- n ) AFIO-EVCR L@ ;
: AFIO-EVCR! ( n -- ) AFIO-EVCR L! ;
: AFIO-MAPR@ ( -- n ) AFIO-MAPR L@ ;
: AFIO-MAPR! ( n -- ) AFIO-MAPR L! ;
: AFIO-EXTICR1@ ( -- n ) AFIO-EXTICR1 L@ ;
: AFIO-EXTICR1! ( n -- ) AFIO-EXTICR1 L! ;
: AFIO-EXTICR2@ ( -- n ) AFIO-EXTICR2 L@ ;
: AFIO-EXTICR2! ( n -- ) AFIO-EXTICR2 L! ;
: AFIO-EXTICR3@ ( -- n ) AFIO-EXTICR3 L@ ;
: AFIO-EXTICR3! ( n -- ) AFIO-EXTICR3 L! ;
: AFIO-MAPR2@ ( -- n ) AFIO-MAPR2 L@ ;
: AFIO-MAPR2! ( n -- ) AFIO-MAPR2 L! ;

\ =========================================
\ 设备初始化
\ =========================================

: STM32F103C8T6-INIT ( -- )
  \ 初始化STM32F103C8T6设备
  ." 初始化STM32F103C8T6..." CR

  \ 初始化寄存器
  0 R0!  \ General Purpose Register 0
  0 R1!  \ General Purpose Register 1
  0 R2!  \ General Purpose Register 2
  0 R3!  \ General Purpose Register 3
  0 R4!  \ General Purpose Register 4
  0 R5!  \ General Purpose Register 5
  0 R6!  \ General Purpose Register 6
  0 R7!  \ General Purpose Register 7
  0 R8!  \ General Purpose Register 8
  0 R9!  \ General Purpose Register 9
  0 R10!  \ General Purpose Register 10
  0 R11!  \ General Purpose Register 11
  0 R12!  \ General Purpose Register 12
  0 SP!  \ Stack Pointer
  0 LR!  \ Link Register
  0 PC!  \ Program Counter
  0 XPSR!  \ Program Status Register

  \ 初始化外设
  \ 初始化RCC
  0 RCC-CR!  \ CR寄存器
  0 RCC-CFGR!  \ CFGR寄存器
  0 RCC-APB2ENR!  \ APB2ENR寄存器
  0 RCC-APB1ENR!  \ APB1ENR寄存器
  \ 初始化GPIOA
  0 GPIOA-CRL!  \ CRL寄存器
  0 GPIOA-CRH!  \ CRH寄存器
  0 GPIOA-IDR!  \ IDR寄存器
  0 GPIOA-ODR!  \ ODR寄存器
  0 GPIOA-BSRR!  \ BSRR寄存器
  0 GPIOA-BRR!  \ BRR寄存器
  0 GPIOA-LCKR!  \ LCKR寄存器
  \ 初始化GPIOB
  0 GPIOB-CRL!  \ CRL寄存器
  0 GPIOB-CRH!  \ CRH寄存器
  0 GPIOB-IDR!  \ IDR寄存器
  0 GPIOB-ODR!  \ ODR寄存器
  0 GPIOB-BSRR!  \ BSRR寄存器
  0 GPIOB-BRR!  \ BRR寄存器
  \ 初始化GPIOC
  0 GPIOC-CRL!  \ CRL寄存器
  0 GPIOC-CRH!  \ CRH寄存器
  0 GPIOC-IDR!  \ IDR寄存器
  0 GPIOC-ODR!  \ ODR寄存器
  0 GPIOC-BSRR!  \ BSRR寄存器
  \ 初始化USART1
  0 USART1-SR!  \ SR寄存器
  0 USART1-DR!  \ DR寄存器
  0 USART1-BRR!  \ BRR寄存器
  0 USART1-CR1!  \ CR1寄存器
  0 USART1-CR2!  \ CR2寄存器
  0 USART1-CR3!  \ CR3寄存器
  0 USART1-GTPR!  \ GTPR寄存器
  \ 初始化USART2
  0 USART2-SR!  \ SR寄存器
  0 USART2-DR!  \ DR寄存器
  0 USART2-BRR!  \ BRR寄存器
  0 USART2-CR1!  \ CR1寄存器
  \ 初始化SPI1
  0 SPI1-CR1!  \ CR1寄存器
  0 SPI1-CR2!  \ CR2寄存器
  0 SPI1-SR!  \ SR寄存器
  0 SPI1-DR!  \ DR寄存器
  \ 初始化SPI2
  0 SPI2-CR1!  \ CR1寄存器
  0 SPI2-CR2!  \ CR2寄存器
  0 SPI2-SR!  \ SR寄存器
  0 SPI2-DR!  \ DR寄存器
  \ 初始化I2C1
  0 I2C1-CR1!  \ CR1寄存器
  0 I2C1-CR2!  \ CR2寄存器
  0 I2C1-SR1!  \ SR1寄存器
  0 I2C1-SR2!  \ SR2寄存器
  0 I2C1-DR!  \ DR寄存器
  0 I2C1-CCR!  \ CCR寄存器
  0 I2C1-TRISE!  \ TRISE寄存器
  \ 初始化I2C2
  0 I2C2-CR1!  \ CR1寄存器
  0 I2C2-CR2!  \ CR2寄存器
  0 I2C2-SR1!  \ SR1寄存器
  0 I2C2-SR2!  \ SR2寄存器
  0 I2C2-DR!  \ DR寄存器
  0 I2C2-CCR!  \ CCR寄存器
  \ 初始化TIM1
  0 TIM1-CR1!  \ CR1寄存器
  0 TIM1-CR2!  \ CR2寄存器
  0 TIM1-SMCR!  \ SMCR寄存器
  0 TIM1-DIER!  \ DIER寄存器
  0 TIM1-SR!  \ SR寄存器
  0 TIM1-EGR!  \ EGR寄存器
  0 TIM1-CCMR1!  \ CCMR1寄存器
  0 TIM1-CCMR2!  \ CCMR2寄存器
  0 TIM1-CCER!  \ CCER寄存器
  0 TIM1-CNT!  \ CNT寄存器
  0 TIM1-PSC!  \ PSC寄存器
  0 TIM1-ARR!  \ ARR寄存器
  0 TIM1-RCR!  \ RCR寄存器
  0 TIM1-CCR1!  \ CCR1寄存器
  0 TIM1-CCR2!  \ CCR2寄存器
  0 TIM1-CCR3!  \ CCR3寄存器
  0 TIM1-CCR4!  \ CCR4寄存器
  0 TIM1-BDTR!  \ BDTR寄存器
  \ 初始化TIM2
  0 TIM2-CR1!  \ CR1寄存器
  0 TIM2-CNT!  \ CNT寄存器
  0 TIM2-PSC!  \ PSC寄存器
  0 TIM2-ARR!  \ ARR寄存器
  0 TIM2-CCR1!  \ CCR1寄存器
  0 TIM2-CCR2!  \ CCR2寄存器
  0 TIM2-CCR3!  \ CCR3寄存器
  0 TIM2-CCR4!  \ CCR4寄存器
  \ 初始化TIM3
  0 TIM3-CR1!  \ CR1寄存器
  0 TIM3-CNT!  \ CNT寄存器
  0 TIM3-ARR!  \ ARR寄存器
  0 TIM3-CCR1!  \ CCR1寄存器
  0 TIM3-CCR2!  \ CCR2寄存器
  0 TIM3-CCR3!  \ CCR3寄存器
  0 TIM3-CCR4!  \ CCR4寄存器
  \ 初始化TIM4
  0 TIM4-CR1!  \ CR1寄存器
  0 TIM4-CNT!  \ CNT寄存器
  0 TIM4-ARR!  \ ARR寄存器
  0 TIM4-CCR1!  \ CCR1寄存器
  0 TIM4-CCR2!  \ CCR2寄存器
  0 TIM4-CCR3!  \ CCR3寄存器
  0 TIM4-CCR4!  \ CCR4寄存器
  \ 初始化ADC1
  0 ADC1-SR!  \ SR寄存器
  0 ADC1-CR1!  \ CR1寄存器
  0 ADC1-CR2!  \ CR2寄存器
  0 ADC1-SMPR1!  \ SMPR1寄存器
  0 ADC1-SMPR2!  \ SMPR2寄存器
  0 ADC1-JOFR1!  \ JOFR1寄存器
  0 ADC1-JOFR2!  \ JOFR2寄存器
  0 ADC1-JOFR3!  \ JOFR3寄存器
  0 ADC1-JOFR4!  \ JOFR4寄存器
  0 ADC1-HTR!  \ HTR寄存器
  0 ADC1-LTR!  \ LTR寄存器
  0 ADC1-SQRT1!  \ SQRT1寄存器
  0 ADC1-SQRT2!  \ SQRT2寄存器
  0 ADC1-SQRT3!  \ SQRT3寄存器
  0 ADC1-JSQR!  \ JSQR寄存器
  0 ADC1-JDR1!  \ JDR1寄存器
  0 ADC1-JDR2!  \ JDR2寄存器
  0 ADC1-JDR3!  \ JDR3寄存器
  0 ADC1-JDR4!  \ JDR4寄存器
  0 ADC1-DR!  \ DR寄存器
  \ 初始化DMA1
  0 DMA1-ISR!  \ ISR寄存器
  0 DMA1-IFCR!  \ IFCR寄存器
  0 DMA1-CCR1!  \ CCR1寄存器
  0 DMA1-CNDTR1!  \ CNDTR1寄存器
  0 DMA1-CPAR1!  \ CPAR1寄存器
  0 DMA1-CMAR1!  \ CMAR1寄存器
  0 DMA1-CCR2!  \ CCR2寄存器
  0 DMA1-CNDTR2!  \ CNDTR2寄存器
  0 DMA1-CPAR2!  \ CPAR2寄存器
  0 DMA1-CMAR2!  \ CMAR2寄存器
  \ 初始化PWR
  0 PWR-CR!  \ CR寄存器
  0 PWR-CSR!  \ CSR寄存器
  \ 初始化BKP
  0 BKP-DR1!  \ DR1寄存器
  0 BKP-DR2!  \ DR2寄存器
  0 BKP-CSR!  \ CSR寄存器
  \ 初始化WWDG
  0 WWDG-CR!  \ CR寄存器
  0 WWDG-CFR!  \ CFR寄存器
  0 WWDG-SR!  \ SR寄存器
  \ 初始化IWDG
  0 IWDG-KR!  \ KR寄存器
  0 IWDG-PR!  \ PR寄存器
  0 IWDG-RLR!  \ RLR寄存器
  \ 初始化EXTI
  0 EXTI-IMR!  \ IMR寄存器
  0 EXTI-EMR!  \ EMR寄存器
  0 EXTI-RTSR!  \ RTSR寄存器
  0 EXTI-FTSR!  \ FTSR寄存器
  0 EXTI-SWIER!  \ SWIER寄存器
  0 EXTI-PR!  \ PR寄存器
  \ 初始化AFIO
  0 AFIO-EVCR!  \ EVCR寄存器
  0 AFIO-MAPR!  \ MAPR寄存器
  0 AFIO-EXTICR1!  \ EXTICR1寄存器
  0 AFIO-EXTICR2!  \ EXTICR2寄存器
  0 AFIO-EXTICR3!  \ EXTICR3寄存器
  0 AFIO-MAPR2!  \ MAPR2寄存器

  ." STM32F103C8T6初始化完成" CR
;

\ =========================================
\ 设备信息显示
\ =========================================

: .DEVICE-INFO ( -- )
  CR
  ." 设备: " DEVICE-NAME TYPE CR
  ." 厂商: " MANUFACTURER TYPE CR
  ." 系列: " FAMILY TYPE CR
  ." 版本: " VERSION TYPE CR
  ." 架构: " ARCHITECTURE TYPE CR
  ." 位宽: " BITS . CR
  ." 时钟: " CLOCK-FREQ . ." Hz" CR
;

: .REGISTERS ( -- )
  CR ." 寄存器状态:" CR
  ." ----------" CR
  R0@ R0 .R 8 .R SPACE ."  R0: " R0@ .
  R1@ R1 .R 8 .R SPACE ."  R1: " R1@ .
  R2@ R2 .R 8 .R SPACE ."  R2: " R2@ .
  R3@ R3 .R 8 .R SPACE ."  R3: " R3@ .
  R4@ R4 .R 8 .R SPACE ."  R4: " R4@ .
  R5@ R5 .R 8 .R SPACE ."  R5: " R5@ .
  R6@ R6 .R 8 .R SPACE ."  R6: " R6@ .
  R7@ R7 .R 8 .R SPACE ."  R7: " R7@ .
  R8@ R8 .R 8 .R SPACE ."  R8: " R8@ .
  R9@ R9 .R 8 .R SPACE ."  R9: " R9@ .
  R10@ R10 .R 8 .R SPACE ."  R10: " R10@ .
  R11@ R11 .R 8 .R SPACE ."  R11: " R11@ .
  R12@ R12 .R 8 .R SPACE ."  R12: " R12@ .
  SP@ SP .R 8 .R SPACE ."  SP: " SP@ .
  LR@ LR .R 8 .R SPACE ."  LR: " LR@ .
  PC@ PC .R 8 .R SPACE ."  PC: " PC@ .
  XPSR@ XPSR .R 8 .R SPACE ."  xPSR: " XPSR@ .
;

\ =========================================
\ 引脚操作
\ =========================================

\ =========================================
\ 中断处理
\ =========================================

\ Window Watchdog Interrupt
: INT-WWDG-HANDLER ( -- )
  ." WWDG中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-WWDG-ENABLE ( -- )
  INT-WWDG INT-ENABLE
;

: INT-WWDG-DISABLE ( -- )
  INT-WWDG INT-DISABLE
;

\ PVD through EXTI Line detection
: INT-PVD-HANDLER ( -- )
  ." PVD中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-PVD-ENABLE ( -- )
  INT-PVD INT-ENABLE
;

: INT-PVD-DISABLE ( -- )
  INT-PVD INT-DISABLE
;

\ Tamper Interrupt
: INT-TAMPER-HANDLER ( -- )
  ." TAMPER中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TAMPER-ENABLE ( -- )
  INT-TAMPER INT-ENABLE
;

: INT-TAMPER-DISABLE ( -- )
  INT-TAMPER INT-DISABLE
;

\ RTC Global Interrupt
: INT-RTC-HANDLER ( -- )
  ." RTC中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RTC-ENABLE ( -- )
  INT-RTC INT-ENABLE
;

: INT-RTC-DISABLE ( -- )
  INT-RTC INT-DISABLE
;

\ FLASH Global Interrupt
: INT-FLASH-HANDLER ( -- )
  ." FLASH中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-FLASH-ENABLE ( -- )
  INT-FLASH INT-ENABLE
;

: INT-FLASH-DISABLE ( -- )
  INT-FLASH INT-DISABLE
;

\ RCC Global Interrupt
: INT-RCC-HANDLER ( -- )
  ." RCC中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RCC-ENABLE ( -- )
  INT-RCC INT-ENABLE
;

: INT-RCC-DISABLE ( -- )
  INT-RCC INT-DISABLE
;

\ EXTI Line 0 Interrupt
: INT-EXTI0-HANDLER ( -- )
  ." EXTI0中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-EXTI0-ENABLE ( -- )
  INT-EXTI0 INT-ENABLE
;

: INT-EXTI0-DISABLE ( -- )
  INT-EXTI0 INT-DISABLE
;

\ EXTI Line 1 Interrupt
: INT-EXTI1-HANDLER ( -- )
  ." EXTI1中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-EXTI1-ENABLE ( -- )
  INT-EXTI1 INT-ENABLE
;

: INT-EXTI1-DISABLE ( -- )
  INT-EXTI1 INT-DISABLE
;

\ EXTI Line 2 Interrupt
: INT-EXTI2-HANDLER ( -- )
  ." EXTI2中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-EXTI2-ENABLE ( -- )
  INT-EXTI2 INT-ENABLE
;

: INT-EXTI2-DISABLE ( -- )
  INT-EXTI2 INT-DISABLE
;

\ EXTI Line 3 Interrupt
: INT-EXTI3-HANDLER ( -- )
  ." EXTI3中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-EXTI3-ENABLE ( -- )
  INT-EXTI3 INT-ENABLE
;

: INT-EXTI3-DISABLE ( -- )
  INT-EXTI3 INT-DISABLE
;

\ EXTI Line 4 Interrupt
: INT-EXTI4-HANDLER ( -- )
  ." EXTI4中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-EXTI4-ENABLE ( -- )
  INT-EXTI4 INT-ENABLE
;

: INT-EXTI4-DISABLE ( -- )
  INT-EXTI4 INT-DISABLE
;

\ DMA1 Channel 1 Interrupt
: INT-DMA1_CHANNEL1-HANDLER ( -- )
  ." DMA1_Channel1中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-DMA1_CHANNEL1-ENABLE ( -- )
  INT-DMA1_CHANNEL1 INT-ENABLE
;

: INT-DMA1_CHANNEL1-DISABLE ( -- )
  INT-DMA1_CHANNEL1 INT-DISABLE
;

\ DMA1 Channel 2 Interrupt
: INT-DMA1_CHANNEL2-HANDLER ( -- )
  ." DMA1_Channel2中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-DMA1_CHANNEL2-ENABLE ( -- )
  INT-DMA1_CHANNEL2 INT-ENABLE
;

: INT-DMA1_CHANNEL2-DISABLE ( -- )
  INT-DMA1_CHANNEL2 INT-DISABLE
;

\ DMA1 Channel 3 Interrupt
: INT-DMA1_CHANNEL3-HANDLER ( -- )
  ." DMA1_Channel3中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-DMA1_CHANNEL3-ENABLE ( -- )
  INT-DMA1_CHANNEL3 INT-ENABLE
;

: INT-DMA1_CHANNEL3-DISABLE ( -- )
  INT-DMA1_CHANNEL3 INT-DISABLE
;

\ DMA1 Channel 4 Interrupt
: INT-DMA1_CHANNEL4-HANDLER ( -- )
  ." DMA1_Channel4中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-DMA1_CHANNEL4-ENABLE ( -- )
  INT-DMA1_CHANNEL4 INT-ENABLE
;

: INT-DMA1_CHANNEL4-DISABLE ( -- )
  INT-DMA1_CHANNEL4 INT-DISABLE
;

\ DMA1 Channel 5 Interrupt
: INT-DMA1_CHANNEL5-HANDLER ( -- )
  ." DMA1_Channel5中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-DMA1_CHANNEL5-ENABLE ( -- )
  INT-DMA1_CHANNEL5 INT-ENABLE
;

: INT-DMA1_CHANNEL5-DISABLE ( -- )
  INT-DMA1_CHANNEL5 INT-DISABLE
;

\ DMA1 Channel 6 Interrupt
: INT-DMA1_CHANNEL6-HANDLER ( -- )
  ." DMA1_Channel6中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-DMA1_CHANNEL6-ENABLE ( -- )
  INT-DMA1_CHANNEL6 INT-ENABLE
;

: INT-DMA1_CHANNEL6-DISABLE ( -- )
  INT-DMA1_CHANNEL6 INT-DISABLE
;

\ DMA1 Channel 7 Interrupt
: INT-DMA1_CHANNEL7-HANDLER ( -- )
  ." DMA1_Channel7中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-DMA1_CHANNEL7-ENABLE ( -- )
  INT-DMA1_CHANNEL7 INT-ENABLE
;

: INT-DMA1_CHANNEL7-DISABLE ( -- )
  INT-DMA1_CHANNEL7 INT-DISABLE
;

\ ADC1 and ADC2 Global Interrupt
: INT-ADC1_2-HANDLER ( -- )
  ." ADC1_2中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-ADC1_2-ENABLE ( -- )
  INT-ADC1_2 INT-ENABLE
;

: INT-ADC1_2-DISABLE ( -- )
  INT-ADC1_2 INT-DISABLE
;

\ USB HP/CAN TX Interrupts
: INT-USB_HP_CAN_TX-HANDLER ( -- )
  ." USB_HP_CAN_TX中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-USB_HP_CAN_TX-ENABLE ( -- )
  INT-USB_HP_CAN_TX INT-ENABLE
;

: INT-USB_HP_CAN_TX-DISABLE ( -- )
  INT-USB_HP_CAN_TX INT-DISABLE
;

\ USB LP/CAN RX0 Interrupt
: INT-USB_LP_CAN_RX0-HANDLER ( -- )
  ." USB_LP_CAN_RX0中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-USB_LP_CAN_RX0-ENABLE ( -- )
  INT-USB_LP_CAN_RX0 INT-ENABLE
;

: INT-USB_LP_CAN_RX0-DISABLE ( -- )
  INT-USB_LP_CAN_RX0 INT-DISABLE
;

\ CAN RX1 Interrupt
: INT-CAN_RX1-HANDLER ( -- )
  ." CAN_RX1中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-CAN_RX1-ENABLE ( -- )
  INT-CAN_RX1 INT-ENABLE
;

: INT-CAN_RX1-DISABLE ( -- )
  INT-CAN_RX1 INT-DISABLE
;

\ CAN SCE Interrupt
: INT-CAN_SCE-HANDLER ( -- )
  ." CAN_SCE中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-CAN_SCE-ENABLE ( -- )
  INT-CAN_SCE INT-ENABLE
;

: INT-CAN_SCE-DISABLE ( -- )
  INT-CAN_SCE INT-DISABLE
;

\ EXTI Line 9..5 Interrupt
: INT-EXTI9_5-HANDLER ( -- )
  ." EXTI9_5中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-EXTI9_5-ENABLE ( -- )
  INT-EXTI9_5 INT-ENABLE
;

: INT-EXTI9_5-DISABLE ( -- )
  INT-EXTI9_5 INT-DISABLE
;

\ TIM1 Break Interrupt
: INT-TIM1_BRK-HANDLER ( -- )
  ." TIM1_BRK中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TIM1_BRK-ENABLE ( -- )
  INT-TIM1_BRK INT-ENABLE
;

: INT-TIM1_BRK-DISABLE ( -- )
  INT-TIM1_BRK INT-DISABLE
;

\ TIM1 Update Interrupt
: INT-TIM1_UP-HANDLER ( -- )
  ." TIM1_UP中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TIM1_UP-ENABLE ( -- )
  INT-TIM1_UP INT-ENABLE
;

: INT-TIM1_UP-DISABLE ( -- )
  INT-TIM1_UP INT-DISABLE
;

\ TIM1 Trigger and Commutation
: INT-TIM1_TRG_COM-HANDLER ( -- )
  ." TIM1_TRG_COM中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TIM1_TRG_COM-ENABLE ( -- )
  INT-TIM1_TRG_COM INT-ENABLE
;

: INT-TIM1_TRG_COM-DISABLE ( -- )
  INT-TIM1_TRG_COM INT-DISABLE
;

\ TIM1 Capture Compare Interrupt
: INT-TIM1_CC-HANDLER ( -- )
  ." TIM1_CC中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TIM1_CC-ENABLE ( -- )
  INT-TIM1_CC INT-ENABLE
;

: INT-TIM1_CC-DISABLE ( -- )
  INT-TIM1_CC INT-DISABLE
;

\ TIM2 Global Interrupt
: INT-TIM2-HANDLER ( -- )
  ." TIM2中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TIM2-ENABLE ( -- )
  INT-TIM2 INT-ENABLE
;

: INT-TIM2-DISABLE ( -- )
  INT-TIM2 INT-DISABLE
;

\ TIM3 Global Interrupt
: INT-TIM3-HANDLER ( -- )
  ." TIM3中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TIM3-ENABLE ( -- )
  INT-TIM3 INT-ENABLE
;

: INT-TIM3-DISABLE ( -- )
  INT-TIM3 INT-DISABLE
;

\ TIM4 Global Interrupt
: INT-TIM4-HANDLER ( -- )
  ." TIM4中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TIM4-ENABLE ( -- )
  INT-TIM4 INT-ENABLE
;

: INT-TIM4-DISABLE ( -- )
  INT-TIM4 INT-DISABLE
;

\ I2C1 Event Interrupt
: INT-I2C1_EV-HANDLER ( -- )
  ." I2C1_EV中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-I2C1_EV-ENABLE ( -- )
  INT-I2C1_EV INT-ENABLE
;

: INT-I2C1_EV-DISABLE ( -- )
  INT-I2C1_EV INT-DISABLE
;

\ I2C1 Error Interrupt
: INT-I2C1_ER-HANDLER ( -- )
  ." I2C1_ER中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-I2C1_ER-ENABLE ( -- )
  INT-I2C1_ER INT-ENABLE
;

: INT-I2C1_ER-DISABLE ( -- )
  INT-I2C1_ER INT-DISABLE
;

\ I2C2 Event Interrupt
: INT-I2C2_EV-HANDLER ( -- )
  ." I2C2_EV中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-I2C2_EV-ENABLE ( -- )
  INT-I2C2_EV INT-ENABLE
;

: INT-I2C2_EV-DISABLE ( -- )
  INT-I2C2_EV INT-DISABLE
;

\ I2C2 Error Interrupt
: INT-I2C2_ER-HANDLER ( -- )
  ." I2C2_ER中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-I2C2_ER-ENABLE ( -- )
  INT-I2C2_ER INT-ENABLE
;

: INT-I2C2_ER-DISABLE ( -- )
  INT-I2C2_ER INT-DISABLE
;

\ SPI1 Global Interrupt
: INT-SPI1-HANDLER ( -- )
  ." SPI1中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SPI1-ENABLE ( -- )
  INT-SPI1 INT-ENABLE
;

: INT-SPI1-DISABLE ( -- )
  INT-SPI1 INT-DISABLE
;

\ SPI2 Global Interrupt
: INT-SPI2-HANDLER ( -- )
  ." SPI2中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SPI2-ENABLE ( -- )
  INT-SPI2 INT-ENABLE
;

: INT-SPI2-DISABLE ( -- )
  INT-SPI2 INT-DISABLE
;

\ USART1 Global Interrupt
: INT-USART1-HANDLER ( -- )
  ." USART1中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-USART1-ENABLE ( -- )
  INT-USART1 INT-ENABLE
;

: INT-USART1-DISABLE ( -- )
  INT-USART1 INT-DISABLE
;

\ USART2 Global Interrupt
: INT-USART2-HANDLER ( -- )
  ." USART2中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-USART2-ENABLE ( -- )
  INT-USART2 INT-ENABLE
;

: INT-USART2-DISABLE ( -- )
  INT-USART2 INT-DISABLE
;

\ USART3 Global Interrupt
: INT-USART3-HANDLER ( -- )
  ." USART3中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-USART3-ENABLE ( -- )
  INT-USART3 INT-ENABLE
;

: INT-USART3-DISABLE ( -- )
  INT-USART3 INT-DISABLE
;

\ EXTI Line 15..10 Interrupt
: INT-EXTI15_10-HANDLER ( -- )
  ." EXTI15_10中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-EXTI15_10-ENABLE ( -- )
  INT-EXTI15_10 INT-ENABLE
;

: INT-EXTI15_10-DISABLE ( -- )
  INT-EXTI15_10 INT-DISABLE
;

\ RTC Alarm through EXTI
: INT-RTCALARM-HANDLER ( -- )
  ." RTCAlarm中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RTCALARM-ENABLE ( -- )
  INT-RTCALARM INT-ENABLE
;

: INT-RTCALARM-DISABLE ( -- )
  INT-RTCALARM INT-DISABLE
;

\ USB Wakeup from suspend
: INT-USBWAKEUP-HANDLER ( -- )
  ." USBWakeup中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-USBWAKEUP-ENABLE ( -- )
  INT-USBWAKEUP INT-ENABLE
;

: INT-USBWAKEUP-DISABLE ( -- )
  INT-USBWAKEUP INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  STM32F103C8T6-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
