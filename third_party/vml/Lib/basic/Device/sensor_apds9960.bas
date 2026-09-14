' APDS9960寄存器定义
' 生成自: Broadcom/Avago/Sensor/APDS9960
' 版本: 1.0
' 日期: 2026-05-06
' 作者: VML Team
' 描述: APDS9960 Gesture/Proximity/Ambient Light/RGB Sensor (I2C)

' CPU架构: Sensor
' 位宽: 8位
' 时钟频率: 400000 Hz

' 外设定义
' APDS9960 Gesture/RGB Sensor (0x39, 3.3V)
CONST APDS9960_BASE = 0x39
CONST APDS9960_ENABLE = 0x80
CONST APDS9960_GESTURE = 0xFC
CONST APDS9960_PROXIMITY = 0x9C
CONST APDS9960_AMBIENT = 0x96
CONST APDS9960_RED = 0x98
CONST APDS9960_GREEN = 0x9A
CONST APDS9960_BLUE = 0x9C
CONST APDS9960_GESTURE_FIFO = 0xFC
CONST APDS9960_GESTURE_COUNT = 0xFD

' 中断向量定义
CONST INT_VECTOR = 0  ' Gesture/Proximity/Light interrupt

' 设备初始化子程序
SUB apds9960_init()
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
