(*
 * 8086梯形图定义
 * 生成自: Intel/x86/8086
 * 版本: 1.0
 * 日期: 2026-04-17
 * 作者: VML Team
 * 描述: 16-bit microprocessor, first x86 processor
 * CPU架构: x86
 * 位宽: 16位
 * 时钟频率: 5000000 Hz
 *)

PROGRAM 8086
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    AX AT %M0 : WORD; (* Accumulator *)
    AX_AH AT %M0.8 : BOOL; (* High byte of AX *)
    AX_AL AT %M0.0 : BOOL; (* Low byte of AX *)
    BX AT %M1 : WORD; (* Base *)
    BX_BH AT %M1.8 : BOOL; (* High byte of BX *)
    BX_BL AT %M1.0 : BOOL; (* Low byte of BX *)
    CX AT %M2 : WORD; (* Counter *)
    CX_CH AT %M2.8 : BOOL; (* High byte of CX *)
    CX_CL AT %M2.0 : BOOL; (* Low byte of CX *)
    DX AT %M3 : WORD; (* Data *)
    DX_DH AT %M3.8 : BOOL; (* High byte of DX *)
    DX_DL AT %M3.0 : BOOL; (* Low byte of DX *)
    SI AT %M4 : WORD; (* Source Index *)
    DI AT %M5 : WORD; (* Destination Index *)
    BP AT %M6 : WORD; (* Base Pointer *)
    SP AT %M7 : WORD; (* Stack Pointer *)
    IP AT %M8 : WORD; (* Instruction Pointer *)
    CS AT %M9 : WORD; (* Code Segment *)
    DS AT %M10 : WORD; (* Data Segment *)
    ES AT %M11 : WORD; (* Extra Segment *)
    SS AT %M12 : WORD; (* Stack Segment *)
    FLAGS AT %M13 : WORD; (* Flags Register *)
    FLAGS_CF AT %M13.0 : BOOL; (* Carry Flag *)
    FLAGS_PF AT %M13.2 : BOOL; (* Parity Flag *)
    FLAGS_AF AT %M13.4 : BOOL; (* Auxiliary Flag *)
    FLAGS_ZF AT %M13.6 : BOOL; (* Zero Flag *)
    FLAGS_SF AT %M13.7 : BOOL; (* Sign Flag *)
    FLAGS_TF AT %M13.8 : BOOL; (* Trap Flag *)
    FLAGS_IF AT %M13.9 : BOOL; (* Interrupt Enable Flag *)
    FLAGS_DF AT %M13.10 : BOOL; (* Direction Flag *)
    FLAGS_OF AT %M13.11 : BOOL; (* Overflow Flag *)

    (* 外设变量 *)
    (* Programmable Interrupt Controller *)
    PIC_BASE AT %MW0x0020 : WORD;
    PIC_PIC1_CMD AT %MW0x0020 : BYTE;
    PIC_PIC1_DATA AT %MW0x0021 : BYTE;
    PIC_PIC2_CMD AT %MW0x00A0 : BYTE;
    PIC_PIC2_DATA AT %MW0x00A1 : BYTE;
    (* Programmable Interval Timer *)
    PIT_BASE AT %MW0x0040 : WORD;
    PIT_PIT_CH0 AT %MW0x0040 : BYTE;
    PIT_PIT_CH1 AT %MW0x0041 : BYTE;
    PIT_PIT_CH2 AT %MW0x0042 : BYTE;
    PIT_PIT_CMD AT %MW0x0043 : BYTE;
    (* Programmable Peripheral Interface *)
    PPI_BASE AT %MW0x0060 : WORD;
    PPI_PPI_PA AT %MW0x0060 : BYTE;
    PPI_PPI_PB AT %MW0x0061 : BYTE;
    PPI_PPI_PC AT %MW0x0062 : BYTE;
    PPI_PPI_CMD AT %MW0x0063 : BYTE;

    (* 中断变量 *)
    INT_DIVIDE_ERROR : BOOL; (* Divide by zero *)
    INT_DEBUG : BOOL; (* Single step *)
    INT_NMI : BOOL; (* Non-maskable interrupt *)
    INT_BREAKPOINT : BOOL; (* Breakpoint *)
    INT_OVERFLOW : BOOL; (* INTO detected overflow *)
    INT_IRQ0 : BOOL; (* Timer interrupt *)
    INT_IRQ1 : BOOL; (* Keyboard interrupt *)
    INT_IRQ2 : BOOL; (* Cascade *)
    INT_IRQ3 : BOOL; (* COM2 *)
    INT_IRQ4 : BOOL; (* COM1 *)
    INT_IRQ5 : BOOL; (* LPT2 *)
    INT_IRQ6 : BOOL; (* Floppy disk *)
    INT_IRQ7 : BOOL; (* LPT1 *)

END_VAR

BEGIN
    (* 8086初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|AX>0|--(监控灯)--
    --|BX>0|--(监控灯)--
    --|CX>0|--(监控灯)--
    --|DX>0|--(监控灯)--
    --|SI>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_DIVIDE_ERROR|--[中断处理]--
    --|INT_DEBUG|--[中断处理]--
    --|INT_NMI|--[中断处理]--
    --|INT_BREAKPOINT|--[中断处理]--
    --|INT_OVERFLOW|--[中断处理]--
    --|INT_IRQ0|--[中断处理]--
    --|INT_IRQ1|--[中断处理]--
    --|INT_IRQ2|--[中断处理]--
    --|INT_IRQ3|--[中断处理]--
    --|INT_IRQ4|--[中断处理]--
    --|INT_IRQ5|--[中断处理]--
    --|INT_IRQ6|--[中断处理]--
    --|INT_IRQ7|--[中断处理]--

END_PROGRAM
