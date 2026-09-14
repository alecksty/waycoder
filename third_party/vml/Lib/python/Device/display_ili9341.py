"""
ILI9341设备定义 - Python模块
生成自: Ilitek/Display/ILI9341
版本: 1.0
日期: 2026-05-06
作者: VML Team
描述: ILI9341 2.8" 240x320 TFT LCD Display (SPI, 18-bit color, touch)
CPU架构: Display
位宽: 18位
时钟频率: 20000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class ILI9341:
    """ILI9341设备类"""

    # 设备信息
    DEVICE_NAME = "ILI9341"
    MANUFACTURER = "Ilitek"
    FAMILY = "Display"
    VERSION = "1.0"
    ARCHITECTURE = "Display"
    BITS = 18
    CLOCK_FREQUENCY = 20000000

    # 内存段定义
    GRAM_START = 0x00
    GRAM_END = 0xBCFF
    GRAM_SIZE = 156672  # Graphics RAM (240x320x18bit)

    # 外设定义
    # ILI9341 240x320 TFT (SPI, 3.3V, 2.8inch)
    ILI9341_BASE = 0x00
    ILI9341_CMD_ADDR = 0x00
    ILI9341_DATA_ADDR = 0x01
    ILI9341_COL_START_ADDR = 0x2A
    ILI9341_PAGE_START_ADDR = 0x2B
    ILI9341_WRITE_RAM_ADDR = 0x2C
    ILI9341_MADCTL_ADDR = 0x36
    ILI9341_PIXFMT_ADDR = 0x3A
    ILI9341_FRMCTL_ADDR = 0xB1
    ILI9341_GAMMA_ADDR = 0x26
    ILI9341_SLEEP_OUT_ADDR = 0x11
    ILI9341_DISP_ON_ADDR = 0x29

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
        self._peripherals["ILI9341"] = {
            "base": 0x00,
            "type": "SPI",
            "description": "ILI9341 240x320 TFT (SPI, 3.3V, 2.8inch)",
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
                "COL_START": {
                    "address": 0x2A,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "PAGE_START": {
                    "address": 0x2B,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "WRITE_RAM": {
                    "address": 0x2C,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "MADCTL": {
                    "address": 0x36,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PIXFMT": {
                    "address": 0x3A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FRMCTL": {
                    "address": 0xB1,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "GAMMA": {
                    "address": 0x26,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SLEEP_OUT": {
                    "address": 0x11,
                    "size": 0,
                    "type": "bytes[0]",
                    "value": 0
                },
                "DISP_ON": {
                    "address": 0x29,
                    "size": 0,
                    "type": "bytes[0]",
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
        return f"ILI9341({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = ILI9341()
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
