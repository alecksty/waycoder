// MLX90614 设备定义 - Dart 库
// 生成自: Melexis/Sensor/MLX90614
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: MLX90614 Infrared Thermometer (I2C, non-contact, -70 to +380°C, 17-bit)
// CPU架构: Sensor
// 位宽: 17位
// 时钟频率: 100000 Hz

class MLX90614Device {
  static const String deviceName = "MLX90614";
  static const String manufacturer = "Melexis";
  static const String family = "Sensor";
  static const String version = "1.0";
  static const String architecture = "Sensor";
  static const int bits = 17;
  static const int clockFrequency = 100000;

  // 内存段定义
  static const int EEPROM_START = 0x00;
  static const int EEPROM_END = 0x1F;
  static const int EEPROM_SIZE = 32;  // Internal EEPROM (calibration data)

  // 外设定义
  // MLX90614 IR Thermometer (0x5A, 3V-5V, TO-39)
  static const int MLX90614_BASE = 0x5A;
  static const int MLX90614_T_AMBIENT_ADDR = 0x06;
  static const int MLX90614_T_OBJECT1_ADDR = 0x07;
  static const int MLX90614_T_OBJECT2_ADDR = 0x08;
  static const int MLX90614_RAW_IR1_ADDR = 0x04;
  static const int MLX90614_RAW_IR2_ADDR = 0x05;
  static const int MLX90614_EMISSIVITY_ADDR = 0x04;

}
