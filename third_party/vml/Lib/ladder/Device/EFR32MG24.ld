(*
 * EFR32MG24梯形图定义
 * 生成自: Silicon Labs/EFR32/EFR32MG24
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 32-bit ARM Cortex-M33 MCU with 1536KB Flash, 256KB RAM, 78MHz, Zigbee/Thread/Matter
 * CPU架构: ARM-Cortex-M33
 * 位宽: 32位
 * 时钟频率: 78000000 Hz
 *)

PROGRAM EFR32MG24
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
    (* Clock Management Unit *)
    CMU_BASE AT %MW0x40080000 : WORD;
    CMU_CTRL AT %MW0x00 : DWORD;
    CMU_HFCORECLKCFG AT %MW0x08 : DWORD;
    CMU_HFPERCLKEN0 AT %MW0x10 : DWORD;
    CMU_HFPERCLKEN0_GPIOEN AT %MW0x10.4 : BOOL; (* GPIO clock enable *)
    CMU_HFPERCLKEN0_USART0EN AT %MW0x10.12 : BOOL; (* USART0 clock enable *)
    CMU_HFPERCLKEN0_USART1EN AT %MW0x10.13 : BOOL; (* USART1 clock enable *)
    CMU_LFBCLKEN0 AT %MW0x20 : DWORD;
    (* GPIO Controller *)
    GPIO_BASE AT %MW0x40088000 : WORD;
    GPIO_PORT_A_CTRL AT %MW0x00 : DWORD;
    GPIO_PORT_B_CTRL AT %MW0x04 : DWORD;
    GPIO_PORT_C_CTRL AT %MW0x08 : DWORD;
    GPIO_PORT_D_CTRL AT %MW0x0C : DWORD;
    GPIO_MODEL AT %MW0x10 : DWORD;
    GPIO_MODEH AT %MW0x14 : DWORD;
    GPIO_DOUT AT %MW0x1C : DWORD;
    GPIO_DOUTSET AT %MW0x20 : DWORD;
    GPIO_DOUTCLR AT %MW0x24 : DWORD;
    GPIO_DOUTTGL AT %MW0x28 : DWORD;
    GPIO_DIN AT %MW0x2C : DWORD;
    (* GPIO Port A extended *)
    GPIO_PA_BASE AT %MW0x40088400 : WORD;
    GPIO_PA_PA_CFG AT %MW0x00 : DWORD;
    GPIO_PA_PA_PINOUT AT %MW0x04 : DWORD;
    (* GPIO Port B extended *)
    GPIO_PB_BASE AT %MW0x40088800 : WORD;
    GPIO_PB_PB_CFG AT %MW0x00 : DWORD;
    (* USART 0 *)
    USART0_BASE AT %MW0x40060000 : WORD;
    USART0_CTRL AT %MW0x00 : DWORD;
    USART0_CMD AT %MW0x04 : DWORD;
    USART0_STATUS AT %MW0x08 : DWORD;
    USART0_RXDATA AT %MW0x0C : DWORD;
    USART0_TXDATA AT %MW0x10 : DWORD;
    USART0_CLKDIV AT %MW0x14 : DWORD;

    (* 中断变量 *)
    INT_RESET : BOOL; (*  *)
    INT_SVCALL : BOOL; (*  *)
    INT_USART0_RX : BOOL; (* USART0 Receive Interrupt *)
    INT_USART0_TX : BOOL; (* USART0 Transmit Interrupt *)

END_VAR

BEGIN
    (* EFR32MG24初始化梯形图 *)
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
    --|INT_USART0_RX|--[中断处理]--
    --|INT_USART0_TX|--[中断处理]--

END_PROGRAM
