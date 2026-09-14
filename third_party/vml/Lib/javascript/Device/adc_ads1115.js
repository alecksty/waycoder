/**
 * ADS1115 寄存器定义
 * 生成自: Texas Instruments/ADC/ADS1115
 * 版本: 1.0
 */
export const ads1115 = {
  // CPU: ADC, 16位, 400000 Hz

  // 外设定义
  // ADS1115 16-bit ADC (0x48-0x4B, 2.0V-5.5V)
  ADS1115_BASE: 0x48,
  ADS1115_CONV_RESULT: 0x00000048,
  ADS1115_CONFIG: 0x00000049,
  ADS1115_CONFIG_OS: 15,  // Operational status/start single-shot
  ADS1115_CONFIG_MUX: 12,  // Input multiplexer: 0=A0-A1,1=A0-A3,2=A1-A3,3=A2-A3,4=A0,5=A1,6=A2,7=A3
  ADS1115_CONFIG_PGA: 9,  // PGA gain: 0=±6.144V,1=±4.096V,2=±2.048V,3=±1.024V,4=±0.512V,5=±0.256V
  ADS1115_CONFIG_MODE: 8,  // 0=continuous, 1=single-shot
  ADS1115_CONFIG_DR: 5,  // Data rate: 0=8,1=16,2=32,3=64,4=128,5=250,6=475,7=860 SPS
  ADS1115_CONFIG_COMP_MODE: 4,  // Comparator mode (0=traditional, 1=window)
  ADS1115_CONFIG_COMP_POL: 3,  // Comparator polarity (0=active low, 1=active high)
  ADS1115_LO_THRESH: 0x0000004A,
  ADS1115_HI_THRESH: 0x0000004B,

  init: function() {
    // 硬件初始化
  }
};
