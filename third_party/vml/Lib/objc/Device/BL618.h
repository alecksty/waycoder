// BL618 设备定义 - Objective-C 头文件
// 生成自: Bouffalo Lab/BL6/BL618
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit RISC-V RV32IMAFC WiFi6 + BLE SoC with 4MB Flash, 512KB SRAM, 480MHz
// CPU架构: RISC-V
// 位宽: 32位
// 时钟频率: 320000000 Hz

#ifndef BL618_DEVICE_H
#define BL618_DEVICE_H

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
#define FLASH_START 0x20000000
#define FLASH_END 0x203FFFFF
#define FLASH_SIZE 4194304  // 
#define SRAM_HPSYS_START 0x22000000
#define SRAM_HPSYS_END 0x22003FFF
#define SRAM_HPSYS_SIZE 16384  // 
#define SRAM_DTCM_START 0x22010000
#define SRAM_DTCM_END 0x22017FFF
#define SRAM_DTCM_SIZE 32768  // DTCM
#define SRAM_SYS_START 0x22020000
#define SRAM_SYS_END 0x2208FFFF
#define SRAM_SYS_SIZE 458752  // 
#define PERIPHERAL_START 0x30000000
#define PERIPHERAL_END 0x300FFFFF
#define PERIPHERAL_SIZE 1048576  // 

// 外设定义
// Global Control (Clock and Reset)
#define GLB_BASE 0x30000000
#define GLB_GLB_CLK_EN_ADDR 0x10
#define GLB_GLB_CLK_EN_GPIO_CLK_EN_BIT 6  // GPIO clock enable
#define GLB_GLB_CLK_EN_UART0_CLK_EN_BIT 12  // UART0 clock enable
#define GLB_GLB_SYS_CLK_CTRL_ADDR 0x14
#define GLB_GLB_PLL_CTRL_ADDR 0x1C
// GPIO Port A
#define GPIO_P0_BASE 0x30007000
#define GPIO_P0_GPIO_CFG0_ADDR 0x00
#define GPIO_P0_GPIO_CFG1_ADDR 0x04
#define GPIO_P0_GPIO_OE_ADDR 0x08
#define GPIO_P0_GPIO_OUT_ADDR 0x0C
#define GPIO_P0_GPIO_IN_ADDR 0x10
#define GPIO_P0_GPIO_SET_ADDR 0x14
#define GPIO_P0_GPIO_CLR_ADDR 0x18
#define GPIO_P0_GPIO_TOG_ADDR 0x1C
// GPIO Port B
#define GPIO_P1_BASE 0x30007200
#define GPIO_P1_GPIO_CFG0_ADDR 0x00
#define GPIO_P1_GPIO_CFG1_ADDR 0x04
#define GPIO_P1_GPIO_OE_ADDR 0x08
#define GPIO_P1_GPIO_OUT_ADDR 0x0C
#define GPIO_P1_GPIO_IN_ADDR 0x10
#define GPIO_P1_GPIO_SET_ADDR 0x14
#define GPIO_P1_GPIO_CLR_ADDR 0x18
#define GPIO_P1_GPIO_TOG_ADDR 0x1C
// UART 0
#define UART0_BASE 0x30002000
#define UART0_UART_CR_ADDR 0x00
#define UART0_UART_BRR_ADDR 0x04
#define UART0_UART_TDR_ADDR 0x08
#define UART0_UART_RDR_ADDR 0x0C
#define UART0_UART_SR_ADDR 0x10

// 中断向量定义
#define INT_RESET 1  // 
#define INT_MACHINESOFTWARE 3  // 
#define INT_MACHINETIMER 7  // 
#define INT_MACHINEEXTERNAL 11  // 
#define INT_UART0 20  // UART0 Interrupt

#endif /* BL618_DEVICE_H */
