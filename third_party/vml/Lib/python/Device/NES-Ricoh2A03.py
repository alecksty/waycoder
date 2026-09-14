"""
Ricoh-2A03设备定义 - Python模块
生成自: Ricoh/MOS-6502/Ricoh-2A03
版本: 1.0
日期: 2026-04-16
作者: VML Team
描述: NES (Famicom) main processor - 8-bit MOS 6502 variant with audio/video support
CPU架构: MOS-6502
位宽: 8位
时钟频率: 10765930 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class Ricoh_2A03:
    """Ricoh-2A03设备类"""

    # 设备信息
    DEVICE_NAME = "Ricoh-2A03"
    MANUFACTURER = "Ricoh"
    FAMILY = "MOS-6502"
    VERSION = "1.0"
    ARCHITECTURE = "MOS-6502"
    BITS = 8
    CLOCK_FREQUENCY = 10765930

    # 寄存器地址定义
    A_ADDR = 0x00  # Accumulator
    X_ADDR = 0x01  # X Index
    Y_ADDR = 0x02  # Y Index
    SP_ADDR = 0x03  # Stack Pointer
    PC_ADDR = 0x04  # Program Counter (16-bit)
    P_ADDR = 0x06  # Processor Status
    P_C_BIT = 0  # Carry
    P_Z_BIT = 1  # Zero
    P_I_BIT = 2  # Interrupt Disable
    P_D_BIT = 3  # Decimal Mode
    P_B_BIT = 4  # Break
    P_U_BIT = 5  # Unused
    P_V_BIT = 6  # Overflow
    P_N_BIT = 7  # Negative

    # 内存段定义
    CPU_RAM_START = 0x0000
    CPU_RAM_END = 0x07FF
    CPU_RAM_SIZE = 2048  # CPU 2KB RAM (mirrored)
    PPU_REGISTERS_START = 0x2000
    PPU_REGISTERS_END = 0x3FFF
    PPU_REGISTERS_SIZE = 8192  # PPU Registers (mirrored every 8 bytes)
    APU_REGISTERS_START = 0x4000
    APU_REGISTERS_END = 0x401F
    APU_REGISTERS_SIZE = 32  # APU and I/O Registers
    EXPANSION_START = 0x4020
    EXPANSION_END = 0x5FFF
    EXPANSION_SIZE = 8160  # Expansion ROM
    SRAM_START = 0x6000
    SRAM_END = 0x7FFF
    SRAM_SIZE = 8192  # Save RAM
    PRG_ROM_LOW_START = 0x8000
    PRG_ROM_LOW_END = 0xBFFF
    PRG_ROM_LOW_SIZE = 16384  # PRG ROM Lower Bank (16KB)
    PRG_ROM_HIGH_START = 0xC000
    PRG_ROM_HIGH_END = 0xFFFF
    PRG_ROM_HIGH_SIZE = 16384  # PRG ROM Higher Bank (16KB)

    # 外设定义
    # Picture Processing Unit
    PPU_BASE = 0x2000
    PPU_PPUCTRL_ADDR = 0x2000
    PPU_PPUMASK_ADDR = 0x2001
    PPU_PPUSTATUS_ADDR = 0x2002
    PPU_OAMADDR_ADDR = 0x2003
    PPU_OAMDATA_ADDR = 0x2004
    PPU_PPUSCROLL_ADDR = 0x2005
    PPU_PPUADDR_ADDR = 0x2006
    PPU_PPUDATA_ADDR = 0x2007
    # Audio Processing Unit
    APU_BASE = 0x4000
    APU_PULSE1_VOL_ADDR = 0x4000
    APU_PULSE1_SWEEP_ADDR = 0x4001
    APU_PULSE1_LO_ADDR = 0x4002
    APU_PULSE1_HI_ADDR = 0x4003
    APU_PULSE2_VOL_ADDR = 0x4004
    APU_PULSE2_SWEEP_ADDR = 0x4005
    APU_PULSE2_LO_ADDR = 0x4006
    APU_PULSE2_HI_ADDR = 0x4007
    APU_TRIANGLE_ADDR = 0x4008
    APU_TRIANGLE_HI_ADDR = 0x400B
    APU_NOISE_VOL_ADDR = 0x400C
    APU_NOISE_HI_ADDR = 0x400E
    APU_NOISE_LENGTH_ADDR = 0x400F
    APU_DMC_RATE_ADDR = 0x4010
    APU_DMC_RAW_ADDR = 0x4011
    APU_DMC_START_ADDR = 0x4012
    APU_DMC_LENGTH_ADDR = 0x4013
    APU_OAMDMA_ADDR = 0x4014
    APU_SNDCHN_ADDR = 0x4015
    APU_JOY1_ADDR = 0x4016
    APU_JOY2_ADDR = 0x4017
    # Controller Port 1
    INPUT1_BASE = 0x4016
    INPUT1_JOYPAD1_ADDR = 0x4016
    # Controller Port 2
    INPUT2_BASE = 0x4017
    INPUT2_JOYPAD2_ADDR = 0x4017

    # 中断向量定义
    INT_RESET = 0  # Reset
    INT_NMI = 1  # Non-Maskable Interrupt (VBlank)
    INT_IRQ = 2  # IRQ / BRK

    def __init__(self, memory_base: int = 0):
        """初始化设备"""
        self.memory_base = memory_base
        self._registers = {}
        self._peripherals = {}
        self._initialize_registers()
        self._initialize_peripherals()

    def _initialize_registers(self):
        """初始化寄存器""""
        self._registers["A"] = {
            "address": 0x00,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Accumulator",
            "value": 0
        }
        self._registers["X"] = {
            "address": 0x01,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "X Index",
            "value": 0
        }
        self._registers["Y"] = {
            "address": 0x02,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Y Index",
            "value": 0
        }
        self._registers["SP"] = {
            "address": 0x03,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Stack Pointer",
            "value": 0
        }
        self._registers["PC"] = {
            "address": 0x04,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Program Counter (16-bit)",
            "value": 0
        }
        self._registers["P"] = {
            "address": 0x06,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Processor Status",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["PPU"] = {
            "base": 0x2000,
            "type": "video",
            "description": "Picture Processing Unit",
            "registers": {
                "PPUCTRL": {
                    "address": 0x2000,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PPUMASK": {
                    "address": 0x2001,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PPUSTATUS": {
                    "address": 0x2002,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "OAMADDR": {
                    "address": 0x2003,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "OAMDATA": {
                    "address": 0x2004,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PPUSCROLL": {
                    "address": 0x2005,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PPUADDR": {
                    "address": 0x2006,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PPUDATA": {
                    "address": 0x2007,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["APU"] = {
            "base": 0x4000,
            "type": "audio",
            "description": "Audio Processing Unit",
            "registers": {
                "PULSE1_VOL": {
                    "address": 0x4000,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PULSE1_SWEEP": {
                    "address": 0x4001,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PULSE1_LO": {
                    "address": 0x4002,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PULSE1_HI": {
                    "address": 0x4003,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PULSE2_VOL": {
                    "address": 0x4004,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PULSE2_SWEEP": {
                    "address": 0x4005,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PULSE2_LO": {
                    "address": 0x4006,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PULSE2_HI": {
                    "address": 0x4007,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TRIANGLE": {
                    "address": 0x4008,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TRIANGLE_HI": {
                    "address": 0x400B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "NOISE_VOL": {
                    "address": 0x400C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "NOISE_HI": {
                    "address": 0x400E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "NOISE_LENGTH": {
                    "address": 0x400F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DMC_RATE": {
                    "address": 0x4010,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DMC_RAW": {
                    "address": 0x4011,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DMC_START": {
                    "address": 0x4012,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DMC_LENGTH": {
                    "address": 0x4013,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "OAMDMA": {
                    "address": 0x4014,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SNDCHN": {
                    "address": 0x4015,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "JOY1": {
                    "address": 0x4016,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "JOY2": {
                    "address": 0x4017,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["INPUT1"] = {
            "base": 0x4016,
            "type": "input",
            "description": "Controller Port 1",
            "registers": {
                "JOYPAD1": {
                    "address": 0x4016,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["INPUT2"] = {
            "base": 0x4017,
            "type": "input",
            "description": "Controller Port 2",
            "registers": {
                "JOYPAD2": {
                    "address": 0x4017,
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
        return f"Ricoh_2A03({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = Ricoh_2A03()
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
