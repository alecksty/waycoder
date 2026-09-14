(*
 * MIPS-R3000A梯形图定义
 * 生成自: Sony / MIPS Technologies/MIPS-I/MIPS-R3000A
 * 版本: 1.0
 * 日期: 2026-04-16
 * 作者: VML Team
 * 描述: Sony PlayStation (PS1) main processor - MIPS R3000A @ 33.87MHz with R4000-like ISA
 * CPU架构: MIPS-R3000A
 * 位宽: 32位
 * 时钟频率: 33870000 Hz
 *)

PROGRAM MIPS_R3000A
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    R0 AT %M0x00 : DWORD; (* Hard-wired Zero *)
    R1 AT %M0x04 : DWORD; (* Assembler Temporary *)
    R2 AT %M0x08 : DWORD; (* Value Returned by Subroutines *)
    R3 AT %M0x0C : DWORD; (* Expression Evaluation *)
    R4 AT %M0x10 : DWORD; (* Expression Evaluation *)
    R5 AT %M0x14 : DWORD; (* Expression Evaluation *)
    R6 AT %M0x18 : DWORD; (* Expression Evaluation *)
    R7 AT %M0x1C : DWORD; (* Expression Evaluation *)
    R8 AT %M0x20 : DWORD; (* Expression Evaluation *)
    R9 AT %M0x24 : DWORD; (* Expression Evaluation *)
    R10 AT %M0x28 : DWORD; (* Expression Evaluation *)
    R11 AT %M0x2C : DWORD; (* Expression Evaluation *)
    R12 AT %M0x30 : DWORD; (* Expression Evaluation *)
    R13 AT %M0x34 : DWORD; (* Expression Evaluation *)
    R14 AT %M0x38 : DWORD; (* Expression Evaluation *)
    R15 AT %M0x3C : DWORD; (* Expression Evaluation *)
    R16 AT %M0x40 : DWORD; (* Saved Value *)
    R17 AT %M0x44 : DWORD; (* Saved Value *)
    R18 AT %M0x48 : DWORD; (* Saved Value *)
    R19 AT %M0x4C : DWORD; (* Saved Value *)
    R20 AT %M0x50 : DWORD; (* Saved Value *)
    R21 AT %M0x54 : DWORD; (* Saved Value *)
    R22 AT %M0x58 : DWORD; (* Saved Value *)
    R23 AT %M0x5C : DWORD; (* Saved Value *)
    R24 AT %M0x60 : DWORD; (* Temporary *)
    R25 AT %M0x64 : DWORD; (* Temporary *)
    R26 AT %M0x68 : DWORD; (* Kernel Reserved *)
    R27 AT %M0x6C : DWORD; (* Kernel Reserved *)
    R28 AT %M0x70 : DWORD; (* Global Pointer *)
    R29 AT %M0x74 : DWORD; (* Stack Pointer *)
    R30 AT %M0x78 : DWORD; (* Frame Pointer *)
    R31 AT %M0x7C : DWORD; (* Return Address *)
    HI AT %M0x80 : DWORD; (* Multiply/Divide High *)
    LO AT %M0x84 : DWORD; (* Multiply/Divide Low *)
    PC AT %M0x88 : DWORD; (* Program Counter *)
    CP0_SR AT %M0x90 : DWORD; (* Coprocessor 0 - Status Register *)
    CP0_SR_IE AT %M0x90.0 : BOOL; (* Interrupt Enable *)
    CP0_SR_EXL AT %M0x90.1 : BOOL; (* Exception Level *)
    CP0_SR_ERL AT %M0x90.2 : BOOL; (* Error Level *)
    CP0_SR_KSU AT %M0x90.0 : BOOL; (* Kernel/User Mode *)
    CP0_SR_IM0_7 AT %M0x90.0 : BOOL; (* Interrupt Mask bits *)
    CP0_SR_CU0 AT %M0x90.28 : BOOL; (* Coprocessor 0 Usable *)
    CP0_SR_BEV AT %M0x90.22 : BOOL; (* Bootstrap Exception Vector *)
    CP0_CAUSE AT %M0x94 : DWORD; (* Coprocessor 0 - Cause Register *)
    CP0_CAUSE_EXCCODE AT %M0x94.0 : BOOL; (* Exception Code *)
    CP0_CAUSE_IP0_7 AT %M0x94.0 : BOOL; (* Interrupt Pending bits *)
    CP0_EPC AT %M0x98 : DWORD; (* Coprocessor 0 - Exception PC *)
    CP0_BADVADDR AT %M0x9C : DWORD; (* Coprocessor 0 - Bad Virtual Address *)
    CP0_CONTEXT AT %M0xA0 : DWORD; (* Coprocessor 0 - Context Register *)
    CP0_PID AT %M0xA4 : DWORD; (* Coprocessor 0 - Process ID *)

    (* 外设变量 *)
    (* Graphics Processing Unit *)
    GPU_BASE AT %MW0x1F801810 : WORD;
    GPU_GP0_CMD AT %MW0x00 : DWORD;
    GPU_GP0_DATA AT %MW0x04 : DWORD;
    GPU_GP1_CMD AT %MW0x08 : DWORD;
    GPU_GP1_DATA AT %MW0x0C : DWORD;
    GPU_TPAGE AT %MW0x00 : BYTE;
    GPU_DRAW_MODE AT %MW0x01 : BYTE;
    GPU_TEXTURE_WIN AT %MW0x02 : BYTE;
    GPU_DRAW_OFFSET_X AT %MW0x03 : BYTE;
    GPU_DRAW_OFFSET_Y AT %MW0x04 : BYTE;
    GPU_DRAW_AREA_X AT %MW0x05 : BYTE;
    GPU_DRAW_AREA_Y AT %MW0x06 : BYTE;
    GPU_DITHER AT %MW0x07 : BYTE;
    GPU_DISPLAY_MODE AT %MW0x08 : BYTE;
    GPU_DISPLAY_START_X AT %MW0x09 : BYTE;
    GPU_DISPLAY_START_Y AT %MW0x0A : BYTE;
    GPU_DISPLAY_HORZ AT %MW0x0B : BYTE;
    GPU_DISPLAY_VERT AT %MW0x0C : BYTE;
    GPU_DMA_MODE AT %MW0x0D : BYTE;
    GPU_GPU_STAT AT %MW0x0C : BYTE;
    GPU_GPU_STAT_READY_CMD AT %MW0x0C.0 : BOOL; (* GPU Ready to Receive Command *)
    GPU_GPU_STAT_READY_DMA AT %MW0x0C.1 : BOOL; (* GPU Ready for DMA *)
    GPU_GPU_STAT_DRAWING AT %MW0x0C.2 : BOOL; (* Drawing Busy *)
    GPU_GPU_STAT_DMA_REQ AT %MW0x0C.3 : BOOL; (* DMA Request *)
    GPU_GPU_STAT_COMMAND_BUSY AT %MW0x0C.4 : BOOL; (* Command Busy *)
    GPU_GPU_STAT_DISPLAY_DISABLE AT %MW0x0C.5 : BOOL; (* Display Disable *)
    GPU_GPU_STAT_INTERRUPT AT %MW0x0C.24 : BOOL; (* V-Blank Interrupt Flag *)
    (* Geometry Transformation Engine *)
    GTE_BASE AT %MW0x1F801880 : WORD;
    GTE_GTE_VXY0 AT %MW0x00 : DWORD;
    GTE_GTE_VZ0 AT %MW0x04 : DWORD;
    GTE_GTE_VXY1 AT %MW0x08 : DWORD;
    GTE_GTE_VZ1 AT %MW0x0C : DWORD;
    GTE_GTE_VXY2 AT %MW0x10 : DWORD;
    GTE_GTE_VZ2 AT %MW0x14 : DWORD;
    GTE_GTE_RGB0 AT %MW0x18 : DWORD;
    GTE_GTE_RGB1 AT %MW0x1C : DWORD;
    GTE_GTE_RGB2 AT %MW0x20 : DWORD;
    GTE_GTE_RTP AT %MW0x30 : DWORD;
    GTE_GTE_TRX AT %MW0x34 : DWORD;
    GTE_GTE_TRY AT %MW0x38 : DWORD;
    GTE_GTE_TRZ AT %MW0x3C : DWORD;
    GTE_GTE_MAC0 AT %MW0x40 : DWORD;
    GTE_GTE_MAC1 AT %MW0x44 : DWORD;
    GTE_GTE_MAC2 AT %MW0x48 : DWORD;
    GTE_GTE_MAC3 AT %MW0x4C : DWORD;
    GTE_GTE_IR0 AT %MW0x50 : DWORD;
    GTE_GTE_IR1 AT %MW0x54 : DWORD;
    GTE_GTE_IR2 AT %MW0x58 : DWORD;
    GTE_GTE_IR3 AT %MW0x5C : DWORD;
    GTE_GTE_LZCS AT %MW0x60 : DWORD;
    GTE_GTE_LZCR AT %MW0x64 : DWORD;
    GTE_GTE_CTX AT %MW0x68 : DWORD;
    GTE_GTE_CTY AT %MW0x6C : DWORD;
    GTE_GTE_CTZ AT %MW0x70 : DWORD;
    GTE_GTE_RTX AT %MW0x74 : DWORD;
    GTE_GTE_RTY AT %MW0x78 : DWORD;
    GTE_GTE_RTZ AT %MW0x7C : DWORD;
    GTE_GTE_SR AT %MW0x80 : DWORD;
    GTE_GTE_CMD AT %MW0x84 : DWORD;
    GTE_GTE_H AT %MW0x88 : DWORD;
    GTE_GTE_DQB AT %MW0x8C : DWORD;
    GTE_GTE_DQA AT %MW0x90 : DWORD;
    GTE_GTE_ZSF3 AT %MW0x94 : DWORD;
    GTE_GTE_ZSF4 AT %MW0x98 : DWORD;
    GTE_GTE_OTZ AT %MW0x9C : DWORD;
    (* Sound Processing Unit (24-channel ADPCM) *)
    SPU_BASE AT %MW0x1F801C00 : WORD;
    SPU_SPU_CTRL AT %MW0x00 : WORD;
    SPU_SPU_CTRL_REVERB_MASTER AT %MW0x00.0 : BOOL; (* Reverb Master Enable *)
    SPU_SPU_CTRL_IRQ9 AT %MW0x00.9 : BOOL; (* Interrupt Request Enable *)
    SPU_SPU_STAT AT %MW0x04 : WORD;
    SPU_SPU_CDVOL_L AT %MW0x08 : WORD;
    SPU_SPU_CDVOL_R AT %MW0x0A : WORD;
    SPU_SPU_MAINVOL_L AT %MW0x0C : WORD;
    SPU_SPU_MAINVOL_R AT %MW0x0E : WORD;
    SPU_SPU_REVERB_L AT %MW0x10 : WORD;
    SPU_SPU_REVERB_R AT %MW0x12 : WORD;
    SPU_SPU_KEYON AT %MW0x80 : WORD;
    SPU_SPU_KEYOFF AT %MW0x82 : WORD;
    SPU_SPU_CHANNEL_MUTE AT %MW0x84 : WORD;
    SPU_SPU_NOISE_CLK AT %MW0x88 : WORD;
    SPU_SPU_REVERB_ADDR AT %MW0x8A : WORD;
    SPU_SPU_IRQ_ADDR AT %MW0x8C : WORD;
    SPU_SPU_REVERB_VOL_L AT %MW0x8E : WORD;
    SPU_SPU_REVERB_VOL_R AT %MW0x90 : WORD;
    SPU_SPU_VOICE_VOL_L AT %MW0x00 : BYTE;
    SPU_SPU_VOICE_VOL_R AT %MW0x01 : BYTE;
    SPU_SPU_VOICE_FREQ AT %MW0x02 : WORD;
    SPU_SPU_VOICE_START AT %MW0x04 : WORD;
    SPU_SPU_VOICE_ADSR1 AT %MW0x06 : WORD;
    SPU_SPU_VOICE_ADSR2 AT %MW0x08 : WORD;
    SPU_SPU_VOICE_ENV AT %MW0x0A : WORD;
    SPU_SPU_VOICE_REPEAT AT %MW0x0C : WORD;
    SPU_VOICE_BASE_SIZE AT %MW0x10 : BYTE;
    (* Motion Decoder (JPEG Decompression) *)
    MDEC_BASE AT %MW0x1F801820 : WORD;
    MDEC_MDEC_CTRL AT %MW0x00 : DWORD;
    MDEC_MDEC_CTRL_DATA_IN_SIZE AT %MW0x00.0 : BOOL; (* Data-in size in words *)
    MDEC_MDEC_CTRL_RESET AT %MW0x00.16 : BOOL; (* Reset MDEC *)
    MDEC_MDEC_CTRL_BUSY AT %MW0x00.17 : BOOL; (* MDEC Busy *)
    MDEC_MDEC_DATA AT %MW0x04 : DWORD;
    MDEC_MDEC_BKGD AT %MW0x08 : DWORD;
    (* DMA Controller (7 channels) *)
    DMA_BASE AT %MW0x1F801080 : WORD;
    DMA_DMA_DPCR AT %MW0x00 : BYTE;
    DMA_DMA_DPCR_CH0_EN AT %MW0x00.0 : BOOL; (* Channel 0 Enable *)
    DMA_DMA_DPCR_CH1_EN AT %MW0x00.4 : BOOL; (* Channel 1 Enable *)
    DMA_DMA_DPCR_CH2_EN AT %MW0x00.8 : BOOL; (* Channel 2 Enable *)
    DMA_DMA_DPCR_CH3_EN AT %MW0x00.12 : BOOL; (* Channel 3 Enable *)
    DMA_DMA_DPCR_CH4_EN AT %MW0x00.16 : BOOL; (* Channel 4 Enable *)
    DMA_DMA_DPCR_CH5_EN AT %MW0x00.20 : BOOL; (* Channel 5 Enable *)
    DMA_DMA_DPCR_CH6_EN AT %MW0x00.24 : BOOL; (* Channel 6 Enable *)
    DMA_DMA_INT AT %MW0x04 : BYTE;
    DMA_DMA_CH0_BASE AT %MW0x10 : DWORD;
    DMA_DMA_CH0_COUNT AT %MW0x14 : WORD;
    DMA_DMA_CH0_CTRL AT %MW0x18 : BYTE;
    DMA_DMA_CH0_CTRL_DEST_DIR AT %MW0x18.0 : BOOL; (* Destination Direction *)
    DMA_DMA_CH0_CTRL_SRC_DIR AT %MW0x18.0 : BOOL; (* Source Direction *)
    DMA_DMA_CH0_CTRL_STEPS AT %MW0x18.0 : BOOL; (* Step *)
    DMA_DMA_CH0_CTRL_CHAIN AT %MW0x18.0 : BOOL; (* Chain Mode (0=manual, 1=request, 2=chain, 3=illegal) *)
    DMA_DMA_CH0_CTRL_SYNC AT %MW0x18.0 : BOOL; (* Sync Mode (0=immediate, 1=request, 2=linked-list) *)
    DMA_DMA_CH0_CTRL_TRIGGER AT %MW0x18.10 : BOOL; (* Trigger *)
    DMA_DMA_CH1_BASE AT %MW0x20 : DWORD;
    DMA_DMA_CH1_COUNT AT %MW0x24 : WORD;
    DMA_DMA_CH1_CTRL AT %MW0x28 : BYTE;
    DMA_DMA_CH2_BASE AT %MW0x30 : DWORD;
    DMA_DMA_CH2_COUNT AT %MW0x34 : WORD;
    DMA_DMA_CH2_CTRL AT %MW0x38 : BYTE;
    DMA_DMA_CH3_BASE AT %MW0x40 : DWORD;
    DMA_DMA_CH3_COUNT AT %MW0x44 : WORD;
    DMA_DMA_CH3_CTRL AT %MW0x48 : BYTE;
    DMA_DMA_CH4_BASE AT %MW0x50 : DWORD;
    DMA_DMA_CH4_COUNT AT %MW0x54 : WORD;
    DMA_DMA_CH4_CTRL AT %MW0x58 : BYTE;
    DMA_DMA_CH5_BASE AT %MW0x60 : DWORD;
    DMA_DMA_CH5_COUNT AT %MW0x64 : WORD;
    DMA_DMA_CH5_CTRL AT %MW0x68 : BYTE;
    DMA_DMA_CH6_BASE AT %MW0x70 : DWORD;
    DMA_DMA_CH6_COUNT AT %MW0x74 : WORD;
    DMA_DMA_CH6_CTRL AT %MW0x78 : BYTE;
    (* Timers (3 timers) *)
    TIMER_BASE AT %MW0x1F801100 : WORD;
    TIMER_TM0_COUNT AT %MW0x00 : WORD;
    TIMER_TM0_MODE AT %MW0x04 : WORD;
    TIMER_TM0_MODE_RELOAD AT %MW0x04.0 : BOOL; (* Reload Enable *)
    TIMER_TM0_MODE_CLOCK AT %MW0x04.0 : BOOL; (* Clock Source (0=sysclk/1, 1=sysclk/8, 2=sysclk/64, 3=sysclk/256) *)
    TIMER_TM0_MODE_IRQ_EN AT %MW0x04.3 : BOOL; (* IRQ Enable *)
    TIMER_TM0_MODE_IRQ_REPEAT AT %MW0x04.4 : BOOL; (* IRQ Repeat *)
    TIMER_TM0_MODE_IRQ_TOGGLE AT %MW0x04.5 : BOOL; (* IRQ Toggle Mode *)
    TIMER_TM0_MODE_REACH_MAX AT %MW0x04.6 : BOOL; (* Reached Max Value *)
    TIMER_TM0_TARGET AT %MW0x08 : WORD;
    TIMER_TM1_COUNT AT %MW0x10 : WORD;
    TIMER_TM1_MODE AT %MW0x14 : WORD;
    TIMER_TM1_TARGET AT %MW0x18 : WORD;
    TIMER_TM2_COUNT AT %MW0x20 : WORD;
    TIMER_TM2_MODE AT %MW0x24 : WORD;
    TIMER_TM2_TARGET AT %MW0x28 : WORD;
    (* CD-ROM Controller *)
    CDROM_BASE AT %MW0x1F801800 : WORD;
    CDROM_CD0_DATA AT %MW0x00 : BYTE;
    CDROM_CD0_STATUS AT %MW0x01 : BYTE;
    CDROM_CD0_RESPONSE AT %MW0x02 : BYTE;
    CDROM_CD0_DATA1 AT %MW0x03 : BYTE;
    CDROM_CD0_INT_FLAG AT %MW0x04 : BYTE;
    CDROM_CD0_INT_FLAG_INT1 AT %MW0x04.0 : BOOL; (* Data Ready *)
    CDROM_CD0_INT_FLAG_INT2 AT %MW0x04.1 : BOOL; (* Command Complete *)
    CDROM_CD0_INT_FLAG_INT3 AT %MW0x04.2 : BOOL; (* Acknowledge Received *)
    CDROM_CD0_INT_FLAG_INT4 AT %MW0x04.3 : BOOL; (* Error / N-Complete *)
    CDROM_CD0_VOLUME_L AT %MW0x08 : BYTE;
    CDROM_CD0_VOLUME_R AT %MW0x09 : BYTE;
    (* JOY Interface *)
    JOY_BASE AT %MW0x1F801040 : WORD;
    JOY_JOY_CTRL AT %MW0x00 : BYTE;
    JOY_JOY_CTRL_TX_EN AT %MW0x00.0 : BOOL; (* Transmit Enable *)
    JOY_JOY_CTRL_RX_EN AT %MW0x00.1 : BOOL; (* Receive Enable *)
    JOY_JOY_CTRL_CLOCK AT %MW0x00.3 : BOOL; (* Internal/External Clock *)
    JOY_JOY_CTRL_IRQ_EN AT %MW0x00.4 : BOOL; (* IRQ Enable *)
    JOY_JOY_MODE AT %MW0x01 : BYTE;
    JOY_JOY_BAUD AT %MW0x02 : BYTE;
    JOY_JOY_TX_DATA AT %MW0x04 : BYTE;
    JOY_JOY_RX_DATA AT %MW0x05 : BYTE;
    JOY_JOY_STAT AT %MW0x06 : BYTE;
    JOY_JOY_STAT_TX_EMPTY AT %MW0x06.0 : BOOL; (* Transmit Buffer Empty *)
    JOY_JOY_STAT_RX_READY AT %MW0x06.2 : BOOL; (* Receive Data Ready *)
    JOY_JOY_STAT_TX_IRQ AT %MW0x06.3 : BOOL; (* Transmit IRQ Pending *)
    JOY_JOY_STAT_RX_IRQ AT %MW0x06.4 : BOOL; (* Receive IRQ Pending *)
    (* SIO (Serial I/O - Memory Card) *)
    SIO_BASE AT %MW0x1F801050 : WORD;
    SIO_SIO_DATA AT %MW0x00 : BYTE;
    SIO_SIO_STATUS AT %MW0x01 : BYTE;
    SIO_SIO_MODE AT %MW0x02 : BYTE;
    SIO_SIO_CTRL AT %MW0x03 : BYTE;
    SIO_SIO_BAUD AT %MW0x04 : BYTE;
    (* Interrupt Controller *)
    INTERRUPT_BASE AT %MW0x1F801070 : WORD;
    INTERRUPT_INT_STAT AT %MW0x00 : WORD;
    INTERRUPT_INT_MASK AT %MW0x04 : WORD;
    INTERRUPT_INT_MASK_VBLANK AT %MW0x04.0 : BOOL; (* V-Blank Interrupt *)
    INTERRUPT_INT_MASK_GPU AT %MW0x04.1 : BOOL; (* GPU Interrupt *)
    INTERRUPT_INT_MASK_CDROM AT %MW0x04.2 : BOOL; (* CD-ROM Interrupt *)
    INTERRUPT_INT_MASK_DMA0 AT %MW0x04.3 : BOOL; (* DMA Channel 0 *)
    INTERRUPT_INT_MASK_DMA1 AT %MW0x04.4 : BOOL; (* DMA Channel 1 *)
    INTERRUPT_INT_MASK_DMA2 AT %MW0x04.5 : BOOL; (* DMA Channel 2 *)
    INTERRUPT_INT_MASK_DMA3 AT %MW0x04.6 : BOOL; (* DMA Channel 3 *)
    INTERRUPT_INT_MASK_DMA4 AT %MW0x04.7 : BOOL; (* DMA Channel 4 *)
    INTERRUPT_INT_MASK_DMA5 AT %MW0x04.8 : BOOL; (* DMA Channel 5 *)
    INTERRUPT_INT_MASK_DMA6 AT %MW0x04.9 : BOOL; (* DMA Channel 6 *)
    INTERRUPT_INT_MASK_TIMER0 AT %MW0x04.10 : BOOL; (* Timer 0 *)
    INTERRUPT_INT_MASK_TIMER1 AT %MW0x04.11 : BOOL; (* Timer 1 *)
    INTERRUPT_INT_MASK_TIMER2 AT %MW0x04.12 : BOOL; (* Timer 2 *)
    INTERRUPT_INT_MASK_SIO AT %MW0x04.13 : BOOL; (* SIO / Memory Card *)
    INTERRUPT_INT_MASK_SPU AT %MW0x04.14 : BOOL; (* SPU Interrupt *)
    INTERRUPT_INT_MASK_PIO AT %MW0x04.15 : BOOL; (* PIO (Expansion) *)

    (* 中断变量 *)
    INT_VBLANK : BOOL; (* V-Blank Interrupt (60Hz NTSC / 50Hz PAL) *)
    INT_GPU : BOOL; (* GPU Interrupt (drawing complete / V-Blank) *)
    INT_CDROM : BOOL; (* CD-ROM Interrupt *)
    INT_DMA0 : BOOL; (* DMA Channel 0 Complete *)
    INT_DMA1 : BOOL; (* DMA Channel 1 Complete *)
    INT_DMA2 : BOOL; (* DMA Channel 2 Complete *)
    INT_DMA3 : BOOL; (* DMA Channel 3 Complete *)
    INT_DMA4 : BOOL; (* DMA Channel 4 Complete *)
    INT_DMA5 : BOOL; (* DMA Channel 5 Complete *)
    INT_DMA6 : BOOL; (* DMA Channel 6 Complete *)
    INT_TIMER0 : BOOL; (* Timer 0 Interrupt *)
    INT_TIMER1 : BOOL; (* Timer 1 Interrupt *)
    INT_TIMER2 : BOOL; (* Timer 2 Interrupt *)
    INT_SIO : BOOL; (* SIO / Memory Card Interrupt *)
    INT_SPU : BOOL; (* SPU Interrupt *)
    INT_PIO : BOOL; (* PIO / Expansion Interrupt *)

END_VAR

BEGIN
    (* MIPS-R3000A初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|R0>0|--(监控灯)--
    --|R1>0|--(监控灯)--
    --|R2>0|--(监控灯)--
    --|R3>0|--(监控灯)--
    --|R4>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_VBLANK|--[中断处理]--
    --|INT_GPU|--[中断处理]--
    --|INT_CDROM|--[中断处理]--
    --|INT_DMA0|--[中断处理]--
    --|INT_DMA1|--[中断处理]--
    --|INT_DMA2|--[中断处理]--
    --|INT_DMA3|--[中断处理]--
    --|INT_DMA4|--[中断处理]--
    --|INT_DMA5|--[中断处理]--
    --|INT_DMA6|--[中断处理]--
    --|INT_TIMER0|--[中断处理]--
    --|INT_TIMER1|--[中断处理]--
    --|INT_TIMER2|--[中断处理]--
    --|INT_SIO|--[中断处理]--
    --|INT_SPU|--[中断处理]--
    --|INT_PIO|--[中断处理]--

END_PROGRAM
