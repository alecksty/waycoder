' BMP280寄存器定义
' 生成自: Bosch/Sensor/BMP280
' 版本: 1.0
' 日期: 2026-05-06
' 作者: VML Team
' 描述: Digital Barometric Pressure and Temperature Sensor (I2C/SPI)

' CPU架构: Sensor
' 位宽: 8位
' 时钟频率: 3400000 Hz

' 内存段定义
' LGA-8 (2.0x2.5x0.95mm)
CONST PACKAGE_START = 0x00
CONST PACKAGE_END = 0x00
CONST PACKAGE_SIZE = 8

' 外设定义
' BMP280 I2C Sensor (0x76/0x77, 1.71V-3.6V)
CONST BMP280_BASE = 0x76
CONST BMP280_TEMP_XLSB = 0xFC
CONST BMP280_TEMP_LSB = 0xFB
CONST BMP280_TEMP_MSB = 0xFA
CONST BMP280_PRESS_XLSB = 0xF9
CONST BMP280_PRESS_LSB = 0xF8
CONST BMP280_PRESS_MSB = 0xF7
CONST BMP280_CONFIG = 0xF5
CONST BMP280_CONFIG_T_SB = 5  ' Standby time in normal mode
CONST BMP280_CONFIG_FILTER = 2  ' Filter coefficient
CONST BMP280_CONFIG_SPI3W_EN = 0  ' Enable 3-wire SPI
CONST BMP280_CTRL_MEAS = 0xF4
CONST BMP280_CTRL_MEAS_MODE = 0  ' 0=sleep, 1/2=forced, 3=normal
CONST BMP280_CTRL_MEAS_OSRS_P = 2  ' Pressure oversampling
CONST BMP280_CTRL_MEAS_OSRS_T = 5  ' Temperature oversampling
CONST BMP280_STATUS = 0xF3
CONST BMP280_STATUS_IM_UPDATE = 0  ' 1=Image register update in progress
CONST BMP280_STATUS_MEASURING = 3  ' 1=Conversion is running
CONST BMP280_CHIP_ID = 0xD0
CONST BMP280_RESET = 0xE0

' 设备初始化子程序
SUB bmp280_init()
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
