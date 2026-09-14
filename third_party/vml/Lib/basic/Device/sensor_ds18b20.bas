' DS18B20寄存器定义
' 生成自: Maxim/Dallas/Sensor/DS18B20
' 版本: 1.0
' 日期: 2026-05-06
' 作者: VML Team
' 描述: Programmable Resolution 1-Wire Digital Thermometer

' CPU架构: Sensor
' 位宽: 8位
' 时钟频率: 100000 Hz

' 内存段定义
' Scratchpad memory (9 bytes)
CONST SCRATCHPAD_START = 0x00
CONST SCRATCHPAD_END = 0x08
CONST SCRATCHPAD_SIZE = 9

' EEPROM (TH, TL, config bytes)
CONST EEPROM_START = 0x00
CONST EEPROM_END = 0x02
CONST EEPROM_SIZE = 3

' 外设定义
' DS18B20 1-Wire Thermometer (3.0V-5.5V, TO-92)
CONST DS18B20_BASE = 0x00
CONST DS18B20_TEMP_LSB = 0x00
CONST DS18B20_TEMP_MSB = 0x01
CONST DS18B20_TH_REG = 0x02
CONST DS18B20_TL_REG = 0x03
CONST DS18B20_CONFIG = 0x04
CONST DS18B20_CONFIG_R0 = 5  ' Resolution select bit 0
CONST DS18B20_CONFIG_R1 = 6  ' Resolution select bit 1 (00=9bit,10=10bit,01=11bit,11=12bit)
CONST DS18B20_COUNT_REMAIN = 0x06
CONST DS18B20_COUNT_PER_C = 0x07
CONST DS18B20_CRC = 0x08

' 设备初始化子程序
SUB ds18b20_init()
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
