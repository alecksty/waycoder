unit ibm_pc_at;

interface

// IBM PC/AT寄存器定义
// 生成自: IBM/IBM PC/IBM PC/AT
// 版本: 
// 日期: 
// 作者: 
// 描述: IBM Personal Computer/Advanced Technology (Model 5170)

// CPU架构: x86-16
// 位宽: 0位
// 时钟频率: 0 Hz

const

  // 外设定义
  // Programmable Interrupt Controller
  _8259A_BASE = ;
  _8259A_ICW1 = 0x20;
  _8259A_ICW2 = 0x21;
  _8259A_ICW3 = 0x21;
  _8259A_ICW4 = 0x21;
  _8259A_OCW1 = 0x21;
  _8259A_OCW2 = 0x20;
  _8259A_OCW3 = 0x20;

  // Programmable Interval Timer
  _8253_BASE = ;
  _8253_COUNTER0 = 0x40;
  _8253_COUNTER1 = 0x41;
  _8253_COUNTER2 = 0x42;
  _8253_CONTROL = 0x43;

  // Direct Memory Access Controller
  _8237_BASE = ;
  _8237_CHANNEL0 = 0x00;
  _8237_CHANNEL1 = 0x02;
  _8237_CHANNEL2 = 0x04;
  _8237_CHANNEL3 = 0x06;
  _8237_STATUS = 0x08;
  _8237_COMMAND = 0x08;
  _8237_REQUEST = 0x09;
  _8237_MASK = 0x0A;
  _8237_MODE = 0x0B;

  // Keyboard Controller
  _8042_BASE = ;
  _8042_DATA = 0x60;
  _8042_STATUS = 0x64;

  // Real-Time Clock with CMOS RAM
  CMOS_BASE = ;
  CMOS_ADDRESS = 0x70;
  CMOS_DATA = 0x71;

  // Floppy Disk Controller
  FDC_BASE = ;
  FDC_SRA = 0x3F2;
  FDC_MSR = 0x3F4;
  FDC_DATA = 0x3F5;
  FDC_DIR = 0x3F7;
  FDC_CCR = 0x3F7;

  // Hard Disk Controller (ST-506/412)
  HDC_BASE = ;
  HDC_DATA = 0x1F0;
  HDC_ERROR = 0x1F1;
  HDC_SECTORCOUNT = 0x1F2;
  HDC_SECTORNUMBER = 0x1F3;
  HDC_CYLINDERLOW = 0x1F4;
  HDC_CYLINDERHIGH = 0x1F5;
  HDC_DRIVEHEAD = 0x1F6;
  HDC_STATUS = 0x1F7;
  HDC_COMMAND = 0x1F7;

  // Color Graphics Adapter
  CGA_BASE = ;
  CGA_CRTC_INDEX = 0x3D4;
  CGA_CRTC_DATA = 0x3D5;
  CGA_MODECONTROL = 0x3D8;
  CGA_COLORSELECT = 0x3D9;
  CGA_STATUS = 0x3DA;

  // Enhanced Graphics Adapter
  EGA_BASE = ;
  EGA_CRTC_INDEX = 0x3D4;
  EGA_CRTC_DATA = 0x3D5;
  EGA_FEATURECONTROL = 0x3DA;
  EGA_GRAPHICS1POS = 0x3CC;
  EGA_GRAPHICS2POS = 0x3CA;
  EGA_SEQUENCERINDEX = 0x3C4;
  EGA_SEQUENCERDATA = 0x3C5;
  EGA_GRAPHICSINDEX = 0x3CE;
  EGA_GRAPHICSDATA = 0x3CF;
  EGA_ATTRIBUTEINDEX = 0x3C0;
  EGA_ATTRIBUTEDATA = 0x3C1;

  // Video Graphics Array
  VGA_BASE = ;
  VGA_CRTC_INDEX = 0x3D4;
  VGA_CRTC_DATA = 0x3D5;
  VGA_INPUTSTATUS1 = 0x3DA;
  VGA_FEATURECONTROL = 0x3DA;
  VGA_MISCOUTPUT = 0x3C2;
  VGA_SEQUENCERINDEX = 0x3C4;
  VGA_SEQUENCERDATA = 0x3C5;
  VGA_GRAPHICSINDEX = 0x3CE;
  VGA_GRAPHICSDATA = 0x3CF;
  VGA_ATTRIBUTEINDEX = 0x3C0;
  VGA_ATTRIBUTEDATA = 0x3C1;
  VGA_DACMASK = 0x3C6;
  VGA_DACREADINDEX = 0x3C7;
  VGA_DACWRITEINDEX = 0x3C8;
  VGA_DACDATA = 0x3C9;

  // Game Port
  GAMEPORT_BASE = ;
  GAMEPORT_DATA = 0x201;

  // Parallel Printer Port
  PARALLELPORT_BASE = ;
  PARALLELPORT_DATA = 0x378;
  PARALLELPORT_STATUS = 0x379;
  PARALLELPORT_CONTROL = 0x37A;

  // Serial Communications Port
  SERIALPORT_BASE = ;
  SERIALPORT_DATA = 0x3F8;
  SERIALPORT_IER = 0x3F9;
  SERIALPORT_IIR = 0x3FA;
  SERIALPORT_LCR = 0x3FB;
  SERIALPORT_MCR = 0x3FC;
  SERIALPORT_LSR = 0x3FD;
  SERIALPORT_MSR = 0x3FE;
  SERIALPORT_SCR = 0x3FF;

  // PC Speaker
  SPEAKER_BASE = ;
  SPEAKER_CONTROL = 0x61;

  // 中断向量定义
  DIVIDE_ERROR_VECTOR = 0;  // Division by zero
  SINGLE_STEP_VECTOR = 1;  // Debug single step
  NMI_VECTOR = 2;  // Non-maskable interrupt
  BREAKPOINT_VECTOR = 3;  // INT 3 instruction
  OVERFLOW_VECTOR = 4;  // INTO instruction
  PRINT_SCREEN_VECTOR = 5;  // Print screen key
  IRQ0_VECTOR = 8;  // Timer interrupt
  IRQ1_VECTOR = 9;  // Keyboard interrupt
  IRQ2_VECTOR = 10;  // Cascade to IRQ8-15
  IRQ3_VECTOR = 11;  // COM2 interrupt
  IRQ4_VECTOR = 12;  // COM1 interrupt
  IRQ5_VECTOR = 13;  // LPT2 interrupt
  IRQ6_VECTOR = 14;  // Floppy disk interrupt
  IRQ7_VECTOR = 15;  // LPT1 interrupt
  IRQ8_VECTOR = 112;  // Real-time clock interrupt
  IRQ9_VECTOR = 113;  // Redirected IRQ2
  IRQ10_VECTOR = 114;  // Reserved
  IRQ11_VECTOR = 115;  // Reserved
  IRQ12_VECTOR = 116;  // PS/2 mouse interrupt
  IRQ13_VECTOR = 117;  // Coprocessor interrupt
  IRQ14_VECTOR = 118;  // Primary IDE interrupt
  IRQ15_VECTOR = 119;  // Secondary IDE interrupt
  VIDEO_SERVICES_VECTOR = 16;  // Video BIOS services
  DISK_SERVICES_VECTOR = 19;  // Disk BIOS services
  DOS_SERVICES_VECTOR = 21;  // DOS function calls

type
  TIBM PC/AT = record
    // 设备状态记录
  end;

// 设备初始化函数
procedure ibm_pc_at_init;

// 常用函数
function read_register(addr: Word): Byte;
procedure write_register(addr: Word; value: Byte);

implementation

procedure ibm_pc_at_init;
begin
  // 初始化代码
end;

function read_register(addr: Word): Byte;
begin
  // 读取寄存器值
  Result := 0;
end;

procedure write_register(addr: Word; value: Byte);
begin
  // 写入寄存器值
end;

end.
