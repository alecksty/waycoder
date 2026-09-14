/**
 * 24C64 寄存器定义
 * 生成自: Generic/Memory/24C64
 * 版本: 1.0
 */
export const _24c64 = {
  // CPU: Memory, 8位, 400000 Hz

  // 内存段
  // EEPROM main memory array (8KB, 32-byte page write)
  EEPROM_START: 0x00,
  EEPROM_END: 0x1FFF,
  EEPROM_SIZE: 8192,

  // 外设定义
  // 24C64 I2C EEPROM (0x50-0x57, 1.7V-5.5V)
  24C64_BASE: 0x50,
  24C64_ADDR_H: 0x00000050,
  24C64_ADDR_L: 0x00000051,
  24C64_DATA: 0x00000052,
  24C64_PAGE_SIZE: 0x0000014E,
  24C64_SIZE: 0x0000014D,

  init: function() {
    // 硬件初始化
  }
};
