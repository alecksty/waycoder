(*
 * BH1750梯形图定义
 * 生成自: ROHM/Sensor/BH1750
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: BH1750FVI Digital Ambient Light Sensor (I2C, 1-65535 lux, 16-bit)
 * CPU架构: Sensor
 * 位宽: 16位
 * 时钟频率: 400000 Hz
 *)

PROGRAM BH1750
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* BH1750 Light Sensor (0x23/0x5C, 2.4V-3.6V) *)
    BH1750_BASE AT %MW0x23 : WORD;
    BH1750_LUX AT %MW0x00 : WORD;
    BH1750_MODE AT %MW0x01 : BYTE;
    BH1750_MODE_CONT_H AT %MW0x01.0 : BOOL; (* Continuous High Res (1lx, 120ms) *)
    BH1750_MODE_CONT_H2 AT %MW0x01.1 : BOOL; (* Continuous High Res 2 (0.5lx, 120ms) *)
    BH1750_MODE_CONT_L AT %MW0x01.2 : BOOL; (* Continuous Low Res (4lx, 16ms) *)
    BH1750_MODE_ONCE_H AT %MW0x01.3 : BOOL; (* One-time High Res (1lx, 120ms) *)
    BH1750_MODE_ONCE_H2 AT %MW0x01.4 : BOOL; (* One-time High Res 2 (0.5lx, 120ms) *)
    BH1750_MODE_ONCE_L AT %MW0x01.5 : BOOL; (* One-time Low Res (4lx, 16ms) *)
    BH1750_CMD_POWER_ON AT %MW0x01 : BYTE;
    BH1750_CMD_POWER_OFF AT %MW0x00 : BYTE;
    BH1750_CMD_RESET AT %MW0x07 : BYTE;

END_VAR

BEGIN
    (* BH1750初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM
