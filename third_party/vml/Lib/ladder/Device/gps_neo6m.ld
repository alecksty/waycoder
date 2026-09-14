(*
 * NEO6M梯形图定义
 * 生成自: u-blox/GPS/NEO6M
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: NEO-6M GPS Module (UART, 50-channel, -162dBm tracking)
 * CPU架构: GPS
 * 位宽: 8位
 * 时钟频率: 9600 Hz
 *)

PROGRAM NEO6M
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* NEO-6M GPS Module (UART 9600bps, 3.3V-5V) *)
    NEO6M_BASE AT %MW0x00 : WORD;
    NEO6M_LATITUDE AT %MW0x00 : DWORD;
    NEO6M_LONGITUDE AT %MW0x04 : DWORD;
    NEO6M_ALTITUDE AT %MW0x08 : DWORD;
    NEO6M_SPEED AT %MW0x0C : WORD;
    NEO6M_HEADING AT %MW0x0E : WORD;
    NEO6M_SATELLITES AT %MW0x10 : BYTE;
    NEO6M_HDOP AT %MW0x11 : WORD;
    NEO6M_FIX_TYPE AT %MW0x13 : BYTE;
    NEO6M_DATE AT %MW0x14 : DWORD;
    NEO6M_TIME AT %MW0x18 : DWORD;
    NEO6M_VALID AT %MW0x1C : BYTE;

END_VAR

BEGIN
    (* NEO6M初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM
