using System;

namespace VML.Device.Various(ASCII/Awanaga/MSXAssociation).MSX1
{
    /// <summary>
    /// MSX1 寄存器定义
    /// 生成自: Various (ASCII/Awanaga/MSX Association)/MSX/MSX1
    /// 版本: 1.0
    /// </summary>
    public static class MSX1
    {
        // CPU架构: Z80A, 8位, 3579545 Hz

        // 寄存器定义
        // Accumulator
        public const int A_ADDR = 0x00;
        public static unsafe byte* A => (byte*)0x00;

        // Flags
        public const int F_ADDR = 0x01;
        public static unsafe byte* F => (byte*)0x01;
        public const int F_C = 0;  // Carry
        public const int F_N = 1;  // Subtract
        public const int F_PV = 2;  // Parity/Overflow
        public const int F_H = 4;  // Half Carry
        public const int F_Z = 6;  // Zero
        public const int F_S = 7;  // Sign

        // B Register
        public const int B_ADDR = 0x02;
        public static unsafe byte* B => (byte*)0x02;

        // C Register
        public const int C_ADDR = 0x03;
        public static unsafe byte* C => (byte*)0x03;

        // D Register
        public const int D_ADDR = 0x04;
        public static unsafe byte* D => (byte*)0x04;

        // E Register
        public const int E_ADDR = 0x05;
        public static unsafe byte* E => (byte*)0x05;

        // H Register
        public const int H_ADDR = 0x06;
        public static unsafe byte* H => (byte*)0x06;

        // L Register
        public const int L_ADDR = 0x07;
        public static unsafe byte* L => (byte*)0x07;

        // Alternate AF
        public const int AF_ADDR = 0x08;
        public static unsafe ushort* AF' => (ushort*)0x08;

        // Alternate BC
        public const int BC_ADDR = 0x0A;
        public static unsafe ushort* BC' => (ushort*)0x0A;

        // Alternate DE
        public const int DE_ADDR = 0x0C;
        public static unsafe ushort* DE' => (ushort*)0x0C;

        // Alternate HL
        public const int HL_ADDR = 0x0E;
        public static unsafe ushort* HL' => (ushort*)0x0E;

        // Interrupt Vector
        public const int I_ADDR = 0x10;
        public static unsafe byte* I => (byte*)0x10;

        // Refresh
        public const int R_ADDR = 0x11;
        public static unsafe byte* R => (byte*)0x11;

        // Index X
        public const int IX_ADDR = 0x12;
        public static unsafe ushort* IX => (ushort*)0x12;

        // Index Y (usually = 0xF38F)
        public const int IY_ADDR = 0x14;
        public static unsafe ushort* IY => (ushort*)0x14;

        // Stack Pointer
        public const int SP_ADDR = 0x16;
        public static unsafe ushort* SP => (ushort*)0x16;

        // Program Counter
        public const int PC_ADDR = 0x18;
        public static unsafe ushort* PC => (ushort*)0x18;

        // 内存段定义
        // Cartridge/SUB-ROM / Main-ROM
        public const int SLOT0_ROM_START = 0x0000;
        public const int SLOT0_ROM_END = 0x7FFF;
        public const int SLOT0_ROM_SIZE = 32768;

        // MSX-BIOS ROM
        public const int SYSROM_START = 0x0000;
        public const int SYSROM_END = 0x3FFF;
        public const int SYSROM_SIZE = 16384;

        // Extension ROM (cartridge)
        public const int EXTROM_START = 0x4000;
        public const int EXTROM_END = 0x7FFF;
        public const int EXTROM_SIZE = 16384;

        // Main RAM (32KB working area)
        public const int MAIN_RAM_START = 0x4000;
        public const int MAIN_RAM_END = 0xC000;
        public const int MAIN_RAM_SIZE = 32768;

        // Work RAM (16KB)
        public const int WORK_RAM_START = 0xC000;
        public const int WORK_RAM_END = 0xFFFF;
        public const int WORK_RAM_SIZE = 16384;

        // System variables area
        public const int SYSVAR_START = 0xF000;
        public const int SYSVAR_END = 0xFCA0;
        public const int SYSVAR_SIZE = 3232;

        // Slot-mapped memory
        public const int SLOTS_START = 0x8000;
        public const int SLOTS_END = 0xFFFF;
        public const int SLOTS_SIZE = 32768;

        // 外设定义
        // TMS9918A Video Display Processor
        public const int VDP_BASE = 0x98;
        public static unsafe byte* VDP_VDP_REG0 => (byte*)0x00000131;
        public static unsafe byte* VDP_VDP_REG1 => (byte*)0x00000131;
        public static unsafe byte* VDP_VDP_REG2 => (byte*)0x00000131;
        public static unsafe byte* VDP_VDP_REG3 => (byte*)0x00000131;
        public static unsafe byte* VDP_VDP_REG4 => (byte*)0x00000131;
        public static unsafe byte* VDP_VDP_REG5 => (byte*)0x00000131;
        public static unsafe byte* VDP_VDP_REG6 => (byte*)0x00000131;
        public static unsafe byte* VDP_VDP_REG7 => (byte*)0x00000131;
        public static unsafe byte* VDP_VDP_STATUS => (byte*)0x00000131;
        public static unsafe byte* VDP_VDP_DATA => (byte*)0x00000130;
        public static unsafe byte* VDP_VDP_POT => (byte*)0x00000130;

        // AY-3-8910 Programmable Sound Generator
        public const int PSG_BASE = 0xA0;
        public static unsafe byte* PSG_PSG_REG => (byte*)0x00000141;
        public static unsafe byte* PSG_PSG_DATA => (byte*)0x00000143;
        public static unsafe byte* PSG_FREQ_A_LO => (byte*)0x00000140;
        public static unsafe byte* PSG_FREQ_A_HI => (byte*)0x00000141;
        public static unsafe byte* PSG_FREQ_B_LO => (byte*)0x00000142;
        public static unsafe byte* PSG_FREQ_B_HI => (byte*)0x00000143;
        public static unsafe byte* PSG_FREQ_C_LO => (byte*)0x00000144;
        public static unsafe byte* PSG_FREQ_C_HI => (byte*)0x00000145;
        public static unsafe byte* PSG_NOISE_FREQ => (byte*)0x00000146;
        public static unsafe byte* PSG_ENABLE => (byte*)0x00000147;
        public static unsafe byte* PSG_VOL_A => (byte*)0x00000148;
        public static unsafe byte* PSG_VOL_B => (byte*)0x00000149;
        public static unsafe byte* PSG_VOL_C => (byte*)0x0000014A;
        public static unsafe byte* PSG_ENV_FREQ_LO => (byte*)0x0000014B;
        public static unsafe byte* PSG_ENV_FREQ_HI => (byte*)0x0000014C;
        public static unsafe byte* PSG_ENV_SHAPE => (byte*)0x0000014D;
        public static unsafe byte* PSG_PORT_A => (byte*)0x0000014E;
        public static unsafe byte* PSG_PORT_B => (byte*)0x0000014F;

        // PPI 8255 Programmable Peripheral Interface
        public const int PPI_BASE = 0xA8;
        public static unsafe byte* PPI_PPI_PA => (byte*)0x00000150;
        public static unsafe byte* PPI_PPI_PB => (byte*)0x00000151;
        public static unsafe byte* PPI_PPI_PC => (byte*)0x00000152;
        public static unsafe byte* PPI_PPI_CTRL => (byte*)0x00000153;

        // MSX Slot Expansion System
        public const int SLOTEXP_BASE = 0x0000;
        public static unsafe byte* SLOTEXP_SLOT0 => (byte*)0x0000FCC0;
        public static unsafe byte* SLOTEXP_SLOT1 => (byte*)0x0000FCC1;
        public static unsafe byte* SLOTEXP_SLOT2 => (byte*)0x0000FCC2;
        public static unsafe byte* SLOTEXP_SLOT3 => (byte*)0x0000FCC3;
        public static unsafe byte* SLOTEXP_EXPTBL0 => (byte*)0x0000FCC4;
        public static unsafe byte* SLOTEXP_EXPTBL1 => (byte*)0x0000FCC5;
        public static unsafe byte* SLOTEXP_EXPTBL2 => (byte*)0x0000FCC6;
        public static unsafe byte* SLOTEXP_EXPTBL3 => (byte*)0x0000FCC7;

        // 中断向量定义
        public const int IRQ_RESET = 0;  // Power-on / Reset
        public const int IRQ_NMI = 1;  // Non-Maskable Interrupt
        public const int IRQ_INT = 2;  // VDP Vertical Interrupt (frame)

        // 引脚定义
        public const int PIN_VCC = 1;  // +5V Power
        public const int PIN_GND = 2;  // Ground
        public const int PIN_CLK = 3;  // Z80 Clock (3.58MHz)
        public const int PIN_A0_A15 = 4;  // Address Bus
        public const int PIN_D0_D7 = 5;  // Data Bus
        public const int PIN_MREQ = 6;  // Memory Request
        public const int PIN_IORQ = 7;  // I/O Request
        public const int PIN_RD = 8;  // Read
        public const int PIN_WR = 9;  // Write
        public const int PIN_INT = 10;  // Interrupt Request
        public const int PIN_NMI = 11;  // Non-Maskable Interrupt
        public const int PIN_RESET = 12;  // Reset
        public const int PIN_SLTSL = 13;  // Slot select (for memory mapping)
        public const int PIN_WAIT = 14;  // Wait (for slow I/O)

        public static void msx1_init()
        {
            // 硬件初始化代码
        }
    }
}
