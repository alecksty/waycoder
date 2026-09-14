(*
 * XMC4500梯形图定义
 * 生成自: Infineon/XMC4000/XMC4500
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 32-bit ARM Cortex-M4 Industrial MCU with 1MB Flash, 160KB RAM, 120MHz
 * CPU架构: ARM-Cortex-M4
 * 位宽: 32位
 * 时钟频率: 120000000 Hz
 *)

PROGRAM XMC4500
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
    (* System Control Unit *)
    SCU_BASE AT %MW0x40020000 : WORD;
    SCU_CLKCR AT %MW0x00 : DWORD;
    SCU_CLKCR_PCLK_SEL AT %MW0x00.0 : BOOL; (* CPU clock selection *)
    SCU_CLKCR_FBKDIV AT %MW0x00.16 : BOOL; (* Feedback divider *)
    SCU_PLLCONFIG AT %MW0x04 : DWORD;
    SCU_OSCHPCTRL AT %MW0x08 : DWORD;
    SCU_CGATSET0 AT %MW0x20 : DWORD;
    SCU_CGATSET0_CG_GATE_GPIO AT %MW0x20.4 : BOOL; (* GPIO gate enable *)
    SCU_CGATCLR0 AT %MW0x24 : DWORD;
    (* Port 0 *)
    PORT0_BASE AT %MW0x48000000 : WORD;
    PORT0_OUT AT %MW0x00 : DWORD;
    PORT0_OMR AT %MW0x04 : DWORD;
    PORT0_IOCR0 AT %MW0x10 : DWORD;
    PORT0_IOCR4 AT %MW0x14 : DWORD;
    PORT0_IOCR8 AT %MW0x18 : DWORD;
    PORT0_IOCR12 AT %MW0x1C : DWORD;
    PORT0_IN AT %MW0x24 : DWORD;
    (* Port 1 *)
    PORT1_BASE AT %MW0x48010000 : WORD;
    PORT1_OUT AT %MW0x00 : DWORD;
    PORT1_OMR AT %MW0x04 : DWORD;
    PORT1_IOCR0 AT %MW0x10 : DWORD;
    PORT1_IOCR4 AT %MW0x14 : DWORD;
    PORT1_IOCR8 AT %MW0x18 : DWORD;
    PORT1_IOCR12 AT %MW0x1C : DWORD;
    PORT1_IN AT %MW0x24 : DWORD;
    (* Port 2 *)
    PORT2_BASE AT %MW0x48020000 : WORD;
    PORT2_OUT AT %MW0x00 : DWORD;
    PORT2_OMR AT %MW0x04 : DWORD;
    PORT2_IOCR0 AT %MW0x10 : DWORD;
    PORT2_IOCR4 AT %MW0x14 : DWORD;
    PORT2_IN AT %MW0x24 : DWORD;
    (* Universal Serial Interface 0 (UART) *)
    USIC0_BASE AT %MW0x48030000 : WORD;
    USIC0_CCR AT %MW0x00 : DWORD;
    USIC0_PCR AT %MW0x04 : DWORD;
    USIC0_RBUF AT %MW0x08 : DWORD;
    USIC0_TBUF AT %MW0x0C : DWORD;
    USIC0_BRG AT %MW0x10 : DWORD;

    (* 中断变量 *)
    INT_RESET : BOOL; (*  *)
    INT_SVCALL : BOOL; (*  *)
    INT_USIC0_SR0 : BOOL; (* USIC0 Service Request 0 *)

END_VAR

BEGIN
    (* XMC4500初始化梯形图 *)
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
    --|INT_USIC0_SR0|--[中断处理]--

END_PROGRAM
