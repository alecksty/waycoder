(*
 * DHT11梯形图定义
 * 生成自: Aosong/Sensor/DHT11
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: Digital Temperature and Humidity Sensor (1-Wire)
 * CPU架构: Sensor
 * 位宽: 8位
 * 时钟频率: 500000 Hz
 *)

PROGRAM DHT11
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* DHT11 1-Wire Sensor (3.0V-5.5V) *)
    DHT11_BASE AT %MW0x00 : WORD;
    DHT11_HUMIDITY_INT AT %MW0x00 : BYTE;
    DHT11_HUMIDITY_DEC AT %MW0x01 : BYTE;
    DHT11_TEMP_INT AT %MW0x02 : BYTE;
    DHT11_TEMP_DEC AT %MW0x03 : BYTE;
    DHT11_CHECKSUM AT %MW0x04 : BYTE;

END_VAR

BEGIN
    (* DHT11初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM
