#ifndef IBM_PC_AT_HPP
#define IBM_PC_AT_HPP

// IBM PC/AT寄存器定义
// 生成自: IBM/IBM PC/IBM PC/AT
// 版本: 
// 日期: 


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: x86-16
// 位宽: 0位
// 时钟频率: 0 Hz

// 外设定义
// Programmable Interrupt Controller
#define _8259A_BASE 
#define _8259A_ICW1 (*(volatile uint64_t*)0x00000020)
#define _8259A_ICW2 (*(volatile uint64_t*)0x00000021)
#define _8259A_ICW3 (*(volatile uint64_t*)0x00000021)
#define _8259A_ICW4 (*(volatile uint64_t*)0x00000021)
#define _8259A_OCW1 (*(volatile uint64_t*)0x00000021)
#define _8259A_OCW2 (*(volatile uint64_t*)0x00000020)
#define _8259A_OCW3 (*(volatile uint64_t*)0x00000020)

// Programmable Interval Timer
#define _8253_BASE 
#define _8253_COUNTER0 (*(volatile uint64_t*)0x00000040)
#define _8253_COUNTER1 (*(volatile uint64_t*)0x00000041)
#define _8253_COUNTER2 (*(volatile uint64_t*)0x00000042)
#define _8253_CONTROL (*(volatile uint64_t*)0x00000043)

// Direct Memory Access Controller
#define _8237_BASE 
#define _8237_CHANNEL0 (*(volatile uint128_t*)0x00000000)
#define _8237_CHANNEL1 (*(volatile uint128_t*)0x00000002)
#define _8237_CHANNEL2 (*(volatile uint128_t*)0x00000004)
#define _8237_CHANNEL3 (*(volatile uint128_t*)0x00000006)
#define _8237_STATUS (*(volatile uint64_t*)0x00000008)
#define _8237_COMMAND (*(volatile uint64_t*)0x00000008)
#define _8237_REQUEST (*(volatile uint64_t*)0x00000009)
#define _8237_MASK (*(volatile uint64_t*)0x0000000A)
#define _8237_MODE (*(volatile uint64_t*)0x0000000B)

// Keyboard Controller
#define _8042_BASE 
#define _8042_DATA (*(volatile uint64_t*)0x00000060)
#define _8042_STATUS (*(volatile uint64_t*)0x00000064)

// Real-Time Clock with CMOS RAM
#define CMOS_BASE 
#define CMOS_ADDRESS (*(volatile uint64_t*)0x00000070)
#define CMOS_DATA (*(volatile uint64_t*)0x00000071)

// Floppy Disk Controller
#define FDC_BASE 
#define FDC_SRA (*(volatile uint64_t*)0x000003F2)
#define FDC_MSR (*(volatile uint64_t*)0x000003F4)
#define FDC_DATA (*(volatile uint64_t*)0x000003F5)
#define FDC_DIR (*(volatile uint64_t*)0x000003F7)
#define FDC_CCR (*(volatile uint64_t*)0x000003F7)

// Hard Disk Controller (ST-506/412)
#define HDC_BASE 
#define HDC_DATA (*(volatile uint128_t*)0x000001F0)
#define HDC_ERROR (*(volatile uint64_t*)0x000001F1)
#define HDC_SECTORCOUNT (*(volatile uint64_t*)0x000001F2)
#define HDC_SECTORNUMBER (*(volatile uint64_t*)0x000001F3)
#define HDC_CYLINDERLOW (*(volatile uint64_t*)0x000001F4)
#define HDC_CYLINDERHIGH (*(volatile uint64_t*)0x000001F5)
#define HDC_DRIVEHEAD (*(volatile uint64_t*)0x000001F6)
#define HDC_STATUS (*(volatile uint64_t*)0x000001F7)
#define HDC_COMMAND (*(volatile uint64_t*)0x000001F7)

// Color Graphics Adapter
#define CGA_BASE 
#define CGA_CRTC_INDEX (*(volatile uint64_t*)0x000003D4)
#define CGA_CRTC_DATA (*(volatile uint64_t*)0x000003D5)
#define CGA_MODECONTROL (*(volatile uint64_t*)0x000003D8)
#define CGA_COLORSELECT (*(volatile uint64_t*)0x000003D9)
#define CGA_STATUS (*(volatile uint64_t*)0x000003DA)

// Enhanced Graphics Adapter
#define EGA_BASE 
#define EGA_CRTC_INDEX (*(volatile uint64_t*)0x000003D4)
#define EGA_CRTC_DATA (*(volatile uint64_t*)0x000003D5)
#define EGA_FEATURECONTROL (*(volatile uint64_t*)0x000003DA)
#define EGA_GRAPHICS1POS (*(volatile uint64_t*)0x000003CC)
#define EGA_GRAPHICS2POS (*(volatile uint64_t*)0x000003CA)
#define EGA_SEQUENCERINDEX (*(volatile uint64_t*)0x000003C4)
#define EGA_SEQUENCERDATA (*(volatile uint64_t*)0x000003C5)
#define EGA_GRAPHICSINDEX (*(volatile uint64_t*)0x000003CE)
#define EGA_GRAPHICSDATA (*(volatile uint64_t*)0x000003CF)
#define EGA_ATTRIBUTEINDEX (*(volatile uint64_t*)0x000003C0)
#define EGA_ATTRIBUTEDATA (*(volatile uint64_t*)0x000003C1)

// Video Graphics Array
#define VGA_BASE 
#define VGA_CRTC_INDEX (*(volatile uint64_t*)0x000003D4)
#define VGA_CRTC_DATA (*(volatile uint64_t*)0x000003D5)
#define VGA_INPUTSTATUS1 (*(volatile uint64_t*)0x000003DA)
#define VGA_FEATURECONTROL (*(volatile uint64_t*)0x000003DA)
#define VGA_MISCOUTPUT (*(volatile uint64_t*)0x000003C2)
#define VGA_SEQUENCERINDEX (*(volatile uint64_t*)0x000003C4)
#define VGA_SEQUENCERDATA (*(volatile uint64_t*)0x000003C5)
#define VGA_GRAPHICSINDEX (*(volatile uint64_t*)0x000003CE)
#define VGA_GRAPHICSDATA (*(volatile uint64_t*)0x000003CF)
#define VGA_ATTRIBUTEINDEX (*(volatile uint64_t*)0x000003C0)
#define VGA_ATTRIBUTEDATA (*(volatile uint64_t*)0x000003C1)
#define VGA_DACMASK (*(volatile uint64_t*)0x000003C6)
#define VGA_DACREADINDEX (*(volatile uint64_t*)0x000003C7)
#define VGA_DACWRITEINDEX (*(volatile uint64_t*)0x000003C8)
#define VGA_DACDATA (*(volatile uint64_t*)0x000003C9)

// Game Port
#define GAMEPORT_BASE 
#define GAMEPORT_DATA (*(volatile uint64_t*)0x00000201)

// Parallel Printer Port
#define PARALLELPORT_BASE 
#define PARALLELPORT_DATA (*(volatile uint64_t*)0x00000378)
#define PARALLELPORT_STATUS (*(volatile uint64_t*)0x00000379)
#define PARALLELPORT_CONTROL (*(volatile uint64_t*)0x0000037A)

// Serial Communications Port
#define SERIALPORT_BASE 
#define SERIALPORT_DATA (*(volatile uint64_t*)0x000003F8)
#define SERIALPORT_IER (*(volatile uint64_t*)0x000003F9)
#define SERIALPORT_IIR (*(volatile uint64_t*)0x000003FA)
#define SERIALPORT_LCR (*(volatile uint64_t*)0x000003FB)
#define SERIALPORT_MCR (*(volatile uint64_t*)0x000003FC)
#define SERIALPORT_LSR (*(volatile uint64_t*)0x000003FD)
#define SERIALPORT_MSR (*(volatile uint64_t*)0x000003FE)
#define SERIALPORT_SCR (*(volatile uint64_t*)0x000003FF)

// PC Speaker
#define SPEAKER_BASE 
#define SPEAKER_CONTROL (*(volatile uint64_t*)0x00000061)

// 中断向量定义
#define DIVIDE_ERROR_VECTOR 0  // Division by zero
#define SINGLE_STEP_VECTOR 1  // Debug single step
#define NMI_VECTOR 2  // Non-maskable interrupt
#define BREAKPOINT_VECTOR 3  // INT 3 instruction
#define OVERFLOW_VECTOR 4  // INTO instruction
#define PRINT_SCREEN_VECTOR 5  // Print screen key
#define IRQ0_VECTOR 8  // Timer interrupt
#define IRQ1_VECTOR 9  // Keyboard interrupt
#define IRQ2_VECTOR 10  // Cascade to IRQ8-15
#define IRQ3_VECTOR 11  // COM2 interrupt
#define IRQ4_VECTOR 12  // COM1 interrupt
#define IRQ5_VECTOR 13  // LPT2 interrupt
#define IRQ6_VECTOR 14  // Floppy disk interrupt
#define IRQ7_VECTOR 15  // LPT1 interrupt
#define IRQ8_VECTOR 112  // Real-time clock interrupt
#define IRQ9_VECTOR 113  // Redirected IRQ2
#define IRQ10_VECTOR 114  // Reserved
#define IRQ11_VECTOR 115  // Reserved
#define IRQ12_VECTOR 116  // PS/2 mouse interrupt
#define IRQ13_VECTOR 117  // Coprocessor interrupt
#define IRQ14_VECTOR 118  // Primary IDE interrupt
#define IRQ15_VECTOR 119  // Secondary IDE interrupt
#define VIDEO_SERVICES_VECTOR 16  // Video BIOS services
#define DISK_SERVICES_VECTOR 19  // Disk BIOS services
#define DOS_SERVICES_VECTOR 21  // DOS function calls

void ibm_pc_at_init(void);

#ifdef __cplusplus
}
#endif

#endif // IBM_PC_AT_HPP
