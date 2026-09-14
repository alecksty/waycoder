#ifndef RA4M2_HPP
#define RA4M2_HPP

// RA4M2寄存器定义
// 生成自: Renesas/RA/RA4M2
// 版本: 1.0
// 日期: 2026-04-28


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: ARM-Cortex-M4
// 位宽: 32位
// 时钟频率: 100000000 Hz

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
#define FLASH_START 0x00000000
#define FLASH_END 0x0003FFFF
#define FLASH_SIZE 262144

// SRAM0
#define SRAM_START 0x1FFE0000
#define SRAM_END 0x1FFE7FFF
#define SRAM_SIZE 32768

// SRAM1
#define SRAM1_START 0x20000000
#define SRAM1_END 0x20017FFF
#define SRAM1_SIZE 98304

#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x400FFFFF
#define PERIPHERAL_SIZE 1048576

// 外设定义
// Module Stop Control
#define MSTP_BASE 0x40020000
#define MSTP_MSTPCR_A (*(volatile uint32_t*)0x40020020)
#define MSTP_MSTPCR_A_MSTP41 9  // GPIO A stop
#define MSTP_MSTPCR_A_MSTP42 10  // GPIO B stop
#define MSTP_MSTPCR_B (*(volatile uint32_t*)0x40020024)
#define MSTP_MSTPCR_C (*(volatile uint32_t*)0x40020028)
#define MSTP_MSTPCR_D (*(volatile uint32_t*)0x4002002C)

// Interrupt Controller Unit
#define ICU_BASE 0x40030000
#define ICU_IRQCR0 (*(volatile uint16_t*)0x40030600)
#define ICU_IRQCR1 (*(volatile uint16_t*)0x40030602)

// General Purpose I/O Port A
#define GPIOA_BASE 0x40040000
#define GPIOA_PDR (*(volatile uint16_t*)0x40040000)
#define GPIOA_PODR (*(volatile uint16_t*)0x40040004)
#define GPIOA_PIDR (*(volatile uint16_t*)0x40040008)
#define GPIOA_PMR (*(volatile uint16_t*)0x40040010)
#define GPIOA_PCR (*(volatile uint32_t*)0x40040018)

// General Purpose I/O Port B
#define GPIOB_BASE 0x40040020
#define GPIOB_PDR (*(volatile uint16_t*)0x40040020)
#define GPIOB_PODR (*(volatile uint16_t*)0x40040024)
#define GPIOB_PIDR (*(volatile uint16_t*)0x40040028)
#define GPIOB_PMR (*(volatile uint16_t*)0x40040030)

// SCI UART 0
#define SCIUART0_BASE 0x40070000
#define SCIUART0_SCR (*(volatile uint8_t*)0x40070000)
#define SCIUART0_BRR (*(volatile uint8_t*)0x40070004)
#define SCIUART0_TDR (*(volatile uint8_t*)0x40070008)
#define SCIUART0_RDR (*(volatile uint8_t*)0x4007000C)
#define SCIUART0_SSR (*(volatile uint8_t*)0x40070010)

// 中断向量定义
#define RESET_VECTOR 0  // 
#define SVCALL_VECTOR 11  // 
#define SCIUART0_RXI_VECTOR 24  // SCI UART0 Receive Interrupt
#define SCIUART0_TXI_VECTOR 25  // SCI UART0 Transmit Interrupt

void ra4m2_init(void);

#ifdef __cplusplus
}
#endif

#endif // RA4M2_HPP
