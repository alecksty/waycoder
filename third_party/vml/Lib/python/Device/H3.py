"""
Allwinner H3设备定义 - Python模块
生成自: Allwinner/H-Series/Allwinner H3
版本: 1.0
日期: 2026-04-29
作者: VML Team
描述: 32-bit ARM Cortex-A7 Quad-core SoC with 512KB L2 Cache, 1.6GHz, Mali-400 GPU
CPU架构: ARM-Cortex-A7
位宽: 32位
时钟频率: 1200000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class Allwinner H3:
    """Allwinner H3设备类"""

    # 设备信息
    DEVICE_NAME = "Allwinner H3"
    MANUFACTURER = "Allwinner"
    FAMILY = "H-Series"
    VERSION = "1.0"
    ARCHITECTURE = "ARM-Cortex-A7"
    BITS = 32
    CLOCK_FREQUENCY = 1200000000

    # 外设定义
    # UART 0 (debug console)
    UART0_BASE = 0x01C28000
    UART0_RBR_ADDR = 0x00
    UART0_THR_ADDR = 0x00
    UART0_IER_ADDR = 0x04
    UART0_IIR_ADDR = 0x08
    UART0_FCR_ADDR = 0x08
    UART0_LCR_ADDR = 0x0C
    UART0_MCR_ADDR = 0x10
    UART0_LSR_ADDR = 0x14
    UART0_MSR_ADDR = 0x18
    UART0_DLL_ADDR = 0x00
    UART0_DLH_ADDR = 0x04
    # UART 1
    UART1_BASE = 0x01C28400
    UART1_RBR_ADDR = 0x00
    UART1_THR_ADDR = 0x00
    UART1_LSR_ADDR = 0x14
    # GPIO 控制器
    GPIO_BASE = 0x01C20800
    GPIO_PA_CFG0_ADDR = 0x00
    GPIO_PA_CFG1_ADDR = 0x04
    GPIO_PA_DAT_ADDR = 0x10
    GPIO_PA_DRV0_ADDR = 0x14
    GPIO_PA_PUL0_ADDR = 0x1C
    GPIO_PB_CFG0_ADDR = 0x24
    GPIO_PB_DAT_ADDR = 0x34
    GPIO_PC_CFG0_ADDR = 0x48
    GPIO_PC_DAT_ADDR = 0x58
    # AVS 定时器
    TIMER_BASE = 0x01C20C00
    TIMER_CNT0_ADDR = 0x00
    TIMER_CNT1_ADDR = 0x04
    TIMER_CTRL_ADDR = 0x08
    TIMER_INTV_ADDR = 0x0C
    # 时钟控制单元
    CCU_BASE = 0x01C20000
    CCU_PLL1_CFG_ADDR = 0x000
    CCU_PLL3_CFG_ADDR = 0x010
    CCU_CPU_AXI_CFG_ADDR = 0x050
    CCU_AHB1_APB1_CFG_ADDR = 0x054
    CCU_APB2_CFG_ADDR = 0x058
    CCU_BUS_GATE0_ADDR = 0x060
    CCU_BUS_GATE1_ADDR = 0x064
    CCU_BUS_GATE2_ADDR = 0x068

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
        self._peripherals["UART0"] = {
            "base": 0x01C28000,
            "type": "uart",
            "description": "UART 0 (debug console)",
            "registers": {
                "RBR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "THR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IER": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IIR": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FCR": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LCR": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MCR": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LSR": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MSR": {
                    "address": 0x18,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DLL": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DLH": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["UART1"] = {
            "base": 0x01C28400,
            "type": "uart",
            "description": "UART 1",
            "registers": {
                "RBR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "THR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LSR": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIO"] = {
            "base": 0x01C20800,
            "type": "gpio",
            "description": "GPIO 控制器",
            "registers": {
                "PA_CFG0": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PA_CFG1": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PA_DAT": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PA_DRV0": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PA_PUL0": {
                    "address": 0x1C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PB_CFG0": {
                    "address": 0x24,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PB_DAT": {
                    "address": 0x34,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PC_CFG0": {
                    "address": 0x48,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PC_DAT": {
                    "address": 0x58,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMER"] = {
            "base": 0x01C20C00,
            "type": "timer",
            "description": "AVS 定时器",
            "registers": {
                "CNT0": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CNT1": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CTRL": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTV": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["CCU"] = {
            "base": 0x01C20000,
            "type": "clock",
            "description": "时钟控制单元",
            "registers": {
                "PLL1_CFG": {
                    "address": 0x000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PLL3_CFG": {
                    "address": 0x010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CPU_AXI_CFG": {
                    "address": 0x050,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "AHB1_APB1_CFG": {
                    "address": 0x054,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "APB2_CFG": {
                    "address": 0x058,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BUS_GATE0": {
                    "address": 0x060,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BUS_GATE1": {
                    "address": 0x064,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BUS_GATE2": {
                    "address": 0x068,
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
        return f"Allwinner H3({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = Allwinner H3()
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
