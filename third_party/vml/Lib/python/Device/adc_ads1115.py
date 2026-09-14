"""
ADS1115设备定义 - Python模块
生成自: Texas Instruments/ADC/ADS1115
版本: 1.0
日期: 2026-05-06
作者: VML Team
描述: ADS1115 16-bit I2C ADC (4-channel, PGA, 860SPS)
CPU架构: ADC
位宽: 16位
时钟频率: 400000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class ADS1115:
    """ADS1115设备类"""

    # 设备信息
    DEVICE_NAME = "ADS1115"
    MANUFACTURER = "Texas Instruments"
    FAMILY = "ADC"
    VERSION = "1.0"
    ARCHITECTURE = "ADC"
    BITS = 16
    CLOCK_FREQUENCY = 400000

    # 外设定义
    # ADS1115 16-bit ADC (0x48-0x4B, 2.0V-5.5V)
    ADS1115_BASE = 0x48
    ADS1115_CONV_RESULT_ADDR = 0x00
    ADS1115_CONFIG_ADDR = 0x01
    ADS1115_CONFIG_OS_BIT = 15  # Operational status/start single-shot
    ADS1115_CONFIG_MUX_BIT = 12  # Input multiplexer: 0=A0-A1,1=A0-A3,2=A1-A3,3=A2-A3,4=A0,5=A1,6=A2,7=A3
    ADS1115_CONFIG_PGA_BIT = 9  # PGA gain: 0=±6.144V,1=±4.096V,2=±2.048V,3=±1.024V,4=±0.512V,5=±0.256V
    ADS1115_CONFIG_MODE_BIT = 8  # 0=continuous, 1=single-shot
    ADS1115_CONFIG_DR_BIT = 5  # Data rate: 0=8,1=16,2=32,3=64,4=128,5=250,6=475,7=860 SPS
    ADS1115_CONFIG_COMP_MODE_BIT = 4  # Comparator mode (0=traditional, 1=window)
    ADS1115_CONFIG_COMP_POL_BIT = 3  # Comparator polarity (0=active low, 1=active high)
    ADS1115_LO_THRESH_ADDR = 0x02
    ADS1115_HI_THRESH_ADDR = 0x03

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
        self._peripherals["ADS1115"] = {
            "base": 0x48,
            "type": "I2C",
            "description": "ADS1115 16-bit ADC (0x48-0x4B, 2.0V-5.5V)",
            "registers": {
                "CONV_RESULT": {
                    "address": 0x00,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "CONFIG": {
                    "address": 0x01,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "LO_THRESH": {
                    "address": 0x02,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "HI_THRESH": {
                    "address": 0x03,
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
        return f"ADS1115({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = ADS1115()
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
