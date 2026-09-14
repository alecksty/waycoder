"""
IBM-PC-5150设备定义 - Python模块
生成自: IBM/Personal Computer/IBM-PC-5150
版本: 1.0
日期: 2026-04-16
作者: VML Team
描述: Original IBM Personal Computer Model 5150
CPU架构: x86
位宽: 16位
时钟频率: 4772727 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class IBM_PC_5150:
    """IBM-PC-5150设备类"""

    # 设备信息
    DEVICE_NAME = "IBM-PC-5150"
    MANUFACTURER = "IBM"
    FAMILY = "Personal Computer"
    VERSION = "1.0"
    ARCHITECTURE = "x86"
    BITS = 16
    CLOCK_FREQUENCY = 4772727

    # 寄存器地址定义
    AX_ADDR = 0x0  # Accumulator Register
    BX_ADDR = 0x1  # Base Register
    CX_ADDR = 0x2  # Count Register
    DX_ADDR = 0x3  # Data Register
    SI_ADDR = 0x4  # Source Index
    DI_ADDR = 0x5  # Destination Index
    BP_ADDR = 0x6  # Base Pointer
    SP_ADDR = 0x7  # Stack Pointer
    CS_ADDR = 0x8  # Code Segment
    DS_ADDR = 0x9  # Data Segment
    ES_ADDR = 0xA  # Extra Segment
    SS_ADDR = 0xB  # Stack Segment
    IP_ADDR = 0xC  # Instruction Pointer
    FLAGS_ADDR = 0xD  # Flags Register
    FLAGS_CF_BIT = 0  # Carry Flag
    FLAGS_PF_BIT = 2  # Parity Flag
    FLAGS_AF_BIT = 4  # Auxiliary Carry Flag
    FLAGS_ZF_BIT = 6  # Zero Flag
    FLAGS_SF_BIT = 7  # Sign Flag
    FLAGS_TF_BIT = 8  # Trap Flag
    FLAGS_IF_BIT = 9  # Interrupt Enable Flag
    FLAGS_DF_BIT = 10  # Direction Flag
    FLAGS_OF_BIT = 11  # Overflow Flag

    # 内存段定义
    BIOS_START = 0xF0000
    BIOS_END = 0xFFFFF
    BIOS_SIZE = 65536  # BIOS ROM
    VIDEO_START = 0xB8000
    VIDEO_END = 0xBFFFF
    VIDEO_SIZE = 32768  # Video Memory
    CONVENTIONAL_START = 0x00000
    CONVENTIONAL_END = 0x9FFFF
    CONVENTIONAL_SIZE = 640  # Conventional Memory (640KB)
    EXTENDED_START = 0x100000
    EXTENDED_END = 0x10FFFF
    EXTENDED_SIZE = 64  # Extended Memory (64KB)

    # 外设定义
    # Programmable Interrupt Controller
    PIC_BASE = 0x20
    PIC_PIC1_CMD_ADDR = 0x20
    PIC_PIC1_DATA_ADDR = 0x21
    PIC_PIC2_CMD_ADDR = 0xA0
    PIC_PIC2_DATA_ADDR = 0xA1
    # Programmable Interval Timer
    PIT_BASE = 0x40
    PIT_PIT_CH0_ADDR = 0x40
    PIT_PIT_CH1_ADDR = 0x41
    PIT_PIT_CH2_ADDR = 0x42
    PIT_PIT_CTRL_ADDR = 0x43
    # Programmable Peripheral Interface
    PPI_BASE = 0x60
    PPI_PPI_PA_ADDR = 0x60
    PPI_PPI_PB_ADDR = 0x61
    PPI_PPI_PC_ADDR = 0x62
    PPI_PPI_CTRL_ADDR = 0x63
    # Direct Memory Access Controller
    DMA_BASE = 0x00
    DMA_DMA_CH0_ADDR_ADDR = 0x00
    DMA_DMA_CH0_COUNT_ADDR = 0x01
    DMA_DMA_CMD_ADDR = 0x08
    DMA_DMA_MASK_ADDR = 0x0A
    DMA_DMA_MODE_ADDR = 0x0B
    # Color Graphics Adapter
    CGA_BASE = 0x3D4
    CGA_CGA_INDEX_ADDR = 0x3D4
    CGA_CGA_DATA_ADDR = 0x3D5
    CGA_CGA_MODE_ADDR = 0x3D8
    CGA_CGA_COLOR_ADDR = 0x3D9

    # 中断向量定义
    INT_DIVIDE_ERROR = 0  # Divide Error
    INT_SINGLE_STEP = 1  # Single Step
    INT_NMI = 2  # Non-Maskable Interrupt
    INT_BREAKPOINT = 3  # Breakpoint
    INT_OVERFLOW = 4  # Overflow
    INT_PRINT_SCREEN = 5  # Print Screen
    INT_IRQ0 = 8  # Timer Interrupt
    INT_IRQ1 = 9  # Keyboard Interrupt
    INT_IRQ2 = 10  # Cascade (8259A)
    INT_IRQ3 = 11  # COM2
    INT_IRQ4 = 12  # COM1
    INT_IRQ5 = 13  # LPT2
    INT_IRQ6 = 14  # Floppy Disk
    INT_IRQ7 = 15  # LPT1
    INT_IRQ8 = 16  # Real Time Clock
    INT_IRQ11 = 19  # Reserved
    INT_IRQ13 = 21  # Coprocessor
    INT_IRQ15 = 31  # Reserved

    # 引脚定义
    PIN_VCC = 1  # +5V Power Supply
    PIN_GND = 2  # Ground
    PIN_RESET = 3  # System Reset
    PIN_CLK = 4  # System Clock (4.77MHz)
    PIN_READY = 5  # CPU Ready Signal
    PIN_NMI = 6  # Non-Maskable Interrupt
    PIN_INTR = 7  # Interrupt Request
    PIN_HLDA = 8  # Hold Acknowledge
    PIN_HOLD = 9  # Hold Request
    PIN_MEMR = 10  # Memory Read
    PIN_MEMW = 11  # Memory Write
    PIN_IOR = 12  # I/O Read
    PIN_IOW = 13  # I/O Write
    PIN_ALE = 14  # Address Latch Enable
    PIN_DTR = 15  # Data Terminal Ready (Serial)
    PIN_RTS = 16  # Request To Send (Serial)
    PIN_CTS = 17  # Clear To Send (Serial)
    PIN_DSR = 18  # Data Set Ready (Serial)
    PIN_RI = 19  # Ring Indicator (Serial)
    PIN_DCD = 20  # Data Carrier Detect (Serial)

    def __init__(self, memory_base: int = 0):
        """初始化设备"""
        self.memory_base = memory_base
        self._registers = {}
        self._peripherals = {}
        self._initialize_registers()
        self._initialize_peripherals()

    def _initialize_registers(self):
        """初始化寄存器""""
        self._registers["AX"] = {
            "address": 0x0,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Accumulator Register",
            "value": 0
        }
        self._registers["BX"] = {
            "address": 0x1,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Base Register",
            "value": 0
        }
        self._registers["CX"] = {
            "address": 0x2,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Count Register",
            "value": 0
        }
        self._registers["DX"] = {
            "address": 0x3,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Data Register",
            "value": 0
        }
        self._registers["SI"] = {
            "address": 0x4,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Source Index",
            "value": 0
        }
        self._registers["DI"] = {
            "address": 0x5,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Destination Index",
            "value": 0
        }
        self._registers["BP"] = {
            "address": 0x6,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Base Pointer",
            "value": 0
        }
        self._registers["SP"] = {
            "address": 0x7,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Stack Pointer",
            "value": 0
        }
        self._registers["CS"] = {
            "address": 0x8,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Code Segment",
            "value": 0
        }
        self._registers["DS"] = {
            "address": 0x9,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Data Segment",
            "value": 0
        }
        self._registers["ES"] = {
            "address": 0xA,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Extra Segment",
            "value": 0
        }
        self._registers["SS"] = {
            "address": 0xB,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Stack Segment",
            "value": 0
        }
        self._registers["IP"] = {
            "address": 0xC,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Instruction Pointer",
            "value": 0
        }
        self._registers["FLAGS"] = {
            "address": 0xD,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Flags Register",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["PIC"] = {
            "base": 0x20,
            "type": "InterruptController",
            "description": "Programmable Interrupt Controller",
            "registers": {
                "PIC1_CMD": {
                    "address": 0x20,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PIC1_DATA": {
                    "address": 0x21,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PIC2_CMD": {
                    "address": 0xA0,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PIC2_DATA": {
                    "address": 0xA1,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PIT"] = {
            "base": 0x40,
            "type": "Timer",
            "description": "Programmable Interval Timer",
            "registers": {
                "PIT_CH0": {
                    "address": 0x40,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PIT_CH1": {
                    "address": 0x41,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PIT_CH2": {
                    "address": 0x42,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PIT_CTRL": {
                    "address": 0x43,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PPI"] = {
            "base": 0x60,
            "type": "GPIO",
            "description": "Programmable Peripheral Interface",
            "registers": {
                "PPI_PA": {
                    "address": 0x60,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PPI_PB": {
                    "address": 0x61,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PPI_PC": {
                    "address": 0x62,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PPI_CTRL": {
                    "address": 0x63,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["DMA"] = {
            "base": 0x00,
            "type": "DMA",
            "description": "Direct Memory Access Controller",
            "registers": {
                "DMA_CH0_ADDR": {
                    "address": 0x00,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "DMA_CH0_COUNT": {
                    "address": 0x01,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "DMA_CMD": {
                    "address": 0x08,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DMA_MASK": {
                    "address": 0x0A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DMA_MODE": {
                    "address": 0x0B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["CGA"] = {
            "base": 0x3D4,
            "type": "Video",
            "description": "Color Graphics Adapter",
            "registers": {
                "CGA_INDEX": {
                    "address": 0x3D4,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CGA_DATA": {
                    "address": 0x3D5,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CGA_MODE": {
                    "address": 0x3D8,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CGA_COLOR": {
                    "address": 0x3D9,
                    "size": 1,
                    "type": "uint8",
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
        return f"IBM_PC_5150({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = IBM_PC_5150()
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
