(*
 * PCF8574梯形图定义
 * 生成自: NXP/TI/GPIO/PCF8574
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: PCF8574 8-bit I2C GPIO Expander (quasi-bidirectional, interrupt)
 * CPU架构: GPIO
 * 位宽: 8位
 * 时钟频率: 100000 Hz
 *)

PROGRAM PCF8574
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* PCF8574 8-bit GPIO (0x20-0x27, 2.5V-6V) *)
    PCF8574_BASE AT %MW0x20 : WORD;
    PCF8574_INPUT AT %MW0x00 : BYTE;
    PCF8574_INPUT_P0 AT %MW0x00.0 : BOOL; (* Pin P0 *)
    PCF8574_INPUT_P1 AT %MW0x00.1 : BOOL; (* Pin P1 *)
    PCF8574_INPUT_P2 AT %MW0x00.2 : BOOL; (* Pin P2 *)
    PCF8574_INPUT_P3 AT %MW0x00.3 : BOOL; (* Pin P3 *)
    PCF8574_INPUT_P4 AT %MW0x00.4 : BOOL; (* Pin P4 *)
    PCF8574_INPUT_P5 AT %MW0x00.5 : BOOL; (* Pin P5 *)
    PCF8574_INPUT_P6 AT %MW0x00.6 : BOOL; (* Pin P6 *)
    PCF8574_INPUT_P7 AT %MW0x00.7 : BOOL; (* Pin P7 *)
    PCF8574_OUTPUT AT %MW0x01 : BYTE;
    PCF8574_POLARITY AT %MW0x02 : BYTE;

    (* 中断变量 *)
    INT_INT : BOOL; (* Pin change interrupt (open-drain, active low) *)

END_VAR

BEGIN
    (* PCF8574初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 4: 中断处理 *)
    --|INT_INT|--[中断处理]--

END_PROGRAM
