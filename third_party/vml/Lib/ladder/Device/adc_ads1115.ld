(*
 * ADS1115梯形图定义
 * 生成自: Texas Instruments/ADC/ADS1115
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: ADS1115 16-bit I2C ADC (4-channel, PGA, 860SPS)
 * CPU架构: ADC
 * 位宽: 16位
 * 时钟频率: 400000 Hz
 *)

PROGRAM ADS1115
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* ADS1115 16-bit ADC (0x48-0x4B, 2.0V-5.5V) *)
    ADS1115_BASE AT %MW0x48 : WORD;
    ADS1115_CONV_RESULT AT %MW0x00 : WORD;
    ADS1115_CONFIG AT %MW0x01 : WORD;
    ADS1115_CONFIG_OS AT %MW0x01.15 : BOOL; (* Operational status/start single-shot *)
    ADS1115_CONFIG_MUX AT %MW0x01.12 : BOOL; (* Input multiplexer: 0=A0-A1,1=A0-A3,2=A1-A3,3=A2-A3,4=A0,5=A1,6=A2,7=A3 *)
    ADS1115_CONFIG_PGA AT %MW0x01.9 : BOOL; (* PGA gain: 0=±6.144V,1=±4.096V,2=±2.048V,3=±1.024V,4=±0.512V,5=±0.256V *)
    ADS1115_CONFIG_MODE AT %MW0x01.8 : BOOL; (* 0=continuous, 1=single-shot *)
    ADS1115_CONFIG_DR AT %MW0x01.5 : BOOL; (* Data rate: 0=8,1=16,2=32,3=64,4=128,5=250,6=475,7=860 SPS *)
    ADS1115_CONFIG_COMP_MODE AT %MW0x01.4 : BOOL; (* Comparator mode (0=traditional, 1=window) *)
    ADS1115_CONFIG_COMP_POL AT %MW0x01.3 : BOOL; (* Comparator polarity (0=active low, 1=active high) *)
    ADS1115_LO_THRESH AT %MW0x02 : WORD;
    ADS1115_HI_THRESH AT %MW0x03 : WORD;

END_VAR

BEGIN
    (* ADS1115初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM
