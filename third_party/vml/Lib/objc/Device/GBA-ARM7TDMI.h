// ARM7TDMI 设备定义 - Objective-C 头文件
// 生成自: ARM/ARM7/ARM7TDMI
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: Game Boy Advance main processor - ARM7TDMI @ 16.78MHz with 32-bit ARM + 16-bit Thumb instruction sets
// CPU架构: ARM7TDMI
// 位宽: 32位
// 时钟频率: 16780000 Hz

#ifndef ARM7TDMI_DEVICE_H
#define ARM7TDMI_DEVICE_H

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
#define R9_ADDR 0x24  // General Purpose Register 9 / SB
#define R10_ADDR 0x28  // General Purpose Register 10 / SL
#define R11_ADDR 0x2C  // Frame Pointer / FP
#define R12_ADDR 0x30  // Intra-Procedure-call Scratch Register / IP
#define R13_ADDR 0x34  // Stack Pointer / SP
#define R14_ADDR 0x38  // Link Register / LR
#define R15_ADDR 0x3C  // Program Counter / PC
#define CPSR_ADDR 0x40  // Current Program Status Register
#define CPSR_MODE_BIT 0  // Processor Mode (10000=User, 10001=FIQ, 10010=IRQ, 10011=SVC, 10111=ABT, 11011=UND, 11111=SYS)
#define CPSR_T_BIT 5  // Thumb State Bit (1=Thumb mode)
#define CPSR_F_BIT 6  // FIQ Disable
#define CPSR_I_BIT 7  // IRQ Disable
#define CPSR_A_BIT 8  // Imprecise Data Abort Disable
#define CPSR_E_BIT 9  // Endianness (0=Little)
#define CPSR_GE_BIT 0  // Greater-than-or-Equal flags
#define CPSR_N_BIT 31  // Negative
#define CPSR_Z_BIT 30  // Zero
#define CPSR_C_BIT 29  // Carry
#define CPSR_V_BIT 28  // Overflow
#define SPSR_SVC_ADDR 0x44  // Saved PSR (Supervisor Mode)
#define SPSR_ABT_ADDR 0x48  // Saved PSR (Abort Mode)
#define SPSR_IRQ_ADDR 0x4C  // Saved PSR (IRQ Mode)
#define SPSR_FIQ_ADDR 0x50  // Saved PSR (FIQ Mode)

// 内存段定义
#define IWRAM_START 0x03000000
#define IWRAM_END 0x03007FFF
#define IWRAM_SIZE 32768  // Internal Work RAM (32KB, 2-cycle access)
#define IWRAM_FAST_START 0x03008000
#define IWRAM_FAST_END 0x03FFFFFF
#define IWRAM_FAST_SIZE 32752  // Internal Work RAM Fast (high-speed region)
#define VRAM_START 0x06000000
#define VRAM_END 0x06017FFF
#define VRAM_SIZE 98304  // Video RAM (96KB + 64KB OBJ VRAM)
#define PALETTE_START 0x05000200
#define PALETTE_END 0x050003FF
#define PALETTE_SIZE 512  // BG Palette RAM (256 colors x 2 bytes)
#define OBJ_PALETTE_START 0x05000400
#define OBJ_PALETTE_END 0x050005FF
#define OBJ_PALETTE_SIZE 512  // Object Palette RAM
#define OAM_START 0x07000000
#define OAM_END 0x070003FF
#define OAM_SIZE 1024  // Object Attribute Memory (OAM, 128 sprites)
#define ROM_START 0x08000000
#define ROM_END 0x09FFFFFF
#define ROM_SIZE 33554432  // Cartridge ROM (max 32MB)
#define CART_RAM_START 0x0E000000
#define CART_RAM_END 0x0E00FFFF
#define CART_RAM_SIZE 65536  // Cartridge SRAM / Flash
#define BIOS_START 0x00000000
#define BIOS_END 0x00003FFF
#define BIOS_SIZE 16384  // GBA BIOS (16KB)
#define IO_REGS_START 0x04000000
#define IO_REGS_END 0x04FFFFFF
#define IO_REGS_SIZE 16777216  // I/O Registers (MMIO)

// 外设定义
// LCD Controller
#define LCD_BASE 0x04000000
#define LCD_DISPCNT_ADDR 0x04000000
#define LCD_DISPCNT_BG_MODE_BIT 0  // BG Mode (0-6)
#define LCD_DISPCNT_GB_WINDOW_BIT 5  // Game Boy Window Enable
#define LCD_DISPCNT_WIN0_ENABLE_BIT 13  // Window 0 Enable
#define LCD_DISPCNT_WIN1_ENABLE_BIT 14  // Window 1 Enable
#define LCD_DISPCNT_OBJ_WIN_BIT 15  // Object Window Enable
#define LCD_DISPCNT_BG0_ENABLE_BIT 8  // BG0 Enable
#define LCD_DISPCNT_BG1_ENABLE_BIT 9  // BG1 Enable
#define LCD_DISPCNT_BG2_ENABLE_BIT 10  // BG2 Enable
#define LCD_DISPCNT_BG3_ENABLE_BIT 11  // BG3 Enable
#define LCD_DISPCNT_OBJ_ENABLE_BIT 12  // Object/Sprite Enable
#define LCD_GREEN_SWAP_ADDR 0x04000002
#define LCD_DISPSTAT_ADDR 0x04000004
#define LCD_DISPSTAT_V_COUNT_BIT 0  // Vertical Line Counter
#define LCD_DISPSTAT_VBLANK_FLAG_BIT 0  // V-Blank Flag (read-only)
#define LCD_DISPSTAT_HBLANK_FLAG_BIT 1  // H-Blank Flag (read-only)
#define LCD_DISPSTAT_V_COUNT_FLAG_BIT 2  // V-Count Flag (LY==LYC)
#define LCD_DISPSTAT_VBLANK_IRQ_BIT 3  // V-Blank IRQ Enable
#define LCD_DISPSTAT_HBLANK_IRQ_BIT 4  // H-Blank IRQ Enable
#define LCD_DISPSTAT_VCOUNT_IRQ_BIT 5  // V-Count IRQ Enable
#define LCD_VCOUNT_ADDR 0x04000006
#define LCD_BG0CNT_ADDR 0x04000008
#define LCD_BG1CNT_ADDR 0x0400000A
#define LCD_BG2CNT_ADDR 0x0400000C
#define LCD_BG3CNT_ADDR 0x0400000E
#define LCD_BG0HOFS_ADDR 0x04000010
#define LCD_BG0VOFS_ADDR 0x04000012
#define LCD_BG1HOFS_ADDR 0x04000014
#define LCD_BG1VOFS_ADDR 0x04000016
#define LCD_BG2HOFS_ADDR 0x04000018
#define LCD_BG2VOFS_ADDR 0x0400001A
#define LCD_BG3HOFS_ADDR 0x0400001C
#define LCD_BG3VOFS_ADDR 0x0400001E
#define LCD_BG2PA_ADDR 0x04000020
#define LCD_BG2PB_ADDR 0x04000022
#define LCD_BG2PC_ADDR 0x04000024
#define LCD_BG2PD_ADDR 0x04000026
#define LCD_BG2X_ADDR 0x04000028
#define LCD_BG2Y_ADDR 0x0400002C
#define LCD_BG3PA_ADDR 0x04000030
#define LCD_BG3PB_ADDR 0x04000032
#define LCD_BG3PC_ADDR 0x04000034
#define LCD_BG3PD_ADDR 0x04000036
#define LCD_BG3X_ADDR 0x04000038
#define LCD_BG3Y_ADDR 0x0400003C
#define LCD_WIN0H_ADDR 0x04000040
#define LCD_WIN1H_ADDR 0x04000042
#define LCD_WIN0V_ADDR 0x04000044
#define LCD_WIN1V_ADDR 0x04000046
#define LCD_WININ_ADDR 0x04000048
#define LCD_WINOUT_ADDR 0x04000049
#define LCD_MOSAIC_ADDR 0x0400004C
#define LCD_BLDCNT_ADDR 0x04000050
#define LCD_BLDCNT_BG1ST_BIT 0  // BG1 1st Target
#define LCD_BLDCNT_BG2ST_BIT 1  // BG2 1st Target
#define LCD_BLDCNT_BG3ST_BIT 2  // BG3 1st Target
#define LCD_BLDCNT_OBJST_BIT 3  // Object 1st Target
#define LCD_BLDCNT_BDST_BIT 4  // Backdrop 1st Target
#define LCD_BLDCNT_BLEND_MODE_BIT 0  // Blend Mode (0=None, 1=Alpha, 2=Increase, 3=Decrease)
#define LCD_BLDCNT_BG1ST2_BIT 8  // BG1 2nd Target
#define LCD_BLDCNT_BG2ST2_BIT 9  // BG2 2nd Target
#define LCD_BLDCNT_BG3ST2_BIT 10  // BG3 2nd Target
#define LCD_BLDCNT_OBJST2_BIT 11  // Object 2nd Target
#define LCD_BLDCNT_BDST2_BIT 12  // Backdrop 2nd Target
#define LCD_BLDALPHA_ADDR 0x04000052
#define LCD_BLDY_ADDR 0x04000054
// Direct Memory Access Controller
#define DMA_BASE 0x040000B0
#define DMA_DMA0SAD_ADDR 0x040000B0
#define DMA_DMA0DAD_ADDR 0x040000B4
#define DMA_DMA0CNT_L_ADDR 0x040000B8
#define DMA_DMA0CNT_H_ADDR 0x040000BA
#define DMA_DMA0CNT_H_TRANSFER_COUNT_BIT 0  // Number of Transfers
#define DMA_DMA0CNT_H_DEST_ADD_MODE_BIT 0  // Dest Address Control (0=fix, 1=inc, 2=dec, 3=inc+reload)
#define DMA_DMA0CNT_H_SRC_ADD_MODE_BIT 0  // Source Address Control (0=fix, 1=inc, 2=dec)
#define DMA_DMA0CNT_H_REPEAT_BIT 18  // Repeat (for 16-bit repeat mode)
#define DMA_DMA0CNT_H_WORD_SIZE_BIT 20  // Word Size (0=16-bit, 1=32-bit)
#define DMA_DMA0CNT_H_DRQ_BIT 27  // DRQ Trigger (DMA from external source)
#define DMA_DMA0CNT_H_TIMING_BIT 0  // Start Timing (0=Now, 1=V-Blank, 2=H-Blank, 3=Special)
#define DMA_DMA0CNT_H_ENABLE_BIT 31  // DMA Enable
#define DMA_DMA1SAD_ADDR 0x040000BC
#define DMA_DMA1DAD_ADDR 0x040000C0
#define DMA_DMA1CNT_L_ADDR 0x040000C4
#define DMA_DMA1CNT_H_ADDR 0x040000C6
#define DMA_DMA2SAD_ADDR 0x040000C8
#define DMA_DMA2DAD_ADDR 0x040000CC
#define DMA_DMA2CNT_L_ADDR 0x040000D0
#define DMA_DMA2CNT_H_ADDR 0x040000D2
#define DMA_DMA3SAD_ADDR 0x040000D4
#define DMA_DMA3DAD_ADDR 0x040000D8
#define DMA_DMA3CNT_L_ADDR 0x040000DC
#define DMA_DMA3CNT_H_ADDR 0x040000DE
// Timer Units (4 timers)
#define TIMER_BASE 0x04000100
#define TIMER_TM0CNT_L_ADDR 0x04000100
#define TIMER_TM0CNT_H_ADDR 0x04000102
#define TIMER_TM0CNT_H_PRESCALER_BIT 0  // Prescaler (0=1, 1=64, 2=256, 3=1024)
#define TIMER_TM0CNT_H_COUNT_UP_BIT 2  // Count Up (cascade mode)
#define TIMER_TM0CNT_H_IRQ_ENABLE_BIT 6  // Timer IRQ Enable
#define TIMER_TM0CNT_H_ENABLE_BIT 7  // Timer Enable
#define TIMER_TM1CNT_L_ADDR 0x04000104
#define TIMER_TM1CNT_H_ADDR 0x04000106
#define TIMER_TM2CNT_L_ADDR 0x04000108
#define TIMER_TM2CNT_H_ADDR 0x0400010A
#define TIMER_TM3CNT_L_ADDR 0x0400010C
#define TIMER_TM3CNT_H_ADDR 0x0400010E
// Serial I/O (JOY BUS / Link Cable)
#define SIO_BASE 0x04000120
#define SIO_SIOCNT_ADDR 0x04000120
#define SIO_SIOCNT_CLOCK_SEL_BIT 0  // Baud Rate Clock (0=9600, 1=57600, 2=115200, 3=768000)
#define SIO_SIOCNT_SO_ENABLE_BIT 3  // SO Output Enable
#define SIO_SIOCNT_RECV_ENABLE_BIT 5  // Receive Enable
#define SIO_SIOCNT_SEND_ENABLE_BIT 6  // Send Enable
#define SIO_SIOCNT_START_BIT_BIT 7  // Start Transfer
#define SIO_SIODATA8_ADDR 0x0400012A
#define SIO_JOYCNT_ADDR 0x04000130
#define SIO_JOYSTAT_ADDR 0x04000134
#define SIO_JOY_RECV_ADDR 0x04000150
#define SIO_JOY_TRANS_ADDR 0x04000154
// Key Input
#define KEYINPUT_BASE 0x04000130
#define KEYINPUT_KEYINPUT_ADDR 0x04000130
#define KEYINPUT_KEYINPUT_A_BIT 0  // A Button (0=Pressed)
#define KEYINPUT_KEYINPUT_B_BIT 1  // B Button (0=Pressed)
#define KEYINPUT_KEYINPUT_SELECT_BIT 2  // Select Button (0=Pressed)
#define KEYINPUT_KEYINPUT_START_BIT 3  // Start Button (0=Pressed)
#define KEYINPUT_KEYINPUT_RIGHT_BIT 4  // D-Pad Right (0=Pressed)
#define KEYINPUT_KEYINPUT_LEFT_BIT 5  // D-Pad Left (0=Pressed)
#define KEYINPUT_KEYINPUT_UP_BIT 6  // D-Pad Up (0=Pressed)
#define KEYINPUT_KEYINPUT_DOWN_BIT 7  // D-Pad Down (0=Pressed)
#define KEYINPUT_KEYINPUT_R_BIT 8  // R Shoulder Button (0=Pressed)
#define KEYINPUT_KEYINPUT_L_BIT 9  // L Shoulder Button (0=Pressed)
#define KEYINPUT_KEYCNT_ADDR 0x04000132
#define KEYINPUT_KEYCNT_KEY_MASK_BIT 0  // Key Interrupt Enable Mask
#define KEYINPUT_KEYCNT_IRQ_ENABLE_BIT 14  // Key Interrupt Enable
// Interrupt Control
#define INTERRUPT_BASE 0x04000200
#define INTERRUPT_IME_ADDR 0x04000208
#define INTERRUPT_IE_ADDR 0x04000210
#define INTERRUPT_IE_VBLANK_BIT 0  // V-Blank Interrupt Enable
#define INTERRUPT_IE_HBLANK_BIT 1  // H-Blank Interrupt Enable
#define INTERRUPT_IE_VCOUNT_BIT 2  // V-Count Match Interrupt Enable
#define INTERRUPT_IE_TIMER0_BIT 3  // Timer 0 Interrupt Enable
#define INTERRUPT_IE_TIMER1_BIT 4  // Timer 1 Interrupt Enable
#define INTERRUPT_IE_TIMER2_BIT 5  // Timer 2 Interrupt Enable
#define INTERRUPT_IE_TIMER3_BIT 6  // Timer 3 Interrupt Enable
#define INTERRUPT_IE_SIO_BIT 7  // Serial I/O Interrupt Enable
#define INTERRUPT_IE_DMA0_BIT 8  // DMA 0 Interrupt Enable
#define INTERRUPT_IE_DMA1_BIT 9  // DMA 1 Interrupt Enable
#define INTERRUPT_IE_DMA2_BIT 10  // DMA 2 Interrupt Enable
#define INTERRUPT_IE_DMA3_BIT 11  // DMA 3 Interrupt Enable
#define INTERRUPT_IE_KEYPAD_BIT 12  // Keypad Interrupt Enable
#define INTERRUPT_IE_CART_BIT 13  // Game Pak Interrupt Enable
#define INTERRUPT_IF_ADDR 0x04000214
// Waitstate Control
#define WAITCNT_BASE 0x04000204
#define WAITCNT_WAITCNT_ADDR 0x04000204
#define WAITCNT_WAITCNT_PHI_OD_BIT 0  // PHI Terminal Output (0=Disable)
#define WAITCNT_WAITCNT_SRAM_WS_BIT 0  // SRAM Wait State (0=4, 1=3, 2=2, 3=8 cycles)
#define WAITCNT_WAITCNT_WS0_N_BIT 0  // Wait State 0 (ROM/SRAM 1st access)
#define WAITCNT_WAITCNT_WS0_S_BIT 5  // Wait State 0 (ROM/SRAM 2nd access)
#define WAITCNT_WAITCNT_WS1_N_BIT 0  // Wait State 1 (ROM 2nd access)
#define WAITCNT_WAITCNT_WS1_S_BIT 8  // Wait State 1 (ROM 2nd access short)
#define WAITCNT_WAITCNT_WS2_N_BIT 0  // Wait State 2 (ROM 3rd access)
#define WAITCNT_WAITCNT_WS2_S_BIT 11  // Wait State 2 (ROM 3rd access short)
#define WAITCNT_WAITCNT_PREFE_BIT 12  // Prefetch Enable (GBA SP only)

// 中断向量定义
#define INT_VBLANK 0  // V-Blank Interrupt
#define INT_HBLANK 1  // H-Blank Interrupt
#define INT_VCOUNT 2  // V-Count Match Interrupt
#define INT_TIMER0 3  // Timer 0 Overflow Interrupt
#define INT_TIMER1 4  // Timer 1 Overflow Interrupt
#define INT_TIMER2 5  // Timer 2 Overflow Interrupt
#define INT_TIMER3 6  // Timer 3 Overflow Interrupt
#define INT_SIO 7  // Serial I/O Interrupt
#define INT_DMA0 8  // DMA 0 Complete Interrupt
#define INT_DMA1 9  // DMA 1 Complete Interrupt
#define INT_DMA2 10  // DMA 2 Complete Interrupt
#define INT_DMA3 11  // DMA 3 Complete Interrupt
#define INT_KEYPAD 12  // Keypad Interrupt
#define INT_CART 13  // Game Pak Interrupt

// 引脚定义
#define PIN_VSS 1  // Ground
#define PIN_VDD 2  // Power Supply
#define PIN_CLK 3  // System Clock Input (16.78MHz)
#define PIN_RESET 4  // Reset Signal
#define PIN_NMI 5  // Non-Maskable Interrupt
#define PIN_IRQ 6  // Interrupt Request
#define PIN_AB0 7  // Address Bus Bit 0
#define PIN_AB1 8  // Address Bus Bit 1
#define PIN_AB2 9  // Address Bus Bit 2
#define PIN_AB3 10  // Address Bus Bit 3
#define PIN_AB4 11  // Address Bus Bit 4
#define PIN_AB5 12  // Address Bus Bit 5
#define PIN_AB6 13  // Address Bus Bit 6
#define PIN_AB7 14  // Address Bus Bit 7
#define PIN_AB8 15  // Address Bus Bit 8
#define PIN_AB9 16  // Address Bus Bit 9
#define PIN_AB10 17  // Address Bus Bit 10
#define PIN_AB11 18  // Address Bus Bit 11
#define PIN_AB12 19  // Address Bus Bit 12
#define PIN_AB13 20  // Address Bus Bit 13
#define PIN_AB14 21  // Address Bus Bit 14
#define PIN_AB15 22  // Address Bus Bit 15
#define PIN_AB16 23  // Address Bus Bit 16
#define PIN_AB17 24  // Address Bus Bit 17
#define PIN_AB18 25  // Address Bus Bit 18
#define PIN_AB19 26  // Address Bus Bit 19
#define PIN_AB20 27  // Address Bus Bit 20
#define PIN_AB21 28  // Address Bus Bit 21
#define PIN_AB22 29  // Address Bus Bit 22
#define PIN_AB23 30  // Address Bus Bit 23
#define PIN_AB24 31  // Address Bus Bit 24
#define PIN_AB25 32  // Address Bus Bit 25
#define PIN_AB26 33  // Address Bus Bit 26
#define PIN_AB27 34  // Address Bus Bit 27
#define PIN_AB28 35  // Address Bus Bit 28
#define PIN_AB29 36  // Address Bus Bit 29
#define PIN_AB30 37  // Address Bus Bit 30
#define PIN_AB31 38  // Address Bus Bit 31
#define PIN_DB0 39  // Data Bus Bit 0
#define PIN_DB1 40  // Data Bus Bit 1
#define PIN_DB2 41  // Data Bus Bit 2
#define PIN_DB3 42  // Data Bus Bit 3
#define PIN_DB4 43  // Data Bus Bit 4
#define PIN_DB5 44  // Data Bus Bit 5
#define PIN_DB6 45  // Data Bus Bit 6
#define PIN_DB7 46  // Data Bus Bit 7
#define PIN_DB8 47  // Data Bus Bit 8
#define PIN_DB9 48  // Data Bus Bit 9
#define PIN_DB10 49  // Data Bus Bit 10
#define PIN_DB11 50  // Data Bus Bit 11
#define PIN_DB12 51  // Data Bus Bit 12
#define PIN_DB13 52  // Data Bus Bit 13
#define PIN_DB14 53  // Data Bus Bit 14
#define PIN_DB15 54  // Data Bus Bit 15
#define PIN_DB16 55  // Data Bus Bit 16
#define PIN_DB17 56  // Data Bus Bit 17
#define PIN_DB18 57  // Data Bus Bit 18
#define PIN_DB19 58  // Data Bus Bit 19
#define PIN_DB20 59  // Data Bus Bit 20
#define PIN_DB21 60  // Data Bus Bit 21
#define PIN_DB22 61  // Data Bus Bit 22
#define PIN_DB23 62  // Data Bus Bit 23
#define PIN_DB24 63  // Data Bus Bit 24
#define PIN_DB25 64  // Data Bus Bit 25
#define PIN_DB26 65  // Data Bus Bit 26
#define PIN_DB27 66  // Data Bus Bit 27
#define PIN_DB28 67  // Data Bus Bit 28
#define PIN_DB29 68  // Data Bus Bit 29
#define PIN_DB30 69  // Data Bus Bit 30
#define PIN_DB31 70  // Data Bus Bit 31
#define PIN_NCS0 71  // Chip Select 0 (ROM)
#define PIN_NCS1 72  // Chip Select 1 (RAM)
#define PIN_NWR 73  // Write Enable (active low)
#define PIN_NRD 74  // Read Enable (active low)
#define PIN_ADV 75  // Address Valid (for external DMA)
#define PIN_BE0 76  // Byte Enable 0
#define PIN_BE1 77  // Byte Enable 1
#define PIN_BREQ 78  // Bus Request (from external master)
#define PIN_BACK 79  // Bus Acknowledge
#define PIN_EKO 80  // Serial Data Out (Link Cable)
#define PIN_EKI 81  // Serial Data In (Link Cable)
#define PIN_SOUND 82  // Stereo Audio Output (L+R)

#endif /* ARM7TDMI_DEVICE_H */
