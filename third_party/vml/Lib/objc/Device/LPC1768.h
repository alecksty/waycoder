// LPC1768 设备定义 - Objective-C 头文件
// 生成自: NXP/LPC17xx/LPC1768
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: ARM Cortex-M3 up to 100MHz with 512KB Flash, 64KB SRAM
// CPU架构: ARM-Cortex-M3
// 位宽: 32位
// 时钟频率: 12000000 Hz

#ifndef LPC1768_DEVICE_H
#define LPC1768_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define R0_ADDR 0x00000000  // General Purpose Register 0
#define R1_ADDR 0x00000004  // General Purpose Register 1
#define R2_ADDR 0x00000008  // General Purpose Register 2
#define R3_ADDR 0x0000000C  // General Purpose Register 3
#define R4_ADDR 0x00000010  // General Purpose Register 4
#define R5_ADDR 0x00000014  // General Purpose Register 5
#define R6_ADDR 0x00000018  // General Purpose Register 6
#define R7_ADDR 0x0000001C  // General Purpose Register 7
#define R8_ADDR 0x00000020  // General Purpose Register 8
#define R9_ADDR 0x00000024  // General Purpose Register 9
#define R10_ADDR 0x00000028  // General Purpose Register 10
#define R11_ADDR 0x0000002C  // General Purpose Register 11
#define R12_ADDR 0x00000030  // General Purpose Register 12
#define SP_ADDR 0x00000034  // Stack Pointer
#define LR_ADDR 0x00000038  // Link Register
#define PC_ADDR 0x0000003C  // Program Counter
#define PSR_ADDR 0x00000040  // Program Status Register
#define PSR_N_BIT 31  // Negative Flag
#define PSR_Z_BIT 30  // Zero Flag
#define PSR_C_BIT 29  // Carry Flag
#define PSR_V_BIT 28  // Overflow Flag
#define PSR_Q_BIT 27  // Saturation Flag
#define PSR_ICI1_BIT 0  // Interrupt Continue State
#define PSR_GE_BIT 0  // Greater than or Equal
#define PSR_IT_BIT 0  // If-Then execution state
#define PSR_APSR_BIT 0  // Application Program Status
#define PRIMASK_ADDR 0xE0000E20  // Priority Mask Register
#define FAULTMASK_ADDR 0xE0000E28  // Fault Mask Register
#define BASEPRI_ADDR 0xE0000E24  // Base Priority Register
#define CONTROL_ADDR 0xE0000E2C  // Control Register

// 内存段定义
#define FLASH_START 0x00000000
#define FLASH_END 0x0007FFFF
#define FLASH_SIZE 524288  // Main Flash (512KB)
#define FLASH_BOOT_START 0x00080000
#define FLASH_BOOT_END 0x0007FFFF
#define FLASH_BOOT_SIZE 32768  // Boot Flash (32KB)
#define SRAM_START 0x10000000
#define SRAM_END 0x1000FFFF
#define SRAM_SIZE 65536  // SRAM (64KB)
#define AHB1_START 0x20000000
#define AHB1_END 0x200FFFFF
#define AHB1_SIZE 1048576  // AHB1 Peripherals
#define APB0_START 0x40000000
#define APB0_END 0x400FFFFF
#define APB0_SIZE 1048576  // APB0 Peripherals
#define APB1_START 0x50000000
#define APB1_END 0x500FFFFF
#define APB1_SIZE 1048576  // APB1 Peripherals

// 外设定义
// GPIO
#define GPIO_BASE 0x2009C000
#define GPIO_FIODIR_ADDR 0x0000
#define GPIO_FIOMASK_ADDR 0x0004
#define GPIO_FIOPIN_ADDR 0x0008
#define GPIO_FIOSET_ADDR 0x000C
#define GPIO_FIOCLR_ADDR 0x0010
#define GPIO_P0_ADDR 0x0014
#define GPIO_P1_ADDR 0x0018
#define GPIO_P2_ADDR 0x001C
#define GPIO_P3_ADDR 0x0020
#define GPIO_P4_ADDR 0x0024
// UART0
#define UART0_BASE 0x4000C000
#define UART0_RBR_ADDR 0x0000
#define UART0_THR_ADDR 0x0000
#define UART0_DLL_ADDR 0x0000
#define UART0_DLM_ADDR 0x0004
#define UART0_IER_ADDR 0x0004
#define UART0_IIR_ADDR 0x0008
#define UART0_FCR_ADDR 0x0008
#define UART0_LCR_ADDR 0x000C
#define UART0_LSR_ADDR 0x0014
#define UART0_SCR_ADDR 0x001C
#define UART0_ACR_ADDR 0x0020
#define UART0_ICR_ADDR 0x0024
#define UART0_FDR_ADDR 0x0028
#define UART0_TER_ADDR 0x0030
// UART1
#define UART1_BASE 0x4000D000
#define UART1_RBR_ADDR 0x0000
#define UART1_THR_ADDR 0x0000
#define UART1_DLL_ADDR 0x0000
#define UART1_DLM_ADDR 0x0004
#define UART1_IER_ADDR 0x0004
#define UART1_IIR_ADDR 0x0008
#define UART1_LCR_ADDR 0x000C
#define UART1_LSR_ADDR 0x0014
#define UART1_SCR_ADDR 0x001C
#define UART1_MSR_ADDR 0x0020
#define UART1_SCR_ADDR 0x0024
// UART2
#define UART2_BASE 0x40098000
#define UART2_RBR_ADDR 0x0000
#define UART2_THR_ADDR 0x0000
#define UART2_DLL_ADDR 0x0000
#define UART2_DLM_ADDR 0x0004
#define UART2_IER_ADDR 0x0004
#define UART2_IIR_ADDR 0x0008
#define UART2_LCR_ADDR 0x000C
#define UART2_LSR_ADDR 0x0014
// UART3
#define UART3_BASE 0x4009C000
#define UART3_RBR_ADDR 0x0000
#define UART3_THR_ADDR 0x0000
#define UART3_DLL_ADDR 0x0000
#define UART3_DLM_ADDR 0x0004
#define UART3_IER_ADDR 0x0004
#define UART3_IIR_ADDR 0x0008
#define UART3_LCR_ADDR 0x000C
#define UART3_LSR_ADDR 0x0014
// SPI0
#define SPI0_BASE 0x40088000
#define SPI0_CR0_ADDR 0x0000
#define SPI0_CR1_ADDR 0x0004
#define SPI0_DR_ADDR 0x0008
#define SPI0_SR_ADDR 0x000C
#define SPI0_CPSR_ADDR 0x0010
#define SPI0_IMSC_ADDR 0x0014
#define SPI0_RIS_ADDR 0x0018
#define SPI0_MIS_ADDR 0x001C
#define SPI0_ICR_ADDR 0x0020
// SPI1
#define SPI1_BASE 0x4008C000
#define SPI1_CR0_ADDR 0x0000
#define SPI1_CR1_ADDR 0x0004
#define SPI1_DR_ADDR 0x0008
#define SPI1_SR_ADDR 0x000C
#define SPI1_CPSR_ADDR 0x0010
// I2C0
#define I2C0_BASE 0x4001C000
#define I2C0_CON_ADDR 0x0000
#define I2C0_TAR_ADDR 0x0004
#define I2C0_DAT_ADDR 0x0008
#define I2C0_SSHC_ADDR 0x000C
#define I2C0_HSH_ADDR 0x0010
#define I2C0_INTX_ADDR 0x0014
#define I2C0_INTM_ADDR 0x0018
#define I2C0_AR_ADDR 0x001C
#define I2C0_SR_ADDR 0x0020
#define I2C0_TXFL_ADDR 0x0024
#define I2C0_RXFL_ADDR 0x0028
#define I2C0_COMP_ADDR 0x002C
#define I2C0_RXFI_ADDR 0x0030
#define I2C0_RXFT_ADDR 0x0030
// I2C1
#define I2C1_BASE 0x4001C000
#define I2C1_CON_ADDR 0x0000
#define I2C1_TAR_ADDR 0x0004
#define I2C1_DAT_ADDR 0x0008
#define I2C1_SR_ADDR 0x0020
// Timer0
#define TIMER0_BASE 0x40004000
#define TIMER0_IR_ADDR 0x0000
#define TIMER0_TCR_ADDR 0x0004
#define TIMER0_TC_ADDR 0x0008
#define TIMER0_PR_ADDR 0x000C
#define TIMER0_PC_ADDR 0x0010
#define TIMER0_MCR_ADDR 0x0014
#define TIMER0_MR0_ADDR 0x0018
#define TIMER0_MR1_ADDR 0x001C
#define TIMER0_MR2_ADDR 0x0020
#define TIMER0_MR3_ADDR 0x0024
#define TIMER0_CCR_ADDR 0x0028
#define TIMER0_CR0_ADDR 0x002C
#define TIMER0_CR1_ADDR 0x0030
#define TIMER0_CR2_ADDR 0x0034
#define TIMER0_CR3_ADDR 0x0038
#define TIMER0_EMR_ADDR 0x003C
#define TIMER0_CTCR_ADDR 0x0070
#define TIMER0_EW_ADDR 0x0074
// Timer1
#define TIMER1_BASE 0x40008000
#define TIMER1_IR_ADDR 0x0000
#define TIMER1_TCR_ADDR 0x0004
#define TIMER1_TC_ADDR 0x0008
#define TIMER1_PR_ADDR 0x000C
#define TIMER1_MCR_ADDR 0x0014
#define TIMER1_MR0_ADDR 0x0018
#define TIMER1_MR1_ADDR 0x001C
#define TIMER1_MR2_ADDR 0x0020
#define TIMER1_MR3_ADDR 0x0024
#define TIMER1_CCR_ADDR 0x0028
#define TIMER1_CR0_ADDR 0x002C
#define TIMER1_CR1_ADDR 0x0030
#define TIMER1_EMR_ADDR 0x003C
// Timer2
#define TIMER2_BASE 0x400A4000
#define TIMER2_IR_ADDR 0x0000
#define TIMER2_TCR_ADDR 0x0004
#define TIMER2_TC_ADDR 0x0008
#define TIMER2_PR_ADDR 0x000C
#define TIMER2_MCR_ADDR 0x0014
#define TIMER2_MR0_ADDR 0x0018
#define TIMER2_CCR_ADDR 0x0028
#define TIMER2_CR0_ADDR 0x002C
// Timer3
#define TIMER3_BASE 0x400A8000
#define TIMER3_IR_ADDR 0x0000
#define TIMER3_TCR_ADDR 0x0004
#define TIMER3_TC_ADDR 0x0008
#define TIMER3_PR_ADDR 0x000C
#define TIMER3_MCR_ADDR 0x0014
#define TIMER3_MR0_ADDR 0x0018
#define TIMER3_CCR_ADDR 0x0028
// PWM0
#define PWM0_BASE 0x40014000
#define PWM0_IR_ADDR 0x0000
#define PWM0_TCR_ADDR 0x0004
#define PWM0_TC_ADDR 0x0008
#define PWM0_PR_ADDR 0x000C
#define PWM0_PC_ADDR 0x0010
#define PWM0_MCR_ADDR 0x0014
#define PWM0_MR0_ADDR 0x0018
#define PWM0_MR1_ADDR 0x001C
#define PWM0_MR2_ADDR 0x0020
#define PWM0_MR3_ADDR 0x0024
#define PWM0_MR4_ADDR 0x0040
#define PWM0_MR5_ADDR 0x0044
#define PWM0_MR6_ADDR 0x0048
#define PWM0_CCR_ADDR 0x0028
#define PWM0_CR0_ADDR 0x002C
#define PWM0_PCR_ADDR 0x004C
#define PWM0_LER_ADDR 0x0050
#define PWM0_CTCR_ADDR 0x0070
// ADC
#define ADC_BASE 0x400E4000
#define ADC_CR_ADDR 0x0000
#define ADC_GDR_ADDR 0x0004
#define ADC_INTEN_ADDR 0x000C
#define ADC_STATUS_ADDR 0x0010
#define ADC_TR_ADDR 0x0014
// DAC
#define DAC_BASE 0x400E5000
#define DAC_CR_ADDR 0x0000
#define DAC_CTRL_ADDR 0x0004
// Ethernet
#define ETH_BASE 0x50000000
#define ETH_MAC1_ADDR 0x0000
#define ETH_MAC2_ADDR 0x0004
#define ETH_IPGT_ADDR 0x0008
#define ETH_IPGR_ADDR 0x000C
#define ETH_CLRT_ADDR 0x0010
#define ETH_MAXF_ADDR 0x0014
#define ETH_SUPP_ADDR 0x0018
#define ETH_TEST_ADDR 0x001C
#define ETH_MCFG_ADDR 0x0020
#define ETH_MCMD_ADDR 0x0024
#define ETH_MADR_ADDR 0x0028
#define ETH_MWTD_ADDR 0x002C
#define ETH_MRDD_ADDR 0x0030
#define ETH_IND_ADDR 0x0034
// USB Controller
#define USB_BASE 0x50000000
#define USB_HCCHAR_ADDR 
#define USB_HCINT_ADDR 
#define USB_HCINTMSK_ADDR 
#define USB_HCTSIZ_ADDR 
#define USB_HCDMA_ADDR 
#define USB_HCDMAB_ADDR 
#define USB_OTGINTST_ADDR 
#define USB_OTGINTEN_ADDR 
#define USB_OTGINTSEL_ADDR 
// DMA Controller
#define DMA_BASE 0x50004000
#define DMA_INTSTAT_ADDR 0x0000
#define DMA_INTTCSTAT_ADDR 0x0004
#define DMA_INTTCCLEAR_ADDR 0x0008
#define DMA_INTERRSTAT_ADDR 0x000C
#define DMA_INTERRCLR_ADDR 0x0010
#define DMA_RAWINTSTAT_ADDR 0x0014
#define DMA_RAWINTTCSTAT_ADDR 0x0018
#define DMA_ENBLDCHNS_ADDR 0x001C
#define DMA_SOFTBREQ_ADDR 0x0020
#define DMA_SOFTSREQ_ADDR 0x0024
#define DMA_CONFIG_ADDR 0x0028
#define DMA_SYNC_ADDR 0x002C
// Watchdog Timer
#define WDT_BASE 0x40000000
#define WDT_WDMOD_ADDR 0x0000
#define WDT_WDTC_ADDR 0x0004
#define WDT_WDFEED_ADDR 0x0008
#define WDT_WDTV_ADDR 0x000C
// RTC
#define RTC_BASE 0x40024000
#define RTC_ILR_ADDR 0x0000
#define RTC_CCR_ADDR 0x0004
#define RTC_CIIR_ADDR 0x0008
#define RTC_CWR_ADDR 0x000C
#define RTC_PREINT_ADDR 0x0010
#define RTC_PREFRAC_ADDR 0x0014
#define RTC_CRT_ADDR 0x0018
#define RTC_SEC_ADDR 0x001C
#define RTC_MIN_ADDR 0x0020
#define RTC_HOUR_ADDR 0x0024
#define RTC_DOM_ADDR 0x0028
#define RTC_DOW_ADDR 0x002C
#define RTC_DOY_ADDR 0x0030
#define RTC_MONTH_ADDR 0x0034
#define RTC_YEAR_ADDR 0x0038
// System Control
#define SC_BASE 0x400FC000
#define SC_PLL0CON_ADDR 0x0000
#define SC_PLL0CFG_ADDR 0x0004
#define SC_PLL0STAT_ADDR 0x0008
#define SC_PLL0FEED_ADDR 0x000C
#define SC_PLL1CON_ADDR 0x0010
#define SC_PLL1CFG_ADDR 0x0014
#define SC_PLL1STAT_ADDR 0x0018
#define SC_PLL1FEED_ADDR 0x001C
#define SC_CCLKCFG_ADDR 0x0020
#define SC_USBCLKCFG_ADDR 0x0024
#define SC_CLKSRC_ADDR 0x0028
#define SC_PCLKSEL0_ADDR 0x002C
#define SC_PCLKSEL1_ADDR 0x0030
#define SC_BOSC_ADDR 0x0050
#define SC_EXTINT_ADDR 0x0054
#define SC_EXTMODE_ADDR 0x0058
#define SC_EXTPOL_ADDR 0x005C
// Pin Connect Block
#define PINCONNECTBLOCK_BASE 0x4002C000
#define PINCONNECTBLOCK_PINSEL0_ADDR 0x0000
#define PINCONNECTBLOCK_PINSEL1_ADDR 0x0004
#define PINCONNECTBLOCK_PINSEL2_ADDR 0x0008
#define PINCONNECTBLOCK_PINSEL3_ADDR 0x000C
#define PINCONNECTBLOCK_PINSEL4_ADDR 0x0010
#define PINCONNECTBLOCK_PINSEL5_ADDR 0x0014
#define PINCONNECTBLOCK_PINSEL6_ADDR 0x0018
#define PINCONNECTBLOCK_PINSEL7_ADDR 0x001C
#define PINCONNECTBLOCK_PINSEL8_ADDR 0x0020
#define PINCONNECTBLOCK_PINSEL9_ADDR 0x0024
#define PINCONNECTBLOCK_PINMODE0_ADDR 0x0040
#define PINCONNECTBLOCK_PINMODE1_ADDR 0x0044
#define PINCONNECTBLOCK_PINMODE2_ADDR 0x0048
#define PINCONNECTBLOCK_PINMODE3_ADDR 0x004C
#define PINCONNECTBLOCK_PINMODE4_ADDR 0x0050
#define PINCONNECTBLOCK_PINMODE5_ADDR 0x0054
#define PINCONNECTBLOCK_PINMODE6_ADDR 0x0058
#define PINCONNECTBLOCK_PINMODE7_ADDR 0x005C
#define PINCONNECTBLOCK_PINMODE8_ADDR 0x0060
#define PINCONNECTBLOCK_PINMODE9_ADDR 0x0064
#define PINCONNECTBLOCK_PINOD0_ADDR 0x0080
#define PINCONNECTBLOCK_PINOD1_ADDR 0x0084
#define PINCONNECTBLOCK_PINOD2_ADDR 0x0088
#define PINCONNECTBLOCK_PINOD3_ADDR 0x008C

// 中断向量定义
#define INT_WDT 0  // Watchdog Timer
#define INT_RESERVED 1  // Reserved
#define INT_DEBUG_MON 2  // ARM Debug Mon
#define INT_RESERVED 3  // Reserved
#define INT_TIMER0 4  // Timer 0
#define INT_TIMER1 5  // Timer 1
#define INT_PWM0 6  // PWM 0
#define INT_UART0 7  // UART 0
#define INT_UART1 8  // UART 1
#define INT_PWM1 9  // PWM 1
#define INT_I2C0 10  // I2C 0
#define INT_I2C1 11  // I2C 1
#define INT_SPI0 12  // SPI 0
#define INT_SPI1 13  // SPI 1
#define INT_RTC 14  // RTC
#define INT_EINT0 15  // External Interrupt 0
#define INT_EINT1 16  // External Interrupt 1
#define INT_EINT2 17  // External Interrupt 2
#define INT_EINT3 18  // External Interrupt 3
#define INT_RESERVED 19  // Reserved
#define INT_ADC 20  // A/D Converter
#define INT_BOD 21  // Brown-Out Detect
#define INT_USB 22  // USB
#define INT_CAN 23  // CAN
#define INT_GP 24  // General Purpose DMA
#define INT_I2S 25  // I2S
#define INT_ETHERNET 26  // Ethernet
#define INT_RIT 27  // Repetitive Interrupt Timer
#define INT_QM 28  // Quadrature Encoder
#define INT_RESERVED 29  // Reserved
#define INT_RESERVED 30  // Reserved

// 引脚定义
#define PIN_RESET 1  // External Reset
#define PIN_P0_0 2  // GPIO Port 0.0
#define PIN_P0_1 3  // GPIO Port 0.1
#define PIN_VSSA 4  // Analog Ground
#define PIN_VDDA 5  // Analog 3.3V
#define PIN_P0_2 6  // GPIO Port 0.2
#define PIN_P0_3 7  // GPIO Port 0.3
#define PIN_P0_4 8  // GPIO Port 0.4
#define PIN_P0_5 9  // GPIO Port 0.5
#define PIN_P0_6 10  // GPIO Port 0.6
#define PIN_P0_7 11  // GPIO Port 0.7
#define PIN_P0_8 12  // GPIO Port 0.8
#define PIN_P0_9 13  // GPIO Port 0.9
#define PIN_P0_10 14  // GPIO Port 0.10
#define PIN_VSS 15  // Ground
#define PIN_VDD 16  // 3.3V
#define PIN_P0_11 17  // GPIO Port 0.11
#define PIN_P0_12 18  // GPIO Port 0.12
#define PIN_P0_13 19  // GPIO Port 0.13
#define PIN_P0_14 20  // GPIO Port 0.14
#define PIN_P0_15 21  // GPIO Port 0.15
#define PIN_P0_16 22  // GPIO Port 0.16
#define PIN_P0_17 23  // GPIO Port 0.17
#define PIN_P0_18 24  // GPIO Port 0.18
#define PIN_P0_19 25  // GPIO Port 0.19
#define PIN_P0_20 26  // GPIO Port 0.20
#define PIN_P0_21 27  // GPIO Port 0.21
#define PIN_P0_22 28  // GPIO Port 0.22
#define PIN_P0_23 29  // GPIO Port 0.23
#define PIN_VSS 30  // Ground
#define PIN_VDD 31  // 3.3V
#define PIN_RTCX1 32  // RTC Crystal Input
#define PIN_RTCX2 33  // RTC Crystal Output
#define PIN_P1_0 34  // GPIO Port 1.0
#define PIN_P1_1 35  // GPIO Port 1.1
#define PIN_P1_2 36  // GPIO Port 1.2
#define PIN_P1_3 37  // GPIO Port 1.3
#define PIN_P1_4 38  // GPIO Port 1.4
#define PIN_P1_5 39  // GPIO Port 1.5
#define PIN_P1_6 40  // GPIO Port 1.6
#define PIN_P1_7 41  // GPIO Port 1.7
#define PIN_P1_8 42  // GPIO Port 1.8
#define PIN_P1_9 43  // GPIO Port 1.9
#define PIN_P1_10 44  // GPIO Port 1.10
#define PIN_P1_11 45  // GPIO Port 1.11
#define PIN_P1_12 46  // GPIO Port 1.12
#define PIN_P1_13 47  // GPIO Port 1.13
#define PIN_P1_14 48  // GPIO Port 1.14
#define PIN_P1_15 49  // GPIO Port 1.15
#define PIN_P1_16 50  // GPIO Port 1.16
#define PIN_P1_17 51  // GPIO Port 1.17
#define PIN_P1_18 52  // GPIO Port 1.18
#define PIN_P1_19 53  // GPIO Port 1.19
#define PIN_P1_20 54  // GPIO Port 1.20
#define PIN_P1_21 55  // GPIO Port 1.21
#define PIN_P1_22 56  // GPIO Port 1.22
#define PIN_P1_23 57  // GPIO Port 1.23
#define PIN_P1_24 58  // GPIO Port 1.24
#define PIN_P1_25 59  // GPIO Port 1.25
#define PIN_P1_26 60  // GPIO Port 1.26
#define PIN_P1_27 61  // GPIO Port 1.27
#define PIN_P1_28 62  // GPIO Port 1.28
#define PIN_P1_29 63  // GPIO Port 1.29
#define PIN_P1_30 64  // GPIO Port 1.30
#define PIN_P1_31 65  // GPIO Port 1.31
#define PIN_P2_0 66  // GPIO Port 2.0
#define PIN_P2_1 67  // GPIO Port 2.1
#define PIN_P2_2 68  // GPIO Port 2.2
#define PIN_P2_3 69  // GPIO Port 2.3
#define PIN_P2_4 70  // GPIO Port 2.4
#define PIN_P2_5 71  // GPIO Port 2.5
#define PIN_P2_6 72  // GPIO Port 2.6
#define PIN_P2_7 73  // GPIO Port 2.7
#define PIN_P2_8 74  // GPIO Port 2.8
#define PIN_P2_9 75  // GPIO Port 2.9
#define PIN_P2_10 76  // GPIO Port 2.10
#define PIN_P2_11 77  // GPIO Port 2.11
#define PIN_P2_12 78  // GPIO Port 2.12
#define PIN_P2_13 79  // GPIO Port 2.13
#define PIN_P2_14 80  // GPIO Port 2.14
#define PIN_P2_15 81  // GPIO Port 2.15
#define PIN_VSS 82  // Ground
#define PIN_VDD 83  // 3.3V

#endif /* LPC1768_DEVICE_H */
