using System;

namespace VML.Device.SinclairResearch.ZX_Spectrum_48K
{
    /// <summary>
    /// ZX-Spectrum-48K 寄存器定义
    /// 生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum-48K
    /// 版本: 1.0
    /// </summary>
    public static class ZX_Spectrum_48K
    {
        // CPU架构: Z80A, 8位, 3500000 Hz

        // 寄存器定义
        // Accumulator
        public const int A_ADDR = 0x00;
        public static unsafe byte* A => (byte*)0x00;

        // Flags Register
        public const int F_ADDR = 0x01;
        public static unsafe byte* F => (byte*)0x01;
        public const int F_C = 0;  // Carry
        public const int F_N = 1;  // Add/Subtract
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

        // Interrupt Vector Register
        public const int I_ADDR = 0x10;
        public static unsafe byte* I => (byte*)0x10;

        // Refresh Counter
        public const int R_ADDR = 0x11;
        public static unsafe byte* R => (byte*)0x11;

        // Index X
        public const int IX_ADDR = 0x12;
        public static unsafe ushort* IX => (ushort*)0x12;

        // Index Y
        public const int IY_ADDR = 0x14;
        public static unsafe ushort* IY => (ushort*)0x14;

        // Stack Pointer
        public const int SP_ADDR = 0x16;
        public static unsafe ushort* SP => (ushort*)0x16;

        // Program Counter
        public const int PC_ADDR = 0x18;
        public static unsafe ushort* PC => (ushort*)0x18;

        // 内存段定义
        // 48KB ZX Spectrum ROM (BASIC + monitor)
        public const int ROM_START = 0x0000;
        public const int ROM_END = 0x3FFF;
        public const int ROM_SIZE = 16384;

        // Display file (256x192 bitmap)
        public const int VIDEO_RAM_START = 0x4000;
        public const int VIDEO_RAM_END = 0x57FF;
        public const int VIDEO_RAM_SIZE = 6144;

        // Attribute file (32x24 color cells)
        public const int ATTR_RAM_START = 0x5800;
        public const int ATTR_RAM_END = 0x5AFF;
        public const int ATTR_RAM_SIZE = 768;

        // User RAM (40KB)
        public const int USER_RAM_START = 0x5B00;
        public const int USER_RAM_END = 0xFFFF;
        public const int USER_RAM_SIZE = 40960;

        // 外设定义
        // Uncommitted Logic Array - Sinclair custom IC
        public const int ULA_BASE = 0xFE;
        public static unsafe byte* ULA_BORDER => (byte*)0x000001FC;
        public static unsafe byte* ULA_KBD_ROW0 => (byte*)0x000001FC;
        public static unsafe byte* ULA_KBD_ROW1 => (byte*)0x000001FC;
        public static unsafe byte* ULA_KBD_ROW2 => (byte*)0x000001FC;
        public static unsafe byte* ULA_KBD_ROW3 => (byte*)0x000001FC;
        public static unsafe byte* ULA_KBD_ROW4 => (byte*)0x000001FC;
        public static unsafe byte* ULA_KBD_ROW5 => (byte*)0x000001FC;
        public static unsafe byte* ULA_KBD_ROW6 => (byte*)0x000001FC;
        public static unsafe byte* ULA_KBD_ROW7 => (byte*)0x000001FC;
        public static unsafe byte* ULA_KBD_ROW8 => (byte*)0x000001FC;

        // Keyboard Matrix (40 keys, 8 rows x 5 cols)
        public const int KEYBOARD_BASE = 0xFE;
        public static unsafe byte* KEYBOARD_KBD_IN => (byte*)0x000001FC;

        // Internal Beeper
        public const int BEEPER_BASE = 0xFE;
        public static unsafe byte* BEEPER_BEEP => (byte*)0x000001FC;

        // Tape Interface
        public const int TAPE_BASE = 0xFE;
        public static unsafe byte* TAPE_EAR_IN => (byte*)0x000001FC;
        public static unsafe byte* TAPE_MIC_OUT => (byte*)0x000001FC;

        // Kempston Joystick Interface
        public const int JOYSTICK_BASE = 0xF7FE;
        public static unsafe byte* JOYSTICK_KEMPSTON => (byte*)0x0001EFFC;

        // 中断向量定义
        public const int IRQ_RESET = 0;  // Power-on / Reset
        public const int IRQ_NMI = 1;  // Non-Maskable Interrupt (BREAK key)
        public const int IRQ_INT = 2;  // Maskable Interrupt (ULA vertical blank, 50Hz)

        // 引脚定义
        public const int PIN_VCC = 1;  // +5V Power
        public const int PIN_GND = 2;  // Ground
        public const int PIN_CLK = 3;  // Z80 Clock (3.5MHz)
        public const int PIN_M1 = 4;  // Machine Cycle 1
        public const int PIN_MREQ = 5;  // Memory Request
        public const int PIN_IORQ = 6;  // I/O Request
        public const int PIN_RD = 7;  // Read
        public const int PIN_WR = 8;  // Write
        public const int PIN_HALT = 9;  // Halt State
        public const int PIN_BUSAK = 10;  // Bus Acknowledge
        public const int PIN_WAIT = 11;  // Wait State (ULA inserts)
        public const int PIN_INT = 12;  // Interrupt Request
        public const int PIN_NMI = 13;  // Non-Maskable Interrupt
        public const int PIN_RESET = 14;  // Reset
        public const int PIN_A0_A15 = 15;  // Address Bus (16-bit)
        public const int PIN_D0_D7 = 16;  // Data Bus (8-bit)

        public static void zx_spectrum_48k_init()
        {
            // 硬件初始化代码
        }
    }
}
