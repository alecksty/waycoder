/**
 * 24C02 寄存器定义
 * 生成自: Generic/Memory/24C02
 * 版本: 1.0
 */
export const _24c02 = {
  // CPU: Memory, 8位, 400000 Hz

  // 内存段
  // EEPROM main memory array (256 bytes, 8-byte page write)
  EEPROM_START: 0x00,
  EEPROM_END: 0xFF,
  EEPROM_SIZE: 256,

  // 外设定义
  // 24C02 I2C EEPROM (0x50-0x57, 1.8V-5.5V, DIP-8)
  24C02_BASE: 0x50,
  24C02_STATUS: 0x0000014F,
  24C02_STATUS_BUSY: 0,  // 1=Write in progress
  24C02_PAGE_SIZE: 0x0000014E,
  24C02_SIZE: 0x0000014D,

  init: function() {
    // 硬件初始化
  }
};
