(*
 * MCP23017梯形图定义
 * 生成自: Microchip/GPIO/MCP23017
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: MCP23017 16-bit I2C GPIO Expander (2 banks, interrupt, 25mA per pin)
 * CPU架构: GPIO
 * 位宽: 16位
 * 时钟频率: 400000 Hz
 *)

PROGRAM MCP23017
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* MCP23017 16-bit GPIO (0x20-0x27, 1.8V-5.5V) *)
    MCP23017_BASE AT %MW0x20 : WORD;
    MCP23017_IODIRA AT %MW0x00 : BYTE;
    MCP23017_IODIRB AT %MW0x01 : BYTE;
    MCP23017_GPIOA AT %MW0x12 : BYTE;
    MCP23017_GPIOB AT %MW0x13 : BYTE;
    MCP23017_GPINTENA AT %MW0x04 : BYTE;
    MCP23017_GPINTENB AT %MW0x05 : BYTE;
    MCP23017_INTCONA AT %MW0x08 : BYTE;
    MCP23017_IOCON AT %MW0x0A : BYTE;
    MCP23017_GPPUA AT %MW0x0C : BYTE;
    MCP23017_GPPUB AT %MW0x0D : BYTE;

    (* 中断变量 *)
    INT_INTA : BOOL; (* Port A interrupt *)
    INT_INTB : BOOL; (* Port B interrupt *)

END_VAR

BEGIN
    (* MCP23017初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 4: 中断处理 *)
    --|INT_INTA|--[中断处理]--
    --|INT_INTB|--[中断处理]--

END_PROGRAM
