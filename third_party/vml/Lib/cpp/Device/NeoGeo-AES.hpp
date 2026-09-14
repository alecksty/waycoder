#ifndef NEOGEO_68000_HPP
#define NEOGEO_68000_HPP

// NeoGeo-68000寄存器定义
// 生成自: SNK/M68K/NeoGeo-68000
// 版本: 1.0
// 日期: 2026-04-16


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: MC68000
// 位宽: 32位
// 时钟频率: 12000000 Hz

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

// User Stack Pointer (USP)
#define A7 (*(volatile uint32_t*)0x3C)

// Supervisor Stack Pointer (SSP)
#define SP (*(volatile uint32_t*)0x3C)

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
#define SR_M 11  // Master/Interrupt
#define SR_S 13  // Supervisor/User
#define SR_T0 14  // Trace Mode 0
#define SR_T1 15  // Trace Mode 1

// 内存段定义
// Work RAM (64KB)
#define WORK_RAM_START 0x100000
#define WORK_RAM_END 0x10FFFF
#define WORK_RAM_SIZE 65536

// Backup SRAM (battery-backed, 64KB)
#define BACKUP_RAM_START 0x200000
#define BACKUP_RAM_END 0x20FFFF
#define BACKUP_RAM_SIZE 65536

// Fix Layer ROM (512KB)
#define FIX_ROM_START 0x000000
#define FIX_ROM_END 0x07FFFF
#define FIX_ROM_SIZE 524288

// Sprite ROM (up to 1MB)
#define SPR_ROM_START 0x400000
#define SPR_ROM_END 0x4FFFFF
#define SPR_ROM_SIZE 1048576

// Audio ROM (up to 64KB)
#define AUDIO_ROM_START 0x800000
#define AUDIO_ROM_END 0x80FFFF
#define AUDIO_ROM_SIZE 65536

// Cartridge ROM (up to 512KB, expandable)
#define CART_ROM_START 0xC00000
#define CART_ROM_END 0xC7FFFF
#define CART_ROM_SIZE 524288

// I/O Area (VDP, YM2610, Z80 port, etc.)
#define IO_AREA_START 0x300000
#define IO_AREA_END 0x3FFFFF
#define IO_AREA_SIZE 1048576

// Z80 Work RAM (2KB)
#define Z80_RAM_START 0x10000
#define Z80_RAM_END 0x107FF
#define Z80_RAM_SIZE 2048

// 外设定义
// Z80 Audio Coprocessor @ 4MHz
#define Z80_BASE 0x300000
#define Z80_Z80_A (*(volatile uint8_t*)0x00300000)
#define Z80_Z80_F (*(volatile uint8_t*)0x00300001)
#define Z80_Z80_B (*(volatile uint8_t*)0x00300002)
#define Z80_Z80_C (*(volatile uint8_t*)0x00300003)
#define Z80_Z80_D (*(volatile uint8_t*)0x00300004)
#define Z80_Z80_E (*(volatile uint8_t*)0x00300005)
#define Z80_Z80_H (*(volatile uint8_t*)0x00300006)
#define Z80_Z80_L (*(volatile uint8_t*)0x00300007)
#define Z80_Z80_AF (*(volatile uint16_t*)0x00300008)
#define Z80_Z80_BC (*(volatile uint16_t*)0x0030000A)
#define Z80_Z80_DE (*(volatile uint16_t*)0x0030000C)
#define Z80_Z80_HL (*(volatile uint16_t*)0x0030000E)
#define Z80_Z80_IX (*(volatile uint16_t*)0x00300010)
#define Z80_Z80_IY (*(volatile uint16_t*)0x00300012)
#define Z80_Z80_SP (*(volatile uint16_t*)0x00300014)
#define Z80_Z80_PC (*(volatile uint16_t*)0x00300016)
#define Z80_Z80_I (*(volatile uint8_t*)0x00300018)
#define Z80_Z80_R (*(volatile uint8_t*)0x00300019)
#define Z80_Z80_IM (*(volatile uint8_t*)0x0030001A)
#define Z80_Z80_BUSREQ (*(volatile uint8_t*)0x0030001E)
#define Z80_Z80_RESET (*(volatile uint8_t*)0x0030001F)

// Yamaha YM2610 FM + ADPCM Audio Generator
#define YM2610_BASE 0x300000
#define YM2610_YM_ADDR_A0 (*(volatile uint8_t*)0x00300000)
#define YM2610_YM_DATA_A0 (*(volatile uint8_t*)0x00300001)
#define YM2610_YM_ADDR_A1 (*(volatile uint8_t*)0x00300002)
#define YM2610_YM_DATA_A1 (*(volatile uint8_t*)0x00300003)
#define YM2610_YM_ADDR_B0 (*(volatile uint8_t*)0x00300004)
#define YM2610_YM_DATA_B0 (*(volatile uint8_t*)0x00300005)
#define YM2610_YM_TEST (*(volatile uint8_t*)0x00300008)
#define YM2610_YM_FM_CH0_FREQ_L (*(volatile uint8_t*)0x003000A0)
#define YM2610_YM_FM_CH0_FREQ_H (*(volatile uint8_t*)0x003000A4)
#define YM2610_YM_FM_CH1_FREQ_L (*(volatile uint8_t*)0x003000A1)
#define YM2610_YM_FM_CH1_FREQ_H (*(volatile uint8_t*)0x003000A5)
#define YM2610_YM_FM_CH2_FREQ_L (*(volatile uint8_t*)0x003000A2)
#define YM2610_YM_FM_CH2_FREQ_H (*(volatile uint8_t*)0x003000A6)
#define YM2610_YM_FM_CH3_FREQ_L (*(volatile uint8_t*)0x003000A3)
#define YM2610_YM_FM_CH3_FREQ_H (*(volatile uint8_t*)0x003000A7)
#define YM2610_YM_FM_KEY_ON (*(volatile uint8_t*)0x00300028)
#define YM2610_YM_FM_CH0_ALG (*(volatile uint8_t*)0x003000B0)
#define YM2610_YM_FM_CH1_ALG (*(volatile uint8_t*)0x003000B1)
#define YM2610_YM_FM_CH2_ALG (*(volatile uint8_t*)0x003000B2)
#define YM2610_YM_FM_CH3_ALG (*(volatile uint8_t*)0x003000B3)
#define YM2610_YM_FM_TIMER_H (*(volatile uint8_t*)0x00300024)
#define YM2610_YM_FM_TIMER_L (*(volatile uint8_t*)0x00300025)
#define YM2610_YM_FM_TIMER_CTRL (*(volatile uint8_t*)0x00300027)
#define YM2610_YM_FM_TIMER_CTRL_TIMER_A_START 0  // Timer A Start
#define YM2610_YM_FM_TIMER_CTRL_TIMER_B_START 1  // Timer B Start
#define YM2610_YM_FM_TIMER_CTRL_LOAD_A 2  // Load Timer A
#define YM2610_YM_FM_TIMER_CTRL_LOAD_B 3  // Load Timer B
#define YM2610_YM_FM_TIMER_CTRL_IRQ_EN_A 4  // Timer A IRQ Enable
#define YM2610_YM_FM_TIMER_CTRL_IRQ_EN_B 5  // Timer B IRQ Enable
#define YM2610_YM_FM_TIMER_CTRL_CSM_MODE 7  // CSM Mode (auto Key-On after timer A)
#define YM2610_YM_FM_CH0_DETUNE (*(volatile uint8_t*)0x00300030)
#define YM2610_YM_FM_CH0_MUL (*(volatile uint8_t*)0x00300030)
#define YM2610_YM_FM_CH0_TL (*(volatile uint8_t*)0x00300040)
#define YM2610_YM_FM_CH0_KS_AR (*(volatile uint8_t*)0x00300050)
#define YM2610_YM_FM_CH0_AM_DR (*(volatile uint8_t*)0x00300060)
#define YM2610_YM_FM_CH0_SR (*(volatile uint8_t*)0x00300070)
#define YM2610_YM_FM_CH0_RR_SL (*(volatile uint8_t*)0x00300080)
#define YM2610_YM_FM_CH0_SSG (*(volatile uint8_t*)0x00300090)
#define YM2610_YM_SSG_CHA_FREQ_L (*(volatile uint8_t*)0x00300000)
#define YM2610_YM_SSG_CHA_FREQ_H (*(volatile uint8_t*)0x00300001)
#define YM2610_YM_SSG_CHB_FREQ_L (*(volatile uint8_t*)0x00300002)
#define YM2610_YM_SSG_CHB_FREQ_H (*(volatile uint8_t*)0x00300003)
#define YM2610_YM_SSG_CHC_FREQ_L (*(volatile uint8_t*)0x00300004)
#define YM2610_YM_SSG_CHC_FREQ_H (*(volatile uint8_t*)0x00300005)
#define YM2610_YM_SSG_CHA_VOL (*(volatile uint8_t*)0x00300008)
#define YM2610_YM_SSG_CHB_VOL (*(volatile uint8_t*)0x00300009)
#define YM2610_YM_SSG_CHC_VOL (*(volatile uint8_t*)0x0030000A)
#define YM2610_YM_SSG_MIXER (*(volatile uint8_t*)0x00300007)
#define YM2610_YM_SSG_ENV_FREQ_L (*(volatile uint8_t*)0x0030000B)
#define YM2610_YM_SSG_ENV_FREQ_H (*(volatile uint8_t*)0x0030000C)
#define YM2610_YM_SSG_ENV_SHAPE (*(volatile uint8_t*)0x0030000D)
#define YM2610_YM_SSG_IO_A (*(volatile uint8_t*)0x0030000E)
#define YM2610_YM_SSG_IO_B (*(volatile uint8_t*)0x0030000F)
#define YM2610_YM_ADPCM_STATUS (*(volatile uint8_t*)0x00300010)
#define YM2610_YM_ADPCM_START (*(volatile uint8_t*)0x00300011)
#define YM2610_YM_ADPCM_END (*(volatile uint8_t*)0x00300012)
#define YM2610_YM_ADPCM_VOL_L (*(volatile uint8_t*)0x00300013)
#define YM2610_YM_ADPCM_VOL_R (*(volatile uint8_t*)0x00300014)
#define YM2610_YM_DELTA_N_L (*(volatile uint8_t*)0x00300015)
#define YM2610_YM_DELTA_N_H (*(volatile uint8_t*)0x00300016)
#define YM2610_YM_ADPCM_B_START (*(volatile uint8_t*)0x00300018)
#define YM2610_YM_ADPCM_B_END (*(volatile uint8_t*)0x00300019)
#define YM2610_YM_ADPCM_B_VOL (*(volatile uint8_t*)0x0030001A)
#define YM2610_YM_ADPCM_B_CTRL (*(volatile uint8_t*)0x0030001B)

// Neo Geo VDP (Video Display Processor)
#define YGV628_BASE 0x3C0000
#define YGV628_VRAM_ADDR_L (*(volatile uint8_t*)0x003C0000)
#define YGV628_VRAM_ADDR_H (*(volatile uint8_t*)0x003C0001)
#define YGV628_VRAM_DATA (*(volatile uint8_t*)0x003C0002)
#define YGV628_VRAM_READ (*(volatile uint8_t*)0x003C0003)
#define YGV628_CRAM_ADDR (*(volatile uint8_t*)0x003C0004)
#define YGV628_CRAM_DATA (*(volatile uint8_t*)0x003C0005)
#define YGV628_VDP_STATUS (*(volatile uint8_t*)0x003C0006)
#define YGV628_VDP_STATUS_VBLANK 0  // V-Blank Flag
#define YGV628_VDP_STATUS_FIELD 1  // Field (0=even, 1=odd for interlace)
#define YGV628_VDP_STATUS_ODD_FIELD 1  // Odd Field Flag
#define YGV628_VDP_STATUS_DMA_BUSY 2  // DMA Busy
#define YGV628_VDP_STATUS_SPRITE_OVERFLOW 3  // Sprite Overflow (more than 16 per line)
#define YGV628_VDP_STATUS_SPRITE_COLLISION 4  // Sprite Collision
#define YGV628_VDP_CTRL (*(volatile uint8_t*)0x003C0007)
#define YGV628_VDP_CTRL_VRAM_INC 0  // VRAM Auto-Increment (0=+1, 1=+2)
#define YGV628_VDP_CTRL_ROW_SCROLL 1  // Row Scroll Mode
#define YGV628_VDP_CTRL_COL_SCROLL 2  // Column Scroll Mode
#define YGV628_VDP_CTRL_FIX_DISP 3  // Fix Layer Display
#define YGV628_VDP_CTRL_SPR_DISP 4  // Sprite Layer Display
#define YGV628_VDP_CTRL_SCROLL2_DISP 5  // Scroll Layer 2 Display
#define YGV628_VDP_CTRL_SCROLL1_DISP 6  // Scroll Layer 1 Display
#define YGV628_VDP_CTRL_DMA_ENABLE 7  // DMA Enable
#define YGV628_SCROLL1_BASE (*(volatile uint16_t*)0x003C0008)
#define YGV628_SCROLL2_BASE (*(volatile uint16_t*)0x003C000A)
#define YGV628_SPR_BASE (*(volatile uint16_t*)0x003C000C)
#define YGV628_SPR_COUNT (*(volatile uint8_t*)0x003C000E)
#define YGV628_WINDOW_X (*(volatile uint8_t*)0x003C0010)
#define YGV628_WINDOW_Y (*(volatile uint8_t*)0x003C0011)
#define YGV628_WINDOW_W (*(volatile uint8_t*)0x003C0012)
#define YGV628_WINDOW_H (*(volatile uint8_t*)0x003C0013)
#define YGV628_LINE_SCROLL_L (*(volatile uint8_t*)0x003C0014)
#define YGV628_LINE_SCROLL_H (*(volatile uint8_t*)0x003C0015)
#define YGV628_RASTER_COMP (*(volatile uint8_t*)0x003C0016)
#define YGV628_H_TIMING (*(volatile uint8_t*)0x003C0018)
#define YGV628_V_TIMING (*(volatile uint8_t*)0x003C0019)
#define YGV628_DMA_SRC_L (*(volatile uint8_t*)0x003C001A)
#define YGV628_DMA_SRC_H (*(volatile uint8_t*)0x003C001B)
#define YGV628_DMA_SRC_B (*(volatile uint8_t*)0x003C001C)
#define YGV628_DMA_DEST_L (*(volatile uint8_t*)0x003C001D)
#define YGV628_DMA_DEST_H (*(volatile uint8_t*)0x003C001E)
#define YGV628_DMA_COUNT (*(volatile uint16_t*)0x003C001F)

// Neo Geo System Driver / Controller
#define NEODRIVER_BASE 0x310000
#define NEODRIVER_PDI0 (*(volatile uint8_t*)0x00310000)
#define NEODRIVER_PDI0_UP 0  // Up (0=pressed)
#define NEODRIVER_PDI0_DOWN 1  // Down (0=pressed)
#define NEODRIVER_PDI0_LEFT 2  // Left (0=pressed)
#define NEODRIVER_PDI0_RIGHT 3  // Right (0=pressed)
#define NEODRIVER_PDI0_A 4  // A Button (0=pressed)
#define NEODRIVER_PDI0_B 5  // B Button (0=pressed)
#define NEODRIVER_PDI0_C 6  // C Button (0=pressed)
#define NEODRIVER_PDI0_D 7  // D Button (0=pressed)
#define NEODRIVER_PDI1 (*(volatile uint8_t*)0x00310001)
#define NEODRIVER_PDI2 (*(volatile uint8_t*)0x00310002)
#define NEODRIVER_PDI3 (*(volatile uint8_t*)0x00310003)
#define NEODRIVER_PDO0 (*(volatile uint8_t*)0x00310004)
#define NEODRIVER_PDO1 (*(volatile uint8_t*)0x00310005)
#define NEODRIVER_PDO2 (*(volatile uint8_t*)0x00310006)
#define NEODRIVER_PDO3 (*(volatile uint8_t*)0x00310007)
#define NEODRIVER_DIPSEL1 (*(volatile uint8_t*)0x00310008)
#define NEODRIVER_DIPSEL1_COIN_SELECT 0  // Coin Select (0=common, 1=1 coin 1 credit)
#define NEODRIVER_DIPSEL1_FREE_PLAY 1  // Free Play
#define NEODRIVER_DIPSEL1_DEMO_SOUND 2  // Demo Sound
#define NEODRIVER_DIPSEL1_CHIP_MODE 3  // Chip Mode (0=AES, 1=MVS)
#define NEODRIVER_DIPSEL1_CONTROLLER_TYPE 4  // Controller Type (0=standard, 1=keyboard)
#define NEODRIVER_DIPSEL2 (*(volatile uint8_t*)0x00310009)
#define NEODRIVER_DIPSEL3 (*(volatile uint8_t*)0x0031000A)
#define NEODRIVER_DIPSEL4 (*(volatile uint8_t*)0x0031000B)
#define NEODRIVER_SYSCTRL (*(volatile uint8_t*)0x0031000C)
#define NEODRIVER_SYSCTRL_RTSEL 0  // Real Time Switch Select
#define NEODRIVER_SYSCTRL_RESERVED0 1  // Reserved
#define NEODRIVER_SYSCTRL_SCC 2  // System Clock Control
#define NEODRIVER_SYSCTRL_PHEN 3  // PHEN (bus timing)
#define NEODRIVER_SYSCTRL_PCK2 4  // PCK2 (bus timing)
#define NEODRIVER_SYSCTRL_PCK1 5  // PCK1 (bus timing)
#define NEODRIVER_SYSCTRL_CKDIV2 6  // Clock Divide by 2
#define NEODRIVER_SYSCTRL_FEFIX 7  // FE Fix
#define NEODRIVER_IRQMASK (*(volatile uint8_t*)0x0031000D)
#define NEODRIVER_IRQMASK_VBLANK_MASK 0  // V-Blank Interrupt Mask
#define NEODRIVER_IRQMASK_HBLANK_MASK 1  // H-Blank Interrupt Mask
#define NEODRIVER_IRQMASK_VECTOR_IN_MASK 2  // Vector In (from Z80) Mask
#define NEODRIVER_IRQMASK_SYSTEM_IN_MASK 3  // System Input (JAMMA) Mask
#define NEODRIVER_IRQFLAG (*(volatile uint8_t*)0x0031000E)
#define NEODRIVER_SECAM_MODE (*(volatile uint8_t*)0x0031000F)

// Controller Port 1
#define CONTROLLER1_BASE 0x310000
#define CONTROLLER1_PDI0 (*(volatile uint8_t*)0x00310000)

// Controller Port 2
#define CONTROLLER2_BASE 0x310001
#define CONTROLLER2_PDI1 (*(volatile uint8_t*)0x00310001)

// Memory Card Interface
#define MEMORY_CARD_BASE 0x320000
#define MEMORY_CARD_CARD_DATA (*(volatile uint8_t*)0x00320000)
#define MEMORY_CARD_CARD_STATUS (*(volatile uint8_t*)0x00320001)
#define MEMORY_CARD_CARD_STATUS_INSERTED 0  // Card Inserted (0=yes)
#define MEMORY_CARD_CARD_STATUS_WRITE_PROTECT 1  // Write Protected (0=yes)
#define MEMORY_CARD_CARD_STATUS_READY 2  // Ready for I/O
#define MEMORY_CARD_CARD_CTRL (*(volatile uint8_t*)0x00320002)

// Cartridge Bank Switching
#define CART_BANK_BASE 0x2FFFF0
#define CART_BANK_BANK_REG (*(volatile uint8_t*)0x002FFFF0)

// 中断向量定义
#define RESET_SP_VECTOR 1  // Reset Initial Stack Pointer
#define RESET_PC_VECTOR 2  // Reset Initial PC
#define BUS_ERROR_VECTOR 3  // Bus Error
#define ADDRESS_ERROR_VECTOR 4  // Address Error
#define ILLEGAL_INSTR_VECTOR 5  // Illegal Instruction
#define ZERO_DIVIDE_VECTOR 6  // Zero Divide
#define CHK_EXCEPTION_VECTOR 7  // CHK Exception
#define TRAPV_VECTOR 8  // TRAPV Exception
#define PRIVILEGE_VECTOR 9  // Privilege Violation
#define TRACE_VECTOR 10  // Trace
#define LINE_A_VECTOR 11  // Line 1010 Emulator
#define LINE_F_VECTOR 12  // Line 1111 Emulator
#define IRQ1_VECTOR 24  // H-Blank / VDP Interrupt (raster)
#define IRQ2_VECTOR 25  // V-Blank / Frame End Interrupt
#define IRQ3_VECTOR 26  // System Controller / Z80 Vector In
#define IRQ4_VECTOR 27  // JAMMA / System Input
#define IRQ5_VECTOR 28  // Z80 Interrupt Request
#define TRAP0_VECTOR 32  // TRAP #0 (system call)
#define TRAP1_VECTOR 33  // TRAP #1

// 引脚定义
#define PIN_VCC 1  // Power Supply (5V)
#define PIN_GND 2  // Ground
#define PIN_CLK 3  // System Clock (12MHz for 68K)
#define PIN_RESET 4  // Reset (active low)
#define PIN_HALT 5  // Halt (stops CPU)
#define PIN_NMI 6  // Non-Maskable Interrupt
#define PIN_IPL0 7  // Interrupt Priority Level 0
#define PIN_IPL1 8  // Interrupt Priority Level 1
#define PIN_IPL2 9  // Interrupt Priority Level 2
#define PIN_DTACK 10  // Data Acknowledge (active low)
#define PIN_BERR 11  // Bus Error (active low)
#define PIN_BR 12  // Bus Request (active low)
#define PIN_BG 13  // Bus Grant (active low)
#define PIN_A0 14  // Address Bus Bit 0
#define PIN_A1 15  // Address Bus Bit 1
#define PIN_A2 16  // Address Bus Bit 2
#define PIN_A3 17  // Address Bus Bit 3
#define PIN_A4 18  // Address Bus Bit 4
#define PIN_A5 19  // Address Bus Bit 5
#define PIN_A6 20  // Address Bus Bit 6
#define PIN_A7 21  // Address Bus Bit 7
#define PIN_A8 22  // Address Bus Bit 8
#define PIN_A9 23  // Address Bus Bit 9
#define PIN_A10 24  // Address Bus Bit 10
#define PIN_A11 25  // Address Bus Bit 11
#define PIN_A12 26  // Address Bus Bit 12
#define PIN_A13 27  // Address Bus Bit 13
#define PIN_A14 28  // Address Bus Bit 14
#define PIN_A15 29  // Address Bus Bit 15
#define PIN_A16 30  // Address Bus Bit 16
#define PIN_A17 31  // Address Bus Bit 17
#define PIN_A18 32  // Address Bus Bit 18
#define PIN_A19 33  // Address Bus Bit 19
#define PIN_A20 34  // Address Bus Bit 20
#define PIN_A21 35  // Address Bus Bit 21
#define PIN_A22 36  // Address Bus Bit 22
#define PIN_A23 37  // Address Bus Bit 23
#define PIN_D0 38  // Data Bus Bit 0
#define PIN_D1 39  // Data Bus Bit 1
#define PIN_D2 40  // Data Bus Bit 2
#define PIN_D3 41  // Data Bus Bit 3
#define PIN_D4 42  // Data Bus Bit 4
#define PIN_D5 43  // Data Bus Bit 5
#define PIN_D6 44  // Data Bus Bit 6
#define PIN_D7 45  // Data Bus Bit 7
#define PIN_D8 46  // Data Bus Bit 8
#define PIN_D9 47  // Data Bus Bit 9
#define PIN_D10 48  // Data Bus Bit 10
#define PIN_D11 49  // Data Bus Bit 11
#define PIN_D12 50  // Data Bus Bit 12
#define PIN_D13 51  // Data Bus Bit 13
#define PIN_D14 52  // Data Bus Bit 14
#define PIN_D15 53  // Data Bus Bit 15
#define PIN_AS 54  // Address Strobe (active low)
#define PIN_UDS 55  // Upper Data Strobe (active low)
#define PIN_LDS 56  // Lower Data Strobe (active low)
#define PIN_R_W 57  // Read/Write (1=Read, 0=Write)
#define PIN_FC0 58  // Function Code 0
#define PIN_FC1 59  // Function Code 1
#define PIN_FC2 60  // Function Code 2
#define PIN_E 61  // E Clock (Enable, for Z80 sync)
#define PIN_VPA 62  // Valid Peripheral Address (for Z80 I/O)
#define PIN_VM 63  // Valid Memory (for Z80 memory access)
#define PIN_BKGR 64  // Background Audio Mix (analog output)
#define PIN_AUDIO_OUT 65  // Main Audio Output (Left)
#define PIN_AUDIO_R 66  // Audio Right Channel
#define PIN_VIDEO_R 67  // Video Output Red
#define PIN_VIDEO_G 68  // Video Output Green
#define PIN_VIDEO_B 69  // Video Output Blue
#define PIN_SYNC 70  // Video Sync

void neogeo_68000_init(void);

#ifdef __cplusplus
}
#endif

#endif // NEOGEO_68000_HPP
