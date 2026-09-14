(*
 * BL618梯形图定义
 * 生成自: Bouffalo Lab/BL6/BL618
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 32-bit RISC-V RV32IMAFC WiFi6 + BLE SoC with 4MB Flash, 512KB SRAM, 480MHz
 * CPU架构: RISC-V
 * 位宽: 32位
 * 时钟频率: 320000000 Hz
 *)

PROGRAM BL618
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
    (* Global Control (Clock and Reset) *)
    GLB_BASE AT %MW0x30000000 : WORD;
    GLB_GLB_CLK_EN AT %MW0x10 : DWORD;
    GLB_GLB_CLK_EN_GPIO_CLK_EN AT %MW0x10.6 : BOOL; (* GPIO clock enable *)
    GLB_GLB_CLK_EN_UART0_CLK_EN AT %MW0x10.12 : BOOL; (* UART0 clock enable *)
    GLB_GLB_SYS_CLK_CTRL AT %MW0x14 : DWORD;
    GLB_GLB_PLL_CTRL AT %MW0x1C : DWORD;
    (* GPIO Port A *)
    GPIO_P0_BASE AT %MW0x30007000 : WORD;
    GPIO_P0_GPIO_CFG0 AT %MW0x00 : DWORD;
    GPIO_P0_GPIO_CFG1 AT %MW0x04 : DWORD;
    GPIO_P0_GPIO_OE AT %MW0x08 : DWORD;
    GPIO_P0_GPIO_OUT AT %MW0x0C : DWORD;
    GPIO_P0_GPIO_IN AT %MW0x10 : DWORD;
    GPIO_P0_GPIO_SET AT %MW0x14 : DWORD;
    GPIO_P0_GPIO_CLR AT %MW0x18 : DWORD;
    GPIO_P0_GPIO_TOG AT %MW0x1C : DWORD;
    (* GPIO Port B *)
    GPIO_P1_BASE AT %MW0x30007200 : WORD;
    GPIO_P1_GPIO_CFG0 AT %MW0x00 : DWORD;
    GPIO_P1_GPIO_CFG1 AT %MW0x04 : DWORD;
    GPIO_P1_GPIO_OE AT %MW0x08 : DWORD;
    GPIO_P1_GPIO_OUT AT %MW0x0C : DWORD;
    GPIO_P1_GPIO_IN AT %MW0x10 : DWORD;
    GPIO_P1_GPIO_SET AT %MW0x14 : DWORD;
    GPIO_P1_GPIO_CLR AT %MW0x18 : DWORD;
    GPIO_P1_GPIO_TOG AT %MW0x1C : DWORD;
    (* UART 0 *)
    UART0_BASE AT %MW0x30002000 : WORD;
    UART0_UART_CR AT %MW0x00 : DWORD;
    UART0_UART_BRR AT %MW0x04 : DWORD;
    UART0_UART_TDR AT %MW0x08 : DWORD;
    UART0_UART_RDR AT %MW0x0C : DWORD;
    UART0_UART_SR AT %MW0x10 : DWORD;

    (* 中断变量 *)
    INT_RESET : BOOL; (*  *)
    INT_MACHINESOFTWARE : BOOL; (*  *)
    INT_MACHINETIMER : BOOL; (*  *)
    INT_MACHINEEXTERNAL : BOOL; (*  *)
    INT_UART0 : BOOL; (* UART0 Interrupt *)

END_VAR

BEGIN
    (* BL618初始化梯形图 *)
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
    --|INT_UART0|--[中断处理]--

END_PROGRAM
