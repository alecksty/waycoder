"""
8086设备定义 - Python模块
生成自: Intel/x86/8086
版本: 1.0
日期: 2026-04-17
作者: VML Team
描述: 16-bit microprocessor, first x86 processor
CPU架构: x86
位宽: 16位
时钟频率: 5000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class 8086:
    """8086设备类"""

    # 设备信息
    DEVICE_NAME = "8086"
    MANUFACTURER = "Intel"
    FAMILY = "x86"
    VERSION = "1.0"
    ARCHITECTURE = "x86"
    BITS = 16
    CLOCK_FREQUENCY = 5000000

    # 寄存器地址定义
    AX_ADDR = 0  # Accumulator
    AX_AH_BIT = 8  # High byte of AX
    AX_AL_BIT = 0  # Low byte of AX
    BX_ADDR = 1  # Base
    BX_BH_BIT = 8  # High byte of BX
    BX_BL_BIT = 0  # Low byte of BX
    CX_ADDR = 2  # Counter
    CX_CH_BIT = 8  # High byte of CX
    CX_CL_BIT = 0  # Low byte of CX
    DX_ADDR = 3  # Data
    DX_DH_BIT = 8  # High byte of DX
    DX_DL_BIT = 0  # Low byte of DX
    SI_ADDR = 4  # Source Index
    DI_ADDR = 5  # Destination Index
    BP_ADDR = 6  # Base Pointer
    SP_ADDR = 7  # Stack Pointer
    IP_ADDR = 8  # Instruction Pointer
    CS_ADDR = 9  # Code Segment
    DS_ADDR = 10  # Data Segment
    ES_ADDR = 11  # Extra Segment
    SS_ADDR = 12  # Stack Segment
    FLAGS_ADDR = 13  # Flags Register
    FLAGS_CF_BIT = 0  # Carry Flag
    FLAGS_PF_BIT = 2  # Parity Flag
    FLAGS_AF_BIT = 4  # Auxiliary Flag
    FLAGS_ZF_BIT = 6  # Zero Flag
    FLAGS_SF_BIT = 7  # Sign Flag
    FLAGS_TF_BIT = 8  # Trap Flag
    FLAGS_IF_BIT = 9  # Interrupt Enable Flag
    FLAGS_DF_BIT = 10  # Direction Flag
    FLAGS_OF_BIT = 11  # Overflow Flag

    # 内存段定义
    CODE_START = 0x00000
    CODE_END = 0xFFFFF
    CODE_SIZE = 1048576  # 1MB address space
    DATA_START = 0x00000
    DATA_END = 0xFFFFF
    DATA_SIZE = 1048576  # Data memory
    STACK_START = 0xF0000
    STACK_END = 0xFFFFF
    STACK_SIZE = 65536  # Stack memory
    BIOS_START = 0xF0000
    BIOS_END = 0xFFFFF
    BIOS_SIZE = 65536  # BIOS ROM

    # 外设定义
    # Programmable Interrupt Controller
    PIC_BASE = 0x0020
    PIC_PIC1_CMD_ADDR = 0x0020
    PIC_PIC1_DATA_ADDR = 0x0021
    PIC_PIC2_CMD_ADDR = 0x00A0
    PIC_PIC2_DATA_ADDR = 0x00A1
    # Programmable Interval Timer
    PIT_BASE = 0x0040
    PIT_PIT_CH0_ADDR = 0x0040
    PIT_PIT_CH1_ADDR = 0x0041
    PIT_PIT_CH2_ADDR = 0x0042
    PIT_PIT_CMD_ADDR = 0x0043
    # Programmable Peripheral Interface
    PPI_BASE = 0x0060
    PPI_PPI_PA_ADDR = 0x0060
    PPI_PPI_PB_ADDR = 0x0061
    PPI_PPI_PC_ADDR = 0x0062
    PPI_PPI_CMD_ADDR = 0x0063

    # 中断向量定义
    INT_DIVIDE_ERROR = 0  # Divide by zero
    INT_DEBUG = 1  # Single step
    INT_NMI = 2  # Non-maskable interrupt
    INT_BREAKPOINT = 3  # Breakpoint
    INT_OVERFLOW = 4  # INTO detected overflow
    INT_IRQ0 = 8  # Timer interrupt
    INT_IRQ1 = 9  # Keyboard interrupt
    INT_IRQ2 = 10  # Cascade
    INT_IRQ3 = 11  # COM2
    INT_IRQ4 = 12  # COM1
    INT_IRQ5 = 13  # LPT2
    INT_IRQ6 = 14  # Floppy disk
    INT_IRQ7 = 15  # LPT1

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
            "address": 0,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Accumulator",
            "value": 0
        }
        self._registers["BX"] = {
            "address": 1,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Base",
            "value": 0
        }
        self._registers["CX"] = {
            "address": 2,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Counter",
            "value": 0
        }
        self._registers["DX"] = {
            "address": 3,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Data",
            "value": 0
        }
        self._registers["SI"] = {
            "address": 4,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Source Index",
            "value": 0
        }
        self._registers["DI"] = {
            "address": 5,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Destination Index",
            "value": 0
        }
        self._registers["BP"] = {
            "address": 6,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Base Pointer",
            "value": 0
        }
        self._registers["SP"] = {
            "address": 7,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Stack Pointer",
            "value": 0
        }
        self._registers["IP"] = {
            "address": 8,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Instruction Pointer",
            "value": 0
        }
        self._registers["CS"] = {
            "address": 9,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Code Segment",
            "value": 0
        }
        self._registers["DS"] = {
            "address": 10,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Data Segment",
            "value": 0
        }
        self._registers["ES"] = {
            "address": 11,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Extra Segment",
            "value": 0
        }
        self._registers["SS"] = {
            "address": 12,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Stack Segment",
            "value": 0
        }
        self._registers["FLAGS"] = {
            "address": 13,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Flags Register",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["PIC"] = {
            "base": 0x0020,
            "type": "InterruptController",
            "description": "Programmable Interrupt Controller",
            "registers": {
                "PIC1_CMD": {
                    "address": 0x0020,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PIC1_DATA": {
                    "address": 0x0021,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PIC2_CMD": {
                    "address": 0x00A0,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PIC2_DATA": {
                    "address": 0x00A1,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PIT"] = {
            "base": 0x0040,
            "type": "Timer",
            "description": "Programmable Interval Timer",
            "registers": {
                "PIT_CH0": {
                    "address": 0x0040,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PIT_CH1": {
                    "address": 0x0041,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PIT_CH2": {
                    "address": 0x0042,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PIT_CMD": {
                    "address": 0x0043,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PPI"] = {
            "base": 0x0060,
            "type": "IO",
            "description": "Programmable Peripheral Interface",
            "registers": {
                "PPI_PA": {
                    "address": 0x0060,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PPI_PB": {
                    "address": 0x0061,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PPI_PC": {
                    "address": 0x0062,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PPI_CMD": {
                    "address": 0x0063,
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
        return f"8086({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = 8086()
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
