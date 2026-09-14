package vml.device.espressifsystems.esp8266;

/**
 * ESP8266 寄存器定义
 * 生成自: Espressif Systems/ESP8266/ESP8266
 * 版本: 
 */
public final class ESP8266 {
    private ESP8266() {} // 工具类
    // CPU架构: Xtensa LX106, 0位, 0 Hz

    // 外设定义
    // Wi-Fi 802.11 b/g/n
    public static final int WIFI_BASE = (int);
    public static final int WIFI_WIFI_MAC = (int)0x60000800;
    public static final int WIFI_WIFI_MODE = (int)0x60000804;
    public static final int WIFI_WIFI_CHANNEL = (int)0x60000808;
    public static final int WIFI_WIFI_RATE = (int)0x6000080C;

    // Universal Asynchronous Receiver/Transmitter 0
    public static final int UART0_BASE = (int);
    public static final int UART0_UART0_FIFO = (int)0x60000000;
    public static final int UART0_UART0_INT_RAW = (int)0x60000004;
    public static final int UART0_UART0_INT_ST = (int)0x60000008;
    public static final int UART0_UART0_INT_ENA = (int)0x6000000C;
    public static final int UART0_UART0_INT_CLR = (int)0x60000010;
    public static final int UART0_UART0_CLKDIV = (int)0x60000014;
    public static final int UART0_UART0_AUTOBAUD = (int)0x60000018;
    public static final int UART0_UART0_STATUS = (int)0x6000001C;
    public static final int UART0_UART0_CONF0 = (int)0x60000020;
    public static final int UART0_UART0_CONF1 = (int)0x60000024;
    public static final int UART0_UART0_LOWPULSE = (int)0x60000028;
    public static final int UART0_UART0_HIGHPULSE = (int)0x6000002C;
    public static final int UART0_UART0_RXD_CNT = (int)0x60000030;

    // Serial Peripheral Interface
    public static final int SPI_BASE = (int);
    public static final int SPI_SPI_CMD = (int)0x60000200;
    public static final int SPI_SPI_ADDR = (int)0x60000204;
    public static final int SPI_SPI_CTRL = (int)0x60000208;
    public static final int SPI_SPI_RD_STATUS = (int)0x6000020C;
    public static final int SPI_SPI_CTRL2 = (int)0x60000210;
    public static final int SPI_SPI_CLOCK = (int)0x60000214;
    public static final int SPI_SPI_USER = (int)0x60000218;
    public static final int SPI_SPI_USER1 = (int)0x6000021C;
    public static final int SPI_SPI_USER2 = (int)0x60000220;
    public static final int SPI_SPI_W0 = (int)0x60000280;

    // Inter-Integrated Circuit
    public static final int I2C_BASE = (int);
    public static final int I2C_I2C_SCL_LOW = (int)0x60000C00;
    public static final int I2C_I2C_SCL_HIGH = (int)0x60000C04;
    public static final int I2C_I2C_SDA_HOLD = (int)0x60000C08;
    public static final int I2C_I2C_SCL_START_HOLD = (int)0x60000C0C;
    public static final int I2C_I2C_SCL_STOP_HOLD = (int)0x60000C10;
    public static final int I2C_I2C_INT_RAW = (int)0x60000C14;
    public static final int I2C_I2C_INT_ST = (int)0x60000C18;
    public static final int I2C_I2C_INT_ENA = (int)0x60000C1C;
    public static final int I2C_I2C_INT_CLR = (int)0x60000C20;
    public static final int I2C_I2C_CMD = (int)0x60000C24;
    public static final int I2C_I2C_FIFO_DATA = (int)0x60000C28;
    public static final int I2C_I2C_FIFO_CNT = (int)0x60000C2C;

    // General Purpose I/O
    public static final int GPIO_BASE = (int);
    public static final int GPIO_GPIO_OUT = (int)0x60000300;
    public static final int GPIO_GPIO_OUT_W1TS = (int)0x60000304;
    public static final int GPIO_GPIO_OUT_W1TC = (int)0x60000308;
    public static final int GPIO_GPIO_ENABLE = (int)0x6000030C;
    public static final int GPIO_GPIO_ENABLE_W1TS = (int)0x60000310;
    public static final int GPIO_GPIO_ENABLE_W1TC = (int)0x60000314;
    public static final int GPIO_GPIO_IN = (int)0x60000318;
    public static final int GPIO_GPIO_STATUS = (int)0x6000031C;
    public static final int GPIO_GPIO_STATUS_W1TS = (int)0x60000320;
    public static final int GPIO_GPIO_STATUS_W1TC = (int)0x60000324;
    public static final int GPIO_GPIO_PIN = (int)0x60000328;

    // Hardware Timer
    public static final int TIMER_BASE = (int);
    public static final int TIMER_TIMER_LOAD = (int)0x60000600;
    public static final int TIMER_TIMER_COUNT = (int)0x60000604;
    public static final int TIMER_TIMER_CTRL = (int)0x60000608;
    public static final int TIMER_TIMER_INT = (int)0x6000060C;
    public static final int TIMER_TIMER_ALARM = (int)0x60000610;

    // Analog-to-Digital Converter
    public static final int ADC_BASE = (int);
    public static final int ADC_ADC_CTRL = (int)0x60000E00;
    public static final int ADC_ADC_DATA = (int)0x60000E04;

    // Pulse Width Modulation
    public static final int PWM_BASE = (int);
    public static final int PWM_PWM_CTRL = (int)0x60000F00;
    public static final int PWM_PWM_PERIOD = (int)0x60000F04;
    public static final int PWM_PWM_DUTY = (int)0x60000F08;

    // 中断向量定义
    public static final int IRQ_NMI = 1;  // Non-maskable interrupt
    public static final int IRQ_LEVEL1 = 3;  // Level 1 interrupt
    public static final int IRQ_LEVEL2 = 4;  // Level 2 interrupt
    public static final int IRQ_LEVEL3 = 5;  // Level 3 interrupt
    public static final int IRQ_LEVEL4 = 6;  // Level 4 interrupt
    public static final int IRQ_LEVEL5 = 7;  // Level 5 interrupt
    public static final int IRQ_TIMER0 = 8;  // Timer 0 interrupt
    public static final int IRQ_TIMER1 = 9;  // Timer 1 interrupt
    public static final int IRQ_UART0 = 10;  // UART0 interrupt
    public static final int IRQ_UART1 = 11;  // UART1 interrupt
    public static final int IRQ_GPIO = 12;  // GPIO interrupt
    public static final int IRQ_PWM = 13;  // PWM interrupt
    public static final int IRQ_I2C = 14;  // I2C interrupt
    public static final int IRQ_SPI = 15;  // SPI interrupt
    public static final int IRQ_ADC = 16;  // ADC interrupt
    public static final int IRQ_WIFI = 17;  // Wi-Fi interrupt
    public static final int IRQ_RTC = 18;  // RTC interrupt

    public static native void esp8266_init();
}
