// Commodore-64 设备定义 - Objective-C 头文件
// 生成自: Commodore/C64/Commodore-64
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Commodore 64 - Best-selling 8-bit home computer with MOS 6510 CPU, VIC-II graphics, and SID audio
// CPU架构: MOS-6510
// 位宽: 8位
// 时钟频率: 1022727 Hz

#ifndef COMMODORE-64_DEVICE_H
#define COMMODORE-64_DEVICE_H

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
#define P_B_BIT 4  // Break Flag
#define P_U_BIT 5  // Unused
#define P_V_BIT 6  // Overflow Flag
#define P_N_BIT 7  // Negative Flag
#define PORT_ADDR 0x00  // I/O Port (6510 only: DDR + data)

// 内存段定义
#define RAM_START 0x0000
#define RAM_END 0xFFFF
#define RAM_SIZE 65536  // 64KB main RAM
#define BASIC_ROM_START 0xA000
#define BASIC_ROM_END 0xBFFF
#define BASIC_ROM_SIZE 8192  // BASIC interpreter ROM
#define KERNAL_ROM_START 0xE000
#define KERNAL_ROM_END 0xFFFF
#define KERNAL_ROM_SIZE 8192  // KERNAL operating system ROM
#define CHAR_ROM_START 0xD000
#define CHAR_ROM_END 0xDFFF
#define CHAR_ROM_SIZE 4096  // Character generator ROM
#define IO_RAM_START 0xD000
#define IO_RAM_END 0xDFFF
#define IO_RAM_SIZE 4096  // I/O + RAM window (switchable)

// 外设定义
// Video Interface Chip II - 6567/6569
#define VICII_BASE 0xD000
#define VICII_SP0X_ADDR 0xD000
#define VICII_SP0Y_ADDR 0xD001
#define VICII_SP1X_ADDR 0xD002
#define VICII_SP1Y_ADDR 0xD003
#define VICII_SP2X_ADDR 0xD004
#define VICII_SP2Y_ADDR 0xD005
#define VICII_SP3X_ADDR 0xD006
#define VICII_SP3Y_ADDR 0xD007
#define VICII_SP4X_ADDR 0xD008
#define VICII_SP4Y_ADDR 0xD009
#define VICII_SP5X_ADDR 0xD00A
#define VICII_SP5Y_ADDR 0xD00B
#define VICII_SP6X_ADDR 0xD00C
#define VICII_SP6Y_ADDR 0xD00D
#define VICII_SP7X_ADDR 0xD00E
#define VICII_SP7Y_ADDR 0xD00F
#define VICII_MSIGX_ADDR 0xD010
#define VICII_SCROLY_ADDR 0xD011
#define VICII_SCROLX_ADDR 0xD016
#define VICII_YPSTOP_ADDR 0xD012
#define VICII_LPX_ADDR 0xD013
#define VICII_LPY_ADDR 0xD014
#define VICII_SPENA_ADDR 0xD015
#define VICII_CSPMC_ADDR 0xD017
#define VICII_MM0_ADDR 0xD018
#define VICII_VM01_ADDR 0xD016
#define VICII_VICBAS_ADDR 0xD018
#define VICII_IRQMASK_ADDR 0xD019
#define VICII_IRQST_ADDR 0xD01A
#define VICII_SPBGPR_ADDR 0xD01B
#define VICII_SPMC_ADDR 0xD01C
#define VICII_SP1C_ADDR 0xD025
#define VICII_SP2C_ADDR 0xD026
#define VICII_SPBC_ADDR 0xD027
#define VICII_SP1C0_ADDR 0xD028
#define VICII_SP2C0_ADDR 0xD029
#define VICII_SP3C0_ADDR 0xD02A
#define VICII_SP4C0_ADDR 0xD02B
#define VICII_SP5C0_ADDR 0xD02C
#define VICII_SP6C0_ADDR 0xD02D
#define VICII_SP7C0_ADDR 0xD02E
#define VICII_REG_FD_ADDR 0xD01D
#define VICII_BGCOL0_ADDR 0xD021
#define VICII_BGCOL1_ADDR 0xD022
#define VICII_BGCOL2_ADDR 0xD023
#define VICII_BGCOL3_ADDR 0xD024
// Sound Interface Device 6581/8580
#define SID_BASE 0xD400
#define SID_FREQ1LO_ADDR 0xD400
#define SID_FREQ1HI_ADDR 0xD401
#define SID_PW1LO_ADDR 0xD402
#define SID_PW1HI_ADDR 0xD403
#define SID_CR1_ADDR 0xD404
#define SID_AD1_ADDR 0xD405
#define SID_SR1_ADDR 0xD406
#define SID_FREQ2LO_ADDR 0xD407
#define SID_FREQ2HI_ADDR 0xD408
#define SID_PW2LO_ADDR 0xD409
#define SID_PW2HI_ADDR 0xD40A
#define SID_CR2_ADDR 0xD40B
#define SID_AD2_ADDR 0xD40C
#define SID_SR2_ADDR 0xD40D
#define SID_FREQ3LO_ADDR 0xD40E
#define SID_FREQ3HI_ADDR 0xD40F
#define SID_PW3LO_ADDR 0xD410
#define SID_PW3HI_ADDR 0xD411
#define SID_CR3_ADDR 0xD412
#define SID_AD3_ADDR 0xD413
#define SID_SR3_ADDR 0xD414
#define SID_FCH_ADDR 0xD415
#define SID_FCL_ADDR 0xD416
#define SID_RES_FLT_ADDR 0xD417
#define SID_VOLUME_ADDR 0xD418
#define SID_POTX_ADDR 0xD419
#define SID_POTY_ADDR 0xD41A
#define SID_OSC3_ADDR 0xD41B
#define SID_ENV3_ADDR 0xD41C
// Complex Interface Adapter 1 - Keyboard/Serial
#define CIA1_BASE 0xDC00
#define CIA1_PRA_ADDR 0xDC00
#define CIA1_PRB_ADDR 0xDC01
#define CIA1_DDRA_ADDR 0xDC02
#define CIA1_DDRB_ADDR 0xDC03
#define CIA1_TA_LO_ADDR 0xDC04
#define CIA1_TA_HI_ADDR 0xDC05
#define CIA1_TB_LO_ADDR 0xDC06
#define CIA1_TB_HI_ADDR 0xDC07
#define CIA1_TOD_TENTH_ADDR 0xDC08
#define CIA1_TOD_SEC_ADDR 0xDC09
#define CIA1_TOD_MIN_ADDR 0xDC0A
#define CIA1_TOD_HR_ADDR 0xDC0B
#define CIA1_SDR_ADDR 0xDC0C
#define CIA1_ICR_ADDR 0xDC0D
#define CIA1_CRA_ADDR 0xDC0E
#define CIA1_CRB_ADDR 0xDC0F
// Complex Interface Adapter 2 - Serial/Bus
#define CIA2_BASE 0xDD00
#define CIA2_PRA_ADDR 0xDD00
#define CIA2_PRB_ADDR 0xDD01
#define CIA2_DDRA_ADDR 0xDD02
#define CIA2_DDRB_ADDR 0xDD03
#define CIA2_TA_LO_ADDR 0xDD04
#define CIA2_TA_HI_ADDR 0xDD05
#define CIA2_TB_LO_ADDR 0xDD06
#define CIA2_TB_HI_ADDR 0xDD07
#define CIA2_TOD_TENTH_ADDR 0xDD08
#define CIA2_TOD_SEC_ADDR 0xDD09
#define CIA2_TOD_MIN_ADDR 0xDD0A
#define CIA2_TOD_HR_ADDR 0xDD0B
#define CIA2_SDR_ADDR 0xDD0C
#define CIA2_ICR_ADDR 0xDD0D
#define CIA2_CRA_ADDR 0xDD0E
#define CIA2_CRB_ADDR 0xDD0F
// Color RAM (4-bit per char cell)
#define COLORRAM_BASE 0xD800
#define COLORRAM_COLOR_ADDR 0xD800
// IEC Serial Bus (via CIA1)
#define IEC_BASE 0xDC00
#define IEC_IEC_DATA_ADDR 0xDC00
#define IEC_IEC_CLOCK_ADDR 0xDC01

// 中断向量定义
#define INT_RESET 0  // Power-on / Reset
#define INT_NMI 1  // Non-Maskable Interrupt
#define INT_IRQ 2  // IRQ (VIC raster / CIA timer)

// 引脚定义
#define PIN_VCC 1  // +5V Power
#define PIN_GND 2  // Ground
#define PIN_RESET 3  // System Reset
#define PIN_CLK 4  // System Clock (~1MHz)
#define PIN_DOTCLK 5  // VIC Dot Clock (8MHz NTSC / 7.8MHz PAL)
#define PIN_AEC 6  // Address Enable Control (VIC steals cycles)
#define PIN_BA 7  // Bus Available (from VIC)
#define PIN_IRQ 8  // Interrupt Request
#define PIN_NMI 9  // Non-Maskable Interrupt
#define PIN_RWB 10  // Read/Write
#define PIN_A0_A15 11  // Address Bus
#define PIN_D0_D7 12  // Data Bus

#endif /* COMMODORE-64_DEVICE_H */
