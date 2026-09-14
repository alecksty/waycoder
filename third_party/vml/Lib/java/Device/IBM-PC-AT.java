package vml.device.ibm.ibm_pc_at;

/**
 * IBM PC/AT 寄存器定义
 * 生成自: IBM/IBM PC/IBM PC/AT
 * 版本: 
 */
public final class IBM PC/AT {
    private IBM PC/AT() {} // 工具类
    // CPU架构: x86-16, 0位, 0 Hz

    // 外设定义
    // Programmable Interrupt Controller
    public static final int _8259A_BASE = (int);
    public static final int _8259A_ICW1 = (int)0x00000020;
    public static final int _8259A_ICW2 = (int)0x00000021;
    public static final int _8259A_ICW3 = (int)0x00000021;
    public static final int _8259A_ICW4 = (int)0x00000021;
    public static final int _8259A_OCW1 = (int)0x00000021;
    public static final int _8259A_OCW2 = (int)0x00000020;
    public static final int _8259A_OCW3 = (int)0x00000020;

    // Programmable Interval Timer
    public static final int _8253_BASE = (int);
    public static final int _8253_COUNTER0 = (int)0x00000040;
    public static final int _8253_COUNTER1 = (int)0x00000041;
    public static final int _8253_COUNTER2 = (int)0x00000042;
    public static final int _8253_CONTROL = (int)0x00000043;

    // Direct Memory Access Controller
    public static final int _8237_BASE = (int);
    public static final int _8237_CHANNEL0 = (int)0x00000000;
    public static final int _8237_CHANNEL1 = (int)0x00000002;
    public static final int _8237_CHANNEL2 = (int)0x00000004;
    public static final int _8237_CHANNEL3 = (int)0x00000006;
    public static final int _8237_STATUS = (int)0x00000008;
    public static final int _8237_COMMAND = (int)0x00000008;
    public static final int _8237_REQUEST = (int)0x00000009;
    public static final int _8237_MASK = (int)0x0000000A;
    public static final int _8237_MODE = (int)0x0000000B;

    // Keyboard Controller
    public static final int _8042_BASE = (int);
    public static final int _8042_DATA = (int)0x00000060;
    public static final int _8042_STATUS = (int)0x00000064;

    // Real-Time Clock with CMOS RAM
    public static final int CMOS_BASE = (int);
    public static final int CMOS_ADDRESS = (int)0x00000070;
    public static final int CMOS_DATA = (int)0x00000071;

    // Floppy Disk Controller
    public static final int FDC_BASE = (int);
    public static final int FDC_SRA = (int)0x000003F2;
    public static final int FDC_MSR = (int)0x000003F4;
    public static final int FDC_DATA = (int)0x000003F5;
    public static final int FDC_DIR = (int)0x000003F7;
    public static final int FDC_CCR = (int)0x000003F7;

    // Hard Disk Controller (ST-506/412)
    public static final int HDC_BASE = (int);
    public static final int HDC_DATA = (int)0x000001F0;
    public static final int HDC_ERROR = (int)0x000001F1;
    public static final int HDC_SECTORCOUNT = (int)0x000001F2;
    public static final int HDC_SECTORNUMBER = (int)0x000001F3;
    public static final int HDC_CYLINDERLOW = (int)0x000001F4;
    public static final int HDC_CYLINDERHIGH = (int)0x000001F5;
    public static final int HDC_DRIVEHEAD = (int)0x000001F6;
    public static final int HDC_STATUS = (int)0x000001F7;
    public static final int HDC_COMMAND = (int)0x000001F7;

    // Color Graphics Adapter
    public static final int CGA_BASE = (int);
    public static final int CGA_CRTC_INDEX = (int)0x000003D4;
    public static final int CGA_CRTC_DATA = (int)0x000003D5;
    public static final int CGA_MODECONTROL = (int)0x000003D8;
    public static final int CGA_COLORSELECT = (int)0x000003D9;
    public static final int CGA_STATUS = (int)0x000003DA;

    // Enhanced Graphics Adapter
    public static final int EGA_BASE = (int);
    public static final int EGA_CRTC_INDEX = (int)0x000003D4;
    public static final int EGA_CRTC_DATA = (int)0x000003D5;
    public static final int EGA_FEATURECONTROL = (int)0x000003DA;
    public static final int EGA_GRAPHICS1POS = (int)0x000003CC;
    public static final int EGA_GRAPHICS2POS = (int)0x000003CA;
    public static final int EGA_SEQUENCERINDEX = (int)0x000003C4;
    public static final int EGA_SEQUENCERDATA = (int)0x000003C5;
    public static final int EGA_GRAPHICSINDEX = (int)0x000003CE;
    public static final int EGA_GRAPHICSDATA = (int)0x000003CF;
    public static final int EGA_ATTRIBUTEINDEX = (int)0x000003C0;
    public static final int EGA_ATTRIBUTEDATA = (int)0x000003C1;

    // Video Graphics Array
    public static final int VGA_BASE = (int);
    public static final int VGA_CRTC_INDEX = (int)0x000003D4;
    public static final int VGA_CRTC_DATA = (int)0x000003D5;
    public static final int VGA_INPUTSTATUS1 = (int)0x000003DA;
    public static final int VGA_FEATURECONTROL = (int)0x000003DA;
    public static final int VGA_MISCOUTPUT = (int)0x000003C2;
    public static final int VGA_SEQUENCERINDEX = (int)0x000003C4;
    public static final int VGA_SEQUENCERDATA = (int)0x000003C5;
    public static final int VGA_GRAPHICSINDEX = (int)0x000003CE;
    public static final int VGA_GRAPHICSDATA = (int)0x000003CF;
    public static final int VGA_ATTRIBUTEINDEX = (int)0x000003C0;
    public static final int VGA_ATTRIBUTEDATA = (int)0x000003C1;
    public static final int VGA_DACMASK = (int)0x000003C6;
    public static final int VGA_DACREADINDEX = (int)0x000003C7;
    public static final int VGA_DACWRITEINDEX = (int)0x000003C8;
    public static final int VGA_DACDATA = (int)0x000003C9;

    // Game Port
    public static final int GAMEPORT_BASE = (int);
    public static final int GAMEPORT_DATA = (int)0x00000201;

    // Parallel Printer Port
    public static final int PARALLELPORT_BASE = (int);
    public static final int PARALLELPORT_DATA = (int)0x00000378;
    public static final int PARALLELPORT_STATUS = (int)0x00000379;
    public static final int PARALLELPORT_CONTROL = (int)0x0000037A;

    // Serial Communications Port
    public static final int SERIALPORT_BASE = (int);
    public static final int SERIALPORT_DATA = (int)0x000003F8;
    public static final int SERIALPORT_IER = (int)0x000003F9;
    public static final int SERIALPORT_IIR = (int)0x000003FA;
    public static final int SERIALPORT_LCR = (int)0x000003FB;
    public static final int SERIALPORT_MCR = (int)0x000003FC;
    public static final int SERIALPORT_LSR = (int)0x000003FD;
    public static final int SERIALPORT_MSR = (int)0x000003FE;
    public static final int SERIALPORT_SCR = (int)0x000003FF;

    // PC Speaker
    public static final int SPEAKER_BASE = (int);
    public static final int SPEAKER_CONTROL = (int)0x00000061;

    // 中断向量定义
    public static final int IRQ_DIVIDE_ERROR = 0;  // Division by zero
    public static final int IRQ_SINGLE_STEP = 1;  // Debug single step
    public static final int IRQ_NMI = 2;  // Non-maskable interrupt
    public static final int IRQ_BREAKPOINT = 3;  // INT 3 instruction
    public static final int IRQ_OVERFLOW = 4;  // INTO instruction
    public static final int IRQ_PRINT_SCREEN = 5;  // Print screen key
    public static final int IRQ_IRQ0 = 8;  // Timer interrupt
    public static final int IRQ_IRQ1 = 9;  // Keyboard interrupt
    public static final int IRQ_IRQ2 = 10;  // Cascade to IRQ8-15
    public static final int IRQ_IRQ3 = 11;  // COM2 interrupt
    public static final int IRQ_IRQ4 = 12;  // COM1 interrupt
    public static final int IRQ_IRQ5 = 13;  // LPT2 interrupt
    public static final int IRQ_IRQ6 = 14;  // Floppy disk interrupt
    public static final int IRQ_IRQ7 = 15;  // LPT1 interrupt
    public static final int IRQ_IRQ8 = 112;  // Real-time clock interrupt
    public static final int IRQ_IRQ9 = 113;  // Redirected IRQ2
    public static final int IRQ_IRQ10 = 114;  // Reserved
    public static final int IRQ_IRQ11 = 115;  // Reserved
    public static final int IRQ_IRQ12 = 116;  // PS/2 mouse interrupt
    public static final int IRQ_IRQ13 = 117;  // Coprocessor interrupt
    public static final int IRQ_IRQ14 = 118;  // Primary IDE interrupt
    public static final int IRQ_IRQ15 = 119;  // Secondary IDE interrupt
    public static final int IRQ_VIDEO_SERVICES = 16;  // Video BIOS services
    public static final int IRQ_DISK_SERVICES = 19;  // Disk BIOS services
    public static final int IRQ_DOS_SERVICES = 21;  // DOS function calls

    public static native void ibm_pc_at_init();
}
