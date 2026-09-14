(*
 * TB6612梯形图定义
 * 生成自: Toshiba/Motor/TB6612
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: TB6612FNG Dual DC Motor Driver (1.2A continuous, 3.2A peak, 2.5V-13.5V)
 * CPU架构: Motor
 * 位宽: 8位
 * 时钟频率: 100000 Hz
 *)

PROGRAM TB6612
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* TB6612 Dual Motor Driver (2.5V-13.5V, 1.2A/3.2A peak) *)
    TB6612_BASE AT %MW0x00 : WORD;
    TB6612_MOTOR_A AT %MW0x00 : BYTE;
    TB6612_MOTOR_A_AIN1 AT %MW0x00.0 : BOOL; (* Motor A input 1 *)
    TB6612_MOTOR_A_AIN2 AT %MW0x00.1 : BOOL; (* Motor A input 2 *)
    TB6612_MOTOR_A_PWMA AT %MW0x00.2 : BOOL; (* Motor A PWM enable *)
    TB6612_MOTOR_B AT %MW0x01 : BYTE;
    TB6612_MOTOR_B_BIN1 AT %MW0x01.0 : BOOL; (* Motor B input 1 *)
    TB6612_MOTOR_B_BIN2 AT %MW0x01.1 : BOOL; (* Motor B input 2 *)
    TB6612_MOTOR_B_PWMB AT %MW0x01.2 : BOOL; (* Motor B PWM enable *)
    TB6612_SPEED_A AT %MW0x02 : WORD;
    TB6612_SPEED_B AT %MW0x04 : WORD;
    TB6612_STBY AT %MW0x06 : BYTE;

END_VAR

BEGIN
    (* TB6612初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM
