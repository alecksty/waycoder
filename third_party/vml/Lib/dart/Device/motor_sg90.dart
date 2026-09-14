// SG90 设备定义 - Dart 库
// 生成自: Tower Pro/Motor/SG90
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: SG90 Micro Servo Motor (0-180°, 4.8V-6V)
// CPU架构: Motor
// 位宽: 8位
// 时钟频率: 0 Hz

class SG90Device {
  static const String deviceName = "SG90";
  static const String manufacturer = "Tower Pro";
  static const String family = "Motor";
  static const String version = "1.0";
  static const String architecture = "Motor";
  static const int bits = 8;
  static const int clockFrequency = 0;

  // 外设定义
  // SG90 Micro Servo (500-2500us pulse, 50Hz)
  static const int SG90_BASE = 0x00;
  static const int SG90_ANGLE_ADDR = 0x00;
  static const int SG90_PULSE_MIN_ADDR = 0x01;
  static const int SG90_PULSE_MAX_ADDR = 0x03;
  static const int SG90_CURRENT_ANGLE_ADDR = 0x05;
  static const int SG90_SPEED_ADDR = 0x06;

}
