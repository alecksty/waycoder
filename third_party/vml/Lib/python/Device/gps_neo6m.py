"""
NEO6M设备定义 - Python模块
生成自: u-blox/GPS/NEO6M
版本: 1.0
日期: 2026-05-06
作者: VML Team
描述: NEO-6M GPS Module (UART, 50-channel, -162dBm tracking)
CPU架构: GPS
位宽: 8位
时钟频率: 9600 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class NEO6M:
    """NEO6M设备类"""

    # 设备信息
    DEVICE_NAME = "NEO6M"
    MANUFACTURER = "u-blox"
    FAMILY = "GPS"
    VERSION = "1.0"
    ARCHITECTURE = "GPS"
    BITS = 8
    CLOCK_FREQUENCY = 9600

    # 外设定义
    # NEO-6M GPS Module (UART 9600bps, 3.3V-5V)
    NEO6M_BASE = 0x00
    NEO6M_LATITUDE_ADDR = 0x00
    NEO6M_LONGITUDE_ADDR = 0x04
    NEO6M_ALTITUDE_ADDR = 0x08
    NEO6M_SPEED_ADDR = 0x0C
    NEO6M_HEADING_ADDR = 0x0E
    NEO6M_SATELLITES_ADDR = 0x10
    NEO6M_HDOP_ADDR = 0x11
    NEO6M_FIX_TYPE_ADDR = 0x13
    NEO6M_DATE_ADDR = 0x14
    NEO6M_TIME_ADDR = 0x18
    NEO6M_VALID_ADDR = 0x1C

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
        self._peripherals["NEO6M"] = {
            "base": 0x00,
            "type": "UART",
            "description": "NEO-6M GPS Module (UART 9600bps, 3.3V-5V)",
            "registers": {
                "LATITUDE": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LONGITUDE": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ALTITUDE": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SPEED": {
                    "address": 0x0C,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "HEADING": {
                    "address": 0x0E,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "SATELLITES": {
                    "address": 0x10,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "HDOP": {
                    "address": 0x11,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "FIX_TYPE": {
                    "address": 0x13,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DATE": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TIME": {
                    "address": 0x18,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "VALID": {
                    "address": 0x1C,
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
        return f"NEO6M({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = NEO6M()
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
