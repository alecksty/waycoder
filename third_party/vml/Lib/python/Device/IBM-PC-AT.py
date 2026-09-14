"""
IBM PC/AT设备定义 - Python模块
生成自: IBM/IBM PC/IBM PC/AT
版本: 
日期: 
作者: 
描述: IBM Personal Computer/Advanced Technology (Model 5170)
CPU架构: x86-16
位宽: 0位
时钟频率: 0 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class IBM PC/AT:
    """IBM PC/AT设备类"""

    # 设备信息
    DEVICE_NAME = "IBM PC/AT"
    MANUFACTURER = "IBM"
    FAMILY = "IBM PC"
    VERSION = ""
    ARCHITECTURE = "x86-16"
    BITS = 0
    CLOCK_FREQUENCY = 0

    # 外设定义
    # Programmable Interrupt Controller
    _8259A_BASE = 
    _8259A_ICW1_ADDR = 0x20
    _8259A_ICW2_ADDR = 0x21
    _8259A_ICW3_ADDR = 0x21
    _8259A_ICW4_ADDR = 0x21
    _8259A_OCW1_ADDR = 0x21
    _8259A_OCW2_ADDR = 0x20
    _8259A_OCW3_ADDR = 0x20
    # Programmable Interval Timer
    _8253_BASE = 
    _8253_COUNTER0_ADDR = 0x40
    _8253_COUNTER1_ADDR = 0x41
    _8253_COUNTER2_ADDR = 0x42
    _8253_CONTROL_ADDR = 0x43
    # Direct Memory Access Controller
    _8237_BASE = 
    _8237_CHANNEL0_ADDR = 0x00
    _8237_CHANNEL1_ADDR = 0x02
    _8237_CHANNEL2_ADDR = 0x04
    _8237_CHANNEL3_ADDR = 0x06
    _8237_STATUS_ADDR = 0x08
    _8237_COMMAND_ADDR = 0x08
    _8237_REQUEST_ADDR = 0x09
    _8237_MASK_ADDR = 0x0A
    _8237_MODE_ADDR = 0x0B
    # Keyboard Controller
    _8042_BASE = 
    _8042_DATA_ADDR = 0x60
    _8042_STATUS_ADDR = 0x64
    # Real-Time Clock with CMOS RAM
    CMOS_BASE = 
    CMOS_ADDRESS_ADDR = 0x70
    CMOS_DATA_ADDR = 0x71
    # Floppy Disk Controller
    FDC_BASE = 
    FDC_SRA_ADDR = 0x3F2
    FDC_MSR_ADDR = 0x3F4
    FDC_DATA_ADDR = 0x3F5
    FDC_DIR_ADDR = 0x3F7
    FDC_CCR_ADDR = 0x3F7
    # Hard Disk Controller (ST-506/412)
    HDC_BASE = 
    HDC_DATA_ADDR = 0x1F0
    HDC_ERROR_ADDR = 0x1F1
    HDC_SECTORCOUNT_ADDR = 0x1F2
    HDC_SECTORNUMBER_ADDR = 0x1F3
    HDC_CYLINDERLOW_ADDR = 0x1F4
    HDC_CYLINDERHIGH_ADDR = 0x1F5
    HDC_DRIVEHEAD_ADDR = 0x1F6
    HDC_STATUS_ADDR = 0x1F7
    HDC_COMMAND_ADDR = 0x1F7
    # Color Graphics Adapter
    CGA_BASE = 
    CGA_CRTC_INDEX_ADDR = 0x3D4
    CGA_CRTC_DATA_ADDR = 0x3D5
    CGA_MODECONTROL_ADDR = 0x3D8
    CGA_COLORSELECT_ADDR = 0x3D9
    CGA_STATUS_ADDR = 0x3DA
    # Enhanced Graphics Adapter
    EGA_BASE = 
    EGA_CRTC_INDEX_ADDR = 0x3D4
    EGA_CRTC_DATA_ADDR = 0x3D5
    EGA_FEATURECONTROL_ADDR = 0x3DA
    EGA_GRAPHICS1POS_ADDR = 0x3CC
    EGA_GRAPHICS2POS_ADDR = 0x3CA
    EGA_SEQUENCERINDEX_ADDR = 0x3C4
    EGA_SEQUENCERDATA_ADDR = 0x3C5
    EGA_GRAPHICSINDEX_ADDR = 0x3CE
    EGA_GRAPHICSDATA_ADDR = 0x3CF
    EGA_ATTRIBUTEINDEX_ADDR = 0x3C0
    EGA_ATTRIBUTEDATA_ADDR = 0x3C1
    # Video Graphics Array
    VGA_BASE = 
    VGA_CRTC_INDEX_ADDR = 0x3D4
    VGA_CRTC_DATA_ADDR = 0x3D5
    VGA_INPUTSTATUS1_ADDR = 0x3DA
    VGA_FEATURECONTROL_ADDR = 0x3DA
    VGA_MISCOUTPUT_ADDR = 0x3C2
    VGA_SEQUENCERINDEX_ADDR = 0x3C4
    VGA_SEQUENCERDATA_ADDR = 0x3C5
    VGA_GRAPHICSINDEX_ADDR = 0x3CE
    VGA_GRAPHICSDATA_ADDR = 0x3CF
    VGA_ATTRIBUTEINDEX_ADDR = 0x3C0
    VGA_ATTRIBUTEDATA_ADDR = 0x3C1
    VGA_DACMASK_ADDR = 0x3C6
    VGA_DACREADINDEX_ADDR = 0x3C7
    VGA_DACWRITEINDEX_ADDR = 0x3C8
    VGA_DACDATA_ADDR = 0x3C9
    # Game Port
    GAMEPORT_BASE = 
    GAMEPORT_DATA_ADDR = 0x201
    # Parallel Printer Port
    PARALLELPORT_BASE = 
    PARALLELPORT_DATA_ADDR = 0x378
    PARALLELPORT_STATUS_ADDR = 0x379
    PARALLELPORT_CONTROL_ADDR = 0x37A
    # Serial Communications Port
    SERIALPORT_BASE = 
    SERIALPORT_DATA_ADDR = 0x3F8
    SERIALPORT_IER_ADDR = 0x3F9
    SERIALPORT_IIR_ADDR = 0x3FA
    SERIALPORT_LCR_ADDR = 0x3FB
    SERIALPORT_MCR_ADDR = 0x3FC
    SERIALPORT_LSR_ADDR = 0x3FD
    SERIALPORT_MSR_ADDR = 0x3FE
    SERIALPORT_SCR_ADDR = 0x3FF
    # PC Speaker
    SPEAKER_BASE = 
    SPEAKER_CONTROL_ADDR = 0x61

    # 中断向量定义
    INT_DIVIDE_ERROR = 0  # Division by zero
    INT_SINGLE_STEP = 1  # Debug single step
    INT_NMI = 2  # Non-maskable interrupt
    INT_BREAKPOINT = 3  # INT 3 instruction
    INT_OVERFLOW = 4  # INTO instruction
    INT_PRINT_SCREEN = 5  # Print screen key
    INT_IRQ0 = 8  # Timer interrupt
    INT_IRQ1 = 9  # Keyboard interrupt
    INT_IRQ2 = 10  # Cascade to IRQ8-15
    INT_IRQ3 = 11  # COM2 interrupt
    INT_IRQ4 = 12  # COM1 interrupt
    INT_IRQ5 = 13  # LPT2 interrupt
    INT_IRQ6 = 14  # Floppy disk interrupt
    INT_IRQ7 = 15  # LPT1 interrupt
    INT_IRQ8 = 112  # Real-time clock interrupt
    INT_IRQ9 = 113  # Redirected IRQ2
    INT_IRQ10 = 114  # Reserved
    INT_IRQ11 = 115  # Reserved
    INT_IRQ12 = 116  # PS/2 mouse interrupt
    INT_IRQ13 = 117  # Coprocessor interrupt
    INT_IRQ14 = 118  # Primary IDE interrupt
    INT_IRQ15 = 119  # Secondary IDE interrupt
    INT_VIDEO_SERVICES = 16  # Video BIOS services
    INT_DISK_SERVICES = 19  # Disk BIOS services
    INT_DOS_SERVICES = 21  # DOS function calls

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
        self._peripherals["8259A"] = {
            "base": ,
            "type": "InterruptController",
            "description": "Programmable Interrupt Controller",
            "registers": {
                "ICW1": {
                    "address": 0x20,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "ICW2": {
                    "address": 0x21,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "ICW3": {
                    "address": 0x21,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "ICW4": {
                    "address": 0x21,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "OCW1": {
                    "address": 0x21,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "OCW2": {
                    "address": 0x20,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "OCW3": {
                    "address": 0x20,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
            }
        }
        self._peripherals["8253"] = {
            "base": ,
            "type": "Timer",
            "description": "Programmable Interval Timer",
            "registers": {
                "Counter0": {
                    "address": 0x40,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "Counter1": {
                    "address": 0x41,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "Counter2": {
                    "address": 0x42,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "Control": {
                    "address": 0x43,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
            }
        }
        self._peripherals["8237"] = {
            "base": ,
            "type": "DMA",
            "description": "Direct Memory Access Controller",
            "registers": {
                "Channel0": {
                    "address": 0x00,
                    "size": 16,
                    "type": "bytes[16]",
                    "value": 0
                },
                "Channel1": {
                    "address": 0x02,
                    "size": 16,
                    "type": "bytes[16]",
                    "value": 0
                },
                "Channel2": {
                    "address": 0x04,
                    "size": 16,
                    "type": "bytes[16]",
                    "value": 0
                },
                "Channel3": {
                    "address": 0x06,
                    "size": 16,
                    "type": "bytes[16]",
                    "value": 0
                },
                "Status": {
                    "address": 0x08,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "Command": {
                    "address": 0x08,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "Request": {
                    "address": 0x09,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "Mask": {
                    "address": 0x0A,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "Mode": {
                    "address": 0x0B,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
            }
        }
        self._peripherals["8042"] = {
            "base": ,
            "type": "KeyboardController",
            "description": "Keyboard Controller",
            "registers": {
                "Data": {
                    "address": 0x60,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "Status": {
                    "address": 0x64,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
            }
        }
        self._peripherals["CMOS"] = {
            "base": ,
            "type": "RTC",
            "description": "Real-Time Clock with CMOS RAM",
            "registers": {
                "Address": {
                    "address": 0x70,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "Data": {
                    "address": 0x71,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
            }
        }
        self._peripherals["FDC"] = {
            "base": ,
            "type": "Floppy",
            "description": "Floppy Disk Controller",
            "registers": {
                "SRA": {
                    "address": 0x3F2,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "MSR": {
                    "address": 0x3F4,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "DATA": {
                    "address": 0x3F5,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "DIR": {
                    "address": 0x3F7,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "CCR": {
                    "address": 0x3F7,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
            }
        }
        self._peripherals["HDC"] = {
            "base": ,
            "type": "HardDisk",
            "description": "Hard Disk Controller (ST-506/412)",
            "registers": {
                "Data": {
                    "address": 0x1F0,
                    "size": 16,
                    "type": "bytes[16]",
                    "value": 0
                },
                "Error": {
                    "address": 0x1F1,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "SectorCount": {
                    "address": 0x1F2,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "SectorNumber": {
                    "address": 0x1F3,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "CylinderLow": {
                    "address": 0x1F4,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "CylinderHigh": {
                    "address": 0x1F5,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "DriveHead": {
                    "address": 0x1F6,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "Status": {
                    "address": 0x1F7,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "Command": {
                    "address": 0x1F7,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
            }
        }
        self._peripherals["CGA"] = {
            "base": ,
            "type": "Video",
            "description": "Color Graphics Adapter",
            "registers": {
                "CRTC_Index": {
                    "address": 0x3D4,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "CRTC_Data": {
                    "address": 0x3D5,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "ModeControl": {
                    "address": 0x3D8,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "ColorSelect": {
                    "address": 0x3D9,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "Status": {
                    "address": 0x3DA,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
            }
        }
        self._peripherals["EGA"] = {
            "base": ,
            "type": "Video",
            "description": "Enhanced Graphics Adapter",
            "registers": {
                "CRTC_Index": {
                    "address": 0x3D4,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "CRTC_Data": {
                    "address": 0x3D5,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "FeatureControl": {
                    "address": 0x3DA,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "Graphics1Pos": {
                    "address": 0x3CC,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "Graphics2Pos": {
                    "address": 0x3CA,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "SequencerIndex": {
                    "address": 0x3C4,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "SequencerData": {
                    "address": 0x3C5,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "GraphicsIndex": {
                    "address": 0x3CE,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "GraphicsData": {
                    "address": 0x3CF,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "AttributeIndex": {
                    "address": 0x3C0,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "AttributeData": {
                    "address": 0x3C1,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
            }
        }
        self._peripherals["VGA"] = {
            "base": ,
            "type": "Video",
            "description": "Video Graphics Array",
            "registers": {
                "CRTC_Index": {
                    "address": 0x3D4,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "CRTC_Data": {
                    "address": 0x3D5,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "InputStatus1": {
                    "address": 0x3DA,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "FeatureControl": {
                    "address": 0x3DA,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "MiscOutput": {
                    "address": 0x3C2,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "SequencerIndex": {
                    "address": 0x3C4,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "SequencerData": {
                    "address": 0x3C5,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "GraphicsIndex": {
                    "address": 0x3CE,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "GraphicsData": {
                    "address": 0x3CF,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "AttributeIndex": {
                    "address": 0x3C0,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "AttributeData": {
                    "address": 0x3C1,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "DACMask": {
                    "address": 0x3C6,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "DACReadIndex": {
                    "address": 0x3C7,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "DACWriteIndex": {
                    "address": 0x3C8,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "DACData": {
                    "address": 0x3C9,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
            }
        }
        self._peripherals["GamePort"] = {
            "base": ,
            "type": "Game",
            "description": "Game Port",
            "registers": {
                "Data": {
                    "address": 0x201,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
            }
        }
        self._peripherals["ParallelPort"] = {
            "base": ,
            "type": "Parallel",
            "description": "Parallel Printer Port",
            "registers": {
                "Data": {
                    "address": 0x378,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "Status": {
                    "address": 0x379,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "Control": {
                    "address": 0x37A,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
            }
        }
        self._peripherals["SerialPort"] = {
            "base": ,
            "type": "Serial",
            "description": "Serial Communications Port",
            "registers": {
                "Data": {
                    "address": 0x3F8,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "IER": {
                    "address": 0x3F9,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "IIR": {
                    "address": 0x3FA,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "LCR": {
                    "address": 0x3FB,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "MCR": {
                    "address": 0x3FC,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "LSR": {
                    "address": 0x3FD,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "MSR": {
                    "address": 0x3FE,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
                "SCR": {
                    "address": 0x3FF,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
            }
        }
        self._peripherals["Speaker"] = {
            "base": ,
            "type": "Audio",
            "description": "PC Speaker",
            "registers": {
                "Control": {
                    "address": 0x61,
                    "size": 8,
                    "type": "uint64",
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
        return f"IBM PC/AT({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = IBM PC/AT()
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
