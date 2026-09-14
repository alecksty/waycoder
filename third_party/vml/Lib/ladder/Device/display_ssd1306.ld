(*
 * SSD1306梯形图定义
 * 生成自: Solomon Systech/Display/SSD1306
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: SSD1306 128x64 OLED Display Controller (I2C/SPI)
 * CPU架构: Display
 * 位宽: 8位
 * 时钟频率: 400000 Hz
 *)

PROGRAM SSD1306
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* SSD1306 128x64 OLED (0x3C/0x3D I2C, 3.3V-5V) *)
    SSD1306_BASE AT %MW0x3C : WORD;
    SSD1306_CMD AT %MW0x00 : BYTE;
    SSD1306_DATA AT %MW0x40 : BYTE;
    SSD1306_DISPLAY_OFF AT %MW0xAE : BYTE;
    SSD1306_DISPLAY_ON AT %MW0xAF : BYTE;
    SSD1306_CONTRAST AT %MW0x81 : BYTE;
    SSD1306_SEG_REMAP AT %MW0xA1 : BYTE;
    SSD1306_COM_SCAN AT %MW0xC8 : BYTE;
    SSD1306_ADDR_MODE AT %MW0x20 : BYTE;
    SSD1306_COL_START AT %MW0x21 : BYTE;
    SSD1306_PAGE_START AT %MW0x22 : BYTE;

END_VAR

BEGIN
    (* SSD1306初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM
