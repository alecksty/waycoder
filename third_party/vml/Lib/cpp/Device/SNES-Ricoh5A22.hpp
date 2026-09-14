#ifndef RICOH_5A22_HPP
#define RICOH_5A22_HPP

// Ricoh-5A22寄存器定义
// 生成自: Ricoh/MOS-6502/Ricoh-5A22
// 版本: 1.0
// 日期: 2026-04-16


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Ricoh-5A22
// 位宽: 16位
// 时钟频率: 3580000 Hz

// 寄存器定义
// Accumulator (8-bit, expandable to 16-bit)
#define A (*(volatile uint8_t*)0x00)

// Accumulator high byte when 16-bit
#define B (*(volatile uint8_t*)0x01)

// X Index Register (8/16-bit)
#define X (*(volatile uint8_t*)0x02)

// Y Index Register (8/16-bit)
#define Y (*(volatile uint8_t*)0x03)

// Stack Pointer (8-bit, banked)
#define SP (*(volatile uint8_t*)0x04)

// Program Counter (16-bit)
#define PC (*(volatile uint16_t*)0x06)

// Direct Page Register
#define D (*(volatile uint8_t*)0x08)

// Processor Status
#define P (*(volatile uint8_t*)0x0A)
#define P_N 0  // Negative
#define P_V 1  // Overflow
#define P_M 2  // Memory/Accumulator Select (0=16-bit, 1=8-bit)
#define P_X 3  // Index Select (0=16-bit, 1=8-bit)
#define P_B 4  // Break
#define P_D 5  // Decimal Mode
#define P_I 6  // Interrupt Disable
#define P_Z 7  // Zero
#define P_C 8  // Carry

// 内存段定义
// Work RAM (128KB internal)
#define WRAM_START 0x7E0000
#define WRAM_END 0x7FFFFF
#define WRAM_SIZE 131072

// Save RAM / Cartridge SRAM
#define SRAM_START 0x600000
#define SRAM_END 0x6FFFFF
#define SRAM_SIZE 1048576

// Cartridge ROM (LoROM/HiROM mapping)
#define CART_ROM_START 0x800000
#define CART_ROM_END 0xFFFFFF
#define CART_ROM_SIZE 8388608

// PPU1 Registers (background)
#define PPU1_REGS_START 0x2100
#define PPU1_REGS_END 0x213F
#define PPU1_REGS_SIZE 64

// PPU2 Registers (sprites)
#define PPU2_REGS_START 0x2140
#define PPU2_REGS_END 0x217F
#define PPU2_REGS_SIZE 64

// PPU3 Registers (extra)
#define PPU3_REGS_START 0x2180
#define PPU3_REGS_END 0x21FF
#define PPU3_REGS_SIZE 128

// APU I/O Registers
#define APU_REGS_START 0x2140
#define APU_REGS_END 0x217F
#define APU_REGS_SIZE 64

// CPU I/O Ports
#define CPU_IO_START 0x2000
#define CPU_IO_END 0x20FF
#define CPU_IO_SIZE 256

// DMA Channel Registers
#define DMA_REGS_START 0x4300
#define DMA_REGS_END 0x437F
#define DMA_REGS_SIZE 128

// HDMA Channel Registers
#define HDMA_REGS_START 0x4380
#define HDMA_REGS_END 0x43FF
#define HDMA_REGS_SIZE 128

// 外设定义
// Picture Processing Unit 1 - Background Rendering
#define PPU1_BASE 0x2100
#define PPU1_INIDISP (*(volatile uint8_t*)0x00004200)
#define PPU1_OBSEL (*(volatile uint8_t*)0x00004201)
#define PPU1_OAMADDL (*(volatile uint8_t*)0x00004202)
#define PPU1_OAMADDH (*(volatile uint8_t*)0x00004203)
#define PPU1_OAMDATA (*(volatile uint8_t*)0x00004204)
#define PPU1_BGMODE (*(volatile uint8_t*)0x00004205)
#define PPU1_MOSAIC (*(volatile uint8_t*)0x00004206)
#define PPU1_BG1SC (*(volatile uint8_t*)0x00004207)
#define PPU1_BG2SC (*(volatile uint8_t*)0x00004208)
#define PPU1_BG3SC (*(volatile uint8_t*)0x00004209)
#define PPU1_BG4SC (*(volatile uint8_t*)0x0000420A)
#define PPU1_BG12NBA (*(volatile uint8_t*)0x0000420B)
#define PPU1_BG34NBA (*(volatile uint8_t*)0x0000420C)
#define PPU1_BG1HOFS (*(volatile uint16_t*)0x0000420D)
#define PPU1_BG1VOFS (*(volatile uint16_t*)0x0000420E)
#define PPU1_BG2HOFS (*(volatile uint16_t*)0x0000420F)
#define PPU1_BG2VOFS (*(volatile uint16_t*)0x00004210)
#define PPU1_BG3HOFS (*(volatile uint16_t*)0x00004211)
#define PPU1_BG3VOFS (*(volatile uint16_t*)0x00004212)
#define PPU1_BG4HOFS (*(volatile uint16_t*)0x00004213)
#define PPU1_BG4VOFS (*(volatile uint16_t*)0x00004214)
#define PPU1_VMAIN (*(volatile uint8_t*)0x00004215)
#define PPU1_VMADDL (*(volatile uint8_t*)0x00004216)
#define PPU1_VMADDH (*(volatile uint8_t*)0x00004217)
#define PPU1_VMDATAL (*(volatile uint8_t*)0x00004218)
#define PPU1_VMDATAH (*(volatile uint8_t*)0x00004219)
#define PPU1_M7SEL (*(volatile uint8_t*)0x0000421A)
#define PPU1_M7A (*(volatile uint16_t*)0x0000421B)
#define PPU1_M7B (*(volatile uint16_t*)0x0000421C)
#define PPU1_M7C (*(volatile uint16_t*)0x0000421D)
#define PPU1_M7D (*(volatile uint16_t*)0x0000421E)
#define PPU1_M7X (*(volatile uint16_t*)0x0000421F)
#define PPU1_M7Y (*(volatile uint16_t*)0x00004220)
#define PPU1_CGADD (*(volatile uint8_t*)0x00004221)
#define PPU1_CGDATA (*(volatile uint8_t*)0x00004222)
#define PPU1_W12SEL (*(volatile uint8_t*)0x00004223)
#define PPU1_W34SEL (*(volatile uint8_t*)0x00004224)
#define PPU1_WOBJSEL (*(volatile uint8_t*)0x00004225)
#define PPU1_WH0 (*(volatile uint8_t*)0x00004226)
#define PPU1_WH1 (*(volatile uint8_t*)0x00004227)
#define PPU1_WH2 (*(volatile uint8_t*)0x00004228)
#define PPU1_WH3 (*(volatile uint8_t*)0x00004229)
#define PPU1_WBGLOG (*(volatile uint8_t*)0x0000422A)
#define PPU1_WOBJLOG (*(volatile uint8_t*)0x0000422B)
#define PPU1_TM (*(volatile uint8_t*)0x0000422C)
#define PPU1_TS (*(volatile uint8_t*)0x0000422D)
#define PPU1_TMW (*(volatile uint8_t*)0x0000422E)
#define PPU1_TSW (*(volatile uint8_t*)0x0000422F)
#define PPU1_CGSWSEL (*(volatile uint8_t*)0x00004230)
#define PPU1_CGADSUB (*(volatile uint8_t*)0x00004231)
#define PPU1_SETINI (*(volatile uint8_t*)0x00004233)

// Picture Processing Unit 2 - Sprite Rendering
#define PPU2_BASE 0x2140
#define PPU2_OAMDATAREAD (*(volatile uint8_t*)0x00004278)
#define PPU2_VMDATAREAD (*(volatile uint8_t*)0x00004279)
#define PPU2_VMDATAHREAD (*(volatile uint8_t*)0x0000427A)
#define PPU2_CGDATAREAD (*(volatile uint8_t*)0x0000427B)
#define PPU2_OPHCT (*(volatile uint8_t*)0x0000427C)
#define PPU2_OPVCT (*(volatile uint8_t*)0x0000427D)
#define PPU2_STAT78 (*(volatile uint8_t*)0x0000427F)

// Sony SPC700 Audio CPU (8-bit)
#define SPC700_BASE 0x00
#define SPC700_PC (*(volatile uint16_t*)0x00000000)
#define SPC700_A (*(volatile uint8_t*)0x00000002)
#define SPC700_X (*(volatile uint8_t*)0x00000003)
#define SPC700_Y (*(volatile uint8_t*)0x00000004)
#define SPC700_SP (*(volatile uint8_t*)0x00000005)
#define SPC700_PSW (*(volatile uint8_t*)0x00000006)
#define SPC700_TEST (*(volatile uint8_t*)0x0000000F)

// S-DSP Audio DSP (8-channel ADPCM)
#define DSP_BASE 0x00
#define DSP_MVOL_L (*(volatile uint8_t*)0x0000000C)
#define DSP_MVOL_R (*(volatile uint8_t*)0x0000001C)
#define DSP_EVOL_L (*(volatile uint8_t*)0x0000002C)
#define DSP_EVOL_R (*(volatile uint8_t*)0x0000003C)
#define DSP_KON (*(volatile uint8_t*)0x0000004C)
#define DSP_KOFF (*(volatile uint8_t*)0x0000005C)
#define DSP_KONKOFF (*(volatile uint8_t*)0x0000004D)
#define DSP_FLG (*(volatile uint8_t*)0x0000006C)
#define DSP_ENDX (*(volatile uint8_t*)0x0000007D)
#define DSP_EBUST (*(volatile uint8_t*)0x0000006D)
#define DSP_EDL (*(volatile uint8_t*)0x0000007D)
#define DSP_ENV0 (*(volatile uint8_t*)0x00000000)
#define DSP_OUT0 (*(volatile uint8_t*)0x0000001C)
#define DSP_ENV1 (*(volatile uint8_t*)0x00000001)
#define DSP_OUT1 (*(volatile uint8_t*)0x0000002C)
#define DSP_ENV2 (*(volatile uint8_t*)0x00000002)
#define DSP_OUT2 (*(volatile uint8_t*)0x0000003C)
#define DSP_ENV3 (*(volatile uint8_t*)0x00000003)
#define DSP_OUT3 (*(volatile uint8_t*)0x0000004C)
#define DSP_ENV4 (*(volatile uint8_t*)0x00000004)
#define DSP_OUT4 (*(volatile uint8_t*)0x0000005C)
#define DSP_ENV5 (*(volatile uint8_t*)0x00000005)
#define DSP_OUT5 (*(volatile uint8_t*)0x0000006C)
#define DSP_ENV6 (*(volatile uint8_t*)0x00000006)
#define DSP_OUT6 (*(volatile uint8_t*)0x0000007C)
#define DSP_ENV7 (*(volatile uint8_t*)0x00000007)
#define DSP_OUT7 (*(volatile uint8_t*)0x0000000D)
#define DSP_V0SRC (*(volatile uint8_t*)0x00000008)
#define DSP_V1SRC (*(volatile uint8_t*)0x00000009)
#define DSP_V2SRC (*(volatile uint8_t*)0x0000000A)
#define DSP_V3SRC (*(volatile uint8_t*)0x0000000B)
#define DSP_V4SRC (*(volatile uint8_t*)0x00000018)
#define DSP_V5SRC (*(volatile uint8_t*)0x00000019)
#define DSP_V6SRC (*(volatile uint8_t*)0x0000001A)
#define DSP_V7SRC (*(volatile uint8_t*)0x0000001B)
#define DSP_V0PITCHL (*(volatile uint8_t*)0x00000002)
#define DSP_V0PITCHH (*(volatile uint8_t*)0x00000003)
#define DSP_V1PITCHL (*(volatile uint8_t*)0x00000012)
#define DSP_V1PITCHH (*(volatile uint8_t*)0x00000013)
#define DSP_V2PITCHL (*(volatile uint8_t*)0x00000022)
#define DSP_V2PITCHH (*(volatile uint8_t*)0x00000023)
#define DSP_V3PITCHL (*(volatile uint8_t*)0x00000032)
#define DSP_V3PITCHH (*(volatile uint8_t*)0x00000033)
#define DSP_V4PITCHL (*(volatile uint8_t*)0x00000042)
#define DSP_V4PITCHH (*(volatile uint8_t*)0x00000043)
#define DSP_V5PITCHL (*(volatile uint8_t*)0x00000052)
#define DSP_V5PITCHH (*(volatile uint8_t*)0x00000053)
#define DSP_V6PITCHL (*(volatile uint8_t*)0x00000062)
#define DSP_V6PITCHH (*(volatile uint8_t*)0x00000063)
#define DSP_V7PITCHL (*(volatile uint8_t*)0x00000072)
#define DSP_V7PITCHH (*(volatile uint8_t*)0x00000073)
#define DSP_V0ADSR0 (*(volatile uint8_t*)0x00000004)
#define DSP_V0ADSR1 (*(volatile uint8_t*)0x00000005)
#define DSP_V0ADSR2 (*(volatile uint8_t*)0x00000006)
#define DSP_V1ADSR0 (*(volatile uint8_t*)0x00000014)
#define DSP_V1ADSR1 (*(volatile uint8_t*)0x00000015)
#define DSP_V1ADSR2 (*(volatile uint8_t*)0x00000016)
#define DSP_V2ADSR0 (*(volatile uint8_t*)0x00000024)
#define DSP_V2ADSR1 (*(volatile uint8_t*)0x00000025)
#define DSP_V2ADSR2 (*(volatile uint8_t*)0x00000026)
#define DSP_V3ADSR0 (*(volatile uint8_t*)0x00000034)
#define DSP_V3ADSR1 (*(volatile uint8_t*)0x00000035)
#define DSP_V3ADSR2 (*(volatile uint8_t*)0x00000036)
#define DSP_V4ADSR0 (*(volatile uint8_t*)0x00000044)
#define DSP_V4ADSR1 (*(volatile uint8_t*)0x00000045)
#define DSP_V4ADSR2 (*(volatile uint8_t*)0x00000046)
#define DSP_V5ADSR0 (*(volatile uint8_t*)0x00000054)
#define DSP_V5ADSR1 (*(volatile uint8_t*)0x00000055)
#define DSP_V5ADSR2 (*(volatile uint8_t*)0x00000056)
#define DSP_V6ADSR0 (*(volatile uint8_t*)0x00000064)
#define DSP_V6ADSR1 (*(volatile uint8_t*)0x00000065)
#define DSP_V6ADSR2 (*(volatile uint8_t*)0x00000066)
#define DSP_V7ADSR0 (*(volatile uint8_t*)0x00000074)
#define DSP_V7ADSR1 (*(volatile uint8_t*)0x00000075)
#define DSP_V7ADSR2 (*(volatile uint8_t*)0x00000076)
#define DSP_V0GAIN (*(volatile uint8_t*)0x00000007)
#define DSP_V1GAIN (*(volatile uint8_t*)0x00000017)
#define DSP_V2GAIN (*(volatile uint8_t*)0x00000027)
#define DSP_V3GAIN (*(volatile uint8_t*)0x00000037)
#define DSP_V4GAIN (*(volatile uint8_t*)0x00000047)
#define DSP_V5GAIN (*(volatile uint8_t*)0x00000057)
#define DSP_V6GAIN (*(volatile uint8_t*)0x00000067)
#define DSP_V7GAIN (*(volatile uint8_t*)0x00000077)
#define DSP_V0WAVE (*(volatile uint8_t*)0x0000000D)
#define DSP_V1WAVE (*(volatile uint8_t*)0x0000001D)
#define DSP_V2WAVE (*(volatile uint8_t*)0x0000002D)
#define DSP_V3WAVE (*(volatile uint8_t*)0x0000003D)
#define DSP_V4WAVE (*(volatile uint8_t*)0x0000004D)
#define DSP_V5WAVE (*(volatile uint8_t*)0x0000005D)
#define DSP_V6WAVE (*(volatile uint8_t*)0x0000006D)
#define DSP_V7WAVE (*(volatile uint8_t*)0x0000007D)

// Direct Memory Access Controller
#define DMA_BASE 0x4300
#define DMA_DMAP0 (*(volatile uint8_t*)0x00008600)
#define DMA_BBAD0 (*(volatile uint8_t*)0x00008601)
#define DMA_A1T0L (*(volatile uint8_t*)0x00008602)
#define DMA_A1T0H (*(volatile uint8_t*)0x00008603)
#define DMA_A1B0 (*(volatile uint8_t*)0x00008604)
#define DMA_DAS0L (*(volatile uint8_t*)0x00008605)
#define DMA_DAS0H (*(volatile uint8_t*)0x00008606)
#define DMA_DASB0 (*(volatile uint8_t*)0x00008607)
#define DMA_A2A0 (*(volatile uint8_t*)0x00008608)
#define DMA_A2A1 (*(volatile uint8_t*)0x00008609)
#define DMA_A2B0 (*(volatile uint8_t*)0x0000860A)
#define DMA_NTT0 (*(volatile uint8_t*)0x0000860B)
#define DMA_DMAP1 (*(volatile uint8_t*)0x00008610)
#define DMA_BBAD1 (*(volatile uint8_t*)0x00008611)
#define DMA_A1T1L (*(volatile uint8_t*)0x00008612)
#define DMA_A1T1H (*(volatile uint8_t*)0x00008613)
#define DMA_A1B1 (*(volatile uint8_t*)0x00008614)
#define DMA_DAS1L (*(volatile uint8_t*)0x00008615)
#define DMA_DAS1H (*(volatile uint8_t*)0x00008616)
#define DMA_DASB1 (*(volatile uint8_t*)0x00008617)
#define DMA_DMAP2 (*(volatile uint8_t*)0x00008620)
#define DMA_BBAD2 (*(volatile uint8_t*)0x00008621)
#define DMA_A1T2L (*(volatile uint8_t*)0x00008622)
#define DMA_A1T2H (*(volatile uint8_t*)0x00008623)
#define DMA_A1B2 (*(volatile uint8_t*)0x00008624)
#define DMA_DAS2L (*(volatile uint8_t*)0x00008625)
#define DMA_DAS2H (*(volatile uint8_t*)0x00008626)
#define DMA_DASB2 (*(volatile uint8_t*)0x00008627)
#define DMA_DMAP3 (*(volatile uint8_t*)0x00008630)
#define DMA_BBAD3 (*(volatile uint8_t*)0x00008631)
#define DMA_A1T3L (*(volatile uint8_t*)0x00008632)
#define DMA_A1T3H (*(volatile uint8_t*)0x00008633)
#define DMA_A1B3 (*(volatile uint8_t*)0x00008634)
#define DMA_DAS3L (*(volatile uint8_t*)0x00008635)
#define DMA_DAS3H (*(volatile uint8_t*)0x00008636)
#define DMA_DASB3 (*(volatile uint8_t*)0x00008637)
#define DMA_MDMAEN (*(volatile uint8_t*)0x00008650)

// Horizontal DMA (scanline-based)
#define HDMA_BASE 0x4380
#define HDMA_HDMAP0 (*(volatile uint8_t*)0x00008700)
#define HDMA_HBAD0 (*(volatile uint8_t*)0x00008701)
#define HDMA_A1T0L (*(volatile uint8_t*)0x00008702)
#define HDMA_A1T0H (*(volatile uint8_t*)0x00008703)
#define HDMA_A1B0 (*(volatile uint8_t*)0x00008704)
#define HDMA_DAS0L (*(volatile uint8_t*)0x00008705)
#define HDMA_DAS0H (*(volatile uint8_t*)0x00008706)
#define HDMA_HDMAP1 (*(volatile uint8_t*)0x00008708)
#define HDMA_HBAD1 (*(volatile uint8_t*)0x00008709)
#define HDMA_A1T1L (*(volatile uint8_t*)0x0000870A)
#define HDMA_A1T1H (*(volatile uint8_t*)0x0000870B)
#define HDMA_A1B1 (*(volatile uint8_t*)0x0000870C)
#define HDMA_DAS1L (*(volatile uint8_t*)0x0000870D)
#define HDMA_DAS1H (*(volatile uint8_t*)0x0000870E)
#define HDMA_HDMAP2 (*(volatile uint8_t*)0x00008710)
#define HDMA_HBAD2 (*(volatile uint8_t*)0x00008711)
#define HDMA_A1T2L (*(volatile uint8_t*)0x00008712)
#define HDMA_A1T2H (*(volatile uint8_t*)0x00008713)
#define HDMA_A1B2 (*(volatile uint8_t*)0x00008714)
#define HDMA_DAS2L (*(volatile uint8_t*)0x00008715)
#define HDMA_DAS2H (*(volatile uint8_t*)0x00008716)
#define HDMA_HDMAP3 (*(volatile uint8_t*)0x00008718)
#define HDMA_HBAD3 (*(volatile uint8_t*)0x00008719)
#define HDMA_A1T3L (*(volatile uint8_t*)0x0000871A)
#define HDMA_A1T3H (*(volatile uint8_t*)0x0000871B)
#define HDMA_A1B3 (*(volatile uint8_t*)0x0000871C)
#define HDMA_DAS3L (*(volatile uint8_t*)0x0000871D)
#define HDMA_DAS3H (*(volatile uint8_t*)0x0000871E)
#define HDMA_HDMAEN (*(volatile uint8_t*)0x00008770)

// Controller Port 1
#define CONTROLLER1_BASE 0x4016
#define CONTROLLER1_JOYPAD1 (*(volatile uint8_t*)0x0000802C)
#define CONTROLLER1_JOYSTROBE (*(volatile uint8_t*)0x0000802C)

// Controller Port 2
#define CONTROLLER2_BASE 0x4017
#define CONTROLLER2_JOYPAD2 (*(volatile uint8_t*)0x0000802E)
#define CONTROLLER2_RDNMI (*(volatile uint8_t*)0x00008227)
#define CONTROLLER2_TIMEUP (*(volatile uint8_t*)0x00008228)
#define CONTROLLER2_HVBJOY (*(volatile uint8_t*)0x00008229)

// Timer / IRQ Control
#define TIMER_BASE 0x4200
#define TIMER_NMITIMEN (*(volatile uint8_t*)0x00008400)
#define TIMER_NMITIMEN_VBLANK_NMI 7  // V-Blank NMI Enable
#define TIMER_NMITIMEN_HTIMER_EN 4  // H-Counter IRQ Enable
#define TIMER_NMITIMEN_VTIMER_EN 5  // V-Counter IRQ Enable
#define TIMER_WRI00 (*(volatile uint8_t*)0x00008401)
#define TIMER_HTIMEL (*(volatile uint8_t*)0x00008402)
#define TIMER_HTIMEH (*(volatile uint8_t*)0x00008403)
#define TIMER_VTIMEL (*(volatile uint8_t*)0x00008404)
#define TIMER_VTIMEH (*(volatile uint8_t*)0x00008405)
#define TIMER_MEMSEL (*(volatile uint8_t*)0x0000840D)

// 中断向量定义
#define RESET_VECTOR 0  // Reset
#define NMI_VECTOR 1  // Non-Maskable Interrupt (V-Blank)
#define IRQ_VECTOR 2  // IRQ / BRK (Timer, HDMA, Controller)
#define TIMER_IRQ_VECTOR 3  // H/V Counter Timer IRQ

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

void ricoh_5a22_init(void);

#ifdef __cplusplus
}
#endif

#endif // RICOH_5A22_HPP
