/**
 * MCP4725 寄存器定义
 * 生成自: Microchip/DAC/MCP4725
 * 版本: 1.0
 */
export const mcp4725 = {
  // CPU: DAC, 12位, 400000 Hz

  // 内存段
  // Power-on default DAC value
  EEPROM_START: 0x00,
  EEPROM_END: 0x01,
  EEPROM_SIZE: 2,

  // 外设定义
  // MCP4725 12-bit DAC (0x60-0x67, 2.7V-5.5V)
  MCP4725_BASE: 0x60,
  MCP4725_DAC_VALUE: 0x00000060,
  MCP4725_DAC_VALUE_PD: 12,  // Power-down: 0=normal,1=1kΩ,2=100kΩ,3=500kΩ
  MCP4725_WRITE_EEPROM: 0x000000C0,

  init: function() {
    // 硬件初始化
  }
};
