' 74HC595寄存器定义
' 生成自: TI/NXP/GPIO/74HC595
' 版本: 1.0
' 日期: 2026-05-06
' 作者: VML Team
' 描述: 74HC595 8-bit Shift Register (SPI-compatible, serial-in parallel-out, daisy-chainable)

' CPU架构: GPIO
' 位宽: 8位
' 时钟频率: 10000000 Hz

' 外设定义
' 74HC595 8-bit Shift Register (2V-6V, DIP-16)
CONST _74HC595_BASE = 0x00
CONST _74HC595_DATA = 0x00
CONST _74HC595_LATCH = 0x01
CONST _74HC595_CHAIN_COUNT = 0x02
CONST _74HC595_OE = 0x03
CONST _74HC595_CLEAR = 0x04

' 设备初始化子程序
SUB _74hc595_init()
    ' 初始化代码
END SUB

' 常用函数
FUNCTION read_register(addr AS INTEGER) AS INTEGER
    ' 读取寄存器值
    RETURN PEEK(addr)
END FUNCTION

SUB write_register(addr AS INTEGER, value AS INTEGER)
    ' 写入寄存器值
    POKE addr, value
END SUB
