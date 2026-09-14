// Macintosh-128K 设备定义 - Objective-C 头文件
// 生成自: Apple Computer/Macintosh/Macintosh-128K
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Original Macintosh 128K with Motorola 68000 CPU, 128KB RAM, and 9-inch monochrome display
// CPU架构: Motorola 68000
// 位宽: 32位
// 时钟频率: 7998000 Hz

#ifndef MACINTOSH-128K_DEVICE_H
#define MACINTOSH-128K_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define D0_ADDR 0  // Data Register 0
#define D1_ADDR 0  // Data Register 1
#define D2_ADDR 0  // Data Register 2
#define D3_ADDR 0  // Data Register 3
#define D4_ADDR 0  // Data Register 4
#define D5_ADDR 0  // Data Register 5
#define D6_ADDR 0  // Data Register 6
#define D7_ADDR 0  // Data Register 7
#define A0_ADDR 0  // Address Register 0
#define A1_ADDR 0  // Address Register 1
#define A2_ADDR 0  // Address Register 2
#define A3_ADDR 0  // Address Register 3
#define A4_ADDR 0  // Address Register 4
#define A5_ADDR 0  // Address Register 5
#define A6_ADDR 0  // Address Register 6
#define A7_ADDR 0  // Address Register 7 (SP)
#define PC_ADDR 0  // Program Counter
#define SR_ADDR 0  // Status Register

// 外设定义
// Versatile Interface Adapter (6522)
#define VIA_BASE 
#define VIA_VIA_ORB_ADDR 0xE80000
#define VIA_VIA_ORA_ADDR 0xE80001
#define VIA_VIA_DDRB_ADDR 0xE80002
#define VIA_VIA_DDRA_ADDR 0xE80003
#define VIA_VIA_T1CL_ADDR 0xE80004
#define VIA_VIA_T1CH_ADDR 0xE80005
#define VIA_VIA_T1LL_ADDR 0xE80006
#define VIA_VIA_T1LH_ADDR 0xE80007
#define VIA_VIA_T2CL_ADDR 0xE80008
#define VIA_VIA_T2CH_ADDR 0xE80009
#define VIA_VIA_SR_ADDR 0xE8000A
#define VIA_VIA_ACR_ADDR 0xE8000B
#define VIA_VIA_PCR_ADDR 0xE8000C
#define VIA_VIA_IFR_ADDR 0xE8000D
#define VIA_VIA_IER_ADDR 0xE8000E
#define VIA_VIA_ORA2_ADDR 0xE8000F
// Integrated Woz Machine (floppy controller)
#define IWM_BASE 
#define IWM_IWM_Q6_ADDR 0xD00000
#define IWM_IWM_Q7_ADDR 0xD00002
#define IWM_IWM_PH0_ADDR 0xD00004
#define IWM_IWM_PH1_ADDR 0xD00006
#define IWM_IWM_PH2_ADDR 0xD00008
#define IWM_IWM_PH3_ADDR 0xD0000A
// Zilog 8530 Serial Communications Controller
#define SCC_BASE 
#define SCC_SCC_CA_ADDR 0x500000
#define SCC_SCC_DA_ADDR 0x500002
#define SCC_SCC_CB_ADDR 0x500004
#define SCC_SCC_DB_ADDR 0x500006
// Built-in speaker
#define SOUND_BASE 
#define SOUND_SOUND_VOL_ADDR 0xE80100
#define SOUND_SOUND_FREQ_ADDR 0xE80102

// 中断向量定义
#define INT_RESET_SP 0  // Reset (Initial SP)
#define INT_RESET_PC 4  // Reset (Initial PC)
#define INT_AUTOVECTOR1 24  // Auto vector 1
#define INT_AUTOVECTOR2 25  // Auto vector 2
#define INT_AUTOVECTOR3 26  // Auto vector 3
#define INT_AUTOVECTOR4 27  // Auto vector 4
#define INT_AUTOVECTOR5 28  // Auto vector 5
#define INT_AUTOVECTOR6 29  // Auto vector 6
#define INT_AUTOVECTOR7 30  // Auto vector 7
#define INT_SPURIOUS 31  // Spurious interrupt

#endif /* MACINTOSH-128K_DEVICE_H */
