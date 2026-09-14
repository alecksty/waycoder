package stm32f407vgt6

/**
 * 设备寄存器定义
 * 设备: STM32F407VGT6
 * 生成自: STMicroelectronics/STM32F4/STM32F407VGT6
 * 版本: 1.0
 * 日期: 2026-04-16
 * 作者: VML Team
 * 描述: High-performance ARM Cortex-M4 with FPU, 168MHz, 1MB Flash, 192KB SRAM
 */

// CPU架构: ARM-Cortex-M4
// 位宽: 32位
// 时钟频率: 16000000 Hz

import kotlinx.cinterop.*

// 寄存器定义






















































// 内存段定义
// 外设定义
// RCC: Reset and Clock Control

// FLASH: Flash Interface

// PWR: Power Control

// DMA1: DMA1 Controller

// DMA2: DMA2 Controller

// USART1: USART1

// USART2: USART2

// USART3: USART3

// SPI1: SPI1

// SPI2: SPI2

// SPI3: SPI3

// I2C1: I2C1

// I2C2: I2C2

// I2C3: I2C3

// TIM1: Advanced Timer 1

// TIM2: General Purpose Timer 2

// TIM3: General Purpose Timer 3

// TIM4: General Purpose Timer 4

// TIM5: General Purpose Timer 5

// TIM9: General Purpose Timer 9

// TIM10: General Purpose Timer 10

// ADC1: ADC1

// ADC2: ADC2

// ADC3: ADC3

// SYSCFG: System Configuration Controller

// EXTI: External Interrupt/Event Controller

// RNG: Random Number Generator

// CRYP: CRYP Accelerator

// HASH: HASH Accelerator

// DCMI: Digital Camera Interface

// USB_OTG_HS: USB OTG High Speed

// ETH: Ethernet

// 中断向量定义
enum class Irq(val vector: Int) {
    WWDG(0),
    // Window WatchDog interrupt
    PVD(1),
    // PVD through EXTI line detection interrupt
    TAMPER(2),
    // Tamper interrupt
    RTC_WKUP(3),
    // RTC Wakeup interrupt
    FLASH(4),
    // FLASH global interrupt
    RCC(5),
    // RCC global interrupt
    EXTI0(6),
    // EXTI Line0 interrupt
    EXTI1(7),
    // EXTI Line1 interrupt
    EXTI2(8),
    // EXTI Line2 interrupt
    EXTI3(9),
    // EXTI Line3 interrupt
    EXTI4(10),
    // EXTI Line4 interrupt
    DMA1_STREAM0(11),
    // DMA1 Stream0 global interrupt
    DMA1_STREAM1(12),
    // DMA1 Stream1 global interrupt
    DMA1_STREAM2(13),
    // DMA1 Stream2 global interrupt
    DMA1_STREAM3(14),
    // DMA1 Stream3 global interrupt
    DMA1_STREAM4(15),
    // DMA1 Stream4 global interrupt
    DMA1_STREAM5(16),
    // DMA1 Stream5 global interrupt
    DMA1_STREAM6(17),
    // DMA1 Stream6 global interrupt
    ADC(18),
    // ADC1 global interrupt
    CAN1_TX(19),
    // CAN1 TX interrupts
    CAN1_RX0(20),
    // CAN1 RX0 interrupts
    CAN1_RX1(21),
    // CAN1 RX1 interrupt
    CAN1_SCE(22),
    // CAN1 SCE interrupt
    EXTI9_5(23),
    // EXTI Line[9:5] interrupt
    TIM1_BRK_TIM9(24),
    // TIM1 Break interrupt and TIM9 global interrupt
    TIM1_UP_TIM10(25),
    // TIM1 Update interrupt and TIM10 global interrupt
    TIM1_TRG_COM_TIM11(26),
    // TIM1 Trigger and commutation interrupt and TIM11 global interrupt
    TIM1_CC(27),
    // TIM1 Capture Compare interrupt
    TIM2(28),
    // TIM2 global interrupt
    TIM3(29),
    // TIM3 global interrupt
    TIM4(30),
    // TIM4 global interrupt
    I2C1_EV(31),
    // I2C1 Event interrupt
    I2C1_ER(32),
    // I2C1 Error interrupt
    I2C2_EV(33),
    // I2C2 Event interrupt
    I2C2_ER(34),
    // I2C2 Error interrupt
    SPI1(35),
    // SPI1 global interrupt
    SPI2(36),
    // SPI2 global interrupt
    USART1(37),
    // USART1 global interrupt
    USART2(38),
    // USART2 global interrupt
    USART3(39),
    // USART3 global interrupt
    EXTI15_10(40),
    // EXTI Line[15:10] interrupts
    RTC_ALARM(41),
    // RTC Alarm (A and B) through EXTI Line interrupt
    OTG_FS_WKUP(42),
    // USB OTG FS Wakeup through EXTI interrupt
    TIM8_BRK_TIM12(43),
    // TIM8 Break interrupt and TIM12 global interrupt
    TIM8_UP_TIM13(44),
    // TIM8 Update interrupt and TIM13 global interrupt
    TIM8_TRG_COM_TIM14(45),
    // TIM8 Trigger and commutation interrupt and TIM14 global interrupt
    TIM8_CC(46),
    // TIM8 Capture Compare interrupt
    SPI3(47),
    // SPI3 global interrupt
    UART4(48),
    // UART4 global interrupt
    UART5(49),
    // UART5 global interrupt
    TIM6(50),
    // TIM6 global interrupt
    TIM7(51),
    // TIM7 global interrupt
    DMA2_STREAM0(52),
    // DMA2 Stream0 global interrupt
    DMA2_STREAM1(53),
    // DMA2 Stream1 global interrupt
    DMA2_STREAM2(54),
    // DMA2 Stream2 global interrupt
    DMA2_STREAM3(55),
    // DMA2 Stream3 global interrupt
    DMA2_STREAM4(56),
    // DMA2 Stream4 global interrupt
    ETH(57),
    // Ethernet global interrupt
    ETH_WKUP(58),
    // Ethernet Wakeup through EXTI interrupt
    CAN2_TX(59),
    // CAN2 TX interrupts
    CAN2_RX0(60),
    // CAN2 RX0 interrupts
    CAN2_RX1(61),
    // CAN2 RX1 interrupt
    CAN2_SCE(62),
    // CAN2 SCE interrupt
    NA(63),
    // Not Available
    OTG_FS(64),
    // USB OTG FS global interrupt
    DCMI(65),
    // DCMI global interrupt
    CRYP(66),
    // CRYP global interrupt
    HASH_RNG(67),
    // HASH and RNG global interrupt
    FPU(68),
    // FPU global interrupt
}

// 寄存器访问函数
@OptIn(ExperimentalForeignApi::class)
inline fun <reified T> readReg(addr: ULong): T {
    return memScoped {
        val ptr = addr.toCPointer<T>() ?: error("Null pointer")
        ptr.pointed.readValue()
    }
}

@OptIn(ExperimentalForeignApi::class)
inline fun <reified T> writeReg(addr: ULong, value: T) {
    memScoped {
        val ptr = addr.toCPointer<T>() ?: error("Null pointer")
        ptr.pointed.writeValue(value)
    }
}

fun initDevice() {
    // 设备初始化
    // 例如: writeReg(AX.toULong(), 0x1234u)
}
