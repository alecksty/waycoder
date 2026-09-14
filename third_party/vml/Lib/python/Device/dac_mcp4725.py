"""
MCP4725设备定义 - Python模块
生成自: Microchip/DAC/MCP4725
版本: 1.0
日期: 2026-05-06
作者: VML Team
描述: MCP4725 12-bit I2C DAC (single channel, EEPROM)
CPU架构: DAC
位宽: 12位
时钟频率: 400000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class MCP4725:
    """MCP4725设备类"""

    # 设备信息
    DEVICE_NAME = "MCP4725"
    MANUFACTURER = "Microchip"
    FAMILY = "DAC"
    VERSION = "1.0"
    ARCHITECTURE = "DAC"
    BITS = 12
    CLOCK_FREQUENCY = 400000

    # 内存段定义
    EEPROM_START = 0x00
    EEPROM_END = 0x01
    EEPROM_SIZE = 2  # Power-on default DAC value

    # 外设定义
    # MCP4725 12-bit DAC (0x60-0x67, 2.7V-5.5V)
    MCP4725_BASE = 0x60
    MCP4725_DAC_VALUE_ADDR = 0x00
    MCP4725_DAC_VALUE_PD_BIT = 12  # Power-down: 0=normal,1=1kΩ,2=100kΩ,3=500kΩ
    MCP4725_WRITE_EEPROM_ADDR = 0x60

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
        self._peripherals["MCP4725"] = {
            "base": 0x60,
            "type": "I2C",
            "description": "MCP4725 12-bit DAC (0x60-0x67, 2.7V-5.5V)",
            "registers": {
                "DAC_VALUE": {
                    "address": 0x00,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "WRITE_EEPROM": {
                    "address": 0x60,
                    "size": 2,
                    "type": "uint16",
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
        return f"MCP4725({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = MCP4725()
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
