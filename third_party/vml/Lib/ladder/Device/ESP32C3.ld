(*
 * ESP32-C3梯形图定义
 * 生成自: Espressif/ESP32-C/ESP32-C3
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 32-bit RISC-V single-core WiFi + BLE SoC, 160MHz, 400KB SRAM
 * CPU架构: RISC-V
 * 位宽: 32位
 * 时钟频率: 160000000 Hz
 *)

PROGRAM ESP32_C3
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    X1 AT %M0x04 : DWORD; (* Return Address *)
    X2 AT %M0x08 : DWORD; (* Stack Pointer (SP) *)
    X3 AT %M0x0C : DWORD; (* Global Pointer (GP) *)
    X8 AT %M0x20 : DWORD; (* Frame Pointer (FP) *)
    X10 AT %M0x28 : DWORD; (* Function Argument (A0) *)
    X11 AT %M0x2C : DWORD; (* Function Argument (A1) *)
    PC AT %M0x3C : DWORD; (* Program Counter *)

    (* 外设变量 *)
    (* General Purpose I/O *)
    GPIO_BASE AT %MW0x60004000 : WORD;
    GPIO_OUT AT %MW0x04 : DWORD;
    GPIO_OUT_W1TS AT %MW0x08 : DWORD;
    GPIO_OUT_W1TC AT %MW0x0C : DWORD;
    GPIO_IN AT %MW0x10 : DWORD;
    GPIO_ENABLE AT %MW0x20 : DWORD;
    GPIO_ENABLE_W1TS AT %MW0x24 : DWORD;
    GPIO_ENABLE_W1TC AT %MW0x28 : DWORD;
    (* I/O MUX *)
    IO_MUX_BASE AT %MW0x60009000 : WORD;
    IO_MUX_GPIO0 AT %MW0x00 : DWORD;
    IO_MUX_GPIO1 AT %MW0x04 : DWORD;
    IO_MUX_GPIO2 AT %MW0x08 : DWORD;
    IO_MUX_GPIO3 AT %MW0x0C : DWORD;
    (* RTC Control *)
    RTC_CNTL_BASE AT %MW0x60008000 : WORD;
    RTC_CNTL_OPTIONS0 AT %MW0x00 : DWORD;
    RTC_CNTL_CLK_CONF AT %MW0x30 : DWORD;

    (* 中断变量 *)
    INT_RESET : BOOL; (*  *)
    INT_MACHINESOFTWARE : BOOL; (*  *)
    INT_MACHINETIMER : BOOL; (*  *)
    INT_MACHINEEXTERNAL : BOOL; (*  *)

END_VAR

BEGIN
    (* ESP32-C3初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|X1>0|--(监控灯)--
    --|X2>0|--(监控灯)--
    --|X3>0|--(监控灯)--
    --|X8>0|--(监控灯)--
    --|X10>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_RESET|--[中断处理]--
    --|INT_MACHINESOFTWARE|--[中断处理]--
    --|INT_MACHINETIMER|--[中断处理]--
    --|INT_MACHINEEXTERNAL|--[中断处理]--

END_PROGRAM
