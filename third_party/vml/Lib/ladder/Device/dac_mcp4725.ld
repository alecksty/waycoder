(*
 * MCP4725梯形图定义
 * 生成自: Microchip/DAC/MCP4725
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: MCP4725 12-bit I2C DAC (single channel, EEPROM)
 * CPU架构: DAC
 * 位宽: 12位
 * 时钟频率: 400000 Hz
 *)

PROGRAM MCP4725
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* MCP4725 12-bit DAC (0x60-0x67, 2.7V-5.5V) *)
    MCP4725_BASE AT %MW0x60 : WORD;
    MCP4725_DAC_VALUE AT %MW0x00 : WORD;
    MCP4725_DAC_VALUE_PD AT %MW0x00.12 : BOOL; (* Power-down: 0=normal,1=1kΩ,2=100kΩ,3=500kΩ *)
    MCP4725_WRITE_EEPROM AT %MW0x60 : WORD;

END_VAR

BEGIN
    (* MCP4725初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM
