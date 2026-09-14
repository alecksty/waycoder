// Macintosh-128K 设备定义 - Objective-C 头文件
// 生成自: Apple Computer/Macintosh/Macintosh-128K
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Apple Macintosh 128K - First Macintosh - Motorola 68000, 128KB RAM, 512x342 display
// CPU架构: MC68000
// 位宽: 32位
// 时钟频率: 7833600 Hz

#ifndef MACINTOSH-128K_DEVICE_H
#define MACINTOSH-128K_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define D0_ADDR 0x00  // Data Register 0
#define D1_ADDR 0x04  // Data Register 1
#define D2_ADDR 0x08  // Data Register 2
#define D3_ADDR 0x0C  // Data Register 3
#define D4_ADDR 0x10  // Data Register 4
#define D5_ADDR 0x14  // Data Register 5
#define D6_ADDR 0x18  // Data Register 6
#define D7_ADDR 0x1C  // Data Register 7
#define A0_ADDR 0x20  // Address Register 0
#define A1_ADDR 0x24  // Address Register 1
#define A2_ADDR 0x28  // Address Register 2
#define A3_ADDR 0x2C  // Address Register 3
#define A4_ADDR 0x30  // Address Register 4
#define A5_ADDR 0x34  // Address Register 5
#define A6_ADDR 0x38  // Address Register 6
#define A7_ADDR 0x3C  // Stack Pointer (USP)
#define PC_ADDR 0x40  // Program Counter
#define SR_ADDR 0x44  // Status Register
#define SR_C_BIT 0  // Carry
#define SR_V_BIT 1  // Overflow
#define SR_Z_BIT 2  // Zero
#define SR_N_BIT 3  // Negative
#define SR_X_BIT 4  // Extend
#define SR_I0_BIT 8  // Interrupt Mask 0
#define SR_I1_BIT 9  // Interrupt Mask 1
#define SR_I2_BIT 10  // Interrupt Mask 2
#define SR_S_BIT 13  // Supervisor/User
#define SR_T0_BIT 14  // Trace Mode 0
#define SR_T1_BIT 15  // Trace Mode 1

// 内存段定义
#define RAM_START 0x000000
#define RAM_END 0x01FFFF
#define RAM_SIZE 131072  // Main RAM (128KB unified)
#define ROM_START 0x40000000
#define ROM_END 0x4001FFFF
#define ROM_SIZE 131072  // Mac ROM (128KB)
#define FRAMEBUFFER_START 0x00400000
#define FRAMEBUFFER_END 0x00400555
#define FRAMEBUFFER_SIZE 1366  // Screen bitmap (512x342x1 = 21792 bytes)
#define FRAMEBUFFER2_START 0x00410000
#define FRAMEBUFFER2_END 0x00410555
#define FRAMEBUFFER2_SIZE 1366  // Shadow screen (double-buffering)
#define VIA_START 0x00E00000
#define VIA_END 0x00E0FFFF
#define VIA_SIZE 4096  // VIA 6522 (I/O)
#define SCC_START 0x00F00000
#define SCC_END 0x00F0FFFF
#define SCC_SIZE 4096  // SCC 8530 (serial)
#define ADB_START 0x01600000
#define ADB_END 0x0160FFFF
#define ADB_SIZE 4096  // ADB bus
#define IWM_START 0x01E00000
#define IWM_END 0x01E0FFFF
#define IWM_SIZE 4096  // IWM floppy controller

// 外设定义
// Versatile Interface Adapter 6522
#define VIA_BASE 0xE00000
#define VIA_ORB_ADDR 0xE00000
#define VIA_ORA_ADDR 0xE00002
#define VIA_DDRB_ADDR 0xE00004
#define VIA_DDRA_ADDR 0xE00006
#define VIA_T1C_L_ADDR 0xE00008
#define VIA_T1C_H_ADDR 0xE0000A
#define VIA_T1L_L_ADDR 0xE0000C
#define VIA_T1L_H_ADDR 0xE0000E
#define VIA_T2C_L_ADDR 0xE00010
#define VIA_T2C_H_ADDR 0xE00012
#define VIA_SR_ADDR 0xE00014
#define VIA_ACR_ADDR 0xE00016
#define VIA_PCR_ADDR 0xE00018
#define VIA_IFR_ADDR 0xE0001E
#define VIA_IER_ADDR 0xE0001E
// SCC 8530 Serial Communications Controller
#define SCC_BASE 0xF00000
#define SCC_SCC_CHA_B_ADDR 0xF00000
#define SCC_SCC_CHA_C_ADDR 0xF00002
#define SCC_SCC_CHB_D_ADDR 0xF00004
#define SCC_SCC_CHB_CT_ADDR 0xF00006
// Integrated Woz Machine - Floppy Disk Controller
#define IWM_BASE 0x1E00000
#define IWM_IWM_DATA_ADDR 0x1E00000
#define IWM_IWM_MODE_ADDR 0x1E00008
#define IWM_IWM_Q6L_ADDR 0x1E00020
#define IWM_IWM_Q7L_ADDR 0x1E00022
#define IWM_IWM_Q6R_ADDR 0x1E00024
#define IWM_IWM_Q7R_ADDR 0x1E00026
// Video Graphics Controller (custom Apple chip)
#define VGC_BASE 0x00F20000
#define VGC_VGC_MODE_ADDR 0x00F20000
#define VGC_VGC_START_HI_ADDR 0x00F20002
#define VGC_VGC_START_LO_ADDR 0x00F20004
// Apple Desktop Bus
#define ADB_BASE 0x01600000
#define ADB_ADB_DATA_ADDR 0x01600000
#define ADB_ADB_STATUS_ADDR 0x01600004
#define ADB_ADB_CMD_ADDR 0x01600008

// 中断向量定义
#define INT_RESET 1  // Reset Initial SP
#define INT_RESET_PC 2  // Reset Initial PC
#define INT_IRQ1 24  // VIA interrupt (level 1)
#define INT_IRQ2 25  // SCC interrupt (level 2)
#define INT_IRQ3 26  // ADB / VIA (level 3)
#define INT_IRQ4 27  // ADB / VIA (level 4)

// 引脚定义
#define PIN_VCC 1  // +5V Power
#define PIN_GND 2  // Ground
#define PIN_CLK 3  // 16MHz master clock / 7.83MHz CPU clock
#define PIN_FC0 4  // Function Code 0
#define PIN_FC1 5  // Function Code 1
#define PIN_FC2 6  // Function Code 2
#define PIN_AS 7  // Address Strobe
#define PIN_UDS 8  // Upper Data Strobe
#define PIN_LDS 9  // Lower Data Strobe
#define PIN_RWB 10  // Read/Write
#define PIN_DTACK 11  // Data Acknowledge
#define PIN_BERR 12  // Bus Error
#define PIN_BR 13  // Bus Request
#define PIN_BG 14  // Bus Grant
#define PIN_BGACK 15  // Bus Grant Acknowledge
#define PIN_IPL0 16  // Interrupt Priority 0
#define PIN_IPL1 17  // Interrupt Priority 1
#define PIN_IPL2 18  // Interrupt Priority 2
#define PIN_RESET 19  // Reset
#define PIN_HALT 20  // Halt
#define PIN_A1_A23 21  // Address Bus (24-bit)
#define PIN_D0_D15 22  // Data Bus (16-bit)

#endif /* MACINTOSH-128K_DEVICE_H */
