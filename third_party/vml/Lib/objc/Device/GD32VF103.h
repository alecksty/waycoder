// GD32VF103 设备定义 - Objective-C 头文件
// 生成自: GigaDevice/GD32/GD32VF103
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit RISC-V RV32IMAC MCU with 128KB Flash, 32KB RAM, 108MHz, STM32F103 compatible
// CPU架构: RISC-V
// 位宽: 32位
// 时钟频率: 108000000 Hz

#ifndef GD32VF103_DEVICE_H
#define GD32VF103_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define X1_ADDR 0x04  // Return Address
#define X2_ADDR 0x08  // Stack Pointer (SP)
#define X3_ADDR 0x0C  // Global Pointer (GP)
#define X8_ADDR 0x20  // Frame Pointer (FP)
#define X10_ADDR 0x28  // Function Argument (A0)
#define X11_ADDR 0x2C  // Function Argument (A1)
#define PC_ADDR 0x3C  // Program Counter

// 内存段定义
#define FLASH_START 0x08000000
#define FLASH_END 0x0801FFFF
#define FLASH_SIZE 131072  // 
#define SRAM_START 0x20000000
#define SRAM_END 0x20007FFF
#define SRAM_SIZE 32768  // 
#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x4003FFFF
#define PERIPHERAL_SIZE 262144  // 

// 外设定义
// Reset and Clock Control
#define RCU_BASE 0x40021000
#define RCU_CTL_ADDR 0x00
#define RCU_CFG0_ADDR 0x04
#define RCU_CFG1_ADDR 0x08
#define RCU_APB2EN_ADDR 0x18
#define RCU_APB2EN_PAEN_BIT 2  // GPIOA enable
#define RCU_APB2EN_PBEN_BIT 3  // GPIOB enable
#define RCU_APB2EN_PCEN_BIT 4  // GPIOC enable
#define RCU_APB2EN_USART0EN_BIT 14  // USART0 enable
#define RCU_APB1EN_ADDR 0x1C
// General Purpose I/O Port A
#define GPIOA_BASE 0x40010800
#define GPIOA_CTL0_ADDR 0x00
#define GPIOA_CTL1_ADDR 0x04
#define GPIOA_ISTAT_ADDR 0x08
#define GPIOA_OCTL_ADDR 0x0C
#define GPIOA_BOP_ADDR 0x10
#define GPIOA_BC_ADDR 0x14
// General Purpose I/O Port B
#define GPIOB_BASE 0x40010C00
#define GPIOB_CTL0_ADDR 0x00
#define GPIOB_CTL1_ADDR 0x04
#define GPIOB_ISTAT_ADDR 0x08
#define GPIOB_OCTL_ADDR 0x0C
#define GPIOB_BOP_ADDR 0x10
#define GPIOB_BC_ADDR 0x14
// General Purpose I/O Port C
#define GPIOC_BASE 0x40011000
#define GPIOC_CTL0_ADDR 0x00
#define GPIOC_CTL1_ADDR 0x04
#define GPIOC_ISTAT_ADDR 0x08
#define GPIOC_OCTL_ADDR 0x0C
#define GPIOC_BOP_ADDR 0x10
#define GPIOC_BC_ADDR 0x14
// USART0
#define USART0_BASE 0x40013800
#define USART0_STATR_ADDR 0x00
#define USART0_DATAR_ADDR 0x04
#define USART0_BRR_ADDR 0x08
#define USART0_CTLR1_ADDR 0x0C

// 中断向量定义
#define INT_RESET 1  // 
#define INT_MACHINESOFTWARE 3  // 
#define INT_MACHINETIMER 7  // 
#define INT_MACHINEEXTERNAL 11  // 
#define INT_USART0 25  // USART0 Global Interrupt

#endif /* GD32VF103_DEVICE_H */
