// MIPS-R3000A 设备定义 - Objective-C 头文件
// 生成自: Sony / MIPS Technologies/MIPS-I/MIPS-R3000A
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: Sony PlayStation (PS1) main processor - MIPS R3000A @ 33.87MHz with R4000-like ISA
// CPU架构: MIPS-R3000A
// 位宽: 32位
// 时钟频率: 33870000 Hz

#ifndef MIPS-R3000A_DEVICE_H
#define MIPS-R3000A_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define R0_ADDR 0x00  // Hard-wired Zero
#define R1_ADDR 0x04  // Assembler Temporary
#define R2_ADDR 0x08  // Value Returned by Subroutines
#define R3_ADDR 0x0C  // Expression Evaluation
#define R4_ADDR 0x10  // Expression Evaluation
#define R5_ADDR 0x14  // Expression Evaluation
#define R6_ADDR 0x18  // Expression Evaluation
#define R7_ADDR 0x1C  // Expression Evaluation
#define R8_ADDR 0x20  // Expression Evaluation
#define R9_ADDR 0x24  // Expression Evaluation
#define R10_ADDR 0x28  // Expression Evaluation
#define R11_ADDR 0x2C  // Expression Evaluation
#define R12_ADDR 0x30  // Expression Evaluation
#define R13_ADDR 0x34  // Expression Evaluation
#define R14_ADDR 0x38  // Expression Evaluation
#define R15_ADDR 0x3C  // Expression Evaluation
#define R16_ADDR 0x40  // Saved Value
#define R17_ADDR 0x44  // Saved Value
#define R18_ADDR 0x48  // Saved Value
#define R19_ADDR 0x4C  // Saved Value
#define R20_ADDR 0x50  // Saved Value
#define R21_ADDR 0x54  // Saved Value
#define R22_ADDR 0x58  // Saved Value
#define R23_ADDR 0x5C  // Saved Value
#define R24_ADDR 0x60  // Temporary
#define R25_ADDR 0x64  // Temporary
#define R26_ADDR 0x68  // Kernel Reserved
#define R27_ADDR 0x6C  // Kernel Reserved
#define R28_ADDR 0x70  // Global Pointer
#define R29_ADDR 0x74  // Stack Pointer
#define R30_ADDR 0x78  // Frame Pointer
#define R31_ADDR 0x7C  // Return Address
#define HI_ADDR 0x80  // Multiply/Divide High
#define LO_ADDR 0x84  // Multiply/Divide Low
#define PC_ADDR 0x88  // Program Counter
#define CP0_SR_ADDR 0x90  // Coprocessor 0 - Status Register
#define CP0_SR_IE_BIT 0  // Interrupt Enable
#define CP0_SR_EXL_BIT 1  // Exception Level
#define CP0_SR_ERL_BIT 2  // Error Level
#define CP0_SR_KSU_BIT 0  // Kernel/User Mode
#define CP0_SR_IM0_7_BIT 0  // Interrupt Mask bits
#define CP0_SR_CU0_BIT 28  // Coprocessor 0 Usable
#define CP0_SR_BEV_BIT 22  // Bootstrap Exception Vector
#define CP0_CAUSE_ADDR 0x94  // Coprocessor 0 - Cause Register
#define CP0_CAUSE_EXCCODE_BIT 0  // Exception Code
#define CP0_CAUSE_IP0_7_BIT 0  // Interrupt Pending bits
#define CP0_EPC_ADDR 0x98  // Coprocessor 0 - Exception PC
#define CP0_BADVADDR_ADDR 0x9C  // Coprocessor 0 - Bad Virtual Address
#define CP0_CONTEXT_ADDR 0xA0  // Coprocessor 0 - Context Register
#define CP0_PID_ADDR 0xA4  // Coprocessor 0 - Process ID

// 内存段定义
#define KSEG0_START 0x80000000
#define KSEG0_END 0x801FFFFF
#define KSEG0_SIZE 2097152  // KSEG0 - Cached RAM (2MB System RAM)
#define KSEG1_START 0xA0000000
#define KSEG1_END 0xA01FFFFF
#define KSEG1_SIZE 2097152  // KSEG1 - Uncached RAM (2MB System RAM)
#define VRAM_START 0xA0000000
#define VRAM_END 0xA01FFFFF
#define VRAM_SIZE 1048576  // VRAM (1MB, mirrored in KSEG1 at 0xB0000000)
#define EXPANSION_START 0xA0000000
#define EXPANSION_END 0xA00FFFFF
#define EXPANSION_SIZE 1048576  // Expansion Region (maps to expansion RAM area)
#define SCRATCHPAD_START 0x1F800000
#define SCRATCHPAD_END 0x1F8003FF
#define SCRATCHPAD_SIZE 1024  // Data Scratchpad (1KB)
#define EXP_ROM_START 0x1FC00000
#define EXP_ROM_END 0x1FC7FFFF
#define EXP_ROM_SIZE 524288  // Kernel BIOS ROM (512KB)
#define USER_ROM_START 0x1F800000
#define USER_ROM_END 0x1FBFFFFF
#define USER_ROM_SIZE 4194304  // User ROM / Kernel Expansion
#define MMIO_START 0x1F801000
#define MMIO_END 0x1F802FFF
#define MMIO_SIZE 8192  // I/O Register Area (Expansion 1)
#define GPU_START 0x1F801810
#define GPU_END 0x1F801817
#define GPU_SIZE 8  // GPU Registers
#define CDROM_START 0x1F801800
#define CDROM_END 0x1F80180F
#define CDROM_SIZE 16  // CD-ROM Registers
#define SPU_START 0x1F801C00
#define SPU_END 0x1F801DFF
#define SPU_SIZE 512  // SPU Registers
#define IRQ_START 0x1F801070
#define IRQ_END 0x1F801077
#define IRQ_SIZE 8  // Interrupt Control
#define DMA_START 0x1F801080
#define DMA_END 0x1F8010FF
#define DMA_SIZE 128  // DMA Registers (7 channels)
#define TIMER_START 0x1F801100
#define TIMER_END 0x1F80112F
#define TIMER_SIZE 48  // Timer Registers
#define JOY_START 0x1F801040
#define JOY_END 0x1F80104F
#define JOY_SIZE 16  // JOY Interface Registers
#define MDEC_START 0x1F801820
#define MDEC_END 0x1F801827
#define MDEC_SIZE 8  // MDEC (Motion Decoder) Registers
#define SIO_START 0x1F801050
#define SIO_END 0x1F80105F
#define SIO_SIZE 16  // SIO Registers
#define GPU_STAT_START 0x1F801814
#define GPU_STAT_END 0x1F801817
#define GPU_STAT_SIZE 4  // GPU Status
#define CDROM_STAT_START 0x1F801801
#define CDROM_STAT_END 0x1F801803
#define CDROM_STAT_SIZE 3  // CD-ROM Status
#define SPU_RAM_START 0x1F800000
#define SPU_RAM_END 0x1F800FFF
#define SPU_RAM_SIZE 1024  // SPU Work RAM (1KB)

// 外设定义
// Graphics Processing Unit
#define GPU_BASE 0x1F801810
#define GPU_GP0_CMD_ADDR 0x00
#define GPU_GP0_DATA_ADDR 0x04
#define GPU_GP1_CMD_ADDR 0x08
#define GPU_GP1_DATA_ADDR 0x0C
#define GPU_TPAGE_ADDR 0x00
#define GPU_DRAW_MODE_ADDR 0x01
#define GPU_TEXTURE_WIN_ADDR 0x02
#define GPU_DRAW_OFFSET_X_ADDR 0x03
#define GPU_DRAW_OFFSET_Y_ADDR 0x04
#define GPU_DRAW_AREA_X_ADDR 0x05
#define GPU_DRAW_AREA_Y_ADDR 0x06
#define GPU_DITHER_ADDR 0x07
#define GPU_DISPLAY_MODE_ADDR 0x08
#define GPU_DISPLAY_START_X_ADDR 0x09
#define GPU_DISPLAY_START_Y_ADDR 0x0A
#define GPU_DISPLAY_HORZ_ADDR 0x0B
#define GPU_DISPLAY_VERT_ADDR 0x0C
#define GPU_DMA_MODE_ADDR 0x0D
#define GPU_GPU_STAT_ADDR 0x0C
#define GPU_GPU_STAT_READY_CMD_BIT 0  // GPU Ready to Receive Command
#define GPU_GPU_STAT_READY_DMA_BIT 1  // GPU Ready for DMA
#define GPU_GPU_STAT_DRAWING_BIT 2  // Drawing Busy
#define GPU_GPU_STAT_DMA_REQ_BIT 3  // DMA Request
#define GPU_GPU_STAT_COMMAND_BUSY_BIT 4  // Command Busy
#define GPU_GPU_STAT_DISPLAY_DISABLE_BIT 5  // Display Disable
#define GPU_GPU_STAT_INTERRUPT_BIT 24  // V-Blank Interrupt Flag
// Geometry Transformation Engine
#define GTE_BASE 0x1F801880
#define GTE_GTE_VXY0_ADDR 0x00
#define GTE_GTE_VZ0_ADDR 0x04
#define GTE_GTE_VXY1_ADDR 0x08
#define GTE_GTE_VZ1_ADDR 0x0C
#define GTE_GTE_VXY2_ADDR 0x10
#define GTE_GTE_VZ2_ADDR 0x14
#define GTE_GTE_RGB0_ADDR 0x18
#define GTE_GTE_RGB1_ADDR 0x1C
#define GTE_GTE_RGB2_ADDR 0x20
#define GTE_GTE_RTP_ADDR 0x30
#define GTE_GTE_TRX_ADDR 0x34
#define GTE_GTE_TRY_ADDR 0x38
#define GTE_GTE_TRZ_ADDR 0x3C
#define GTE_GTE_MAC0_ADDR 0x40
#define GTE_GTE_MAC1_ADDR 0x44
#define GTE_GTE_MAC2_ADDR 0x48
#define GTE_GTE_MAC3_ADDR 0x4C
#define GTE_GTE_IR0_ADDR 0x50
#define GTE_GTE_IR1_ADDR 0x54
#define GTE_GTE_IR2_ADDR 0x58
#define GTE_GTE_IR3_ADDR 0x5C
#define GTE_GTE_LZCS_ADDR 0x60
#define GTE_GTE_LZCR_ADDR 0x64
#define GTE_GTE_CTX_ADDR 0x68
#define GTE_GTE_CTY_ADDR 0x6C
#define GTE_GTE_CTZ_ADDR 0x70
#define GTE_GTE_RTX_ADDR 0x74
#define GTE_GTE_RTY_ADDR 0x78
#define GTE_GTE_RTZ_ADDR 0x7C
#define GTE_GTE_SR_ADDR 0x80
#define GTE_GTE_CMD_ADDR 0x84
#define GTE_GTE_H_ADDR 0x88
#define GTE_GTE_DQB_ADDR 0x8C
#define GTE_GTE_DQA_ADDR 0x90
#define GTE_GTE_ZSF3_ADDR 0x94
#define GTE_GTE_ZSF4_ADDR 0x98
#define GTE_GTE_OTZ_ADDR 0x9C
// Sound Processing Unit (24-channel ADPCM)
#define SPU_BASE 0x1F801C00
#define SPU_SPU_CTRL_ADDR 0x00
#define SPU_SPU_CTRL_REVERB_MASTER_BIT 0  // Reverb Master Enable
#define SPU_SPU_CTRL_IRQ9_BIT 9  // Interrupt Request Enable
#define SPU_SPU_STAT_ADDR 0x04
#define SPU_SPU_CDVOL_L_ADDR 0x08
#define SPU_SPU_CDVOL_R_ADDR 0x0A
#define SPU_SPU_MAINVOL_L_ADDR 0x0C
#define SPU_SPU_MAINVOL_R_ADDR 0x0E
#define SPU_SPU_REVERB_L_ADDR 0x10
#define SPU_SPU_REVERB_R_ADDR 0x12
#define SPU_SPU_KEYON_ADDR 0x80
#define SPU_SPU_KEYOFF_ADDR 0x82
#define SPU_SPU_CHANNEL_MUTE_ADDR 0x84
#define SPU_SPU_NOISE_CLK_ADDR 0x88
#define SPU_SPU_REVERB_ADDR_ADDR 0x8A
#define SPU_SPU_IRQ_ADDR_ADDR 0x8C
#define SPU_SPU_REVERB_VOL_L_ADDR 0x8E
#define SPU_SPU_REVERB_VOL_R_ADDR 0x90
#define SPU_SPU_VOICE_VOL_L_ADDR 0x00
#define SPU_SPU_VOICE_VOL_R_ADDR 0x01
#define SPU_SPU_VOICE_FREQ_ADDR 0x02
#define SPU_SPU_VOICE_START_ADDR 0x04
#define SPU_SPU_VOICE_ADSR1_ADDR 0x06
#define SPU_SPU_VOICE_ADSR2_ADDR 0x08
#define SPU_SPU_VOICE_ENV_ADDR 0x0A
#define SPU_SPU_VOICE_REPEAT_ADDR 0x0C
#define SPU_VOICE_BASE_SIZE_ADDR 0x10
// Motion Decoder (JPEG Decompression)
#define MDEC_BASE 0x1F801820
#define MDEC_MDEC_CTRL_ADDR 0x00
#define MDEC_MDEC_CTRL_DATA_IN_SIZE_BIT 0  // Data-in size in words
#define MDEC_MDEC_CTRL_RESET_BIT 16  // Reset MDEC
#define MDEC_MDEC_CTRL_BUSY_BIT 17  // MDEC Busy
#define MDEC_MDEC_DATA_ADDR 0x04
#define MDEC_MDEC_BKGD_ADDR 0x08
// DMA Controller (7 channels)
#define DMA_BASE 0x1F801080
#define DMA_DMA_DPCR_ADDR 0x00
#define DMA_DMA_DPCR_CH0_EN_BIT 0  // Channel 0 Enable
#define DMA_DMA_DPCR_CH1_EN_BIT 4  // Channel 1 Enable
#define DMA_DMA_DPCR_CH2_EN_BIT 8  // Channel 2 Enable
#define DMA_DMA_DPCR_CH3_EN_BIT 12  // Channel 3 Enable
#define DMA_DMA_DPCR_CH4_EN_BIT 16  // Channel 4 Enable
#define DMA_DMA_DPCR_CH5_EN_BIT 20  // Channel 5 Enable
#define DMA_DMA_DPCR_CH6_EN_BIT 24  // Channel 6 Enable
#define DMA_DMA_INT_ADDR 0x04
#define DMA_DMA_CH0_BASE_ADDR 0x10
#define DMA_DMA_CH0_COUNT_ADDR 0x14
#define DMA_DMA_CH0_CTRL_ADDR 0x18
#define DMA_DMA_CH0_CTRL_DEST_DIR_BIT 0  // Destination Direction
#define DMA_DMA_CH0_CTRL_SRC_DIR_BIT 0  // Source Direction
#define DMA_DMA_CH0_CTRL_STEPS_BIT 0  // Step
#define DMA_DMA_CH0_CTRL_CHAIN_BIT 0  // Chain Mode (0=manual, 1=request, 2=chain, 3=illegal)
#define DMA_DMA_CH0_CTRL_SYNC_BIT 0  // Sync Mode (0=immediate, 1=request, 2=linked-list)
#define DMA_DMA_CH0_CTRL_TRIGGER_BIT 10  // Trigger
#define DMA_DMA_CH1_BASE_ADDR 0x20
#define DMA_DMA_CH1_COUNT_ADDR 0x24
#define DMA_DMA_CH1_CTRL_ADDR 0x28
#define DMA_DMA_CH2_BASE_ADDR 0x30
#define DMA_DMA_CH2_COUNT_ADDR 0x34
#define DMA_DMA_CH2_CTRL_ADDR 0x38
#define DMA_DMA_CH3_BASE_ADDR 0x40
#define DMA_DMA_CH3_COUNT_ADDR 0x44
#define DMA_DMA_CH3_CTRL_ADDR 0x48
#define DMA_DMA_CH4_BASE_ADDR 0x50
#define DMA_DMA_CH4_COUNT_ADDR 0x54
#define DMA_DMA_CH4_CTRL_ADDR 0x58
#define DMA_DMA_CH5_BASE_ADDR 0x60
#define DMA_DMA_CH5_COUNT_ADDR 0x64
#define DMA_DMA_CH5_CTRL_ADDR 0x68
#define DMA_DMA_CH6_BASE_ADDR 0x70
#define DMA_DMA_CH6_COUNT_ADDR 0x74
#define DMA_DMA_CH6_CTRL_ADDR 0x78
// Timers (3 timers)
#define TIMER_BASE 0x1F801100
#define TIMER_TM0_COUNT_ADDR 0x00
#define TIMER_TM0_MODE_ADDR 0x04
#define TIMER_TM0_MODE_RELOAD_BIT 0  // Reload Enable
#define TIMER_TM0_MODE_CLOCK_BIT 0  // Clock Source (0=sysclk/1, 1=sysclk/8, 2=sysclk/64, 3=sysclk/256)
#define TIMER_TM0_MODE_IRQ_EN_BIT 3  // IRQ Enable
#define TIMER_TM0_MODE_IRQ_REPEAT_BIT 4  // IRQ Repeat
#define TIMER_TM0_MODE_IRQ_TOGGLE_BIT 5  // IRQ Toggle Mode
#define TIMER_TM0_MODE_REACH_MAX_BIT 6  // Reached Max Value
#define TIMER_TM0_TARGET_ADDR 0x08
#define TIMER_TM1_COUNT_ADDR 0x10
#define TIMER_TM1_MODE_ADDR 0x14
#define TIMER_TM1_TARGET_ADDR 0x18
#define TIMER_TM2_COUNT_ADDR 0x20
#define TIMER_TM2_MODE_ADDR 0x24
#define TIMER_TM2_TARGET_ADDR 0x28
// CD-ROM Controller
#define CDROM_BASE 0x1F801800
#define CDROM_CD0_DATA_ADDR 0x00
#define CDROM_CD0_STATUS_ADDR 0x01
#define CDROM_CD0_RESPONSE_ADDR 0x02
#define CDROM_CD0_DATA1_ADDR 0x03
#define CDROM_CD0_INT_FLAG_ADDR 0x04
#define CDROM_CD0_INT_FLAG_INT1_BIT 0  // Data Ready
#define CDROM_CD0_INT_FLAG_INT2_BIT 1  // Command Complete
#define CDROM_CD0_INT_FLAG_INT3_BIT 2  // Acknowledge Received
#define CDROM_CD0_INT_FLAG_INT4_BIT 3  // Error / N-Complete
#define CDROM_CD0_VOLUME_L_ADDR 0x08
#define CDROM_CD0_VOLUME_R_ADDR 0x09
// JOY Interface
#define JOY_BASE 0x1F801040
#define JOY_JOY_CTRL_ADDR 0x00
#define JOY_JOY_CTRL_TX_EN_BIT 0  // Transmit Enable
#define JOY_JOY_CTRL_RX_EN_BIT 1  // Receive Enable
#define JOY_JOY_CTRL_CLOCK_BIT 3  // Internal/External Clock
#define JOY_JOY_CTRL_IRQ_EN_BIT 4  // IRQ Enable
#define JOY_JOY_MODE_ADDR 0x01
#define JOY_JOY_BAUD_ADDR 0x02
#define JOY_JOY_TX_DATA_ADDR 0x04
#define JOY_JOY_RX_DATA_ADDR 0x05
#define JOY_JOY_STAT_ADDR 0x06
#define JOY_JOY_STAT_TX_EMPTY_BIT 0  // Transmit Buffer Empty
#define JOY_JOY_STAT_RX_READY_BIT 2  // Receive Data Ready
#define JOY_JOY_STAT_TX_IRQ_BIT 3  // Transmit IRQ Pending
#define JOY_JOY_STAT_RX_IRQ_BIT 4  // Receive IRQ Pending
// SIO (Serial I/O - Memory Card)
#define SIO_BASE 0x1F801050
#define SIO_SIO_DATA_ADDR 0x00
#define SIO_SIO_STATUS_ADDR 0x01
#define SIO_SIO_MODE_ADDR 0x02
#define SIO_SIO_CTRL_ADDR 0x03
#define SIO_SIO_BAUD_ADDR 0x04
// Interrupt Controller
#define INTERRUPT_BASE 0x1F801070
#define INTERRUPT_INT_STAT_ADDR 0x00
#define INTERRUPT_INT_MASK_ADDR 0x04
#define INTERRUPT_INT_MASK_VBLANK_BIT 0  // V-Blank Interrupt
#define INTERRUPT_INT_MASK_GPU_BIT 1  // GPU Interrupt
#define INTERRUPT_INT_MASK_CDROM_BIT 2  // CD-ROM Interrupt
#define INTERRUPT_INT_MASK_DMA0_BIT 3  // DMA Channel 0
#define INTERRUPT_INT_MASK_DMA1_BIT 4  // DMA Channel 1
#define INTERRUPT_INT_MASK_DMA2_BIT 5  // DMA Channel 2
#define INTERRUPT_INT_MASK_DMA3_BIT 6  // DMA Channel 3
#define INTERRUPT_INT_MASK_DMA4_BIT 7  // DMA Channel 4
#define INTERRUPT_INT_MASK_DMA5_BIT 8  // DMA Channel 5
#define INTERRUPT_INT_MASK_DMA6_BIT 9  // DMA Channel 6
#define INTERRUPT_INT_MASK_TIMER0_BIT 10  // Timer 0
#define INTERRUPT_INT_MASK_TIMER1_BIT 11  // Timer 1
#define INTERRUPT_INT_MASK_TIMER2_BIT 12  // Timer 2
#define INTERRUPT_INT_MASK_SIO_BIT 13  // SIO / Memory Card
#define INTERRUPT_INT_MASK_SPU_BIT 14  // SPU Interrupt
#define INTERRUPT_INT_MASK_PIO_BIT 15  // PIO (Expansion)

// 中断向量定义
#define INT_VBLANK 0  // V-Blank Interrupt (60Hz NTSC / 50Hz PAL)
#define INT_GPU 1  // GPU Interrupt (drawing complete / V-Blank)
#define INT_CDROM 2  // CD-ROM Interrupt
#define INT_DMA0 3  // DMA Channel 0 Complete
#define INT_DMA1 4  // DMA Channel 1 Complete
#define INT_DMA2 5  // DMA Channel 2 Complete
#define INT_DMA3 6  // DMA Channel 3 Complete
#define INT_DMA4 7  // DMA Channel 4 Complete
#define INT_DMA5 8  // DMA Channel 5 Complete
#define INT_DMA6 9  // DMA Channel 6 Complete
#define INT_TIMER0 10  // Timer 0 Interrupt
#define INT_TIMER1 11  // Timer 1 Interrupt
#define INT_TIMER2 12  // Timer 2 Interrupt
#define INT_SIO 13  // SIO / Memory Card Interrupt
#define INT_SPU 14  // SPU Interrupt
#define INT_PIO 15  // PIO / Expansion Interrupt

// 引脚定义
#define PIN_VCC 1  // Power Supply (3.3V regulated)
#define PIN_VSS 2  // Ground
#define PIN_CLK 3  // System Clock Input (53.6932MHz / 2 = 26.8466MHz bus)
#define PIN_RESET 4  // Reset (active low)
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
#define PIN_NCS2 73  // Chip Select 2 (I/O)
#define PIN_NWR 74  // Write Enable
#define PIN_NRD 75  // Read Enable
#define PIN_BE0 76  // Byte Enable 0 (bits 0-7)
#define PIN_BE1 77  // Byte Enable 1 (bits 8-15)
#define PIN_BE2 78  // Byte Enable 2 (bits 16-23)
#define PIN_BE3 79  // Byte Enable 3 (bits 24-31)
#define PIN_BUSREQ 80  // Bus Request (from external DMA)
#define PIN_BUSACK 81  // Bus Acknowledge
#define PIN_INT0 82  // Interrupt 0 (V-Blank)
#define PIN_INT1 83  // Interrupt 1 (GPU)
#define PIN_INT2 84  // Interrupt 2 (CD-ROM)
#define PIN_INT3 85  // Interrupt 3 (DMA)
#define PIN_INT4 86  // Interrupt 4 (Timer)
#define PIN_INT5 87  // Interrupt 5 (SIO)
#define PIN_AUDIO_L 88  // Audio Output Left
#define PIN_AUDIO_R 89  // Audio Output Right
#define PIN_VIDEO_R 90  // Video Output Red (analog RGB)
#define PIN_VIDEO_G 91  // Video Output Green
#define PIN_VIDEO_B 92  // Video Output Blue
#define PIN_SYNC 93  // Video Sync / Composite

#endif /* MIPS-R3000A_DEVICE_H */
