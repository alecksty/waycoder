#ifndef SAMD21_HPP
#define SAMD21_HPP

// SAMD21寄存器定义
// 生成自: Atmel (Microchip)/SAM D/SAMD21
// 版本: 
// 日期: 


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: ARM Cortex-M0+
// 位宽: 0位
// 时钟频率: 0 Hz

// 外设定义
// Power Manager
#define PM_BASE 
#define PM_PM_CTRL (*(volatile uint64_t*)0x40000400)

// System Controller
#define SYSCTRL_BASE 
#define SYSCTRL_SYSCTRL_INTENCLR (*(volatile uint256_t*)0x40000800)

// Generic Clock Generator
#define GCLK_BASE 
#define GCLK_GCLK_CTRL (*(volatile uint64_t*)0x40000C00)

// Watchdog Timer
#define WDT_BASE 
#define WDT_WDT_CTRL (*(volatile uint64_t*)0x40001000)

// Real-Time Clock
#define RTC_BASE 
#define RTC_RTC_CTRL (*(volatile uint128_t*)0x40001400)

// External Interrupt Controller
#define EIC_BASE 
#define EIC_EIC_CTRL (*(volatile uint64_t*)0x40001800)

// Serial Communication Interface 0
#define SERCOM0_BASE 
#define SERCOM0_SERCOM0_I2CM_CTRLA (*(volatile uint256_t*)0x42000800)

// Analog-to-Digital Converter
#define ADC_BASE 
#define ADC_ADC_CTRLA (*(volatile uint64_t*)0x42002000)

// Digital-to-Analog Converter
#define DAC_BASE 
#define DAC_DAC_CTRLA (*(volatile uint64_t*)0x42002400)

// General Purpose I/O
#define PORT_BASE 
#define PORT_PORT_DIR (*(volatile uint256_t*)0x41004400)

// Timer/Counter 0
#define TC0_BASE 
#define TC0_TC0_CTRLA (*(volatile uint128_t*)0x42002800)

// USB Device Controller
#define USB_BASE 
#define USB_USB_CTRLA (*(volatile uint64_t*)0x41005000)

// 中断向量定义
#define RESET_VECTOR 0  // Reset vector
#define NONMASKABLEINT_VECTOR 1  // Non-maskable interrupt
#define HARDFAULT_VECTOR 2  // Hard fault
#define SVCALL_VECTOR 3  // Supervisor call
#define PENDSV_VECTOR 4  // Pendable service call
#define SYSTICK_VECTOR 5  // System tick timer
#define PM_VECTOR 6  // Power Manager
#define SYSCTRL_VECTOR 7  // System Controller
#define WDT_VECTOR 8  // Watchdog Timer
#define RTC_VECTOR 9  // Real-Time Clock
#define EIC_VECTOR 10  // External Interrupt Controller
#define NVMCTRL_VECTOR 11  // Non-Volatile Memory Controller
#define DMAC_VECTOR 12  // Direct Memory Access Controller
#define USB_VECTOR 13  // USB Device Controller
#define EVSYS_VECTOR 14  // Event System
#define SERCOM0_VECTOR 15  // Serial Communication Interface 0
#define SERCOM1_VECTOR 16  // Serial Communication Interface 1
#define SERCOM2_VECTOR 17  // Serial Communication Interface 2
#define SERCOM3_VECTOR 18  // Serial Communication Interface 3
#define SERCOM4_VECTOR 19  // Serial Communication Interface 4
#define SERCOM5_VECTOR 20  // Serial Communication Interface 5
#define TCC0_VECTOR 21  // Timer/Counter for Control 0
#define TCC1_VECTOR 22  // Timer/Counter for Control 1
#define TCC2_VECTOR 23  // Timer/Counter for Control 2
#define TC3_VECTOR 24  // Timer/Counter 3
#define TC4_VECTOR 25  // Timer/Counter 4
#define TC5_VECTOR 26  // Timer/Counter 5
#define TC6_VECTOR 27  // Timer/Counter 6
#define TC7_VECTOR 28  // Timer/Counter 7
#define ADC_VECTOR 29  // Analog-to-Digital Converter
#define AC_VECTOR 30  // Analog Comparator
#define DAC_VECTOR 31  // Digital-to-Analog Converter
#define PTC_VECTOR 32  // Peripheral Touch Controller
#define I2S_VECTOR 33  // Inter-IC Sound Interface

void samd21_init(void);

#ifdef __cplusplus
}
#endif

#endif // SAMD21_HPP
