// Commodore-PET 设备定义 - Objective-C 头文件
// 生成自: Commodore International/PET/Commodore-PET
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Commodore PET 2001 personal computer with MOS 6502 CPU and built-in monitor
// CPU架构: MOS 6502
// 位宽: 8位
// 时钟频率: 1000000 Hz

#ifndef COMMODORE-PET_DEVICE_H
#define COMMODORE-PET_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define A_ADDR 0  // Accumulator
#define X_ADDR 0  // Index Register X
#define Y_ADDR 0  // Index Register Y
#define SP_ADDR 0  // Stack Pointer
#define PC_ADDR 0  // Program Counter
#define P_ADDR 0  // Status Register

// 外设定义
// Peripheral Interface Adapter 1 (6520)
#define PIA1_BASE 
#define PIA1_PIA1_DDRA_ADDR 0xE810
#define PIA1_PIA1_ORA_ADDR 0xE811
#define PIA1_PIA1_DDRB_ADDR 0xE812
#define PIA1_PIA1_ORB_ADDR 0xE813
#define PIA1_PIA1_CRA_ADDR 0xE814
#define PIA1_PIA1_CRB_ADDR 0xE815
// Peripheral Interface Adapter 2 (6520)
#define PIA2_BASE 
#define PIA2_PIA2_DDRA_ADDR 0xE820
#define PIA2_PIA2_ORA_ADDR 0xE821
#define PIA2_PIA2_DDRB_ADDR 0xE822
#define PIA2_PIA2_ORB_ADDR 0xE823
#define PIA2_PIA2_CRA_ADDR 0xE824
#define PIA2_PIA2_CRB_ADDR 0xE825
// Versatile Interface Adapter (6522)
#define VIA_BASE 
#define VIA_VIA_ORB_ADDR 0xE840
#define VIA_VIA_ORA_ADDR 0xE841
#define VIA_VIA_DDRB_ADDR 0xE842
#define VIA_VIA_DDRA_ADDR 0xE843
#define VIA_VIA_T1CL_ADDR 0xE844
#define VIA_VIA_T1CH_ADDR 0xE845
#define VIA_VIA_T1LL_ADDR 0xE846
#define VIA_VIA_T1LH_ADDR 0xE847
#define VIA_VIA_T2CL_ADDR 0xE848
#define VIA_VIA_T2CH_ADDR 0xE849
#define VIA_VIA_SR_ADDR 0xE84A
#define VIA_VIA_ACR_ADDR 0xE84B
#define VIA_VIA_PCR_ADDR 0xE84C
#define VIA_VIA_IFR_ADDR 0xE84D
#define VIA_VIA_IER_ADDR 0xE84E
// CRT Controller (6545)
#define CRTC_BASE 
#define CRTC_CRTC_ADDR_ADDR 0xE880
#define CRTC_CRTC_DATA_ADDR 0xE881
// Cassette tape interface
#define CASSETTE_BASE 
#define CASSETTE_CASS_MOTOR_ADDR 0xE840
#define CASSETTE_CASS_WRITE_ADDR 0xE842
#define CASSETTE_CASS_READ_ADDR 0xE812
// IEEE-488 bus interface
#define IEEE488_BASE 
#define IEEE488_IEEE_DATA_ADDR 0xE801
#define IEEE488_IEEE_STATUS_ADDR 0xE802
#define IEEE488_IEEE_CONTROL_ADDR 0xE803

// 中断向量定义
#define INT_NMI 65526  // Non-maskable interrupt
#define INT_RESET 65528  // Reset vector
#define INT_IRQ 65530  // Interrupt request
#define INT_BRK 65532  // Break instruction

#endif /* COMMODORE-PET_DEVICE_H */
