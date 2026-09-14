// ULN2003 设备定义 - Dart 库
// 生成自: ST/TI/Motor/ULN2003
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: ULN2003 7-Channel Darlington Driver + 28BYJ-48 Stepper Motor (5V)
// CPU架构: Motor
// 位宽: 8位
// 时钟频率: 0 Hz

class ULN2003Device {
  static const String deviceName = "ULN2003";
  static const String manufacturer = "ST/TI";
  static const String family = "Motor";
  static const String version = "1.0";
  static const String architecture = "Motor";
  static const int bits = 8;
  static const int clockFrequency = 0;

  // 外设定义
  // ULN2003 + 28BYJ-48 Stepper (5V, 64:1 gear, 5.625°/step)
  static const int ULN2003_BASE = 0x00;
  static const int ULN2003_STEPPER_ADDR = 0x00;
  static const int ULN2003_STEP_MODE_ADDR = 0x01;
  static const int ULN2003_STEPS_ADDR = 0x02;
  static const int ULN2003_DELAY_MS_ADDR = 0x04;
  static const int ULN2003_POSITION_ADDR = 0x05;
  static const int ULN2003_DIRECTION_ADDR = 0x07;

}
