unit mcp23017;

interface

// MCP23017寄存器定义
// 生成自: Microchip/GPIO/MCP23017
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: MCP23017 16-bit I2C GPIO Expander (2 banks, interrupt, 25mA per pin)

// CPU架构: GPIO
// 位宽: 16位
// 时钟频率: 400000 Hz

const

  // 外设定义
  // MCP23017 16-bit GPIO (0x20-0x27, 1.8V-5.5V)
  MCP23017_BASE = 0x20;
  MCP23017_IODIRA = 0x00;
  MCP23017_IODIRB = 0x01;
  MCP23017_GPIOA = 0x12;
  MCP23017_GPIOB = 0x13;
  MCP23017_GPINTENA = 0x04;
  MCP23017_GPINTENB = 0x05;
  MCP23017_INTCONA = 0x08;
  MCP23017_IOCON = 0x0A;
  MCP23017_GPPUA = 0x0C;
  MCP23017_GPPUB = 0x0D;

  // 中断向量定义
  INTA_VECTOR = 0;  // Port A interrupt
  INTB_VECTOR = 1;  // Port B interrupt

type
  TMCP23017 = record
    // 设备状态记录
  end;

// 设备初始化函数
procedure mcp23017_init;

// 常用函数
function read_register(addr: Word): Byte;
procedure write_register(addr: Word; value: Byte);

implementation

procedure mcp23017_init;
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
