#ifndef MACINTOSH_128K_HPP
#define MACINTOSH_128K_HPP

// Macintosh-128K寄存器定义
// 生成自: Apple Computer/Macintosh/Macintosh-128K
// 版本: 1.0
// 日期: 2026-04-17


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: MC68000
// 位宽: 32位
// 时钟频率: 7833600 Hz

// 寄存器定义
// Data Register 0
#define D0 (*(volatile uint32_t*)0x00)

// Data Register 1
#define D1 (*(volatile uint32_t*)0x04)

// Data Register 2
#define D2 (*(volatile uint32_t*)0x08)

// Data Register 3
#define D3 (*(volatile uint32_t*)0x0C)

// Data Register 4
#define D4 (*(volatile uint32_t*)0x10)

// Data Register 5
#define D5 (*(volatile uint32_t*)0x14)

// Data Register 6
#define D6 (*(volatile uint32_t*)0x18)

// Data Register 7
#define D7 (*(volatile uint32_t*)0x1C)

// Address Register 0
#define A0 (*(volatile uint32_t*)0x20)

// Address Register 1
#define A1 (*(volatile uint32_t*)0x24)

// Address Register 2
#define A2 (*(volatile uint32_t*)0x28)

// Address Register 3
#define A3 (*(volatile uint32_t*)0x2C)

// Address Register 4
#define A4 (*(volatile uint32_t*)0x30)

// Address Register 5
#define A5 (*(volatile uint32_t*)0x34)

// Address Register 6
#define A6 (*(volatile uint32_t*)0x38)

// Stack Pointer (USP)
#define A7 (*(volatile uint32_t*)0x3C)

// Program Counter
#define PC (*(volatile uint32_t*)0x40)

// Status Register
#define SR (*(volatile uint16_t*)0x44)
#define SR_C 0  // Carry
#define SR_V 1  // Overflow
#define SR_Z 2  // Zero
#define SR_N 3  // Negative
#define SR_X 4  // Extend
#define SR_I0 8  // Interrupt Mask 0
#define SR_I1 9  // Interrupt Mask 1
#define SR_I2 10  // Interrupt Mask 2
#define SR_S 13  // Supervisor/User
#define SR_T0 14  // Trace Mode 0
#define SR_T1 15  // Trace Mode 1

// 内存段定义
// Main RAM (128KB unified)
#define RAM_START 0x000000
#define RAM_END 0x01FFFF
#define RAM_SIZE 131072

// Mac ROM (128KB)
#define ROM_START 0x40000000
#define ROM_END 0x4001FFFF
#define ROM_SIZE 131072

// Screen bitmap (512x342x1 = 21792 bytes)
#define FRAMEBUFFER_START 0x00400000
#define FRAMEBUFFER_END 0x00400555
#define FRAMEBUFFER_SIZE 1366

// Shadow screen (double-buffering)
#define FRAMEBUFFER2_START 0x00410000
#define FRAMEBUFFER2_END 0x00410555
#define FRAMEBUFFER2_SIZE 1366

// VIA 6522 (I/O)
#define VIA_START 0x00E00000
#define VIA_END 0x00E0FFFF
#define VIA_SIZE 4096

// SCC 8530 (serial)
#define SCC_START 0x00F00000
#define SCC_END 0x00F0FFFF
#define SCC_SIZE 4096

// ADB bus
#define ADB_START 0x01600000
#define ADB_END 0x0160FFFF
#define ADB_SIZE 4096

// IWM floppy controller
#define IWM_START 0x01E00000
#define IWM_END 0x01E0FFFF
#define IWM_SIZE 4096

// 外设定义
// Versatile Interface Adapter 6522
#define VIA_BASE 0xE00000
#define VIA_ORB (*(volatile uint8_t*)0x01C00000)
#define VIA_ORA (*(volatile uint8_t*)0x01C00002)
#define VIA_DDRB (*(volatile uint8_t*)0x01C00004)
#define VIA_DDRA (*(volatile uint8_t*)0x01C00006)
#define VIA_T1C_L (*(volatile uint16_t*)0x01C00008)
#define VIA_T1C_H (*(volatile uint16_t*)0x01C0000A)
#define VIA_T1L_L (*(volatile uint16_t*)0x01C0000C)
#define VIA_T1L_H (*(volatile uint16_t*)0x01C0000E)
#define VIA_T2C_L (*(volatile uint16_t*)0x01C00010)
#define VIA_T2C_H (*(volatile uint16_t*)0x01C00012)
#define VIA_SR (*(volatile uint8_t*)0x01C00014)
#define VIA_ACR (*(volatile uint8_t*)0x01C00016)
#define VIA_PCR (*(volatile uint8_t*)0x01C00018)
#define VIA_IFR (*(volatile uint8_t*)0x01C0001E)
#define VIA_IER (*(volatile uint8_t*)0x01C0001E)

// SCC 8530 Serial Communications Controller
#define SCC_BASE 0xF00000
#define SCC_SCC_CHA_B (*(volatile uint8_t*)0x01E00000)
#define SCC_SCC_CHA_C (*(volatile uint8_t*)0x01E00002)
#define SCC_SCC_CHB_D (*(volatile uint8_t*)0x01E00004)
#define SCC_SCC_CHB_CT (*(volatile uint8_t*)0x01E00006)

// Integrated Woz Machine - Floppy Disk Controller
#define IWM_BASE 0x1E00000
#define IWM_IWM_DATA (*(volatile uint8_t*)0x03C00000)
#define IWM_IWM_MODE (*(volatile uint8_t*)0x03C00008)
#define IWM_IWM_Q6L (*(volatile uint8_t*)0x03C00020)
#define IWM_IWM_Q7L (*(volatile uint8_t*)0x03C00022)
#define IWM_IWM_Q6R (*(volatile uint8_t*)0x03C00024)
#define IWM_IWM_Q7R (*(volatile uint8_t*)0x03C00026)

// Video Graphics Controller (custom Apple chip)
#define VGC_BASE 0x00F20000
#define VGC_VGC_MODE (*(volatile uint8_t*)0x01E40000)
#define VGC_VGC_START_HI (*(volatile uint8_t*)0x01E40002)
#define VGC_VGC_START_LO (*(volatile uint8_t*)0x01E40004)

// Apple Desktop Bus
#define ADB_BASE 0x01600000
#define ADB_ADB_DATA (*(volatile uint8_t*)0x02C00000)
#define ADB_ADB_STATUS (*(volatile uint8_t*)0x02C00004)
#define ADB_ADB_CMD (*(volatile uint8_t*)0x02C00008)

// 中断向量定义
#define RESET_VECTOR 1  // Reset Initial SP
#define RESET_PC_VECTOR 2  // Reset Initial PC
#define IRQ1_VECTOR 24  // VIA interrupt (level 1)
#define IRQ2_VECTOR 25  // SCC interrupt (level 2)
#define IRQ3_VECTOR 26  // ADB / VIA (level 3)
#define IRQ4_VECTOR 27  // ADB / VIA (level 4)

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

void macintosh_128k_init(void);

#ifdef __cplusplus
}
#endif

#endif // MACINTOSH_128K_HPP
