(*
 * SG90梯形图定义
 * 生成自: Tower Pro/Motor/SG90
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: SG90 Micro Servo Motor (0-180°, 4.8V-6V)
 * CPU架构: Motor
 * 位宽: 8位
 * 时钟频率: 0 Hz
 *)

PROGRAM SG90
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* SG90 Micro Servo (500-2500us pulse, 50Hz) *)
    SG90_BASE AT %MW0x00 : WORD;
    SG90_ANGLE AT %MW0x00 : BYTE;
    SG90_PULSE_MIN AT %MW0x01 : WORD;
    SG90_PULSE_MAX AT %MW0x03 : WORD;
    SG90_CURRENT_ANGLE AT %MW0x05 : BYTE;
    SG90_SPEED AT %MW0x06 : BYTE;

END_VAR

BEGIN
    (* SG90初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM
