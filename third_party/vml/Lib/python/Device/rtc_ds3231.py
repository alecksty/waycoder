"""
DS3231设备定义 - Python模块
生成自: Maxim/Dallas/RTC/DS3231
版本: 1.0
日期: 2026-05-06
作者: VML Team
描述: DS3231 I2C High-Precision RTC (±2ppm, temperature compensated, 32K EEPROM)
CPU架构: RTC
位宽: 8位
时钟频率: 400000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class DS3231:
    """DS3231设备类"""

    # 设备信息
    DEVICE_NAME = "DS3231"
    MANUFACTURER = "Maxim/Dallas"
    FAMILY = "RTC"
    VERSION = "1.0"
    ARCHITECTURE = "RTC"
    BITS = 8
    CLOCK_FREQUENCY = 400000

    # 内存段定义
    EEPROM_START = 0x14
    EEPROM_END = 0xFF
    EEPROM_SIZE = 236  # AT24C32 EEPROM (32Kbit)

    # 外设定义
    # DS3231 Precision RTC (0x68, 3.3V-5.5V)
    DS3231_BASE = 0x68
    DS3231_SEC_ADDR = 0x00
    DS3231_MIN_ADDR = 0x01
    DS3231_HOUR_ADDR = 0x02
    DS3231_DAY_ADDR = 0x03
    DS3231_DATE_ADDR = 0x04
    DS3231_MONTH_CENT_ADDR = 0x05
    DS3231_YEAR_ADDR = 0x06
    DS3231_ALARM1_SEC_ADDR = 0x07
    DS3231_ALARM1_MIN_ADDR = 0x08
    DS3231_ALARM1_HOUR_ADDR = 0x09
    DS3231_ALARM2_MIN_ADDR = 0x0B
    DS3231_ALARM2_HOUR_ADDR = 0x0C
    DS3231_CTRL_ADDR = 0x0E
    DS3231_CTRL_STATUS_ADDR = 0x0F
    DS3231_TEMP_MSB_ADDR = 0x11
    DS3231_TEMP_LSB_ADDR = 0x12

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
        self._peripherals["DS3231"] = {
            "base": 0x68,
            "type": "I2C",
            "description": "DS3231 Precision RTC (0x68, 3.3V-5.5V)",
            "registers": {
                "SEC": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "MIN": {
                    "address": 0x01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "HOUR": {
                    "address": 0x02,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DAY": {
                    "address": 0x03,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DATE": {
                    "address": 0x04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "MONTH_CENT": {
                    "address": 0x05,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YEAR": {
                    "address": 0x06,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ALARM1_SEC": {
                    "address": 0x07,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ALARM1_MIN": {
                    "address": 0x08,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ALARM1_HOUR": {
                    "address": 0x09,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ALARM2_MIN": {
                    "address": 0x0B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ALARM2_HOUR": {
                    "address": 0x0C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CTRL": {
                    "address": 0x0E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CTRL_STATUS": {
                    "address": 0x0F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TEMP_MSB": {
                    "address": 0x11,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TEMP_LSB": {
                    "address": 0x12,
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
        return f"DS3231({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = DS3231()
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
