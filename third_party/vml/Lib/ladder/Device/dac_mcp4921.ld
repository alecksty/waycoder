(*
 * MCP4921梯形图定义
 * 生成自: Microchip/DAC/MCP4921
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: MCP4921 12-bit SPI DAC (single channel, 2x buffered output)
 * CPU架构: DAC
 * 位宽: 12位
 * 时钟频率: 20000000 Hz
 *)

PROGRAM MCP4921
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* MCP4921 12-bit DAC (SPI, 2.7V-5.5V) *)
    MCP4921_BASE AT %MW0x00 : WORD;
    MCP4921_DAC_VALUE AT %MW0x00 : WORD;
    MCP4921_DAC_VALUE_BUF AT %MW0x00.14 : BOOL; (* VREF buffer (0=unbuffered, 1=buffered) *)
    MCP4921_DAC_VALUE_GA AT %MW0x00.13 : BOOL; (* Gain (0=2x, 1=1x) *)
    MCP4921_DAC_VALUE_SHDN AT %MW0x00.12 : BOOL; (* Shutdown (0=shutdown, 1=active) *)
    MCP4921_VREF AT %MW0x02 : WORD;

END_VAR

BEGIN
    (* MCP4921初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM
