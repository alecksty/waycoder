"""
Sega-Master-System设备定义 - Python模块
生成自: Sega/Master System/Sega-Master-System
版本: 1.0
日期: 2026-04-17
作者: VML Team
描述: Sega Master System 8-bit video game console with Z80 CPU
CPU架构: Zilog Z80
位宽: 8位
时钟频率: 3579545 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class Sega_Master_System:
    """Sega-Master-System设备类"""

    # 设备信息
    DEVICE_NAME = "Sega-Master-System"
    MANUFACTURER = "Sega"
    FAMILY = "Master System"
    VERSION = "1.0"
    ARCHITECTURE = "Zilog Z80"
    BITS = 8
    CLOCK_FREQUENCY = 3579545

    # 寄存器地址定义
    A_ADDR = 0  # Accumulator
    F_ADDR = 0  # Flags
    B_ADDR = 0  # B
    C_ADDR = 0  # C
    D_ADDR = 0  # D
    E_ADDR = 0  # E
    H_ADDR = 0  # H
    L_ADDR = 0  # L
    IX_ADDR = 0  # Index Register X
    IY_ADDR = 0  # Index Register Y
    SP_ADDR = 0  # Stack Pointer
    PC_ADDR = 0  # Program Counter
    I_ADDR = 0  # Interrupt Vector
    R_ADDR = 0  # Memory Refresh

    # 外设定义
    # Video Display Processor (TMS9918A)
    VDP_BASE = 
    VDP_VDP_DATA_ADDR = 0xBE
    VDP_VDP_ADDR_ADDR = 0xBF
    VDP_VDP_STATUS_ADDR = 0xBF
    # Programmable Sound Generator (SN76489)
    PSG_BASE = 
    PSG_PSG_DATA_ADDR = 0x7F
    # I/O ports
    IO_BASE = 
    IO_IO_PORT_A_ADDR = 0xDC
    IO_IO_PORT_B_ADDR = 0xDD
    IO_IO_PORT_MISC_ADDR = 0xDE
    IO_IO_PORT_VDP_ADDR = 0xDF
    # Memory mapper
    MEMORYMAPPER_BASE = 
    MEMORYMAPPER_MAPPER_0_ADDR = 0xFFFC
    MEMORYMAPPER_MAPPER_1_ADDR = 0xFFFD
    MEMORYMAPPER_MAPPER_2_ADDR = 0xFFFE
    MEMORYMAPPER_MAPPER_3_ADDR = 0xFFFF
    # FM Sound Unit (optional)
    FMUNIT_BASE = 
    FMUNIT_FM_ADDR_ADDR = 0xF0
    FMUNIT_FM_DATA_ADDR = 0xF1
    FMUNIT_FM_DETECT_ADDR = 0xF2

    # 中断向量定义
    INT_RST_00 = 0  # Restart 00h
    INT_IM1 = 56  # Interrupt Mode 1
    INT_VBLANK = 56  # Vertical blank interrupt
    INT_LINE = 100  # Line interrupt

    def __init__(self, memory_base: int = 0):
        """初始化设备"""
        self.memory_base = memory_base
        self._registers = {}
        self._peripherals = {}
        self._initialize_registers()
        self._initialize_peripherals()

    def _initialize_registers(self):
        """初始化寄存器""""
        self._registers["A"] = {
            "address": 0,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Accumulator",
            "value": 0
        }
        self._registers["F"] = {
            "address": 0,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Flags",
            "value": 0
        }
        self._registers["B"] = {
            "address": 0,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "B",
            "value": 0
        }
        self._registers["C"] = {
            "address": 0,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "C",
            "value": 0
        }
        self._registers["D"] = {
            "address": 0,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "D",
            "value": 0
        }
        self._registers["E"] = {
            "address": 0,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "E",
            "value": 0
        }
        self._registers["H"] = {
            "address": 0,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "H",
            "value": 0
        }
        self._registers["L"] = {
            "address": 0,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "L",
            "value": 0
        }
        self._registers["IX"] = {
            "address": 0,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Index Register X",
            "value": 0
        }
        self._registers["IY"] = {
            "address": 0,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Index Register Y",
            "value": 0
        }
        self._registers["SP"] = {
            "address": 0,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Stack Pointer",
            "value": 0
        }
        self._registers["PC"] = {
            "address": 0,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Program Counter",
            "value": 0
        }
        self._registers["I"] = {
            "address": 0,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Interrupt Vector",
            "value": 0
        }
        self._registers["R"] = {
            "address": 0,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Memory Refresh",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["VDP"] = {
            "base": ,
            "type": "Video",
            "description": "Video Display Processor (TMS9918A)",
            "registers": {
                "VDP_DATA": {
                    "address": 0xBE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VDP_ADDR": {
                    "address": 0xBF,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VDP_STATUS": {
                    "address": 0xBF,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PSG"] = {
            "base": ,
            "type": "Audio",
            "description": "Programmable Sound Generator (SN76489)",
            "registers": {
                "PSG_DATA": {
                    "address": 0x7F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["IO"] = {
            "base": ,
            "type": "IO",
            "description": "I/O ports",
            "registers": {
                "IO_PORT_A": {
                    "address": 0xDC,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IO_PORT_B": {
                    "address": 0xDD,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IO_PORT_MISC": {
                    "address": 0xDE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IO_PORT_VDP": {
                    "address": 0xDF,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["MemoryMapper"] = {
            "base": ,
            "type": "Memory",
            "description": "Memory mapper",
            "registers": {
                "MAPPER_0": {
                    "address": 0xFFFC,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "MAPPER_1": {
                    "address": 0xFFFD,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "MAPPER_2": {
                    "address": 0xFFFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "MAPPER_3": {
                    "address": 0xFFFF,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["FMUnit"] = {
            "base": ,
            "type": "Audio",
            "description": "FM Sound Unit (optional)",
            "registers": {
                "FM_ADDR": {
                    "address": 0xF0,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FM_DATA": {
                    "address": 0xF1,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FM_DETECT": {
                    "address": 0xF2,
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
        return f"Sega_Master_System({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = Sega_Master_System()
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
