"""
nRF52832设备定义 - Python模块
生成自: Nordic/nRF52/nRF52832
版本: 1.0
日期: 2026-04-28
作者: VML Team
描述: 32-bit ARM Cortex-M4F BLE SoC with 512KB Flash, 64KB RAM, 64MHz
CPU架构: ARM-Cortex-M4F
位宽: 32位
时钟频率: 64000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class nRF52832:
    """nRF52832设备类"""

    # 设备信息
    DEVICE_NAME = "nRF52832"
    MANUFACTURER = "Nordic"
    FAMILY = "nRF52"
    VERSION = "1.0"
    ARCHITECTURE = "ARM-Cortex-M4F"
    BITS = 32
    CLOCK_FREQUENCY = 64000000

    # 寄存器地址定义
    R0_ADDR = 0x00  # 
    R1_ADDR = 0x04  # 
    R2_ADDR = 0x08  # 
    R3_ADDR = 0x0C  # 
    SP_ADDR = 0x34  # 
    LR_ADDR = 0x38  # 
    PC_ADDR = 0x3C  # 

    # 内存段定义
    FLASH_START = 0x00000000
    FLASH_END = 0x0007FFFF
    FLASH_SIZE = 524288  # 
    SRAM_START = 0x20000000
    SRAM_END = 0x2000FFFF
    SRAM_SIZE = 65536  # 
    PERIPHERAL_START = 0x40000000
    PERIPHERAL_END = 0x400FFFFF
    PERIPHERAL_SIZE = 1048576  # 
    FICR_START = 0x10000000
    FICR_END = 0x10000FFF
    FICR_SIZE = 4096  # Factory Information Configuration Registers

    # 外设定义
    # General Purpose I/O Port 0
    GPIO_P0_BASE = 0x50000000
    GPIO_P0_OUT_ADDR = 0x504
    GPIO_P0_OUTSET_ADDR = 0x508
    GPIO_P0_OUTCLR_ADDR = 0x50C
    GPIO_P0_IN_ADDR = 0x510
    GPIO_P0_DIR_ADDR = 0x514
    GPIO_P0_DIRSET_ADDR = 0x518
    GPIO_P0_DIRCLR_ADDR = 0x51C
    # Power Control
    POWER_BASE = 0x40000000
    POWER_DCDCEN_ADDR = 0x1C4
    POWER_RAMSTATUS_ADDR = 0x268
    # Clock Control
    CLOCK_BASE = 0x40000000
    CLOCK_HFCLKSTART_ADDR = 0x108
    CLOCK_HFCLKSTARTED_ADDR = 0x208

    # 中断向量定义
    INT_RESET = 0  # 
    INT_SVCALL = 11  # 

    def __init__(self, memory_base: int = 0):
        """初始化设备"""
        self.memory_base = memory_base
        self._registers = {}
        self._peripherals = {}
        self._initialize_registers()
        self._initialize_peripherals()

    def _initialize_registers(self):
        """初始化寄存器""""
        self._registers["R0"] = {
            "address": 0x00,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R1"] = {
            "address": 0x04,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R2"] = {
            "address": 0x08,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R3"] = {
            "address": 0x0C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["SP"] = {
            "address": 0x34,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["LR"] = {
            "address": 0x38,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["PC"] = {
            "address": 0x3C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["GPIO_P0"] = {
            "base": 0x50000000,
            "type": "GPIO",
            "description": "General Purpose I/O Port 0",
            "registers": {
                "OUT": {
                    "address": 0x504,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OUTSET": {
                    "address": 0x508,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OUTCLR": {
                    "address": 0x50C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IN": {
                    "address": 0x510,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DIR": {
                    "address": 0x514,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DIRSET": {
                    "address": 0x518,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DIRCLR": {
                    "address": 0x51C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["POWER"] = {
            "base": 0x40000000,
            "type": "PowerControl",
            "description": "Power Control",
            "registers": {
                "DCDCEN": {
                    "address": 0x1C4,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RAMSTATUS": {
                    "address": 0x268,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["CLOCK"] = {
            "base": 0x40000000,
            "type": "ClockControl",
            "description": "Clock Control",
            "registers": {
                "HFCLKSTART": {
                    "address": 0x108,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HFCLKSTARTED": {
                    "address": 0x208,
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
        return f"nRF52832({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = nRF52832()
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
