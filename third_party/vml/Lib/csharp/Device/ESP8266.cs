using System;

namespace VML.Device.EspressifSystems.ESP8266
{
    /// <summary>
    /// ESP8266 寄存器定义
    /// 生成自: Espressif Systems/ESP8266/ESP8266
    /// 版本: 
    /// </summary>
    public static class ESP8266
    {
        // CPU架构: Xtensa LX106, 0位, 0 Hz

        // 外设定义
        // Wi-Fi 802.11 b/g/n
        public const int WIFI_BASE = ;
        public static unsafe uint* WIFI_WIFI_MAC => (uint*)0x60000800;
        public static unsafe uint* WIFI_WIFI_MODE => (uint*)0x60000804;
        public static unsafe uint* WIFI_WIFI_CHANNEL => (uint*)0x60000808;
        public static unsafe uint* WIFI_WIFI_RATE => (uint*)0x6000080C;

        // Universal Asynchronous Receiver/Transmitter 0
        public const int UART0_BASE = ;
        public static unsafe uint* UART0_UART0_FIFO => (uint*)0x60000000;
        public static unsafe uint* UART0_UART0_INT_RAW => (uint*)0x60000004;
        public static unsafe uint* UART0_UART0_INT_ST => (uint*)0x60000008;
        public static unsafe uint* UART0_UART0_INT_ENA => (uint*)0x6000000C;
        public static unsafe uint* UART0_UART0_INT_CLR => (uint*)0x60000010;
        public static unsafe uint* UART0_UART0_CLKDIV => (uint*)0x60000014;
        public static unsafe uint* UART0_UART0_AUTOBAUD => (uint*)0x60000018;
        public static unsafe uint* UART0_UART0_STATUS => (uint*)0x6000001C;
        public static unsafe uint* UART0_UART0_CONF0 => (uint*)0x60000020;
        public static unsafe uint* UART0_UART0_CONF1 => (uint*)0x60000024;
        public static unsafe uint* UART0_UART0_LOWPULSE => (uint*)0x60000028;
        public static unsafe uint* UART0_UART0_HIGHPULSE => (uint*)0x6000002C;
        public static unsafe uint* UART0_UART0_RXD_CNT => (uint*)0x60000030;

        // Serial Peripheral Interface
        public const int SPI_BASE = ;
        public static unsafe uint* SPI_SPI_CMD => (uint*)0x60000200;
        public static unsafe uint* SPI_SPI_ADDR => (uint*)0x60000204;
        public static unsafe uint* SPI_SPI_CTRL => (uint*)0x60000208;
        public static unsafe uint* SPI_SPI_RD_STATUS => (uint*)0x6000020C;
        public static unsafe uint* SPI_SPI_CTRL2 => (uint*)0x60000210;
        public static unsafe uint* SPI_SPI_CLOCK => (uint*)0x60000214;
        public static unsafe uint* SPI_SPI_USER => (uint*)0x60000218;
        public static unsafe uint* SPI_SPI_USER1 => (uint*)0x6000021C;
        public static unsafe uint* SPI_SPI_USER2 => (uint*)0x60000220;
        public static unsafe uint* SPI_SPI_W0 => (uint*)0x60000280;

        // Inter-Integrated Circuit
        public const int I2C_BASE = ;
        public static unsafe uint* I2C_I2C_SCL_LOW => (uint*)0x60000C00;
        public static unsafe uint* I2C_I2C_SCL_HIGH => (uint*)0x60000C04;
        public static unsafe uint* I2C_I2C_SDA_HOLD => (uint*)0x60000C08;
        public static unsafe uint* I2C_I2C_SCL_START_HOLD => (uint*)0x60000C0C;
        public static unsafe uint* I2C_I2C_SCL_STOP_HOLD => (uint*)0x60000C10;
        public static unsafe uint* I2C_I2C_INT_RAW => (uint*)0x60000C14;
        public static unsafe uint* I2C_I2C_INT_ST => (uint*)0x60000C18;
        public static unsafe uint* I2C_I2C_INT_ENA => (uint*)0x60000C1C;
        public static unsafe uint* I2C_I2C_INT_CLR => (uint*)0x60000C20;
        public static unsafe uint* I2C_I2C_CMD => (uint*)0x60000C24;
        public static unsafe uint* I2C_I2C_FIFO_DATA => (uint*)0x60000C28;
        public static unsafe uint* I2C_I2C_FIFO_CNT => (uint*)0x60000C2C;

        // General Purpose I/O
        public const int GPIO_BASE = ;
        public static unsafe uint* GPIO_GPIO_OUT => (uint*)0x60000300;
        public static unsafe uint* GPIO_GPIO_OUT_W1TS => (uint*)0x60000304;
        public static unsafe uint* GPIO_GPIO_OUT_W1TC => (uint*)0x60000308;
        public static unsafe uint* GPIO_GPIO_ENABLE => (uint*)0x6000030C;
        public static unsafe uint* GPIO_GPIO_ENABLE_W1TS => (uint*)0x60000310;
        public static unsafe uint* GPIO_GPIO_ENABLE_W1TC => (uint*)0x60000314;
        public static unsafe uint* GPIO_GPIO_IN => (uint*)0x60000318;
        public static unsafe uint* GPIO_GPIO_STATUS => (uint*)0x6000031C;
        public static unsafe uint* GPIO_GPIO_STATUS_W1TS => (uint*)0x60000320;
        public static unsafe uint* GPIO_GPIO_STATUS_W1TC => (uint*)0x60000324;
        public static unsafe uint* GPIO_GPIO_PIN => (uint*)0x60000328;

        // Hardware Timer
        public const int TIMER_BASE = ;
        public static unsafe uint* TIMER_TIMER_LOAD => (uint*)0x60000600;
        public static unsafe uint* TIMER_TIMER_COUNT => (uint*)0x60000604;
        public static unsafe uint* TIMER_TIMER_CTRL => (uint*)0x60000608;
        public static unsafe uint* TIMER_TIMER_INT => (uint*)0x6000060C;
        public static unsafe uint* TIMER_TIMER_ALARM => (uint*)0x60000610;

        // Analog-to-Digital Converter
        public const int ADC_BASE = ;
        public static unsafe uint* ADC_ADC_CTRL => (uint*)0x60000E00;
        public static unsafe uint* ADC_ADC_DATA => (uint*)0x60000E04;

        // Pulse Width Modulation
        public const int PWM_BASE = ;
        public static unsafe uint* PWM_PWM_CTRL => (uint*)0x60000F00;
        public static unsafe uint* PWM_PWM_PERIOD => (uint*)0x60000F04;
        public static unsafe uint* PWM_PWM_DUTY => (uint*)0x60000F08;

        // 中断向量定义
        public const int IRQ_NMI = 1;  // Non-maskable interrupt
        public const int IRQ_LEVEL1 = 3;  // Level 1 interrupt
        public const int IRQ_LEVEL2 = 4;  // Level 2 interrupt
        public const int IRQ_LEVEL3 = 5;  // Level 3 interrupt
        public const int IRQ_LEVEL4 = 6;  // Level 4 interrupt
        public const int IRQ_LEVEL5 = 7;  // Level 5 interrupt
        public const int IRQ_TIMER0 = 8;  // Timer 0 interrupt
        public const int IRQ_TIMER1 = 9;  // Timer 1 interrupt
        public const int IRQ_UART0 = 10;  // UART0 interrupt
        public const int IRQ_UART1 = 11;  // UART1 interrupt
        public const int IRQ_GPIO = 12;  // GPIO interrupt
        public const int IRQ_PWM = 13;  // PWM interrupt
        public const int IRQ_I2C = 14;  // I2C interrupt
        public const int IRQ_SPI = 15;  // SPI interrupt
        public const int IRQ_ADC = 16;  // ADC interrupt
        public const int IRQ_WIFI = 17;  // Wi-Fi interrupt
        public const int IRQ_RTC = 18;  // RTC interrupt

        public static void esp8266_init()
        {
            // 硬件初始化代码
        }
    }
}
