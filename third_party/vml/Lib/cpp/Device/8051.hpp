#ifndef _8051_HPP
#define _8051_HPP

// 8051寄存器定义
// 生成自: Intel/MCS-51/8051
// 版本: 1.0
// 日期: 2026-04-16


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: MCS-51
// 位宽: 8位
// 时钟频率: 11059200 Hz

// 寄存器定义
// Accumulator
#define ACC (*(volatile uint8_t*)0xE0)

// B Register
#define B (*(volatile uint8_t*)0xF0)

// Program Status Word
#define PSW (*(volatile uint8_t*)0xD0)
#define PSW_P 0  // Parity Flag
#define PSW_OV 2  // Overflow Flag
#define PSW_RS0 3  // Register Bank Select 0
#define PSW_RS1 4  // Register Bank Select 1
#define PSW_F0 5  // Flag 0
#define PSW_AC 6  // Auxiliary Carry Flag
#define PSW_CY 7  // Carry Flag

// Stack Pointer
#define SP (*(volatile uint8_t*)0x81)

// Data Pointer (DPL/DPH)
#define DPTR (*(volatile uint16_t*)0x82)

// 内存段定义
// Program Memory
#define CODE_START 0x0000
#define CODE_END 0x0FFF
#define CODE_SIZE 4096

// Internal Data Memory
#define IDATA_START 0x00
#define IDATA_END 0x7F
#define IDATA_SIZE 128

// Special Function Registers
#define SFR_START 0x80
#define SFR_END 0xFF
#define SFR_SIZE 128

// External Data Memory
#define XDATA_START 0x0000
#define XDATA_END 0xFFFF
#define XDATA_SIZE 65536

// 外设定义
// Port 0
#define PORT0_BASE 0x80
#define PORT0_P0 (*(volatile uint8_t*)0x00000100)

// Port 1
#define PORT1_BASE 0x90
#define PORT1_P1 (*(volatile uint8_t*)0x00000120)

// Port 2
#define PORT2_BASE 0xA0
#define PORT2_P2 (*(volatile uint8_t*)0x00000140)

// Port 3
#define PORT3_BASE 0xB0
#define PORT3_P3 (*(volatile uint8_t*)0x00000160)

// Timer/Counter 0
#define TIMER0_BASE 0x8A
#define TIMER0_TH0 (*(volatile uint8_t*)0x00000116)
#define TIMER0_TL0 (*(volatile uint8_t*)0x00000114)
#define TIMER0_TMOD (*(volatile uint8_t*)0x00000113)
#define TIMER0_TMOD_M0_0 0  // Timer 0 Mode bit 0
#define TIMER0_TMOD_M1_0 1  // Timer 0 Mode bit 1
#define TIMER0_TMOD_C_T0 2  // Timer 0 Counter/Timer Select
#define TIMER0_TMOD_GATE0 3  // Timer 0 Gate Control
#define TIMER0_TCON (*(volatile uint8_t*)0x00000112)
#define TIMER0_TCON_TR0 4  // Timer 0 Run Control
#define TIMER0_TCON_TF0 5  // Timer 0 Overflow Flag

// Serial Port
#define UART_BASE 0x98
#define UART_SBUF (*(volatile uint8_t*)0x00000131)
#define UART_SCON (*(volatile uint8_t*)0x00000130)
#define UART_SCON_RI 0  // Receive Interrupt Flag
#define UART_SCON_TI 1  // Transmit Interrupt Flag
#define UART_SCON_REN 4  // Receive Enable
#define UART_SCON_SM0 6  // Serial Mode bit 0
#define UART_SCON_SM1 7  // Serial Mode bit 1

// 中断向量定义
#define RESET_VECTOR 0  // Reset Vector
#define INT0_VECTOR 1  // External Interrupt 0
#define TIMER0_VECTOR 2  // Timer 0 Interrupt
#define INT1_VECTOR 3  // External Interrupt 1
#define TIMER1_VECTOR 4  // Timer 1 Interrupt
#define UART_VECTOR 5  // Serial Port Interrupt

// 引脚定义
#define PIN_P1_0 1  // Port 1, bit 0
#define PIN_P1_1 2  // Port 1, bit 1
#define PIN_P1_2 3  // Port 1, bit 2
#define PIN_P1_3 4  // Port 1, bit 3
#define PIN_P1_4 5  // Port 1, bit 4
#define PIN_P1_5 6  // Port 1, bit 5
#define PIN_P1_6 7  // Port 1, bit 6
#define PIN_P1_7 8  // Port 1, bit 7
#define PIN_RST 9  // Reset Pin
#define PIN_RX 10  // Serial Receive (P3.0)
#define PIN_TX 11  // Serial Transmit (P3.1)
#define PIN_INT0 12  // External Interrupt 0 (P3.2)
#define PIN_INT1 13  // External Interrupt 1 (P3.3)
#define PIN_T0 14  // Timer 0 Input (P3.4)
#define PIN_T1 15  // Timer 1 Input (P3.5)
#define PIN_WR 16  // External Memory Write Strobe (P3.6)
#define PIN_RD 17  // External Memory Read Strobe (P3.7)
#define PIN_XTAL1 18  // Crystal Oscillator Input
#define PIN_XTAL2 19  // Crystal Oscillator Output
#define PIN_VCC 20  // Power Supply (+5V)
#define PIN_GND 21  // Ground

void _8051_init(void);

#ifdef __cplusplus
}
#endif

#endif // _8051_HPP
