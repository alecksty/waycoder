// Apple-IIe 设备定义 - Objective-C 头文件
// 生成自: Apple Computer/Apple II/Apple-IIe
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Apple II Enhanced - 8-bit personal computer with MOS 6502 CPU
// CPU架构: MOS-6502
// 位宽: 8位
// 时钟频率: 1021800 Hz

#ifndef APPLE-IIE_DEVICE_H
#define APPLE-IIE_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define A_ADDR 0x00  // Accumulator
#define X_ADDR 0x01  // X Index Register
#define Y_ADDR 0x02  // Y Index Register
#define SP_ADDR 0x03  // Stack Pointer
#define PC_ADDR 0x04  // Program Counter
#define P_ADDR 0x06  // Processor Status
#define P_C_BIT 0  // Carry Flag
#define P_Z_BIT 1  // Zero Flag
#define P_I_BIT 2  // Interrupt Disable
#define P_D_BIT 3  // Decimal Mode
#define P_B_BIT 4  // Break Command
#define P_U_BIT 5  // Unused
#define P_V_BIT 6  // Overflow Flag
#define P_N_BIT 7  // Negative Flag

// 内存段定义
#define MAIN_RAM_START 0x0000
#define MAIN_RAM_END 0xBFFF
#define MAIN_RAM_SIZE 49152  // Main RAM (48KB base, up to 64KB with slot RAM)
#define TEXT_RAM_START 0x0400
#define TEXT_RAM_END 0x07FF
#define TEXT_RAM_SIZE 1024  // Text screen buffer (40x24)
#define HIRES_RAM_START 0x2000
#define HIRES_RAM_END 0x5FFF
#define HIRES_RAM_SIZE 16384  // High-resolution graphics buffer
#define AUX_RAM_START 0x0400
#define AUX_RAM_END 0x09FF
#define AUX_RAM_SIZE 1536  // 80-column text auxiliary RAM
#define MONITOR_ROM_START 0xC100
#define MONITOR_ROM_END 0xCFFF
#define MONITOR_ROM_SIZE 3840  // Monitor ROM (applesoft/Integer)
#define BASIC_ROM_START 0xD000
#define BASIC_ROM_END 0xFFFF
#define BASIC_ROM_SIZE 12288  // Applesoft BASIC ROM
#define SLOT_ROM_START 0xC100
#define SLOT_ROM_END 0xC7FF
#define SLOT_ROM_SIZE 768  // Expansion Slot ROM
#define MMIO_START 0xC080
#define MMIO_END 0xC0FF
#define MMIO_SIZE 128  // I/O Select (slot space)

// 外设定义
// Versatile Interface Adapter (6522)
#define VIA_BASE 0xC000
#define VIA_ORB_ADDR 0xC000
#define VIA_ORA_ADDR 0xC001
#define VIA_DDRB_ADDR 0xC002
#define VIA_DDRA_ADDR 0xC003
#define VIA_T1C_ADDR 0xC004
#define VIA_T1L_ADDR 0xC006
#define VIA_T2C_ADDR 0xC008
#define VIA_SR_ADDR 0xC00A
#define VIA_ACR_ADDR 0xC00B
#define VIA_PCR_ADDR 0xC00C
#define VIA_IFG_ADDR 0xC00D
#define VIA_IER_ADDR 0xC00E
#define VIA_ORA_NH_ADDR 0xC00F
// Peripheral Interface Adapter (6520)
#define PIA_BASE 0xC010
#define PIA_PA_ADDR 0xC010
#define PIA_PB_ADDR 0xC011
#define PIA_DDRA_ADDR 0xC012
#define PIA_DDRB_ADDR 0xC013
#define PIA_CA1_ADDR 0xC014
#define PIA_CA2_ADDR 0xC015
#define PIA_CB1_ADDR 0xC016
#define PIA_CB2_ADDR 0xC017
// Keyboard (via PIA)
#define KBD_BASE 0xC000
#define KBD_KEYDATA_ADDR 0xC000
#define KBD_KEYSTROBE_ADDR 0xC010
#define KBD_KBDCTRL_ADDR 0xC025
#define KBD_KBDERR_ADDR 0xC026
// Speaker
#define SPEAKER_BASE 0xC030
#define SPEAKER_SPKR_ADDR 0xC030
// Game I/O Port
#define GAME_PORT_BASE 0xC050
#define GAME_PORT_GAME_SW0_ADDR 0xC061
#define GAME_PORT_GAME_SW1_ADDR 0xC062
#define GAME_PORT_GAME_AN0_ADDR 0xC064
#define GAME_PORT_GAME_AN1_ADDR 0xC065
#define GAME_PORT_GAME_AN2_ADDR 0xC066
#define GAME_PORT_GAME_AN3_ADDR 0xC067
#define GAME_PORT_GAME_TRIG_ADDR 0xC070
// Disk II Controller
#define DISKII_BASE 0xC0E0
#define DISKII_PHASE0_ADDR 0xC0E0
#define DISKII_PHASE1_ADDR 0xC0E1
#define DISKII_PHASE2_ADDR 0xC0E2
#define DISKII_PHASE3_ADDR 0xC0E3
#define DISKII_Q6L_ADDR 0xC0EC
#define DISKII_Q7L_ADDR 0xC0ED
#define DISKII_Q6R_ADDR 0xC0EE
#define DISKII_Q7R_ADDR 0xC0EF
// Video Display Generator
#define VIDEO_BASE 0xC050
#define VIDEO_TXTCLR_ADDR 0xC050
#define VIDEO_MIXCLR_ADDR 0xC051
#define VIDEO_TXTPAGE2_ADDR 0xC054
#define VIDEO_TXTPAGE1_ADDR 0xC055
#define VIDEO_LORES_ADDR 0xC056
#define VIDEO_HIRES_ADDR 0xC057
#define VIDEO_DHIRESON_ADDR 0xC05E
#define VIDEO_AN0_ADDR 0xC058
#define VIDEO_AN1_ADDR 0xC059
#define VIDEO_AN2_ADDR 0xC05A
#define VIDEO_AN3_ADDR 0xC05B
#define VIDEO__80STORE_ADDR 0xC000
// RAM Read/Write Control
#define RAMRD_BASE 0xC080
#define RAMRD_INTCXROM_ADDR 0xCFFF

// 中断向量定义
#define INT_RESET 0  // Power-on Reset
#define INT_NMI 1  // Non-Maskable Interrupt (from VIA)
#define INT_IRQ 2  // IRQ from VIA/timer/slot
#define INT_BRK 3  // BRK Instruction

// 引脚定义
#define PIN_VCC 1  // +5V Power
#define PIN_GND 2  // Ground
#define PIN_RESET 3  // System Reset
#define PIN_CLK 4  // System Clock (1.023MHz NTSC)
#define PIN_RDY 5  // CPU Ready
#define PIN_NMI 6  // Non-Maskable Interrupt
#define PIN_IRQ 7  // Interrupt Request
#define PIN_SO 8  // Set Overflow
#define PIN_RWB 9  // Read/Write Bar
#define PIN_SYNC 10  // Instruction Sync
#define PIN_A0_A15 11  // Address Bus (16-bit)
#define PIN_D0_D7 12  // Data Bus (8-bit)
#define PIN_PHASE0 13  // Phase 0 (4MHz system)
#define PIN_PHASE1 14  // Phase 1
#define PIN_PHASE2 15  // Phase 2
#define PIN_PHASE3 16  // Phase 3

#endif /* APPLE-IIE_DEVICE_H */
