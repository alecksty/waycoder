"""
Macintosh-128K设备定义 - Python模块
生成自: Apple Computer/Macintosh/Macintosh-128K
版本: 1.0
日期: 2026-04-17
作者: VML Team
描述: Original Macintosh 128K with Motorola 68000 CPU, 128KB RAM, and 9-inch monochrome display
CPU架构: Motorola 68000
位宽: 32位
时钟频率: 7998000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class Macintosh_128K:
    """Macintosh-128K设备类"""

    # 设备信息
    DEVICE_NAME = "Macintosh-128K"
    MANUFACTURER = "Apple Computer"
    FAMILY = "Macintosh"
    VERSION = "1.0"
    ARCHITECTURE = "Motorola 68000"
    BITS = 32
    CLOCK_FREQUENCY = 7998000

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
    # Versatile Interface Adapter (6522)
    VIA_BASE = 
    VIA_VIA_ORB_ADDR = 0xE80000
    VIA_VIA_ORA_ADDR = 0xE80001
    VIA_VIA_DDRB_ADDR = 0xE80002
    VIA_VIA_DDRA_ADDR = 0xE80003
    VIA_VIA_T1CL_ADDR = 0xE80004
    VIA_VIA_T1CH_ADDR = 0xE80005
    VIA_VIA_T1LL_ADDR = 0xE80006
    VIA_VIA_T1LH_ADDR = 0xE80007
    VIA_VIA_T2CL_ADDR = 0xE80008
    VIA_VIA_T2CH_ADDR = 0xE80009
    VIA_VIA_SR_ADDR = 0xE8000A
    VIA_VIA_ACR_ADDR = 0xE8000B
    VIA_VIA_PCR_ADDR = 0xE8000C
    VIA_VIA_IFR_ADDR = 0xE8000D
    VIA_VIA_IER_ADDR = 0xE8000E
    VIA_VIA_ORA2_ADDR = 0xE8000F
    # Integrated Woz Machine (floppy controller)
    IWM_BASE = 
    IWM_IWM_Q6_ADDR = 0xD00000
    IWM_IWM_Q7_ADDR = 0xD00002
    IWM_IWM_PH0_ADDR = 0xD00004
    IWM_IWM_PH1_ADDR = 0xD00006
    IWM_IWM_PH2_ADDR = 0xD00008
    IWM_IWM_PH3_ADDR = 0xD0000A
    # Zilog 8530 Serial Communications Controller
    SCC_BASE = 
    SCC_SCC_CA_ADDR = 0x500000
    SCC_SCC_DA_ADDR = 0x500002
    SCC_SCC_CB_ADDR = 0x500004
    SCC_SCC_DB_ADDR = 0x500006
    # Built-in speaker
    SOUND_BASE = 
    SOUND_SOUND_VOL_ADDR = 0xE80100
    SOUND_SOUND_FREQ_ADDR = 0xE80102

    # 中断向量定义
    INT_RESET_SP = 0  # Reset (Initial SP)
    INT_RESET_PC = 4  # Reset (Initial PC)
    INT_AUTOVECTOR1 = 24  # Auto vector 1
    INT_AUTOVECTOR2 = 25  # Auto vector 2
    INT_AUTOVECTOR3 = 26  # Auto vector 3
    INT_AUTOVECTOR4 = 27  # Auto vector 4
    INT_AUTOVECTOR5 = 28  # Auto vector 5
    INT_AUTOVECTOR6 = 29  # Auto vector 6
    INT_AUTOVECTOR7 = 30  # Auto vector 7
    INT_SPURIOUS = 31  # Spurious interrupt

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
        self._peripherals["VIA"] = {
            "base": ,
            "type": "IO",
            "description": "Versatile Interface Adapter (6522)",
            "registers": {
                "VIA_ORB": {
                    "address": 0xE80000,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_ORA": {
                    "address": 0xE80001,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_DDRB": {
                    "address": 0xE80002,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_DDRA": {
                    "address": 0xE80003,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_T1CL": {
                    "address": 0xE80004,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_T1CH": {
                    "address": 0xE80005,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_T1LL": {
                    "address": 0xE80006,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_T1LH": {
                    "address": 0xE80007,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_T2CL": {
                    "address": 0xE80008,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_T2CH": {
                    "address": 0xE80009,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_SR": {
                    "address": 0xE8000A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_ACR": {
                    "address": 0xE8000B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_PCR": {
                    "address": 0xE8000C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_IFR": {
                    "address": 0xE8000D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_IER": {
                    "address": 0xE8000E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_ORA2": {
                    "address": 0xE8000F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["IWM"] = {
            "base": ,
            "type": "Storage",
            "description": "Integrated Woz Machine (floppy controller)",
            "registers": {
                "IWM_Q6": {
                    "address": 0xD00000,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IWM_Q7": {
                    "address": 0xD00002,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IWM_PH0": {
                    "address": 0xD00004,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IWM_PH1": {
                    "address": 0xD00006,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IWM_PH2": {
                    "address": 0xD00008,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IWM_PH3": {
                    "address": 0xD0000A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["SCC"] = {
            "base": ,
            "type": "Serial",
            "description": "Zilog 8530 Serial Communications Controller",
            "registers": {
                "SCC_CA": {
                    "address": 0x500000,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SCC_DA": {
                    "address": 0x500002,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SCC_CB": {
                    "address": 0x500004,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SCC_DB": {
                    "address": 0x500006,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["Sound"] = {
            "base": ,
            "type": "Audio",
            "description": "Built-in speaker",
            "registers": {
                "SOUND_VOL": {
                    "address": 0xE80100,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SOUND_FREQ": {
                    "address": 0xE80102,
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
        return f"Macintosh_128K({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = Macintosh_128K()
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
