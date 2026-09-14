// Acorn-Archimedes-A310 设备定义 - Objective-C 头文件
// 生成自: Acorn Computers/Archimedes/Acorn-Archimedes-A310
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Acorn Archimedes A310 - First ARM-based home computer with RISC OS, ARM250 @ 26MHz
// CPU架构: ARM250
// 位宽: 32位
// 时钟频率: 26000000 Hz

#ifndef ACORN-ARCHIMEDES-A310_DEVICE_H
#define ACORN-ARCHIMEDES-A310_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define R0_ADDR 0x00  // General Purpose Register 0
#define R1_ADDR 0x04  // General Purpose Register 1
#define R2_ADDR 0x08  // General Purpose Register 2
#define R3_ADDR 0x0C  // General Purpose Register 3
#define R4_ADDR 0x10  // General Purpose Register 4
#define R5_ADDR 0x14  // General Purpose Register 5
#define R6_ADDR 0x18  // General Purpose Register 6
#define R7_ADDR 0x1C  // General Purpose Register 7
#define R8_ADDR 0x20  // General Purpose Register 8
#define R9_ADDR 0x24  // General Purpose Register 9
#define R10_ADDR 0x28  // General Purpose Register 10
#define R11_ADDR 0x2C  // General Purpose Register 11 (fp)
#define R12_ADDR 0x30  // General Purpose Register 12
#define SP_ADDR 0x34  // Stack Pointer (R13)
#define LR_ADDR 0x38  // Link Register (R14)
#define PC_ADDR 0x3C  // Program Counter (R15)
#define PSR_ADDR 0x40  // Processor Status Register
#define PSR_MODE_BIT 0  // Mode bits (0-4)
#define PSR_T_BIT 5  // Thumb state
#define PSR_F_BIT 6  // FIQ disable
#define PSR_I_BIT 7  // IRQ disable
#define PSR_V_BIT 28  // Overflow
#define PSR_C_BIT 29  // Carry
#define PSR_Z_BIT 30  // Zero
#define PSR_N_BIT 31  // Negative

// 内存段定义
#define ROM_START 0x00000000
#define ROM_END 0x0007FFFF
#define ROM_SIZE 524288  // RISC OS ROM (512KB)
#define RAM_START 0x00080000
#define RAM_END 0x003FFFFF
#define RAM_SIZE 3932160  // Main RAM (up to 4MB)
#define VRAM_START 0x00400000
#define VRAM_END 0x007FFFFF
#define VRAM_SIZE 4194304  // Video RAM (4MB, VIDC)
#define IO_START 0x03000000
#define IO_END 0x0301FFFF
#define IO_SIZE 131072  // I/O controller (IOC)
#define MEMC_START 0x03200000
#define MEMC_END 0x0320FFFF
#define MEMC_SIZE 4096  // Memory Controller (MEMC)
#define VIDC_START 0x03400000
#define VIDC_END 0x0340FFFF
#define VIDC_SIZE 4096  // Video Controller (VIDC)
#define IOMD_START 0x03300000
#define IOMD_END 0x0330FFFF
#define IOMD_SIZE 4096  // I/O and Memory DMA

// 外设定义
// I/O Controller (IOC) - Interrupt/Keyboard/RTC
#define IOC_BASE 0x03000000
#define IOC_IOC_TIMER1_ADDR 0x03000000
#define IOC_IOC_TIMER2_ADDR 0x03000004
#define IOC_IOC_IOSEL_ADDR 0x03000008
#define IOC_IOC_IRQST_ADDR 0x0300000C
#define IOC_IOC_IRQLATCH_ADDR 0x03000010
#define IOC_IOC_FIQST_ADDR 0x03000014
#define IOC_IOC_FIQEN_ADDR 0x03000018
#define IOC_IOC_IRQEN_ADDR 0x0300001C
#define IOC_IOC_KBDDATA_ADDR 0x03000020
#define IOC_IOC_KBDCR_ADDR 0x03000024
#define IOC_IOC_RTCDR_ADDR 0x03000028
#define IOC_IOC_RTCCR_ADDR 0x0300002C
#define IOC_IOC_PRST_ADDR 0x03000030
#define IOC_IOC_PORTA_ADDR 0x03000034
#define IOC_IOC_PORTB_ADDR 0x03000038
#define IOC_IOC_PORTC_ADDR 0x0300003C
// Memory Controller (MEMC1)
#define MEMC_BASE 0x03200000
#define MEMC_MEMC_PT_ADDR 0x03200000
#define MEMC_MEMC_CTRL_ADDR 0x03200004
#define MEMC_MEMC_DRAM_ADDR 0x03200008
#define MEMC_MEMC_ERR_ADDR 0x0320000C
// Video Controller - VIDC1
#define VIDC_BASE 0x03400000
#define VIDC_VIDC_PALETTE_ADDR 0x03400000
#define VIDC_VIDC_STARTL_ADDR 0x03400004
#define VIDC_VIDC_STARTH_ADDR 0x03400008
#define VIDC_VIDC_CONFIG_ADDR 0x0340000C
#define VIDC_VIDC_HDISP_ADDR 0x03400010
#define VIDC_VIDC_VDISP_ADDR 0x03400014
#define VIDC_VIDC_HSYNC_ADDR 0x03400018
#define VIDC_VIDC_VSYNC_ADDR 0x0340001C
#define VIDC_VIDC_BORDER_ADDR 0x03400020
#define VIDC_VIDC_CURSOR_ADDR 0x03400024
#define VIDC_VIDC_SOUND_ADDR 0x03400028
// Intel 82710 Floppy Disk Controller
#define FDC_BASE 0x03010000
#define FDC_FDC_STATUS_ADDR 0x03010000
#define FDC_FDC_COMMAND_ADDR 0x03010000
#define FDC_FDC_TRACK_ADDR 0x03010004
#define FDC_FDC_SECTOR_ADDR 0x03010008
#define FDC_FDC_DATA_ADDR 0x0301000C
// Serial Port (via IOC)
#define SERIAL_BASE 0x03010010
#define SERIAL_SERIAL_TX_ADDR 0x03010010
#define SERIAL_SERIAL_RX_ADDR 0x03010014
#define SERIAL_SERIAL_CTRL_ADDR 0x03010018

// 中断向量定义
#define INT_RESET 0  // Reset
#define INT_UND 1  // Undefined instruction
#define INT_SWI 2  // Software Interrupt (SWI/SVC)
#define INT_PABORT 3  // Prefetch Abort
#define INT_DABORT 4  // Data Abort
#define INT_ADDRESS 5  // Address Exception
#define INT_IRQ 6  // IRQ interrupt (IOC)
#define INT_FIQ 7  // FIQ interrupt (VIDC)

// 引脚定义
#define PIN_VCC 1  // +5V Power
#define PIN_GND 2  // Ground
#define PIN_CLK 3  // ARM clock (26MHz)
#define PIN_NRESET 4  // Reset (active low)
#define PIN_NMREQ 5  // Memory Request (active low)
#define PIN_NIORQ 6  // I/O Request (active low)
#define PIN_NRW 7  // Read/Write (0=write, 1=read)
#define PIN_MAS0 8  // Master address bit 0
#define PIN_MAS1 9  // Master address bit 1
#define PIN_MAS2 10  // Master address bit 2
#define PIN_LOCK 11  // Bus lock
#define PIN_NMREQ 12  // Memory request (active low)
#define PIN_NWAIT 13  // Wait state (active low)
#define PIN_NIRQLINE 14  // IRQ line (active low)
#define PIN_NFIRQLINE 15  // FIQ line (active low)
#define PIN_A1_A25 16  // Address Bus (26-bit)
#define PIN_D0_D31 17  // Data Bus (32-bit)

#endif /* ACORN-ARCHIMEDES-A310_DEVICE_H */
