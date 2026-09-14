' NEO6M寄存器定义
' 生成自: u-blox/GPS/NEO6M
' 版本: 1.0
' 日期: 2026-05-06
' 作者: VML Team
' 描述: NEO-6M GPS Module (UART, 50-channel, -162dBm tracking)

' CPU架构: GPS
' 位宽: 8位
' 时钟频率: 9600 Hz

' 外设定义
' NEO-6M GPS Module (UART 9600bps, 3.3V-5V)
CONST NEO6M_BASE = 0x00
CONST NEO6M_LATITUDE = 0x00
CONST NEO6M_LONGITUDE = 0x04
CONST NEO6M_ALTITUDE = 0x08
CONST NEO6M_SPEED = 0x0C
CONST NEO6M_HEADING = 0x0E
CONST NEO6M_SATELLITES = 0x10
CONST NEO6M_HDOP = 0x11
CONST NEO6M_FIX_TYPE = 0x13
CONST NEO6M_DATE = 0x14
CONST NEO6M_TIME = 0x18
CONST NEO6M_VALID = 0x1C

' 设备初始化子程序
SUB neo6m_init()
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
