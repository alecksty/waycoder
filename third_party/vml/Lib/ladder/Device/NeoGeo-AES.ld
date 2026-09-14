(*
 * NeoGeo-68000梯形图定义
 * 生成自: SNK/M68K/NeoGeo-68000
 * 版本: 1.0
 * 日期: 2026-04-16
 * 作者: VML Team
 * 描述: SNK Neo Geo AES main processor - Motorola 68000 @ 12MHz + Z80 @ 4MHz (audio coprocessor)
 * CPU架构: MC68000
 * 位宽: 32位
 * 时钟频率: 12000000 Hz
 *)

PROGRAM NeoGeo_68000
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    D0 AT %M0x00 : DWORD; (* Data Register 0 *)
    D1 AT %M0x04 : DWORD; (* Data Register 1 *)
    D2 AT %M0x08 : DWORD; (* Data Register 2 *)
    D3 AT %M0x0C : DWORD; (* Data Register 3 *)
    D4 AT %M0x10 : DWORD; (* Data Register 4 *)
    D5 AT %M0x14 : DWORD; (* Data Register 5 *)
    D6 AT %M0x18 : DWORD; (* Data Register 6 *)
    D7 AT %M0x1C : DWORD; (* Data Register 7 *)
    A0 AT %M0x20 : DWORD; (* Address Register 0 *)
    A1 AT %M0x24 : DWORD; (* Address Register 1 *)
    A2 AT %M0x28 : DWORD; (* Address Register 2 *)
    A3 AT %M0x2C : DWORD; (* Address Register 3 *)
    A4 AT %M0x30 : DWORD; (* Address Register 4 *)
    A5 AT %M0x34 : DWORD; (* Address Register 5 *)
    A6 AT %M0x38 : DWORD; (* Address Register 6 *)
    A7 AT %M0x3C : DWORD; (* User Stack Pointer (USP) *)
    SP AT %M0x3C : DWORD; (* Supervisor Stack Pointer (SSP) *)
    PC AT %M0x40 : DWORD; (* Program Counter *)
    SR AT %M0x44 : WORD; (* Status Register *)
    SR_C AT %M0x44.0 : BOOL; (* Carry *)
    SR_V AT %M0x44.1 : BOOL; (* Overflow *)
    SR_Z AT %M0x44.2 : BOOL; (* Zero *)
    SR_N AT %M0x44.3 : BOOL; (* Negative *)
    SR_X AT %M0x44.4 : BOOL; (* Extend *)
    SR_I0 AT %M0x44.8 : BOOL; (* Interrupt Mask 0 *)
    SR_I1 AT %M0x44.9 : BOOL; (* Interrupt Mask 1 *)
    SR_I2 AT %M0x44.10 : BOOL; (* Interrupt Mask 2 *)
    SR_M AT %M0x44.11 : BOOL; (* Master/Interrupt *)
    SR_S AT %M0x44.13 : BOOL; (* Supervisor/User *)
    SR_T0 AT %M0x44.14 : BOOL; (* Trace Mode 0 *)
    SR_T1 AT %M0x44.15 : BOOL; (* Trace Mode 1 *)

    (* 外设变量 *)
    (* Z80 Audio Coprocessor @ 4MHz *)
    Z80_BASE AT %MW0x300000 : WORD;
    Z80_Z80_A AT %MW0x00 : BYTE;
    Z80_Z80_F AT %MW0x01 : BYTE;
    Z80_Z80_B AT %MW0x02 : BYTE;
    Z80_Z80_C AT %MW0x03 : BYTE;
    Z80_Z80_D AT %MW0x04 : BYTE;
    Z80_Z80_E AT %MW0x05 : BYTE;
    Z80_Z80_H AT %MW0x06 : BYTE;
    Z80_Z80_L AT %MW0x07 : BYTE;
    Z80_Z80_AF AT %MW0x08 : WORD;
    Z80_Z80_BC AT %MW0x0A : WORD;
    Z80_Z80_DE AT %MW0x0C : WORD;
    Z80_Z80_HL AT %MW0x0E : WORD;
    Z80_Z80_IX AT %MW0x10 : WORD;
    Z80_Z80_IY AT %MW0x12 : WORD;
    Z80_Z80_SP AT %MW0x14 : WORD;
    Z80_Z80_PC AT %MW0x16 : WORD;
    Z80_Z80_I AT %MW0x18 : BYTE;
    Z80_Z80_R AT %MW0x19 : BYTE;
    Z80_Z80_IM AT %MW0x1A : BYTE;
    Z80_Z80_BUSREQ AT %MW0x1E : BYTE;
    Z80_Z80_RESET AT %MW0x1F : BYTE;
    (* Yamaha YM2610 FM + ADPCM Audio Generator *)
    YM2610_BASE AT %MW0x300000 : WORD;
    YM2610_YM_ADDR_A0 AT %MW0x00 : BYTE;
    YM2610_YM_DATA_A0 AT %MW0x01 : BYTE;
    YM2610_YM_ADDR_A1 AT %MW0x02 : BYTE;
    YM2610_YM_DATA_A1 AT %MW0x03 : BYTE;
    YM2610_YM_ADDR_B0 AT %MW0x04 : BYTE;
    YM2610_YM_DATA_B0 AT %MW0x05 : BYTE;
    YM2610_YM_TEST AT %MW0x08 : BYTE;
    YM2610_YM_FM_CH0_FREQ_L AT %MW0xA0 : BYTE;
    YM2610_YM_FM_CH0_FREQ_H AT %MW0xA4 : BYTE;
    YM2610_YM_FM_CH1_FREQ_L AT %MW0xA1 : BYTE;
    YM2610_YM_FM_CH1_FREQ_H AT %MW0xA5 : BYTE;
    YM2610_YM_FM_CH2_FREQ_L AT %MW0xA2 : BYTE;
    YM2610_YM_FM_CH2_FREQ_H AT %MW0xA6 : BYTE;
    YM2610_YM_FM_CH3_FREQ_L AT %MW0xA3 : BYTE;
    YM2610_YM_FM_CH3_FREQ_H AT %MW0xA7 : BYTE;
    YM2610_YM_FM_KEY_ON AT %MW0x28 : BYTE;
    YM2610_YM_FM_CH0_ALG AT %MW0xB0 : BYTE;
    YM2610_YM_FM_CH1_ALG AT %MW0xB1 : BYTE;
    YM2610_YM_FM_CH2_ALG AT %MW0xB2 : BYTE;
    YM2610_YM_FM_CH3_ALG AT %MW0xB3 : BYTE;
    YM2610_YM_FM_TIMER_H AT %MW0x24 : BYTE;
    YM2610_YM_FM_TIMER_L AT %MW0x25 : BYTE;
    YM2610_YM_FM_TIMER_CTRL AT %MW0x27 : BYTE;
    YM2610_YM_FM_TIMER_CTRL_TIMER_A_START AT %MW0x27.0 : BOOL; (* Timer A Start *)
    YM2610_YM_FM_TIMER_CTRL_TIMER_B_START AT %MW0x27.1 : BOOL; (* Timer B Start *)
    YM2610_YM_FM_TIMER_CTRL_LOAD_A AT %MW0x27.2 : BOOL; (* Load Timer A *)
    YM2610_YM_FM_TIMER_CTRL_LOAD_B AT %MW0x27.3 : BOOL; (* Load Timer B *)
    YM2610_YM_FM_TIMER_CTRL_IRQ_EN_A AT %MW0x27.4 : BOOL; (* Timer A IRQ Enable *)
    YM2610_YM_FM_TIMER_CTRL_IRQ_EN_B AT %MW0x27.5 : BOOL; (* Timer B IRQ Enable *)
    YM2610_YM_FM_TIMER_CTRL_CSM_MODE AT %MW0x27.7 : BOOL; (* CSM Mode (auto Key-On after timer A) *)
    YM2610_YM_FM_CH0_DETUNE AT %MW0x30 : BYTE;
    YM2610_YM_FM_CH0_MUL AT %MW0x30 : BYTE;
    YM2610_YM_FM_CH0_TL AT %MW0x40 : BYTE;
    YM2610_YM_FM_CH0_KS_AR AT %MW0x50 : BYTE;
    YM2610_YM_FM_CH0_AM_DR AT %MW0x60 : BYTE;
    YM2610_YM_FM_CH0_SR AT %MW0x70 : BYTE;
    YM2610_YM_FM_CH0_RR_SL AT %MW0x80 : BYTE;
    YM2610_YM_FM_CH0_SSG AT %MW0x90 : BYTE;
    YM2610_YM_SSG_CHA_FREQ_L AT %MW0x00 : BYTE;
    YM2610_YM_SSG_CHA_FREQ_H AT %MW0x01 : BYTE;
    YM2610_YM_SSG_CHB_FREQ_L AT %MW0x02 : BYTE;
    YM2610_YM_SSG_CHB_FREQ_H AT %MW0x03 : BYTE;
    YM2610_YM_SSG_CHC_FREQ_L AT %MW0x04 : BYTE;
    YM2610_YM_SSG_CHC_FREQ_H AT %MW0x05 : BYTE;
    YM2610_YM_SSG_CHA_VOL AT %MW0x08 : BYTE;
    YM2610_YM_SSG_CHB_VOL AT %MW0x09 : BYTE;
    YM2610_YM_SSG_CHC_VOL AT %MW0x0A : BYTE;
    YM2610_YM_SSG_MIXER AT %MW0x07 : BYTE;
    YM2610_YM_SSG_ENV_FREQ_L AT %MW0x0B : BYTE;
    YM2610_YM_SSG_ENV_FREQ_H AT %MW0x0C : BYTE;
    YM2610_YM_SSG_ENV_SHAPE AT %MW0x0D : BYTE;
    YM2610_YM_SSG_IO_A AT %MW0x0E : BYTE;
    YM2610_YM_SSG_IO_B AT %MW0x0F : BYTE;
    YM2610_YM_ADPCM_STATUS AT %MW0x10 : BYTE;
    YM2610_YM_ADPCM_START AT %MW0x11 : BYTE;
    YM2610_YM_ADPCM_END AT %MW0x12 : BYTE;
    YM2610_YM_ADPCM_VOL_L AT %MW0x13 : BYTE;
    YM2610_YM_ADPCM_VOL_R AT %MW0x14 : BYTE;
    YM2610_YM_DELTA_N_L AT %MW0x15 : BYTE;
    YM2610_YM_DELTA_N_H AT %MW0x16 : BYTE;
    YM2610_YM_ADPCM_B_START AT %MW0x18 : BYTE;
    YM2610_YM_ADPCM_B_END AT %MW0x19 : BYTE;
    YM2610_YM_ADPCM_B_VOL AT %MW0x1A : BYTE;
    YM2610_YM_ADPCM_B_CTRL AT %MW0x1B : BYTE;
    (* Neo Geo VDP (Video Display Processor) *)
    YGV628_BASE AT %MW0x3C0000 : WORD;
    YGV628_VRAM_ADDR_L AT %MW0x00 : BYTE;
    YGV628_VRAM_ADDR_H AT %MW0x01 : BYTE;
    YGV628_VRAM_DATA AT %MW0x02 : BYTE;
    YGV628_VRAM_READ AT %MW0x03 : BYTE;
    YGV628_CRAM_ADDR AT %MW0x04 : BYTE;
    YGV628_CRAM_DATA AT %MW0x05 : BYTE;
    YGV628_VDP_STATUS AT %MW0x06 : BYTE;
    YGV628_VDP_STATUS_VBLANK AT %MW0x06.0 : BOOL; (* V-Blank Flag *)
    YGV628_VDP_STATUS_FIELD AT %MW0x06.1 : BOOL; (* Field (0=even, 1=odd for interlace) *)
    YGV628_VDP_STATUS_ODD_FIELD AT %MW0x06.1 : BOOL; (* Odd Field Flag *)
    YGV628_VDP_STATUS_DMA_BUSY AT %MW0x06.2 : BOOL; (* DMA Busy *)
    YGV628_VDP_STATUS_SPRITE_OVERFLOW AT %MW0x06.3 : BOOL; (* Sprite Overflow (more than 16 per line) *)
    YGV628_VDP_STATUS_SPRITE_COLLISION AT %MW0x06.4 : BOOL; (* Sprite Collision *)
    YGV628_VDP_CTRL AT %MW0x07 : BYTE;
    YGV628_VDP_CTRL_VRAM_INC AT %MW0x07.0 : BOOL; (* VRAM Auto-Increment (0=+1, 1=+2) *)
    YGV628_VDP_CTRL_ROW_SCROLL AT %MW0x07.1 : BOOL; (* Row Scroll Mode *)
    YGV628_VDP_CTRL_COL_SCROLL AT %MW0x07.2 : BOOL; (* Column Scroll Mode *)
    YGV628_VDP_CTRL_FIX_DISP AT %MW0x07.3 : BOOL; (* Fix Layer Display *)
    YGV628_VDP_CTRL_SPR_DISP AT %MW0x07.4 : BOOL; (* Sprite Layer Display *)
    YGV628_VDP_CTRL_SCROLL2_DISP AT %MW0x07.5 : BOOL; (* Scroll Layer 2 Display *)
    YGV628_VDP_CTRL_SCROLL1_DISP AT %MW0x07.6 : BOOL; (* Scroll Layer 1 Display *)
    YGV628_VDP_CTRL_DMA_ENABLE AT %MW0x07.7 : BOOL; (* DMA Enable *)
    YGV628_SCROLL1_BASE AT %MW0x08 : WORD;
    YGV628_SCROLL2_BASE AT %MW0x0A : WORD;
    YGV628_SPR_BASE AT %MW0x0C : WORD;
    YGV628_SPR_COUNT AT %MW0x0E : BYTE;
    YGV628_WINDOW_X AT %MW0x10 : BYTE;
    YGV628_WINDOW_Y AT %MW0x11 : BYTE;
    YGV628_WINDOW_W AT %MW0x12 : BYTE;
    YGV628_WINDOW_H AT %MW0x13 : BYTE;
    YGV628_LINE_SCROLL_L AT %MW0x14 : BYTE;
    YGV628_LINE_SCROLL_H AT %MW0x15 : BYTE;
    YGV628_RASTER_COMP AT %MW0x16 : BYTE;
    YGV628_H_TIMING AT %MW0x18 : BYTE;
    YGV628_V_TIMING AT %MW0x19 : BYTE;
    YGV628_DMA_SRC_L AT %MW0x1A : BYTE;
    YGV628_DMA_SRC_H AT %MW0x1B : BYTE;
    YGV628_DMA_SRC_B AT %MW0x1C : BYTE;
    YGV628_DMA_DEST_L AT %MW0x1D : BYTE;
    YGV628_DMA_DEST_H AT %MW0x1E : BYTE;
    YGV628_DMA_COUNT AT %MW0x1F : WORD;
    (* Neo Geo System Driver / Controller *)
    NEODRIVER_BASE AT %MW0x310000 : WORD;
    NEODRIVER_PDI0 AT %MW0x00 : BYTE;
    NEODRIVER_PDI0_UP AT %MW0x00.0 : BOOL; (* Up (0=pressed) *)
    NEODRIVER_PDI0_DOWN AT %MW0x00.1 : BOOL; (* Down (0=pressed) *)
    NEODRIVER_PDI0_LEFT AT %MW0x00.2 : BOOL; (* Left (0=pressed) *)
    NEODRIVER_PDI0_RIGHT AT %MW0x00.3 : BOOL; (* Right (0=pressed) *)
    NEODRIVER_PDI0_A AT %MW0x00.4 : BOOL; (* A Button (0=pressed) *)
    NEODRIVER_PDI0_B AT %MW0x00.5 : BOOL; (* B Button (0=pressed) *)
    NEODRIVER_PDI0_C AT %MW0x00.6 : BOOL; (* C Button (0=pressed) *)
    NEODRIVER_PDI0_D AT %MW0x00.7 : BOOL; (* D Button (0=pressed) *)
    NEODRIVER_PDI1 AT %MW0x01 : BYTE;
    NEODRIVER_PDI2 AT %MW0x02 : BYTE;
    NEODRIVER_PDI3 AT %MW0x03 : BYTE;
    NEODRIVER_PDO0 AT %MW0x04 : BYTE;
    NEODRIVER_PDO1 AT %MW0x05 : BYTE;
    NEODRIVER_PDO2 AT %MW0x06 : BYTE;
    NEODRIVER_PDO3 AT %MW0x07 : BYTE;
    NEODRIVER_DIPSEL1 AT %MW0x08 : BYTE;
    NEODRIVER_DIPSEL1_COIN_SELECT AT %MW0x08.0 : BOOL; (* Coin Select (0=common, 1=1 coin 1 credit) *)
    NEODRIVER_DIPSEL1_FREE_PLAY AT %MW0x08.1 : BOOL; (* Free Play *)
    NEODRIVER_DIPSEL1_DEMO_SOUND AT %MW0x08.2 : BOOL; (* Demo Sound *)
    NEODRIVER_DIPSEL1_CHIP_MODE AT %MW0x08.3 : BOOL; (* Chip Mode (0=AES, 1=MVS) *)
    NEODRIVER_DIPSEL1_CONTROLLER_TYPE AT %MW0x08.4 : BOOL; (* Controller Type (0=standard, 1=keyboard) *)
    NEODRIVER_DIPSEL2 AT %MW0x09 : BYTE;
    NEODRIVER_DIPSEL3 AT %MW0x0A : BYTE;
    NEODRIVER_DIPSEL4 AT %MW0x0B : BYTE;
    NEODRIVER_SYSCTRL AT %MW0x0C : BYTE;
    NEODRIVER_SYSCTRL_RTSEL AT %MW0x0C.0 : BOOL; (* Real Time Switch Select *)
    NEODRIVER_SYSCTRL_RESERVED0 AT %MW0x0C.1 : BOOL; (* Reserved *)
    NEODRIVER_SYSCTRL_SCC AT %MW0x0C.2 : BOOL; (* System Clock Control *)
    NEODRIVER_SYSCTRL_PHEN AT %MW0x0C.3 : BOOL; (* PHEN (bus timing) *)
    NEODRIVER_SYSCTRL_PCK2 AT %MW0x0C.4 : BOOL; (* PCK2 (bus timing) *)
    NEODRIVER_SYSCTRL_PCK1 AT %MW0x0C.5 : BOOL; (* PCK1 (bus timing) *)
    NEODRIVER_SYSCTRL_CKDIV2 AT %MW0x0C.6 : BOOL; (* Clock Divide by 2 *)
    NEODRIVER_SYSCTRL_FEFIX AT %MW0x0C.7 : BOOL; (* FE Fix *)
    NEODRIVER_IRQMASK AT %MW0x0D : BYTE;
    NEODRIVER_IRQMASK_VBLANK_MASK AT %MW0x0D.0 : BOOL; (* V-Blank Interrupt Mask *)
    NEODRIVER_IRQMASK_HBLANK_MASK AT %MW0x0D.1 : BOOL; (* H-Blank Interrupt Mask *)
    NEODRIVER_IRQMASK_VECTOR_IN_MASK AT %MW0x0D.2 : BOOL; (* Vector In (from Z80) Mask *)
    NEODRIVER_IRQMASK_SYSTEM_IN_MASK AT %MW0x0D.3 : BOOL; (* System Input (JAMMA) Mask *)
    NEODRIVER_IRQFLAG AT %MW0x0E : BYTE;
    NEODRIVER_SECAM_MODE AT %MW0x0F : BYTE;
    (* Controller Port 1 *)
    CONTROLLER1_BASE AT %MW0x310000 : WORD;
    CONTROLLER1_PDI0 AT %MW0x00 : BYTE;
    (* Controller Port 2 *)
    CONTROLLER2_BASE AT %MW0x310001 : WORD;
    CONTROLLER2_PDI1 AT %MW0x00 : BYTE;
    (* Memory Card Interface *)
    MEMORY_CARD_BASE AT %MW0x320000 : WORD;
    MEMORY_CARD_CARD_DATA AT %MW0x00 : BYTE;
    MEMORY_CARD_CARD_STATUS AT %MW0x01 : BYTE;
    MEMORY_CARD_CARD_STATUS_INSERTED AT %MW0x01.0 : BOOL; (* Card Inserted (0=yes) *)
    MEMORY_CARD_CARD_STATUS_WRITE_PROTECT AT %MW0x01.1 : BOOL; (* Write Protected (0=yes) *)
    MEMORY_CARD_CARD_STATUS_READY AT %MW0x01.2 : BOOL; (* Ready for I/O *)
    MEMORY_CARD_CARD_CTRL AT %MW0x02 : BYTE;
    (* Cartridge Bank Switching *)
    CART_BANK_BASE AT %MW0x2FFFF0 : WORD;
    CART_BANK_BANK_REG AT %MW0x00 : BYTE;

    (* 中断变量 *)
    INT_RESET_SP : BOOL; (* Reset Initial Stack Pointer *)
    INT_RESET_PC : BOOL; (* Reset Initial PC *)
    INT_BUS_ERROR : BOOL; (* Bus Error *)
    INT_ADDRESS_ERROR : BOOL; (* Address Error *)
    INT_ILLEGAL_INSTR : BOOL; (* Illegal Instruction *)
    INT_ZERO_DIVIDE : BOOL; (* Zero Divide *)
    INT_CHK_EXCEPTION : BOOL; (* CHK Exception *)
    INT_TRAPV : BOOL; (* TRAPV Exception *)
    INT_PRIVILEGE : BOOL; (* Privilege Violation *)
    INT_TRACE : BOOL; (* Trace *)
    INT_LINE_A : BOOL; (* Line 1010 Emulator *)
    INT_LINE_F : BOOL; (* Line 1111 Emulator *)
    INT_IRQ1 : BOOL; (* H-Blank / VDP Interrupt (raster) *)
    INT_IRQ2 : BOOL; (* V-Blank / Frame End Interrupt *)
    INT_IRQ3 : BOOL; (* System Controller / Z80 Vector In *)
    INT_IRQ4 : BOOL; (* JAMMA / System Input *)
    INT_IRQ5 : BOOL; (* Z80 Interrupt Request *)
    INT_TRAP0 : BOOL; (* TRAP #0 (system call) *)
    INT_TRAP1 : BOOL; (* TRAP #1 *)

END_VAR

BEGIN
    (* NeoGeo-68000初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|D0>0|--(监控灯)--
    --|D1>0|--(监控灯)--
    --|D2>0|--(监控灯)--
    --|D3>0|--(监控灯)--
    --|D4>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_RESET_SP|--[中断处理]--
    --|INT_RESET_PC|--[中断处理]--
    --|INT_BUS_ERROR|--[中断处理]--
    --|INT_ADDRESS_ERROR|--[中断处理]--
    --|INT_ILLEGAL_INSTR|--[中断处理]--
    --|INT_ZERO_DIVIDE|--[中断处理]--
    --|INT_CHK_EXCEPTION|--[中断处理]--
    --|INT_TRAPV|--[中断处理]--
    --|INT_PRIVILEGE|--[中断处理]--
    --|INT_TRACE|--[中断处理]--
    --|INT_LINE_A|--[中断处理]--
    --|INT_LINE_F|--[中断处理]--
    --|INT_IRQ1|--[中断处理]--
    --|INT_IRQ2|--[中断处理]--
    --|INT_IRQ3|--[中断处理]--
    --|INT_IRQ4|--[中断处理]--
    --|INT_IRQ5|--[中断处理]--
    --|INT_TRAP0|--[中断处理]--
    --|INT_TRAP1|--[中断处理]--

END_PROGRAM
