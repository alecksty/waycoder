"""
MAX7219设备定义 - Python模块
生成自: Maxim/LED/MAX7219
版本: 1.0
日期: 2026-05-06
作者: VML Team
描述: MAX7219 8-Digit LED Display Driver (SPI, daisy-chainable, 8x8 matrix)
CPU架构: LED
位宽: 8位
时钟频率: 10000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class MAX7219:
    """MAX7219设备类"""

    # 设备信息
    DEVICE_NAME = "MAX7219"
    MANUFACTURER = "Maxim"
    FAMILY = "LED"
    VERSION = "1.0"
    ARCHITECTURE = "LED"
    BITS = 8
    CLOCK_FREQUENCY = 10000000

    # 外设定义
    # MAX7219 8-Digit/8x8 Matrix Driver (4.0V-5.5V, DIP-24)
    MAX7219_BASE = 0x00
    MAX7219_DIGIT0_ADDR = 0x01
    MAX7219_DIGIT1_ADDR = 0x02
    MAX7219_DIGIT2_ADDR = 0x03
    MAX7219_DIGIT3_ADDR = 0x04
    MAX7219_DIGIT4_ADDR = 0x05
    MAX7219_DIGIT5_ADDR = 0x06
    MAX7219_DIGIT6_ADDR = 0x07
    MAX7219_DIGIT7_ADDR = 0x08
    MAX7219_DECODE_ADDR = 0x09
    MAX7219_INTENSITY_ADDR = 0x0A
    MAX7219_SCAN_LIMIT_ADDR = 0x0B
    MAX7219_SHUTDOWN_ADDR = 0x0C
    MAX7219_TEST_ADDR = 0x0F

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
        self._peripherals["MAX7219"] = {
            "base": 0x00,
            "type": "SPI",
            "description": "MAX7219 8-Digit/8x8 Matrix Driver (4.0V-5.5V, DIP-24)",
            "registers": {
                "DIGIT0": {
                    "address": 0x01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DIGIT1": {
                    "address": 0x02,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DIGIT2": {
                    "address": 0x03,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DIGIT3": {
                    "address": 0x04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DIGIT4": {
                    "address": 0x05,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DIGIT5": {
                    "address": 0x06,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DIGIT6": {
                    "address": 0x07,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DIGIT7": {
                    "address": 0x08,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DECODE": {
                    "address": 0x09,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "INTENSITY": {
                    "address": 0x0A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SCAN_LIMIT": {
                    "address": 0x0B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SHUTDOWN": {
                    "address": 0x0C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TEST": {
                    "address": 0x0F,
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
        return f"MAX7219({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = MAX7219()
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
