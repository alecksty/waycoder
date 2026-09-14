/**
 * DS18B20 寄存器定义
 * 生成自: Maxim/Dallas/Sensor/DS18B20
 * 版本: 1.0
 */
export const ds18b20 = {
  // CPU: Sensor, 8位, 100000 Hz

  // 内存段
  // Scratchpad memory (9 bytes)
  SCRATCHPAD_START: 0x00,
  SCRATCHPAD_END: 0x08,
  SCRATCHPAD_SIZE: 9,
  // EEPROM (TH, TL, config bytes)
  EEPROM_START: 0x00,
  EEPROM_END: 0x02,
  EEPROM_SIZE: 3,

  // 外设定义
  // DS18B20 1-Wire Thermometer (3.0V-5.5V, TO-92)
  DS18B20_BASE: 0x00,
  DS18B20_TEMP_LSB: 0x00000000,
  DS18B20_TEMP_MSB: 0x00000001,
  DS18B20_TH_REG: 0x00000002,
  DS18B20_TL_REG: 0x00000003,
  DS18B20_CONFIG: 0x00000004,
  DS18B20_CONFIG_R0: 5,  // Resolution select bit 0
  DS18B20_CONFIG_R1: 6,  // Resolution select bit 1 (00=9bit,10=10bit,01=11bit,11=12bit)
  DS18B20_COUNT_REMAIN: 0x00000006,
  DS18B20_COUNT_PER_C: 0x00000007,
  DS18B20_CRC: 0x00000008,

  init: function() {
    // 硬件初始化
  }
};
