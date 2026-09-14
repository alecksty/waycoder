' CCS811寄存器定义
' 生成自: AMS/ScioSense/Sensor/CCS811
' 版本: 1.0
' 日期: 2026-05-06
' 作者: VML Team
' 描述: CCS811 VOC/eCO2 Air Quality Sensor (I2C, 400-8192ppm CO2, 0-1187ppb TVOC)

' CPU架构: Sensor
' 位宽: 16位
' 时钟频率: 400000 Hz

' 外设定义
' CCS811 Air Quality Sensor (0x5A/0x5B, 1.8V-3.6V)
CONST CCS811_BASE = 0x5A
CONST CCS811_STATUS = 0x00
CONST CCS811_MEAS_MODE = 0x01
CONST CCS811_ALG_RESULT = 0x02
CONST CCS811_ECO2 = 0x02
CONST CCS811_TVOC = 0x04
CONST CCS811_RAW_DATA = 0x06
CONST CCS811_BASELINE = 0x0B
CONST CCS811_HW_ID = 0x20
CONST CCS811_ERROR_ID = 0xE0
CONST CCS811_APP_START = 0xF4
CONST CCS811_SW_RESET = 0xFF

' 中断向量定义
CONST INT_VECTOR = 0  ' Data ready / interrupt pin

' 引脚定义
CONST PIN_WAKE = 1  ' Wake pin (active low)

' 设备初始化子程序
SUB ccs811_init()
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
