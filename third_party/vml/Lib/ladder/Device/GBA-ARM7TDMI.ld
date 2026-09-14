(*
 * ARM7TDMI梯形图定义
 * 生成自: ARM/ARM7/ARM7TDMI
 * 版本: 1.0
 * 日期: 2026-04-16
 * 作者: VML Team
 * 描述: Game Boy Advance main processor - ARM7TDMI @ 16.78MHz with 32-bit ARM + 16-bit Thumb instruction sets
 * CPU架构: ARM7TDMI
 * 位宽: 32位
 * 时钟频率: 16780000 Hz
 *)

PROGRAM ARM7TDMI
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    R0 AT %M0x00 : DWORD; (* General Purpose Register 0 *)
    R1 AT %M0x04 : DWORD; (* General Purpose Register 1 *)
    R2 AT %M0x08 : DWORD; (* General Purpose Register 2 *)
    R3 AT %M0x0C : DWORD; (* General Purpose Register 3 *)
    R4 AT %M0x10 : DWORD; (* General Purpose Register 4 *)
    R5 AT %M0x14 : DWORD; (* General Purpose Register 5 *)
    R6 AT %M0x18 : DWORD; (* General Purpose Register 6 *)
    R7 AT %M0x1C : DWORD; (* General Purpose Register 7 *)
    R8 AT %M0x20 : DWORD; (* General Purpose Register 8 *)
    R9 AT %M0x24 : DWORD; (* General Purpose Register 9 / SB *)
    R10 AT %M0x28 : DWORD; (* General Purpose Register 10 / SL *)
    R11 AT %M0x2C : DWORD; (* Frame Pointer / FP *)
    R12 AT %M0x30 : DWORD; (* Intra-Procedure-call Scratch Register / IP *)
    R13 AT %M0x34 : DWORD; (* Stack Pointer / SP *)
    R14 AT %M0x38 : DWORD; (* Link Register / LR *)
    R15 AT %M0x3C : DWORD; (* Program Counter / PC *)
    CPSR AT %M0x40 : DWORD; (* Current Program Status Register *)
    CPSR_MODE AT %M0x40.0 : BOOL; (* Processor Mode (10000=User, 10001=FIQ, 10010=IRQ, 10011=SVC, 10111=ABT, 11011=UND, 11111=SYS) *)
    CPSR_T AT %M0x40.5 : BOOL; (* Thumb State Bit (1=Thumb mode) *)
    CPSR_F AT %M0x40.6 : BOOL; (* FIQ Disable *)
    CPSR_I AT %M0x40.7 : BOOL; (* IRQ Disable *)
    CPSR_A AT %M0x40.8 : BOOL; (* Imprecise Data Abort Disable *)
    CPSR_E AT %M0x40.9 : BOOL; (* Endianness (0=Little) *)
    CPSR_GE AT %M0x40.0 : BOOL; (* Greater-than-or-Equal flags *)
    CPSR_N AT %M0x40.31 : BOOL; (* Negative *)
    CPSR_Z AT %M0x40.30 : BOOL; (* Zero *)
    CPSR_C AT %M0x40.29 : BOOL; (* Carry *)
    CPSR_V AT %M0x40.28 : BOOL; (* Overflow *)
    SPSR_SVC AT %M0x44 : DWORD; (* Saved PSR (Supervisor Mode) *)
    SPSR_ABT AT %M0x48 : DWORD; (* Saved PSR (Abort Mode) *)
    SPSR_IRQ AT %M0x4C : DWORD; (* Saved PSR (IRQ Mode) *)
    SPSR_FIQ AT %M0x50 : DWORD; (* Saved PSR (FIQ Mode) *)

    (* 外设变量 *)
    (* LCD Controller *)
    LCD_BASE AT %MW0x04000000 : WORD;
    LCD_DISPCNT AT %MW0x04000000 : WORD;
    LCD_DISPCNT_BG_MODE AT %MW0x04000000.0 : BOOL; (* BG Mode (0-6) *)
    LCD_DISPCNT_GB_WINDOW AT %MW0x04000000.5 : BOOL; (* Game Boy Window Enable *)
    LCD_DISPCNT_WIN0_ENABLE AT %MW0x04000000.13 : BOOL; (* Window 0 Enable *)
    LCD_DISPCNT_WIN1_ENABLE AT %MW0x04000000.14 : BOOL; (* Window 1 Enable *)
    LCD_DISPCNT_OBJ_WIN AT %MW0x04000000.15 : BOOL; (* Object Window Enable *)
    LCD_DISPCNT_BG0_ENABLE AT %MW0x04000000.8 : BOOL; (* BG0 Enable *)
    LCD_DISPCNT_BG1_ENABLE AT %MW0x04000000.9 : BOOL; (* BG1 Enable *)
    LCD_DISPCNT_BG2_ENABLE AT %MW0x04000000.10 : BOOL; (* BG2 Enable *)
    LCD_DISPCNT_BG3_ENABLE AT %MW0x04000000.11 : BOOL; (* BG3 Enable *)
    LCD_DISPCNT_OBJ_ENABLE AT %MW0x04000000.12 : BOOL; (* Object/Sprite Enable *)
    LCD_GREEN_SWAP AT %MW0x04000002 : BYTE;
    LCD_DISPSTAT AT %MW0x04000004 : WORD;
    LCD_DISPSTAT_V_COUNT AT %MW0x04000004.0 : BOOL; (* Vertical Line Counter *)
    LCD_DISPSTAT_VBLANK_FLAG AT %MW0x04000004.0 : BOOL; (* V-Blank Flag (read-only) *)
    LCD_DISPSTAT_HBLANK_FLAG AT %MW0x04000004.1 : BOOL; (* H-Blank Flag (read-only) *)
    LCD_DISPSTAT_V_COUNT_FLAG AT %MW0x04000004.2 : BOOL; (* V-Count Flag (LY==LYC) *)
    LCD_DISPSTAT_VBLANK_IRQ AT %MW0x04000004.3 : BOOL; (* V-Blank IRQ Enable *)
    LCD_DISPSTAT_HBLANK_IRQ AT %MW0x04000004.4 : BOOL; (* H-Blank IRQ Enable *)
    LCD_DISPSTAT_VCOUNT_IRQ AT %MW0x04000004.5 : BOOL; (* V-Count IRQ Enable *)
    LCD_VCOUNT AT %MW0x04000006 : WORD;
    LCD_BG0CNT AT %MW0x04000008 : WORD;
    LCD_BG1CNT AT %MW0x0400000A : WORD;
    LCD_BG2CNT AT %MW0x0400000C : WORD;
    LCD_BG3CNT AT %MW0x0400000E : WORD;
    LCD_BG0HOFS AT %MW0x04000010 : WORD;
    LCD_BG0VOFS AT %MW0x04000012 : WORD;
    LCD_BG1HOFS AT %MW0x04000014 : WORD;
    LCD_BG1VOFS AT %MW0x04000016 : WORD;
    LCD_BG2HOFS AT %MW0x04000018 : WORD;
    LCD_BG2VOFS AT %MW0x0400001A : WORD;
    LCD_BG3HOFS AT %MW0x0400001C : WORD;
    LCD_BG3VOFS AT %MW0x0400001E : WORD;
    LCD_BG2PA AT %MW0x04000020 : WORD;
    LCD_BG2PB AT %MW0x04000022 : WORD;
    LCD_BG2PC AT %MW0x04000024 : WORD;
    LCD_BG2PD AT %MW0x04000026 : WORD;
    LCD_BG2X AT %MW0x04000028 : DWORD;
    LCD_BG2Y AT %MW0x0400002C : DWORD;
    LCD_BG3PA AT %MW0x04000030 : WORD;
    LCD_BG3PB AT %MW0x04000032 : WORD;
    LCD_BG3PC AT %MW0x04000034 : WORD;
    LCD_BG3PD AT %MW0x04000036 : WORD;
    LCD_BG3X AT %MW0x04000038 : DWORD;
    LCD_BG3Y AT %MW0x0400003C : DWORD;
    LCD_WIN0H AT %MW0x04000040 : WORD;
    LCD_WIN1H AT %MW0x04000042 : WORD;
    LCD_WIN0V AT %MW0x04000044 : WORD;
    LCD_WIN1V AT %MW0x04000046 : WORD;
    LCD_WININ AT %MW0x04000048 : BYTE;
    LCD_WINOUT AT %MW0x04000049 : BYTE;
    LCD_MOSAIC AT %MW0x0400004C : WORD;
    LCD_BLDCNT AT %MW0x04000050 : WORD;
    LCD_BLDCNT_BG1ST AT %MW0x04000050.0 : BOOL; (* BG1 1st Target *)
    LCD_BLDCNT_BG2ST AT %MW0x04000050.1 : BOOL; (* BG2 1st Target *)
    LCD_BLDCNT_BG3ST AT %MW0x04000050.2 : BOOL; (* BG3 1st Target *)
    LCD_BLDCNT_OBJST AT %MW0x04000050.3 : BOOL; (* Object 1st Target *)
    LCD_BLDCNT_BDST AT %MW0x04000050.4 : BOOL; (* Backdrop 1st Target *)
    LCD_BLDCNT_BLEND_MODE AT %MW0x04000050.0 : BOOL; (* Blend Mode (0=None, 1=Alpha, 2=Increase, 3=Decrease) *)
    LCD_BLDCNT_BG1ST2 AT %MW0x04000050.8 : BOOL; (* BG1 2nd Target *)
    LCD_BLDCNT_BG2ST2 AT %MW0x04000050.9 : BOOL; (* BG2 2nd Target *)
    LCD_BLDCNT_BG3ST2 AT %MW0x04000050.10 : BOOL; (* BG3 2nd Target *)
    LCD_BLDCNT_OBJST2 AT %MW0x04000050.11 : BOOL; (* Object 2nd Target *)
    LCD_BLDCNT_BDST2 AT %MW0x04000050.12 : BOOL; (* Backdrop 2nd Target *)
    LCD_BLDALPHA AT %MW0x04000052 : WORD;
    LCD_BLDY AT %MW0x04000054 : BYTE;
    (* Direct Memory Access Controller *)
    DMA_BASE AT %MW0x040000B0 : WORD;
    DMA_DMA0SAD AT %MW0x040000B0 : DWORD;
    DMA_DMA0DAD AT %MW0x040000B4 : DWORD;
    DMA_DMA0CNT_L AT %MW0x040000B8 : WORD;
    DMA_DMA0CNT_H AT %MW0x040000BA : WORD;
    DMA_DMA0CNT_H_TRANSFER_COUNT AT %MW0x040000BA.0 : BOOL; (* Number of Transfers *)
    DMA_DMA0CNT_H_DEST_ADD_MODE AT %MW0x040000BA.0 : BOOL; (* Dest Address Control (0=fix, 1=inc, 2=dec, 3=inc+reload) *)
    DMA_DMA0CNT_H_SRC_ADD_MODE AT %MW0x040000BA.0 : BOOL; (* Source Address Control (0=fix, 1=inc, 2=dec) *)
    DMA_DMA0CNT_H_REPEAT AT %MW0x040000BA.18 : BOOL; (* Repeat (for 16-bit repeat mode) *)
    DMA_DMA0CNT_H_WORD_SIZE AT %MW0x040000BA.20 : BOOL; (* Word Size (0=16-bit, 1=32-bit) *)
    DMA_DMA0CNT_H_DRQ AT %MW0x040000BA.27 : BOOL; (* DRQ Trigger (DMA from external source) *)
    DMA_DMA0CNT_H_TIMING AT %MW0x040000BA.0 : BOOL; (* Start Timing (0=Now, 1=V-Blank, 2=H-Blank, 3=Special) *)
    DMA_DMA0CNT_H_ENABLE AT %MW0x040000BA.31 : BOOL; (* DMA Enable *)
    DMA_DMA1SAD AT %MW0x040000BC : DWORD;
    DMA_DMA1DAD AT %MW0x040000C0 : DWORD;
    DMA_DMA1CNT_L AT %MW0x040000C4 : WORD;
    DMA_DMA1CNT_H AT %MW0x040000C6 : WORD;
    DMA_DMA2SAD AT %MW0x040000C8 : DWORD;
    DMA_DMA2DAD AT %MW0x040000CC : DWORD;
    DMA_DMA2CNT_L AT %MW0x040000D0 : WORD;
    DMA_DMA2CNT_H AT %MW0x040000D2 : WORD;
    DMA_DMA3SAD AT %MW0x040000D4 : DWORD;
    DMA_DMA3DAD AT %MW0x040000D8 : DWORD;
    DMA_DMA3CNT_L AT %MW0x040000DC : WORD;
    DMA_DMA3CNT_H AT %MW0x040000DE : WORD;
    (* Timer Units (4 timers) *)
    TIMER_BASE AT %MW0x04000100 : WORD;
    TIMER_TM0CNT_L AT %MW0x04000100 : WORD;
    TIMER_TM0CNT_H AT %MW0x04000102 : WORD;
    TIMER_TM0CNT_H_PRESCALER AT %MW0x04000102.0 : BOOL; (* Prescaler (0=1, 1=64, 2=256, 3=1024) *)
    TIMER_TM0CNT_H_COUNT_UP AT %MW0x04000102.2 : BOOL; (* Count Up (cascade mode) *)
    TIMER_TM0CNT_H_IRQ_ENABLE AT %MW0x04000102.6 : BOOL; (* Timer IRQ Enable *)
    TIMER_TM0CNT_H_ENABLE AT %MW0x04000102.7 : BOOL; (* Timer Enable *)
    TIMER_TM1CNT_L AT %MW0x04000104 : WORD;
    TIMER_TM1CNT_H AT %MW0x04000106 : WORD;
    TIMER_TM2CNT_L AT %MW0x04000108 : WORD;
    TIMER_TM2CNT_H AT %MW0x0400010A : WORD;
    TIMER_TM3CNT_L AT %MW0x0400010C : WORD;
    TIMER_TM3CNT_H AT %MW0x0400010E : WORD;
    (* Serial I/O (JOY BUS / Link Cable) *)
    SIO_BASE AT %MW0x04000120 : WORD;
    SIO_SIOCNT AT %MW0x04000120 : WORD;
    SIO_SIOCNT_CLOCK_SEL AT %MW0x04000120.0 : BOOL; (* Baud Rate Clock (0=9600, 1=57600, 2=115200, 3=768000) *)
    SIO_SIOCNT_SO_ENABLE AT %MW0x04000120.3 : BOOL; (* SO Output Enable *)
    SIO_SIOCNT_RECV_ENABLE AT %MW0x04000120.5 : BOOL; (* Receive Enable *)
    SIO_SIOCNT_SEND_ENABLE AT %MW0x04000120.6 : BOOL; (* Send Enable *)
    SIO_SIOCNT_START_BIT AT %MW0x04000120.7 : BOOL; (* Start Transfer *)
    SIO_SIODATA8 AT %MW0x0400012A : BYTE;
    SIO_JOYCNT AT %MW0x04000130 : WORD;
    SIO_JOYSTAT AT %MW0x04000134 : WORD;
    SIO_JOY_RECV AT %MW0x04000150 : DWORD;
    SIO_JOY_TRANS AT %MW0x04000154 : DWORD;
    (* Key Input *)
    KEYINPUT_BASE AT %MW0x04000130 : WORD;
    KEYINPUT_KEYINPUT AT %MW0x04000130 : WORD;
    KEYINPUT_KEYINPUT_A AT %MW0x04000130.0 : BOOL; (* A Button (0=Pressed) *)
    KEYINPUT_KEYINPUT_B AT %MW0x04000130.1 : BOOL; (* B Button (0=Pressed) *)
    KEYINPUT_KEYINPUT_SELECT AT %MW0x04000130.2 : BOOL; (* Select Button (0=Pressed) *)
    KEYINPUT_KEYINPUT_START AT %MW0x04000130.3 : BOOL; (* Start Button (0=Pressed) *)
    KEYINPUT_KEYINPUT_RIGHT AT %MW0x04000130.4 : BOOL; (* D-Pad Right (0=Pressed) *)
    KEYINPUT_KEYINPUT_LEFT AT %MW0x04000130.5 : BOOL; (* D-Pad Left (0=Pressed) *)
    KEYINPUT_KEYINPUT_UP AT %MW0x04000130.6 : BOOL; (* D-Pad Up (0=Pressed) *)
    KEYINPUT_KEYINPUT_DOWN AT %MW0x04000130.7 : BOOL; (* D-Pad Down (0=Pressed) *)
    KEYINPUT_KEYINPUT_R AT %MW0x04000130.8 : BOOL; (* R Shoulder Button (0=Pressed) *)
    KEYINPUT_KEYINPUT_L AT %MW0x04000130.9 : BOOL; (* L Shoulder Button (0=Pressed) *)
    KEYINPUT_KEYCNT AT %MW0x04000132 : WORD;
    KEYINPUT_KEYCNT_KEY_MASK AT %MW0x04000132.0 : BOOL; (* Key Interrupt Enable Mask *)
    KEYINPUT_KEYCNT_IRQ_ENABLE AT %MW0x04000132.14 : BOOL; (* Key Interrupt Enable *)
    (* Interrupt Control *)
    INTERRUPT_BASE AT %MW0x04000200 : WORD;
    INTERRUPT_IME AT %MW0x04000208 : DWORD;
    INTERRUPT_IE AT %MW0x04000210 : DWORD;
    INTERRUPT_IE_VBLANK AT %MW0x04000210.0 : BOOL; (* V-Blank Interrupt Enable *)
    INTERRUPT_IE_HBLANK AT %MW0x04000210.1 : BOOL; (* H-Blank Interrupt Enable *)
    INTERRUPT_IE_VCOUNT AT %MW0x04000210.2 : BOOL; (* V-Count Match Interrupt Enable *)
    INTERRUPT_IE_TIMER0 AT %MW0x04000210.3 : BOOL; (* Timer 0 Interrupt Enable *)
    INTERRUPT_IE_TIMER1 AT %MW0x04000210.4 : BOOL; (* Timer 1 Interrupt Enable *)
    INTERRUPT_IE_TIMER2 AT %MW0x04000210.5 : BOOL; (* Timer 2 Interrupt Enable *)
    INTERRUPT_IE_TIMER3 AT %MW0x04000210.6 : BOOL; (* Timer 3 Interrupt Enable *)
    INTERRUPT_IE_SIO AT %MW0x04000210.7 : BOOL; (* Serial I/O Interrupt Enable *)
    INTERRUPT_IE_DMA0 AT %MW0x04000210.8 : BOOL; (* DMA 0 Interrupt Enable *)
    INTERRUPT_IE_DMA1 AT %MW0x04000210.9 : BOOL; (* DMA 1 Interrupt Enable *)
    INTERRUPT_IE_DMA2 AT %MW0x04000210.10 : BOOL; (* DMA 2 Interrupt Enable *)
    INTERRUPT_IE_DMA3 AT %MW0x04000210.11 : BOOL; (* DMA 3 Interrupt Enable *)
    INTERRUPT_IE_KEYPAD AT %MW0x04000210.12 : BOOL; (* Keypad Interrupt Enable *)
    INTERRUPT_IE_CART AT %MW0x04000210.13 : BOOL; (* Game Pak Interrupt Enable *)
    INTERRUPT_IF AT %MW0x04000214 : DWORD;
    (* Waitstate Control *)
    WAITCNT_BASE AT %MW0x04000204 : WORD;
    WAITCNT_WAITCNT AT %MW0x04000204 : WORD;
    WAITCNT_WAITCNT_PHI_OD AT %MW0x04000204.0 : BOOL; (* PHI Terminal Output (0=Disable) *)
    WAITCNT_WAITCNT_SRAM_WS AT %MW0x04000204.0 : BOOL; (* SRAM Wait State (0=4, 1=3, 2=2, 3=8 cycles) *)
    WAITCNT_WAITCNT_WS0_N AT %MW0x04000204.0 : BOOL; (* Wait State 0 (ROM/SRAM 1st access) *)
    WAITCNT_WAITCNT_WS0_S AT %MW0x04000204.5 : BOOL; (* Wait State 0 (ROM/SRAM 2nd access) *)
    WAITCNT_WAITCNT_WS1_N AT %MW0x04000204.0 : BOOL; (* Wait State 1 (ROM 2nd access) *)
    WAITCNT_WAITCNT_WS1_S AT %MW0x04000204.8 : BOOL; (* Wait State 1 (ROM 2nd access short) *)
    WAITCNT_WAITCNT_WS2_N AT %MW0x04000204.0 : BOOL; (* Wait State 2 (ROM 3rd access) *)
    WAITCNT_WAITCNT_WS2_S AT %MW0x04000204.11 : BOOL; (* Wait State 2 (ROM 3rd access short) *)
    WAITCNT_WAITCNT_PREFE AT %MW0x04000204.12 : BOOL; (* Prefetch Enable (GBA SP only) *)

    (* 中断变量 *)
    INT_VBLANK : BOOL; (* V-Blank Interrupt *)
    INT_HBLANK : BOOL; (* H-Blank Interrupt *)
    INT_VCOUNT : BOOL; (* V-Count Match Interrupt *)
    INT_TIMER0 : BOOL; (* Timer 0 Overflow Interrupt *)
    INT_TIMER1 : BOOL; (* Timer 1 Overflow Interrupt *)
    INT_TIMER2 : BOOL; (* Timer 2 Overflow Interrupt *)
    INT_TIMER3 : BOOL; (* Timer 3 Overflow Interrupt *)
    INT_SIO : BOOL; (* Serial I/O Interrupt *)
    INT_DMA0 : BOOL; (* DMA 0 Complete Interrupt *)
    INT_DMA1 : BOOL; (* DMA 1 Complete Interrupt *)
    INT_DMA2 : BOOL; (* DMA 2 Complete Interrupt *)
    INT_DMA3 : BOOL; (* DMA 3 Complete Interrupt *)
    INT_KEYPAD : BOOL; (* Keypad Interrupt *)
    INT_CART : BOOL; (* Game Pak Interrupt *)

END_VAR

BEGIN
    (* ARM7TDMI初始化梯形图 *)
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
    --|INT_HBLANK|--[中断处理]--
    --|INT_VCOUNT|--[中断处理]--
    --|INT_TIMER0|--[中断处理]--
    --|INT_TIMER1|--[中断处理]--
    --|INT_TIMER2|--[中断处理]--
    --|INT_TIMER3|--[中断处理]--
    --|INT_SIO|--[中断处理]--
    --|INT_DMA0|--[中断处理]--
    --|INT_DMA1|--[中断处理]--
    --|INT_DMA2|--[中断处理]--
    --|INT_DMA3|--[中断处理]--
    --|INT_KEYPAD|--[中断处理]--
    --|INT_CART|--[中断处理]--

END_PROGRAM
