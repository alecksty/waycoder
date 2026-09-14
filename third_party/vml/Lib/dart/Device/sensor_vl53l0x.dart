// VL53L0X 设备定义 - Dart 库
// 生成自: STMicroelectronics/Sensor/VL53L0X
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: VL53L0X ToF Laser Distance Sensor (I2C, 2cm-200cm, 940nm VCSEL)
// CPU架构: Sensor
// 位宽: 16位
// 时钟频率: 400000 Hz

class VL53L0XDevice {
  static const String deviceName = "VL53L0X";
  static const String manufacturer = "STMicroelectronics";
  static const String family = "Sensor";
  static const String version = "1.0";
  static const String architecture = "Sensor";
  static const int bits = 16;
  static const int clockFrequency = 400000;

  // 外设定义
  // VL53L0X ToF Distance Sensor (0x29, 2.6V-3.5V)
  static const int VL53L0X_BASE = 0x29;
  static const int VL53L0X_DISTANCE_ADDR = 0x00;
  static const int VL53L0X_SIGNAL_RATE_ADDR = 0x02;
  static const int VL53L0X_AMBIENT_RATE_ADDR = 0x04;
  static const int VL53L0X_SPAD_COUNT_ADDR = 0x06;
  static const int VL53L0X_RANGE_STATUS_ADDR = 0x08;
  static const int VL53L0X_TIMING_BUDGET_ADDR = 0x09;
  static const int VL53L0X_INTER_MEAS_ADDR = 0x0D;
  static const int VL53L0X_MODE_ADDR = 0x0E;

  // 引脚定义
  static const int PIN_XSHUT = 1;  // Shutdown pin (active low)
  static const int PIN_INT = 2;  // Interrupt (open-drain)

}
