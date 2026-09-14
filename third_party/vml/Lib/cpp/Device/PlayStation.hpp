#ifndef MIPS_R3000A_HPP
#define MIPS_R3000A_HPP

// MIPS-R3000A寄存器定义
// 生成自: Sony / MIPS Technologies/MIPS-I/MIPS-R3000A
// 版本: 1.0
// 日期: 2026-04-16


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: MIPS-R3000A
// 位宽: 32位
// 时钟频率: 33870000 Hz

// 寄存器定义
// Hard-wired Zero
#define R0 (*(volatile uint32_t*)0x00)

// Assembler Temporary
#define R1 (*(volatile uint32_t*)0x04)

// Value Returned by Subroutines
#define R2 (*(volatile uint32_t*)0x08)

// Expression Evaluation
#define R3 (*(volatile uint32_t*)0x0C)

// Expression Evaluation
#define R4 (*(volatile uint32_t*)0x10)

// Expression Evaluation
#define R5 (*(volatile uint32_t*)0x14)

// Expression Evaluation
#define R6 (*(volatile uint32_t*)0x18)

// Expression Evaluation
#define R7 (*(volatile uint32_t*)0x1C)

// Expression Evaluation
#define R8 (*(volatile uint32_t*)0x20)

// Expression Evaluation
#define R9 (*(volatile uint32_t*)0x24)

// Expression Evaluation
#define R10 (*(volatile uint32_t*)0x28)

// Expression Evaluation
#define R11 (*(volatile uint32_t*)0x2C)

// Expression Evaluation
#define R12 (*(volatile uint32_t*)0x30)

// Expression Evaluation
#define R13 (*(volatile uint32_t*)0x34)

// Expression Evaluation
#define R14 (*(volatile uint32_t*)0x38)

// Expression Evaluation
#define R15 (*(volatile uint32_t*)0x3C)

// Saved Value
#define R16 (*(volatile uint32_t*)0x40)

// Saved Value
#define R17 (*(volatile uint32_t*)0x44)

// Saved Value
#define R18 (*(volatile uint32_t*)0x48)

// Saved Value
#define R19 (*(volatile uint32_t*)0x4C)

// Saved Value
#define R20 (*(volatile uint32_t*)0x50)

// Saved Value
#define R21 (*(volatile uint32_t*)0x54)

// Saved Value
#define R22 (*(volatile uint32_t*)0x58)

// Saved Value
#define R23 (*(volatile uint32_t*)0x5C)

// Temporary
#define R24 (*(volatile uint32_t*)0x60)

// Temporary
#define R25 (*(volatile uint32_t*)0x64)

// Kernel Reserved
#define R26 (*(volatile uint32_t*)0x68)

// Kernel Reserved
#define R27 (*(volatile uint32_t*)0x6C)

// Global Pointer
#define R28 (*(volatile uint32_t*)0x70)

// Stack Pointer
#define R29 (*(volatile uint32_t*)0x74)

// Frame Pointer
#define R30 (*(volatile uint32_t*)0x78)

// Return Address
#define R31 (*(volatile uint32_t*)0x7C)

// Multiply/Divide High
#define HI (*(volatile uint32_t*)0x80)

// Multiply/Divide Low
#define LO (*(volatile uint32_t*)0x84)

// Program Counter
#define PC (*(volatile uint32_t*)0x88)

// Coprocessor 0 - Status Register
#define CP0_SR (*(volatile uint32_t*)0x90)
#define CP0_SR_IE 0  // Interrupt Enable
#define CP0_SR_EXL 1  // Exception Level
#define CP0_SR_ERL 2  // Error Level
#define CP0_SR_KSU 0  // Kernel/User Mode
#define CP0_SR_IM0_7 0  // Interrupt Mask bits
#define CP0_SR_CU0 28  // Coprocessor 0 Usable
#define CP0_SR_BEV 22  // Bootstrap Exception Vector

// Coprocessor 0 - Cause Register
#define CP0_CAUSE (*(volatile uint32_t*)0x94)
#define CP0_CAUSE_EXCCODE 0  // Exception Code
#define CP0_CAUSE_IP0_7 0  // Interrupt Pending bits

// Coprocessor 0 - Exception PC
#define CP0_EPC (*(volatile uint32_t*)0x98)

// Coprocessor 0 - Bad Virtual Address
#define CP0_BADVADDR (*(volatile uint32_t*)0x9C)

// Coprocessor 0 - Context Register
#define CP0_CONTEXT (*(volatile uint32_t*)0xA0)

// Coprocessor 0 - Process ID
#define CP0_PID (*(volatile uint32_t*)0xA4)

// 内存段定义
// KSEG0 - Cached RAM (2MB System RAM)
#define KSEG0_START 0x80000000
#define KSEG0_END 0x801FFFFF
#define KSEG0_SIZE 2097152

// KSEG1 - Uncached RAM (2MB System RAM)
#define KSEG1_START 0xA0000000
#define KSEG1_END 0xA01FFFFF
#define KSEG1_SIZE 2097152

// VRAM (1MB, mirrored in KSEG1 at 0xB0000000)
#define VRAM_START 0xA0000000
#define VRAM_END 0xA01FFFFF
#define VRAM_SIZE 1048576

// Expansion Region (maps to expansion RAM area)
#define EXPANSION_START 0xA0000000
#define EXPANSION_END 0xA00FFFFF
#define EXPANSION_SIZE 1048576

// Data Scratchpad (1KB)
#define SCRATCHPAD_START 0x1F800000
#define SCRATCHPAD_END 0x1F8003FF
#define SCRATCHPAD_SIZE 1024

// Kernel BIOS ROM (512KB)
#define EXP_ROM_START 0x1FC00000
#define EXP_ROM_END 0x1FC7FFFF
#define EXP_ROM_SIZE 524288

// User ROM / Kernel Expansion
#define USER_ROM_START 0x1F800000
#define USER_ROM_END 0x1FBFFFFF
#define USER_ROM_SIZE 4194304

// I/O Register Area (Expansion 1)
#define MMIO_START 0x1F801000
#define MMIO_END 0x1F802FFF
#define MMIO_SIZE 8192

// GPU Registers
#define GPU_START 0x1F801810
#define GPU_END 0x1F801817
#define GPU_SIZE 8

// CD-ROM Registers
#define CDROM_START 0x1F801800
#define CDROM_END 0x1F80180F
#define CDROM_SIZE 16

// SPU Registers
#define SPU_START 0x1F801C00
#define SPU_END 0x1F801DFF
#define SPU_SIZE 512

// Interrupt Control
#define IRQ_START 0x1F801070
#define IRQ_END 0x1F801077
#define IRQ_SIZE 8

// DMA Registers (7 channels)
#define DMA_START 0x1F801080
#define DMA_END 0x1F8010FF
#define DMA_SIZE 128

// Timer Registers
#define TIMER_START 0x1F801100
#define TIMER_END 0x1F80112F
#define TIMER_SIZE 48

// JOY Interface Registers
#define JOY_START 0x1F801040
#define JOY_END 0x1F80104F
#define JOY_SIZE 16

// MDEC (Motion Decoder) Registers
#define MDEC_START 0x1F801820
#define MDEC_END 0x1F801827
#define MDEC_SIZE 8

// SIO Registers
#define SIO_START 0x1F801050
#define SIO_END 0x1F80105F
#define SIO_SIZE 16

// GPU Status
#define GPU_STAT_START 0x1F801814
#define GPU_STAT_END 0x1F801817
#define GPU_STAT_SIZE 4

// CD-ROM Status
#define CDROM_STAT_START 0x1F801801
#define CDROM_STAT_END 0x1F801803
#define CDROM_STAT_SIZE 3

// SPU Work RAM (1KB)
#define SPU_RAM_START 0x1F800000
#define SPU_RAM_END 0x1F800FFF
#define SPU_RAM_SIZE 1024

// 外设定义
// Graphics Processing Unit
#define GPU_BASE 0x1F801810
#define GPU_GP0_CMD (*(volatile uint32_t*)0x1F801810)
#define GPU_GP0_DATA (*(volatile uint32_t*)0x1F801814)
#define GPU_GP1_CMD (*(volatile uint32_t*)0x1F801818)
#define GPU_GP1_DATA (*(volatile uint32_t*)0x1F80181C)
#define GPU_TPAGE (*(volatile uint8_t*)0x1F801810)
#define GPU_DRAW_MODE (*(volatile uint8_t*)0x1F801811)
#define GPU_TEXTURE_WIN (*(volatile uint8_t*)0x1F801812)
#define GPU_DRAW_OFFSET_X (*(volatile uint8_t*)0x1F801813)
#define GPU_DRAW_OFFSET_Y (*(volatile uint8_t*)0x1F801814)
#define GPU_DRAW_AREA_X (*(volatile uint8_t*)0x1F801815)
#define GPU_DRAW_AREA_Y (*(volatile uint8_t*)0x1F801816)
#define GPU_DITHER (*(volatile uint8_t*)0x1F801817)
#define GPU_DISPLAY_MODE (*(volatile uint8_t*)0x1F801818)
#define GPU_DISPLAY_START_X (*(volatile uint8_t*)0x1F801819)
#define GPU_DISPLAY_START_Y (*(volatile uint8_t*)0x1F80181A)
#define GPU_DISPLAY_HORZ (*(volatile uint8_t*)0x1F80181B)
#define GPU_DISPLAY_VERT (*(volatile uint8_t*)0x1F80181C)
#define GPU_DMA_MODE (*(volatile uint8_t*)0x1F80181D)
#define GPU_GPU_STAT (*(volatile uint8_t*)0x1F80181C)
#define GPU_GPU_STAT_READY_CMD 0  // GPU Ready to Receive Command
#define GPU_GPU_STAT_READY_DMA 1  // GPU Ready for DMA
#define GPU_GPU_STAT_DRAWING 2  // Drawing Busy
#define GPU_GPU_STAT_DMA_REQ 3  // DMA Request
#define GPU_GPU_STAT_COMMAND_BUSY 4  // Command Busy
#define GPU_GPU_STAT_DISPLAY_DISABLE 5  // Display Disable
#define GPU_GPU_STAT_INTERRUPT 24  // V-Blank Interrupt Flag

// Geometry Transformation Engine
#define GTE_BASE 0x1F801880
#define GTE_GTE_VXY0 (*(volatile uint32_t*)0x1F801880)
#define GTE_GTE_VZ0 (*(volatile uint32_t*)0x1F801884)
#define GTE_GTE_VXY1 (*(volatile uint32_t*)0x1F801888)
#define GTE_GTE_VZ1 (*(volatile uint32_t*)0x1F80188C)
#define GTE_GTE_VXY2 (*(volatile uint32_t*)0x1F801890)
#define GTE_GTE_VZ2 (*(volatile uint32_t*)0x1F801894)
#define GTE_GTE_RGB0 (*(volatile uint32_t*)0x1F801898)
#define GTE_GTE_RGB1 (*(volatile uint32_t*)0x1F80189C)
#define GTE_GTE_RGB2 (*(volatile uint32_t*)0x1F8018A0)
#define GTE_GTE_RTP (*(volatile uint32_t*)0x1F8018B0)
#define GTE_GTE_TRX (*(volatile uint32_t*)0x1F8018B4)
#define GTE_GTE_TRY (*(volatile uint32_t*)0x1F8018B8)
#define GTE_GTE_TRZ (*(volatile uint32_t*)0x1F8018BC)
#define GTE_GTE_MAC0 (*(volatile uint32_t*)0x1F8018C0)
#define GTE_GTE_MAC1 (*(volatile uint32_t*)0x1F8018C4)
#define GTE_GTE_MAC2 (*(volatile uint32_t*)0x1F8018C8)
#define GTE_GTE_MAC3 (*(volatile uint32_t*)0x1F8018CC)
#define GTE_GTE_IR0 (*(volatile uint32_t*)0x1F8018D0)
#define GTE_GTE_IR1 (*(volatile uint32_t*)0x1F8018D4)
#define GTE_GTE_IR2 (*(volatile uint32_t*)0x1F8018D8)
#define GTE_GTE_IR3 (*(volatile uint32_t*)0x1F8018DC)
#define GTE_GTE_LZCS (*(volatile uint32_t*)0x1F8018E0)
#define GTE_GTE_LZCR (*(volatile uint32_t*)0x1F8018E4)
#define GTE_GTE_CTX (*(volatile uint32_t*)0x1F8018E8)
#define GTE_GTE_CTY (*(volatile uint32_t*)0x1F8018EC)
#define GTE_GTE_CTZ (*(volatile uint32_t*)0x1F8018F0)
#define GTE_GTE_RTX (*(volatile uint32_t*)0x1F8018F4)
#define GTE_GTE_RTY (*(volatile uint32_t*)0x1F8018F8)
#define GTE_GTE_RTZ (*(volatile uint32_t*)0x1F8018FC)
#define GTE_GTE_SR (*(volatile uint32_t*)0x1F801900)
#define GTE_GTE_CMD (*(volatile uint32_t*)0x1F801904)
#define GTE_GTE_H (*(volatile uint32_t*)0x1F801908)
#define GTE_GTE_DQB (*(volatile uint32_t*)0x1F80190C)
#define GTE_GTE_DQA (*(volatile uint32_t*)0x1F801910)
#define GTE_GTE_ZSF3 (*(volatile uint32_t*)0x1F801914)
#define GTE_GTE_ZSF4 (*(volatile uint32_t*)0x1F801918)
#define GTE_GTE_OTZ (*(volatile uint32_t*)0x1F80191C)

// Sound Processing Unit (24-channel ADPCM)
#define SPU_BASE 0x1F801C00
#define SPU_SPU_CTRL (*(volatile uint16_t*)0x1F801C00)
#define SPU_SPU_CTRL_REVERB_MASTER 0  // Reverb Master Enable
#define SPU_SPU_CTRL_IRQ9 9  // Interrupt Request Enable
#define SPU_SPU_STAT (*(volatile uint16_t*)0x1F801C04)
#define SPU_SPU_CDVOL_L (*(volatile uint16_t*)0x1F801C08)
#define SPU_SPU_CDVOL_R (*(volatile uint16_t*)0x1F801C0A)
#define SPU_SPU_MAINVOL_L (*(volatile uint16_t*)0x1F801C0C)
#define SPU_SPU_MAINVOL_R (*(volatile uint16_t*)0x1F801C0E)
#define SPU_SPU_REVERB_L (*(volatile uint16_t*)0x1F801C10)
#define SPU_SPU_REVERB_R (*(volatile uint16_t*)0x1F801C12)
#define SPU_SPU_KEYON (*(volatile uint16_t*)0x1F801C80)
#define SPU_SPU_KEYOFF (*(volatile uint16_t*)0x1F801C82)
#define SPU_SPU_CHANNEL_MUTE (*(volatile uint16_t*)0x1F801C84)
#define SPU_SPU_NOISE_CLK (*(volatile uint16_t*)0x1F801C88)
#define SPU_SPU_REVERB_ADDR (*(volatile uint16_t*)0x1F801C8A)
#define SPU_SPU_IRQ_ADDR (*(volatile uint16_t*)0x1F801C8C)
#define SPU_SPU_REVERB_VOL_L (*(volatile uint16_t*)0x1F801C8E)
#define SPU_SPU_REVERB_VOL_R (*(volatile uint16_t*)0x1F801C90)
#define SPU_SPU_VOICE_VOL_L (*(volatile uint8_t*)0x1F801C00)
#define SPU_SPU_VOICE_VOL_R (*(volatile uint8_t*)0x1F801C01)
#define SPU_SPU_VOICE_FREQ (*(volatile uint16_t*)0x1F801C02)
#define SPU_SPU_VOICE_START (*(volatile uint16_t*)0x1F801C04)
#define SPU_SPU_VOICE_ADSR1 (*(volatile uint16_t*)0x1F801C06)
#define SPU_SPU_VOICE_ADSR2 (*(volatile uint16_t*)0x1F801C08)
#define SPU_SPU_VOICE_ENV (*(volatile uint16_t*)0x1F801C0A)
#define SPU_SPU_VOICE_REPEAT (*(volatile uint16_t*)0x1F801C0C)
#define SPU_VOICE_BASE_SIZE (*(volatile uint8_t*)0x1F801C10)

// Motion Decoder (JPEG Decompression)
#define MDEC_BASE 0x1F801820
#define MDEC_MDEC_CTRL (*(volatile uint32_t*)0x1F801820)
#define MDEC_MDEC_CTRL_DATA_IN_SIZE 0  // Data-in size in words
#define MDEC_MDEC_CTRL_RESET 16  // Reset MDEC
#define MDEC_MDEC_CTRL_BUSY 17  // MDEC Busy
#define MDEC_MDEC_DATA (*(volatile uint32_t*)0x1F801824)
#define MDEC_MDEC_BKGD (*(volatile uint32_t*)0x1F801828)

// DMA Controller (7 channels)
#define DMA_BASE 0x1F801080
#define DMA_DMA_DPCR (*(volatile uint8_t*)0x1F801080)
#define DMA_DMA_DPCR_CH0_EN 0  // Channel 0 Enable
#define DMA_DMA_DPCR_CH1_EN 4  // Channel 1 Enable
#define DMA_DMA_DPCR_CH2_EN 8  // Channel 2 Enable
#define DMA_DMA_DPCR_CH3_EN 12  // Channel 3 Enable
#define DMA_DMA_DPCR_CH4_EN 16  // Channel 4 Enable
#define DMA_DMA_DPCR_CH5_EN 20  // Channel 5 Enable
#define DMA_DMA_DPCR_CH6_EN 24  // Channel 6 Enable
#define DMA_DMA_INT (*(volatile uint8_t*)0x1F801084)
#define DMA_DMA_CH0_BASE (*(volatile uint32_t*)0x1F801090)
#define DMA_DMA_CH0_COUNT (*(volatile uint16_t*)0x1F801094)
#define DMA_DMA_CH0_CTRL (*(volatile uint8_t*)0x1F801098)
#define DMA_DMA_CH0_CTRL_DEST_DIR 0  // Destination Direction
#define DMA_DMA_CH0_CTRL_SRC_DIR 0  // Source Direction
#define DMA_DMA_CH0_CTRL_STEPS 0  // Step
#define DMA_DMA_CH0_CTRL_CHAIN 0  // Chain Mode (0=manual, 1=request, 2=chain, 3=illegal)
#define DMA_DMA_CH0_CTRL_SYNC 0  // Sync Mode (0=immediate, 1=request, 2=linked-list)
#define DMA_DMA_CH0_CTRL_TRIGGER 10  // Trigger
#define DMA_DMA_CH1_BASE (*(volatile uint32_t*)0x1F8010A0)
#define DMA_DMA_CH1_COUNT (*(volatile uint16_t*)0x1F8010A4)
#define DMA_DMA_CH1_CTRL (*(volatile uint8_t*)0x1F8010A8)
#define DMA_DMA_CH2_BASE (*(volatile uint32_t*)0x1F8010B0)
#define DMA_DMA_CH2_COUNT (*(volatile uint16_t*)0x1F8010B4)
#define DMA_DMA_CH2_CTRL (*(volatile uint8_t*)0x1F8010B8)
#define DMA_DMA_CH3_BASE (*(volatile uint32_t*)0x1F8010C0)
#define DMA_DMA_CH3_COUNT (*(volatile uint16_t*)0x1F8010C4)
#define DMA_DMA_CH3_CTRL (*(volatile uint8_t*)0x1F8010C8)
#define DMA_DMA_CH4_BASE (*(volatile uint32_t*)0x1F8010D0)
#define DMA_DMA_CH4_COUNT (*(volatile uint16_t*)0x1F8010D4)
#define DMA_DMA_CH4_CTRL (*(volatile uint8_t*)0x1F8010D8)
#define DMA_DMA_CH5_BASE (*(volatile uint32_t*)0x1F8010E0)
#define DMA_DMA_CH5_COUNT (*(volatile uint16_t*)0x1F8010E4)
#define DMA_DMA_CH5_CTRL (*(volatile uint8_t*)0x1F8010E8)
#define DMA_DMA_CH6_BASE (*(volatile uint32_t*)0x1F8010F0)
#define DMA_DMA_CH6_COUNT (*(volatile uint16_t*)0x1F8010F4)
#define DMA_DMA_CH6_CTRL (*(volatile uint8_t*)0x1F8010F8)

// Timers (3 timers)
#define TIMER_BASE 0x1F801100
#define TIMER_TM0_COUNT (*(volatile uint16_t*)0x1F801100)
#define TIMER_TM0_MODE (*(volatile uint16_t*)0x1F801104)
#define TIMER_TM0_MODE_RELOAD 0  // Reload Enable
#define TIMER_TM0_MODE_CLOCK 0  // Clock Source (0=sysclk/1, 1=sysclk/8, 2=sysclk/64, 3=sysclk/256)
#define TIMER_TM0_MODE_IRQ_EN 3  // IRQ Enable
#define TIMER_TM0_MODE_IRQ_REPEAT 4  // IRQ Repeat
#define TIMER_TM0_MODE_IRQ_TOGGLE 5  // IRQ Toggle Mode
#define TIMER_TM0_MODE_REACH_MAX 6  // Reached Max Value
#define TIMER_TM0_TARGET (*(volatile uint16_t*)0x1F801108)
#define TIMER_TM1_COUNT (*(volatile uint16_t*)0x1F801110)
#define TIMER_TM1_MODE (*(volatile uint16_t*)0x1F801114)
#define TIMER_TM1_TARGET (*(volatile uint16_t*)0x1F801118)
#define TIMER_TM2_COUNT (*(volatile uint16_t*)0x1F801120)
#define TIMER_TM2_MODE (*(volatile uint16_t*)0x1F801124)
#define TIMER_TM2_TARGET (*(volatile uint16_t*)0x1F801128)

// CD-ROM Controller
#define CDROM_BASE 0x1F801800
#define CDROM_CD0_DATA (*(volatile uint8_t*)0x1F801800)
#define CDROM_CD0_STATUS (*(volatile uint8_t*)0x1F801801)
#define CDROM_CD0_RESPONSE (*(volatile uint8_t*)0x1F801802)
#define CDROM_CD0_DATA1 (*(volatile uint8_t*)0x1F801803)
#define CDROM_CD0_INT_FLAG (*(volatile uint8_t*)0x1F801804)
#define CDROM_CD0_INT_FLAG_INT1 0  // Data Ready
#define CDROM_CD0_INT_FLAG_INT2 1  // Command Complete
#define CDROM_CD0_INT_FLAG_INT3 2  // Acknowledge Received
#define CDROM_CD0_INT_FLAG_INT4 3  // Error / N-Complete
#define CDROM_CD0_VOLUME_L (*(volatile uint8_t*)0x1F801808)
#define CDROM_CD0_VOLUME_R (*(volatile uint8_t*)0x1F801809)

// JOY Interface
#define JOY_BASE 0x1F801040
#define JOY_JOY_CTRL (*(volatile uint8_t*)0x1F801040)
#define JOY_JOY_CTRL_TX_EN 0  // Transmit Enable
#define JOY_JOY_CTRL_RX_EN 1  // Receive Enable
#define JOY_JOY_CTRL_CLOCK 3  // Internal/External Clock
#define JOY_JOY_CTRL_IRQ_EN 4  // IRQ Enable
#define JOY_JOY_MODE (*(volatile uint8_t*)0x1F801041)
#define JOY_JOY_BAUD (*(volatile uint8_t*)0x1F801042)
#define JOY_JOY_TX_DATA (*(volatile uint8_t*)0x1F801044)
#define JOY_JOY_RX_DATA (*(volatile uint8_t*)0x1F801045)
#define JOY_JOY_STAT (*(volatile uint8_t*)0x1F801046)
#define JOY_JOY_STAT_TX_EMPTY 0  // Transmit Buffer Empty
#define JOY_JOY_STAT_RX_READY 2  // Receive Data Ready
#define JOY_JOY_STAT_TX_IRQ 3  // Transmit IRQ Pending
#define JOY_JOY_STAT_RX_IRQ 4  // Receive IRQ Pending

// SIO (Serial I/O - Memory Card)
#define SIO_BASE 0x1F801050
#define SIO_SIO_DATA (*(volatile uint8_t*)0x1F801050)
#define SIO_SIO_STATUS (*(volatile uint8_t*)0x1F801051)
#define SIO_SIO_MODE (*(volatile uint8_t*)0x1F801052)
#define SIO_SIO_CTRL (*(volatile uint8_t*)0x1F801053)
#define SIO_SIO_BAUD (*(volatile uint8_t*)0x1F801054)

// Interrupt Controller
#define INTERRUPT_BASE 0x1F801070
#define INTERRUPT_INT_STAT (*(volatile uint16_t*)0x1F801070)
#define INTERRUPT_INT_MASK (*(volatile uint16_t*)0x1F801074)
#define INTERRUPT_INT_MASK_VBLANK 0  // V-Blank Interrupt
#define INTERRUPT_INT_MASK_GPU 1  // GPU Interrupt
#define INTERRUPT_INT_MASK_CDROM 2  // CD-ROM Interrupt
#define INTERRUPT_INT_MASK_DMA0 3  // DMA Channel 0
#define INTERRUPT_INT_MASK_DMA1 4  // DMA Channel 1
#define INTERRUPT_INT_MASK_DMA2 5  // DMA Channel 2
#define INTERRUPT_INT_MASK_DMA3 6  // DMA Channel 3
#define INTERRUPT_INT_MASK_DMA4 7  // DMA Channel 4
#define INTERRUPT_INT_MASK_DMA5 8  // DMA Channel 5
#define INTERRUPT_INT_MASK_DMA6 9  // DMA Channel 6
#define INTERRUPT_INT_MASK_TIMER0 10  // Timer 0
#define INTERRUPT_INT_MASK_TIMER1 11  // Timer 1
#define INTERRUPT_INT_MASK_TIMER2 12  // Timer 2
#define INTERRUPT_INT_MASK_SIO 13  // SIO / Memory Card
#define INTERRUPT_INT_MASK_SPU 14  // SPU Interrupt
#define INTERRUPT_INT_MASK_PIO 15  // PIO (Expansion)

// 中断向量定义
#define VBLANK_VECTOR 0  // V-Blank Interrupt (60Hz NTSC / 50Hz PAL)
#define GPU_VECTOR 1  // GPU Interrupt (drawing complete / V-Blank)
#define CDROM_VECTOR 2  // CD-ROM Interrupt
#define DMA0_VECTOR 3  // DMA Channel 0 Complete
#define DMA1_VECTOR 4  // DMA Channel 1 Complete
#define DMA2_VECTOR 5  // DMA Channel 2 Complete
#define DMA3_VECTOR 6  // DMA Channel 3 Complete
#define DMA4_VECTOR 7  // DMA Channel 4 Complete
#define DMA5_VECTOR 8  // DMA Channel 5 Complete
#define DMA6_VECTOR 9  // DMA Channel 6 Complete
#define TIMER0_VECTOR 10  // Timer 0 Interrupt
#define TIMER1_VECTOR 11  // Timer 1 Interrupt
#define TIMER2_VECTOR 12  // Timer 2 Interrupt
#define SIO_VECTOR 13  // SIO / Memory Card Interrupt
#define SPU_VECTOR 14  // SPU Interrupt
#define PIO_VECTOR 15  // PIO / Expansion Interrupt

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

void mips_r3000a_init(void);

#ifdef __cplusplus
}
#endif

#endif // MIPS_R3000A_HPP
