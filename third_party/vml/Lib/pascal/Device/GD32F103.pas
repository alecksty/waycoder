unit gd32f103;

interface

// GD32F103寄存器定义
// 生成自: GigaDevice/GD32/GD32F103
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M3 MCU, 108MHz, STM32F103 compatible

// CPU架构: ARM-Cortex-M3
// 位宽: 32位
// 时钟频率: 108000000 Hz

const

  // 寄存器定义
  R0 = 0x00;

  R1 = 0x04;

  R2 = 0x08;

  R3 = 0x0C;

  R4 = 0x10;

  R5 = 0x14;

  SP = 0x34;

  LR = 0x38;

  PC = 0x3C;

  // 内存段定义
  FLASH_START = 0x08000000;
  FLASH_END = 0x0801FFFF;
  FLASH_SIZE = 131072;

  SRAM_START = 0x20000000;
  SRAM_END = 0x20004FFF;
  SRAM_SIZE = 20480;

  PERIPHERAL_START = 0x40000000;
  PERIPHERAL_END = 0x4003FFFF;
  PERIPHERAL_SIZE = 262144;

  // 外设定义
  // Reset and Clock Control
  RCC_BASE = 0x40021000;
  RCC_CTLR = 0x00;
  RCC_CFGR0 = 0x04;
  RCC_APB2PCENR = 0x18;
  RCC_APB2PCENR_IOPAEN = 2;  // GPIOA clock enable
  RCC_APB2PCENR_IOPBEN = 3;  // GPIOB clock enable
  RCC_APB2PCENR_IOPCEN = 4;  // GPIOC clock enable
  RCC_APB2PCENR_USART0EN = 14;  // USART0 clock enable
  RCC_APB1PCENR = 0x1C;
  RCC_APB1PCENR_USART1EN = 17;  // USART1 clock enable

  // General Purpose I/O Port A
  GPIOA_BASE = 0x40010800;
  GPIOA_CTL0 = 0x00;
  GPIOA_CTL1 = 0x04;
  GPIOA_ISTAT = 0x08;
  GPIOA_OCTL = 0x0C;
  GPIOA_BOP = 0x10;
  GPIOA_BC = 0x14;

  // General Purpose I/O Port B
  GPIOB_BASE = 0x40010C00;
  GPIOB_CTL0 = 0x00;
  GPIOB_CTL1 = 0x04;
  GPIOB_ISTAT = 0x08;
  GPIOB_OCTL = 0x0C;
  GPIOB_BOP = 0x10;
  GPIOB_BC = 0x14;

  // General Purpose I/O Port C
  GPIOC_BASE = 0x40011000;
  GPIOC_CTL0 = 0x00;
  GPIOC_CTL1 = 0x04;
  GPIOC_ISTAT = 0x08;
  GPIOC_OCTL = 0x0C;
  GPIOC_BOP = 0x10;
  GPIOC_BC = 0x14;

  // USART0
  USART0_BASE = 0x40013800;
  USART0_STATR = 0x00;
  USART0_DATAR = 0x04;
  USART0_BRR = 0x08;
  USART0_CTLR1 = 0x0C;

  // USART1
  USART1_BASE = 0x40004400;
  USART1_STATR = 0x00;
  USART1_DATAR = 0x04;
  USART1_BRR = 0x08;
  USART1_CTLR1 = 0x0C;

  // 中断向量定义
  RESET_VECTOR = 0;  // 
  SVCALL_VECTOR = 11;  // 
  USART0_VECTOR = 25;  // USART0 Global Interrupt
  USART1_VECTOR = 37;  // USART1 Global Interrupt

type
  TGD32F103 = record
    // 设备状态记录
  end;

// 设备初始化函数
procedure gd32f103_init;

// 常用函数
function read_register(addr: Word): Byte;
procedure write_register(addr: Word; value: Byte);

implementation

procedure gd32f103_init;
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
