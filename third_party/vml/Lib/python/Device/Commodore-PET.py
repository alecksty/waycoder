"""
Commodore-PET设备定义 - Python模块
生成自: Commodore International/PET/Commodore-PET
版本: 1.0
日期: 2026-04-17
作者: VML Team
描述: Commodore PET 2001 personal computer with MOS 6502 CPU and built-in monitor
CPU架构: MOS 6502
位宽: 8位
时钟频率: 1000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class Commodore_PET:
    """Commodore-PET设备类"""

    # 设备信息
    DEVICE_NAME = "Commodore-PET"
    MANUFACTURER = "Commodore International"
    FAMILY = "PET"
    VERSION = "1.0"
    ARCHITECTURE = "MOS 6502"
    BITS = 8
    CLOCK_FREQUENCY = 1000000

    # 寄存器地址定义
    A_ADDR = 0  # Accumulator
    X_ADDR = 0  # Index Register X
    Y_ADDR = 0  # Index Register Y
    SP_ADDR = 0  # Stack Pointer
    PC_ADDR = 0  # Program Counter
    P_ADDR = 0  # Status Register

    # 外设定义
    # Peripheral Interface Adapter 1 (6520)
    PIA1_BASE = 
    PIA1_PIA1_DDRA_ADDR = 0xE810
    PIA1_PIA1_ORA_ADDR = 0xE811
    PIA1_PIA1_DDRB_ADDR = 0xE812
    PIA1_PIA1_ORB_ADDR = 0xE813
    PIA1_PIA1_CRA_ADDR = 0xE814
    PIA1_PIA1_CRB_ADDR = 0xE815
    # Peripheral Interface Adapter 2 (6520)
    PIA2_BASE = 
    PIA2_PIA2_DDRA_ADDR = 0xE820
    PIA2_PIA2_ORA_ADDR = 0xE821
    PIA2_PIA2_DDRB_ADDR = 0xE822
    PIA2_PIA2_ORB_ADDR = 0xE823
    PIA2_PIA2_CRA_ADDR = 0xE824
    PIA2_PIA2_CRB_ADDR = 0xE825
    # Versatile Interface Adapter (6522)
    VIA_BASE = 
    VIA_VIA_ORB_ADDR = 0xE840
    VIA_VIA_ORA_ADDR = 0xE841
    VIA_VIA_DDRB_ADDR = 0xE842
    VIA_VIA_DDRA_ADDR = 0xE843
    VIA_VIA_T1CL_ADDR = 0xE844
    VIA_VIA_T1CH_ADDR = 0xE845
    VIA_VIA_T1LL_ADDR = 0xE846
    VIA_VIA_T1LH_ADDR = 0xE847
    VIA_VIA_T2CL_ADDR = 0xE848
    VIA_VIA_T2CH_ADDR = 0xE849
    VIA_VIA_SR_ADDR = 0xE84A
    VIA_VIA_ACR_ADDR = 0xE84B
    VIA_VIA_PCR_ADDR = 0xE84C
    VIA_VIA_IFR_ADDR = 0xE84D
    VIA_VIA_IER_ADDR = 0xE84E
    # CRT Controller (6545)
    CRTC_BASE = 
    CRTC_CRTC_ADDR_ADDR = 0xE880
    CRTC_CRTC_DATA_ADDR = 0xE881
    # Cassette tape interface
    CASSETTE_BASE = 
    CASSETTE_CASS_MOTOR_ADDR = 0xE840
    CASSETTE_CASS_WRITE_ADDR = 0xE842
    CASSETTE_CASS_READ_ADDR = 0xE812
    # IEEE-488 bus interface
    IEEE488_BASE = 
    IEEE488_IEEE_DATA_ADDR = 0xE801
    IEEE488_IEEE_STATUS_ADDR = 0xE802
    IEEE488_IEEE_CONTROL_ADDR = 0xE803

    # 中断向量定义
    INT_NMI = 65526  # Non-maskable interrupt
    INT_RESET = 65528  # Reset vector
    INT_IRQ = 65530  # Interrupt request
    INT_BRK = 65532  # Break instruction

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
        self._registers["X"] = {
            "address": 0,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Index Register X",
            "value": 0
        }
        self._registers["Y"] = {
            "address": 0,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Index Register Y",
            "value": 0
        }
        self._registers["SP"] = {
            "address": 0,
            "size": 1,
            "type": "uint8",
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
        self._registers["P"] = {
            "address": 0,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Status Register",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["PIA1"] = {
            "base": ,
            "type": "IO",
            "description": "Peripheral Interface Adapter 1 (6520)",
            "registers": {
                "PIA1_DDRA": {
                    "address": 0xE810,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PIA1_ORA": {
                    "address": 0xE811,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PIA1_DDRB": {
                    "address": 0xE812,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PIA1_ORB": {
                    "address": 0xE813,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PIA1_CRA": {
                    "address": 0xE814,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PIA1_CRB": {
                    "address": 0xE815,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PIA2"] = {
            "base": ,
            "type": "IO",
            "description": "Peripheral Interface Adapter 2 (6520)",
            "registers": {
                "PIA2_DDRA": {
                    "address": 0xE820,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PIA2_ORA": {
                    "address": 0xE821,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PIA2_DDRB": {
                    "address": 0xE822,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PIA2_ORB": {
                    "address": 0xE823,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PIA2_CRA": {
                    "address": 0xE824,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PIA2_CRB": {
                    "address": 0xE825,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["VIA"] = {
            "base": ,
            "type": "IO",
            "description": "Versatile Interface Adapter (6522)",
            "registers": {
                "VIA_ORB": {
                    "address": 0xE840,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_ORA": {
                    "address": 0xE841,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_DDRB": {
                    "address": 0xE842,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_DDRA": {
                    "address": 0xE843,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_T1CL": {
                    "address": 0xE844,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_T1CH": {
                    "address": 0xE845,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_T1LL": {
                    "address": 0xE846,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_T1LH": {
                    "address": 0xE847,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_T2CL": {
                    "address": 0xE848,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_T2CH": {
                    "address": 0xE849,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_SR": {
                    "address": 0xE84A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_ACR": {
                    "address": 0xE84B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_PCR": {
                    "address": 0xE84C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_IFR": {
                    "address": 0xE84D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIA_IER": {
                    "address": 0xE84E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["CRTC"] = {
            "base": ,
            "type": "Video",
            "description": "CRT Controller (6545)",
            "registers": {
                "CRTC_ADDR": {
                    "address": 0xE880,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CRTC_DATA": {
                    "address": 0xE881,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["Cassette"] = {
            "base": ,
            "type": "Storage",
            "description": "Cassette tape interface",
            "registers": {
                "CASS_MOTOR": {
                    "address": 0xE840,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CASS_WRITE": {
                    "address": 0xE842,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CASS_READ": {
                    "address": 0xE812,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["IEEE488"] = {
            "base": ,
            "type": "IO",
            "description": "IEEE-488 bus interface",
            "registers": {
                "IEEE_DATA": {
                    "address": 0xE801,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IEEE_STATUS": {
                    "address": 0xE802,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IEEE_CONTROL": {
                    "address": 0xE803,
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
        return f"Commodore_PET({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = Commodore_PET()
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
