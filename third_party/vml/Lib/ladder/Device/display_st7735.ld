(*
 * ST7735梯形图定义
 * 生成自: Sitronix/Display/ST7735
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: ST7735 1.8" 128x160 TFT LCD Display (SPI, 16-bit color)
 * CPU架构: Display
 * 位宽: 16位
 * 时钟频率: 16000000 Hz
 *)

PROGRAM ST7735
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* ST7735 128x160 TFT (SPI, 3.3V-5V) *)
    ST7735_BASE AT %MW0x00 : WORD;
    ST7735_CMD AT %MW0x00 : BYTE;
    ST7735_DATA AT %MW0x01 : BYTE;
    ST7735_COL_START AT %MW0x2A : WORD;
    ST7735_ROW_START AT %MW0x2B : WORD;
    ST7735_WRITE_RAM AT %MW0x2C : WORD;
    ST7735_MADCTL AT %MW0x36 : BYTE;
    ST7735_COLMOD AT %MW0x3A : BYTE;
    ST7735_INVON AT %MW0x21 : ARRAY[0..-1] OF BOOL;
    ST7735_SLEEP_OUT AT %MW0x11 : ARRAY[0..-1] OF BOOL;
    ST7735_DISP_ON AT %MW0x29 : ARRAY[0..-1] OF BOOL;

END_VAR

BEGIN
    (* ST7735初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM
