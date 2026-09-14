(*
 * IBM-PC-5150梯形图定义
 * 生成自: IBM/Personal Computer/IBM-PC-5150
 * 版本: 1.0
 * 日期: 2026-04-16
 * 作者: VML Team
 * 描述: Original IBM Personal Computer Model 5150
 * CPU架构: x86
 * 位宽: 16位
 * 时钟频率: 4772727 Hz
 *)

PROGRAM IBM_PC_5150
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    AX AT %M0x0 : WORD; (* Accumulator Register *)
    BX AT %M0x1 : WORD; (* Base Register *)
    CX AT %M0x2 : WORD; (* Count Register *)
    DX AT %M0x3 : WORD; (* Data Register *)
    SI AT %M0x4 : WORD; (* Source Index *)
    DI AT %M0x5 : WORD; (* Destination Index *)
    BP AT %M0x6 : WORD; (* Base Pointer *)
    SP AT %M0x7 : WORD; (* Stack Pointer *)
    CS AT %M0x8 : WORD; (* Code Segment *)
    DS AT %M0x9 : WORD; (* Data Segment *)
    ES AT %M0xA : WORD; (* Extra Segment *)
    SS AT %M0xB : WORD; (* Stack Segment *)
    IP AT %M0xC : WORD; (* Instruction Pointer *)
    FLAGS AT %M0xD : WORD; (* Flags Register *)
    FLAGS_CF AT %M0xD.0 : BOOL; (* Carry Flag *)
    FLAGS_PF AT %M0xD.2 : BOOL; (* Parity Flag *)
    FLAGS_AF AT %M0xD.4 : BOOL; (* Auxiliary Carry Flag *)
    FLAGS_ZF AT %M0xD.6 : BOOL; (* Zero Flag *)
    FLAGS_SF AT %M0xD.7 : BOOL; (* Sign Flag *)
    FLAGS_TF AT %M0xD.8 : BOOL; (* Trap Flag *)
    FLAGS_IF AT %M0xD.9 : BOOL; (* Interrupt Enable Flag *)
    FLAGS_DF AT %M0xD.10 : BOOL; (* Direction Flag *)
    FLAGS_OF AT %M0xD.11 : BOOL; (* Overflow Flag *)

    (* 外设变量 *)
    (* Programmable Interrupt Controller *)
    PIC_BASE AT %MW0x20 : WORD;
    PIC_PIC1_CMD AT %MW0x20 : BYTE;
    PIC_PIC1_DATA AT %MW0x21 : BYTE;
    PIC_PIC2_CMD AT %MW0xA0 : BYTE;
    PIC_PIC2_DATA AT %MW0xA1 : BYTE;
    (* Programmable Interval Timer *)
    PIT_BASE AT %MW0x40 : WORD;
    PIT_PIT_CH0 AT %MW0x40 : BYTE;
    PIT_PIT_CH1 AT %MW0x41 : BYTE;
    PIT_PIT_CH2 AT %MW0x42 : BYTE;
    PIT_PIT_CTRL AT %MW0x43 : BYTE;
    (* Programmable Peripheral Interface *)
    PPI_BASE AT %MW0x60 : WORD;
    PPI_PPI_PA AT %MW0x60 : BYTE;
    PPI_PPI_PB AT %MW0x61 : BYTE;
    PPI_PPI_PC AT %MW0x62 : BYTE;
    PPI_PPI_CTRL AT %MW0x63 : BYTE;
    (* Direct Memory Access Controller *)
    DMA_BASE AT %MW0x00 : WORD;
    DMA_DMA_CH0_ADDR AT %MW0x00 : WORD;
    DMA_DMA_CH0_COUNT AT %MW0x01 : WORD;
    DMA_DMA_CMD AT %MW0x08 : BYTE;
    DMA_DMA_MASK AT %MW0x0A : BYTE;
    DMA_DMA_MODE AT %MW0x0B : BYTE;
    (* Color Graphics Adapter *)
    CGA_BASE AT %MW0x3D4 : WORD;
    CGA_CGA_INDEX AT %MW0x3D4 : BYTE;
    CGA_CGA_DATA AT %MW0x3D5 : BYTE;
    CGA_CGA_MODE AT %MW0x3D8 : BYTE;
    CGA_CGA_COLOR AT %MW0x3D9 : BYTE;

    (* 中断变量 *)
    INT_DIVIDE_ERROR : BOOL; (* Divide Error *)
    INT_SINGLE_STEP : BOOL; (* Single Step *)
    INT_NMI : BOOL; (* Non-Maskable Interrupt *)
    INT_BREAKPOINT : BOOL; (* Breakpoint *)
    INT_OVERFLOW : BOOL; (* Overflow *)
    INT_PRINT_SCREEN : BOOL; (* Print Screen *)
    INT_IRQ0 : BOOL; (* Timer Interrupt *)
    INT_IRQ1 : BOOL; (* Keyboard Interrupt *)
    INT_IRQ2 : BOOL; (* Cascade (8259A) *)
    INT_IRQ3 : BOOL; (* COM2 *)
    INT_IRQ4 : BOOL; (* COM1 *)
    INT_IRQ5 : BOOL; (* LPT2 *)
    INT_IRQ6 : BOOL; (* Floppy Disk *)
    INT_IRQ7 : BOOL; (* LPT1 *)
    INT_IRQ8 : BOOL; (* Real Time Clock *)
    INT_IRQ11 : BOOL; (* Reserved *)
    INT_IRQ13 : BOOL; (* Coprocessor *)
    INT_IRQ15 : BOOL; (* Reserved *)

END_VAR

BEGIN
    (* IBM-PC-5150初始化梯形图 *)
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
    --|INT_SINGLE_STEP|--[中断处理]--
    --|INT_NMI|--[中断处理]--
    --|INT_BREAKPOINT|--[中断处理]--
    --|INT_OVERFLOW|--[中断处理]--
    --|INT_PRINT_SCREEN|--[中断处理]--
    --|INT_IRQ0|--[中断处理]--
    --|INT_IRQ1|--[中断处理]--
    --|INT_IRQ2|--[中断处理]--
    --|INT_IRQ3|--[中断处理]--
    --|INT_IRQ4|--[中断处理]--
    --|INT_IRQ5|--[中断处理]--
    --|INT_IRQ6|--[中断处理]--
    --|INT_IRQ7|--[中断处理]--
    --|INT_IRQ8|--[中断处理]--
    --|INT_IRQ11|--[中断处理]--
    --|INT_IRQ13|--[中断处理]--
    --|INT_IRQ15|--[中断处理]--

END_PROGRAM
