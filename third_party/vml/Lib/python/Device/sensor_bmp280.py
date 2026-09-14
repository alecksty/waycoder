"""
BMP280设备定义 - Python模块
生成自: Bosch/Sensor/BMP280
版本: 1.0
日期: 2026-05-06
作者: VML Team
描述: Digital Barometric Pressure and Temperature Sensor (I2C/SPI)
CPU架构: Sensor
位宽: 8位
时钟频率: 3400000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class BMP280:
    """BMP280设备类"""

    # 设备信息
    DEVICE_NAME = "BMP280"
    MANUFACTURER = "Bosch"
    FAMILY = "Sensor"
    VERSION = "1.0"
    ARCHITECTURE = "Sensor"
    BITS = 8
    CLOCK_FREQUENCY = 3400000

    # 内存段定义
    PACKAGE_START = 0x00
    PACKAGE_END = 0x00
    PACKAGE_SIZE = 8  # LGA-8 (2.0x2.5x0.95mm)

    # 外设定义
    # BMP280 I2C Sensor (0x76/0x77, 1.71V-3.6V)
    BMP280_BASE = 0x76
    BMP280_TEMP_XLSB_ADDR = 0xFC
    BMP280_TEMP_LSB_ADDR = 0xFB
    BMP280_TEMP_MSB_ADDR = 0xFA
    BMP280_PRESS_XLSB_ADDR = 0xF9
    BMP280_PRESS_LSB_ADDR = 0xF8
    BMP280_PRESS_MSB_ADDR = 0xF7
    BMP280_CONFIG_ADDR = 0xF5
    BMP280_CONFIG_T_SB_BIT = 5  # Standby time in normal mode
    BMP280_CONFIG_FILTER_BIT = 2  # Filter coefficient
    BMP280_CONFIG_SPI3W_EN_BIT = 0  # Enable 3-wire SPI
    BMP280_CTRL_MEAS_ADDR = 0xF4
    BMP280_CTRL_MEAS_MODE_BIT = 0  # 0=sleep, 1/2=forced, 3=normal
    BMP280_CTRL_MEAS_OSRS_P_BIT = 2  # Pressure oversampling
    BMP280_CTRL_MEAS_OSRS_T_BIT = 5  # Temperature oversampling
    BMP280_STATUS_ADDR = 0xF3
    BMP280_STATUS_IM_UPDATE_BIT = 0  # 1=Image register update in progress
    BMP280_STATUS_MEASURING_BIT = 3  # 1=Conversion is running
    BMP280_CHIP_ID_ADDR = 0xD0
    BMP280_RESET_ADDR = 0xE0

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
        self._peripherals["BMP280"] = {
            "base": 0x76,
            "type": "I2C",
            "description": "BMP280 I2C Sensor (0x76/0x77, 1.71V-3.6V)",
            "registers": {
                "TEMP_XLSB": {
                    "address": 0xFC,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TEMP_LSB": {
                    "address": 0xFB,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TEMP_MSB": {
                    "address": 0xFA,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PRESS_XLSB": {
                    "address": 0xF9,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PRESS_LSB": {
                    "address": 0xF8,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PRESS_MSB": {
                    "address": 0xF7,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CONFIG": {
                    "address": 0xF5,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CTRL_MEAS": {
                    "address": 0xF4,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "STATUS": {
                    "address": 0xF3,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CHIP_ID": {
                    "address": 0xD0,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "RESET": {
                    "address": 0xE0,
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
        return f"BMP280({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = BMP280()
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
