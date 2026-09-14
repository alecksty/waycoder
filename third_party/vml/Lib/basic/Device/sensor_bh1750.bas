' BH1750寄存器定义
' 生成自: ROHM/Sensor/BH1750
' 版本: 1.0
' 日期: 2026-05-06
' 作者: VML Team
' 描述: BH1750FVI Digital Ambient Light Sensor (I2C, 1-65535 lux, 16-bit)

' CPU架构: Sensor
' 位宽: 16位
' 时钟频率: 400000 Hz

' 外设定义
' BH1750 Light Sensor (0x23/0x5C, 2.4V-3.6V)
CONST BH1750_BASE = 0x23
CONST BH1750_LUX = 0x00
CONST BH1750_MODE = 0x01
CONST BH1750_MODE_CONT_H = 0  ' Continuous High Res (1lx, 120ms)
CONST BH1750_MODE_CONT_H2 = 1  ' Continuous High Res 2 (0.5lx, 120ms)
CONST BH1750_MODE_CONT_L = 2  ' Continuous Low Res (4lx, 16ms)
CONST BH1750_MODE_ONCE_H = 3  ' One-time High Res (1lx, 120ms)
CONST BH1750_MODE_ONCE_H2 = 4  ' One-time High Res 2 (0.5lx, 120ms)
CONST BH1750_MODE_ONCE_L = 5  ' One-time Low Res (4lx, 16ms)
CONST BH1750_CMD_POWER_ON = 0x01
CONST BH1750_CMD_POWER_OFF = 0x00
CONST BH1750_CMD_RESET = 0x07

' 设备初始化子程序
SUB bh1750_init()
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
