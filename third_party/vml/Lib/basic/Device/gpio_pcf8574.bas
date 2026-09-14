' PCF8574寄存器定义
' 生成自: NXP/TI/GPIO/PCF8574
' 版本: 1.0
' 日期: 2026-05-06
' 作者: VML Team
' 描述: PCF8574 8-bit I2C GPIO Expander (quasi-bidirectional, interrupt)

' CPU架构: GPIO
' 位宽: 8位
' 时钟频率: 100000 Hz

' 外设定义
' PCF8574 8-bit GPIO (0x20-0x27, 2.5V-6V)
CONST PCF8574_BASE = 0x20
CONST PCF8574_INPUT = 0x00
CONST PCF8574_INPUT_P0 = 0  ' Pin P0
CONST PCF8574_INPUT_P1 = 1  ' Pin P1
CONST PCF8574_INPUT_P2 = 2  ' Pin P2
CONST PCF8574_INPUT_P3 = 3  ' Pin P3
CONST PCF8574_INPUT_P4 = 4  ' Pin P4
CONST PCF8574_INPUT_P5 = 5  ' Pin P5
CONST PCF8574_INPUT_P6 = 6  ' Pin P6
CONST PCF8574_INPUT_P7 = 7  ' Pin P7
CONST PCF8574_OUTPUT = 0x01
CONST PCF8574_POLARITY = 0x02

' 中断向量定义
CONST INT_VECTOR = 0  ' Pin change interrupt (open-drain, active low)

' 设备初始化子程序
SUB pcf8574_init()
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
