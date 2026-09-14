"""
Apple-II设备定义 - Python模块
生成自: Apple Computer/Apple II/Apple-II
版本: 1.0
日期: 2026-04-17
作者: VML Team
描述: Apple II personal computer with MOS 6502 CPU, 48KB RAM, and color graphics
CPU架构: MOS 6502
位宽: 8位
时钟频率: 1023000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class Apple_II:
    """Apple-II设备类"""

    # 设备信息
    DEVICE_NAME = "Apple-II"
    MANUFACTURER = "Apple Computer"
    FAMILY = "Apple II"
    VERSION = "1.0"
    ARCHITECTURE = "MOS 6502"
    BITS = 8
    CLOCK_FREQUENCY = 1023000

    # 寄存器地址定义
    A_ADDR = 0  # Accumulator
    X_ADDR = 0  # Index Register X
    Y_ADDR = 0  # Index Register Y
    SP_ADDR = 0  # Stack Pointer
    PC_ADDR = 0  # Program Counter
    P_ADDR = 0  # Status Register

    # 外设定义
    # Apple II keyboard
    KEYBOARD_BASE = 
    KEYBOARD_KBD_ADDR = 0xC000
    KEYBOARD_KBDSTRB_ADDR = 0xC010
    # Built-in speaker
    SPEAKER_BASE = 
    SPEAKER_SPKR_ADDR = 0xC030
    # Cassette tape interface
    CASSETTE_BASE = 
    CASSETTE_TAPEIN_ADDR = 0xC060
    CASSETTE_TAPEOUT_ADDR = 0xC020
    # Game controller port
    GAMEPORT_BASE = 
    GAMEPORT_PADDLE0_ADDR = 0xC064
    GAMEPORT_PADDLE1_ADDR = 0xC065
    GAMEPORT_PADDLE2_ADDR = 0xC066
    GAMEPORT_PADDLE3_ADDR = 0xC067
    GAMEPORT_BUTTON0_ADDR = 0xC061
    GAMEPORT_BUTTON1_ADDR = 0xC062
    # Disk II controller
    DISKCONTROLLER_BASE = 
    DISKCONTROLLER_DISKUNIT_ADDR = 0xC0E0
    DISKCONTROLLER_DISKCMD_ADDR = 0xC0E8
    DISKCONTROLLER_DISKSTAT_ADDR = 0xC0E9
    DISKCONTROLLER_DISKDATA_ADDR = 0xC0EA

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
        self._peripherals["Keyboard"] = {
            "base": ,
            "type": "Input",
            "description": "Apple II keyboard",
            "registers": {
                "KBD": {
                    "address": 0xC000,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "KBDSTRB": {
                    "address": 0xC010,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["Speaker"] = {
            "base": ,
            "type": "Audio",
            "description": "Built-in speaker",
            "registers": {
                "SPKR": {
                    "address": 0xC030,
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
                "TAPEIN": {
                    "address": 0xC060,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TAPEOUT": {
                    "address": 0xC020,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["GamePort"] = {
            "base": ,
            "type": "Input",
            "description": "Game controller port",
            "registers": {
                "PADDLE0": {
                    "address": 0xC064,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PADDLE1": {
                    "address": 0xC065,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PADDLE2": {
                    "address": 0xC066,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PADDLE3": {
                    "address": 0xC067,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "BUTTON0": {
                    "address": 0xC061,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "BUTTON1": {
                    "address": 0xC062,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["DiskController"] = {
            "base": ,
            "type": "Storage",
            "description": "Disk II controller",
            "registers": {
                "DISKUNIT": {
                    "address": 0xC0E0,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DISKCMD": {
                    "address": 0xC0E8,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DISKSTAT": {
                    "address": 0xC0E9,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DISKDATA": {
                    "address": 0xC0EA,
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
        return f"Apple_II({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = Apple_II()
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
