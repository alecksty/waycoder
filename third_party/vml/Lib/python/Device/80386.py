"""
Intel 80386设备定义 - Python模块
生成自: Intel/x86/Intel 80386
版本: 
日期: 
作者: 
描述: Intel 80386 32-bit microprocessor with virtual 8086 mode and paging
CPU架构: x86-32
位宽: 0位
时钟频率: 0 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class Intel 80386:
    """Intel 80386设备类"""

    # 设备信息
    DEVICE_NAME = "Intel 80386"
    MANUFACTURER = "Intel"
    FAMILY = "x86"
    VERSION = ""
    ARCHITECTURE = "x86-32"
    BITS = 0
    CLOCK_FREQUENCY = 0

    # 外设定义
    # Programmable Interrupt Controller
    _8259A_BASE = 
    _8259A_ICW1_ADDR = 0x20
    _8259A_ICW2_ADDR = 0x21
    _8259A_ICW3_ADDR = 0x21
    _8259A_ICW4_ADDR = 0x21
    _8259A_OCW1_ADDR = 0x21
    _8259A_OCW2_ADDR = 0x20
    _8259A_OCW3_ADDR = 0x20
    # Programmable Interval Timer
    _8253_BASE = 
    _8253_COUNTER0_ADDR = 0x40
    _8253_COUNTER1_ADDR = 0x41
    _8253_COUNTER2_ADDR = 0x42
    _8253_CONTROL_ADDR = 0x43
    # Direct Memory Access Controller
    _8237_BASE = 
    _8237_CHANNEL0_ADDR = 0x00
    _8237_CHANNEL1_ADDR = 0x02
    _8237_CHANNEL2_ADDR = 0x04
    _8237_CHANNEL3_ADDR = 0x06
    _8237_STATUS_ADDR = 0x08
    _8237_COMMAND_ADDR = 0x08
    _8237_REQUEST_ADDR = 0x09
    _8237_MASK_ADDR = 0x0A
    _8237_MODE_ADDR = 0x0B
    _8237_FLIPFLOP_ADDR = 0x0C
    _8237_TEMP_ADDR = 0x0D
    _8237_MASTERCLEAR_ADDR = 0x0D
    _8237_MASKALL_ADDR = 0x0F
    # Keyboard Controller
    _8042_BASE = 
    _8042_DATA_ADDR = 0x60
    _8042_STATUS_ADDR = 0x64
    # Integrated System Peripheral
    _82380_BASE = 
    _82380_DMA_ADDR = 0x0000
    _82380_INTERRUPT_ADDR = 0x0200
    _82380_TIMER_ADDR = 0x0400
    _82380_DRAM_ADDR = 0x0600
    _82380_WAITSTATE_ADDR = 0x0800

    # 中断向量定义
    INT_DIVIDE_ERROR = 0  # Division by zero or overflow
    INT_DEBUG_EXCEPTION = 1  # Single-step or debug register access
    INT_NMI = 2  # Non-maskable interrupt
    INT_BREAKPOINT = 3  # INT 3 instruction
    INT_OVERFLOW = 4  # INTO instruction with OF=1
    INT_BOUNDS_CHECK = 5  # BOUND instruction
    INT_INVALID_OPCODE = 6  # Undefined opcode
    INT_COPROCESSOR_NOT_AVAILABLE = 7  # No math coprocessor
    INT_DOUBLE_FAULT = 8  # Two exceptions in handler
    INT_COPROCESSOR_SEGMENT_OVERRUN = 9  # Coprocessor operand beyond segment
    INT_INVALID_TSS = 10  # Invalid Task State Segment
    INT_SEGMENT_NOT_PRESENT = 11  # Segment not present
    INT_STACK_FAULT = 12  # Stack segment limit violation
    INT_GENERAL_PROTECTION = 13  # Memory access violation
    INT_PAGE_FAULT = 14  # Page not present
    INT_COPROCESSOR_ERROR = 16  # Math coprocessor error
    INT_ALIGNMENT_CHECK = 17  # Unaligned memory access
    INT_IRQ0 = 32  # Timer interrupt
    INT_IRQ1 = 33  # Keyboard interrupt
    INT_IRQ2 = 34  # Cascade to IRQ8-15
    INT_IRQ3 = 35  # COM2 interrupt
    INT_IRQ4 = 36  # COM1 interrupt
    INT_IRQ5 = 37  # LPT2 interrupt
    INT_IRQ6 = 38  # Floppy disk interrupt
    INT_IRQ7 = 39  # LPT1 interrupt
    INT_IRQ8 = 40  # Real-time clock interrupt
    INT_IRQ9 = 41  # Redirected IRQ2
    INT_IRQ10 = 42  # Reserved
    INT_IRQ11 = 43  # Reserved
    INT_IRQ12 = 44  # PS/2 mouse interrupt
    INT_IRQ13 = 45  # Coprocessor interrupt
    INT_IRQ14 = 46  # Primary IDE interrupt
    INT_IRQ15 = 47  # Secondary IDE interrupt

    def __init__(self, memory_base: int = 0):
        """初始化设备"""
        self.memory_base = memory_base
        self._registers = {}
        self._peripherals = {}
        self._initialize_registers()
        self._initialize_peripherals()

    def _initialize_registers(self):
        """初始化寄存器""""

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["8259A"] = {
            "base": ,
            "type": "InterruptController",
            "description": "Programmable Interrupt Controller",
            "registers": {
                "ICW1": {
                    "address": 0x20,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "ICW2": {
                    "address": 0x21,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "ICW3": {
                    "address": 0x21,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "ICW4": {
                    "address": 0x21,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "OCW1": {
                    "address": 0x21,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "OCW2": {
                    "address": 0x20,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "OCW3": {
                    "address": 0x20,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
            }
        }
        self._peripherals["8253"] = {
            "base": ,
            "type": "Timer",
            "description": "Programmable Interval Timer",
            "registers": {
                "Counter0": {
                    "address": 0x40,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "Counter1": {
                    "address": 0x41,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "Counter2": {
                    "address": 0x42,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "Control": {
                    "address": 0x43,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
            }
        }
        self._peripherals["8237"] = {
            "base": ,
            "type": "DMA",
            "description": "Direct Memory Access Controller",
            "registers": {
                "Channel0": {
                    "address": 0x00,
                    "size": 16,
                    "type": "bytes[16]",
                    "value": 0
                },
                "Channel1": {
                    "address": 0x02,
                    "size": 16,
                    "type": "bytes[16]",
                    "value": 0
                },
                "Channel2": {
                    "address": 0x04,
                    "size": 16,
                    "type": "bytes[16]",
                    "value": 0
                },
                "Channel3": {
                    "address": 0x06,
                    "size": 16,
                    "type": "bytes[16]",
                    "value": 0
                },
                "Status": {
                    "address": 0x08,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "Command": {
                    "address": 0x08,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "Request": {
                    "address": 0x09,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "Mask": {
                    "address": 0x0A,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "Mode": {
                    "address": 0x0B,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "FlipFlop": {
                    "address": 0x0C,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "Temp": {
                    "address": 0x0D,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "MasterClear": {
                    "address": 0x0D,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "MaskAll": {
                    "address": 0x0F,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
            }
        }
        self._peripherals["8042"] = {
            "base": ,
            "type": "KeyboardController",
            "description": "Keyboard Controller",
            "registers": {
                "Data": {
                    "address": 0x60,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "Status": {
                    "address": 0x64,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
            }
        }
        self._peripherals["82380"] = {
            "base": ,
            "type": "SystemController",
            "description": "Integrated System Peripheral",
            "registers": {
                "DMA": {
                    "address": 0x0000,
                    "size": 256,
                    "type": "bytes[256]",
                    "value": 0
                },
                "Interrupt": {
                    "address": 0x0200,
                    "size": 256,
                    "type": "bytes[256]",
                    "value": 0
                },
                "Timer": {
                    "address": 0x0400,
                    "size": 256,
                    "type": "bytes[256]",
                    "value": 0
                },
                "DRAM": {
                    "address": 0x0600,
                    "size": 256,
                    "type": "bytes[256]",
                    "value": 0
                },
                "WaitState": {
                    "address": 0x0800,
                    "size": 256,
                    "type": "bytes[256]",
                    "value": 0
                },
            }
        }

    def read_register(self, name: str) -> int:
        """读取寄存器值"""
        if name in self._registers:
            return self._registers[name]["value"]
        raise KeyError(f"寄存器 {name} 不存在")

    def write_register(self, name: str, value: int):
        """写入寄存器值"""
        if name in self._registers:
            reg = self._registers[name]
            max_value = (1 << (reg["size"] * 8)) - 1
            if value < 0 or value > max_value:
                raise ValueError(f"值 {value} 超出范围 [0, {max_value}]")
            reg["value"] = value
        else:
            raise KeyError(f"寄存器 {name} 不存在")

    def set_bit(self, register_name: str, bit: int, value: bool):
        """设置寄存器位"""
        if register_name in self._registers:
            reg = self._registers[register_name]
            if value:
                reg["value"] |= (1 << bit)
            else:
                reg["value"] &= ~(1 << bit)
        else:
            raise KeyError(f"寄存器 {register_name} 不存在")

    def get_bit(self, register_name: str, bit: int) -> bool:
        """获取寄存器位"""
        if register_name in self._registers:
            reg = self._registers[register_name]
            return (reg["value"] >> bit) & 1 == 1
        raise KeyError(f"寄存器 {register_name} 不存在")

    def get_device_info(self) -> dict:
        """获取设备信息"""
        return {
            "name": self.DEVICE_NAME,
            "manufacturer": self.MANUFACTURER,
            "family": self.FAMILY,
            "version": self.VERSION,
            "architecture": self.ARCHITECTURE,
            "bits": self.BITS,
            "clock_frequency": self.CLOCK_FREQUENCY
        }

    def get_register_info(self, name: str) -> Optional[dict]:
        """获取寄存器信息"""
        return self._registers.get(name)

    def get_peripheral_info(self, name: str) -> Optional[dict]:
        """获取外设信息"""
        return self._peripherals.get(name)

    def reset(self):
        """重置设备"""
        for reg in self._registers.values():
            reg["value"] = 0
        for peripheral in self._peripherals.values():
            for reg in peripheral["registers"].values():
                reg["value"] = 0

    def __str__(self) -> str:
        """字符串表示"""
        info = self.get_device_info()
        return f"Intel 80386({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = Intel 80386()
    print(f"设备: {device}")
    print(f"设备信息: {device.get_device_info()}")
    print()
    
    # 演示寄存器操作
    if device.Cpu.Registers.RegisterList.Count > 0:
        first_reg = device.Cpu.Registers.RegisterList[0].Name
        print(f"第一个寄存器: {first_reg}")
        device.write_register(first_reg, 0x55)
        value = device.read_register(first_reg)
        print(f"读取值: 0x{value:X}")
