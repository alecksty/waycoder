(*
 * DS18B20梯形图定义
 * 生成自: Maxim/Dallas/Sensor/DS18B20
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: Programmable Resolution 1-Wire Digital Thermometer
 * CPU架构: Sensor
 * 位宽: 8位
 * 时钟频率: 100000 Hz
 *)

PROGRAM DS18B20
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* DS18B20 1-Wire Thermometer (3.0V-5.5V, TO-92) *)
    DS18B20_BASE AT %MW0x00 : WORD;
    DS18B20_TEMP_LSB AT %MW0x00 : BYTE;
    DS18B20_TEMP_MSB AT %MW0x01 : BYTE;
    DS18B20_TH_REG AT %MW0x02 : BYTE;
    DS18B20_TL_REG AT %MW0x03 : BYTE;
    DS18B20_CONFIG AT %MW0x04 : BYTE;
    DS18B20_CONFIG_R0 AT %MW0x04.5 : BOOL; (* Resolution select bit 0 *)
    DS18B20_CONFIG_R1 AT %MW0x04.6 : BOOL; (* Resolution select bit 1 (00=9bit,10=10bit,01=11bit,11=12bit) *)
    DS18B20_COUNT_REMAIN AT %MW0x06 : BYTE;
    DS18B20_COUNT_PER_C AT %MW0x07 : BYTE;
    DS18B20_CRC AT %MW0x08 : BYTE;

END_VAR

BEGIN
    (* DS18B20初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM
