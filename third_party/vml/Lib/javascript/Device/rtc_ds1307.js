/**
 * DS1307 寄存器定义
 * 生成自: Maxim/Dallas/RTC/DS1307
 * 版本: 1.0
 */
export const ds1307 = {
  // CPU: RTC, 8位, 100000 Hz

  // 内存段
  // Non-volatile RAM (56 bytes)
  NVRAM_START: 0x08,
  NVRAM_END: 0x3F,
  NVRAM_SIZE: 56,

  // 外设定义
  // DS1307 RTC (0x68, 5V, DIP-8)
  DS1307_BASE: 0x68,
  DS1307_SEC: 0x00000068,
  DS1307_MIN: 0x00000069,
  DS1307_HOUR: 0x0000006A,
  DS1307_DAY: 0x0000006B,
  DS1307_DATE: 0x0000006C,
  DS1307_MONTH: 0x0000006D,
  DS1307_YEAR: 0x0000006E,
  DS1307_CTRL: 0x0000006F,

  init: function() {
    // 硬件初始化
  }
};
