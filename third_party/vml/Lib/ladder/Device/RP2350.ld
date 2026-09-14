(*
 * RP2350梯形图定义
 * 生成自: Raspberry/RP2/RP2350
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: Dual Cortex-M33 + RISC-V Hazard3 MCU with 520KB SRAM, 150MHz
 * CPU架构: ARM-Cortex-M33
 * 位宽: 32位
 * 时钟频率: 150000000 Hz
 *)

PROGRAM RP2350
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    R0 AT %M0x00 : DWORD; (*  *)
    R1 AT %M0x04 : DWORD; (*  *)
    R2 AT %M0x08 : DWORD; (*  *)
    R3 AT %M0x0C : DWORD; (*  *)
    R4 AT %M0x10 : DWORD; (*  *)
    R5 AT %M0x14 : DWORD; (*  *)
    SP AT %M0x34 : DWORD; (*  *)
    LR AT %M0x38 : DWORD; (*  *)
    PC AT %M0x3C : DWORD; (*  *)

    (* 外设变量 *)
    (* Single-Cycle I/O (GPIO) *)
    SIO_BASE AT %MW0xD0000000 : WORD;
    SIO_GPIO_IN AT %MW0x004 : DWORD;
    SIO_GPIO_OUT AT %MW0x010 : DWORD;
    SIO_GPIO_OUT_SET AT %MW0x014 : DWORD;
    SIO_GPIO_OUT_CLR AT %MW0x018 : DWORD;
    SIO_GPIO_OUT_XOR AT %MW0x01C : DWORD;
    SIO_GPIO_OE AT %MW0x020 : DWORD;
    SIO_GPIO_OE_SET AT %MW0x024 : DWORD;
    SIO_GPIO_OE_CLR AT %MW0x028 : DWORD;
    (* IO Bank 0 (GPIO control) *)
    IO_BANK0_BASE AT %MW0x40028000 : WORD;
    IO_BANK0_GPIO0_STATUS AT %MW0x000 : DWORD;
    IO_BANK0_GPIO0_CTRL AT %MW0x004 : DWORD;
    IO_BANK0_GPIO1_STATUS AT %MW0x008 : DWORD;
    IO_BANK0_GPIO1_CTRL AT %MW0x00C : DWORD;
    (* Pad controls for GPIO 0-29 *)
    PADS_BANK0_BASE AT %MW0x4002C000 : WORD;
    PADS_BANK0_GPIO0 AT %MW0x000 : DWORD;
    PADS_BANK0_GPIO1 AT %MW0x004 : DWORD;
    (* Reset Controller *)
    RESETS_BASE AT %MW0x4000C000 : WORD;
    RESETS_RESET AT %MW0x000 : DWORD;
    RESETS_RESET_DONE AT %MW0x008 : DWORD;

    (* 中断变量 *)
    INT_RESET : BOOL; (*  *)
    INT_SVCALL : BOOL; (*  *)

END_VAR

BEGIN
    (* RP2350初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|R0>0|--(监控灯)--
    --|R1>0|--(监控灯)--
    --|R2>0|--(监控灯)--
    --|R3>0|--(监控灯)--
    --|R4>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_RESET|--[中断处理]--
    --|INT_SVCALL|--[中断处理]--

END_PROGRAM
