(*
 * MCP3008梯形图定义
 * 生成自: Microchip/ADC/MCP3008
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: MCP3008 10-bit SPI ADC (8-channel, 200ksps)
 * CPU架构: ADC
 * 位宽: 10位
 * 时钟频率: 1350000 Hz
 *)

PROGRAM MCP3008
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* MCP3008 10-bit 8-ch ADC (SPI, 2.7V-5.5V, DIP-16) *)
    MCP3008_BASE AT %MW0x00 : WORD;
    MCP3008_CH0 AT %MW0x00 : WORD;
    MCP3008_CH1 AT %MW0x01 : WORD;
    MCP3008_CH2 AT %MW0x02 : WORD;
    MCP3008_CH3 AT %MW0x03 : WORD;
    MCP3008_CH4 AT %MW0x04 : WORD;
    MCP3008_CH5 AT %MW0x05 : WORD;
    MCP3008_CH6 AT %MW0x06 : WORD;
    MCP3008_CH7 AT %MW0x07 : WORD;
    MCP3008_DIFF_01 AT %MW0x08 : WORD;
    MCP3008_DIFF_23 AT %MW0x09 : WORD;

END_VAR

BEGIN
    (* MCP3008初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM
