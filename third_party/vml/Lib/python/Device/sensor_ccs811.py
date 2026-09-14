"""
CCS811设备定义 - Python模块
生成自: AMS/ScioSense/Sensor/CCS811
版本: 1.0
日期: 2026-05-06
作者: VML Team
描述: CCS811 VOC/eCO2 Air Quality Sensor (I2C, 400-8192ppm CO2, 0-1187ppb TVOC)
CPU架构: Sensor
位宽: 16位
时钟频率: 400000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class CCS811:
    """CCS811设备类"""

    # 设备信息
    DEVICE_NAME = "CCS811"
    MANUFACTURER = "AMS/ScioSense"
    FAMILY = "Sensor"
    VERSION = "1.0"
    ARCHITECTURE = "Sensor"
    BITS = 16
    CLOCK_FREQUENCY = 400000

    # 外设定义
    # CCS811 Air Quality Sensor (0x5A/0x5B, 1.8V-3.6V)
    CCS811_BASE = 0x5A
    CCS811_STATUS_ADDR = 0x00
    CCS811_MEAS_MODE_ADDR = 0x01
    CCS811_ALG_RESULT_ADDR = 0x02
    CCS811_ECO2_ADDR = 0x02
    CCS811_TVOC_ADDR = 0x04
    CCS811_RAW_DATA_ADDR = 0x06
    CCS811_BASELINE_ADDR = 0x0B
    CCS811_HW_ID_ADDR = 0x20
    CCS811_ERROR_ID_ADDR = 0xE0
    CCS811_APP_START_ADDR = 0xF4
    CCS811_SW_RESET_ADDR = 0xFF

    # 中断向量定义
    INT_INT = 0  # Data ready / interrupt pin

    # 引脚定义
    PIN_WAKE = 1  # Wake pin (active low)

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
        self._peripherals["CCS811"] = {
            "base": 0x5A,
            "type": "I2C",
            "description": "CCS811 Air Quality Sensor (0x5A/0x5B, 1.8V-3.6V)",
            "registers": {
                "STATUS": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "MEAS_MODE": {
                    "address": 0x01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ALG_RESULT": {
                    "address": 0x02,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ECO2": {
                    "address": 0x02,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "TVOC": {
                    "address": 0x04,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "RAW_DATA": {
                    "address": 0x06,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "BASELINE": {
                    "address": 0x0B,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "HW_ID": {
                    "address": 0x20,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ERROR_ID": {
                    "address": 0xE0,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "APP_START": {
                    "address": 0xF4,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SW_RESET": {
                    "address": 0xFF,
                    "size": 4,
                    "type": "uint32",
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
        return f"CCS811({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = CCS811()
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
