/**
 * IBM PC/AT 寄存器定义
 * 生成自: IBM/IBM PC/IBM PC/AT
 * 版本: 
 */
export const ibm_pc_at = {
  // CPU: x86-16, 0位, 0 Hz

  // 外设定义
  // Programmable Interrupt Controller
  8259A_BASE: ,
  8259A_ICW1: 0x00000020,
  8259A_ICW2: 0x00000021,
  8259A_ICW3: 0x00000021,
  8259A_ICW4: 0x00000021,
  8259A_OCW1: 0x00000021,
  8259A_OCW2: 0x00000020,
  8259A_OCW3: 0x00000020,
  // Programmable Interval Timer
  8253_BASE: ,
  8253_Counter0: 0x00000040,
  8253_Counter1: 0x00000041,
  8253_Counter2: 0x00000042,
  8253_Control: 0x00000043,
  // Direct Memory Access Controller
  8237_BASE: ,
  8237_Channel0: 0x00000000,
  8237_Channel1: 0x00000002,
  8237_Channel2: 0x00000004,
  8237_Channel3: 0x00000006,
  8237_Status: 0x00000008,
  8237_Command: 0x00000008,
  8237_Request: 0x00000009,
  8237_Mask: 0x0000000A,
  8237_Mode: 0x0000000B,
  // Keyboard Controller
  8042_BASE: ,
  8042_Data: 0x00000060,
  8042_Status: 0x00000064,
  // Real-Time Clock with CMOS RAM
  CMOS_BASE: ,
  CMOS_Address: 0x00000070,
  CMOS_Data: 0x00000071,
  // Floppy Disk Controller
  FDC_BASE: ,
  FDC_SRA: 0x000003F2,
  FDC_MSR: 0x000003F4,
  FDC_DATA: 0x000003F5,
  FDC_DIR: 0x000003F7,
  FDC_CCR: 0x000003F7,
  // Hard Disk Controller (ST-506/412)
  HDC_BASE: ,
  HDC_Data: 0x000001F0,
  HDC_Error: 0x000001F1,
  HDC_SectorCount: 0x000001F2,
  HDC_SectorNumber: 0x000001F3,
  HDC_CylinderLow: 0x000001F4,
  HDC_CylinderHigh: 0x000001F5,
  HDC_DriveHead: 0x000001F6,
  HDC_Status: 0x000001F7,
  HDC_Command: 0x000001F7,
  // Color Graphics Adapter
  CGA_BASE: ,
  CGA_CRTC_Index: 0x000003D4,
  CGA_CRTC_Data: 0x000003D5,
  CGA_ModeControl: 0x000003D8,
  CGA_ColorSelect: 0x000003D9,
  CGA_Status: 0x000003DA,
  // Enhanced Graphics Adapter
  EGA_BASE: ,
  EGA_CRTC_Index: 0x000003D4,
  EGA_CRTC_Data: 0x000003D5,
  EGA_FeatureControl: 0x000003DA,
  EGA_Graphics1Pos: 0x000003CC,
  EGA_Graphics2Pos: 0x000003CA,
  EGA_SequencerIndex: 0x000003C4,
  EGA_SequencerData: 0x000003C5,
  EGA_GraphicsIndex: 0x000003CE,
  EGA_GraphicsData: 0x000003CF,
  EGA_AttributeIndex: 0x000003C0,
  EGA_AttributeData: 0x000003C1,
  // Video Graphics Array
  VGA_BASE: ,
  VGA_CRTC_Index: 0x000003D4,
  VGA_CRTC_Data: 0x000003D5,
  VGA_InputStatus1: 0x000003DA,
  VGA_FeatureControl: 0x000003DA,
  VGA_MiscOutput: 0x000003C2,
  VGA_SequencerIndex: 0x000003C4,
  VGA_SequencerData: 0x000003C5,
  VGA_GraphicsIndex: 0x000003CE,
  VGA_GraphicsData: 0x000003CF,
  VGA_AttributeIndex: 0x000003C0,
  VGA_AttributeData: 0x000003C1,
  VGA_DACMask: 0x000003C6,
  VGA_DACReadIndex: 0x000003C7,
  VGA_DACWriteIndex: 0x000003C8,
  VGA_DACData: 0x000003C9,
  // Game Port
  GamePort_BASE: ,
  GamePort_Data: 0x00000201,
  // Parallel Printer Port
  ParallelPort_BASE: ,
  ParallelPort_Data: 0x00000378,
  ParallelPort_Status: 0x00000379,
  ParallelPort_Control: 0x0000037A,
  // Serial Communications Port
  SerialPort_BASE: ,
  SerialPort_Data: 0x000003F8,
  SerialPort_IER: 0x000003F9,
  SerialPort_IIR: 0x000003FA,
  SerialPort_LCR: 0x000003FB,
  SerialPort_MCR: 0x000003FC,
  SerialPort_LSR: 0x000003FD,
  SerialPort_MSR: 0x000003FE,
  SerialPort_SCR: 0x000003FF,
  // PC Speaker
  Speaker_BASE: ,
  Speaker_Control: 0x00000061,

  // 中断向量
  IRQ_Divide Error: 0,  // Division by zero
  IRQ_Single Step: 1,  // Debug single step
  IRQ_NMI: 2,  // Non-maskable interrupt
  IRQ_Breakpoint: 3,  // INT 3 instruction
  IRQ_Overflow: 4,  // INTO instruction
  IRQ_Print Screen: 5,  // Print screen key
  IRQ_IRQ0: 8,  // Timer interrupt
  IRQ_IRQ1: 9,  // Keyboard interrupt
  IRQ_IRQ2: 10,  // Cascade to IRQ8-15
  IRQ_IRQ3: 11,  // COM2 interrupt
  IRQ_IRQ4: 12,  // COM1 interrupt
  IRQ_IRQ5: 13,  // LPT2 interrupt
  IRQ_IRQ6: 14,  // Floppy disk interrupt
  IRQ_IRQ7: 15,  // LPT1 interrupt
  IRQ_IRQ8: 112,  // Real-time clock interrupt
  IRQ_IRQ9: 113,  // Redirected IRQ2
  IRQ_IRQ10: 114,  // Reserved
  IRQ_IRQ11: 115,  // Reserved
  IRQ_IRQ12: 116,  // PS/2 mouse interrupt
  IRQ_IRQ13: 117,  // Coprocessor interrupt
  IRQ_IRQ14: 118,  // Primary IDE interrupt
  IRQ_IRQ15: 119,  // Secondary IDE interrupt
  IRQ_Video Services: 16,  // Video BIOS services
  IRQ_Disk Services: 19,  // Disk BIOS services
  IRQ_DOS Services: 21,  // DOS function calls

  init: function() {
    // 硬件初始化
  }
};
