/**
 * APDS9960 寄存器定义
 * 生成自: Broadcom/Avago/Sensor/APDS9960
 * 版本: 1.0
 */
export const apds9960 = {
  // CPU: Sensor, 8位, 400000 Hz

  // 外设定义
  // APDS9960 Gesture/RGB Sensor (0x39, 3.3V)
  APDS9960_BASE: 0x39,
  APDS9960_ENABLE: 0x000000B9,
  APDS9960_GESTURE: 0x00000135,
  APDS9960_PROXIMITY: 0x000000D5,
  APDS9960_AMBIENT: 0x000000CF,
  APDS9960_RED: 0x000000D1,
  APDS9960_GREEN: 0x000000D3,
  APDS9960_BLUE: 0x000000D5,
  APDS9960_GESTURE_FIFO: 0x00000135,
  APDS9960_GESTURE_COUNT: 0x00000136,

  // 中断向量
  IRQ_INT: 0,  // Gesture/Proximity/Light interrupt

  init: function() {
    // 硬件初始化
  }
};
