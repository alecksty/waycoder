// EFR32MG24 设备定义 - Objective-C 头文件
// 生成自: Silicon Labs/EFR32/EFR32MG24
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M33 MCU with 1536KB Flash, 256KB RAM, 78MHz, Zigbee/Thread/Matter
// CPU架构: ARM-Cortex-M33
// 位宽: 32位
// 时钟频率: 78000000 Hz

#ifndef EFR32MG24_DEVICE_H
#define EFR32MG24_DEVICE_H

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
#define FLASH_END 0x0817FFFF
#define FLASH_SIZE 1572864  // 
#define SRAM_START 0x20000000
#define SRAM_END 0x2003FFFF
#define SRAM_SIZE 262144  // 
#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x4007FFFF
#define PERIPHERAL_SIZE 524288  // 

// 外设定义
// Clock Management Unit
#define CMU_BASE 0x40080000
#define CMU_CTRL_ADDR 0x00
#define CMU_HFCORECLKCFG_ADDR 0x08
#define CMU_HFPERCLKEN0_ADDR 0x10
#define CMU_HFPERCLKEN0_GPIOEN_BIT 4  // GPIO clock enable
#define CMU_HFPERCLKEN0_USART0EN_BIT 12  // USART0 clock enable
#define CMU_HFPERCLKEN0_USART1EN_BIT 13  // USART1 clock enable
#define CMU_LFBCLKEN0_ADDR 0x20
// GPIO Controller
#define GPIO_BASE 0x40088000
#define GPIO_PORT_A_CTRL_ADDR 0x00
#define GPIO_PORT_B_CTRL_ADDR 0x04
#define GPIO_PORT_C_CTRL_ADDR 0x08
#define GPIO_PORT_D_CTRL_ADDR 0x0C
#define GPIO_MODEL_ADDR 0x10
#define GPIO_MODEH_ADDR 0x14
#define GPIO_DOUT_ADDR 0x1C
#define GPIO_DOUTSET_ADDR 0x20
#define GPIO_DOUTCLR_ADDR 0x24
#define GPIO_DOUTTGL_ADDR 0x28
#define GPIO_DIN_ADDR 0x2C
// GPIO Port A extended
#define GPIO_PA_BASE 0x40088400
#define GPIO_PA_PA_CFG_ADDR 0x00
#define GPIO_PA_PA_PINOUT_ADDR 0x04
// GPIO Port B extended
#define GPIO_PB_BASE 0x40088800
#define GPIO_PB_PB_CFG_ADDR 0x00
// USART 0
#define USART0_BASE 0x40060000
#define USART0_CTRL_ADDR 0x00
#define USART0_CMD_ADDR 0x04
#define USART0_STATUS_ADDR 0x08
#define USART0_RXDATA_ADDR 0x0C
#define USART0_TXDATA_ADDR 0x10
#define USART0_CLKDIV_ADDR 0x14

// 中断向量定义
#define INT_RESET 0  // 
#define INT_SVCALL 11  // 
#define INT_USART0_RX 12  // USART0 Receive Interrupt
#define INT_USART0_TX 13  // USART0 Transmit Interrupt

#endif /* EFR32MG24_DEVICE_H */
