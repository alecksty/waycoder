/**
 * 74HC595 寄存器定义
 * 生成自: TI/NXP/GPIO/74HC595
 * 版本: 1.0
 */
export const _74hc595 = {
  // CPU: GPIO, 8位, 10000000 Hz

  // 外设定义
  // 74HC595 8-bit Shift Register (2V-6V, DIP-16)
  74HC595_BASE: 0x00,
  74HC595_DATA: 0x00000000,
  74HC595_LATCH: 0x00000001,
  74HC595_CHAIN_COUNT: 0x00000002,
  74HC595_OE: 0x00000003,
  74HC595_CLEAR: 0x00000004,

  init: function() {
    // 硬件初始化
  }
};
