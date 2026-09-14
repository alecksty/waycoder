' 24C64寄存器定义
' 生成自: Generic/Memory/24C64
' 版本: 1.0
' 日期: 2026-05-06
' 作者: VML Team
' 描述: 24C64 64Kbit I2C Serial EEPROM (8K×8, 32-byte page write)

' CPU架构: Memory
' 位宽: 8位
' 时钟频率: 400000 Hz

' 内存段定义
' EEPROM main memory array (8KB, 32-byte page write)
CONST EEPROM_START = 0x00
CONST EEPROM_END = 0x1FFF
CONST EEPROM_SIZE = 8192

' 外设定义
' 24C64 I2C EEPROM (0x50-0x57, 1.7V-5.5V)
CONST _24C64_BASE = 0x50
CONST _24C64_ADDR_H = 0x00
CONST _24C64_ADDR_L = 0x01
CONST _24C64_DATA = 0x02
CONST _24C64_PAGE_SIZE = 0xFE
CONST _24C64_SIZE = 0xFD

' 设备初始化子程序
SUB _24c64_init()
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
