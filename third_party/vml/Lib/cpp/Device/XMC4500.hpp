#ifndef XMC4500_HPP
#define XMC4500_HPP

// XMC4500寄存器定义
// 生成自: Infineon/XMC4000/XMC4500
// 版本: 1.0
// 日期: 2026-04-28


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: ARM-Cortex-M4
// 位宽: 32位
// 时钟频率: 120000000 Hz

// 寄存器定义
#define R0 (*(volatile uint32_t*)0x00)

#define R1 (*(volatile uint32_t*)0x04)

#define R2 (*(volatile uint32_t*)0x08)

#define R3 (*(volatile uint32_t*)0x0C)

#define R4 (*(volatile uint32_t*)0x10)

#define R5 (*(volatile uint32_t*)0x14)

#define SP (*(volatile uint32_t*)0x34)

#define LR (*(volatile uint32_t*)0x38)

#define PC (*(volatile uint32_t*)0x3C)

// 内存段定义
#define FLASH_START 0x08000000
#define FLASH_END 0x080FFFFF
#define FLASH_SIZE 1048576

#define SRAM_START 0x1FF00000
#define SRAM_END 0x1FF0FFFF
#define SRAM_SIZE 65536

// Communication Memory
#define SRAM_COM_START 0x20000000
#define SRAM_COM_END 0x20007FFF
#define SRAM_COM_SIZE 32768

// CPU SRAM
#define SRAM_CPU_START 0x20010000
#define SRAM_CPU_END 0x2001FFFF
#define SRAM_CPU_SIZE 65536

#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x4FFFFFFF
#define PERIPHERAL_SIZE 268435456

// 外设定义
// System Control Unit
#define SCU_BASE 0x40020000
#define SCU_CLKCR (*(volatile uint32_t*)0x40020000)
#define SCU_CLKCR_PCLK_SEL 0  // CPU clock selection
#define SCU_CLKCR_FBKDIV 16  // Feedback divider
#define SCU_PLLCONFIG (*(volatile uint32_t*)0x40020004)
#define SCU_OSCHPCTRL (*(volatile uint32_t*)0x40020008)
#define SCU_CGATSET0 (*(volatile uint32_t*)0x40020020)
#define SCU_CGATSET0_CG_GATE_GPIO 4  // GPIO gate enable
#define SCU_CGATCLR0 (*(volatile uint32_t*)0x40020024)

// Port 0
#define PORT0_BASE 0x48000000
#define PORT0_OUT (*(volatile uint32_t*)0x48000000)
#define PORT0_OMR (*(volatile uint32_t*)0x48000004)
#define PORT0_IOCR0 (*(volatile uint32_t*)0x48000010)
#define PORT0_IOCR4 (*(volatile uint32_t*)0x48000014)
#define PORT0_IOCR8 (*(volatile uint32_t*)0x48000018)
#define PORT0_IOCR12 (*(volatile uint32_t*)0x4800001C)
#define PORT0_IN (*(volatile uint32_t*)0x48000024)

// Port 1
#define PORT1_BASE 0x48010000
#define PORT1_OUT (*(volatile uint32_t*)0x48010000)
#define PORT1_OMR (*(volatile uint32_t*)0x48010004)
#define PORT1_IOCR0 (*(volatile uint32_t*)0x48010010)
#define PORT1_IOCR4 (*(volatile uint32_t*)0x48010014)
#define PORT1_IOCR8 (*(volatile uint32_t*)0x48010018)
#define PORT1_IOCR12 (*(volatile uint32_t*)0x4801001C)
#define PORT1_IN (*(volatile uint32_t*)0x48010024)

// Port 2
#define PORT2_BASE 0x48020000
#define PORT2_OUT (*(volatile uint32_t*)0x48020000)
#define PORT2_OMR (*(volatile uint32_t*)0x48020004)
#define PORT2_IOCR0 (*(volatile uint32_t*)0x48020010)
#define PORT2_IOCR4 (*(volatile uint32_t*)0x48020014)
#define PORT2_IN (*(volatile uint32_t*)0x48020024)

// Universal Serial Interface 0 (UART)
#define USIC0_BASE 0x48030000
#define USIC0_CCR (*(volatile uint32_t*)0x48030000)
#define USIC0_PCR (*(volatile uint32_t*)0x48030004)
#define USIC0_RBUF (*(volatile uint32_t*)0x48030008)
#define USIC0_TBUF (*(volatile uint32_t*)0x4803000C)
#define USIC0_BRG (*(volatile uint32_t*)0x48030010)

// 中断向量定义
#define RESET_VECTOR 0  // 
#define SVCALL_VECTOR 11  // 
#define USIC0_SR0_VECTOR 12  // USIC0 Service Request 0

void xmc4500_init(void);

#ifdef __cplusplus
}
#endif

#endif // XMC4500_HPP
