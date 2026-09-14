' WS2812B寄存器定义
' 生成自: Worldsemi/LED/WS2812B
' 版本: 1.0
' 日期: 2026-05-06
' 作者: VML Team
' 描述: WS2812B Intelligent RGB LED (single-wire, 800KHz, daisy-chainable)

' CPU架构: LED
' 位宽: 24位
' 时钟频率: 800000 Hz

' 内存段定义
' Frame buffer (up to 256 LEDs × 3 bytes)
CONST LED_FB_START = 0x00
CONST LED_FB_END = 0xFF
CONST LED_FB_SIZE = 256

' 外设定义
' WS2812B RGB LED Strip (5V, 60mA/led)
CONST WS2812B_BASE = 0x00
CONST WS2812B_LED_COUNT = 0x00
CONST WS2812B_LED_DATA = 0x02
CONST WS2812B_BRIGHTNESS = 0x05
CONST WS2812B_SHOW = 0x06
CONST WS2812B_CLEAR = 0x07

' 设备初始化子程序
SUB ws2812b_init()
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
