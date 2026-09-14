//
// Intel 80286 Register Definitions
// Generated from: Intel 80286 16-bit microprocessor with memory management and protection
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

// MARK: - 8255 (Programmable Peripheral Interface)
let 8255_PortA: UInt64 = 0x0x60
let 8255_PortB: UInt64 = 0x0x61
let 8255_PortC: UInt64 = 0x0x62
let 8255_Control: UInt64 = 0x0x63

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
let 8237_FlipFlop: UInt64 = 0x0x0C
let 8237_Temp: UInt64 = 0x0x0D
let 8237_MasterClear: UInt64 = 0x0x0D
let 8237_MaskAll: UInt64 = 0x0x0F

// MARK: - 8042 (Keyboard Controller)
let 8042_Data: UInt64 = 0x0x60
let 8042_Status: UInt64 = 0x0x64

// MARK: - Interrupt Vectors
let IRQ_Divide Error: Int = 0
let IRQ_Debug Exception: Int = 1
let IRQ_NMI: Int = 2
let IRQ_Breakpoint: Int = 3
let IRQ_Overflow: Int = 4
let IRQ_Bounds Check: Int = 5
let IRQ_Invalid Opcode: Int = 6
let IRQ_Coprocessor Not Available: Int = 7
let IRQ_Double Fault: Int = 8
let IRQ_Coprocessor Segment Overrun: Int = 9
let IRQ_Invalid TSS: Int = 10
let IRQ_Segment Not Present: Int = 11
let IRQ_Stack Fault: Int = 12
let IRQ_General Protection: Int = 13
let IRQ_Page Fault: Int = 14
let IRQ_Coprocessor Error: Int = 16
let IRQ_IRQ0: Int = 32
let IRQ_IRQ1: Int = 33
let IRQ_IRQ2: Int = 34
let IRQ_IRQ3: Int = 35
let IRQ_IRQ4: Int = 36
let IRQ_IRQ5: Int = 37
let IRQ_IRQ6: Int = 38
let IRQ_IRQ7: Int = 39
let IRQ_IRQ8: Int = 40
let IRQ_IRQ9: Int = 41
let IRQ_IRQ10: Int = 42
let IRQ_IRQ11: Int = 43
let IRQ_IRQ12: Int = 44
let IRQ_IRQ13: Int = 45
let IRQ_IRQ14: Int = 46
let IRQ_IRQ15: Int = 47

// MARK: - Memory Segments

// MARK: - Device Functions
func intel_80286_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
