using System;

namespace VML.Device.IBM.IBM PC/AT
{
    /// <summary>
    /// IBM PC/AT 寄存器定义
    /// 生成自: IBM/IBM PC/IBM PC/AT
    /// 版本: 
    /// </summary>
    public static class IBM PC/AT
    {
        // CPU架构: x86-16, 0位, 0 Hz

        // 外设定义
        // Programmable Interrupt Controller
        public const int _8259A_BASE = ;
        public static unsafe ulong* _8259A_ICW1 => (ulong*)0x00000020;
        public static unsafe ulong* _8259A_ICW2 => (ulong*)0x00000021;
        public static unsafe ulong* _8259A_ICW3 => (ulong*)0x00000021;
        public static unsafe ulong* _8259A_ICW4 => (ulong*)0x00000021;
        public static unsafe ulong* _8259A_OCW1 => (ulong*)0x00000021;
        public static unsafe ulong* _8259A_OCW2 => (ulong*)0x00000020;
        public static unsafe ulong* _8259A_OCW3 => (ulong*)0x00000020;

        // Programmable Interval Timer
        public const int _8253_BASE = ;
        public static unsafe ulong* _8253_COUNTER0 => (ulong*)0x00000040;
        public static unsafe ulong* _8253_COUNTER1 => (ulong*)0x00000041;
        public static unsafe ulong* _8253_COUNTER2 => (ulong*)0x00000042;
        public static unsafe ulong* _8253_CONTROL => (ulong*)0x00000043;

        // Direct Memory Access Controller
        public const int _8237_BASE = ;
        public static unsafe uint* _8237_CHANNEL0 => (uint*)0x00000000;
        public static unsafe uint* _8237_CHANNEL1 => (uint*)0x00000002;
        public static unsafe uint* _8237_CHANNEL2 => (uint*)0x00000004;
        public static unsafe uint* _8237_CHANNEL3 => (uint*)0x00000006;
        public static unsafe ulong* _8237_STATUS => (ulong*)0x00000008;
        public static unsafe ulong* _8237_COMMAND => (ulong*)0x00000008;
        public static unsafe ulong* _8237_REQUEST => (ulong*)0x00000009;
        public static unsafe ulong* _8237_MASK => (ulong*)0x0000000A;
        public static unsafe ulong* _8237_MODE => (ulong*)0x0000000B;

        // Keyboard Controller
        public const int _8042_BASE = ;
        public static unsafe ulong* _8042_DATA => (ulong*)0x00000060;
        public static unsafe ulong* _8042_STATUS => (ulong*)0x00000064;

        // Real-Time Clock with CMOS RAM
        public const int CMOS_BASE = ;
        public static unsafe ulong* CMOS_ADDRESS => (ulong*)0x00000070;
        public static unsafe ulong* CMOS_DATA => (ulong*)0x00000071;

        // Floppy Disk Controller
        public const int FDC_BASE = ;
        public static unsafe ulong* FDC_SRA => (ulong*)0x000003F2;
        public static unsafe ulong* FDC_MSR => (ulong*)0x000003F4;
        public static unsafe ulong* FDC_DATA => (ulong*)0x000003F5;
        public static unsafe ulong* FDC_DIR => (ulong*)0x000003F7;
        public static unsafe ulong* FDC_CCR => (ulong*)0x000003F7;

        // Hard Disk Controller (ST-506/412)
        public const int HDC_BASE = ;
        public static unsafe uint* HDC_DATA => (uint*)0x000001F0;
        public static unsafe ulong* HDC_ERROR => (ulong*)0x000001F1;
        public static unsafe ulong* HDC_SECTORCOUNT => (ulong*)0x000001F2;
        public static unsafe ulong* HDC_SECTORNUMBER => (ulong*)0x000001F3;
        public static unsafe ulong* HDC_CYLINDERLOW => (ulong*)0x000001F4;
        public static unsafe ulong* HDC_CYLINDERHIGH => (ulong*)0x000001F5;
        public static unsafe ulong* HDC_DRIVEHEAD => (ulong*)0x000001F6;
        public static unsafe ulong* HDC_STATUS => (ulong*)0x000001F7;
        public static unsafe ulong* HDC_COMMAND => (ulong*)0x000001F7;

        // Color Graphics Adapter
        public const int CGA_BASE = ;
        public static unsafe ulong* CGA_CRTC_INDEX => (ulong*)0x000003D4;
        public static unsafe ulong* CGA_CRTC_DATA => (ulong*)0x000003D5;
        public static unsafe ulong* CGA_MODECONTROL => (ulong*)0x000003D8;
        public static unsafe ulong* CGA_COLORSELECT => (ulong*)0x000003D9;
        public static unsafe ulong* CGA_STATUS => (ulong*)0x000003DA;

        // Enhanced Graphics Adapter
        public const int EGA_BASE = ;
        public static unsafe ulong* EGA_CRTC_INDEX => (ulong*)0x000003D4;
        public static unsafe ulong* EGA_CRTC_DATA => (ulong*)0x000003D5;
        public static unsafe ulong* EGA_FEATURECONTROL => (ulong*)0x000003DA;
        public static unsafe ulong* EGA_GRAPHICS1POS => (ulong*)0x000003CC;
        public static unsafe ulong* EGA_GRAPHICS2POS => (ulong*)0x000003CA;
        public static unsafe ulong* EGA_SEQUENCERINDEX => (ulong*)0x000003C4;
        public static unsafe ulong* EGA_SEQUENCERDATA => (ulong*)0x000003C5;
        public static unsafe ulong* EGA_GRAPHICSINDEX => (ulong*)0x000003CE;
        public static unsafe ulong* EGA_GRAPHICSDATA => (ulong*)0x000003CF;
        public static unsafe ulong* EGA_ATTRIBUTEINDEX => (ulong*)0x000003C0;
        public static unsafe ulong* EGA_ATTRIBUTEDATA => (ulong*)0x000003C1;

        // Video Graphics Array
        public const int VGA_BASE = ;
        public static unsafe ulong* VGA_CRTC_INDEX => (ulong*)0x000003D4;
        public static unsafe ulong* VGA_CRTC_DATA => (ulong*)0x000003D5;
        public static unsafe ulong* VGA_INPUTSTATUS1 => (ulong*)0x000003DA;
        public static unsafe ulong* VGA_FEATURECONTROL => (ulong*)0x000003DA;
        public static unsafe ulong* VGA_MISCOUTPUT => (ulong*)0x000003C2;
        public static unsafe ulong* VGA_SEQUENCERINDEX => (ulong*)0x000003C4;
        public static unsafe ulong* VGA_SEQUENCERDATA => (ulong*)0x000003C5;
        public static unsafe ulong* VGA_GRAPHICSINDEX => (ulong*)0x000003CE;
        public static unsafe ulong* VGA_GRAPHICSDATA => (ulong*)0x000003CF;
        public static unsafe ulong* VGA_ATTRIBUTEINDEX => (ulong*)0x000003C0;
        public static unsafe ulong* VGA_ATTRIBUTEDATA => (ulong*)0x000003C1;
        public static unsafe ulong* VGA_DACMASK => (ulong*)0x000003C6;
        public static unsafe ulong* VGA_DACREADINDEX => (ulong*)0x000003C7;
        public static unsafe ulong* VGA_DACWRITEINDEX => (ulong*)0x000003C8;
        public static unsafe ulong* VGA_DACDATA => (ulong*)0x000003C9;

        // Game Port
        public const int GAMEPORT_BASE = ;
        public static unsafe ulong* GAMEPORT_DATA => (ulong*)0x00000201;

        // Parallel Printer Port
        public const int PARALLELPORT_BASE = ;
        public static unsafe ulong* PARALLELPORT_DATA => (ulong*)0x00000378;
        public static unsafe ulong* PARALLELPORT_STATUS => (ulong*)0x00000379;
        public static unsafe ulong* PARALLELPORT_CONTROL => (ulong*)0x0000037A;

        // Serial Communications Port
        public const int SERIALPORT_BASE = ;
        public static unsafe ulong* SERIALPORT_DATA => (ulong*)0x000003F8;
        public static unsafe ulong* SERIALPORT_IER => (ulong*)0x000003F9;
        public static unsafe ulong* SERIALPORT_IIR => (ulong*)0x000003FA;
        public static unsafe ulong* SERIALPORT_LCR => (ulong*)0x000003FB;
        public static unsafe ulong* SERIALPORT_MCR => (ulong*)0x000003FC;
        public static unsafe ulong* SERIALPORT_LSR => (ulong*)0x000003FD;
        public static unsafe ulong* SERIALPORT_MSR => (ulong*)0x000003FE;
        public static unsafe ulong* SERIALPORT_SCR => (ulong*)0x000003FF;

        // PC Speaker
        public const int SPEAKER_BASE = ;
        public static unsafe ulong* SPEAKER_CONTROL => (ulong*)0x00000061;

        // 中断向量定义
        public const int IRQ_DIVIDE_ERROR = 0;  // Division by zero
        public const int IRQ_SINGLE_STEP = 1;  // Debug single step
        public const int IRQ_NMI = 2;  // Non-maskable interrupt
        public const int IRQ_BREAKPOINT = 3;  // INT 3 instruction
        public const int IRQ_OVERFLOW = 4;  // INTO instruction
        public const int IRQ_PRINT_SCREEN = 5;  // Print screen key
        public const int IRQ_IRQ0 = 8;  // Timer interrupt
        public const int IRQ_IRQ1 = 9;  // Keyboard interrupt
        public const int IRQ_IRQ2 = 10;  // Cascade to IRQ8-15
        public const int IRQ_IRQ3 = 11;  // COM2 interrupt
        public const int IRQ_IRQ4 = 12;  // COM1 interrupt
        public const int IRQ_IRQ5 = 13;  // LPT2 interrupt
        public const int IRQ_IRQ6 = 14;  // Floppy disk interrupt
        public const int IRQ_IRQ7 = 15;  // LPT1 interrupt
        public const int IRQ_IRQ8 = 112;  // Real-time clock interrupt
        public const int IRQ_IRQ9 = 113;  // Redirected IRQ2
        public const int IRQ_IRQ10 = 114;  // Reserved
        public const int IRQ_IRQ11 = 115;  // Reserved
        public const int IRQ_IRQ12 = 116;  // PS/2 mouse interrupt
        public const int IRQ_IRQ13 = 117;  // Coprocessor interrupt
        public const int IRQ_IRQ14 = 118;  // Primary IDE interrupt
        public const int IRQ_IRQ15 = 119;  // Secondary IDE interrupt
        public const int IRQ_VIDEO_SERVICES = 16;  // Video BIOS services
        public const int IRQ_DISK_SERVICES = 19;  // Disk BIOS services
        public const int IRQ_DOS_SERVICES = 21;  // DOS function calls

        public static void ibm_pc_at_init()
        {
            // 硬件初始化代码
        }
    }
}
