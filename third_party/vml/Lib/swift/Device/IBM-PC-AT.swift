//
// IBM PC/AT Register Definitions
// Generated from: IBM Personal Computer/Advanced Technology (Model 5170)
// Version: 
// Date: 
//

import Foundation

// MARK: - 8259A (Programmable Interrupt Controller)
let 8259A_ICW1: UInt64 = 0x0x20
let 8259A_ICW2: UInt64 = 0x0x21
let 8259A_ICW3: UInt64 = 0x0x21
let 8259A_ICW4: UInt64 = 0x0x21
let 8259A_OCW1: UInt64 = 0x0x21
let 8259A_OCW2: UInt64 = 0x0x20
let 8259A_OCW3: UInt64 = 0x0x20

// MARK: - 8253 (Programmable Interval Timer)
let 8253_Counter0: UInt64 = 0x0x40
let 8253_Counter1: UInt64 = 0x0x41
let 8253_Counter2: UInt64 = 0x0x42
let 8253_Control: UInt64 = 0x0x43

// MARK: - 8237 (Direct Memory Access Controller)
let 8237_Channel0: UInt32 = 0x0x00
let 8237_Channel1: UInt32 = 0x0x02
let 8237_Channel2: UInt32 = 0x0x04
let 8237_Channel3: UInt32 = 0x0x06
let 8237_Status: UInt64 = 0x0x08
let 8237_Command: UInt64 = 0x0x08
let 8237_Request: UInt64 = 0x0x09
let 8237_Mask: UInt64 = 0x0x0A
let 8237_Mode: UInt64 = 0x0x0B

// MARK: - 8042 (Keyboard Controller)
let 8042_Data: UInt64 = 0x0x60
let 8042_Status: UInt64 = 0x0x64

// MARK: - CMOS (Real-Time Clock with CMOS RAM)
let CMOS_Address: UInt64 = 0x0x70
let CMOS_Data: UInt64 = 0x0x71

// MARK: - FDC (Floppy Disk Controller)
let FDC_SRA: UInt64 = 0x0x3F2
let FDC_MSR: UInt64 = 0x0x3F4
let FDC_DATA: UInt64 = 0x0x3F5
let FDC_DIR: UInt64 = 0x0x3F7
let FDC_CCR: UInt64 = 0x0x3F7

// MARK: - HDC (Hard Disk Controller (ST-506/412))
let HDC_Data: UInt32 = 0x0x1F0
let HDC_Error: UInt64 = 0x0x1F1
let HDC_SectorCount: UInt64 = 0x0x1F2
let HDC_SectorNumber: UInt64 = 0x0x1F3
let HDC_CylinderLow: UInt64 = 0x0x1F4
let HDC_CylinderHigh: UInt64 = 0x0x1F5
let HDC_DriveHead: UInt64 = 0x0x1F6
let HDC_Status: UInt64 = 0x0x1F7
let HDC_Command: UInt64 = 0x0x1F7

// MARK: - CGA (Color Graphics Adapter)
let CGA_CRTC_Index: UInt64 = 0x0x3D4
let CGA_CRTC_Data: UInt64 = 0x0x3D5
let CGA_ModeControl: UInt64 = 0x0x3D8
let CGA_ColorSelect: UInt64 = 0x0x3D9
let CGA_Status: UInt64 = 0x0x3DA

// MARK: - EGA (Enhanced Graphics Adapter)
let EGA_CRTC_Index: UInt64 = 0x0x3D4
let EGA_CRTC_Data: UInt64 = 0x0x3D5
let EGA_FeatureControl: UInt64 = 0x0x3DA
let EGA_Graphics1Pos: UInt64 = 0x0x3CC
let EGA_Graphics2Pos: UInt64 = 0x0x3CA
let EGA_SequencerIndex: UInt64 = 0x0x3C4
let EGA_SequencerData: UInt64 = 0x0x3C5
let EGA_GraphicsIndex: UInt64 = 0x0x3CE
let EGA_GraphicsData: UInt64 = 0x0x3CF
let EGA_AttributeIndex: UInt64 = 0x0x3C0
let EGA_AttributeData: UInt64 = 0x0x3C1

// MARK: - VGA (Video Graphics Array)
let VGA_CRTC_Index: UInt64 = 0x0x3D4
let VGA_CRTC_Data: UInt64 = 0x0x3D5
let VGA_InputStatus1: UInt64 = 0x0x3DA
let VGA_FeatureControl: UInt64 = 0x0x3DA
let VGA_MiscOutput: UInt64 = 0x0x3C2
let VGA_SequencerIndex: UInt64 = 0x0x3C4
let VGA_SequencerData: UInt64 = 0x0x3C5
let VGA_GraphicsIndex: UInt64 = 0x0x3CE
let VGA_GraphicsData: UInt64 = 0x0x3CF
let VGA_AttributeIndex: UInt64 = 0x0x3C0
let VGA_AttributeData: UInt64 = 0x0x3C1
let VGA_DACMask: UInt64 = 0x0x3C6
let VGA_DACReadIndex: UInt64 = 0x0x3C7
let VGA_DACWriteIndex: UInt64 = 0x0x3C8
let VGA_DACData: UInt64 = 0x0x3C9

// MARK: - GamePort (Game Port)
let GamePort_Data: UInt64 = 0x0x201

// MARK: - ParallelPort (Parallel Printer Port)
let ParallelPort_Data: UInt64 = 0x0x378
let ParallelPort_Status: UInt64 = 0x0x379
let ParallelPort_Control: UInt64 = 0x0x37A

// MARK: - SerialPort (Serial Communications Port)
let SerialPort_Data: UInt64 = 0x0x3F8
let SerialPort_IER: UInt64 = 0x0x3F9
let SerialPort_IIR: UInt64 = 0x0x3FA
let SerialPort_LCR: UInt64 = 0x0x3FB
let SerialPort_MCR: UInt64 = 0x0x3FC
let SerialPort_LSR: UInt64 = 0x0x3FD
let SerialPort_MSR: UInt64 = 0x0x3FE
let SerialPort_SCR: UInt64 = 0x0x3FF

// MARK: - Speaker (PC Speaker)
let Speaker_Control: UInt64 = 0x0x61

// MARK: - Interrupt Vectors
let IRQ_Divide Error: Int = 0
let IRQ_Single Step: Int = 1
let IRQ_NMI: Int = 2
let IRQ_Breakpoint: Int = 3
let IRQ_Overflow: Int = 4
let IRQ_Print Screen: Int = 5
let IRQ_IRQ0: Int = 8
let IRQ_IRQ1: Int = 9
let IRQ_IRQ2: Int = 10
let IRQ_IRQ3: Int = 11
let IRQ_IRQ4: Int = 12
let IRQ_IRQ5: Int = 13
let IRQ_IRQ6: Int = 14
let IRQ_IRQ7: Int = 15
let IRQ_IRQ8: Int = 112
let IRQ_IRQ9: Int = 113
let IRQ_IRQ10: Int = 114
let IRQ_IRQ11: Int = 115
let IRQ_IRQ12: Int = 116
let IRQ_IRQ13: Int = 117
let IRQ_IRQ14: Int = 118
let IRQ_IRQ15: Int = 119
let IRQ_Video Services: Int = 16
let IRQ_Disk Services: Int = 19
let IRQ_DOS Services: Int = 21

// MARK: - Memory Segments

// MARK: - Device Functions
func ibm_pc_at_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
