(*
 * L298N梯形图定义
 * 生成自: STMicroelectronics/Motor/L298N
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: L298N Dual H-Bridge DC Motor Driver (2A per channel, 5V-35V)
 * CPU架构: Motor
 * 位宽: 8位
 * 时钟频率: 0 Hz
 *)

PROGRAM L298N
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* L298N Dual H-Bridge Motor Driver (5V logic, 5-35V motor) *)
    L298N_BASE AT %MW0x00 : WORD;
    L298N_MOTOR_A AT %MW0x00 : BYTE;
    L298N_MOTOR_A_IN1 AT %MW0x00.0 : BOOL; (* Motor A Input 1 *)
    L298N_MOTOR_A_IN2 AT %MW0x00.1 : BOOL; (* Motor A Input 2 *)
    L298N_MOTOR_A_ENA AT %MW0x00.2 : BOOL; (* Motor A Enable/PWM *)
    L298N_MOTOR_B AT %MW0x01 : BYTE;
    L298N_MOTOR_B_IN3 AT %MW0x01.0 : BOOL; (* Motor B Input 3 *)
    L298N_MOTOR_B_IN4 AT %MW0x01.1 : BOOL; (* Motor B Input 4 *)
    L298N_MOTOR_B_ENB AT %MW0x01.2 : BOOL; (* Motor B Enable/PWM *)
    L298N_SPEED_A AT %MW0x02 : BYTE;
    L298N_SPEED_B AT %MW0x03 : BYTE;
    L298N_STATUS AT %MW0x04 : BYTE;

END_VAR

BEGIN
    (* L298N初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM
