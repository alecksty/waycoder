(*
 * BMP280梯形图定义
 * 生成自: Bosch/Sensor/BMP280
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: Digital Barometric Pressure and Temperature Sensor (I2C/SPI)
 * CPU架构: Sensor
 * 位宽: 8位
 * 时钟频率: 3400000 Hz
 *)

PROGRAM BMP280
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* BMP280 I2C Sensor (0x76/0x77, 1.71V-3.6V) *)
    BMP280_BASE AT %MW0x76 : WORD;
    BMP280_TEMP_XLSB AT %MW0xFC : BYTE;
    BMP280_TEMP_LSB AT %MW0xFB : BYTE;
    BMP280_TEMP_MSB AT %MW0xFA : BYTE;
    BMP280_PRESS_XLSB AT %MW0xF9 : BYTE;
    BMP280_PRESS_LSB AT %MW0xF8 : BYTE;
    BMP280_PRESS_MSB AT %MW0xF7 : BYTE;
    BMP280_CONFIG AT %MW0xF5 : BYTE;
    BMP280_CONFIG_T_SB AT %MW0xF5.5 : BOOL; (* Standby time in normal mode *)
    BMP280_CONFIG_FILTER AT %MW0xF5.2 : BOOL; (* Filter coefficient *)
    BMP280_CONFIG_SPI3W_EN AT %MW0xF5.0 : BOOL; (* Enable 3-wire SPI *)
    BMP280_CTRL_MEAS AT %MW0xF4 : BYTE;
    BMP280_CTRL_MEAS_MODE AT %MW0xF4.0 : BOOL; (* 0=sleep, 1/2=forced, 3=normal *)
    BMP280_CTRL_MEAS_OSRS_P AT %MW0xF4.2 : BOOL; (* Pressure oversampling *)
    BMP280_CTRL_MEAS_OSRS_T AT %MW0xF4.5 : BOOL; (* Temperature oversampling *)
    BMP280_STATUS AT %MW0xF3 : BYTE;
    BMP280_STATUS_IM_UPDATE AT %MW0xF3.0 : BOOL; (* 1=Image register update in progress *)
    BMP280_STATUS_MEASURING AT %MW0xF3.3 : BOOL; (* 1=Conversion is running *)
    BMP280_CHIP_ID AT %MW0xD0 : BYTE;
    BMP280_RESET AT %MW0xE0 : BYTE;

END_VAR

BEGIN
    (* BMP280初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM
