using System;

namespace VML.Device.Motorola.Motorola_68000
{
    /// <summary>
    /// Motorola-68000 寄存器定义
    /// 生成自: Motorola/68000/Motorola-68000
    /// 版本: 1.0
    /// </summary>
    public static class Motorola_68000
    {
        // CPU架构: MC68000, 32位, 7670452 Hz

        // 寄存器定义
        // Data Register 0
        public const int D0_ADDR = 0x00;
        public static unsafe uint* D0 => (uint*)0x00;

        // Data Register 1
        public const int D1_ADDR = 0x04;
        public static unsafe uint* D1 => (uint*)0x04;

        // Data Register 2
        public const int D2_ADDR = 0x08;
        public static unsafe uint* D2 => (uint*)0x08;

        // Data Register 3
        public const int D3_ADDR = 0x0C;
        public static unsafe uint* D3 => (uint*)0x0C;

        // Data Register 4
        public const int D4_ADDR = 0x10;
        public static unsafe uint* D4 => (uint*)0x10;

        // Data Register 5
        public const int D5_ADDR = 0x14;
        public static unsafe uint* D5 => (uint*)0x14;

        // Data Register 6
        public const int D6_ADDR = 0x18;
        public static unsafe uint* D6 => (uint*)0x18;

        // Data Register 7
        public const int D7_ADDR = 0x1C;
        public static unsafe uint* D7 => (uint*)0x1C;

        // Address Register 0
        public const int A0_ADDR = 0x20;
        public static unsafe uint* A0 => (uint*)0x20;

        // Address Register 1
        public const int A1_ADDR = 0x24;
        public static unsafe uint* A1 => (uint*)0x24;

        // Address Register 2
        public const int A2_ADDR = 0x28;
        public static unsafe uint* A2 => (uint*)0x28;

        // Address Register 3
        public const int A3_ADDR = 0x2C;
        public static unsafe uint* A3 => (uint*)0x2C;

        // Address Register 4
        public const int A4_ADDR = 0x30;
        public static unsafe uint* A4 => (uint*)0x30;

        // Address Register 5
        public const int A5_ADDR = 0x34;
        public static unsafe uint* A5 => (uint*)0x34;

        // Address Register 6
        public const int A6_ADDR = 0x38;
        public static unsafe uint* A6 => (uint*)0x38;

        // Stack Pointer (USP)
        public const int A7_ADDR = 0x3C;
        public static unsafe uint* A7 => (uint*)0x3C;

        // Program Counter
        public const int PC_ADDR = 0x40;
        public static unsafe uint* PC => (uint*)0x40;

        // Status Register
        public const int SR_ADDR = 0x44;
        public static unsafe ushort* SR => (ushort*)0x44;
        public const int SR_C = 0;  // Carry
        public const int SR_V = 1;  // Overflow
        public const int SR_Z = 2;  // Zero
        public const int SR_N = 3;  // Negative
        public const int SR_X = 4;  // Extend
        public const int SR_I0 = 8;  // Interrupt Mask 0
        public const int SR_I1 = 9;  // Interrupt Mask 1
        public const int SR_I2 = 10;  // Interrupt Mask 2
        public const int SR_M = 11;  // Master/Interrupt
        public const int SR_S = 13;  // Supervisor/User
        public const int SR_T0 = 14;  // Trace Mode 0
        public const int SR_T1 = 15;  // Trace Mode 1

        // 内存段定义
        // System RAM (4MB)
        public const int RAM_START = 0x000000;
        public const int RAM_END = 0x3FFFFF;
        public const int RAM_SIZE = 4194304;

        // Cartridge ROM
        public const int ROM_START = 0x000000;
        public const int ROM_END = 0x3FFFFF;
        public const int ROM_SIZE = 4194304;

        // I/O Register Area
        public const int IO_START = 0xA00000;
        public const int IO_END = 0xA1FFFF;
        public const int IO_SIZE = 131072;

        // VDP Registers
        public const int VDP_START = 0xC00000;
        public const int VDP_END = 0xC0001F;
        public const int VDP_SIZE = 32;

        // Video RAM (256KB)
        public const int VRAM_START = 0xE00000;
        public const int VRAM_END = 0xE3FFFF;
        public const int VRAM_SIZE = 262144;

        // 外设定义
        // Video Display Processor (TMS9918A variant)
        public const int VDP_BASE = 0xC00000;
        public static unsafe ushort* VDP_DATA => (ushort*)0x00C00000;
        public static unsafe ushort* VDP_CTRL => (ushort*)0x00C00004;
        public static unsafe ushort* VDP_HVCOUNT => (ushort*)0x00C00008;
        public static unsafe byte* VDP_HVB_STATUS => (byte*)0x00C0000A;

        // Programmable Sound Generator (AY-3-8910)
        public const int PSG_BASE = 0xC00011;
        public static unsafe byte* PSG_CH_A_FREQ => (byte*)0x00C00011;
        public static unsafe byte* PSG_CH_A_VOL => (byte*)0x00C00019;
        public static unsafe byte* PSG_CH_B_FREQ => (byte*)0x00C00013;
        public static unsafe byte* PSG_CH_B_VOL => (byte*)0x00C0001A;
        public static unsafe byte* PSG_CH_C_FREQ => (byte*)0x00C00015;
        public static unsafe byte* PSG_CH_C_VOL => (byte*)0x00C0001B;
        public static unsafe byte* PSG_NOISE_FREQ => (byte*)0x00C00017;
        public static unsafe byte* PSG_MIXER => (byte*)0x00C00018;
        public static unsafe byte* PSG_ENV_FREQ => (byte*)0x00C0001E;
        public static unsafe byte* PSG_ENV_SHAPE => (byte*)0x00C0001C;

        // Z80 Secondary CPU (Sound)
        public const int Z80_BASE = 0xA00000;
        public static unsafe byte* Z80_Z80_RESET => (byte*)0x00A00000;
        public static unsafe byte* Z80_Z80_BUSREQ => (byte*)0x00A00004;
        public static unsafe byte* Z80_Z80_STATUS => (byte*)0x00A00008;

        // Bank Register
        public const int BANK_REG_BASE = 0xA12000;
        public static unsafe byte* BANK_REG_ROM_BANK => (byte*)0x00A12000;
        public static unsafe byte* BANK_REG_RAM_BANK => (byte*)0x00A12004;

        // Hardware Version
        public const int HW_VERSION_BASE = 0xA10001;
        public static unsafe byte* HW_VERSION_VERSION => (byte*)0x00A10001;

        // Controller Port 1
        public const int CONTROLLER1_BASE = 0xA10003;
        public static unsafe byte* CONTROLLER1_DATA => (byte*)0x00A10003;
        public static unsafe byte* CONTROLLER1_CTRL => (byte*)0x00A10007;

        // Controller Port 2
        public const int CONTROLLER2_BASE = 0xA10005;
        public static unsafe byte* CONTROLLER2_DATA => (byte*)0x00A10005;
        public static unsafe byte* CONTROLLER2_CTRL => (byte*)0x00A10009;

        // External Port
        public const int EXT_PORT_BASE = 0xA10007;
        public static unsafe byte* EXT_PORT_DATA => (byte*)0x00A10007;

        // DMA Controller
        public const int DMA_BASE = 0xA10008;
        public static unsafe uint* DMA_SOURCE => (uint*)0x00A10008;
        public static unsafe uint* DMA_DEST => (uint*)0x00A1000C;
        public static unsafe ushort* DMA_COUNT => (ushort*)0x00A10010;
        public static unsafe byte* DMA_CTRL => (byte*)0x00A10012;

        // Hardware Timer
        public const int TIMER_BASE = 0xA1000E;
        public static unsafe byte* TIMER_H_COUNTER => (byte*)0x00A1000E;
        public static unsafe byte* TIMER_V_COUNTER => (byte*)0x00A10012;

        // 中断向量定义
        public const int IRQ_RESET_SP = 1;  // Reset Initial Stack Pointer
        public const int IRQ_RESET_PC = 2;  // Reset Initial PC
        public const int IRQ_BUS_ERROR = 3;  // Bus Error
        public const int IRQ_ADDRESS_ERROR = 4;  // Address Error
        public const int IRQ_ILLEGAL_INSTR = 5;  // Illegal Instruction
        public const int IRQ_ZERO_DIVIDE = 6;  // Zero Divide
        public const int IRQ_CHK_EXCEPTION = 7;  // CHK Exception
        public const int IRQ_TRAPV = 8;  // TRAPV Exception
        public const int IRQ_PRIVILEGE = 9;  // Privilege Violation
        public const int IRQ_TRACE = 10;  // Trace
        public const int IRQ_LINE_A = 11;  // Line 1010 Emulator
        public const int IRQ_LINE_F = 12;  // Line 1111 Emulator
        public const int IRQ_IRQ1 = 24;  // External Interrupt 1 (H-Blank)
        public const int IRQ_IRQ2 = 25;  // External Interrupt 2 (V-Blank)
        public const int IRQ_IRQ3 = 26;  // External Interrupt 3
        public const int IRQ_IRQ4 = 27;  // External Interrupt 4 (D-Req)
        public const int IRQ_IRQ5 = 28;  // External Interrupt 5
        public const int IRQ_IRQ6 = 29;  // External Interrupt 6
        public const int IRQ_IRQ7 = 30;  // External Interrupt 7
        public const int IRQ_TRAP0 = 32;  // TRAP #0
        public const int IRQ_TRAP1 = 33;  // TRAP #1
        public const int IRQ_TRAP15 = 47;  // TRAP #15

        public static void motorola_68000_init()
        {
            // 硬件初始化代码
        }
    }
}
