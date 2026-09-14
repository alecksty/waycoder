#ifndef COMMODORE_64_HPP
#define COMMODORE_64_HPP

// Commodore-64寄存器定义
// 生成自: Commodore/C64/Commodore-64
// 版本: 1.0
// 日期: 2026-04-17


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: MOS-6510
// 位宽: 8位
// 时钟频率: 1022727 Hz

// 寄存器定义
// Accumulator
#define A (*(volatile uint8_t*)0x00)

// X Index Register
#define X (*(volatile uint8_t*)0x01)

// Y Index Register
#define Y (*(volatile uint8_t*)0x02)

// Stack Pointer
#define SP (*(volatile uint8_t*)0x03)

// Program Counter
#define PC (*(volatile uint16_t*)0x04)

// Processor Status
#define P (*(volatile uint8_t*)0x06)
#define P_C 0  // Carry Flag
#define P_Z 1  // Zero Flag
#define P_I 2  // Interrupt Disable
#define P_D 3  // Decimal Mode
#define P_B 4  // Break Flag
#define P_U 5  // Unused
#define P_V 6  // Overflow Flag
#define P_N 7  // Negative Flag

// I/O Port (6510 only: DDR + data)
#define PORT (*(volatile uint8_t*)0x00)

// 内存段定义
// 64KB main RAM
#define RAM_START 0x0000
#define RAM_END 0xFFFF
#define RAM_SIZE 65536

// BASIC interpreter ROM
#define BASIC_ROM_START 0xA000
#define BASIC_ROM_END 0xBFFF
#define BASIC_ROM_SIZE 8192

// KERNAL operating system ROM
#define KERNAL_ROM_START 0xE000
#define KERNAL_ROM_END 0xFFFF
#define KERNAL_ROM_SIZE 8192

// Character generator ROM
#define CHAR_ROM_START 0xD000
#define CHAR_ROM_END 0xDFFF
#define CHAR_ROM_SIZE 4096

// I/O + RAM window (switchable)
#define IO_RAM_START 0xD000
#define IO_RAM_END 0xDFFF
#define IO_RAM_SIZE 4096

// 外设定义
// Video Interface Chip II - 6567/6569
#define VICII_BASE 0xD000
#define VICII_SP0X (*(volatile uint8_t*)0x0001A000)
#define VICII_SP0Y (*(volatile uint8_t*)0x0001A001)
#define VICII_SP1X (*(volatile uint8_t*)0x0001A002)
#define VICII_SP1Y (*(volatile uint8_t*)0x0001A003)
#define VICII_SP2X (*(volatile uint8_t*)0x0001A004)
#define VICII_SP2Y (*(volatile uint8_t*)0x0001A005)
#define VICII_SP3X (*(volatile uint8_t*)0x0001A006)
#define VICII_SP3Y (*(volatile uint8_t*)0x0001A007)
#define VICII_SP4X (*(volatile uint8_t*)0x0001A008)
#define VICII_SP4Y (*(volatile uint8_t*)0x0001A009)
#define VICII_SP5X (*(volatile uint8_t*)0x0001A00A)
#define VICII_SP5Y (*(volatile uint8_t*)0x0001A00B)
#define VICII_SP6X (*(volatile uint8_t*)0x0001A00C)
#define VICII_SP6Y (*(volatile uint8_t*)0x0001A00D)
#define VICII_SP7X (*(volatile uint8_t*)0x0001A00E)
#define VICII_SP7Y (*(volatile uint8_t*)0x0001A00F)
#define VICII_MSIGX (*(volatile uint8_t*)0x0001A010)
#define VICII_SCROLY (*(volatile uint8_t*)0x0001A011)
#define VICII_SCROLX (*(volatile uint8_t*)0x0001A016)
#define VICII_YPSTOP (*(volatile uint8_t*)0x0001A012)
#define VICII_LPX (*(volatile uint8_t*)0x0001A013)
#define VICII_LPY (*(volatile uint8_t*)0x0001A014)
#define VICII_SPENA (*(volatile uint8_t*)0x0001A015)
#define VICII_CSPMC (*(volatile uint8_t*)0x0001A017)
#define VICII_MM0 (*(volatile uint8_t*)0x0001A018)
#define VICII_VM01 (*(volatile uint8_t*)0x0001A016)
#define VICII_VICBAS (*(volatile uint8_t*)0x0001A018)
#define VICII_IRQMASK (*(volatile uint8_t*)0x0001A019)
#define VICII_IRQST (*(volatile uint8_t*)0x0001A01A)
#define VICII_SPBGPR (*(volatile uint8_t*)0x0001A01B)
#define VICII_SPMC (*(volatile uint8_t*)0x0001A01C)
#define VICII_SP1C (*(volatile uint8_t*)0x0001A025)
#define VICII_SP2C (*(volatile uint8_t*)0x0001A026)
#define VICII_SPBC (*(volatile uint8_t*)0x0001A027)
#define VICII_SP1C0 (*(volatile uint8_t*)0x0001A028)
#define VICII_SP2C0 (*(volatile uint8_t*)0x0001A029)
#define VICII_SP3C0 (*(volatile uint8_t*)0x0001A02A)
#define VICII_SP4C0 (*(volatile uint8_t*)0x0001A02B)
#define VICII_SP5C0 (*(volatile uint8_t*)0x0001A02C)
#define VICII_SP6C0 (*(volatile uint8_t*)0x0001A02D)
#define VICII_SP7C0 (*(volatile uint8_t*)0x0001A02E)
#define VICII_REG_FD (*(volatile uint8_t*)0x0001A01D)
#define VICII_BGCOL0 (*(volatile uint8_t*)0x0001A021)
#define VICII_BGCOL1 (*(volatile uint8_t*)0x0001A022)
#define VICII_BGCOL2 (*(volatile uint8_t*)0x0001A023)
#define VICII_BGCOL3 (*(volatile uint8_t*)0x0001A024)

// Sound Interface Device 6581/8580
#define SID_BASE 0xD400
#define SID_FREQ1LO (*(volatile uint8_t*)0x0001A800)
#define SID_FREQ1HI (*(volatile uint8_t*)0x0001A801)
#define SID_PW1LO (*(volatile uint8_t*)0x0001A802)
#define SID_PW1HI (*(volatile uint8_t*)0x0001A803)
#define SID_CR1 (*(volatile uint8_t*)0x0001A804)
#define SID_AD1 (*(volatile uint8_t*)0x0001A805)
#define SID_SR1 (*(volatile uint8_t*)0x0001A806)
#define SID_FREQ2LO (*(volatile uint8_t*)0x0001A807)
#define SID_FREQ2HI (*(volatile uint8_t*)0x0001A808)
#define SID_PW2LO (*(volatile uint8_t*)0x0001A809)
#define SID_PW2HI (*(volatile uint8_t*)0x0001A80A)
#define SID_CR2 (*(volatile uint8_t*)0x0001A80B)
#define SID_AD2 (*(volatile uint8_t*)0x0001A80C)
#define SID_SR2 (*(volatile uint8_t*)0x0001A80D)
#define SID_FREQ3LO (*(volatile uint8_t*)0x0001A80E)
#define SID_FREQ3HI (*(volatile uint8_t*)0x0001A80F)
#define SID_PW3LO (*(volatile uint8_t*)0x0001A810)
#define SID_PW3HI (*(volatile uint8_t*)0x0001A811)
#define SID_CR3 (*(volatile uint8_t*)0x0001A812)
#define SID_AD3 (*(volatile uint8_t*)0x0001A813)
#define SID_SR3 (*(volatile uint8_t*)0x0001A814)
#define SID_FCH (*(volatile uint8_t*)0x0001A815)
#define SID_FCL (*(volatile uint8_t*)0x0001A816)
#define SID_RES_FLT (*(volatile uint8_t*)0x0001A817)
#define SID_VOLUME (*(volatile uint8_t*)0x0001A818)
#define SID_POTX (*(volatile uint8_t*)0x0001A819)
#define SID_POTY (*(volatile uint8_t*)0x0001A81A)
#define SID_OSC3 (*(volatile uint8_t*)0x0001A81B)
#define SID_ENV3 (*(volatile uint8_t*)0x0001A81C)

// Complex Interface Adapter 1 - Keyboard/Serial
#define CIA1_BASE 0xDC00
#define CIA1_PRA (*(volatile uint8_t*)0x0001B800)
#define CIA1_PRB (*(volatile uint8_t*)0x0001B801)
#define CIA1_DDRA (*(volatile uint8_t*)0x0001B802)
#define CIA1_DDRB (*(volatile uint8_t*)0x0001B803)
#define CIA1_TA_LO (*(volatile uint8_t*)0x0001B804)
#define CIA1_TA_HI (*(volatile uint8_t*)0x0001B805)
#define CIA1_TB_LO (*(volatile uint8_t*)0x0001B806)
#define CIA1_TB_HI (*(volatile uint8_t*)0x0001B807)
#define CIA1_TOD_TENTH (*(volatile uint8_t*)0x0001B808)
#define CIA1_TOD_SEC (*(volatile uint8_t*)0x0001B809)
#define CIA1_TOD_MIN (*(volatile uint8_t*)0x0001B80A)
#define CIA1_TOD_HR (*(volatile uint8_t*)0x0001B80B)
#define CIA1_SDR (*(volatile uint8_t*)0x0001B80C)
#define CIA1_ICR (*(volatile uint8_t*)0x0001B80D)
#define CIA1_CRA (*(volatile uint8_t*)0x0001B80E)
#define CIA1_CRB (*(volatile uint8_t*)0x0001B80F)

// Complex Interface Adapter 2 - Serial/Bus
#define CIA2_BASE 0xDD00
#define CIA2_PRA (*(volatile uint8_t*)0x0001BA00)
#define CIA2_PRB (*(volatile uint8_t*)0x0001BA01)
#define CIA2_DDRA (*(volatile uint8_t*)0x0001BA02)
#define CIA2_DDRB (*(volatile uint8_t*)0x0001BA03)
#define CIA2_TA_LO (*(volatile uint8_t*)0x0001BA04)
#define CIA2_TA_HI (*(volatile uint8_t*)0x0001BA05)
#define CIA2_TB_LO (*(volatile uint8_t*)0x0001BA06)
#define CIA2_TB_HI (*(volatile uint8_t*)0x0001BA07)
#define CIA2_TOD_TENTH (*(volatile uint8_t*)0x0001BA08)
#define CIA2_TOD_SEC (*(volatile uint8_t*)0x0001BA09)
#define CIA2_TOD_MIN (*(volatile uint8_t*)0x0001BA0A)
#define CIA2_TOD_HR (*(volatile uint8_t*)0x0001BA0B)
#define CIA2_SDR (*(volatile uint8_t*)0x0001BA0C)
#define CIA2_ICR (*(volatile uint8_t*)0x0001BA0D)
#define CIA2_CRA (*(volatile uint8_t*)0x0001BA0E)
#define CIA2_CRB (*(volatile uint8_t*)0x0001BA0F)

// Color RAM (4-bit per char cell)
#define COLORRAM_BASE 0xD800
#define COLORRAM_COLOR (*(volatile uint8_t*)0x0001B000)

// IEC Serial Bus (via CIA1)
#define IEC_BASE 0xDC00
#define IEC_IEC_DATA (*(volatile uint8_t*)0x0001B800)
#define IEC_IEC_CLOCK (*(volatile uint8_t*)0x0001B801)

// 中断向量定义
#define RESET_VECTOR 0  // Power-on / Reset
#define NMI_VECTOR 1  // Non-Maskable Interrupt
#define IRQ_VECTOR 2  // IRQ (VIC raster / CIA timer)

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

void commodore_64_init(void);

#ifdef __cplusplus
}
#endif

#endif // COMMODORE_64_HPP
