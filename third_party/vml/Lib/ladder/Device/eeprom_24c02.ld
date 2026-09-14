(*
 * 24C02梯形图定义
 * 生成自: Generic/Memory/24C02
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: 2Kbit I2C Serial EEPROM (256 x 8 bits)
 * CPU架构: Memory
 * 位宽: 8位
 * 时钟频率: 400000 Hz
 *)

PROGRAM 24C02
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* 24C02 I2C EEPROM (0x50-0x57, 1.8V-5.5V, DIP-8) *)
    _24C02_BASE AT %MW0x50 : WORD;
    _24C02_STATUS AT %MW0xFF : BYTE;
    _24C02_STATUS_BUSY AT %MW0xFF.0 : BOOL; (* 1=Write in progress *)
    _24C02_PAGE_SIZE AT %MW0xFE : BYTE;
    _24C02_SIZE AT %MW0xFD : WORD;

END_VAR

BEGIN
    (* 24C02初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM
