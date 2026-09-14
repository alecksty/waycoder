(*
 * 74HC595梯形图定义
 * 生成自: TI/NXP/GPIO/74HC595
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: 74HC595 8-bit Shift Register (SPI-compatible, serial-in parallel-out, daisy-chainable)
 * CPU架构: GPIO
 * 位宽: 8位
 * 时钟频率: 10000000 Hz
 *)

PROGRAM 74HC595
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* 74HC595 8-bit Shift Register (2V-6V, DIP-16) *)
    _74HC595_BASE AT %MW0x00 : WORD;
    _74HC595_DATA AT %MW0x00 : BYTE;
    _74HC595_LATCH AT %MW0x01 : BYTE;
    _74HC595_CHAIN_COUNT AT %MW0x02 : BYTE;
    _74HC595_OE AT %MW0x03 : BYTE;
    _74HC595_CLEAR AT %MW0x04 : BYTE;

END_VAR

BEGIN
    (* 74HC595初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM
