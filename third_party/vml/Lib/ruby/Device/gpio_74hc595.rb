# 74HC595 设备定义 - Ruby 模块
# 生成自: TI/NXP/GPIO/74HC595
# 版本: 1.0
# 日期: 2026-05-06
# 作者: VML Team
# 描述: 74HC595 8-bit Shift Register (SPI-compatible, serial-in parallel-out, daisy-chainable)
# CPU架构: GPIO
# 位宽: 8位
# 时钟频率: 10000000 Hz

module 74HC595

  # 外设定义
  # 74HC595 8-bit Shift Register (2V-6V, DIP-16)
  _74HC595_BASE = 0x00
  _74HC595_DATA_ADDR = 0x00
  _74HC595_LATCH_ADDR = 0x01
  _74HC595_CHAIN_COUNT_ADDR = 0x02
  _74HC595_OE_ADDR = 0x03
  _74HC595_CLEAR_ADDR = 0x04

end
