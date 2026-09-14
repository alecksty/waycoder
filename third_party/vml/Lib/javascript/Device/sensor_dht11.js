/**
 * DHT11 寄存器定义
 * 生成自: Aosong/Sensor/DHT11
 * 版本: 1.0
 */
export const dht11 = {
  // CPU: Sensor, 8位, 500000 Hz

  // 内存段
  // DIP-4/SMD-4
  PACKAGE_START: 0x00,
  PACKAGE_END: 0x00,
  PACKAGE_SIZE: 4,

  // 外设定义
  // DHT11 1-Wire Sensor (3.0V-5.5V)
  DHT11_BASE: 0x00,
  DHT11_HUMIDITY_INT: 0x00000000,
  DHT11_HUMIDITY_DEC: 0x00000001,
  DHT11_TEMP_INT: 0x00000002,
  DHT11_TEMP_DEC: 0x00000003,
  DHT11_CHECKSUM: 0x00000004,

  init: function() {
    // 硬件初始化
  }
};
