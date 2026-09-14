(*
 * IBM PC/AT梯形图定义
 * 生成自: IBM/IBM PC/IBM PC/AT
 * 版本: 
 * 日期: 
 * 作者: 
 * 描述: IBM Personal Computer/Advanced Technology (Model 5170)
 * CPU架构: x86-16
 * 位宽: 0位
 * 时钟频率: 0 Hz
 *)

PROGRAM IBM PC/AT
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* Programmable Interrupt Controller *)
    _8259A_BASE AT %MW : WORD;
    _8259A_ICW1 AT %MW0x20 : LWORD;
    _8259A_ICW2 AT %MW0x21 : LWORD;
    _8259A_ICW3 AT %MW0x21 : LWORD;
    _8259A_ICW4 AT %MW0x21 : LWORD;
    _8259A_OCW1 AT %MW0x21 : LWORD;
    _8259A_OCW2 AT %MW0x20 : LWORD;
    _8259A_OCW3 AT %MW0x20 : LWORD;
    (* Programmable Interval Timer *)
    _8253_BASE AT %MW : WORD;
    _8253_COUNTER0 AT %MW0x40 : LWORD;
    _8253_COUNTER1 AT %MW0x41 : LWORD;
    _8253_COUNTER2 AT %MW0x42 : LWORD;
    _8253_CONTROL AT %MW0x43 : LWORD;
    (* Direct Memory Access Controller *)
    _8237_BASE AT %MW : WORD;
    _8237_CHANNEL0 AT %MW0x00 : ARRAY[0..15] OF BOOL;
    _8237_CHANNEL1 AT %MW0x02 : ARRAY[0..15] OF BOOL;
    _8237_CHANNEL2 AT %MW0x04 : ARRAY[0..15] OF BOOL;
    _8237_CHANNEL3 AT %MW0x06 : ARRAY[0..15] OF BOOL;
    _8237_STATUS AT %MW0x08 : LWORD;
    _8237_COMMAND AT %MW0x08 : LWORD;
    _8237_REQUEST AT %MW0x09 : LWORD;
    _8237_MASK AT %MW0x0A : LWORD;
    _8237_MODE AT %MW0x0B : LWORD;
    (* Keyboard Controller *)
    _8042_BASE AT %MW : WORD;
    _8042_DATA AT %MW0x60 : LWORD;
    _8042_STATUS AT %MW0x64 : LWORD;
    (* Real-Time Clock with CMOS RAM *)
    CMOS_BASE AT %MW : WORD;
    CMOS_ADDRESS AT %MW0x70 : LWORD;
    CMOS_DATA AT %MW0x71 : LWORD;
    (* Floppy Disk Controller *)
    FDC_BASE AT %MW : WORD;
    FDC_SRA AT %MW0x3F2 : LWORD;
    FDC_MSR AT %MW0x3F4 : LWORD;
    FDC_DATA AT %MW0x3F5 : LWORD;
    FDC_DIR AT %MW0x3F7 : LWORD;
    FDC_CCR AT %MW0x3F7 : LWORD;
    (* Hard Disk Controller (ST-506/412) *)
    HDC_BASE AT %MW : WORD;
    HDC_DATA AT %MW0x1F0 : ARRAY[0..15] OF BOOL;
    HDC_ERROR AT %MW0x1F1 : LWORD;
    HDC_SECTORCOUNT AT %MW0x1F2 : LWORD;
    HDC_SECTORNUMBER AT %MW0x1F3 : LWORD;
    HDC_CYLINDERLOW AT %MW0x1F4 : LWORD;
    HDC_CYLINDERHIGH AT %MW0x1F5 : LWORD;
    HDC_DRIVEHEAD AT %MW0x1F6 : LWORD;
    HDC_STATUS AT %MW0x1F7 : LWORD;
    HDC_COMMAND AT %MW0x1F7 : LWORD;
    (* Color Graphics Adapter *)
    CGA_BASE AT %MW : WORD;
    CGA_CRTC_INDEX AT %MW0x3D4 : LWORD;
    CGA_CRTC_DATA AT %MW0x3D5 : LWORD;
    CGA_MODECONTROL AT %MW0x3D8 : LWORD;
    CGA_COLORSELECT AT %MW0x3D9 : LWORD;
    CGA_STATUS AT %MW0x3DA : LWORD;
    (* Enhanced Graphics Adapter *)
    EGA_BASE AT %MW : WORD;
    EGA_CRTC_INDEX AT %MW0x3D4 : LWORD;
    EGA_CRTC_DATA AT %MW0x3D5 : LWORD;
    EGA_FEATURECONTROL AT %MW0x3DA : LWORD;
    EGA_GRAPHICS1POS AT %MW0x3CC : LWORD;
    EGA_GRAPHICS2POS AT %MW0x3CA : LWORD;
    EGA_SEQUENCERINDEX AT %MW0x3C4 : LWORD;
    EGA_SEQUENCERDATA AT %MW0x3C5 : LWORD;
    EGA_GRAPHICSINDEX AT %MW0x3CE : LWORD;
    EGA_GRAPHICSDATA AT %MW0x3CF : LWORD;
    EGA_ATTRIBUTEINDEX AT %MW0x3C0 : LWORD;
    EGA_ATTRIBUTEDATA AT %MW0x3C1 : LWORD;
    (* Video Graphics Array *)
    VGA_BASE AT %MW : WORD;
    VGA_CRTC_INDEX AT %MW0x3D4 : LWORD;
    VGA_CRTC_DATA AT %MW0x3D5 : LWORD;
    VGA_INPUTSTATUS1 AT %MW0x3DA : LWORD;
    VGA_FEATURECONTROL AT %MW0x3DA : LWORD;
    VGA_MISCOUTPUT AT %MW0x3C2 : LWORD;
    VGA_SEQUENCERINDEX AT %MW0x3C4 : LWORD;
    VGA_SEQUENCERDATA AT %MW0x3C5 : LWORD;
    VGA_GRAPHICSINDEX AT %MW0x3CE : LWORD;
    VGA_GRAPHICSDATA AT %MW0x3CF : LWORD;
    VGA_ATTRIBUTEINDEX AT %MW0x3C0 : LWORD;
    VGA_ATTRIBUTEDATA AT %MW0x3C1 : LWORD;
    VGA_DACMASK AT %MW0x3C6 : LWORD;
    VGA_DACREADINDEX AT %MW0x3C7 : LWORD;
    VGA_DACWRITEINDEX AT %MW0x3C8 : LWORD;
    VGA_DACDATA AT %MW0x3C9 : LWORD;
    (* Game Port *)
    GAMEPORT_BASE AT %MW : WORD;
    GAMEPORT_DATA AT %MW0x201 : LWORD;
    (* Parallel Printer Port *)
    PARALLELPORT_BASE AT %MW : WORD;
    PARALLELPORT_DATA AT %MW0x378 : LWORD;
    PARALLELPORT_STATUS AT %MW0x379 : LWORD;
    PARALLELPORT_CONTROL AT %MW0x37A : LWORD;
    (* Serial Communications Port *)
    SERIALPORT_BASE AT %MW : WORD;
    SERIALPORT_DATA AT %MW0x3F8 : LWORD;
    SERIALPORT_IER AT %MW0x3F9 : LWORD;
    SERIALPORT_IIR AT %MW0x3FA : LWORD;
    SERIALPORT_LCR AT %MW0x3FB : LWORD;
    SERIALPORT_MCR AT %MW0x3FC : LWORD;
    SERIALPORT_LSR AT %MW0x3FD : LWORD;
    SERIALPORT_MSR AT %MW0x3FE : LWORD;
    SERIALPORT_SCR AT %MW0x3FF : LWORD;
    (* PC Speaker *)
    SPEAKER_BASE AT %MW : WORD;
    SPEAKER_CONTROL AT %MW0x61 : LWORD;

    (* 中断变量 *)
    INT_DIVIDE_ERROR : BOOL; (* Division by zero *)
    INT_SINGLE_STEP : BOOL; (* Debug single step *)
    INT_NMI : BOOL; (* Non-maskable interrupt *)
    INT_BREAKPOINT : BOOL; (* INT 3 instruction *)
    INT_OVERFLOW : BOOL; (* INTO instruction *)
    INT_PRINT_SCREEN : BOOL; (* Print screen key *)
    INT_IRQ0 : BOOL; (* Timer interrupt *)
    INT_IRQ1 : BOOL; (* Keyboard interrupt *)
    INT_IRQ2 : BOOL; (* Cascade to IRQ8-15 *)
    INT_IRQ3 : BOOL; (* COM2 interrupt *)
    INT_IRQ4 : BOOL; (* COM1 interrupt *)
    INT_IRQ5 : BOOL; (* LPT2 interrupt *)
    INT_IRQ6 : BOOL; (* Floppy disk interrupt *)
    INT_IRQ7 : BOOL; (* LPT1 interrupt *)
    INT_IRQ8 : BOOL; (* Real-time clock interrupt *)
    INT_IRQ9 : BOOL; (* Redirected IRQ2 *)
    INT_IRQ10 : BOOL; (* Reserved *)
    INT_IRQ11 : BOOL; (* Reserved *)
    INT_IRQ12 : BOOL; (* PS/2 mouse interrupt *)
    INT_IRQ13 : BOOL; (* Coprocessor interrupt *)
    INT_IRQ14 : BOOL; (* Primary IDE interrupt *)
    INT_IRQ15 : BOOL; (* Secondary IDE interrupt *)
    INT_VIDEO_SERVICES : BOOL; (* Video BIOS services *)
    INT_DISK_SERVICES : BOOL; (* Disk BIOS services *)
    INT_DOS_SERVICES : BOOL; (* DOS function calls *)

END_VAR

BEGIN
    (* IBM PC/AT初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
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
    --|INT_IRQ9|--[中断处理]--
    --|INT_IRQ10|--[中断处理]--
    --|INT_IRQ11|--[中断处理]--
    --|INT_IRQ12|--[中断处理]--
    --|INT_IRQ13|--[中断处理]--
    --|INT_IRQ14|--[中断处理]--
    --|INT_IRQ15|--[中断处理]--
    --|INT_VIDEO_SERVICES|--[中断处理]--
    --|INT_DISK_SERVICES|--[中断处理]--
    --|INT_DOS_SERVICES|--[中断处理]--

END_PROGRAM
