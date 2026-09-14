/**
 * PCF8574 寄存器定义
 * 生成自: NXP/TI/GPIO/PCF8574
 * 版本: 1.0
 */
export const pcf8574 = {
  // CPU: GPIO, 8位, 100000 Hz

  // 外设定义
  // PCF8574 8-bit GPIO (0x20-0x27, 2.5V-6V)
  PCF8574_BASE: 0x20,
  PCF8574_INPUT: 0x00000020,
  PCF8574_INPUT_P0: 0,  // Pin P0
  PCF8574_INPUT_P1: 1,  // Pin P1
  PCF8574_INPUT_P2: 2,  // Pin P2
  PCF8574_INPUT_P3: 3,  // Pin P3
  PCF8574_INPUT_P4: 4,  // Pin P4
  PCF8574_INPUT_P5: 5,  // Pin P5
  PCF8574_INPUT_P6: 6,  // Pin P6
  PCF8574_INPUT_P7: 7,  // Pin P7
  PCF8574_OUTPUT: 0x00000021,
  PCF8574_POLARITY: 0x00000022,

  // 中断向量
  IRQ_INT: 0,  // Pin change interrupt (open-drain, active low)

  init: function() {
    // 硬件初始化
  }
};
