#ifndef SEGA_GENESIS_HPP
#define SEGA_GENESIS_HPP

// Sega-Genesis寄存器定义
// 生成自: Sega/Genesis/Mega Drive/Sega-Genesis
// 版本: 1.0
// 日期: 2026-04-17


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Motorola 68000
// 位宽: 32位
// 时钟频率: 7670000 Hz

// 寄存器定义
// Data Register 0
#define D0 (*(volatile uint32_t*)0)

// Data Register 1
#define D1 (*(volatile uint32_t*)0)

// Data Register 2
#define D2 (*(volatile uint32_t*)0)

// Data Register 3
#define D3 (*(volatile uint32_t*)0)

// Data Register 4
#define D4 (*(volatile uint32_t*)0)

// Data Register 5
#define D5 (*(volatile uint32_t*)0)

// Data Register 6
#define D6 (*(volatile uint32_t*)0)

// Data Register 7
#define D7 (*(volatile uint32_t*)0)

// Address Register 0
#define A0 (*(volatile uint32_t*)0)

// Address Register 1
#define A1 (*(volatile uint32_t*)0)

// Address Register 2
#define A2 (*(volatile uint32_t*)0)

// Address Register 3
#define A3 (*(volatile uint32_t*)0)

// Address Register 4
#define A4 (*(volatile uint32_t*)0)

// Address Register 5
#define A5 (*(volatile uint32_t*)0)

// Address Register 6
#define A6 (*(volatile uint32_t*)0)

// Address Register 7 (SP)
#define A7 (*(volatile uint32_t*)0)

// Program Counter
#define PC (*(volatile uint32_t*)0)

// Status Register
#define SR (*(volatile uint16_t*)0)

// 外设定义
// Video Display Processor (315-5313)
#define VDP_BASE 
#define VDP_VDP_DATA (*(volatile uint16_t*)0x00C00000)
#define VDP_VDP_CONTROL (*(volatile uint16_t*)0x00C00004)
#define VDP_VDP_HVCOUNTER (*(volatile uint16_t*)0x00C00008)
#define VDP_VDP_PSG (*(volatile uint8_t*)0x00C00011)

// FM synthesis sound chip
#define YM2612_BASE 
#define YM2612_YM2612_ADDR0 (*(volatile uint8_t*)0x00A04000)
#define YM2612_YM2612_DATA0 (*(volatile uint8_t*)0x00A04001)
#define YM2612_YM2612_ADDR1 (*(volatile uint8_t*)0x00A04002)
#define YM2612_YM2612_DATA1 (*(volatile uint8_t*)0x00A04003)

// I/O ports
#define IOPORTS_BASE 
#define IOPORTS_IO_DATA1 (*(volatile uint8_t*)0x00A10002)
#define IOPORTS_IO_DATA2 (*(volatile uint8_t*)0x00A10004)
#define IOPORTS_IO_DATA3 (*(volatile uint8_t*)0x00A10006)
#define IOPORTS_IO_CTRL1 (*(volatile uint8_t*)0x00A10008)
#define IOPORTS_IO_CTRL2 (*(volatile uint8_t*)0x00A1000A)
#define IOPORTS_IO_CTRL3 (*(volatile uint8_t*)0x00A1000C)

// TradeMark Security System
#define TMSS_BASE 
#define TMSS_TMSS (*(volatile uint8_t*)0x00A14000)

// Z80 bus control
#define Z80BUS_BASE 
#define Z80BUS_Z80_BUSREQ (*(volatile uint16_t*)0x00A11100)
#define Z80BUS_Z80_RESET (*(volatile uint16_t*)0x00A11200)
#define Z80BUS_Z80_YM2612 (*(volatile uint32_t*)0x00A04000)

// 中断向量定义
#define RESET_SP_VECTOR 0  // Reset (Initial SP)
#define RESET_PC_VECTOR 4  // Reset (Initial PC)
#define HBLANK_VECTOR 24  // Horizontal blank interrupt
#define VBLANK_VECTOR 28  // Vertical blank interrupt
#define EXTINT1_VECTOR 32  // External interrupt 1
#define EXTINT2_VECTOR 36  // External interrupt 2
#define EXTINT3_VECTOR 40  // External interrupt 3
#define EXTINT4_VECTOR 44  // External interrupt 4
#define EXTINT5_VECTOR 48  // External interrupt 5
#define EXTINT6_VECTOR 52  // External interrupt 6
#define EXTINT7_VECTOR 56  // External interrupt 7

void sega_genesis_init(void);

#ifdef __cplusplus
}
#endif

#endif // SEGA_GENESIS_HPP
