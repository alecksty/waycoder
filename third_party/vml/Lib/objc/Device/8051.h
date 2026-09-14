// 8051 设备定义 - Objective-C 头文件
// 生成自: Intel/MCS-51/8051
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: 8-bit microcontroller with 4KB ROM, 128B RAM, 32 I/O lines
// CPU架构: MCS-51
// 位宽: 8位
// 时钟频率: 11059200 Hz

#ifndef 8051_DEVICE_H
#define 8051_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define ACC_ADDR 0xE0  // Accumulator
#define B_ADDR 0xF0  // B Register
#define PSW_ADDR 0xD0  // Program Status Word
#define PSW_P_BIT 0  // Parity Flag
#define PSW_OV_BIT 2  // Overflow Flag
#define PSW_RS0_BIT 3  // Register Bank Select 0
#define PSW_RS1_BIT 4  // Register Bank Select 1
#define PSW_F0_BIT 5  // Flag 0
#define PSW_AC_BIT 6  // Auxiliary Carry Flag
#define PSW_CY_BIT 7  // Carry Flag
#define SP_ADDR 0x81  // Stack Pointer
#define DPTR_ADDR 0x82  // Data Pointer (DPL/DPH)

// 内存段定义
#define CODE_START 0x0000
#define CODE_END 0x0FFF
#define CODE_SIZE 4096  // Program Memory
#define IDATA_START 0x00
#define IDATA_END 0x7F
#define IDATA_SIZE 128  // Internal Data Memory
#define SFR_START 0x80
#define SFR_END 0xFF
#define SFR_SIZE 128  // Special Function Registers
#define XDATA_START 0x0000
#define XDATA_END 0xFFFF
#define XDATA_SIZE 65536  // External Data Memory

// 外设定义
// Port 0
#define PORT0_BASE 0x80
#define PORT0_P0_ADDR 0x80
// Port 1
#define PORT1_BASE 0x90
#define PORT1_P1_ADDR 0x90
// Port 2
#define PORT2_BASE 0xA0
#define PORT2_P2_ADDR 0xA0
// Port 3
#define PORT3_BASE 0xB0
#define PORT3_P3_ADDR 0xB0
// Timer/Counter 0
#define TIMER0_BASE 0x8A
#define TIMER0_TH0_ADDR 0x8C
#define TIMER0_TL0_ADDR 0x8A
#define TIMER0_TMOD_ADDR 0x89
#define TIMER0_TMOD_M0_0_BIT 0  // Timer 0 Mode bit 0
#define TIMER0_TMOD_M1_0_BIT 1  // Timer 0 Mode bit 1
#define TIMER0_TMOD_C_T0_BIT 2  // Timer 0 Counter/Timer Select
#define TIMER0_TMOD_GATE0_BIT 3  // Timer 0 Gate Control
#define TIMER0_TCON_ADDR 0x88
#define TIMER0_TCON_TR0_BIT 4  // Timer 0 Run Control
#define TIMER0_TCON_TF0_BIT 5  // Timer 0 Overflow Flag
// Serial Port
#define UART_BASE 0x98
#define UART_SBUF_ADDR 0x99
#define UART_SCON_ADDR 0x98
#define UART_SCON_RI_BIT 0  // Receive Interrupt Flag
#define UART_SCON_TI_BIT 1  // Transmit Interrupt Flag
#define UART_SCON_REN_BIT 4  // Receive Enable
#define UART_SCON_SM0_BIT 6  // Serial Mode bit 0
#define UART_SCON_SM1_BIT 7  // Serial Mode bit 1

// 中断向量定义
#define INT_RESET 0  // Reset Vector
#define INT_INT0 1  // External Interrupt 0
#define INT_TIMER0 2  // Timer 0 Interrupt
#define INT_INT1 3  // External Interrupt 1
#define INT_TIMER1 4  // Timer 1 Interrupt
#define INT_UART 5  // Serial Port Interrupt

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

#endif /* 8051_DEVICE_H */
