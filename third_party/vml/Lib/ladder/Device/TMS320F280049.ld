(*
 * TMS320F280049梯形图定义
 * 生成自: Texas Instruments/C2000/TMS320F280049
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 32-bit C28x DSP + CLA MCU with 256KB Flash, 100KB RAM, 100MHz
 * CPU架构: C28x-DSP
 * 位宽: 32位
 * 时钟频率: 100000000 Hz
 *)

PROGRAM TMS320F280049
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    AL AT %M0x00 : WORD; (* Accumulator Low *)
    AH AT %M0x02 : WORD; (* Accumulator High *)
    PH AT %M0x04 : WORD; (* Product High *)
    PL AT %M0x06 : WORD; (* Product Low *)
    TREG AT %M0x08 : WORD; (* Temporary Register *)
    AR0 AT %M0x0A : WORD; (*  *)
    AR1 AT %M0x0C : WORD; (*  *)
    ST0 AT %M0x20 : WORD; (* Status 0 *)
    ST1 AT %M0x22 : WORD; (* Status 1 *)
    PC AT %M0x24 : WORD; (* Program Counter *)
    SP AT %M0x26 : WORD; (* Stack Pointer *)

    (* 外设变量 *)
    (* PLL Clock Control *)
    PLL_BASE AT %MW0x5C10 : WORD;
    PLL_SYSPLLCTL1 AT %MW0x00 : WORD;
    PLL_SYSPLLCTL2 AT %MW0x02 : WORD;
    PLL_CLKSRCCTL1 AT %MW0x04 : WORD;
    PLL_CLKSRCCTL2 AT %MW0x06 : WORD;
    (* GPIO Control Registers *)
    GPIO_CTRL_BASE AT %MW0x7C00 : WORD;
    GPIO_CTRL_GPACTRL AT %MW0x00 : WORD;
    GPIO_CTRL_GPAQSEL1 AT %MW0x02 : WORD;
    GPIO_CTRL_GPAQSEL2 AT %MW0x04 : WORD;
    GPIO_CTRL_GPAMUX1 AT %MW0x06 : WORD;
    GPIO_CTRL_GPAMUX2 AT %MW0x08 : WORD;
    GPIO_CTRL_GPADIR AT %MW0x0A : WORD;
    GPIO_CTRL_GPAPUD AT %MW0x0C : WORD;
    (* GPIO Data Registers *)
    GPIO_DATA_BASE AT %MW0x7F00 : WORD;
    GPIO_DATA_GPADAT AT %MW0x00 : WORD;
    GPIO_DATA_GPASET AT %MW0x02 : WORD;
    GPIO_DATA_GPACLEAR AT %MW0x04 : WORD;
    GPIO_DATA_GPATOGGLE AT %MW0x06 : WORD;
    GPIO_DATA_GPBDAT AT %MW0x08 : WORD;
    GPIO_DATA_GPBSET AT %MW0x0A : WORD;
    GPIO_DATA_GPBCLEAR AT %MW0x0C : WORD;
    GPIO_DATA_GPBTOGGLE AT %MW0x0E : WORD;
    (* GPIO B Control *)
    GPIO_B_CTRL_BASE AT %MW0x7C20 : WORD;
    GPIO_B_CTRL_GPBMUX1 AT %MW0x00 : WORD;
    GPIO_B_CTRL_GPBMUX2 AT %MW0x02 : WORD;
    GPIO_B_CTRL_GPBDIR AT %MW0x04 : WORD;
    GPIO_B_CTRL_GPBPUD AT %MW0x06 : WORD;
    (* SCI-A UART *)
    SCI_A_BASE AT %MW0x7320 : WORD;
    SCI_A_SCICCR AT %MW0x00 : WORD;
    SCI_A_SCICTL1 AT %MW0x02 : WORD;
    SCI_A_SCIBAUD AT %MW0x04 : WORD;
    SCI_A_SCIRXBUF AT %MW0x0A : WORD;
    SCI_A_SCITXBUF AT %MW0x0C : WORD;

    (* 中断变量 *)
    INT_RESET : BOOL; (*  *)
    INT_SCIA_RX : BOOL; (* SCI-A Receive Interrupt *)
    INT_SCIA_TX : BOOL; (* SCI-A Transmit Interrupt *)

END_VAR

BEGIN
    (* TMS320F280049初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|AL>0|--(监控灯)--
    --|AH>0|--(监控灯)--
    --|PH>0|--(监控灯)--
    --|PL>0|--(监控灯)--
    --|TREG>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_RESET|--[中断处理]--
    --|INT_SCIA_RX|--[中断处理]--
    --|INT_SCIA_TX|--[中断处理]--

END_PROGRAM
