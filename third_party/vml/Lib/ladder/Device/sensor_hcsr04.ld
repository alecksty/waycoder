(*
 * HC_SR04梯形图定义
 * 生成自: Generic/Sensor/HC_SR04
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: Ultrasonic Distance Sensor (2cm-400cm)
 * CPU架构: Sensor
 * 位宽: 8位
 * 时钟频率: 0 Hz
 *)

PROGRAM HC_SR04
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* HC-SR04 Ultrasonic Sensor (4.5V-5.5V) *)
    HC_SR04_BASE AT %MW0x00 : WORD;
    HC_SR04_TRIG AT %MW0x00 : BYTE;
    HC_SR04_DISTANCE_H AT %MW0x01 : BYTE;
    HC_SR04_DISTANCE_L AT %MW0x02 : BYTE;
    HC_SR04_STATUS AT %MW0x03 : BYTE;
    HC_SR04_STATUS_BUSY AT %MW0x03.0 : BOOL; (* 1=Measurement in progress *)
    HC_SR04_STATUS_VALID AT %MW0x03.1 : BOOL; (* 1=Valid measurement available *)
    HC_SR04_STATUS_TIMEOUT AT %MW0x03.2 : BOOL; (* 1=No echo received (out of range) *)

END_VAR

BEGIN
    (* HC_SR04初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM
