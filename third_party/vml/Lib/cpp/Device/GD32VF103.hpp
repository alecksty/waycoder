#ifndef GD32VF103_HPP
#define GD32VF103_HPP

// GD32VF103寄存器定义
// 生成自: GigaDevice/GD32/GD32VF103
// 版本: 1.0
// 日期: 2026-04-28


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: RISC-V
// 位宽: 32位
// 时钟频率: 108000000 Hz

// 寄存器定义
// Return Address
#define X1 (*(volatile uint32_t*)0x04)

// Stack Pointer (SP)
#define X2 (*(volatile uint32_t*)0x08)

// Global Pointer (GP)
#define X3 (*(volatile uint32_t*)0x0C)

// Frame Pointer (FP)
#define X8 (*(volatile uint32_t*)0x20)

// Function Argument (A0)
#define X10 (*(volatile uint32_t*)0x28)

// Function Argument (A1)
#define X11 (*(volatile uint32_t*)0x2C)

// Program Counter
#define PC (*(volatile uint32_t*)0x3C)

// 内存段定义
#define FLASH_START 0x08000000
#define FLASH_END 0x0801FFFF
#define FLASH_SIZE 131072

#define SRAM_START 0x20000000
#define SRAM_END 0x20007FFF
#define SRAM_SIZE 32768

#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x4003FFFF
#define PERIPHERAL_SIZE 262144

// 外设定义
// Reset and Clock Control
#define RCU_BASE 0x40021000
#define RCU_CTL (*(volatile uint32_t*)0x40021000)
#define RCU_CFG0 (*(volatile uint32_t*)0x40021004)
#define RCU_CFG1 (*(volatile uint32_t*)0x40021008)
#define RCU_APB2EN (*(volatile uint32_t*)0x40021018)
#define RCU_APB2EN_PAEN 2  // GPIOA enable
#define RCU_APB2EN_PBEN 3  // GPIOB enable
#define RCU_APB2EN_PCEN 4  // GPIOC enable
#define RCU_APB2EN_USART0EN 14  // USART0 enable
#define RCU_APB1EN (*(volatile uint32_t*)0x4002101C)

// General Purpose I/O Port A
#define GPIOA_BASE 0x40010800
#define GPIOA_CTL0 (*(volatile uint32_t*)0x40010800)
#define GPIOA_CTL1 (*(volatile uint32_t*)0x40010804)
#define GPIOA_ISTAT (*(volatile uint32_t*)0x40010808)
#define GPIOA_OCTL (*(volatile uint32_t*)0x4001080C)
#define GPIOA_BOP (*(volatile uint32_t*)0x40010810)
#define GPIOA_BC (*(volatile uint32_t*)0x40010814)

// General Purpose I/O Port B
#define GPIOB_BASE 0x40010C00
#define GPIOB_CTL0 (*(volatile uint32_t*)0x40010C00)
#define GPIOB_CTL1 (*(volatile uint32_t*)0x40010C04)
#define GPIOB_ISTAT (*(volatile uint32_t*)0x40010C08)
#define GPIOB_OCTL (*(volatile uint32_t*)0x40010C0C)
#define GPIOB_BOP (*(volatile uint32_t*)0x40010C10)
#define GPIOB_BC (*(volatile uint32_t*)0x40010C14)

// General Purpose I/O Port C
#define GPIOC_BASE 0x40011000
#define GPIOC_CTL0 (*(volatile uint32_t*)0x40011000)
#define GPIOC_CTL1 (*(volatile uint32_t*)0x40011004)
#define GPIOC_ISTAT (*(volatile uint32_t*)0x40011008)
#define GPIOC_OCTL (*(volatile uint32_t*)0x4001100C)
#define GPIOC_BOP (*(volatile uint32_t*)0x40011010)
#define GPIOC_BC (*(volatile uint32_t*)0x40011014)

// USART0
#define USART0_BASE 0x40013800
#define USART0_STATR (*(volatile uint32_t*)0x40013800)
#define USART0_DATAR (*(volatile uint32_t*)0x40013804)
#define USART0_BRR (*(volatile uint32_t*)0x40013808)
#define USART0_CTLR1 (*(volatile uint32_t*)0x4001380C)

// 中断向量定义
#define RESET_VECTOR 1  // 
#define MACHINESOFTWARE_VECTOR 3  // 
#define MACHINETIMER_VECTOR 7  // 
#define MACHINEEXTERNAL_VECTOR 11  // 
#define USART0_VECTOR 25  // USART0 Global Interrupt

void gd32vf103_init(void);

#ifdef __cplusplus
}
#endif

#endif // GD32VF103_HPP
