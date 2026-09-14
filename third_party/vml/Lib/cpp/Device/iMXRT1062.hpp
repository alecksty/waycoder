#ifndef I_MX_RT1062_HPP
#define I_MX_RT1062_HPP

// i.MX RT1062寄存器定义
// 生成自: NXP/i.MX RT/i.MX RT1062
// 版本: 1.0
// 日期: 2026-04-29


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: ARM-Cortex-M7
// 位宽: 32位
// 时钟频率: 528000000 Hz

// 外设定义
// LPUART 1
#define UART1_BASE 0x40184000
#define UART1_VERID (*(volatile uint32_t*)0x40184000)
#define UART1_CTRL (*(volatile uint32_t*)0x40184010)
#define UART1_STAT (*(volatile uint32_t*)0x40184014)
#define UART1_DATA (*(volatile uint32_t*)0x4018401C)
#define UART1_BAUD (*(volatile uint32_t*)0x40184024)

// LPUART 2
#define UART2_BASE 0x40188000
#define UART2_CTRL (*(volatile uint32_t*)0x40188010)
#define UART2_STAT (*(volatile uint32_t*)0x40188014)
#define UART2_DATA (*(volatile uint32_t*)0x4018801C)
#define UART2_BAUD (*(volatile uint32_t*)0x40188024)

// GPIO 1
#define GPIO1_BASE 0x401B8000
#define GPIO1_DR (*(volatile uint32_t*)0x401B8000)
#define GPIO1_GDIR (*(volatile uint32_t*)0x401B8004)
#define GPIO1_PSR (*(volatile uint32_t*)0x401B8008)
#define GPIO1_ICR1 (*(volatile uint32_t*)0x401B800C)
#define GPIO1_ICR2 (*(volatile uint32_t*)0x401B8010)
#define GPIO1_IMR (*(volatile uint32_t*)0x401B8014)
#define GPIO1_ISR (*(volatile uint32_t*)0x401B8018)
#define GPIO1_EDGE_SEL (*(volatile uint32_t*)0x401B801C)

// GPT 定时器 1
#define GPT1_BASE 0x401EC000
#define GPT1_CR (*(volatile uint32_t*)0x401EC000)
#define GPT1_PR (*(volatile uint32_t*)0x401EC004)
#define GPT1_SR (*(volatile uint32_t*)0x401EC008)
#define GPT1_IR (*(volatile uint32_t*)0x401EC00C)
#define GPT1_OCR1 (*(volatile uint32_t*)0x401EC010)
#define GPT1_CNT (*(volatile uint32_t*)0x401EC024)

// USB OTG 1
#define USB1_BASE 0x402E0000
#define USB1_ID (*(volatile uint32_t*)0x402E0000)
#define USB1_OTGSC (*(volatile uint32_t*)0x402E000C)
#define USB1_USBCMD (*(volatile uint32_t*)0x402E0100)
#define USB1_PORTSC1 (*(volatile uint32_t*)0x402E0184)

void i_mx_rt1062_init(void);

#ifdef __cplusplus
}
#endif

#endif // I_MX_RT1062_HPP
