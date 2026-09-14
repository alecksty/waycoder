"""
BH1750设备定义 - Python模块
生成自: ROHM/Sensor/BH1750
版本: 1.0
日期: 2026-05-06
作者: VML Team
描述: BH1750FVI Digital Ambient Light Sensor (I2C, 1-65535 lux, 16-bit)
CPU架构: Sensor
位宽: 16位
时钟频率: 400000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class BH1750:
    """BH1750设备类"""

    # 设备信息
    DEVICE_NAME = "BH1750"
    MANUFACTURER = "ROHM"
    FAMILY = "Sensor"
    VERSION = "1.0"
    ARCHITECTURE = "Sensor"
    BITS = 16
    CLOCK_FREQUENCY = 400000

    # 外设定义
    # BH1750 Light Sensor (0x23/0x5C, 2.4V-3.6V)
    BH1750_BASE = 0x23
    BH1750_LUX_ADDR = 0x00
    BH1750_MODE_ADDR = 0x01
    BH1750_MODE_CONT_H_BIT = 0  # Continuous High Res (1lx, 120ms)
    BH1750_MODE_CONT_H2_BIT = 1  # Continuous High Res 2 (0.5lx, 120ms)
    BH1750_MODE_CONT_L_BIT = 2  # Continuous Low Res (4lx, 16ms)
    BH1750_MODE_ONCE_H_BIT = 3  # One-time High Res (1lx, 120ms)
    BH1750_MODE_ONCE_H2_BIT = 4  # One-time High Res 2 (0.5lx, 120ms)
    BH1750_MODE_ONCE_L_BIT = 5  # One-time Low Res (4lx, 16ms)
    BH1750_CMD_POWER_ON_ADDR = 0x01
    BH1750_CMD_POWER_OFF_ADDR = 0x00
    BH1750_CMD_RESET_ADDR = 0x07

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
        self._peripherals["BH1750"] = {
            "base": 0x23,
            "type": "I2C",
            "description": "BH1750 Light Sensor (0x23/0x5C, 2.4V-3.6V)",
            "registers": {
                "LUX": {
                    "address": 0x00,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "MODE": {
                    "address": 0x01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CMD_POWER_ON": {
                    "address": 0x01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CMD_POWER_OFF": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CMD_RESET": {
                    "address": 0x07,
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
        return f"BH1750({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = BH1750()
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
