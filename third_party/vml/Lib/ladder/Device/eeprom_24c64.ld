(*
 * 24C64梯形图定义
 * 生成自: Generic/Memory/24C64
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: 24C64 64Kbit I2C Serial EEPROM (8K×8, 32-byte page write)
 * CPU架构: Memory
 * 位宽: 8位
 * 时钟频率: 400000 Hz
 *)

PROGRAM 24C64
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* 24C64 I2C EEPROM (0x50-0x57, 1.7V-5.5V) *)
    _24C64_BASE AT %MW0x50 : WORD;
    _24C64_ADDR_H AT %MW0x00 : BYTE;
    _24C64_ADDR_L AT %MW0x01 : BYTE;
    _24C64_DATA AT %MW0x02 : BYTE;
    _24C64_PAGE_SIZE AT %MW0xFE : BYTE;
    _24C64_SIZE AT %MW0xFD : WORD;

END_VAR

BEGIN
    (* 24C64初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM
