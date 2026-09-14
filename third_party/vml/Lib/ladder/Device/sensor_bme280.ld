(*
 * BME280梯形图定义
 * 生成自: Bosch/Sensor/BME280
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: BME280 Combined Humidity, Pressure, and Temperature Sensor (I2C/SPI)
 * CPU架构: Sensor
 * 位宽: 8位
 * 时钟频率: 400000 Hz
 *)

PROGRAM BME280
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* BME280 Environmental Sensor (0x76/0x77, 1.71V-3.6V) *)
    BME280_BASE AT %MW0x76 : WORD;
    BME280_CHIP_ID AT %MW0xD0 : BYTE;
    BME280_RESET AT %MW0xE0 : BYTE;
    BME280_CTRL_HUM AT %MW0xF2 : BYTE;
    BME280_STATUS AT %MW0xF3 : BYTE;
    BME280_CTRL_MEAS AT %MW0xF4 : BYTE;
    BME280_CONFIG AT %MW0xF5 : BYTE;
    BME280_PRESS AT %MW0xF7 : ARRAY[0..2] OF BOOL;
    BME280_TEMP AT %MW0xFA : ARRAY[0..2] OF BOOL;
    BME280_HUM AT %MW0xFD : WORD;

END_VAR

BEGIN
    (* BME280初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM
