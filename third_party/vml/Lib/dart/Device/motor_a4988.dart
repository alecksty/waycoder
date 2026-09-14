// A4988 设备定义 - Dart 库
// 生成自: Allegro/Motor/A4988
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: A4988 Stepper Motor Driver (up to 1/16 microstepping, 2A, 8V-35V)
// CPU架构: Motor
// 位宽: 8位
// 时钟频率: 0 Hz

class A4988Device {
  static const String deviceName = "A4988";
  static const String manufacturer = "Allegro";
  static const String family = "Motor";
  static const String version = "1.0";
  static const String architecture = "Motor";
  static const int bits = 8;
  static const int clockFrequency = 0;

  // 外设定义
  // A4988 Stepper Motor Driver (3.3V/5V logic)
  static const int A4988_BASE = 0x00;
  static const int A4988_CTRL_ADDR = 0x00;
  static const int A4988_CTRL_STEP_BIT = 0;  // Step pulse (rising edge)
  static const int A4988_CTRL_DIR_BIT = 1;  // Direction (0=CW, 1=CCW)
  static const int A4988_CTRL_ENABLE_BIT = 2;  // Enable (active low)
  static const int A4988_CTRL_SLEEP_BIT = 3;  // Sleep mode (active low)
  static const int A4988_CTRL_RESET_BIT = 4;  // Reset (active low)
  static const int A4988_MICROSTEP_ADDR = 0x01;
  static const int A4988_MICROSTEP_MS1_BIT = 0;  // Microstep select 1
  static const int A4988_MICROSTEP_MS2_BIT = 1;  // Microstep select 2
  static const int A4988_MICROSTEP_MS3_BIT = 2;  // Microstep select 3
  static const int A4988_STEPS_ADDR = 0x02;
  static const int A4988_DELAY_US_ADDR = 0x06;

}
