/**
 * MCP4921 寄存器定义
 * 生成自: Microchip/DAC/MCP4921
 * 版本: 1.0
 */
export const mcp4921 = {
  // CPU: DAC, 12位, 20000000 Hz

  // 外设定义
  // MCP4921 12-bit DAC (SPI, 2.7V-5.5V)
  MCP4921_BASE: 0x00,
  MCP4921_DAC_VALUE: 0x00000000,
  MCP4921_DAC_VALUE_BUF: 14,  // VREF buffer (0=unbuffered, 1=buffered)
  MCP4921_DAC_VALUE_GA: 13,  // Gain (0=2x, 1=1x)
  MCP4921_DAC_VALUE_SHDN: 12,  // Shutdown (0=shutdown, 1=active)
  MCP4921_VREF: 0x00000002,

  init: function() {
    // 硬件初始化
  }
};
