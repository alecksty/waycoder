#ifndef COMMODORE_64_HPP
#define COMMODORE_64_HPP

// Commodore-64寄存器定义
// 生成自: Commodore International/Commodore 64/Commodore-64
// 版本: 1.0
// 日期: 2026-04-17


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: MOS 6510
// 位宽: 8位
// 时钟频率: 985248 Hz

// 寄存器定义
// Accumulator
#define A (*(volatile uint8_t*)0)

// Index Register X
#define X (*(volatile uint8_t*)0)

// Index Register Y
#define Y (*(volatile uint8_t*)0)

// Stack Pointer
#define SP (*(volatile uint8_t*)0)

// Program Counter
#define PC (*(volatile uint16_t*)0)

// Status Register
#define P (*(volatile uint8_t*)0)

// I/O Port (6510 specific)
#define PORT (*(volatile uint8_t*)1)

// 外设定义
// Video Interface Chip II
#define VIC_II_BASE 
#define VIC_II_VIC_CTRL1 (*(volatile uint8_t*)0x0000D011)
#define VIC_II_VIC_CTRL2 (*(volatile uint8_t*)0x0000D016)
#define VIC_II_VIC_RASTER (*(volatile uint8_t*)0x0000D012)
#define VIC_II_VIC_MEMPTR (*(volatile uint8_t*)0x0000D018)
#define VIC_II_VIC_IRQ (*(volatile uint8_t*)0x0000D019)
#define VIC_II_VIC_IRQMASK (*(volatile uint8_t*)0x0000D01A)
#define VIC_II_VIC_BORDER (*(volatile uint8_t*)0x0000D020)
#define VIC_II_VIC_BG0 (*(volatile uint8_t*)0x0000D021)
#define VIC_II_VIC_BG1 (*(volatile uint8_t*)0x0000D022)
#define VIC_II_VIC_BG2 (*(volatile uint8_t*)0x0000D023)
#define VIC_II_VIC_BG3 (*(volatile uint8_t*)0x0000D024)
#define VIC_II_VIC_SPRITE0_X (*(volatile uint8_t*)0x0000D000)
#define VIC_II_VIC_SPRITE0_Y (*(volatile uint8_t*)0x0000D001)
#define VIC_II_VIC_SPRITE1_X (*(volatile uint8_t*)0x0000D002)
#define VIC_II_VIC_SPRITE1_Y (*(volatile uint8_t*)0x0000D003)

// Sound Interface Device (6581)
#define SID_BASE 
#define SID_SID_VOICE1_FREQ_LO (*(volatile uint8_t*)0x0000D400)
#define SID_SID_VOICE1_FREQ_HI (*(volatile uint8_t*)0x0000D401)
#define SID_SID_VOICE1_PW_LO (*(volatile uint8_t*)0x0000D402)
#define SID_SID_VOICE1_PW_HI (*(volatile uint8_t*)0x0000D403)
#define SID_SID_VOICE1_CTRL (*(volatile uint8_t*)0x0000D404)
#define SID_SID_VOICE1_AD (*(volatile uint8_t*)0x0000D405)
#define SID_SID_VOICE1_SR (*(volatile uint8_t*)0x0000D406)
#define SID_SID_VOICE2_FREQ_LO (*(volatile uint8_t*)0x0000D407)
#define SID_SID_VOICE2_FREQ_HI (*(volatile uint8_t*)0x0000D408)
#define SID_SID_VOICE2_PW_LO (*(volatile uint8_t*)0x0000D409)
#define SID_SID_VOICE2_PW_HI (*(volatile uint8_t*)0x0000D40A)
#define SID_SID_VOICE2_CTRL (*(volatile uint8_t*)0x0000D40B)
#define SID_SID_VOICE2_AD (*(volatile uint8_t*)0x0000D40C)
#define SID_SID_VOICE2_SR (*(volatile uint8_t*)0x0000D40D)
#define SID_SID_VOICE3_FREQ_LO (*(volatile uint8_t*)0x0000D40E)
#define SID_SID_VOICE3_FREQ_HI (*(volatile uint8_t*)0x0000D40F)
#define SID_SID_VOICE3_PW_LO (*(volatile uint8_t*)0x0000D410)
#define SID_SID_VOICE3_PW_HI (*(volatile uint8_t*)0x0000D411)
#define SID_SID_VOICE3_CTRL (*(volatile uint8_t*)0x0000D412)
#define SID_SID_VOICE3_AD (*(volatile uint8_t*)0x0000D413)
#define SID_SID_VOICE3_SR (*(volatile uint8_t*)0x0000D414)
#define SID_SID_FILTER_CUTOFF_LO (*(volatile uint8_t*)0x0000D415)
#define SID_SID_FILTER_CUTOFF_HI (*(volatile uint8_t*)0x0000D416)
#define SID_SID_FILTER_CTRL (*(volatile uint8_t*)0x0000D417)
#define SID_SID_VOLUME (*(volatile uint8_t*)0x0000D418)
#define SID_SID_POTX (*(volatile uint8_t*)0x0000D419)
#define SID_SID_POTY (*(volatile uint8_t*)0x0000D41A)
#define SID_SID_OSC3 (*(volatile uint8_t*)0x0000D41B)
#define SID_SID_ENV3 (*(volatile uint8_t*)0x0000D41C)

// Complex Interface Adapter 1 (6526)
#define CIA1_BASE 
#define CIA1_CIA1_PRA (*(volatile uint8_t*)0x0000DC00)
#define CIA1_CIA1_PRB (*(volatile uint8_t*)0x0000DC01)
#define CIA1_CIA1_DDRA (*(volatile uint8_t*)0x0000DC02)
#define CIA1_CIA1_DDRB (*(volatile uint8_t*)0x0000DC03)
#define CIA1_CIA1_TALO (*(volatile uint8_t*)0x0000DC04)
#define CIA1_CIA1_TAHI (*(volatile uint8_t*)0x0000DC05)
#define CIA1_CIA1_TBLO (*(volatile uint8_t*)0x0000DC06)
#define CIA1_CIA1_TBHI (*(volatile uint8_t*)0x0000DC07)
#define CIA1_CIA1_TODTEN (*(volatile uint8_t*)0x0000DC08)
#define CIA1_CIA1_TODSEC (*(volatile uint8_t*)0x0000DC09)
#define CIA1_CIA1_TODMIN (*(volatile uint8_t*)0x0000DC0A)
#define CIA1_CIA1_TODHR (*(volatile uint8_t*)0x0000DC0B)
#define CIA1_CIA1_SDR (*(volatile uint8_t*)0x0000DC0C)
#define CIA1_CIA1_ICR (*(volatile uint8_t*)0x0000DC0D)
#define CIA1_CIA1_CRA (*(volatile uint8_t*)0x0000DC0E)
#define CIA1_CIA1_CRB (*(volatile uint8_t*)0x0000DC0F)

// Complex Interface Adapter 2 (6526)
#define CIA2_BASE 
#define CIA2_CIA2_PRA (*(volatile uint8_t*)0x0000DD00)
#define CIA2_CIA2_PRB (*(volatile uint8_t*)0x0000DD01)
#define CIA2_CIA2_DDRA (*(volatile uint8_t*)0x0000DD02)
#define CIA2_CIA2_DDRB (*(volatile uint8_t*)0x0000DD03)
#define CIA2_CIA2_TALO (*(volatile uint8_t*)0x0000DD04)
#define CIA2_CIA2_TAHI (*(volatile uint8_t*)0x0000DD05)
#define CIA2_CIA2_TBLO (*(volatile uint8_t*)0x0000DD06)
#define CIA2_CIA2_TBHI (*(volatile uint8_t*)0x0000DD07)
#define CIA2_CIA2_TODTEN (*(volatile uint8_t*)0x0000DD08)
#define CIA2_CIA2_TODSEC (*(volatile uint8_t*)0x0000DD09)
#define CIA2_CIA2_TODMIN (*(volatile uint8_t*)0x0000DD0A)
#define CIA2_CIA2_TODHR (*(volatile uint8_t*)0x0000DD0B)
#define CIA2_CIA2_SDR (*(volatile uint8_t*)0x0000DD0C)
#define CIA2_CIA2_ICR (*(volatile uint8_t*)0x0000DD0D)
#define CIA2_CIA2_CRA (*(volatile uint8_t*)0x0000DD0E)
#define CIA2_CIA2_CRB (*(volatile uint8_t*)0x0000DD0F)

// 中断向量定义
#define IRQ_VECTOR 65532  // Maskable Interrupt
#define NMI_VECTOR 65534  // Non-Maskable Interrupt
#define RESET_VECTOR 65526  // Reset Vector

void commodore_64_init(void);

#ifdef __cplusplus
}
#endif

#endif // COMMODORE_64_HPP
