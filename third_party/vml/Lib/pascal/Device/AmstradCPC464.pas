unit amstrad_cpc_464;

interface

// Amstrad-CPC-464寄存器定义
// 生成自: Amstrad/CPC/Amstrad-CPC-464
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Amstrad CPC 464 - British 8-bit home computer with Z80 CPU and built-in cassette recorder

// CPU架构: Z80A
// 位宽: 8位
// 时钟频率: 4000000 Hz

const

  // 寄存器定义
  // Accumulator
  A = 0x00;

  // Flags
  F = 0x01;
  F_C = 0;  // Carry
  F_N = 1;  // Subtract
  F_PV = 2;  // Parity/Overflow
  F_H = 4;  // Half Carry
  F_Z = 6;  // Zero
  F_S = 7;  // Sign

  // B Register
  B = 0x02;

  // C Register
  C = 0x03;

  // D Register
  D = 0x04;

  // E Register
  E = 0x05;

  // H Register
  H = 0x06;

  // L Register
  L = 0x07;

  // Alternate AF
  AF = 0x08;

  // Alternate BC
  BC = 0x0A;

  // Alternate DE
  DE = 0x0C;

  // Alternate HL
  HL = 0x0E;

  // Interrupt Vector
  I = 0x10;

  // Refresh
  R = 0x11;

  // Index X
  IX = 0x12;

  // Index Y
  IY = 0x14;

  // Stack Pointer
  SP = 0x16;

  // Program Counter
  PC = 0x18;

  // 内存段定义
  // Lower ROM (AMSDOS / CP/M)
  LOWER_ROM_START = 0x0000;
  LOWER_ROM_END = 0x3FFF;
  LOWER_ROM_SIZE = 16384;

  // Lower RAM bank (switchable)
  RAM_BANK0_START = 0x0000;
  RAM_BANK0_END = 0x3FFF;
  RAM_BANK0_SIZE = 16384;

  // Main RAM (32KB)
  RAM_MAIN_START = 0x4000;
  RAM_MAIN_END = 0xBFFF;
  RAM_MAIN_SIZE = 32768;

  // Upper ROM (BASIC)
  UPPER_ROM_START = 0xC000;
  UPPER_ROM_END = 0xFFFF;
  UPPER_ROM_SIZE = 16384;

  // 外设定义
  // Gate Array - Custom ASIC (video/sound/RAM control)
  GA_BASE = 0x7F00;
  GA_GA_MR = 0x7F00;
  GA_GA_IR = 0x7F01;
  GA_GA_R1 = 0x7F02;
  GA_GA_R2 = 0x7F03;
  GA_GA_R3 = 0x7F04;
  GA_GA_R4 = 0x7F05;
  GA_GA_R5 = 0x7F06;
  GA_GA_R6 = 0x7F07;
  GA_GA_R7 = 0x7F08;

  // CRT Controller 6845 - Video timing
  CRTC_BASE = 0xBC00;
  CRTC_CRTC_REG = 0xBC00;
  CRTC_CRTC_DATA = 0xBD00;
  CRTC_CRTC_H_TOTAL = 0xBC01;
  CRTC_CRTC_H_DISP = 0xBC02;
  CRTC_CRTC_HSYNC_POS = 0xBC03;
  CRTC_CRTC_HSYNC_WIDTH = 0xBC04;
  CRTC_CRTC_V_TOTAL = 0xBC05;
  CRTC_CRTC_V_TOTAL_ADJ = 0xBC06;
  CRTC_CRTC_V_DISP = 0xBC07;
  CRTC_CRTC_VSYNC_POS = 0xBC08;
  CRTC_CRTC_INTERLACE = 0xBC09;
  CRTC_CRTC_CURSOR_START = 0xBC0A;
  CRTC_CRTC_CURSOR_END = 0xBC0B;
  CRTC_CRTC_SA_HI = 0xBC0C;
  CRTC_CRTC_SA_LO = 0xBC0D;
  CRTC_CRTC_CURSOR_HI = 0xBC0E;
  CRTC_CRTC_CURSOR_LO = 0xBC0F;

  // AY-3-8912 Programmable Sound Generator
  PSG_BASE = 0xF400;
  PSG_PSG_REG = 0xF400;
  PSG_PSG_DATA = 0xF600;
  PSG_FREQ_A_LO = 0xF400;
  PSG_FREQ_A_HI = 0xF401;
  PSG_FREQ_B_LO = 0xF402;
  PSG_FREQ_B_HI = 0xF403;
  PSG_FREQ_C_LO = 0xF404;
  PSG_FREQ_C_HI = 0xF405;
  PSG_NOISE_FREQ = 0xF406;
  PSG_ENABLE = 0xF407;
  PSG_VOL_A = 0xF408;
  PSG_VOL_B = 0xF409;
  PSG_VOL_C = 0xF40A;
  PSG_ENV_FREQ_LO = 0xF40B;
  PSG_ENV_FREQ_HI = 0xF40C;
  PSG_ENV_SHAPE = 0xF40D;
  PSG_PORT_A = 0xF40E;
  PSG_PORT_B = 0xF40F;

  // WD1772 Floppy Disk Controller (via expansion)
  FDC_BASE = 0xF800;
  FDC_FDC_STATUS = 0xF8E0;
  FDC_FDC_COMMAND = 0xF8E0;
  FDC_FDC_TRACK = 0xF8E1;
  FDC_FDC_SECTOR = 0xF8E2;
  FDC_FDC_DATA = 0xF8E3;

  // Centronics Parallel Printer Port
  PRINTER_BASE = 0xEE;
  PRINTER_PRN_DATA = 0xEE;
  PRINTER_PRN_STROBE = 0xEF;

  // 中断向量定义
  RESET_VECTOR = 0;  // Power-on / Reset
  NMI_VECTOR = 1;  // Non-Maskable Interrupt
  INT_VECTOR = 2;  // Gate Array interrupt (50Hz vertical blank)

  // 引脚定义
  PIN_VCC = 1;  // +5V Power
  PIN_GND = 2;  // Ground
  PIN_CLK = 3;  // Z80 Clock (4MHz)
  PIN_A0_A15 = 4;  // Address Bus
  PIN_D0_D7 = 5;  // Data Bus
  PIN_MREQ = 6;  // Memory Request
  PIN_IORQ = 7;  // I/O Request
  PIN_RD = 8;  // Read
  PIN_WR = 9;  // Write
  PIN_INT = 10;  // Interrupt Request
  PIN_NMI = 11;  // Non-Maskable Interrupt
  PIN_RESET = 12;  // Reset

type
  TAmstrad-CPC-464 = record
    // 设备状态记录
  end;

// 设备初始化函数
procedure amstrad_cpc_464_init;

// 常用函数
function read_register(addr: Word): Byte;
procedure write_register(addr: Word; value: Byte);

implementation

procedure amstrad_cpc_464_init;
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
