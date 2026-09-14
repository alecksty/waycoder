(*
 * nRF52832梯形图定义
 * 生成自: Nordic/nRF52/nRF52832
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 32-bit ARM Cortex-M4F BLE SoC with 512KB Flash, 64KB RAM, 64MHz
 * CPU架构: ARM-Cortex-M4F
 * 位宽: 32位
 * 时钟频率: 64000000 Hz
 *)

PROGRAM nRF52832
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    R0 AT %M0x00 : DWORD; (*  *)
    R1 AT %M0x04 : DWORD; (*  *)
    R2 AT %M0x08 : DWORD; (*  *)
    R3 AT %M0x0C : DWORD; (*  *)
    SP AT %M0x34 : DWORD; (*  *)
    LR AT %M0x38 : DWORD; (*  *)
    PC AT %M0x3C : DWORD; (*  *)

    (* 外设变量 *)
    (* General Purpose I/O Port 0 *)
    GPIO_P0_BASE AT %MW0x50000000 : WORD;
    GPIO_P0_OUT AT %MW0x504 : DWORD;
    GPIO_P0_OUTSET AT %MW0x508 : DWORD;
    GPIO_P0_OUTCLR AT %MW0x50C : DWORD;
    GPIO_P0_IN AT %MW0x510 : DWORD;
    GPIO_P0_DIR AT %MW0x514 : DWORD;
    GPIO_P0_DIRSET AT %MW0x518 : DWORD;
    GPIO_P0_DIRCLR AT %MW0x51C : DWORD;
    (* Power Control *)
    POWER_BASE AT %MW0x40000000 : WORD;
    POWER_DCDCEN AT %MW0x1C4 : DWORD;
    POWER_RAMSTATUS AT %MW0x268 : DWORD;
    (* Clock Control *)
    CLOCK_BASE AT %MW0x40000000 : WORD;
    CLOCK_HFCLKSTART AT %MW0x108 : DWORD;
    CLOCK_HFCLKSTARTED AT %MW0x208 : DWORD;

    (* 中断变量 *)
    INT_RESET : BOOL; (*  *)
    INT_SVCALL : BOOL; (*  *)

END_VAR

BEGIN
    (* nRF52832初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|R0>0|--(监控灯)--
    --|R1>0|--(监控灯)--
    --|R2>0|--(监控灯)--
    --|R3>0|--(监控灯)--
    --|SP>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_RESET|--[中断处理]--
    --|INT_SVCALL|--[中断处理]--

END_PROGRAM
