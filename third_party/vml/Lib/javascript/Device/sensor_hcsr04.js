/**
 * HC_SR04 寄存器定义
 * 生成自: Generic/Sensor/HC_SR04
 * 版本: 1.0
 */
export const hc_sr04 = {
  // CPU: Sensor, 8位, 0 Hz

  // 内存段
  // PCB Module (45x20x15mm)
  PACKAGE_START: 0x00,
  PACKAGE_END: 0x00,
  PACKAGE_SIZE: 0,

  // 外设定义
  // HC-SR04 Ultrasonic Sensor (4.5V-5.5V)
  HC_SR04_BASE: 0x00,
  HC_SR04_TRIG: 0x00000000,
  HC_SR04_DISTANCE_H: 0x00000001,
  HC_SR04_DISTANCE_L: 0x00000002,
  HC_SR04_STATUS: 0x00000003,
  HC_SR04_STATUS_BUSY: 0,  // 1=Measurement in progress
  HC_SR04_STATUS_VALID: 1,  // 1=Valid measurement available
  HC_SR04_STATUS_TIMEOUT: 2,  // 1=No echo received (out of range)

  init: function() {
    // 硬件初始化
  }
};
