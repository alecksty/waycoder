using System;

namespace VML.Device.AppleComputer.Macintosh_128K
{
    /// <summary>
    /// Macintosh-128K 寄存器定义
    /// 生成自: Apple Computer/Macintosh/Macintosh-128K
    /// 版本: 1.0
    /// </summary>
    public static class Macintosh_128K
    {
        // CPU架构: Motorola 68000, 32位, 7998000 Hz

        // 寄存器定义
        // Data Register 0
        public const int D0_ADDR = 0;
        public static unsafe uint* D0 => (uint*)0;

        // Data Register 1
        public const int D1_ADDR = 0;
        public static unsafe uint* D1 => (uint*)0;

        // Data Register 2
        public const int D2_ADDR = 0;
        public static unsafe uint* D2 => (uint*)0;

        // Data Register 3
        public const int D3_ADDR = 0;
        public static unsafe uint* D3 => (uint*)0;

        // Data Register 4
        public const int D4_ADDR = 0;
        public static unsafe uint* D4 => (uint*)0;

        // Data Register 5
        public const int D5_ADDR = 0;
        public static unsafe uint* D5 => (uint*)0;

        // Data Register 6
        public const int D6_ADDR = 0;
        public static unsafe uint* D6 => (uint*)0;

        // Data Register 7
        public const int D7_ADDR = 0;
        public static unsafe uint* D7 => (uint*)0;

        // Address Register 0
        public const int A0_ADDR = 0;
        public static unsafe uint* A0 => (uint*)0;

        // Address Register 1
        public const int A1_ADDR = 0;
        public static unsafe uint* A1 => (uint*)0;

        // Address Register 2
        public const int A2_ADDR = 0;
        public static unsafe uint* A2 => (uint*)0;

        // Address Register 3
        public const int A3_ADDR = 0;
        public static unsafe uint* A3 => (uint*)0;

        // Address Register 4
        public const int A4_ADDR = 0;
        public static unsafe uint* A4 => (uint*)0;

        // Address Register 5
        public const int A5_ADDR = 0;
        public static unsafe uint* A5 => (uint*)0;

        // Address Register 6
        public const int A6_ADDR = 0;
        public static unsafe uint* A6 => (uint*)0;

        // Address Register 7 (SP)
        public const int A7_ADDR = 0;
        public static unsafe uint* A7 => (uint*)0;

        // Program Counter
        public const int PC_ADDR = 0;
        public static unsafe uint* PC => (uint*)0;

        // Status Register
        public const int SR_ADDR = 0;
        public static unsafe ushort* SR => (ushort*)0;

        // 外设定义
        // Versatile Interface Adapter (6522)
        public const int VIA_BASE = ;
        public static unsafe byte* VIA_VIA_ORB => (byte*)0x00E80000;
        public static unsafe byte* VIA_VIA_ORA => (byte*)0x00E80001;
        public static unsafe byte* VIA_VIA_DDRB => (byte*)0x00E80002;
        public static unsafe byte* VIA_VIA_DDRA => (byte*)0x00E80003;
        public static unsafe byte* VIA_VIA_T1CL => (byte*)0x00E80004;
        public static unsafe byte* VIA_VIA_T1CH => (byte*)0x00E80005;
        public static unsafe byte* VIA_VIA_T1LL => (byte*)0x00E80006;
        public static unsafe byte* VIA_VIA_T1LH => (byte*)0x00E80007;
        public static unsafe byte* VIA_VIA_T2CL => (byte*)0x00E80008;
        public static unsafe byte* VIA_VIA_T2CH => (byte*)0x00E80009;
        public static unsafe byte* VIA_VIA_SR => (byte*)0x00E8000A;
        public static unsafe byte* VIA_VIA_ACR => (byte*)0x00E8000B;
        public static unsafe byte* VIA_VIA_PCR => (byte*)0x00E8000C;
        public static unsafe byte* VIA_VIA_IFR => (byte*)0x00E8000D;
        public static unsafe byte* VIA_VIA_IER => (byte*)0x00E8000E;
        public static unsafe byte* VIA_VIA_ORA2 => (byte*)0x00E8000F;

        // Integrated Woz Machine (floppy controller)
        public const int IWM_BASE = ;
        public static unsafe byte* IWM_IWM_Q6 => (byte*)0x00D00000;
        public static unsafe byte* IWM_IWM_Q7 => (byte*)0x00D00002;
        public static unsafe byte* IWM_IWM_PH0 => (byte*)0x00D00004;
        public static unsafe byte* IWM_IWM_PH1 => (byte*)0x00D00006;
        public static unsafe byte* IWM_IWM_PH2 => (byte*)0x00D00008;
        public static unsafe byte* IWM_IWM_PH3 => (byte*)0x00D0000A;

        // Zilog 8530 Serial Communications Controller
        public const int SCC_BASE = ;
        public static unsafe byte* SCC_SCC_CA => (byte*)0x00500000;
        public static unsafe byte* SCC_SCC_DA => (byte*)0x00500002;
        public static unsafe byte* SCC_SCC_CB => (byte*)0x00500004;
        public static unsafe byte* SCC_SCC_DB => (byte*)0x00500006;

        // Built-in speaker
        public const int SOUND_BASE = ;
        public static unsafe byte* SOUND_SOUND_VOL => (byte*)0x00E80100;
        public static unsafe byte* SOUND_SOUND_FREQ => (byte*)0x00E80102;

        // 中断向量定义
        public const int IRQ_RESET_SP = 0;  // Reset (Initial SP)
        public const int IRQ_RESET_PC = 4;  // Reset (Initial PC)
        public const int IRQ_AUTOVECTOR1 = 24;  // Auto vector 1
        public const int IRQ_AUTOVECTOR2 = 25;  // Auto vector 2
        public const int IRQ_AUTOVECTOR3 = 26;  // Auto vector 3
        public const int IRQ_AUTOVECTOR4 = 27;  // Auto vector 4
        public const int IRQ_AUTOVECTOR5 = 28;  // Auto vector 5
        public const int IRQ_AUTOVECTOR6 = 29;  // Auto vector 6
        public const int IRQ_AUTOVECTOR7 = 30;  // Auto vector 7
        public const int IRQ_SPURIOUS = 31;  // Spurious interrupt

        public static void macintosh_128k_init()
        {
            // 硬件初始化代码
        }
    }
}
