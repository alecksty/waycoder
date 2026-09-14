# L298N 设备定义 - R 脚本
# 生成自: STMicroelectronics/Motor/L298N
# 版本: 1.0
# 日期: 2026-05-06
# 作者: VML Team
# 描述: L298N Dual H-Bridge DC Motor Driver (2A per channel, 5V-35V)
# CPU架构: Motor
# 位宽: 8位
# 时钟频率: 0 Hz

# 外设定义
# L298N Dual H-Bridge Motor Driver (5V logic, 5-35V motor)
L298N_BASE <- 0x00
L298N_MOTOR_A_ADDR <- 0x00
L298N_MOTOR_A_IN1_BIT <- 0  # Motor A Input 1
L298N_MOTOR_A_IN2_BIT <- 1  # Motor A Input 2
L298N_MOTOR_A_ENA_BIT <- 2  # Motor A Enable/PWM
L298N_MOTOR_B_ADDR <- 0x01
L298N_MOTOR_B_IN3_BIT <- 0  # Motor B Input 3
L298N_MOTOR_B_IN4_BIT <- 1  # Motor B Input 4
L298N_MOTOR_B_ENB_BIT <- 2  # Motor B Enable/PWM
L298N_SPEED_A_ADDR <- 0x02
L298N_SPEED_B_ADDR <- 0x03
L298N_STATUS_ADDR <- 0x04

