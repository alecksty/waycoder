// IBM PC/AT 设备定义 - Objective-C 头文件
// 生成自: IBM/IBM PC/IBM PC/AT
// 版本: 
// 日期: 
// 作者: 
// 描述: IBM Personal Computer/Advanced Technology (Model 5170)
// CPU架构: x86-16
// 位宽: 0位
// 时钟频率: 0 Hz

#ifndef IBM PC/AT_DEVICE_H
#define IBM PC/AT_DEVICE_H

#import <Foundation/Foundation.h>

// 外设定义
// Programmable Interrupt Controller
#define _8259A_BASE 
#define _8259A_ICW1_ADDR 0x20
#define _8259A_ICW2_ADDR 0x21
#define _8259A_ICW3_ADDR 0x21
#define _8259A_ICW4_ADDR 0x21
#define _8259A_OCW1_ADDR 0x21
#define _8259A_OCW2_ADDR 0x20
#define _8259A_OCW3_ADDR 0x20
// Programmable Interval Timer
#define _8253_BASE 
#define _8253_COUNTER0_ADDR 0x40
#define _8253_COUNTER1_ADDR 0x41
#define _8253_COUNTER2_ADDR 0x42
#define _8253_CONTROL_ADDR 0x43
// Direct Memory Access Controller
#define _8237_BASE 
#define _8237_CHANNEL0_ADDR 0x00
#define _8237_CHANNEL1_ADDR 0x02
#define _8237_CHANNEL2_ADDR 0x04
#define _8237_CHANNEL3_ADDR 0x06
#define _8237_STATUS_ADDR 0x08
#define _8237_COMMAND_ADDR 0x08
#define _8237_REQUEST_ADDR 0x09
#define _8237_MASK_ADDR 0x0A
#define _8237_MODE_ADDR 0x0B
// Keyboard Controller
#define _8042_BASE 
#define _8042_DATA_ADDR 0x60
#define _8042_STATUS_ADDR 0x64
// Real-Time Clock with CMOS RAM
#define CMOS_BASE 
#define CMOS_ADDRESS_ADDR 0x70
#define CMOS_DATA_ADDR 0x71
// Floppy Disk Controller
#define FDC_BASE 
#define FDC_SRA_ADDR 0x3F2
#define FDC_MSR_ADDR 0x3F4
#define FDC_DATA_ADDR 0x3F5
#define FDC_DIR_ADDR 0x3F7
#define FDC_CCR_ADDR 0x3F7
// Hard Disk Controller (ST-506/412)
#define HDC_BASE 
#define HDC_DATA_ADDR 0x1F0
#define HDC_ERROR_ADDR 0x1F1
#define HDC_SECTORCOUNT_ADDR 0x1F2
#define HDC_SECTORNUMBER_ADDR 0x1F3
#define HDC_CYLINDERLOW_ADDR 0x1F4
#define HDC_CYLINDERHIGH_ADDR 0x1F5
#define HDC_DRIVEHEAD_ADDR 0x1F6
#define HDC_STATUS_ADDR 0x1F7
#define HDC_COMMAND_ADDR 0x1F7
// Color Graphics Adapter
#define CGA_BASE 
#define CGA_CRTC_INDEX_ADDR 0x3D4
#define CGA_CRTC_DATA_ADDR 0x3D5
#define CGA_MODECONTROL_ADDR 0x3D8
#define CGA_COLORSELECT_ADDR 0x3D9
#define CGA_STATUS_ADDR 0x3DA
// Enhanced Graphics Adapter
#define EGA_BASE 
#define EGA_CRTC_INDEX_ADDR 0x3D4
#define EGA_CRTC_DATA_ADDR 0x3D5
#define EGA_FEATURECONTROL_ADDR 0x3DA
#define EGA_GRAPHICS1POS_ADDR 0x3CC
#define EGA_GRAPHICS2POS_ADDR 0x3CA
#define EGA_SEQUENCERINDEX_ADDR 0x3C4
#define EGA_SEQUENCERDATA_ADDR 0x3C5
#define EGA_GRAPHICSINDEX_ADDR 0x3CE
#define EGA_GRAPHICSDATA_ADDR 0x3CF
#define EGA_ATTRIBUTEINDEX_ADDR 0x3C0
#define EGA_ATTRIBUTEDATA_ADDR 0x3C1
// Video Graphics Array
#define VGA_BASE 
#define VGA_CRTC_INDEX_ADDR 0x3D4
#define VGA_CRTC_DATA_ADDR 0x3D5
#define VGA_INPUTSTATUS1_ADDR 0x3DA
#define VGA_FEATURECONTROL_ADDR 0x3DA
#define VGA_MISCOUTPUT_ADDR 0x3C2
#define VGA_SEQUENCERINDEX_ADDR 0x3C4
#define VGA_SEQUENCERDATA_ADDR 0x3C5
#define VGA_GRAPHICSINDEX_ADDR 0x3CE
#define VGA_GRAPHICSDATA_ADDR 0x3CF
#define VGA_ATTRIBUTEINDEX_ADDR 0x3C0
#define VGA_ATTRIBUTEDATA_ADDR 0x3C1
#define VGA_DACMASK_ADDR 0x3C6
#define VGA_DACREADINDEX_ADDR 0x3C7
#define VGA_DACWRITEINDEX_ADDR 0x3C8
#define VGA_DACDATA_ADDR 0x3C9
// Game Port
#define GAMEPORT_BASE 
#define GAMEPORT_DATA_ADDR 0x201
// Parallel Printer Port
#define PARALLELPORT_BASE 
#define PARALLELPORT_DATA_ADDR 0x378
#define PARALLELPORT_STATUS_ADDR 0x379
#define PARALLELPORT_CONTROL_ADDR 0x37A
// Serial Communications Port
#define SERIALPORT_BASE 
#define SERIALPORT_DATA_ADDR 0x3F8
#define SERIALPORT_IER_ADDR 0x3F9
#define SERIALPORT_IIR_ADDR 0x3FA
#define SERIALPORT_LCR_ADDR 0x3FB
#define SERIALPORT_MCR_ADDR 0x3FC
#define SERIALPORT_LSR_ADDR 0x3FD
#define SERIALPORT_MSR_ADDR 0x3FE
#define SERIALPORT_SCR_ADDR 0x3FF
// PC Speaker
#define SPEAKER_BASE 
#define SPEAKER_CONTROL_ADDR 0x61

// 中断向量定义
#define INT_DIVIDE_ERROR 0  // Division by zero
#define INT_SINGLE_STEP 1  // Debug single step
#define INT_NMI 2  // Non-maskable interrupt
#define INT_BREAKPOINT 3  // INT 3 instruction
#define INT_OVERFLOW 4  // INTO instruction
#define INT_PRINT_SCREEN 5  // Print screen key
#define INT_IRQ0 8  // Timer interrupt
#define INT_IRQ1 9  // Keyboard interrupt
#define INT_IRQ2 10  // Cascade to IRQ8-15
#define INT_IRQ3 11  // COM2 interrupt
#define INT_IRQ4 12  // COM1 interrupt
#define INT_IRQ5 13  // LPT2 interrupt
#define INT_IRQ6 14  // Floppy disk interrupt
#define INT_IRQ7 15  // LPT1 interrupt
#define INT_IRQ8 112  // Real-time clock interrupt
#define INT_IRQ9 113  // Redirected IRQ2
#define INT_IRQ10 114  // Reserved
#define INT_IRQ11 115  // Reserved
#define INT_IRQ12 116  // PS/2 mouse interrupt
#define INT_IRQ13 117  // Coprocessor interrupt
#define INT_IRQ14 118  // Primary IDE interrupt
#define INT_IRQ15 119  // Secondary IDE interrupt
#define INT_VIDEO_SERVICES 16  // Video BIOS services
#define INT_DISK_SERVICES 19  // Disk BIOS services
#define INT_DOS_SERVICES 21  // DOS function calls

#endif /* IBM PC/AT_DEVICE_H */
