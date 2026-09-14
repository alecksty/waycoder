"""
HD44780设备定义 - Python模块
生成自: Hitachi/Display/HD44780
版本: 1.0
日期: 2026-05-06
作者: VML Team
描述: HD44780 16x2 Character LCD Controller (4-bit/8-bit parallel or I2C via PCF8574)
CPU架构: Display
位宽: 8位
时钟频率: 0 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class HD44780:
    """HD44780设备类"""

    # 设备信息
    DEVICE_NAME = "HD44780"
    MANUFACTURER = "Hitachi"
    FAMILY = "Display"
    VERSION = "1.0"
    ARCHITECTURE = "Display"
    BITS = 8
    CLOCK_FREQUENCY = 0

    # 内存段定义
    DDRAM_START = 0x00
    DDRAM_END = 0x4F
    DDRAM_SIZE = 80  # Display Data RAM (80 bytes, 2 lines)
    CGRAM_START = 0x00
    CGRAM_END = 0x3F
    CGRAM_SIZE = 64  # Character Generator RAM (8 custom chars x 8 bytes)

    # 外设定义
    # HD44780 16x2 LCD (0x27/0x3F I2C, 5V)
    HD44780_BASE = 0x27
    HD44780_CMD_ADDR = 0x00
    HD44780_DATA_ADDR = 0x01
    HD44780_CTRL_RS_ADDR = 0x00
    HD44780_CTRL_RW_ADDR = 0x01
    HD44780_CTRL_EN_ADDR = 0x02
    HD44780_CTRL_BL_ADDR = 0x03
    HD44780_ADDR_DDRAM_ADDR = 0x80
    HD44780_ADDR_CGRAM_ADDR = 0x40

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
        self._peripherals["HD44780"] = {
            "base": 0x27,
            "type": "Parallel/I2C",
            "description": "HD44780 16x2 LCD (0x27/0x3F I2C, 5V)",
            "registers": {
                "CMD": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DATA": {
                    "address": 0x01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CTRL_RS": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CTRL_RW": {
                    "address": 0x01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CTRL_EN": {
                    "address": 0x02,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CTRL_BL": {
                    "address": 0x03,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ADDR_DDRAM": {
                    "address": 0x80,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ADDR_CGRAM": {
                    "address": 0x40,
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
        return f"HD44780({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = HD44780()
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
