// ESP8266 设备定义 - Dart 库
// 生成自: Espressif Systems/ESP8266/ESP8266
// 版本: 
// 日期: 
// 作者: 
// 描述: Espressif ESP8266 Wi-Fi SoC with integrated TCP/IP stack
// CPU架构: Xtensa LX106
// 位宽: 0位
// 时钟频率: 0 Hz

class ESP8266Device {
  static const String deviceName = "ESP8266";
  static const String manufacturer = "Espressif Systems";
  static const String family = "ESP8266";
  static const String version = "";
  static const String architecture = "Xtensa LX106";
  static const int bits = 0;
  static const int clockFrequency = 0;

  // 外设定义
  // Wi-Fi 802.11 b/g/n
  static const int WIFI_BASE = ;
  static const int WIFI_WIFI_MAC_ADDR = 0x60000800;
  static const int WIFI_WIFI_MODE_ADDR = 0x60000804;
  static const int WIFI_WIFI_CHANNEL_ADDR = 0x60000808;
  static const int WIFI_WIFI_RATE_ADDR = 0x6000080C;
  // Universal Asynchronous Receiver/Transmitter 0
  static const int UART0_BASE = ;
  static const int UART0_UART0_FIFO_ADDR = 0x60000000;
  static const int UART0_UART0_INT_RAW_ADDR = 0x60000004;
  static const int UART0_UART0_INT_ST_ADDR = 0x60000008;
  static const int UART0_UART0_INT_ENA_ADDR = 0x6000000C;
  static const int UART0_UART0_INT_CLR_ADDR = 0x60000010;
  static const int UART0_UART0_CLKDIV_ADDR = 0x60000014;
  static const int UART0_UART0_AUTOBAUD_ADDR = 0x60000018;
  static const int UART0_UART0_STATUS_ADDR = 0x6000001C;
  static const int UART0_UART0_CONF0_ADDR = 0x60000020;
  static const int UART0_UART0_CONF1_ADDR = 0x60000024;
  static const int UART0_UART0_LOWPULSE_ADDR = 0x60000028;
  static const int UART0_UART0_HIGHPULSE_ADDR = 0x6000002C;
  static const int UART0_UART0_RXD_CNT_ADDR = 0x60000030;
  // Serial Peripheral Interface
  static const int SPI_BASE = ;
  static const int SPI_SPI_CMD_ADDR = 0x60000200;
  static const int SPI_SPI_ADDR_ADDR = 0x60000204;
  static const int SPI_SPI_CTRL_ADDR = 0x60000208;
  static const int SPI_SPI_RD_STATUS_ADDR = 0x6000020C;
  static const int SPI_SPI_CTRL2_ADDR = 0x60000210;
  static const int SPI_SPI_CLOCK_ADDR = 0x60000214;
  static const int SPI_SPI_USER_ADDR = 0x60000218;
  static const int SPI_SPI_USER1_ADDR = 0x6000021C;
  static const int SPI_SPI_USER2_ADDR = 0x60000220;
  static const int SPI_SPI_W0_ADDR = 0x60000280;
  // Inter-Integrated Circuit
  static const int I2C_BASE = ;
  static const int I2C_I2C_SCL_LOW_ADDR = 0x60000C00;
  static const int I2C_I2C_SCL_HIGH_ADDR = 0x60000C04;
  static const int I2C_I2C_SDA_HOLD_ADDR = 0x60000C08;
  static const int I2C_I2C_SCL_START_HOLD_ADDR = 0x60000C0C;
  static const int I2C_I2C_SCL_STOP_HOLD_ADDR = 0x60000C10;
  static const int I2C_I2C_INT_RAW_ADDR = 0x60000C14;
  static const int I2C_I2C_INT_ST_ADDR = 0x60000C18;
  static const int I2C_I2C_INT_ENA_ADDR = 0x60000C1C;
  static const int I2C_I2C_INT_CLR_ADDR = 0x60000C20;
  static const int I2C_I2C_CMD_ADDR = 0x60000C24;
  static const int I2C_I2C_FIFO_DATA_ADDR = 0x60000C28;
  static const int I2C_I2C_FIFO_CNT_ADDR = 0x60000C2C;
  // General Purpose I/O
  static const int GPIO_BASE = ;
  static const int GPIO_GPIO_OUT_ADDR = 0x60000300;
  static const int GPIO_GPIO_OUT_W1TS_ADDR = 0x60000304;
  static const int GPIO_GPIO_OUT_W1TC_ADDR = 0x60000308;
  static const int GPIO_GPIO_ENABLE_ADDR = 0x6000030C;
  static const int GPIO_GPIO_ENABLE_W1TS_ADDR = 0x60000310;
  static const int GPIO_GPIO_ENABLE_W1TC_ADDR = 0x60000314;
  static const int GPIO_GPIO_IN_ADDR = 0x60000318;
  static const int GPIO_GPIO_STATUS_ADDR = 0x6000031C;
  static const int GPIO_GPIO_STATUS_W1TS_ADDR = 0x60000320;
  static const int GPIO_GPIO_STATUS_W1TC_ADDR = 0x60000324;
  static const int GPIO_GPIO_PIN_ADDR = 0x60000328;
  // Hardware Timer
  static const int TIMER_BASE = ;
  static const int TIMER_TIMER_LOAD_ADDR = 0x60000600;
  static const int TIMER_TIMER_COUNT_ADDR = 0x60000604;
  static const int TIMER_TIMER_CTRL_ADDR = 0x60000608;
  static const int TIMER_TIMER_INT_ADDR = 0x6000060C;
  static const int TIMER_TIMER_ALARM_ADDR = 0x60000610;
  // Analog-to-Digital Converter
  static const int ADC_BASE = ;
  static const int ADC_ADC_CTRL_ADDR = 0x60000E00;
  static const int ADC_ADC_DATA_ADDR = 0x60000E04;
  // Pulse Width Modulation
  static const int PWM_BASE = ;
  static const int PWM_PWM_CTRL_ADDR = 0x60000F00;
  static const int PWM_PWM_PERIOD_ADDR = 0x60000F04;
  static const int PWM_PWM_DUTY_ADDR = 0x60000F08;

  // 中断向量定义
  static const int INT_NMI = 1;  // Non-maskable interrupt
  static const int INT_LEVEL1 = 3;  // Level 1 interrupt
  static const int INT_LEVEL2 = 4;  // Level 2 interrupt
  static const int INT_LEVEL3 = 5;  // Level 3 interrupt
  static const int INT_LEVEL4 = 6;  // Level 4 interrupt
  static const int INT_LEVEL5 = 7;  // Level 5 interrupt
  static const int INT_TIMER0 = 8;  // Timer 0 interrupt
  static const int INT_TIMER1 = 9;  // Timer 1 interrupt
  static const int INT_UART0 = 10;  // UART0 interrupt
  static const int INT_UART1 = 11;  // UART1 interrupt
  static const int INT_GPIO = 12;  // GPIO interrupt
  static const int INT_PWM = 13;  // PWM interrupt
  static const int INT_I2C = 14;  // I2C interrupt
  static const int INT_SPI = 15;  // SPI interrupt
  static const int INT_ADC = 16;  // ADC interrupt
  static const int INT_WIFI = 17;  // Wi-Fi interrupt
  static const int INT_RTC = 18;  // RTC interrupt

}
