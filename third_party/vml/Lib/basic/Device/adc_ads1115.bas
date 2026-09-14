' ADS1115寄存器定义
' 生成自: Texas Instruments/ADC/ADS1115
' 版本: 1.0
' 日期: 2026-05-06
' 作者: VML Team
' 描述: ADS1115 16-bit I2C ADC (4-channel, PGA, 860SPS)

' CPU架构: ADC
' 位宽: 16位
' 时钟频率: 400000 Hz

' 外设定义
' ADS1115 16-bit ADC (0x48-0x4B, 2.0V-5.5V)
CONST ADS1115_BASE = 0x48
CONST ADS1115_CONV_RESULT = 0x00
CONST ADS1115_CONFIG = 0x01
CONST ADS1115_CONFIG_OS = 15  ' Operational status/start single-shot
CONST ADS1115_CONFIG_MUX = 12  ' Input multiplexer: 0=A0-A1,1=A0-A3,2=A1-A3,3=A2-A3,4=A0,5=A1,6=A2,7=A3
CONST ADS1115_CONFIG_PGA = 9  ' PGA gain: 0=±6.144V,1=±4.096V,2=±2.048V,3=±1.024V,4=±0.512V,5=±0.256V
CONST ADS1115_CONFIG_MODE = 8  ' 0=continuous, 1=single-shot
CONST ADS1115_CONFIG_DR = 5  ' Data rate: 0=8,1=16,2=32,3=64,4=128,5=250,6=475,7=860 SPS
CONST ADS1115_CONFIG_COMP_MODE = 4  ' Comparator mode (0=traditional, 1=window)
CONST ADS1115_CONFIG_COMP_POL = 3  ' Comparator polarity (0=active low, 1=active high)
CONST ADS1115_LO_THRESH = 0x02
CONST ADS1115_HI_THRESH = 0x03

' 设备初始化子程序
SUB ads1115_init()
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
