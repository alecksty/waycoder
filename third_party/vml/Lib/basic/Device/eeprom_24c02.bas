' 24C02寄存器定义
' 生成自: Generic/Memory/24C02
' 版本: 1.0
' 日期: 2026-05-06
' 作者: VML Team
' 描述: 2Kbit I2C Serial EEPROM (256 x 8 bits)

' CPU架构: Memory
' 位宽: 8位
' 时钟频率: 400000 Hz

' 内存段定义
' EEPROM main memory array (256 bytes, 8-byte page write)
CONST EEPROM_START = 0x00
CONST EEPROM_END = 0xFF
CONST EEPROM_SIZE = 256

' 外设定义
' 24C02 I2C EEPROM (0x50-0x57, 1.8V-5.5V, DIP-8)
CONST _24C02_BASE = 0x50
CONST _24C02_STATUS = 0xFF
CONST _24C02_STATUS_BUSY = 0  ' 1=Write in progress
CONST _24C02_PAGE_SIZE = 0xFE
CONST _24C02_SIZE = 0xFD

' 设备初始化子程序
SUB _24c02_init()
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
