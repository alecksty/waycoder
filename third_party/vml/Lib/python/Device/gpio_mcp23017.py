"""
MCP23017设备定义 - Python模块
生成自: Microchip/GPIO/MCP23017
版本: 1.0
日期: 2026-05-06
作者: VML Team
描述: MCP23017 16-bit I2C GPIO Expander (2 banks, interrupt, 25mA per pin)
CPU架构: GPIO
位宽: 16位
时钟频率: 400000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class MCP23017:
    """MCP23017设备类"""

    # 设备信息
    DEVICE_NAME = "MCP23017"
    MANUFACTURER = "Microchip"
    FAMILY = "GPIO"
    VERSION = "1.0"
    ARCHITECTURE = "GPIO"
    BITS = 16
    CLOCK_FREQUENCY = 400000

    # 外设定义
    # MCP23017 16-bit GPIO (0x20-0x27, 1.8V-5.5V)
    MCP23017_BASE = 0x20
    MCP23017_IODIRA_ADDR = 0x00
    MCP23017_IODIRB_ADDR = 0x01
    MCP23017_GPIOA_ADDR = 0x12
    MCP23017_GPIOB_ADDR = 0x13
    MCP23017_GPINTENA_ADDR = 0x04
    MCP23017_GPINTENB_ADDR = 0x05
    MCP23017_INTCONA_ADDR = 0x08
    MCP23017_IOCON_ADDR = 0x0A
    MCP23017_GPPUA_ADDR = 0x0C
    MCP23017_GPPUB_ADDR = 0x0D

    # 中断向量定义
    INT_INTA = 0  # Port A interrupt
    INT_INTB = 1  # Port B interrupt

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
        self._peripherals["MCP23017"] = {
            "base": 0x20,
            "type": "I2C",
            "description": "MCP23017 16-bit GPIO (0x20-0x27, 1.8V-5.5V)",
            "registers": {
                "IODIRA": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IODIRB": {
                    "address": 0x01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GPIOA": {
                    "address": 0x12,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GPIOB": {
                    "address": 0x13,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GPINTENA": {
                    "address": 0x04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GPINTENB": {
                    "address": 0x05,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "INTCONA": {
                    "address": 0x08,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IOCON": {
                    "address": 0x0A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GPPUA": {
                    "address": 0x0C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GPPUB": {
                    "address": 0x0D,
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
        return f"MCP23017({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = MCP23017()
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
