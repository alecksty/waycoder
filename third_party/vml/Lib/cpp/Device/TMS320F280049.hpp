#ifndef TMS320F280049_HPP
#define TMS320F280049_HPP

// TMS320F280049寄存器定义
// 生成自: Texas Instruments/C2000/TMS320F280049
// 版本: 1.0
// 日期: 2026-04-28


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: C28x-DSP
// 位宽: 32位
// 时钟频率: 100000000 Hz

// 寄存器定义
// Accumulator Low
#define AL (*(volatile uint16_t*)0x00)

// Accumulator High
#define AH (*(volatile uint16_t*)0x02)

// Product High
#define PH (*(volatile uint16_t*)0x04)

// Product Low
#define PL (*(volatile uint16_t*)0x06)

// Temporary Register
#define TREG (*(volatile uint16_t*)0x08)

#define AR0 (*(volatile uint16_t*)0x0A)

#define AR1 (*(volatile uint16_t*)0x0C)

// Status 0
#define ST0 (*(volatile uint16_t*)0x20)

// Status 1
#define ST1 (*(volatile uint16_t*)0x22)

// Program Counter
#define PC (*(volatile uint16_t*)0x24)

// Stack Pointer
#define SP (*(volatile uint16_t*)0x26)

// 内存段定义
#define FLASH_START 0x080000
#define FLASH_END 0x0BFFFF
#define FLASH_SIZE 262144

// Local Shared RAM
#define SRAM_LS_START 0x008000
#define SRAM_LS_END 0x00BFFF
#define SRAM_LS_SIZE 16384

// Global Shared RAM
#define SRAM_GS_START 0x00C000
#define SRAM_GS_END 0x01FFFF
#define SRAM_GS_SIZE 81920

#define PERIPHERAL_START 0x400000
#define PERIPHERAL_END 0x40FFFF
#define PERIPHERAL_SIZE 65536

// 外设定义
// PLL Clock Control
#define PLL_BASE 0x5C10
#define PLL_SYSPLLCTL1 (*(volatile uint16_t*)0x00005C10)
#define PLL_SYSPLLCTL2 (*(volatile uint16_t*)0x00005C12)
#define PLL_CLKSRCCTL1 (*(volatile uint16_t*)0x00005C14)
#define PLL_CLKSRCCTL2 (*(volatile uint16_t*)0x00005C16)

// GPIO Control Registers
#define GPIO_CTRL_BASE 0x7C00
#define GPIO_CTRL_GPACTRL (*(volatile uint16_t*)0x00007C00)
#define GPIO_CTRL_GPAQSEL1 (*(volatile uint16_t*)0x00007C02)
#define GPIO_CTRL_GPAQSEL2 (*(volatile uint16_t*)0x00007C04)
#define GPIO_CTRL_GPAMUX1 (*(volatile uint16_t*)0x00007C06)
#define GPIO_CTRL_GPAMUX2 (*(volatile uint16_t*)0x00007C08)
#define GPIO_CTRL_GPADIR (*(volatile uint16_t*)0x00007C0A)
#define GPIO_CTRL_GPAPUD (*(volatile uint16_t*)0x00007C0C)

// GPIO Data Registers
#define GPIO_DATA_BASE 0x7F00
#define GPIO_DATA_GPADAT (*(volatile uint16_t*)0x00007F00)
#define GPIO_DATA_GPASET (*(volatile uint16_t*)0x00007F02)
#define GPIO_DATA_GPACLEAR (*(volatile uint16_t*)0x00007F04)
#define GPIO_DATA_GPATOGGLE (*(volatile uint16_t*)0x00007F06)
#define GPIO_DATA_GPBDAT (*(volatile uint16_t*)0x00007F08)
#define GPIO_DATA_GPBSET (*(volatile uint16_t*)0x00007F0A)
#define GPIO_DATA_GPBCLEAR (*(volatile uint16_t*)0x00007F0C)
#define GPIO_DATA_GPBTOGGLE (*(volatile uint16_t*)0x00007F0E)

// GPIO B Control
#define GPIO_B_CTRL_BASE 0x7C20
#define GPIO_B_CTRL_GPBMUX1 (*(volatile uint16_t*)0x00007C20)
#define GPIO_B_CTRL_GPBMUX2 (*(volatile uint16_t*)0x00007C22)
#define GPIO_B_CTRL_GPBDIR (*(volatile uint16_t*)0x00007C24)
#define GPIO_B_CTRL_GPBPUD (*(volatile uint16_t*)0x00007C26)

// SCI-A UART
#define SCI_A_BASE 0x7320
#define SCI_A_SCICCR (*(volatile uint16_t*)0x00007320)
#define SCI_A_SCICTL1 (*(volatile uint16_t*)0x00007322)
#define SCI_A_SCIBAUD (*(volatile uint16_t*)0x00007324)
#define SCI_A_SCIRXBUF (*(volatile uint16_t*)0x0000732A)
#define SCI_A_SCITXBUF (*(volatile uint16_t*)0x0000732C)

// 中断向量定义
#define RESET_VECTOR 1  // 
#define SCIA_RX_VECTOR 8  // SCI-A Receive Interrupt
#define SCIA_TX_VECTOR 9  // SCI-A Transmit Interrupt

void tms320f280049_init(void);

#ifdef __cplusplus
}
#endif

#endif // TMS320F280049_HPP
