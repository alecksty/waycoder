"""
Sega-Genesis设备定义 - Python模块
生成自: Sega/Genesis/Mega Drive/Sega-Genesis
版本: 1.0
日期: 2026-04-17
作者: VML Team
描述: Sega Genesis/Mega Drive 16-bit video game console with Motorola 68000 CPU
CPU架构: Motorola 68000
位宽: 32位
时钟频率: 7670000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class Sega_Genesis:
    """Sega-Genesis设备类"""

    # 设备信息
    DEVICE_NAME = "Sega-Genesis"
    MANUFACTURER = "Sega"
    FAMILY = "Genesis/Mega Drive"
    VERSION = "1.0"
    ARCHITECTURE = "Motorola 68000"
    BITS = 32
    CLOCK_FREQUENCY = 7670000

    # 寄存器地址定义
    D0_ADDR = 0  # Data Register 0
    D1_ADDR = 0  # Data Register 1
    D2_ADDR = 0  # Data Register 2
    D3_ADDR = 0  # Data Register 3
    D4_ADDR = 0  # Data Register 4
    D5_ADDR = 0  # Data Register 5
    D6_ADDR = 0  # Data Register 6
    D7_ADDR = 0  # Data Register 7
    A0_ADDR = 0  # Address Register 0
    A1_ADDR = 0  # Address Register 1
    A2_ADDR = 0  # Address Register 2
    A3_ADDR = 0  # Address Register 3
    A4_ADDR = 0  # Address Register 4
    A5_ADDR = 0  # Address Register 5
    A6_ADDR = 0  # Address Register 6
    A7_ADDR = 0  # Address Register 7 (SP)
    PC_ADDR = 0  # Program Counter
    SR_ADDR = 0  # Status Register

    # 外设定义
    # Video Display Processor (315-5313)
    VDP_BASE = 
    VDP_VDP_DATA_ADDR = 0xC00000
    VDP_VDP_CONTROL_ADDR = 0xC00004
    VDP_VDP_HVCOUNTER_ADDR = 0xC00008
    VDP_VDP_PSG_ADDR = 0xC00011
    # FM synthesis sound chip
    YM2612_BASE = 
    YM2612_YM2612_ADDR0_ADDR = 0xA04000
    YM2612_YM2612_DATA0_ADDR = 0xA04001
    YM2612_YM2612_ADDR1_ADDR = 0xA04002
    YM2612_YM2612_DATA1_ADDR = 0xA04003
    # I/O ports
    IOPORTS_BASE = 
    IOPORTS_IO_DATA1_ADDR = 0xA10002
    IOPORTS_IO_DATA2_ADDR = 0xA10004
    IOPORTS_IO_DATA3_ADDR = 0xA10006
    IOPORTS_IO_CTRL1_ADDR = 0xA10008
    IOPORTS_IO_CTRL2_ADDR = 0xA1000A
    IOPORTS_IO_CTRL3_ADDR = 0xA1000C
    # TradeMark Security System
    TMSS_BASE = 
    TMSS_TMSS_ADDR = 0xA14000
    # Z80 bus control
    Z80BUS_BASE = 
    Z80BUS_Z80_BUSREQ_ADDR = 0xA11100
    Z80BUS_Z80_RESET_ADDR = 0xA11200
    Z80BUS_Z80_YM2612_ADDR = 0xA04000

    # 中断向量定义
    INT_RESET_SP = 0  # Reset (Initial SP)
    INT_RESET_PC = 4  # Reset (Initial PC)
    INT_HBLANK = 24  # Horizontal blank interrupt
    INT_VBLANK = 28  # Vertical blank interrupt
    INT_EXTINT1 = 32  # External interrupt 1
    INT_EXTINT2 = 36  # External interrupt 2
    INT_EXTINT3 = 40  # External interrupt 3
    INT_EXTINT4 = 44  # External interrupt 4
    INT_EXTINT5 = 48  # External interrupt 5
    INT_EXTINT6 = 52  # External interrupt 6
    INT_EXTINT7 = 56  # External interrupt 7

    def __init__(self, memory_base: int = 0):
        """初始化设备"""
        self.memory_base = memory_base
        self._registers = {}
        self._peripherals = {}
        self._initialize_registers()
        self._initialize_peripherals()

    def _initialize_registers(self):
        """初始化寄存器""""
        self._registers["D0"] = {
            "address": 0,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Data Register 0",
            "value": 0
        }
        self._registers["D1"] = {
            "address": 0,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Data Register 1",
            "value": 0
        }
        self._registers["D2"] = {
            "address": 0,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Data Register 2",
            "value": 0
        }
        self._registers["D3"] = {
            "address": 0,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Data Register 3",
            "value": 0
        }
        self._registers["D4"] = {
            "address": 0,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Data Register 4",
            "value": 0
        }
        self._registers["D5"] = {
            "address": 0,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Data Register 5",
            "value": 0
        }
        self._registers["D6"] = {
            "address": 0,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Data Register 6",
            "value": 0
        }
        self._registers["D7"] = {
            "address": 0,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Data Register 7",
            "value": 0
        }
        self._registers["A0"] = {
            "address": 0,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Address Register 0",
            "value": 0
        }
        self._registers["A1"] = {
            "address": 0,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Address Register 1",
            "value": 0
        }
        self._registers["A2"] = {
            "address": 0,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Address Register 2",
            "value": 0
        }
        self._registers["A3"] = {
            "address": 0,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Address Register 3",
            "value": 0
        }
        self._registers["A4"] = {
            "address": 0,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Address Register 4",
            "value": 0
        }
        self._registers["A5"] = {
            "address": 0,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Address Register 5",
            "value": 0
        }
        self._registers["A6"] = {
            "address": 0,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Address Register 6",
            "value": 0
        }
        self._registers["A7"] = {
            "address": 0,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Address Register 7 (SP)",
            "value": 0
        }
        self._registers["PC"] = {
            "address": 0,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Program Counter",
            "value": 0
        }
        self._registers["SR"] = {
            "address": 0,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Status Register",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["VDP"] = {
            "base": ,
            "type": "Video",
            "description": "Video Display Processor (315-5313)",
            "registers": {
                "VDP_DATA": {
                    "address": 0xC00000,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "VDP_CONTROL": {
                    "address": 0xC00004,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "VDP_HVCOUNTER": {
                    "address": 0xC00008,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "VDP_PSG": {
                    "address": 0xC00011,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["YM2612"] = {
            "base": ,
            "type": "Audio",
            "description": "FM synthesis sound chip",
            "registers": {
                "YM2612_ADDR0": {
                    "address": 0xA04000,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM2612_DATA0": {
                    "address": 0xA04001,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM2612_ADDR1": {
                    "address": 0xA04002,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM2612_DATA1": {
                    "address": 0xA04003,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["IOPorts"] = {
            "base": ,
            "type": "IO",
            "description": "I/O ports",
            "registers": {
                "IO_DATA1": {
                    "address": 0xA10002,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IO_DATA2": {
                    "address": 0xA10004,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IO_DATA3": {
                    "address": 0xA10006,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IO_CTRL1": {
                    "address": 0xA10008,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IO_CTRL2": {
                    "address": 0xA1000A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IO_CTRL3": {
                    "address": 0xA1000C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["TMSS"] = {
            "base": ,
            "type": "Security",
            "description": "TradeMark Security System",
            "registers": {
                "TMSS": {
                    "address": 0xA14000,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["Z80Bus"] = {
            "base": ,
            "type": "Bus",
            "description": "Z80 bus control",
            "registers": {
                "Z80_BUSREQ": {
                    "address": 0xA11100,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "Z80_RESET": {
                    "address": 0xA11200,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "Z80_YM2612": {
                    "address": 0xA04000,
                    "size": 4,
                    "type": "uint32",
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
        return f"Sega_Genesis({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = Sega_Genesis()
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
