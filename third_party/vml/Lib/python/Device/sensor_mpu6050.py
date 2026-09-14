"""
MPU6050设备定义 - Python模块
生成自: InvenSense/TDK/Sensor/MPU6050
版本: 1.0
日期: 2026-05-06
作者: VML Team
描述: 6-Axis MEMS Accelerometer and Gyroscope (I2C)
CPU架构: Sensor
位宽: 8位
时钟频率: 400000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class MPU6050:
    """MPU6050设备类"""

    # 设备信息
    DEVICE_NAME = "MPU6050"
    MANUFACTURER = "InvenSense/TDK"
    FAMILY = "Sensor"
    VERSION = "1.0"
    ARCHITECTURE = "Sensor"
    BITS = 8
    CLOCK_FREQUENCY = 400000

    # 内存段定义
    PACKAGE_START = 0x00
    PACKAGE_END = 0x00
    PACKAGE_SIZE = 24  # QFN-24 (4x4x0.9mm)

    # 外设定义
    # MPU6050 IMU (0x68/0x69, 2.375V-3.46V)
    MPU6050_BASE = 0x68
    MPU6050_SMPLRT_DIV_ADDR = 0x19
    MPU6050_CONFIG_ADDR = 0x1A
    MPU6050_CONFIG_DLPF_CFG_BIT = 0  # Digital low-pass filter configuration
    MPU6050_GYRO_CONFIG_ADDR = 0x1B
    MPU6050_GYRO_CONFIG_FS_SEL_BIT = 3  # Gyro full scale: 0=±250, 1=±500, 2=±1000, 3=±2000 °/s
    MPU6050_ACCEL_CONFIG_ADDR = 0x1C
    MPU6050_ACCEL_CONFIG_AFS_SEL_BIT = 3  # Accel full scale: 0=±2g, 1=±4g, 2=±8g, 3=±16g
    MPU6050_ACCEL_XOUT_H_ADDR = 0x3B
    MPU6050_ACCEL_XOUT_L_ADDR = 0x3C
    MPU6050_ACCEL_YOUT_H_ADDR = 0x3D
    MPU6050_ACCEL_YOUT_L_ADDR = 0x3E
    MPU6050_ACCEL_ZOUT_H_ADDR = 0x3F
    MPU6050_ACCEL_ZOUT_L_ADDR = 0x40
    MPU6050_TEMP_OUT_H_ADDR = 0x41
    MPU6050_TEMP_OUT_L_ADDR = 0x42
    MPU6050_GYRO_XOUT_H_ADDR = 0x43
    MPU6050_GYRO_XOUT_L_ADDR = 0x44
    MPU6050_GYRO_YOUT_H_ADDR = 0x45
    MPU6050_GYRO_YOUT_L_ADDR = 0x46
    MPU6050_GYRO_ZOUT_H_ADDR = 0x47
    MPU6050_GYRO_ZOUT_L_ADDR = 0x48
    MPU6050_PWR_MGMT_1_ADDR = 0x6B
    MPU6050_PWR_MGMT_1_DEVICE_RESET_BIT = 7  # 1=Reset all internal registers
    MPU6050_PWR_MGMT_1_SLEEP_BIT = 6  # 1=Sleep mode
    MPU6050_PWR_MGMT_1_CYCLE_BIT = 5  # 1=Cycle mode
    MPU6050_PWR_MGMT_1_CLKSEL_BIT = 0  # Clock source select
    MPU6050_WHO_AM_I_ADDR = 0x75

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
        self._peripherals["MPU6050"] = {
            "base": 0x68,
            "type": "I2C",
            "description": "MPU6050 IMU (0x68/0x69, 2.375V-3.46V)",
            "registers": {
                "SMPLRT_DIV": {
                    "address": 0x19,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CONFIG": {
                    "address": 0x1A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GYRO_CONFIG": {
                    "address": 0x1B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ACCEL_CONFIG": {
                    "address": 0x1C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ACCEL_XOUT_H": {
                    "address": 0x3B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ACCEL_XOUT_L": {
                    "address": 0x3C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ACCEL_YOUT_H": {
                    "address": 0x3D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ACCEL_YOUT_L": {
                    "address": 0x3E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ACCEL_ZOUT_H": {
                    "address": 0x3F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ACCEL_ZOUT_L": {
                    "address": 0x40,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TEMP_OUT_H": {
                    "address": 0x41,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TEMP_OUT_L": {
                    "address": 0x42,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GYRO_XOUT_H": {
                    "address": 0x43,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GYRO_XOUT_L": {
                    "address": 0x44,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GYRO_YOUT_H": {
                    "address": 0x45,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GYRO_YOUT_L": {
                    "address": 0x46,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GYRO_ZOUT_H": {
                    "address": 0x47,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GYRO_ZOUT_L": {
                    "address": 0x48,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PWR_MGMT_1": {
                    "address": 0x6B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "WHO_AM_I": {
                    "address": 0x75,
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
        return f"MPU6050({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = MPU6050()
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
