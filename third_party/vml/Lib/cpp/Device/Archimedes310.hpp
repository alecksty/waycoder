#ifndef ACORN_ARCHIMEDES_A310_HPP
#define ACORN_ARCHIMEDES_A310_HPP

// Acorn-Archimedes-A310寄存器定义
// 生成自: Acorn Computers/Archimedes/Acorn-Archimedes-A310
// 版本: 1.0
// 日期: 2026-04-17


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: ARM250
// 位宽: 32位
// 时钟频率: 26000000 Hz

// 寄存器定义
// General Purpose Register 0
#define R0 (*(volatile uint32_t*)0x00)

// General Purpose Register 1
#define R1 (*(volatile uint32_t*)0x04)

// General Purpose Register 2
#define R2 (*(volatile uint32_t*)0x08)

// General Purpose Register 3
#define R3 (*(volatile uint32_t*)0x0C)

// General Purpose Register 4
#define R4 (*(volatile uint32_t*)0x10)

// General Purpose Register 5
#define R5 (*(volatile uint32_t*)0x14)

// General Purpose Register 6
#define R6 (*(volatile uint32_t*)0x18)

// General Purpose Register 7
#define R7 (*(volatile uint32_t*)0x1C)

// General Purpose Register 8
#define R8 (*(volatile uint32_t*)0x20)

// General Purpose Register 9
#define R9 (*(volatile uint32_t*)0x24)

// General Purpose Register 10
#define R10 (*(volatile uint32_t*)0x28)

// General Purpose Register 11 (fp)
#define R11 (*(volatile uint32_t*)0x2C)

// General Purpose Register 12
#define R12 (*(volatile uint32_t*)0x30)

// Stack Pointer (R13)
#define SP (*(volatile uint32_t*)0x34)

// Link Register (R14)
#define LR (*(volatile uint32_t*)0x38)

// Program Counter (R15)
#define PC (*(volatile uint32_t*)0x3C)

// Processor Status Register
#define PSR (*(volatile uint32_t*)0x40)
#define PSR_MODE 0  // Mode bits (0-4)
#define PSR_T 5  // Thumb state
#define PSR_F 6  // FIQ disable
#define PSR_I 7  // IRQ disable
#define PSR_V 28  // Overflow
#define PSR_C 29  // Carry
#define PSR_Z 30  // Zero
#define PSR_N 31  // Negative

// 内存段定义
// RISC OS ROM (512KB)
#define ROM_START 0x00000000
#define ROM_END 0x0007FFFF
#define ROM_SIZE 524288

// Main RAM (up to 4MB)
#define RAM_START 0x00080000
#define RAM_END 0x003FFFFF
#define RAM_SIZE 3932160

// Video RAM (4MB, VIDC)
#define VRAM_START 0x00400000
#define VRAM_END 0x007FFFFF
#define VRAM_SIZE 4194304

// I/O controller (IOC)
#define IO_START 0x03000000
#define IO_END 0x0301FFFF
#define IO_SIZE 131072

// Memory Controller (MEMC)
#define MEMC_START 0x03200000
#define MEMC_END 0x0320FFFF
#define MEMC_SIZE 4096

// Video Controller (VIDC)
#define VIDC_START 0x03400000
#define VIDC_END 0x0340FFFF
#define VIDC_SIZE 4096

// I/O and Memory DMA
#define IOMD_START 0x03300000
#define IOMD_END 0x0330FFFF
#define IOMD_SIZE 4096

// 外设定义
// I/O Controller (IOC) - Interrupt/Keyboard/RTC
#define IOC_BASE 0x03000000
#define IOC_IOC_TIMER1 (*(volatile uint32_t*)0x06000000)
#define IOC_IOC_TIMER2 (*(volatile uint32_t*)0x06000004)
#define IOC_IOC_IOSEL (*(volatile uint32_t*)0x06000008)
#define IOC_IOC_IRQST (*(volatile uint32_t*)0x0600000C)
#define IOC_IOC_IRQLATCH (*(volatile uint32_t*)0x06000010)
#define IOC_IOC_FIQST (*(volatile uint32_t*)0x06000014)
#define IOC_IOC_FIQEN (*(volatile uint32_t*)0x06000018)
#define IOC_IOC_IRQEN (*(volatile uint32_t*)0x0600001C)
#define IOC_IOC_KBDDATA (*(volatile uint32_t*)0x06000020)
#define IOC_IOC_KBDCR (*(volatile uint32_t*)0x06000024)
#define IOC_IOC_RTCDR (*(volatile uint32_t*)0x06000028)
#define IOC_IOC_RTCCR (*(volatile uint32_t*)0x0600002C)
#define IOC_IOC_PRST (*(volatile uint32_t*)0x06000030)
#define IOC_IOC_PORTA (*(volatile uint32_t*)0x06000034)
#define IOC_IOC_PORTB (*(volatile uint32_t*)0x06000038)
#define IOC_IOC_PORTC (*(volatile uint32_t*)0x0600003C)

// Memory Controller (MEMC1)
#define MEMC_BASE 0x03200000
#define MEMC_MEMC_PT (*(volatile uint32_t*)0x06400000)
#define MEMC_MEMC_CTRL (*(volatile uint32_t*)0x06400004)
#define MEMC_MEMC_DRAM (*(volatile uint32_t*)0x06400008)
#define MEMC_MEMC_ERR (*(volatile uint32_t*)0x0640000C)

// Video Controller - VIDC1
#define VIDC_BASE 0x03400000
#define VIDC_VIDC_PALETTE (*(volatile uint32_t*)0x06800000)
#define VIDC_VIDC_STARTL (*(volatile uint32_t*)0x06800004)
#define VIDC_VIDC_STARTH (*(volatile uint32_t*)0x06800008)
#define VIDC_VIDC_CONFIG (*(volatile uint32_t*)0x0680000C)
#define VIDC_VIDC_HDISP (*(volatile uint32_t*)0x06800010)
#define VIDC_VIDC_VDISP (*(volatile uint32_t*)0x06800014)
#define VIDC_VIDC_HSYNC (*(volatile uint32_t*)0x06800018)
#define VIDC_VIDC_VSYNC (*(volatile uint32_t*)0x0680001C)
#define VIDC_VIDC_BORDER (*(volatile uint32_t*)0x06800020)
#define VIDC_VIDC_CURSOR (*(volatile uint32_t*)0x06800024)
#define VIDC_VIDC_SOUND (*(volatile uint32_t*)0x06800028)

// Intel 82710 Floppy Disk Controller
#define FDC_BASE 0x03010000
#define FDC_FDC_STATUS (*(volatile uint8_t*)0x06020000)
#define FDC_FDC_COMMAND (*(volatile uint8_t*)0x06020000)
#define FDC_FDC_TRACK (*(volatile uint8_t*)0x06020004)
#define FDC_FDC_SECTOR (*(volatile uint8_t*)0x06020008)
#define FDC_FDC_DATA (*(volatile uint8_t*)0x0602000C)

// Serial Port (via IOC)
#define SERIAL_BASE 0x03010010
#define SERIAL_SERIAL_TX (*(volatile uint8_t*)0x06020020)
#define SERIAL_SERIAL_RX (*(volatile uint8_t*)0x06020024)
#define SERIAL_SERIAL_CTRL (*(volatile uint8_t*)0x06020028)

// 中断向量定义
#define RESET_VECTOR 0  // Reset
#define UND_VECTOR 1  // Undefined instruction
#define SWI_VECTOR 2  // Software Interrupt (SWI/SVC)
#define PABORT_VECTOR 3  // Prefetch Abort
#define DABORT_VECTOR 4  // Data Abort
#define ADDRESS_VECTOR 5  // Address Exception
#define IRQ_VECTOR 6  // IRQ interrupt (IOC)
#define FIQ_VECTOR 7  // FIQ interrupt (VIDC)

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

void acorn_archimedes_a310_init(void);

#ifdef __cplusplus
}
#endif

#endif // ACORN_ARCHIMEDES_A310_HPP
