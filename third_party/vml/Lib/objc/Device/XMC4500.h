// XMC4500 设备定义 - Objective-C 头文件
// 生成自: Infineon/XMC4000/XMC4500
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M4 Industrial MCU with 1MB Flash, 160KB RAM, 120MHz
// CPU架构: ARM-Cortex-M4
// 位宽: 32位
// 时钟频率: 120000000 Hz

#ifndef XMC4500_DEVICE_H
#define XMC4500_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define R0_ADDR 0x00  // 
#define R1_ADDR 0x04  // 
#define R2_ADDR 0x08  // 
#define R3_ADDR 0x0C  // 
#define R4_ADDR 0x10  // 
#define R5_ADDR 0x14  // 
#define SP_ADDR 0x34  // 
#define LR_ADDR 0x38  // 
#define PC_ADDR 0x3C  // 

// 内存段定义
#define FLASH_START 0x08000000
#define FLASH_END 0x080FFFFF
#define FLASH_SIZE 1048576  // 
#define SRAM_START 0x1FF00000
#define SRAM_END 0x1FF0FFFF
#define SRAM_SIZE 65536  // 
#define SRAM_COM_START 0x20000000
#define SRAM_COM_END 0x20007FFF
#define SRAM_COM_SIZE 32768  // Communication Memory
#define SRAM_CPU_START 0x20010000
#define SRAM_CPU_END 0x2001FFFF
#define SRAM_CPU_SIZE 65536  // CPU SRAM
#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x4FFFFFFF
#define PERIPHERAL_SIZE 268435456  // 

// 外设定义
// System Control Unit
#define SCU_BASE 0x40020000
#define SCU_CLKCR_ADDR 0x00
#define SCU_CLKCR_PCLK_SEL_BIT 0  // CPU clock selection
#define SCU_CLKCR_FBKDIV_BIT 16  // Feedback divider
#define SCU_PLLCONFIG_ADDR 0x04
#define SCU_OSCHPCTRL_ADDR 0x08
#define SCU_CGATSET0_ADDR 0x20
#define SCU_CGATSET0_CG_GATE_GPIO_BIT 4  // GPIO gate enable
#define SCU_CGATCLR0_ADDR 0x24
// Port 0
#define PORT0_BASE 0x48000000
#define PORT0_OUT_ADDR 0x00
#define PORT0_OMR_ADDR 0x04
#define PORT0_IOCR0_ADDR 0x10
#define PORT0_IOCR4_ADDR 0x14
#define PORT0_IOCR8_ADDR 0x18
#define PORT0_IOCR12_ADDR 0x1C
#define PORT0_IN_ADDR 0x24
// Port 1
#define PORT1_BASE 0x48010000
#define PORT1_OUT_ADDR 0x00
#define PORT1_OMR_ADDR 0x04
#define PORT1_IOCR0_ADDR 0x10
#define PORT1_IOCR4_ADDR 0x14
#define PORT1_IOCR8_ADDR 0x18
#define PORT1_IOCR12_ADDR 0x1C
#define PORT1_IN_ADDR 0x24
// Port 2
#define PORT2_BASE 0x48020000
#define PORT2_OUT_ADDR 0x00
#define PORT2_OMR_ADDR 0x04
#define PORT2_IOCR0_ADDR 0x10
#define PORT2_IOCR4_ADDR 0x14
#define PORT2_IN_ADDR 0x24
// Universal Serial Interface 0 (UART)
#define USIC0_BASE 0x48030000
#define USIC0_CCR_ADDR 0x00
#define USIC0_PCR_ADDR 0x04
#define USIC0_RBUF_ADDR 0x08
#define USIC0_TBUF_ADDR 0x0C
#define USIC0_BRG_ADDR 0x10

// 中断向量定义
#define INT_RESET 0  // 
#define INT_SVCALL 11  // 
#define INT_USIC0_SR0 12  // USIC0 Service Request 0

#endif /* XMC4500_DEVICE_H */
