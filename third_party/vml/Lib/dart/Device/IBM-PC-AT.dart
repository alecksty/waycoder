// IBM PC/AT 设备定义 - Dart 库
// 生成自: IBM/IBM PC/IBM PC/AT
// 版本: 
// 日期: 
// 作者: 
// 描述: IBM Personal Computer/Advanced Technology (Model 5170)
// CPU架构: x86-16
// 位宽: 0位
// 时钟频率: 0 Hz

class IBM PC/ATDevice {
  static const String deviceName = "IBM PC/AT";
  static const String manufacturer = "IBM";
  static const String family = "IBM PC";
  static const String version = "";
  static const String architecture = "x86-16";
  static const int bits = 0;
  static const int clockFrequency = 0;

  // 外设定义
  // Programmable Interrupt Controller
  static const int _8259A_BASE = ;
  static const int _8259A_ICW1_ADDR = 0x20;
  static const int _8259A_ICW2_ADDR = 0x21;
  static const int _8259A_ICW3_ADDR = 0x21;
  static const int _8259A_ICW4_ADDR = 0x21;
  static const int _8259A_OCW1_ADDR = 0x21;
  static const int _8259A_OCW2_ADDR = 0x20;
  static const int _8259A_OCW3_ADDR = 0x20;
  // Programmable Interval Timer
  static const int _8253_BASE = ;
  static const int _8253_COUNTER0_ADDR = 0x40;
  static const int _8253_COUNTER1_ADDR = 0x41;
  static const int _8253_COUNTER2_ADDR = 0x42;
  static const int _8253_CONTROL_ADDR = 0x43;
  // Direct Memory Access Controller
  static const int _8237_BASE = ;
  static const int _8237_CHANNEL0_ADDR = 0x00;
  static const int _8237_CHANNEL1_ADDR = 0x02;
  static const int _8237_CHANNEL2_ADDR = 0x04;
  static const int _8237_CHANNEL3_ADDR = 0x06;
  static const int _8237_STATUS_ADDR = 0x08;
  static const int _8237_COMMAND_ADDR = 0x08;
  static const int _8237_REQUEST_ADDR = 0x09;
  static const int _8237_MASK_ADDR = 0x0A;
  static const int _8237_MODE_ADDR = 0x0B;
  // Keyboard Controller
  static const int _8042_BASE = ;
  static const int _8042_DATA_ADDR = 0x60;
  static const int _8042_STATUS_ADDR = 0x64;
  // Real-Time Clock with CMOS RAM
  static const int CMOS_BASE = ;
  static const int CMOS_ADDRESS_ADDR = 0x70;
  static const int CMOS_DATA_ADDR = 0x71;
  // Floppy Disk Controller
  static const int FDC_BASE = ;
  static const int FDC_SRA_ADDR = 0x3F2;
  static const int FDC_MSR_ADDR = 0x3F4;
  static const int FDC_DATA_ADDR = 0x3F5;
  static const int FDC_DIR_ADDR = 0x3F7;
  static const int FDC_CCR_ADDR = 0x3F7;
  // Hard Disk Controller (ST-506/412)
  static const int HDC_BASE = ;
  static const int HDC_DATA_ADDR = 0x1F0;
  static const int HDC_ERROR_ADDR = 0x1F1;
  static const int HDC_SECTORCOUNT_ADDR = 0x1F2;
  static const int HDC_SECTORNUMBER_ADDR = 0x1F3;
  static const int HDC_CYLINDERLOW_ADDR = 0x1F4;
  static const int HDC_CYLINDERHIGH_ADDR = 0x1F5;
  static const int HDC_DRIVEHEAD_ADDR = 0x1F6;
  static const int HDC_STATUS_ADDR = 0x1F7;
  static const int HDC_COMMAND_ADDR = 0x1F7;
  // Color Graphics Adapter
  static const int CGA_BASE = ;
  static const int CGA_CRTC_INDEX_ADDR = 0x3D4;
  static const int CGA_CRTC_DATA_ADDR = 0x3D5;
  static const int CGA_MODECONTROL_ADDR = 0x3D8;
  static const int CGA_COLORSELECT_ADDR = 0x3D9;
  static const int CGA_STATUS_ADDR = 0x3DA;
  // Enhanced Graphics Adapter
  static const int EGA_BASE = ;
  static const int EGA_CRTC_INDEX_ADDR = 0x3D4;
  static const int EGA_CRTC_DATA_ADDR = 0x3D5;
  static const int EGA_FEATURECONTROL_ADDR = 0x3DA;
  static const int EGA_GRAPHICS1POS_ADDR = 0x3CC;
  static const int EGA_GRAPHICS2POS_ADDR = 0x3CA;
  static const int EGA_SEQUENCERINDEX_ADDR = 0x3C4;
  static const int EGA_SEQUENCERDATA_ADDR = 0x3C5;
  static const int EGA_GRAPHICSINDEX_ADDR = 0x3CE;
  static const int EGA_GRAPHICSDATA_ADDR = 0x3CF;
  static const int EGA_ATTRIBUTEINDEX_ADDR = 0x3C0;
  static const int EGA_ATTRIBUTEDATA_ADDR = 0x3C1;
  // Video Graphics Array
  static const int VGA_BASE = ;
  static const int VGA_CRTC_INDEX_ADDR = 0x3D4;
  static const int VGA_CRTC_DATA_ADDR = 0x3D5;
  static const int VGA_INPUTSTATUS1_ADDR = 0x3DA;
  static const int VGA_FEATURECONTROL_ADDR = 0x3DA;
  static const int VGA_MISCOUTPUT_ADDR = 0x3C2;
  static const int VGA_SEQUENCERINDEX_ADDR = 0x3C4;
  static const int VGA_SEQUENCERDATA_ADDR = 0x3C5;
  static const int VGA_GRAPHICSINDEX_ADDR = 0x3CE;
  static const int VGA_GRAPHICSDATA_ADDR = 0x3CF;
  static const int VGA_ATTRIBUTEINDEX_ADDR = 0x3C0;
  static const int VGA_ATTRIBUTEDATA_ADDR = 0x3C1;
  static const int VGA_DACMASK_ADDR = 0x3C6;
  static const int VGA_DACREADINDEX_ADDR = 0x3C7;
  static const int VGA_DACWRITEINDEX_ADDR = 0x3C8;
  static const int VGA_DACDATA_ADDR = 0x3C9;
  // Game Port
  static const int GAMEPORT_BASE = ;
  static const int GAMEPORT_DATA_ADDR = 0x201;
  // Parallel Printer Port
  static const int PARALLELPORT_BASE = ;
  static const int PARALLELPORT_DATA_ADDR = 0x378;
  static const int PARALLELPORT_STATUS_ADDR = 0x379;
  static const int PARALLELPORT_CONTROL_ADDR = 0x37A;
  // Serial Communications Port
  static const int SERIALPORT_BASE = ;
  static const int SERIALPORT_DATA_ADDR = 0x3F8;
  static const int SERIALPORT_IER_ADDR = 0x3F9;
  static const int SERIALPORT_IIR_ADDR = 0x3FA;
  static const int SERIALPORT_LCR_ADDR = 0x3FB;
  static const int SERIALPORT_MCR_ADDR = 0x3FC;
  static const int SERIALPORT_LSR_ADDR = 0x3FD;
  static const int SERIALPORT_MSR_ADDR = 0x3FE;
  static const int SERIALPORT_SCR_ADDR = 0x3FF;
  // PC Speaker
  static const int SPEAKER_BASE = ;
  static const int SPEAKER_CONTROL_ADDR = 0x61;

  // 中断向量定义
  static const int INT_DIVIDE_ERROR = 0;  // Division by zero
  static const int INT_SINGLE_STEP = 1;  // Debug single step
  static const int INT_NMI = 2;  // Non-maskable interrupt
  static const int INT_BREAKPOINT = 3;  // INT 3 instruction
  static const int INT_OVERFLOW = 4;  // INTO instruction
  static const int INT_PRINT_SCREEN = 5;  // Print screen key
  static const int INT_IRQ0 = 8;  // Timer interrupt
  static const int INT_IRQ1 = 9;  // Keyboard interrupt
  static const int INT_IRQ2 = 10;  // Cascade to IRQ8-15
  static const int INT_IRQ3 = 11;  // COM2 interrupt
  static const int INT_IRQ4 = 12;  // COM1 interrupt
  static const int INT_IRQ5 = 13;  // LPT2 interrupt
  static const int INT_IRQ6 = 14;  // Floppy disk interrupt
  static const int INT_IRQ7 = 15;  // LPT1 interrupt
  static const int INT_IRQ8 = 112;  // Real-time clock interrupt
  static const int INT_IRQ9 = 113;  // Redirected IRQ2
  static const int INT_IRQ10 = 114;  // Reserved
  static const int INT_IRQ11 = 115;  // Reserved
  static const int INT_IRQ12 = 116;  // PS/2 mouse interrupt
  static const int INT_IRQ13 = 117;  // Coprocessor interrupt
  static const int INT_IRQ14 = 118;  // Primary IDE interrupt
  static const int INT_IRQ15 = 119;  // Secondary IDE interrupt
  static const int INT_VIDEO_SERVICES = 16;  // Video BIOS services
  static const int INT_DISK_SERVICES = 19;  // Disk BIOS services
  static const int INT_DOS_SERVICES = 21;  // DOS function calls

}
