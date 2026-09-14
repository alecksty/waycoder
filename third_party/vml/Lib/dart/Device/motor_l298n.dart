// L298N 设备定义 - Dart 库
// 生成自: STMicroelectronics/Motor/L298N
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: L298N Dual H-Bridge DC Motor Driver (2A per channel, 5V-35V)
// CPU架构: Motor
// 位宽: 8位
// 时钟频率: 0 Hz

class L298NDevice {
  static const String deviceName = "L298N";
  static const String manufacturer = "STMicroelectronics";
  static const String family = "Motor";
  static const String version = "1.0";
  static const String architecture = "Motor";
  static const int bits = 8;
  static const int clockFrequency = 0;

  // 外设定义
  // L298N Dual H-Bridge Motor Driver (5V logic, 5-35V motor)
  static const int L298N_BASE = 0x00;
  static const int L298N_MOTOR_A_ADDR = 0x00;
  static const int L298N_MOTOR_A_IN1_BIT = 0;  // Motor A Input 1
  static const int L298N_MOTOR_A_IN2_BIT = 1;  // Motor A Input 2
  static const int L298N_MOTOR_A_ENA_BIT = 2;  // Motor A Enable/PWM
  static const int L298N_MOTOR_B_ADDR = 0x01;
  static const int L298N_MOTOR_B_IN3_BIT = 0;  // Motor B Input 3
  static const int L298N_MOTOR_B_IN4_BIT = 1;  // Motor B Input 4
  static const int L298N_MOTOR_B_ENB_BIT = 2;  // Motor B Enable/PWM
  static const int L298N_SPEED_A_ADDR = 0x02;
  static const int L298N_SPEED_B_ADDR = 0x03;
  static const int L298N_STATUS_ADDR = 0x04;

}
