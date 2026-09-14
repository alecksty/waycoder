/**
 * HD44780 寄存器定义
 * 生成自: Hitachi/Display/HD44780
 * 版本: 1.0
 */
export const hd44780 = {
  // CPU: Display, 8位, 0 Hz

  // 内存段
  // Display Data RAM (80 bytes, 2 lines)
  DDRAM_START: 0x00,
  DDRAM_END: 0x4F,
  DDRAM_SIZE: 80,
  // Character Generator RAM (8 custom chars x 8 bytes)
  CGRAM_START: 0x00,
  CGRAM_END: 0x3F,
  CGRAM_SIZE: 64,

  // 外设定义
  // HD44780 16x2 LCD (0x27/0x3F I2C, 5V)
  HD44780_BASE: 0x27,
  HD44780_CMD: 0x00000027,
  HD44780_DATA: 0x00000028,
  HD44780_CTRL_RS: 0x00000027,
  HD44780_CTRL_RW: 0x00000028,
  HD44780_CTRL_EN: 0x00000029,
  HD44780_CTRL_BL: 0x0000002A,
  HD44780_ADDR_DDRAM: 0x000000A7,
  HD44780_ADDR_CGRAM: 0x00000067,

  init: function() {
    // 硬件初始化
  }
};
