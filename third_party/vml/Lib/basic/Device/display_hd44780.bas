' HD44780寄存器定义
' 生成自: Hitachi/Display/HD44780
' 版本: 1.0
' 日期: 2026-05-06
' 作者: VML Team
' 描述: HD44780 16x2 Character LCD Controller (4-bit/8-bit parallel or I2C via PCF8574)

' CPU架构: Display
' 位宽: 8位
' 时钟频率: 0 Hz

' 内存段定义
' Display Data RAM (80 bytes, 2 lines)
CONST DDRAM_START = 0x00
CONST DDRAM_END = 0x4F
CONST DDRAM_SIZE = 80

' Character Generator RAM (8 custom chars x 8 bytes)
CONST CGRAM_START = 0x00
CONST CGRAM_END = 0x3F
CONST CGRAM_SIZE = 64

' 外设定义
' HD44780 16x2 LCD (0x27/0x3F I2C, 5V)
CONST HD44780_BASE = 0x27
CONST HD44780_CMD = 0x00
CONST HD44780_DATA = 0x01
CONST HD44780_CTRL_RS = 0x00
CONST HD44780_CTRL_RW = 0x01
CONST HD44780_CTRL_EN = 0x02
CONST HD44780_CTRL_BL = 0x03
CONST HD44780_ADDR_DDRAM = 0x80
CONST HD44780_ADDR_CGRAM = 0x40

' 设备初始化子程序
SUB hd44780_init()
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
