unit stm32f103c8t6;

interface

// STM32F103C8T6寄存器定义
// 生成自: STMicroelectronics/STM32/STM32F103C8T6
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M3 MCU with 64KB Flash, 20KB RAM, 72MHz

// CPU架构: ARM-Cortex-M3
// 位宽: 32位
// 时钟频率: 72000000 Hz

const

  // 寄存器定义
  // General Purpose Register 0
  R0 = 0x00;

  // General Purpose Register 1
  R1 = 0x04;

  // General Purpose Register 2
  R2 = 0x08;

  // General Purpose Register 3
  R3 = 0x0C;

  // General Purpose Register 4
  R4 = 0x10;

  // General Purpose Register 5
  R5 = 0x14;

  // General Purpose Register 6
  R6 = 0x18;

  // General Purpose Register 7
  R7 = 0x1C;

  // General Purpose Register 8
  R8 = 0x20;

  // General Purpose Register 9
  R9 = 0x24;

  // General Purpose Register 10
  R10 = 0x28;

  // General Purpose Register 11
  R11 = 0x2C;

  // General Purpose Register 12
  R12 = 0x30;

  // Stack Pointer
  SP = 0x34;

  // Link Register
  LR = 0x38;

  // Program Counter
  PC = 0x3C;

  // Program Status Register
  XPSR = 0x40;

  // 内存段定义
  // Main Flash Memory (64KB)
  FLASH_START = 0x08000000;
  FLASH_END = 0x0800FFFF;
  FLASH_SIZE = 65536;

  // System Memory (2KB)
  SYSTEM_MEMORY_START = 0x1FFFF000;
  SYSTEM_MEMORY_END = 0x1FFFF7FF;
  SYSTEM_MEMORY_SIZE = 2048;

  // SRAM (20KB)
  SRAM_START = 0x20000000;
  SRAM_END = 0x20004FFF;
  SRAM_SIZE = 20480;

  // Peripheral Registers
  PERIPHERAL_START = 0x40000000;
  PERIPHERAL_END = 0x40023FFF;
  PERIPHERAL_SIZE = 143360;

  // Core Peripheral Registers
  CORTEX_M_START = 0xE0000000;
  CORTEX_M_END = 0xE00FFFFF;
  CORTEX_M_SIZE = 1048576;

  // 外设定义
  // Reset and Clock Control
  RCC_BASE = 0x40021000;
  RCC_CR = 0x00;
  RCC_CFGR = 0x04;
  RCC_APB2ENR = 0x18;
  RCC_APB1ENR = 0x1C;

  // GPIO Port A
  GPIOA_BASE = 0x40010800;
  GPIOA_CRL = 0x00;
  GPIOA_CRH = 0x04;
  GPIOA_IDR = 0x08;
  GPIOA_ODR = 0x0C;
  GPIOA_BSRR = 0x10;
  GPIOA_BRR = 0x14;
  GPIOA_LCKR = 0x18;

  // GPIO Port B
  GPIOB_BASE = 0x40010C00;
  GPIOB_CRL = 0x00;
  GPIOB_CRH = 0x04;
  GPIOB_IDR = 0x08;
  GPIOB_ODR = 0x0C;
  GPIOB_BSRR = 0x10;
  GPIOB_BRR = 0x14;

  // GPIO Port C
  GPIOC_BASE = 0x40011000;
  GPIOC_CRL = 0x00;
  GPIOC_CRH = 0x04;
  GPIOC_IDR = 0x08;
  GPIOC_ODR = 0x0C;
  GPIOC_BSRR = 0x10;

  // USART 1
  USART1_BASE = 0x40013800;
  USART1_SR = 0x00;
  USART1_DR = 0x04;
  USART1_BRR = 0x08;
  USART1_CR1 = 0x0C;
  USART1_CR2 = 0x10;
  USART1_CR3 = 0x14;
  USART1_GTPR = 0x18;

  // USART 2
  USART2_BASE = 0x40004400;
  USART2_SR = 0x00;
  USART2_DR = 0x04;
  USART2_BRR = 0x08;
  USART2_CR1 = 0x0C;

  // SPI 1
  SPI1_BASE = 0x40013000;
  SPI1_CR1 = 0x00;
  SPI1_CR2 = 0x04;
  SPI1_SR = 0x08;
  SPI1_DR = 0x0C;

  // SPI 2
  SPI2_BASE = 0x40003800;
  SPI2_CR1 = 0x00;
  SPI2_CR2 = 0x04;
  SPI2_SR = 0x08;
  SPI2_DR = 0x0C;

  // I2C 1
  I2C1_BASE = 0x40005400;
  I2C1_CR1 = 0x00;
  I2C1_CR2 = 0x04;
  I2C1_SR1 = 0x08;
  I2C1_SR2 = 0x0C;
  I2C1_DR = 0x10;
  I2C1_CCR = 0x14;
  I2C1_TRISE = 0x18;

  // I2C 2
  I2C2_BASE = 0x40005800;
  I2C2_CR1 = 0x00;
  I2C2_CR2 = 0x04;
  I2C2_SR1 = 0x08;
  I2C2_SR2 = 0x0C;
  I2C2_DR = 0x10;
  I2C2_CCR = 0x14;

  // Advanced Timer 1
  TIM1_BASE = 0x40012C00;
  TIM1_CR1 = 0x00;
  TIM1_CR2 = 0x04;
  TIM1_SMCR = 0x08;
  TIM1_DIER = 0x0C;
  TIM1_SR = 0x10;
  TIM1_EGR = 0x14;
  TIM1_CCMR1 = 0x18;
  TIM1_CCMR2 = 0x1C;
  TIM1_CCER = 0x20;
  TIM1_CNT = 0x24;
  TIM1_PSC = 0x28;
  TIM1_ARR = 0x2C;
  TIM1_RCR = 0x30;
  TIM1_CCR1 = 0x34;
  TIM1_CCR2 = 0x38;
  TIM1_CCR3 = 0x3C;
  TIM1_CCR4 = 0x40;
  TIM1_BDTR = 0x44;

  // General Purpose Timer 2
  TIM2_BASE = 0x40000400;
  TIM2_CR1 = 0x00;
  TIM2_CNT = 0x24;
  TIM2_PSC = 0x28;
  TIM2_ARR = 0x2C;
  TIM2_CCR1 = 0x34;
  TIM2_CCR2 = 0x38;
  TIM2_CCR3 = 0x3C;
  TIM2_CCR4 = 0x40;

  // General Purpose Timer 3
  TIM3_BASE = 0x40000400;
  TIM3_CR1 = 0x00;
  TIM3_CNT = 0x24;
  TIM3_ARR = 0x2C;
  TIM3_CCR1 = 0x34;
  TIM3_CCR2 = 0x38;
  TIM3_CCR3 = 0x3C;
  TIM3_CCR4 = 0x40;

  // General Purpose Timer 4
  TIM4_BASE = 0x40000800;
  TIM4_CR1 = 0x00;
  TIM4_CNT = 0x24;
  TIM4_ARR = 0x2C;
  TIM4_CCR1 = 0x34;
  TIM4_CCR2 = 0x38;
  TIM4_CCR3 = 0x3C;
  TIM4_CCR4 = 0x40;

  // ADC 1
  ADC1_BASE = 0x40012400;
  ADC1_SR = 0x00;
  ADC1_CR1 = 0x04;
  ADC1_CR2 = 0x08;
  ADC1_SMPR1 = 0x0C;
  ADC1_SMPR2 = 0x10;
  ADC1_JOFR1 = 0x14;
  ADC1_JOFR2 = 0x18;
  ADC1_JOFR3 = 0x1C;
  ADC1_JOFR4 = 0x20;
  ADC1_HTR = 0x24;
  ADC1_LTR = 0x28;
  ADC1_SQRT1 = 0x2C;
  ADC1_SQRT2 = 0x30;
  ADC1_SQRT3 = 0x34;
  ADC1_JSQR = 0x38;
  ADC1_JDR1 = 0x3C;
  ADC1_JDR2 = 0x40;
  ADC1_JDR3 = 0x44;
  ADC1_JDR4 = 0x48;
  ADC1_DR = 0x4C;

  // DMA Controller 1
  DMA1_BASE = 0x40020000;
  DMA1_ISR = 0x00;
  DMA1_IFCR = 0x04;
  DMA1_CCR1 = 0x08;
  DMA1_CNDTR1 = 0x0C;
  DMA1_CPAR1 = 0x10;
  DMA1_CMAR1 = 0x14;
  DMA1_CCR2 = 0x1C;
  DMA1_CNDTR2 = 0x20;
  DMA1_CPAR2 = 0x24;
  DMA1_CMAR2 = 0x28;

  // Power Control
  PWR_BASE = 0x40007000;
  PWR_CR = 0x00;
  PWR_CSR = 0x04;

  // Backup Registers
  BKP_BASE = 0x40006C00;
  BKP_DR1 = 0x04;
  BKP_DR2 = 0x08;
  BKP_CSR = 0x2C;

  // Window Watchdog
  WWDG_BASE = 0x40002C00;
  WWDG_CR = 0x00;
  WWDG_CFR = 0x04;
  WWDG_SR = 0x08;

  // Independent Watchdog
  IWDG_BASE = 0x40003000;
  IWDG_KR = 0x00;
  IWDG_PR = 0x04;
  IWDG_RLR = 0x08;

  // External Interrupt/Event Controller
  EXTI_BASE = 0x40010400;
  EXTI_IMR = 0x00;
  EXTI_EMR = 0x04;
  EXTI_RTSR = 0x08;
  EXTI_FTSR = 0x0C;
  EXTI_SWIER = 0x10;
  EXTI_PR = 0x14;

  // Alternate Function IO
  AFIO_BASE = 0x40010000;
  AFIO_EVCR = 0x00;
  AFIO_MAPR = 0x04;
  AFIO_EXTICR1 = 0x08;
  AFIO_EXTICR2 = 0x0C;
  AFIO_EXTICR3 = 0x10;
  AFIO_MAPR2 = 0x1C;

  // 中断向量定义
  WWDG_VECTOR = 0;  // Window Watchdog Interrupt
  PVD_VECTOR = 1;  // PVD through EXTI Line detection
  TAMPER_VECTOR = 2;  // Tamper Interrupt
  RTC_VECTOR = 3;  // RTC Global Interrupt
  FLASH_VECTOR = 4;  // FLASH Global Interrupt
  RCC_VECTOR = 5;  // RCC Global Interrupt
  EXTI0_VECTOR = 6;  // EXTI Line 0 Interrupt
  EXTI1_VECTOR = 7;  // EXTI Line 1 Interrupt
  EXTI2_VECTOR = 8;  // EXTI Line 2 Interrupt
  EXTI3_VECTOR = 9;  // EXTI Line 3 Interrupt
  EXTI4_VECTOR = 10;  // EXTI Line 4 Interrupt
  DMA1_CHANNEL1_VECTOR = 11;  // DMA1 Channel 1 Interrupt
  DMA1_CHANNEL2_VECTOR = 12;  // DMA1 Channel 2 Interrupt
  DMA1_CHANNEL3_VECTOR = 13;  // DMA1 Channel 3 Interrupt
  DMA1_CHANNEL4_VECTOR = 14;  // DMA1 Channel 4 Interrupt
  DMA1_CHANNEL5_VECTOR = 15;  // DMA1 Channel 5 Interrupt
  DMA1_CHANNEL6_VECTOR = 16;  // DMA1 Channel 6 Interrupt
  DMA1_CHANNEL7_VECTOR = 17;  // DMA1 Channel 7 Interrupt
  ADC1_2_VECTOR = 18;  // ADC1 and ADC2 Global Interrupt
  USB_HP_CAN_TX_VECTOR = 19;  // USB HP/CAN TX Interrupts
  USB_LP_CAN_RX0_VECTOR = 20;  // USB LP/CAN RX0 Interrupt
  CAN_RX1_VECTOR = 21;  // CAN RX1 Interrupt
  CAN_SCE_VECTOR = 22;  // CAN SCE Interrupt
  EXTI9_5_VECTOR = 23;  // EXTI Line 9..5 Interrupt
  TIM1_BRK_VECTOR = 25;  // TIM1 Break Interrupt
  TIM1_UP_VECTOR = 26;  // TIM1 Update Interrupt
  TIM1_TRG_COM_VECTOR = 27;  // TIM1 Trigger and Commutation
  TIM1_CC_VECTOR = 28;  // TIM1 Capture Compare Interrupt
  TIM2_VECTOR = 29;  // TIM2 Global Interrupt
  TIM3_VECTOR = 30;  // TIM3 Global Interrupt
  TIM4_VECTOR = 31;  // TIM4 Global Interrupt
  I2C1_EV_VECTOR = 32;  // I2C1 Event Interrupt
  I2C1_ER_VECTOR = 33;  // I2C1 Error Interrupt
  I2C2_EV_VECTOR = 34;  // I2C2 Event Interrupt
  I2C2_ER_VECTOR = 35;  // I2C2 Error Interrupt
  SPI1_VECTOR = 35;  // SPI1 Global Interrupt
  SPI2_VECTOR = 36;  // SPI2 Global Interrupt
  USART1_VECTOR = 37;  // USART1 Global Interrupt
  USART2_VECTOR = 38;  // USART2 Global Interrupt
  USART3_VECTOR = 39;  // USART3 Global Interrupt
  EXTI15_10_VECTOR = 40;  // EXTI Line 15..10 Interrupt
  RTCALARM_VECTOR = 41;  // RTC Alarm through EXTI
  USBWAKEUP_VECTOR = 42;  // USB Wakeup from suspend

  // 引脚定义
  PIN_VBAT = 1;  // Battery Supply
  PIN_PC13 = 2;  // GPIO Port C Pin 13
  PIN_PC14 = 3;  // GPIO Port C Pin 14
  PIN_PC15 = 4;  // GPIO Port C Pin 15
  PIN_PD0 = 5;  // GPIO Port D Pin 0
  PIN_PD1 = 6;  // GPIO Port D Pin 1
  PIN_NRST = 7;  // Reset
  PIN_VSSA = 8;  // Analog Ground
  PIN_VDDA = 9;  // Analog Supply
  PIN_PA0 = 10;  // GPIO Port A Pin 0 / ADC1_IN0
  PIN_PA1 = 11;  // GPIO Port A Pin 1 / ADC1_IN1
  PIN_PA2 = 12;  // GPIO Port A Pin 2 / ADC1_IN2 / USART2_TX
  PIN_PA3 = 13;  // GPIO Port A Pin 3 / ADC1_IN3 / USART2_RX
  PIN_PA4 = 14;  // GPIO Port A Pin 4 / DAC_OUT1 / SPI1_NSS
  PIN_PA5 = 15;  // GPIO Port A Pin 5 / DAC_OUT2 / SPI1_SCK
  PIN_PA6 = 16;  // GPIO Port A Pin 6 / ADC1_IN6 / SPI1_MISO / TIM3_CH1
  PIN_PA7 = 17;  // GPIO Port A Pin 7 / ADC1_IN7 / SPI1_MOSI / TIM3_CH2
  PIN_PB0 = 18;  // GPIO Port B Pin 0 / ADC1_IN8 / TIM3_CH3
  PIN_PB1 = 19;  // GPIO Port B Pin 1 / ADC1_IN9 / TIM3_CH4
  PIN_PB2 = 20;  // GPIO Port B Pin 2
  PIN_PB10 = 21;  // GPIO Port B Pin 10 / I2C2_SCL / USART3_TX
  PIN_PB11 = 22;  // GPIO Port B Pin 11 / I2C2_SDA / USART3_RX
  PIN_VSS = 23;  // Ground
  PIN_VDD = 24;  // Digital Supply
  PIN_PB12 = 25;  // GPIO Port B Pin 12 / SPI2_NSS / I2C2_SMBA
  PIN_PB13 = 26;  // GPIO Port B Pin 13 / SPI2_SCK / USART3_CK
  PIN_PB14 = 27;  // GPIO Port B Pin 14 / SPI2_MISO / USART3_RTS
  PIN_PB15 = 28;  // GPIO Port B Pin 15 / SPI2_MOSI / USART3_CTS
  PIN_PA8 = 29;  // GPIO Port A Pin 8 / USART1_CK / TIM1_CH1 / MCO
  PIN_PA9 = 30;  // GPIO Port A Pin 9 / USART1_TX / TIM1_CH2
  PIN_PA10 = 31;  // GPIO Port A Pin 10 / USART1_RX / TIM1_CH3
  PIN_PA11 = 32;  // GPIO Port A Pin 11 / USART1_CT / TIM1_CH4 / CAN_RX
  PIN_PA12 = 33;  // GPIO Port A Pin 12 / USART1_RT / TIM1_ETR / CAN_TX
  PIN_PA13 = 34;  // JTMS/SWDIO
  PIN_PA14 = 37;  // JTCK/SWCLK
  PIN_PA15 = 38;  // GPIO Port A Pin 15 / JTDI / TIM2_CH1_ETR / SPI1_NSS
  PIN_PB3 = 39;  // GPIO Port B Pin 3 / JTDO / TIM2_CH2 / SPI1_SCK
  PIN_PB4 = 40;  // GPIO Port B Pin 4 / JNTRST / TIM3_CH1 / SPI1_MISO
  PIN_PB5 = 41;  // GPIO Port B Pin 5 / TIM3_CH2 / SPI1_MOSI / I2C1_SMBA
  PIN_PB6 = 42;  // GPIO Port B Pin 6 / TIM4_CH1 / I2C1_SCL / USART1_TX
  PIN_PB7 = 43;  // GPIO Port B Pin 7 / TIM4_CH2 / I2C1_SDA / USART1_RX
  PIN_BOOT0 = 44;  // Boot Selection
  PIN_PB8 = 45;  // GPIO Port B Pin 8 / TIM4_CH3 / I2C1_SCL / CAN_RX
  PIN_PB9 = 46;  // GPIO Port B Pin 9 / TIM4_CH4 / I2C1_SDA / CAN_TX
  PIN_VSS = 47;  // Ground
  PIN_VDD = 48;  // Digital Supply

type
  TSTM32F103C8T6 = record
    // 设备状态记录
  end;

// 设备初始化函数
procedure stm32f103c8t6_init;

// 常用函数
function read_register(addr: Word): Byte;
procedure write_register(addr: Word; value: Byte);

implementation

procedure stm32f103c8t6_init;
begin
  // 初始化代码
end;

function read_register(addr: Word): Byte;
begin
  // 读取寄存器值
  Result := 0;
end;

procedure write_register(addr: Word; value: Byte);
begin
  // 写入寄存器值
end;

end.
