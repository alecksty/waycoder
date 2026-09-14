(*
 * HD44780梯形图定义
 * 生成自: Hitachi/Display/HD44780
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: HD44780 16x2 Character LCD Controller (4-bit/8-bit parallel or I2C via PCF8574)
 * CPU架构: Display
 * 位宽: 8位
 * 时钟频率: 0 Hz
 *)

PROGRAM HD44780
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* HD44780 16x2 LCD (0x27/0x3F I2C, 5V) *)
    HD44780_BASE AT %MW0x27 : WORD;
    HD44780_CMD AT %MW0x00 : BYTE;
    HD44780_DATA AT %MW0x01 : BYTE;
    HD44780_CTRL_RS AT %MW0x00 : BYTE;
    HD44780_CTRL_RW AT %MW0x01 : BYTE;
    HD44780_CTRL_EN AT %MW0x02 : BYTE;
    HD44780_CTRL_BL AT %MW0x03 : BYTE;
    HD44780_ADDR_DDRAM AT %MW0x80 : BYTE;
    HD44780_ADDR_CGRAM AT %MW0x40 : BYTE;

END_VAR

BEGIN
    (* HD44780初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM
