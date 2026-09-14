"""
VL53L0X设备定义 - Python模块
生成自: STMicroelectronics/Sensor/VL53L0X
版本: 1.0
日期: 2026-05-06
作者: VML Team
描述: VL53L0X ToF Laser Distance Sensor (I2C, 2cm-200cm, 940nm VCSEL)
CPU架构: Sensor
位宽: 16位
时钟频率: 400000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class VL53L0X:
    """VL53L0X设备类"""

    # 设备信息
    DEVICE_NAME = "VL53L0X"
    MANUFACTURER = "STMicroelectronics"
    FAMILY = "Sensor"
    VERSION = "1.0"
    ARCHITECTURE = "Sensor"
    BITS = 16
    CLOCK_FREQUENCY = 400000

    # 外设定义
    # VL53L0X ToF Distance Sensor (0x29, 2.6V-3.5V)
    VL53L0X_BASE = 0x29
    VL53L0X_DISTANCE_ADDR = 0x00
    VL53L0X_SIGNAL_RATE_ADDR = 0x02
    VL53L0X_AMBIENT_RATE_ADDR = 0x04
    VL53L0X_SPAD_COUNT_ADDR = 0x06
    VL53L0X_RANGE_STATUS_ADDR = 0x08
    VL53L0X_TIMING_BUDGET_ADDR = 0x09
    VL53L0X_INTER_MEAS_ADDR = 0x0D
    VL53L0X_MODE_ADDR = 0x0E

    # 引脚定义
    PIN_XSHUT = 1  # Shutdown pin (active low)
    PIN_INT = 2  # Interrupt (open-drain)

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
        self._peripherals["VL53L0X"] = {
            "base": 0x29,
            "type": "I2C",
            "description": "VL53L0X ToF Distance Sensor (0x29, 2.6V-3.5V)",
            "registers": {
                "DISTANCE": {
                    "address": 0x00,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "SIGNAL_RATE": {
                    "address": 0x02,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "AMBIENT_RATE": {
                    "address": 0x04,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "SPAD_COUNT": {
                    "address": 0x06,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "RANGE_STATUS": {
                    "address": 0x08,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TIMING_BUDGET": {
                    "address": 0x09,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTER_MEAS": {
                    "address": 0x0D,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MODE": {
                    "address": 0x0E,
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
        return f"VL53L0X({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = VL53L0X()
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
