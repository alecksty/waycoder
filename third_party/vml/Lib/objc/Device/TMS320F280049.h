// TMS320F280049 设备定义 - Objective-C 头文件
// 生成自: Texas Instruments/C2000/TMS320F280049
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit C28x DSP + CLA MCU with 256KB Flash, 100KB RAM, 100MHz
// CPU架构: C28x-DSP
// 位宽: 32位
// 时钟频率: 100000000 Hz

#ifndef TMS320F280049_DEVICE_H
#define TMS320F280049_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define AL_ADDR 0x00  // Accumulator Low
#define AH_ADDR 0x02  // Accumulator High
#define PH_ADDR 0x04  // Product High
#define PL_ADDR 0x06  // Product Low
#define TREG_ADDR 0x08  // Temporary Register
#define AR0_ADDR 0x0A  // 
#define AR1_ADDR 0x0C  // 
#define ST0_ADDR 0x20  // Status 0
#define ST1_ADDR 0x22  // Status 1
#define PC_ADDR 0x24  // Program Counter
#define SP_ADDR 0x26  // Stack Pointer

// 内存段定义
#define FLASH_START 0x080000
#define FLASH_END 0x0BFFFF
#define FLASH_SIZE 262144  // 
#define SRAM_LS_START 0x008000
#define SRAM_LS_END 0x00BFFF
#define SRAM_LS_SIZE 16384  // Local Shared RAM
#define SRAM_GS_START 0x00C000
#define SRAM_GS_END 0x01FFFF
#define SRAM_GS_SIZE 81920  // Global Shared RAM
#define PERIPHERAL_START 0x400000
#define PERIPHERAL_END 0x40FFFF
#define PERIPHERAL_SIZE 65536  // 

// 外设定义
// PLL Clock Control
#define PLL_BASE 0x5C10
#define PLL_SYSPLLCTL1_ADDR 0x00
#define PLL_SYSPLLCTL2_ADDR 0x02
#define PLL_CLKSRCCTL1_ADDR 0x04
#define PLL_CLKSRCCTL2_ADDR 0x06
// GPIO Control Registers
#define GPIO_CTRL_BASE 0x7C00
#define GPIO_CTRL_GPACTRL_ADDR 0x00
#define GPIO_CTRL_GPAQSEL1_ADDR 0x02
#define GPIO_CTRL_GPAQSEL2_ADDR 0x04
#define GPIO_CTRL_GPAMUX1_ADDR 0x06
#define GPIO_CTRL_GPAMUX2_ADDR 0x08
#define GPIO_CTRL_GPADIR_ADDR 0x0A
#define GPIO_CTRL_GPAPUD_ADDR 0x0C
// GPIO Data Registers
#define GPIO_DATA_BASE 0x7F00
#define GPIO_DATA_GPADAT_ADDR 0x00
#define GPIO_DATA_GPASET_ADDR 0x02
#define GPIO_DATA_GPACLEAR_ADDR 0x04
#define GPIO_DATA_GPATOGGLE_ADDR 0x06
#define GPIO_DATA_GPBDAT_ADDR 0x08
#define GPIO_DATA_GPBSET_ADDR 0x0A
#define GPIO_DATA_GPBCLEAR_ADDR 0x0C
#define GPIO_DATA_GPBTOGGLE_ADDR 0x0E
// GPIO B Control
#define GPIO_B_CTRL_BASE 0x7C20
#define GPIO_B_CTRL_GPBMUX1_ADDR 0x00
#define GPIO_B_CTRL_GPBMUX2_ADDR 0x02
#define GPIO_B_CTRL_GPBDIR_ADDR 0x04
#define GPIO_B_CTRL_GPBPUD_ADDR 0x06
// SCI-A UART
#define SCI_A_BASE 0x7320
#define SCI_A_SCICCR_ADDR 0x00
#define SCI_A_SCICTL1_ADDR 0x02
#define SCI_A_SCIBAUD_ADDR 0x04
#define SCI_A_SCIRXBUF_ADDR 0x0A
#define SCI_A_SCITXBUF_ADDR 0x0C

// 中断向量定义
#define INT_RESET 1  // 
#define INT_SCIA_RX 8  // SCI-A Receive Interrupt
#define INT_SCIA_TX 9  // SCI-A Transmit Interrupt

#endif /* TMS320F280049_DEVICE_H */
