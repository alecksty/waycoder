' MCP4921寄存器定义
' 生成自: Microchip/DAC/MCP4921
' 版本: 1.0
' 日期: 2026-05-06
' 作者: VML Team
' 描述: MCP4921 12-bit SPI DAC (single channel, 2x buffered output)

' CPU架构: DAC
' 位宽: 12位
' 时钟频率: 20000000 Hz

' 外设定义
' MCP4921 12-bit DAC (SPI, 2.7V-5.5V)
CONST MCP4921_BASE = 0x00
CONST MCP4921_DAC_VALUE = 0x00
CONST MCP4921_DAC_VALUE_BUF = 14  ' VREF buffer (0=unbuffered, 1=buffered)
CONST MCP4921_DAC_VALUE_GA = 13  ' Gain (0=2x, 1=1x)
CONST MCP4921_DAC_VALUE_SHDN = 12  ' Shutdown (0=shutdown, 1=active)
CONST MCP4921_VREF = 0x02

' 设备初始化子程序
SUB mcp4921_init()
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
