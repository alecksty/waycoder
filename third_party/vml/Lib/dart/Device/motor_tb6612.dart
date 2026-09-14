// TB6612 设备定义 - Dart 库
// 生成自: Toshiba/Motor/TB6612
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: TB6612FNG Dual DC Motor Driver (1.2A continuous, 3.2A peak, 2.5V-13.5V)
// CPU架构: Motor
// 位宽: 8位
// 时钟频率: 100000 Hz

class TB6612Device {
  static const String deviceName = "TB6612";
  static const String manufacturer = "Toshiba";
  static const String family = "Motor";
  static const String version = "1.0";
  static const String architecture = "Motor";
  static const int bits = 8;
  static const int clockFrequency = 100000;

  // 外设定义
  // TB6612 Dual Motor Driver (2.5V-13.5V, 1.2A/3.2A peak)
  static const int TB6612_BASE = 0x00;
  static const int TB6612_MOTOR_A_ADDR = 0x00;
  static const int TB6612_MOTOR_A_AIN1_BIT = 0;  // Motor A input 1
  static const int TB6612_MOTOR_A_AIN2_BIT = 1;  // Motor A input 2
  static const int TB6612_MOTOR_A_PWMA_BIT = 2;  // Motor A PWM enable
  static const int TB6612_MOTOR_B_ADDR = 0x01;
  static const int TB6612_MOTOR_B_BIN1_BIT = 0;  // Motor B input 1
  static const int TB6612_MOTOR_B_BIN2_BIT = 1;  // Motor B input 2
  static const int TB6612_MOTOR_B_PWMB_BIT = 2;  // Motor B PWM enable
  static const int TB6612_SPEED_A_ADDR = 0x02;
  static const int TB6612_SPEED_B_ADDR = 0x04;
  static const int TB6612_STBY_ADDR = 0x06;

}
