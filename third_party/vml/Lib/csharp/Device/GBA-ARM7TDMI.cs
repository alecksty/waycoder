using System;

namespace VML.Device.ARM.ARM7TDMI
{
    /// <summary>
    /// ARM7TDMI 寄存器定义
    /// 生成自: ARM/ARM7/ARM7TDMI
    /// 版本: 1.0
    /// </summary>
    public static class ARM7TDMI
    {
        // CPU架构: ARM7TDMI, 32位, 16780000 Hz

        // 寄存器定义
        // General Purpose Register 0
        public const int R0_ADDR = 0x00;
        public static unsafe uint* R0 => (uint*)0x00;

        // General Purpose Register 1
        public const int R1_ADDR = 0x04;
        public static unsafe uint* R1 => (uint*)0x04;

        // General Purpose Register 2
        public const int R2_ADDR = 0x08;
        public static unsafe uint* R2 => (uint*)0x08;

        // General Purpose Register 3
        public const int R3_ADDR = 0x0C;
        public static unsafe uint* R3 => (uint*)0x0C;

        // General Purpose Register 4
        public const int R4_ADDR = 0x10;
        public static unsafe uint* R4 => (uint*)0x10;

        // General Purpose Register 5
        public const int R5_ADDR = 0x14;
        public static unsafe uint* R5 => (uint*)0x14;

        // General Purpose Register 6
        public const int R6_ADDR = 0x18;
        public static unsafe uint* R6 => (uint*)0x18;

        // General Purpose Register 7
        public const int R7_ADDR = 0x1C;
        public static unsafe uint* R7 => (uint*)0x1C;

        // General Purpose Register 8
        public const int R8_ADDR = 0x20;
        public static unsafe uint* R8 => (uint*)0x20;

        // General Purpose Register 9 / SB
        public const int R9_ADDR = 0x24;
        public static unsafe uint* R9 => (uint*)0x24;

        // General Purpose Register 10 / SL
        public const int R10_ADDR = 0x28;
        public static unsafe uint* R10 => (uint*)0x28;

        // Frame Pointer / FP
        public const int R11_ADDR = 0x2C;
        public static unsafe uint* R11 => (uint*)0x2C;

        // Intra-Procedure-call Scratch Register / IP
        public const int R12_ADDR = 0x30;
        public static unsafe uint* R12 => (uint*)0x30;

        // Stack Pointer / SP
        public const int R13_ADDR = 0x34;
        public static unsafe uint* R13 => (uint*)0x34;

        // Link Register / LR
        public const int R14_ADDR = 0x38;
        public static unsafe uint* R14 => (uint*)0x38;

        // Program Counter / PC
        public const int R15_ADDR = 0x3C;
        public static unsafe uint* R15 => (uint*)0x3C;

        // Current Program Status Register
        public const int CPSR_ADDR = 0x40;
        public static unsafe uint* CPSR => (uint*)0x40;
        public const int CPSR_MODE = 0;  // Processor Mode (10000=User, 10001=FIQ, 10010=IRQ, 10011=SVC, 10111=ABT, 11011=UND, 11111=SYS)
        public const int CPSR_T = 5;  // Thumb State Bit (1=Thumb mode)
        public const int CPSR_F = 6;  // FIQ Disable
        public const int CPSR_I = 7;  // IRQ Disable
        public const int CPSR_A = 8;  // Imprecise Data Abort Disable
        public const int CPSR_E = 9;  // Endianness (0=Little)
        public const int CPSR_GE = 0;  // Greater-than-or-Equal flags
        public const int CPSR_N = 31;  // Negative
        public const int CPSR_Z = 30;  // Zero
        public const int CPSR_C = 29;  // Carry
        public const int CPSR_V = 28;  // Overflow

        // Saved PSR (Supervisor Mode)
        public const int SPSR_SVC_ADDR = 0x44;
        public static unsafe uint* SPSR_SVC => (uint*)0x44;

        // Saved PSR (Abort Mode)
        public const int SPSR_ABT_ADDR = 0x48;
        public static unsafe uint* SPSR_ABT => (uint*)0x48;

        // Saved PSR (IRQ Mode)
        public const int SPSR_IRQ_ADDR = 0x4C;
        public static unsafe uint* SPSR_IRQ => (uint*)0x4C;

        // Saved PSR (FIQ Mode)
        public const int SPSR_FIQ_ADDR = 0x50;
        public static unsafe uint* SPSR_FIQ => (uint*)0x50;

        // 内存段定义
        // Internal Work RAM (32KB, 2-cycle access)
        public const int IWRAM_START = 0x03000000;
        public const int IWRAM_END = 0x03007FFF;
        public const int IWRAM_SIZE = 32768;

        // Internal Work RAM Fast (high-speed region)
        public const int IWRAM_FAST_START = 0x03008000;
        public const int IWRAM_FAST_END = 0x03FFFFFF;
        public const int IWRAM_FAST_SIZE = 32752;

        // Video RAM (96KB + 64KB OBJ VRAM)
        public const int VRAM_START = 0x06000000;
        public const int VRAM_END = 0x06017FFF;
        public const int VRAM_SIZE = 98304;

        // BG Palette RAM (256 colors x 2 bytes)
        public const int PALETTE_START = 0x05000200;
        public const int PALETTE_END = 0x050003FF;
        public const int PALETTE_SIZE = 512;

        // Object Palette RAM
        public const int OBJ_PALETTE_START = 0x05000400;
        public const int OBJ_PALETTE_END = 0x050005FF;
        public const int OBJ_PALETTE_SIZE = 512;

        // Object Attribute Memory (OAM, 128 sprites)
        public const int OAM_START = 0x07000000;
        public const int OAM_END = 0x070003FF;
        public const int OAM_SIZE = 1024;

        // Cartridge ROM (max 32MB)
        public const int ROM_START = 0x08000000;
        public const int ROM_END = 0x09FFFFFF;
        public const int ROM_SIZE = 33554432;

        // Cartridge SRAM / Flash
        public const int CART_RAM_START = 0x0E000000;
        public const int CART_RAM_END = 0x0E00FFFF;
        public const int CART_RAM_SIZE = 65536;

        // GBA BIOS (16KB)
        public const int BIOS_START = 0x00000000;
        public const int BIOS_END = 0x00003FFF;
        public const int BIOS_SIZE = 16384;

        // I/O Registers (MMIO)
        public const int IO_REGS_START = 0x04000000;
        public const int IO_REGS_END = 0x04FFFFFF;
        public const int IO_REGS_SIZE = 16777216;

        // 外设定义
        // LCD Controller
        public const int LCD_BASE = 0x04000000;
        public static unsafe ushort* LCD_DISPCNT => (ushort*)0x08000000;
        public const int LCD_DISPCNT_BG_MODE = 0;  // BG Mode (0-6)
        public const int LCD_DISPCNT_GB_WINDOW = 5;  // Game Boy Window Enable
        public const int LCD_DISPCNT_WIN0_ENABLE = 13;  // Window 0 Enable
        public const int LCD_DISPCNT_WIN1_ENABLE = 14;  // Window 1 Enable
        public const int LCD_DISPCNT_OBJ_WIN = 15;  // Object Window Enable
        public const int LCD_DISPCNT_BG0_ENABLE = 8;  // BG0 Enable
        public const int LCD_DISPCNT_BG1_ENABLE = 9;  // BG1 Enable
        public const int LCD_DISPCNT_BG2_ENABLE = 10;  // BG2 Enable
        public const int LCD_DISPCNT_BG3_ENABLE = 11;  // BG3 Enable
        public const int LCD_DISPCNT_OBJ_ENABLE = 12;  // Object/Sprite Enable
        public static unsafe byte* LCD_GREEN_SWAP => (byte*)0x08000002;
        public static unsafe ushort* LCD_DISPSTAT => (ushort*)0x08000004;
        public const int LCD_DISPSTAT_V_COUNT = 0;  // Vertical Line Counter
        public const int LCD_DISPSTAT_VBLANK_FLAG = 0;  // V-Blank Flag (read-only)
        public const int LCD_DISPSTAT_HBLANK_FLAG = 1;  // H-Blank Flag (read-only)
        public const int LCD_DISPSTAT_V_COUNT_FLAG = 2;  // V-Count Flag (LY==LYC)
        public const int LCD_DISPSTAT_VBLANK_IRQ = 3;  // V-Blank IRQ Enable
        public const int LCD_DISPSTAT_HBLANK_IRQ = 4;  // H-Blank IRQ Enable
        public const int LCD_DISPSTAT_VCOUNT_IRQ = 5;  // V-Count IRQ Enable
        public static unsafe ushort* LCD_VCOUNT => (ushort*)0x08000006;
        public static unsafe ushort* LCD_BG0CNT => (ushort*)0x08000008;
        public static unsafe ushort* LCD_BG1CNT => (ushort*)0x0800000A;
        public static unsafe ushort* LCD_BG2CNT => (ushort*)0x0800000C;
        public static unsafe ushort* LCD_BG3CNT => (ushort*)0x0800000E;
        public static unsafe ushort* LCD_BG0HOFS => (ushort*)0x08000010;
        public static unsafe ushort* LCD_BG0VOFS => (ushort*)0x08000012;
        public static unsafe ushort* LCD_BG1HOFS => (ushort*)0x08000014;
        public static unsafe ushort* LCD_BG1VOFS => (ushort*)0x08000016;
        public static unsafe ushort* LCD_BG2HOFS => (ushort*)0x08000018;
        public static unsafe ushort* LCD_BG2VOFS => (ushort*)0x0800001A;
        public static unsafe ushort* LCD_BG3HOFS => (ushort*)0x0800001C;
        public static unsafe ushort* LCD_BG3VOFS => (ushort*)0x0800001E;
        public static unsafe ushort* LCD_BG2PA => (ushort*)0x08000020;
        public static unsafe ushort* LCD_BG2PB => (ushort*)0x08000022;
        public static unsafe ushort* LCD_BG2PC => (ushort*)0x08000024;
        public static unsafe ushort* LCD_BG2PD => (ushort*)0x08000026;
        public static unsafe uint* LCD_BG2X => (uint*)0x08000028;
        public static unsafe uint* LCD_BG2Y => (uint*)0x0800002C;
        public static unsafe ushort* LCD_BG3PA => (ushort*)0x08000030;
        public static unsafe ushort* LCD_BG3PB => (ushort*)0x08000032;
        public static unsafe ushort* LCD_BG3PC => (ushort*)0x08000034;
        public static unsafe ushort* LCD_BG3PD => (ushort*)0x08000036;
        public static unsafe uint* LCD_BG3X => (uint*)0x08000038;
        public static unsafe uint* LCD_BG3Y => (uint*)0x0800003C;
        public static unsafe ushort* LCD_WIN0H => (ushort*)0x08000040;
        public static unsafe ushort* LCD_WIN1H => (ushort*)0x08000042;
        public static unsafe ushort* LCD_WIN0V => (ushort*)0x08000044;
        public static unsafe ushort* LCD_WIN1V => (ushort*)0x08000046;
        public static unsafe byte* LCD_WININ => (byte*)0x08000048;
        public static unsafe byte* LCD_WINOUT => (byte*)0x08000049;
        public static unsafe ushort* LCD_MOSAIC => (ushort*)0x0800004C;
        public static unsafe ushort* LCD_BLDCNT => (ushort*)0x08000050;
        public const int LCD_BLDCNT_BG1ST = 0;  // BG1 1st Target
        public const int LCD_BLDCNT_BG2ST = 1;  // BG2 1st Target
        public const int LCD_BLDCNT_BG3ST = 2;  // BG3 1st Target
        public const int LCD_BLDCNT_OBJST = 3;  // Object 1st Target
        public const int LCD_BLDCNT_BDST = 4;  // Backdrop 1st Target
        public const int LCD_BLDCNT_BLEND_MODE = 0;  // Blend Mode (0=None, 1=Alpha, 2=Increase, 3=Decrease)
        public const int LCD_BLDCNT_BG1ST2 = 8;  // BG1 2nd Target
        public const int LCD_BLDCNT_BG2ST2 = 9;  // BG2 2nd Target
        public const int LCD_BLDCNT_BG3ST2 = 10;  // BG3 2nd Target
        public const int LCD_BLDCNT_OBJST2 = 11;  // Object 2nd Target
        public const int LCD_BLDCNT_BDST2 = 12;  // Backdrop 2nd Target
        public static unsafe ushort* LCD_BLDALPHA => (ushort*)0x08000052;
        public static unsafe byte* LCD_BLDY => (byte*)0x08000054;

        // Direct Memory Access Controller
        public const int DMA_BASE = 0x040000B0;
        public static unsafe uint* DMA_DMA0SAD => (uint*)0x08000160;
        public static unsafe uint* DMA_DMA0DAD => (uint*)0x08000164;
        public static unsafe ushort* DMA_DMA0CNT_L => (ushort*)0x08000168;
        public static unsafe ushort* DMA_DMA0CNT_H => (ushort*)0x0800016A;
        public const int DMA_DMA0CNT_H_TRANSFER_COUNT = 0;  // Number of Transfers
        public const int DMA_DMA0CNT_H_DEST_ADD_MODE = 0;  // Dest Address Control (0=fix, 1=inc, 2=dec, 3=inc+reload)
        public const int DMA_DMA0CNT_H_SRC_ADD_MODE = 0;  // Source Address Control (0=fix, 1=inc, 2=dec)
        public const int DMA_DMA0CNT_H_REPEAT = 18;  // Repeat (for 16-bit repeat mode)
        public const int DMA_DMA0CNT_H_WORD_SIZE = 20;  // Word Size (0=16-bit, 1=32-bit)
        public const int DMA_DMA0CNT_H_DRQ = 27;  // DRQ Trigger (DMA from external source)
        public const int DMA_DMA0CNT_H_TIMING = 0;  // Start Timing (0=Now, 1=V-Blank, 2=H-Blank, 3=Special)
        public const int DMA_DMA0CNT_H_ENABLE = 31;  // DMA Enable
        public static unsafe uint* DMA_DMA1SAD => (uint*)0x0800016C;
        public static unsafe uint* DMA_DMA1DAD => (uint*)0x08000170;
        public static unsafe ushort* DMA_DMA1CNT_L => (ushort*)0x08000174;
        public static unsafe ushort* DMA_DMA1CNT_H => (ushort*)0x08000176;
        public static unsafe uint* DMA_DMA2SAD => (uint*)0x08000178;
        public static unsafe uint* DMA_DMA2DAD => (uint*)0x0800017C;
        public static unsafe ushort* DMA_DMA2CNT_L => (ushort*)0x08000180;
        public static unsafe ushort* DMA_DMA2CNT_H => (ushort*)0x08000182;
        public static unsafe uint* DMA_DMA3SAD => (uint*)0x08000184;
        public static unsafe uint* DMA_DMA3DAD => (uint*)0x08000188;
        public static unsafe ushort* DMA_DMA3CNT_L => (ushort*)0x0800018C;
        public static unsafe ushort* DMA_DMA3CNT_H => (ushort*)0x0800018E;

        // Timer Units (4 timers)
        public const int TIMER_BASE = 0x04000100;
        public static unsafe ushort* TIMER_TM0CNT_L => (ushort*)0x08000200;
        public static unsafe ushort* TIMER_TM0CNT_H => (ushort*)0x08000202;
        public const int TIMER_TM0CNT_H_PRESCALER = 0;  // Prescaler (0=1, 1=64, 2=256, 3=1024)
        public const int TIMER_TM0CNT_H_COUNT_UP = 2;  // Count Up (cascade mode)
        public const int TIMER_TM0CNT_H_IRQ_ENABLE = 6;  // Timer IRQ Enable
        public const int TIMER_TM0CNT_H_ENABLE = 7;  // Timer Enable
        public static unsafe ushort* TIMER_TM1CNT_L => (ushort*)0x08000204;
        public static unsafe ushort* TIMER_TM1CNT_H => (ushort*)0x08000206;
        public static unsafe ushort* TIMER_TM2CNT_L => (ushort*)0x08000208;
        public static unsafe ushort* TIMER_TM2CNT_H => (ushort*)0x0800020A;
        public static unsafe ushort* TIMER_TM3CNT_L => (ushort*)0x0800020C;
        public static unsafe ushort* TIMER_TM3CNT_H => (ushort*)0x0800020E;

        // Serial I/O (JOY BUS / Link Cable)
        public const int SIO_BASE = 0x04000120;
        public static unsafe ushort* SIO_SIOCNT => (ushort*)0x08000240;
        public const int SIO_SIOCNT_CLOCK_SEL = 0;  // Baud Rate Clock (0=9600, 1=57600, 2=115200, 3=768000)
        public const int SIO_SIOCNT_SO_ENABLE = 3;  // SO Output Enable
        public const int SIO_SIOCNT_RECV_ENABLE = 5;  // Receive Enable
        public const int SIO_SIOCNT_SEND_ENABLE = 6;  // Send Enable
        public const int SIO_SIOCNT_START_BIT = 7;  // Start Transfer
        public static unsafe byte* SIO_SIODATA8 => (byte*)0x0800024A;
        public static unsafe ushort* SIO_JOYCNT => (ushort*)0x08000250;
        public static unsafe ushort* SIO_JOYSTAT => (ushort*)0x08000254;
        public static unsafe uint* SIO_JOY_RECV => (uint*)0x08000270;
        public static unsafe uint* SIO_JOY_TRANS => (uint*)0x08000274;

        // Key Input
        public const int KEYINPUT_BASE = 0x04000130;
        public static unsafe ushort* KEYINPUT_KEYINPUT => (ushort*)0x08000260;
        public const int KEYINPUT_KEYINPUT_A = 0;  // A Button (0=Pressed)
        public const int KEYINPUT_KEYINPUT_B = 1;  // B Button (0=Pressed)
        public const int KEYINPUT_KEYINPUT_SELECT = 2;  // Select Button (0=Pressed)
        public const int KEYINPUT_KEYINPUT_START = 3;  // Start Button (0=Pressed)
        public const int KEYINPUT_KEYINPUT_RIGHT = 4;  // D-Pad Right (0=Pressed)
        public const int KEYINPUT_KEYINPUT_LEFT = 5;  // D-Pad Left (0=Pressed)
        public const int KEYINPUT_KEYINPUT_UP = 6;  // D-Pad Up (0=Pressed)
        public const int KEYINPUT_KEYINPUT_DOWN = 7;  // D-Pad Down (0=Pressed)
        public const int KEYINPUT_KEYINPUT_R = 8;  // R Shoulder Button (0=Pressed)
        public const int KEYINPUT_KEYINPUT_L = 9;  // L Shoulder Button (0=Pressed)
        public static unsafe ushort* KEYINPUT_KEYCNT => (ushort*)0x08000262;
        public const int KEYINPUT_KEYCNT_KEY_MASK = 0;  // Key Interrupt Enable Mask
        public const int KEYINPUT_KEYCNT_IRQ_ENABLE = 14;  // Key Interrupt Enable

        // Interrupt Control
        public const int INTERRUPT_BASE = 0x04000200;
        public static unsafe uint* INTERRUPT_IME => (uint*)0x08000408;
        public static unsafe uint* INTERRUPT_IE => (uint*)0x08000410;
        public const int INTERRUPT_IE_VBLANK = 0;  // V-Blank Interrupt Enable
        public const int INTERRUPT_IE_HBLANK = 1;  // H-Blank Interrupt Enable
        public const int INTERRUPT_IE_VCOUNT = 2;  // V-Count Match Interrupt Enable
        public const int INTERRUPT_IE_TIMER0 = 3;  // Timer 0 Interrupt Enable
        public const int INTERRUPT_IE_TIMER1 = 4;  // Timer 1 Interrupt Enable
        public const int INTERRUPT_IE_TIMER2 = 5;  // Timer 2 Interrupt Enable
        public const int INTERRUPT_IE_TIMER3 = 6;  // Timer 3 Interrupt Enable
        public const int INTERRUPT_IE_SIO = 7;  // Serial I/O Interrupt Enable
        public const int INTERRUPT_IE_DMA0 = 8;  // DMA 0 Interrupt Enable
        public const int INTERRUPT_IE_DMA1 = 9;  // DMA 1 Interrupt Enable
        public const int INTERRUPT_IE_DMA2 = 10;  // DMA 2 Interrupt Enable
        public const int INTERRUPT_IE_DMA3 = 11;  // DMA 3 Interrupt Enable
        public const int INTERRUPT_IE_KEYPAD = 12;  // Keypad Interrupt Enable
        public const int INTERRUPT_IE_CART = 13;  // Game Pak Interrupt Enable
        public static unsafe uint* INTERRUPT_IF => (uint*)0x08000414;

        // Waitstate Control
        public const int WAITCNT_BASE = 0x04000204;
        public static unsafe ushort* WAITCNT_WAITCNT => (ushort*)0x08000408;
        public const int WAITCNT_WAITCNT_PHI_OD = 0;  // PHI Terminal Output (0=Disable)
        public const int WAITCNT_WAITCNT_SRAM_WS = 0;  // SRAM Wait State (0=4, 1=3, 2=2, 3=8 cycles)
        public const int WAITCNT_WAITCNT_WS0_N = 0;  // Wait State 0 (ROM/SRAM 1st access)
        public const int WAITCNT_WAITCNT_WS0_S = 5;  // Wait State 0 (ROM/SRAM 2nd access)
        public const int WAITCNT_WAITCNT_WS1_N = 0;  // Wait State 1 (ROM 2nd access)
        public const int WAITCNT_WAITCNT_WS1_S = 8;  // Wait State 1 (ROM 2nd access short)
        public const int WAITCNT_WAITCNT_WS2_N = 0;  // Wait State 2 (ROM 3rd access)
        public const int WAITCNT_WAITCNT_WS2_S = 11;  // Wait State 2 (ROM 3rd access short)
        public const int WAITCNT_WAITCNT_PREFE = 12;  // Prefetch Enable (GBA SP only)

        // 中断向量定义
        public const int IRQ_VBLANK = 0;  // V-Blank Interrupt
        public const int IRQ_HBLANK = 1;  // H-Blank Interrupt
        public const int IRQ_VCOUNT = 2;  // V-Count Match Interrupt
        public const int IRQ_TIMER0 = 3;  // Timer 0 Overflow Interrupt
        public const int IRQ_TIMER1 = 4;  // Timer 1 Overflow Interrupt
        public const int IRQ_TIMER2 = 5;  // Timer 2 Overflow Interrupt
        public const int IRQ_TIMER3 = 6;  // Timer 3 Overflow Interrupt
        public const int IRQ_SIO = 7;  // Serial I/O Interrupt
        public const int IRQ_DMA0 = 8;  // DMA 0 Complete Interrupt
        public const int IRQ_DMA1 = 9;  // DMA 1 Complete Interrupt
        public const int IRQ_DMA2 = 10;  // DMA 2 Complete Interrupt
        public const int IRQ_DMA3 = 11;  // DMA 3 Complete Interrupt
        public const int IRQ_KEYPAD = 12;  // Keypad Interrupt
        public const int IRQ_CART = 13;  // Game Pak Interrupt

        // 引脚定义
        public const int PIN_VSS = 1;  // Ground
        public const int PIN_VDD = 2;  // Power Supply
        public const int PIN_CLK = 3;  // System Clock Input (16.78MHz)
        public const int PIN_RESET = 4;  // Reset Signal
        public const int PIN_NMI = 5;  // Non-Maskable Interrupt
        public const int PIN_IRQ = 6;  // Interrupt Request
        public const int PIN_AB0 = 7;  // Address Bus Bit 0
        public const int PIN_AB1 = 8;  // Address Bus Bit 1
        public const int PIN_AB2 = 9;  // Address Bus Bit 2
        public const int PIN_AB3 = 10;  // Address Bus Bit 3
        public const int PIN_AB4 = 11;  // Address Bus Bit 4
        public const int PIN_AB5 = 12;  // Address Bus Bit 5
        public const int PIN_AB6 = 13;  // Address Bus Bit 6
        public const int PIN_AB7 = 14;  // Address Bus Bit 7
        public const int PIN_AB8 = 15;  // Address Bus Bit 8
        public const int PIN_AB9 = 16;  // Address Bus Bit 9
        public const int PIN_AB10 = 17;  // Address Bus Bit 10
        public const int PIN_AB11 = 18;  // Address Bus Bit 11
        public const int PIN_AB12 = 19;  // Address Bus Bit 12
        public const int PIN_AB13 = 20;  // Address Bus Bit 13
        public const int PIN_AB14 = 21;  // Address Bus Bit 14
        public const int PIN_AB15 = 22;  // Address Bus Bit 15
        public const int PIN_AB16 = 23;  // Address Bus Bit 16
        public const int PIN_AB17 = 24;  // Address Bus Bit 17
        public const int PIN_AB18 = 25;  // Address Bus Bit 18
        public const int PIN_AB19 = 26;  // Address Bus Bit 19
        public const int PIN_AB20 = 27;  // Address Bus Bit 20
        public const int PIN_AB21 = 28;  // Address Bus Bit 21
        public const int PIN_AB22 = 29;  // Address Bus Bit 22
        public const int PIN_AB23 = 30;  // Address Bus Bit 23
        public const int PIN_AB24 = 31;  // Address Bus Bit 24
        public const int PIN_AB25 = 32;  // Address Bus Bit 25
        public const int PIN_AB26 = 33;  // Address Bus Bit 26
        public const int PIN_AB27 = 34;  // Address Bus Bit 27
        public const int PIN_AB28 = 35;  // Address Bus Bit 28
        public const int PIN_AB29 = 36;  // Address Bus Bit 29
        public const int PIN_AB30 = 37;  // Address Bus Bit 30
        public const int PIN_AB31 = 38;  // Address Bus Bit 31
        public const int PIN_DB0 = 39;  // Data Bus Bit 0
        public const int PIN_DB1 = 40;  // Data Bus Bit 1
        public const int PIN_DB2 = 41;  // Data Bus Bit 2
        public const int PIN_DB3 = 42;  // Data Bus Bit 3
        public const int PIN_DB4 = 43;  // Data Bus Bit 4
        public const int PIN_DB5 = 44;  // Data Bus Bit 5
        public const int PIN_DB6 = 45;  // Data Bus Bit 6
        public const int PIN_DB7 = 46;  // Data Bus Bit 7
        public const int PIN_DB8 = 47;  // Data Bus Bit 8
        public const int PIN_DB9 = 48;  // Data Bus Bit 9
        public const int PIN_DB10 = 49;  // Data Bus Bit 10
        public const int PIN_DB11 = 50;  // Data Bus Bit 11
        public const int PIN_DB12 = 51;  // Data Bus Bit 12
        public const int PIN_DB13 = 52;  // Data Bus Bit 13
        public const int PIN_DB14 = 53;  // Data Bus Bit 14
        public const int PIN_DB15 = 54;  // Data Bus Bit 15
        public const int PIN_DB16 = 55;  // Data Bus Bit 16
        public const int PIN_DB17 = 56;  // Data Bus Bit 17
        public const int PIN_DB18 = 57;  // Data Bus Bit 18
        public const int PIN_DB19 = 58;  // Data Bus Bit 19
        public const int PIN_DB20 = 59;  // Data Bus Bit 20
        public const int PIN_DB21 = 60;  // Data Bus Bit 21
        public const int PIN_DB22 = 61;  // Data Bus Bit 22
        public const int PIN_DB23 = 62;  // Data Bus Bit 23
        public const int PIN_DB24 = 63;  // Data Bus Bit 24
        public const int PIN_DB25 = 64;  // Data Bus Bit 25
        public const int PIN_DB26 = 65;  // Data Bus Bit 26
        public const int PIN_DB27 = 66;  // Data Bus Bit 27
        public const int PIN_DB28 = 67;  // Data Bus Bit 28
        public const int PIN_DB29 = 68;  // Data Bus Bit 29
        public const int PIN_DB30 = 69;  // Data Bus Bit 30
        public const int PIN_DB31 = 70;  // Data Bus Bit 31
        public const int PIN_NCS0 = 71;  // Chip Select 0 (ROM)
        public const int PIN_NCS1 = 72;  // Chip Select 1 (RAM)
        public const int PIN_NWR = 73;  // Write Enable (active low)
        public const int PIN_NRD = 74;  // Read Enable (active low)
        public const int PIN_ADV = 75;  // Address Valid (for external DMA)
        public const int PIN_BE0 = 76;  // Byte Enable 0
        public const int PIN_BE1 = 77;  // Byte Enable 1
        public const int PIN_BREQ = 78;  // Bus Request (from external master)
        public const int PIN_BACK = 79;  // Bus Acknowledge
        public const int PIN_EKO = 80;  // Serial Data Out (Link Cable)
        public const int PIN_EKI = 81;  // Serial Data In (Link Cable)
        public const int PIN_SOUND = 82;  // Stereo Audio Output (L+R)

        public static void arm7tdmi_init()
        {
            // 硬件初始化代码
        }
    }
}
