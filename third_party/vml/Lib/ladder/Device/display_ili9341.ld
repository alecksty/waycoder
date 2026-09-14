(*
 * ILI9341梯形图定义
 * 生成自: Ilitek/Display/ILI9341
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: ILI9341 2.8" 240x320 TFT LCD Display (SPI, 18-bit color, touch)
 * CPU架构: Display
 * 位宽: 18位
 * 时钟频率: 20000000 Hz
 *)

PROGRAM ILI9341
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* ILI9341 240x320 TFT (SPI, 3.3V, 2.8inch) *)
    ILI9341_BASE AT %MW0x00 : WORD;
    ILI9341_CMD AT %MW0x00 : BYTE;
    ILI9341_DATA AT %MW0x01 : BYTE;
    ILI9341_COL_START AT %MW0x2A : WORD;
    ILI9341_PAGE_START AT %MW0x2B : WORD;
    ILI9341_WRITE_RAM AT %MW0x2C : WORD;
    ILI9341_MADCTL AT %MW0x36 : BYTE;
    ILI9341_PIXFMT AT %MW0x3A : BYTE;
    ILI9341_FRMCTL AT %MW0xB1 : WORD;
    ILI9341_GAMMA AT %MW0x26 : BYTE;
    ILI9341_SLEEP_OUT AT %MW0x11 : ARRAY[0..-1] OF BOOL;
    ILI9341_DISP_ON AT %MW0x29 : ARRAY[0..-1] OF BOOL;

END_VAR

BEGIN
    (* ILI9341初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM
