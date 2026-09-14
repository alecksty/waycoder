"""
ZX-Spectrum设备定义 - Python模块
生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum
版本: 1.0
日期: 2026-04-17
作者: VML Team
描述: ZX Spectrum 48K home computer with Z80 CPU, 48KB RAM, and color graphics
CPU架构: Zilog Z80
位宽: 8位
时钟频率: 3500000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class ZX_Spectrum:
    """ZX-Spectrum设备类"""

    # 设备信息
    DEVICE_NAME = "ZX-Spectrum"
    MANUFACTURER = "Sinclair Research"
    FAMILY = "ZX Spectrum"
    VERSION = "1.0"
    ARCHITECTURE = "Zilog Z80"
    BITS = 8
    CLOCK_FREQUENCY = 3500000

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
    AF_ADDR = 0  # Alternate AF
    BC_ADDR = 0  # Alternate BC
    DE_ADDR = 0  # Alternate DE
    HL_ADDR = 0  # Alternate HL

    # 外设定义
    # Uncommitted Logic Array (video and I/O)
    ULA_BASE = 
    ULA_ULA_PORT_FE_ADDR = 0xFE
    ULA_ULA_BORDER_ADDR = 0xFE
    ULA_ULA_BEEPER_ADDR = 0xFE
    ULA_ULA_MIC_ADDR = 0xFE
    # General Instruments AY-3-8912 sound chip
    AY_3_8912_BASE = 
    AY_3_8912_AY_REG_SEL_ADDR = 0xFFFD
    AY_3_8912_AY_DATA_ADDR = 0xBFFD
    AY_3_8912_AY_READ_ADDR = 0xFFFD
    # 40-key rubber keyboard
    KEYBOARD_BASE = 
    KEYBOARD_KEY_ROW0_ADDR = 0xFEFE
    KEYBOARD_KEY_ROW1_ADDR = 0xFDFE
    KEYBOARD_KEY_ROW2_ADDR = 0xFBFE
    KEYBOARD_KEY_ROW3_ADDR = 0xF7FE
    KEYBOARD_KEY_ROW4_ADDR = 0xEFFE
    KEYBOARD_KEY_ROW5_ADDR = 0xDFFE
    KEYBOARD_KEY_ROW6_ADDR = 0xBFFE
    KEYBOARD_KEY_ROW7_ADDR = 0x7FFE
    # Kempston joystick interface
    KEMPSTON_BASE = 
    KEMPSTON_KEMPSTON_JOY_ADDR = 0x1F
    # ZX Interface 1 (RS-232 and Microdrive)
    INTERFACE1_BASE = 
    INTERFACE1_IF1_STATUS_ADDR = 0x1FFD
    INTERFACE1_IF1_DATA_ADDR = 0x3FFD
    # ZX Interface 2 (joystick and ROM cartridge)
    INTERFACE2_BASE = 
    INTERFACE2_IF2_JOY1_ADDR = 0x1F
    INTERFACE2_IF2_JOY2_ADDR = 0x37

    # 中断向量定义
    INT_IM1 = 56  # Interrupt Mode 1
    INT_RST_00 = 0  # Restart 00h
    INT_RST_08 = 8  # Restart 08h
    INT_RST_10 = 16  # Restart 10h
    INT_RST_18 = 24  # Restart 18h
    INT_RST_20 = 32  # Restart 20h
    INT_RST_28 = 40  # Restart 28h
    INT_RST_30 = 48  # Restart 30h
    INT_RST_38 = 56  # Restart 38h

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
        self._registers["AF'"] = {
            "address": 0,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Alternate AF",
            "value": 0
        }
        self._registers["BC'"] = {
            "address": 0,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Alternate BC",
            "value": 0
        }
        self._registers["DE'"] = {
            "address": 0,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Alternate DE",
            "value": 0
        }
        self._registers["HL'"] = {
            "address": 0,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Alternate HL",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["ULA"] = {
            "base": ,
            "type": "Video",
            "description": "Uncommitted Logic Array (video and I/O)",
            "registers": {
                "ULA_PORT_FE": {
                    "address": 0xFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ULA_BORDER": {
                    "address": 0xFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ULA_BEEPER": {
                    "address": 0xFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ULA_MIC": {
                    "address": 0xFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["AY-3-8912"] = {
            "base": ,
            "type": "Audio",
            "description": "General Instruments AY-3-8912 sound chip",
            "registers": {
                "AY_REG_SEL": {
                    "address": 0xFFFD,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "AY_DATA": {
                    "address": 0xBFFD,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "AY_READ": {
                    "address": 0xFFFD,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["Keyboard"] = {
            "base": ,
            "type": "Input",
            "description": "40-key rubber keyboard",
            "registers": {
                "KEY_ROW0": {
                    "address": 0xFEFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "KEY_ROW1": {
                    "address": 0xFDFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "KEY_ROW2": {
                    "address": 0xFBFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "KEY_ROW3": {
                    "address": 0xF7FE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "KEY_ROW4": {
                    "address": 0xEFFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "KEY_ROW5": {
                    "address": 0xDFFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "KEY_ROW6": {
                    "address": 0xBFFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "KEY_ROW7": {
                    "address": 0x7FFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["Kempston"] = {
            "base": ,
            "type": "Input",
            "description": "Kempston joystick interface",
            "registers": {
                "KEMPSTON_JOY": {
                    "address": 0x1F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["Interface1"] = {
            "base": ,
            "type": "Storage",
            "description": "ZX Interface 1 (RS-232 and Microdrive)",
            "registers": {
                "IF1_STATUS": {
                    "address": 0x1FFD,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IF1_DATA": {
                    "address": 0x3FFD,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["Interface2"] = {
            "base": ,
            "type": "Storage",
            "description": "ZX Interface 2 (joystick and ROM cartridge)",
            "registers": {
                "IF2_JOY1": {
                    "address": 0x1F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IF2_JOY2": {
                    "address": 0x37,
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
        return f"ZX_Spectrum({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = ZX_Spectrum()
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
