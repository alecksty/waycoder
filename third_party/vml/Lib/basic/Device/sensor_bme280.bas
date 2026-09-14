' BME280寄存器定义
' 生成自: Bosch/Sensor/BME280
' 版本: 1.0
' 日期: 2026-05-06
' 作者: VML Team
' 描述: BME280 Combined Humidity, Pressure, and Temperature Sensor (I2C/SPI)

' CPU架构: Sensor
' 位宽: 8位
' 时钟频率: 400000 Hz

' 外设定义
' BME280 Environmental Sensor (0x76/0x77, 1.71V-3.6V)
CONST BME280_BASE = 0x76
CONST BME280_CHIP_ID = 0xD0
CONST BME280_RESET = 0xE0
CONST BME280_CTRL_HUM = 0xF2
CONST BME280_STATUS = 0xF3
CONST BME280_CTRL_MEAS = 0xF4
CONST BME280_CONFIG = 0xF5
CONST BME280_PRESS = 0xF7
CONST BME280_TEMP = 0xFA
CONST BME280_HUM = 0xFD

' 设备初始化子程序
SUB bme280_init()
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
