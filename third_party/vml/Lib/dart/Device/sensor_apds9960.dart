// APDS9960 设备定义 - Dart 库
// 生成自: Broadcom/Avago/Sensor/APDS9960
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: APDS9960 Gesture/Proximity/Ambient Light/RGB Sensor (I2C)
// CPU架构: Sensor
// 位宽: 8位
// 时钟频率: 400000 Hz

class APDS9960Device {
  static const String deviceName = "APDS9960";
  static const String manufacturer = "Broadcom/Avago";
  static const String family = "Sensor";
  static const String version = "1.0";
  static const String architecture = "Sensor";
  static const int bits = 8;
  static const int clockFrequency = 400000;

  // 外设定义
  // APDS9960 Gesture/RGB Sensor (0x39, 3.3V)
  static const int APDS9960_BASE = 0x39;
  static const int APDS9960_ENABLE_ADDR = 0x80;
  static const int APDS9960_GESTURE_ADDR = 0xFC;
  static const int APDS9960_PROXIMITY_ADDR = 0x9C;
  static const int APDS9960_AMBIENT_ADDR = 0x96;
  static const int APDS9960_RED_ADDR = 0x98;
  static const int APDS9960_GREEN_ADDR = 0x9A;
  static const int APDS9960_BLUE_ADDR = 0x9C;
  static const int APDS9960_GESTURE_FIFO_ADDR = 0xFC;
  static const int APDS9960_GESTURE_COUNT_ADDR = 0xFD;

  // 中断向量定义
  static const int INT_INT = 0;  // Gesture/Proximity/Light interrupt

}
