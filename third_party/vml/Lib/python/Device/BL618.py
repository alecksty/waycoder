"""
BL618设备定义 - Python模块
生成自: Bouffalo Lab/BL6/BL618
版本: 1.0
日期: 2026-04-28
作者: VML Team
描述: 32-bit RISC-V RV32IMAFC WiFi6 + BLE SoC with 4MB Flash, 512KB SRAM, 480MHz
CPU架构: RISC-V
位宽: 32位
时钟频率: 320000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class BL618:
    """BL618设备类"""

    # 设备信息
    DEVICE_NAME = "BL618"
    MANUFACTURER = "Bouffalo Lab"
    FAMILY = "BL6"
    VERSION = "1.0"
    ARCHITECTURE = "RISC-V"
    BITS = 32
    CLOCK_FREQUENCY = 320000000

    # 寄存器地址定义
    X1_ADDR = 0x04  # Return Address
    X2_ADDR = 0x08  # Stack Pointer (SP)
    X3_ADDR = 0x0C  # Global Pointer (GP)
    X8_ADDR = 0x20  # Frame Pointer (FP)
    X10_ADDR = 0x28  # Function Argument (A0)
    X11_ADDR = 0x2C  # Function Argument (A1)
    PC_ADDR = 0x3C  # Program Counter

    # 内存段定义
    FLASH_START = 0x20000000
    FLASH_END = 0x203FFFFF
    FLASH_SIZE = 4194304  # 
    SRAM_HPSYS_START = 0x22000000
    SRAM_HPSYS_END = 0x22003FFF
    SRAM_HPSYS_SIZE = 16384  # 
    SRAM_DTCM_START = 0x22010000
    SRAM_DTCM_END = 0x22017FFF
    SRAM_DTCM_SIZE = 32768  # DTCM
    SRAM_SYS_START = 0x22020000
    SRAM_SYS_END = 0x2208FFFF
    SRAM_SYS_SIZE = 458752  # 
    PERIPHERAL_START = 0x30000000
    PERIPHERAL_END = 0x300FFFFF
    PERIPHERAL_SIZE = 1048576  # 

    # 外设定义
    # Global Control (Clock and Reset)
    GLB_BASE = 0x30000000
    GLB_GLB_CLK_EN_ADDR = 0x10
    GLB_GLB_CLK_EN_GPIO_CLK_EN_BIT = 6  # GPIO clock enable
    GLB_GLB_CLK_EN_UART0_CLK_EN_BIT = 12  # UART0 clock enable
    GLB_GLB_SYS_CLK_CTRL_ADDR = 0x14
    GLB_GLB_PLL_CTRL_ADDR = 0x1C
    # GPIO Port A
    GPIO_P0_BASE = 0x30007000
    GPIO_P0_GPIO_CFG0_ADDR = 0x00
    GPIO_P0_GPIO_CFG1_ADDR = 0x04
    GPIO_P0_GPIO_OE_ADDR = 0x08
    GPIO_P0_GPIO_OUT_ADDR = 0x0C
    GPIO_P0_GPIO_IN_ADDR = 0x10
    GPIO_P0_GPIO_SET_ADDR = 0x14
    GPIO_P0_GPIO_CLR_ADDR = 0x18
    GPIO_P0_GPIO_TOG_ADDR = 0x1C
    # GPIO Port B
    GPIO_P1_BASE = 0x30007200
    GPIO_P1_GPIO_CFG0_ADDR = 0x00
    GPIO_P1_GPIO_CFG1_ADDR = 0x04
    GPIO_P1_GPIO_OE_ADDR = 0x08
    GPIO_P1_GPIO_OUT_ADDR = 0x0C
    GPIO_P1_GPIO_IN_ADDR = 0x10
    GPIO_P1_GPIO_SET_ADDR = 0x14
    GPIO_P1_GPIO_CLR_ADDR = 0x18
    GPIO_P1_GPIO_TOG_ADDR = 0x1C
    # UART 0
    UART0_BASE = 0x30002000
    UART0_UART_CR_ADDR = 0x00
    UART0_UART_BRR_ADDR = 0x04
    UART0_UART_TDR_ADDR = 0x08
    UART0_UART_RDR_ADDR = 0x0C
    UART0_UART_SR_ADDR = 0x10

    # 中断向量定义
    INT_RESET = 1  # 
    INT_MACHINESOFTWARE = 3  # 
    INT_MACHINETIMER = 7  # 
    INT_MACHINEEXTERNAL = 11  # 
    INT_UART0 = 20  # UART0 Interrupt

    def __init__(self, memory_base: int = 0):
        """初始化设备"""
        self.memory_base = memory_base
        self._registers = {}
        self._peripherals = {}
        self._initialize_registers()
        self._initialize_peripherals()

    def _initialize_registers(self):
        """初始化寄存器""""
        self._registers["x1"] = {
            "address": 0x04,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Return Address",
            "value": 0
        }
        self._registers["x2"] = {
            "address": 0x08,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Stack Pointer (SP)",
            "value": 0
        }
        self._registers["x3"] = {
            "address": 0x0C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Global Pointer (GP)",
            "value": 0
        }
        self._registers["x8"] = {
            "address": 0x20,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Frame Pointer (FP)",
            "value": 0
        }
        self._registers["x10"] = {
            "address": 0x28,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Function Argument (A0)",
            "value": 0
        }
        self._registers["x11"] = {
            "address": 0x2C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Function Argument (A1)",
            "value": 0
        }
        self._registers["pc"] = {
            "address": 0x3C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Program Counter",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["GLB"] = {
            "base": 0x30000000,
            "type": "ClockControl",
            "description": "Global Control (Clock and Reset)",
            "registers": {
                "GLB_CLK_EN": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GLB_SYS_CLK_CTRL": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GLB_PLL_CTRL": {
                    "address": 0x1C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIO_P0"] = {
            "base": 0x30007000,
            "type": "GPIO",
            "description": "GPIO Port A",
            "registers": {
                "GPIO_CFG0": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_CFG1": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_OE": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_OUT": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_IN": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_SET": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_CLR": {
                    "address": 0x18,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_TOG": {
                    "address": 0x1C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIO_P1"] = {
            "base": 0x30007200,
            "type": "GPIO",
            "description": "GPIO Port B",
            "registers": {
                "GPIO_CFG0": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_CFG1": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_OE": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_OUT": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_IN": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_SET": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_CLR": {
                    "address": 0x18,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_TOG": {
                    "address": 0x1C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["UART0"] = {
            "base": 0x30002000,
            "type": "UART",
            "description": "UART 0",
            "registers": {
                "UART_CR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UART_BRR": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UART_TDR": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UART_RDR": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UART_SR": {
                    "address": 0x10,
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
        return f"BL618({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = BL618()
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
