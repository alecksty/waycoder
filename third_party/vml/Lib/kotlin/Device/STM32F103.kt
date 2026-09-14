package stm32f103c8t6

/**
 * 设备寄存器定义
 * 设备: STM32F103C8T6
 * 生成自: STMicroelectronics/STM32/STM32F103C8T6
 * 版本: 1.0
 * 日期: 2026-04-16
 * 作者: VML Team
 * 描述: 32-bit ARM Cortex-M3 MCU with 64KB Flash, 20KB RAM, 72MHz
 */

// CPU架构: ARM-Cortex-M3
// 位宽: 32位
// 时钟频率: 72000000 Hz

import kotlinx.cinterop.*

// 寄存器定义

















// 内存段定义
// 外设定义
// RCC: Reset and Clock Control

// GPIOA: GPIO Port A

// GPIOB: GPIO Port B

// GPIOC: GPIO Port C

// USART1: USART 1

// USART2: USART 2

// SPI1: SPI 1

// SPI2: SPI 2

// I2C1: I2C 1

// I2C2: I2C 2

// TIM1: Advanced Timer 1

// TIM2: General Purpose Timer 2

// TIM3: General Purpose Timer 3

// TIM4: General Purpose Timer 4

// ADC1: ADC 1

// DMA1: DMA Controller 1

// PWR: Power Control

// BKP: Backup Registers

// WWDG: Window Watchdog

// IWDG: Independent Watchdog

// EXTI: External Interrupt/Event Controller

// AFIO: Alternate Function IO

// 中断向量定义
enum class Irq(val vector: Int) {
    WWDG(0),
    // Window Watchdog Interrupt
    PVD(1),
    // PVD through EXTI Line detection
    TAMPER(2),
    // Tamper Interrupt
    RTC(3),
    // RTC Global Interrupt
    FLASH(4),
    // FLASH Global Interrupt
    RCC(5),
    // RCC Global Interrupt
    EXTI0(6),
    // EXTI Line 0 Interrupt
    EXTI1(7),
    // EXTI Line 1 Interrupt
    EXTI2(8),
    // EXTI Line 2 Interrupt
    EXTI3(9),
    // EXTI Line 3 Interrupt
    EXTI4(10),
    // EXTI Line 4 Interrupt
    DMA1_CHANNEL1(11),
    // DMA1 Channel 1 Interrupt
    DMA1_CHANNEL2(12),
    // DMA1 Channel 2 Interrupt
    DMA1_CHANNEL3(13),
    // DMA1 Channel 3 Interrupt
    DMA1_CHANNEL4(14),
    // DMA1 Channel 4 Interrupt
    DMA1_CHANNEL5(15),
    // DMA1 Channel 5 Interrupt
    DMA1_CHANNEL6(16),
    // DMA1 Channel 6 Interrupt
    DMA1_CHANNEL7(17),
    // DMA1 Channel 7 Interrupt
    ADC1_2(18),
    // ADC1 and ADC2 Global Interrupt
    USB_HP_CAN_TX(19),
    // USB HP/CAN TX Interrupts
    USB_LP_CAN_RX0(20),
    // USB LP/CAN RX0 Interrupt
    CAN_RX1(21),
    // CAN RX1 Interrupt
    CAN_SCE(22),
    // CAN SCE Interrupt
    EXTI9_5(23),
    // EXTI Line 9..5 Interrupt
    TIM1_BRK(25),
    // TIM1 Break Interrupt
    TIM1_UP(26),
    // TIM1 Update Interrupt
    TIM1_TRG_COM(27),
    // TIM1 Trigger and Commutation
    TIM1_CC(28),
    // TIM1 Capture Compare Interrupt
    TIM2(29),
    // TIM2 Global Interrupt
    TIM3(30),
    // TIM3 Global Interrupt
    TIM4(31),
    // TIM4 Global Interrupt
    I2C1_EV(32),
    // I2C1 Event Interrupt
    I2C1_ER(33),
    // I2C1 Error Interrupt
    I2C2_EV(34),
    // I2C2 Event Interrupt
    I2C2_ER(35),
    // I2C2 Error Interrupt
    SPI1(35),
    // SPI1 Global Interrupt
    SPI2(36),
    // SPI2 Global Interrupt
    USART1(37),
    // USART1 Global Interrupt
    USART2(38),
    // USART2 Global Interrupt
    USART3(39),
    // USART3 Global Interrupt
    EXTI15_10(40),
    // EXTI Line 15..10 Interrupt
    RTCALARM(41),
    // RTC Alarm through EXTI
    USBWAKEUP(42),
    // USB Wakeup from suspend
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
