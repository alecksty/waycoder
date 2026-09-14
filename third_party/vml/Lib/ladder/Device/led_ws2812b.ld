(*
 * WS2812B梯形图定义
 * 生成自: Worldsemi/LED/WS2812B
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: WS2812B Intelligent RGB LED (single-wire, 800KHz, daisy-chainable)
 * CPU架构: LED
 * 位宽: 24位
 * 时钟频率: 800000 Hz
 *)

PROGRAM WS2812B
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* WS2812B RGB LED Strip (5V, 60mA/led) *)
    WS2812B_BASE AT %MW0x00 : WORD;
    WS2812B_LED_COUNT AT %MW0x00 : WORD;
    WS2812B_LED_DATA AT %MW0x02 : ARRAY[0..2] OF BOOL;
    WS2812B_BRIGHTNESS AT %MW0x05 : BYTE;
    WS2812B_SHOW AT %MW0x06 : BYTE;
    WS2812B_CLEAR AT %MW0x07 : BYTE;

END_VAR

BEGIN
    (* WS2812B初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM
