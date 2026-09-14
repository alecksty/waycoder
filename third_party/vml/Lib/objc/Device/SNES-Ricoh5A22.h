// Ricoh-5A22 设备定义 - Objective-C 头文件
// 生成自: Ricoh/MOS-6502/Ricoh-5A22
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: Super Nintendo Entertainment System (SNES) main processor - 16-bit 6502 variant with enhanced capabilities
// CPU架构: Ricoh-5A22
// 位宽: 16位
// 时钟频率: 3580000 Hz

#ifndef RICOH-5A22_DEVICE_H
#define RICOH-5A22_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define A_ADDR 0x00  // Accumulator (8-bit, expandable to 16-bit)
#define B_ADDR 0x01  // Accumulator high byte when 16-bit
#define X_ADDR 0x02  // X Index Register (8/16-bit)
#define Y_ADDR 0x03  // Y Index Register (8/16-bit)
#define SP_ADDR 0x04  // Stack Pointer (8-bit, banked)
#define PC_ADDR 0x06  // Program Counter (16-bit)
#define D_ADDR 0x08  // Direct Page Register
#define P_ADDR 0x0A  // Processor Status
#define P_N_BIT 0  // Negative
#define P_V_BIT 1  // Overflow
#define P_M_BIT 2  // Memory/Accumulator Select (0=16-bit, 1=8-bit)
#define P_X_BIT 3  // Index Select (0=16-bit, 1=8-bit)
#define P_B_BIT 4  // Break
#define P_D_BIT 5  // Decimal Mode
#define P_I_BIT 6  // Interrupt Disable
#define P_Z_BIT 7  // Zero
#define P_C_BIT 8  // Carry

// 内存段定义
#define WRAM_START 0x7E0000
#define WRAM_END 0x7FFFFF
#define WRAM_SIZE 131072  // Work RAM (128KB internal)
#define SRAM_START 0x600000
#define SRAM_END 0x6FFFFF
#define SRAM_SIZE 1048576  // Save RAM / Cartridge SRAM
#define CART_ROM_START 0x800000
#define CART_ROM_END 0xFFFFFF
#define CART_ROM_SIZE 8388608  // Cartridge ROM (LoROM/HiROM mapping)
#define PPU1_REGS_START 0x2100
#define PPU1_REGS_END 0x213F
#define PPU1_REGS_SIZE 64  // PPU1 Registers (background)
#define PPU2_REGS_START 0x2140
#define PPU2_REGS_END 0x217F
#define PPU2_REGS_SIZE 64  // PPU2 Registers (sprites)
#define PPU3_REGS_START 0x2180
#define PPU3_REGS_END 0x21FF
#define PPU3_REGS_SIZE 128  // PPU3 Registers (extra)
#define APU_REGS_START 0x2140
#define APU_REGS_END 0x217F
#define APU_REGS_SIZE 64  // APU I/O Registers
#define CPU_IO_START 0x2000
#define CPU_IO_END 0x20FF
#define CPU_IO_SIZE 256  // CPU I/O Ports
#define DMA_REGS_START 0x4300
#define DMA_REGS_END 0x437F
#define DMA_REGS_SIZE 128  // DMA Channel Registers
#define HDMA_REGS_START 0x4380
#define HDMA_REGS_END 0x43FF
#define HDMA_REGS_SIZE 128  // HDMA Channel Registers

// 外设定义
// Picture Processing Unit 1 - Background Rendering
#define PPU1_BASE 0x2100
#define PPU1_INIDISP_ADDR 0x2100
#define PPU1_OBSEL_ADDR 0x2101
#define PPU1_OAMADDL_ADDR 0x2102
#define PPU1_OAMADDH_ADDR 0x2103
#define PPU1_OAMDATA_ADDR 0x2104
#define PPU1_BGMODE_ADDR 0x2105
#define PPU1_MOSAIC_ADDR 0x2106
#define PPU1_BG1SC_ADDR 0x2107
#define PPU1_BG2SC_ADDR 0x2108
#define PPU1_BG3SC_ADDR 0x2109
#define PPU1_BG4SC_ADDR 0x210A
#define PPU1_BG12NBA_ADDR 0x210B
#define PPU1_BG34NBA_ADDR 0x210C
#define PPU1_BG1HOFS_ADDR 0x210D
#define PPU1_BG1VOFS_ADDR 0x210E
#define PPU1_BG2HOFS_ADDR 0x210F
#define PPU1_BG2VOFS_ADDR 0x2110
#define PPU1_BG3HOFS_ADDR 0x2111
#define PPU1_BG3VOFS_ADDR 0x2112
#define PPU1_BG4HOFS_ADDR 0x2113
#define PPU1_BG4VOFS_ADDR 0x2114
#define PPU1_VMAIN_ADDR 0x2115
#define PPU1_VMADDL_ADDR 0x2116
#define PPU1_VMADDH_ADDR 0x2117
#define PPU1_VMDATAL_ADDR 0x2118
#define PPU1_VMDATAH_ADDR 0x2119
#define PPU1_M7SEL_ADDR 0x211A
#define PPU1_M7A_ADDR 0x211B
#define PPU1_M7B_ADDR 0x211C
#define PPU1_M7C_ADDR 0x211D
#define PPU1_M7D_ADDR 0x211E
#define PPU1_M7X_ADDR 0x211F
#define PPU1_M7Y_ADDR 0x2120
#define PPU1_CGADD_ADDR 0x2121
#define PPU1_CGDATA_ADDR 0x2122
#define PPU1_W12SEL_ADDR 0x2123
#define PPU1_W34SEL_ADDR 0x2124
#define PPU1_WOBJSEL_ADDR 0x2125
#define PPU1_WH0_ADDR 0x2126
#define PPU1_WH1_ADDR 0x2127
#define PPU1_WH2_ADDR 0x2128
#define PPU1_WH3_ADDR 0x2129
#define PPU1_WBGLOG_ADDR 0x212A
#define PPU1_WOBJLOG_ADDR 0x212B
#define PPU1_TM_ADDR 0x212C
#define PPU1_TS_ADDR 0x212D
#define PPU1_TMW_ADDR 0x212E
#define PPU1_TSW_ADDR 0x212F
#define PPU1_CGSWSEL_ADDR 0x2130
#define PPU1_CGADSUB_ADDR 0x2131
#define PPU1_SETINI_ADDR 0x2133
// Picture Processing Unit 2 - Sprite Rendering
#define PPU2_BASE 0x2140
#define PPU2_OAMDATAREAD_ADDR 0x2138
#define PPU2_VMDATAREAD_ADDR 0x2139
#define PPU2_VMDATAHREAD_ADDR 0x213A
#define PPU2_CGDATAREAD_ADDR 0x213B
#define PPU2_OPHCT_ADDR 0x213C
#define PPU2_OPVCT_ADDR 0x213D
#define PPU2_STAT78_ADDR 0x213F
// Sony SPC700 Audio CPU (8-bit)
#define SPC700_BASE 0x00
#define SPC700_PC_ADDR 0x00
#define SPC700_A_ADDR 0x02
#define SPC700_X_ADDR 0x03
#define SPC700_Y_ADDR 0x04
#define SPC700_SP_ADDR 0x05
#define SPC700_PSW_ADDR 0x06
#define SPC700_TEST_ADDR 0x0F
// S-DSP Audio DSP (8-channel ADPCM)
#define DSP_BASE 0x00
#define DSP_MVOL_L_ADDR 0x0C
#define DSP_MVOL_R_ADDR 0x1C
#define DSP_EVOL_L_ADDR 0x2C
#define DSP_EVOL_R_ADDR 0x3C
#define DSP_KON_ADDR 0x4C
#define DSP_KOFF_ADDR 0x5C
#define DSP_KONKOFF_ADDR 0x4D
#define DSP_FLG_ADDR 0x6C
#define DSP_ENDX_ADDR 0x7D
#define DSP_EBUST_ADDR 0x6D
#define DSP_EDL_ADDR 0x7D
#define DSP_ENV0_ADDR 0x00
#define DSP_OUT0_ADDR 0x1C
#define DSP_ENV1_ADDR 0x01
#define DSP_OUT1_ADDR 0x2C
#define DSP_ENV2_ADDR 0x02
#define DSP_OUT2_ADDR 0x3C
#define DSP_ENV3_ADDR 0x03
#define DSP_OUT3_ADDR 0x4C
#define DSP_ENV4_ADDR 0x04
#define DSP_OUT4_ADDR 0x5C
#define DSP_ENV5_ADDR 0x05
#define DSP_OUT5_ADDR 0x6C
#define DSP_ENV6_ADDR 0x06
#define DSP_OUT6_ADDR 0x7C
#define DSP_ENV7_ADDR 0x07
#define DSP_OUT7_ADDR 0x0D
#define DSP_V0SRC_ADDR 0x08
#define DSP_V1SRC_ADDR 0x09
#define DSP_V2SRC_ADDR 0x0A
#define DSP_V3SRC_ADDR 0x0B
#define DSP_V4SRC_ADDR 0x18
#define DSP_V5SRC_ADDR 0x19
#define DSP_V6SRC_ADDR 0x1A
#define DSP_V7SRC_ADDR 0x1B
#define DSP_V0PITCHL_ADDR 0x02
#define DSP_V0PITCHH_ADDR 0x03
#define DSP_V1PITCHL_ADDR 0x12
#define DSP_V1PITCHH_ADDR 0x13
#define DSP_V2PITCHL_ADDR 0x22
#define DSP_V2PITCHH_ADDR 0x23
#define DSP_V3PITCHL_ADDR 0x32
#define DSP_V3PITCHH_ADDR 0x33
#define DSP_V4PITCHL_ADDR 0x42
#define DSP_V4PITCHH_ADDR 0x43
#define DSP_V5PITCHL_ADDR 0x52
#define DSP_V5PITCHH_ADDR 0x53
#define DSP_V6PITCHL_ADDR 0x62
#define DSP_V6PITCHH_ADDR 0x63
#define DSP_V7PITCHL_ADDR 0x72
#define DSP_V7PITCHH_ADDR 0x73
#define DSP_V0ADSR0_ADDR 0x04
#define DSP_V0ADSR1_ADDR 0x05
#define DSP_V0ADSR2_ADDR 0x06
#define DSP_V1ADSR0_ADDR 0x14
#define DSP_V1ADSR1_ADDR 0x15
#define DSP_V1ADSR2_ADDR 0x16
#define DSP_V2ADSR0_ADDR 0x24
#define DSP_V2ADSR1_ADDR 0x25
#define DSP_V2ADSR2_ADDR 0x26
#define DSP_V3ADSR0_ADDR 0x34
#define DSP_V3ADSR1_ADDR 0x35
#define DSP_V3ADSR2_ADDR 0x36
#define DSP_V4ADSR0_ADDR 0x44
#define DSP_V4ADSR1_ADDR 0x45
#define DSP_V4ADSR2_ADDR 0x46
#define DSP_V5ADSR0_ADDR 0x54
#define DSP_V5ADSR1_ADDR 0x55
#define DSP_V5ADSR2_ADDR 0x56
#define DSP_V6ADSR0_ADDR 0x64
#define DSP_V6ADSR1_ADDR 0x65
#define DSP_V6ADSR2_ADDR 0x66
#define DSP_V7ADSR0_ADDR 0x74
#define DSP_V7ADSR1_ADDR 0x75
#define DSP_V7ADSR2_ADDR 0x76
#define DSP_V0GAIN_ADDR 0x07
#define DSP_V1GAIN_ADDR 0x17
#define DSP_V2GAIN_ADDR 0x27
#define DSP_V3GAIN_ADDR 0x37
#define DSP_V4GAIN_ADDR 0x47
#define DSP_V5GAIN_ADDR 0x57
#define DSP_V6GAIN_ADDR 0x67
#define DSP_V7GAIN_ADDR 0x77
#define DSP_V0WAVE_ADDR 0x0D
#define DSP_V1WAVE_ADDR 0x1D
#define DSP_V2WAVE_ADDR 0x2D
#define DSP_V3WAVE_ADDR 0x3D
#define DSP_V4WAVE_ADDR 0x4D
#define DSP_V5WAVE_ADDR 0x5D
#define DSP_V6WAVE_ADDR 0x6D
#define DSP_V7WAVE_ADDR 0x7D
// Direct Memory Access Controller
#define DMA_BASE 0x4300
#define DMA_DMAP0_ADDR 0x4300
#define DMA_BBAD0_ADDR 0x4301
#define DMA_A1T0L_ADDR 0x4302
#define DMA_A1T0H_ADDR 0x4303
#define DMA_A1B0_ADDR 0x4304
#define DMA_DAS0L_ADDR 0x4305
#define DMA_DAS0H_ADDR 0x4306
#define DMA_DASB0_ADDR 0x4307
#define DMA_A2A0_ADDR 0x4308
#define DMA_A2A1_ADDR 0x4309
#define DMA_A2B0_ADDR 0x430A
#define DMA_NTT0_ADDR 0x430B
#define DMA_DMAP1_ADDR 0x4310
#define DMA_BBAD1_ADDR 0x4311
#define DMA_A1T1L_ADDR 0x4312
#define DMA_A1T1H_ADDR 0x4313
#define DMA_A1B1_ADDR 0x4314
#define DMA_DAS1L_ADDR 0x4315
#define DMA_DAS1H_ADDR 0x4316
#define DMA_DASB1_ADDR 0x4317
#define DMA_DMAP2_ADDR 0x4320
#define DMA_BBAD2_ADDR 0x4321
#define DMA_A1T2L_ADDR 0x4322
#define DMA_A1T2H_ADDR 0x4323
#define DMA_A1B2_ADDR 0x4324
#define DMA_DAS2L_ADDR 0x4325
#define DMA_DAS2H_ADDR 0x4326
#define DMA_DASB2_ADDR 0x4327
#define DMA_DMAP3_ADDR 0x4330
#define DMA_BBAD3_ADDR 0x4331
#define DMA_A1T3L_ADDR 0x4332
#define DMA_A1T3H_ADDR 0x4333
#define DMA_A1B3_ADDR 0x4334
#define DMA_DAS3L_ADDR 0x4335
#define DMA_DAS3H_ADDR 0x4336
#define DMA_DASB3_ADDR 0x4337
#define DMA_MDMAEN_ADDR 0x4350
// Horizontal DMA (scanline-based)
#define HDMA_BASE 0x4380
#define HDMA_HDMAP0_ADDR 0x4380
#define HDMA_HBAD0_ADDR 0x4381
#define HDMA_A1T0L_ADDR 0x4382
#define HDMA_A1T0H_ADDR 0x4383
#define HDMA_A1B0_ADDR 0x4384
#define HDMA_DAS0L_ADDR 0x4385
#define HDMA_DAS0H_ADDR 0x4386
#define HDMA_HDMAP1_ADDR 0x4388
#define HDMA_HBAD1_ADDR 0x4389
#define HDMA_A1T1L_ADDR 0x438A
#define HDMA_A1T1H_ADDR 0x438B
#define HDMA_A1B1_ADDR 0x438C
#define HDMA_DAS1L_ADDR 0x438D
#define HDMA_DAS1H_ADDR 0x438E
#define HDMA_HDMAP2_ADDR 0x4390
#define HDMA_HBAD2_ADDR 0x4391
#define HDMA_A1T2L_ADDR 0x4392
#define HDMA_A1T2H_ADDR 0x4393
#define HDMA_A1B2_ADDR 0x4394
#define HDMA_DAS2L_ADDR 0x4395
#define HDMA_DAS2H_ADDR 0x4396
#define HDMA_HDMAP3_ADDR 0x4398
#define HDMA_HBAD3_ADDR 0x4399
#define HDMA_A1T3L_ADDR 0x439A
#define HDMA_A1T3H_ADDR 0x439B
#define HDMA_A1B3_ADDR 0x439C
#define HDMA_DAS3L_ADDR 0x439D
#define HDMA_DAS3H_ADDR 0x439E
#define HDMA_HDMAEN_ADDR 0x43F0
// Controller Port 1
#define CONTROLLER1_BASE 0x4016
#define CONTROLLER1_JOYPAD1_ADDR 0x4016
#define CONTROLLER1_JOYSTROBE_ADDR 0x4016
// Controller Port 2
#define CONTROLLER2_BASE 0x4017
#define CONTROLLER2_JOYPAD2_ADDR 0x4017
#define CONTROLLER2_RDNMI_ADDR 0x4210
#define CONTROLLER2_TIMEUP_ADDR 0x4211
#define CONTROLLER2_HVBJOY_ADDR 0x4212
// Timer / IRQ Control
#define TIMER_BASE 0x4200
#define TIMER_NMITIMEN_ADDR 0x4200
#define TIMER_NMITIMEN_VBLANK_NMI_BIT 7  // V-Blank NMI Enable
#define TIMER_NMITIMEN_HTIMER_EN_BIT 4  // H-Counter IRQ Enable
#define TIMER_NMITIMEN_VTIMER_EN_BIT 5  // V-Counter IRQ Enable
#define TIMER_WRI00_ADDR 0x4201
#define TIMER_HTIMEL_ADDR 0x4202
#define TIMER_HTIMEH_ADDR 0x4203
#define TIMER_VTIMEL_ADDR 0x4204
#define TIMER_VTIMEH_ADDR 0x4205
#define TIMER_MEMSEL_ADDR 0x420D

// 中断向量定义
#define INT_RESET 0  // Reset
#define INT_NMI 1  // Non-Maskable Interrupt (V-Blank)
#define INT_IRQ 2  // IRQ / BRK (Timer, HDMA, Controller)
#define INT_TIMER_IRQ 3  // H/V Counter Timer IRQ

// 引脚定义
#define PIN_VCC 1  // Power Supply
#define PIN_GND 2  // Ground
#define PIN_CLK 3  // System Clock Input (21.47727 MHz)
#define PIN_RESET 4  // Reset Signal
#define PIN_NMI 5  // Non-Maskable Interrupt
#define PIN_IRQ 6  // Interrupt Request
#define PIN_RDY 7  // Ready / Wait State
#define PIN_AB0 8  // Address Bus Bit 0
#define PIN_AB1 9  // Address Bus Bit 1
#define PIN_AB2 10  // Address Bus Bit 2
#define PIN_AB3 11  // Address Bus Bit 3
#define PIN_AB4 12  // Address Bus Bit 4
#define PIN_AB5 13  // Address Bus Bit 5
#define PIN_AB6 14  // Address Bus Bit 6
#define PIN_AB7 15  // Address Bus Bit 7
#define PIN_AB8 16  // Address Bus Bit 8
#define PIN_AB9 17  // Address Bus Bit 9
#define PIN_AB10 18  // Address Bus Bit 10
#define PIN_AB11 19  // Address Bus Bit 11
#define PIN_AB12 20  // Address Bus Bit 12
#define PIN_AB13 21  // Address Bus Bit 13
#define PIN_AB14 22  // Address Bus Bit 14
#define PIN_AB15 23  // Address Bus Bit 15
#define PIN_AB16 24  // Address Bus Bit 16
#define PIN_AB17 25  // Address Bus Bit 17
#define PIN_AB18 26  // Address Bus Bit 18
#define PIN_AB19 27  // Address Bus Bit 19
#define PIN_AB20 28  // Address Bus Bit 20
#define PIN_AB21 29  // Address Bus Bit 21
#define PIN_AB22 30  // Address Bus Bit 22
#define PIN_AB23 31  // Address Bus Bit 23
#define PIN_DB0 32  // Data Bus Bit 0
#define PIN_DB1 33  // Data Bus Bit 1
#define PIN_DB2 34  // Data Bus Bit 2
#define PIN_DB3 35  // Data Bus Bit 3
#define PIN_DB4 36  // Data Bus Bit 4
#define PIN_DB5 37  // Data Bus Bit 5
#define PIN_DB6 38  // Data Bus Bit 6
#define PIN_DB7 39  // Data Bus Bit 7
#define PIN_PHI 40  // Phase Out Clock

#endif /* RICOH-5A22_DEVICE_H */
