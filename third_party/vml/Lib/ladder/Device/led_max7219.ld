(*
 * MAX7219梯形图定义
 * 生成自: Maxim/LED/MAX7219
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: MAX7219 8-Digit LED Display Driver (SPI, daisy-chainable, 8x8 matrix)
 * CPU架构: LED
 * 位宽: 8位
 * 时钟频率: 10000000 Hz
 *)

PROGRAM MAX7219
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* MAX7219 8-Digit/8x8 Matrix Driver (4.0V-5.5V, DIP-24) *)
    MAX7219_BASE AT %MW0x00 : WORD;
    MAX7219_DIGIT0 AT %MW0x01 : BYTE;
    MAX7219_DIGIT1 AT %MW0x02 : BYTE;
    MAX7219_DIGIT2 AT %MW0x03 : BYTE;
    MAX7219_DIGIT3 AT %MW0x04 : BYTE;
    MAX7219_DIGIT4 AT %MW0x05 : BYTE;
    MAX7219_DIGIT5 AT %MW0x06 : BYTE;
    MAX7219_DIGIT6 AT %MW0x07 : BYTE;
    MAX7219_DIGIT7 AT %MW0x08 : BYTE;
    MAX7219_DECODE AT %MW0x09 : BYTE;
    MAX7219_INTENSITY AT %MW0x0A : BYTE;
    MAX7219_SCAN_LIMIT AT %MW0x0B : BYTE;
    MAX7219_SHUTDOWN AT %MW0x0C : BYTE;
    MAX7219_TEST AT %MW0x0F : BYTE;

END_VAR

BEGIN
    (* MAX7219初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM
