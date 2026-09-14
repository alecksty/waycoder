unit acorn_archimedes_a310;

interface

// Acorn-Archimedes-A310寄存器定义
// 生成自: Acorn Computers/Archimedes/Acorn-Archimedes-A310
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Acorn Archimedes A310 - First ARM-based home computer with RISC OS, ARM250 @ 26MHz

// CPU架构: ARM250
// 位宽: 32位
// 时钟频率: 26000000 Hz

const

  // 寄存器定义
  // General Purpose Register 0
  R0 = 0x00;

  // General Purpose Register 1
  R1 = 0x04;

  // General Purpose Register 2
  R2 = 0x08;

  // General Purpose Register 3
  R3 = 0x0C;

  // General Purpose Register 4
  R4 = 0x10;

  // General Purpose Register 5
  R5 = 0x14;

  // General Purpose Register 6
  R6 = 0x18;

  // General Purpose Register 7
  R7 = 0x1C;

  // General Purpose Register 8
  R8 = 0x20;

  // General Purpose Register 9
  R9 = 0x24;

  // General Purpose Register 10
  R10 = 0x28;

  // General Purpose Register 11 (fp)
  R11 = 0x2C;

  // General Purpose Register 12
  R12 = 0x30;

  // Stack Pointer (R13)
  SP = 0x34;

  // Link Register (R14)
  LR = 0x38;

  // Program Counter (R15)
  PC = 0x3C;

  // Processor Status Register
  PSR = 0x40;
  PSR_MODE = 0;  // Mode bits (0-4)
  PSR_T = 5;  // Thumb state
  PSR_F = 6;  // FIQ disable
  PSR_I = 7;  // IRQ disable
  PSR_V = 28;  // Overflow
  PSR_C = 29;  // Carry
  PSR_Z = 30;  // Zero
  PSR_N = 31;  // Negative

  // 内存段定义
  // RISC OS ROM (512KB)
  ROM_START = 0x00000000;
  ROM_END = 0x0007FFFF;
  ROM_SIZE = 524288;

  // Main RAM (up to 4MB)
  RAM_START = 0x00080000;
  RAM_END = 0x003FFFFF;
  RAM_SIZE = 3932160;

  // Video RAM (4MB, VIDC)
  VRAM_START = 0x00400000;
  VRAM_END = 0x007FFFFF;
  VRAM_SIZE = 4194304;

  // I/O controller (IOC)
  IO_START = 0x03000000;
  IO_END = 0x0301FFFF;
  IO_SIZE = 131072;

  // Memory Controller (MEMC)
  MEMC_START = 0x03200000;
  MEMC_END = 0x0320FFFF;
  MEMC_SIZE = 4096;

  // Video Controller (VIDC)
  VIDC_START = 0x03400000;
  VIDC_END = 0x0340FFFF;
  VIDC_SIZE = 4096;

  // I/O and Memory DMA
  IOMD_START = 0x03300000;
  IOMD_END = 0x0330FFFF;
  IOMD_SIZE = 4096;

  // 外设定义
  // I/O Controller (IOC) - Interrupt/Keyboard/RTC
  IOC_BASE = 0x03000000;
  IOC_IOC_TIMER1 = 0x03000000;
  IOC_IOC_TIMER2 = 0x03000004;
  IOC_IOC_IOSEL = 0x03000008;
  IOC_IOC_IRQST = 0x0300000C;
  IOC_IOC_IRQLATCH = 0x03000010;
  IOC_IOC_FIQST = 0x03000014;
  IOC_IOC_FIQEN = 0x03000018;
  IOC_IOC_IRQEN = 0x0300001C;
  IOC_IOC_KBDDATA = 0x03000020;
  IOC_IOC_KBDCR = 0x03000024;
  IOC_IOC_RTCDR = 0x03000028;
  IOC_IOC_RTCCR = 0x0300002C;
  IOC_IOC_PRST = 0x03000030;
  IOC_IOC_PORTA = 0x03000034;
  IOC_IOC_PORTB = 0x03000038;
  IOC_IOC_PORTC = 0x0300003C;

  // Memory Controller (MEMC1)
  MEMC_BASE = 0x03200000;
  MEMC_MEMC_PT = 0x03200000;
  MEMC_MEMC_CTRL = 0x03200004;
  MEMC_MEMC_DRAM = 0x03200008;
  MEMC_MEMC_ERR = 0x0320000C;

  // Video Controller - VIDC1
  VIDC_BASE = 0x03400000;
  VIDC_VIDC_PALETTE = 0x03400000;
  VIDC_VIDC_STARTL = 0x03400004;
  VIDC_VIDC_STARTH = 0x03400008;
  VIDC_VIDC_CONFIG = 0x0340000C;
  VIDC_VIDC_HDISP = 0x03400010;
  VIDC_VIDC_VDISP = 0x03400014;
  VIDC_VIDC_HSYNC = 0x03400018;
  VIDC_VIDC_VSYNC = 0x0340001C;
  VIDC_VIDC_BORDER = 0x03400020;
  VIDC_VIDC_CURSOR = 0x03400024;
  VIDC_VIDC_SOUND = 0x03400028;

  // Intel 82710 Floppy Disk Controller
  FDC_BASE = 0x03010000;
  FDC_FDC_STATUS = 0x03010000;
  FDC_FDC_COMMAND = 0x03010000;
  FDC_FDC_TRACK = 0x03010004;
  FDC_FDC_SECTOR = 0x03010008;
  FDC_FDC_DATA = 0x0301000C;

  // Serial Port (via IOC)
  SERIAL_BASE = 0x03010010;
  SERIAL_SERIAL_TX = 0x03010010;
  SERIAL_SERIAL_RX = 0x03010014;
  SERIAL_SERIAL_CTRL = 0x03010018;

  // 中断向量定义
  RESET_VECTOR = 0;  // Reset
  UND_VECTOR = 1;  // Undefined instruction
  SWI_VECTOR = 2;  // Software Interrupt (SWI/SVC)
  PABORT_VECTOR = 3;  // Prefetch Abort
  DABORT_VECTOR = 4;  // Data Abort
  ADDRESS_VECTOR = 5;  // Address Exception
  IRQ_VECTOR = 6;  // IRQ interrupt (IOC)
  FIQ_VECTOR = 7;  // FIQ interrupt (VIDC)

  // 引脚定义
  PIN_VCC = 1;  // +5V Power
  PIN_GND = 2;  // Ground
  PIN_CLK = 3;  // ARM clock (26MHz)
  PIN_NRESET = 4;  // Reset (active low)
  PIN_NMREQ = 5;  // Memory Request (active low)
  PIN_NIORQ = 6;  // I/O Request (active low)
  PIN_NRW = 7;  // Read/Write (0=write, 1=read)
  PIN_MAS0 = 8;  // Master address bit 0
  PIN_MAS1 = 9;  // Master address bit 1
  PIN_MAS2 = 10;  // Master address bit 2
  PIN_LOCK = 11;  // Bus lock
  PIN_NMREQ = 12;  // Memory request (active low)
  PIN_NWAIT = 13;  // Wait state (active low)
  PIN_NIRQLINE = 14;  // IRQ line (active low)
  PIN_NFIRQLINE = 15;  // FIQ line (active low)
  PIN_A1_A25 = 16;  // Address Bus (26-bit)
  PIN_D0_D31 = 17;  // Data Bus (32-bit)

type
  TAcorn-Archimedes-A310 = record
    // 设备状态记录
  end;

// 设备初始化函数
procedure acorn_archimedes_a310_init;

// 常用函数
function read_register(addr: Word): Byte;
procedure write_register(addr: Word; value: Byte);

implementation

procedure acorn_archimedes_a310_init;
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
