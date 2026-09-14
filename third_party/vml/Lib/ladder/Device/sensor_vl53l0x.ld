(*
 * VL53L0X梯形图定义
 * 生成自: STMicroelectronics/Sensor/VL53L0X
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: VL53L0X ToF Laser Distance Sensor (I2C, 2cm-200cm, 940nm VCSEL)
 * CPU架构: Sensor
 * 位宽: 16位
 * 时钟频率: 400000 Hz
 *)

PROGRAM VL53L0X
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* VL53L0X ToF Distance Sensor (0x29, 2.6V-3.5V) *)
    VL53L0X_BASE AT %MW0x29 : WORD;
    VL53L0X_DISTANCE AT %MW0x00 : WORD;
    VL53L0X_SIGNAL_RATE AT %MW0x02 : WORD;
    VL53L0X_AMBIENT_RATE AT %MW0x04 : WORD;
    VL53L0X_SPAD_COUNT AT %MW0x06 : WORD;
    VL53L0X_RANGE_STATUS AT %MW0x08 : BYTE;
    VL53L0X_TIMING_BUDGET AT %MW0x09 : DWORD;
    VL53L0X_INTER_MEAS AT %MW0x0D : DWORD;
    VL53L0X_MODE AT %MW0x0E : BYTE;

END_VAR

BEGIN
    (* VL53L0X初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM
