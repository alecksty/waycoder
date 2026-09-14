# MCP4725 设备定义 - Ruby 模块
# 生成自: Microchip/DAC/MCP4725
# 版本: 1.0
# 日期: 2026-05-06
# 作者: VML Team
# 描述: MCP4725 12-bit I2C DAC (single channel, EEPROM)
# CPU架构: DAC
# 位宽: 12位
# 时钟频率: 400000 Hz

module MCP4725

  # 内存段定义
  EEPROM_START = 0x00
  EEPROM_END = 0x01
  EEPROM_SIZE = 2  # Power-on default DAC value

  # 外设定义
  # MCP4725 12-bit DAC (0x60-0x67, 2.7V-5.5V)
  MCP4725_BASE = 0x60
  MCP4725_DAC_VALUE_ADDR = 0x00
  MCP4725_DAC_VALUE_PD_BIT = 12  # Power-down: 0=normal,1=1kΩ,2=100kΩ,3=500kΩ
  MCP4725_WRITE_EEPROM_ADDR = 0x60

end
