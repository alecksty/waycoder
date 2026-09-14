#ifndef ALLWINNER_H3_HPP
#define ALLWINNER_H3_HPP

// Allwinner H3寄存器定义
// 生成自: Allwinner/H-Series/Allwinner H3
// 版本: 1.0
// 日期: 2026-04-29


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: ARM-Cortex-A7
// 位宽: 32位
// 时钟频率: 1200000000 Hz

// 外设定义
// UART 0 (debug console)
#define UART0_BASE 0x01C28000
#define UART0_RBR (*(volatile uint32_t*)0x01C28000)
#define UART0_THR (*(volatile uint32_t*)0x01C28000)
#define UART0_IER (*(volatile uint32_t*)0x01C28004)
#define UART0_IIR (*(volatile uint32_t*)0x01C28008)
#define UART0_FCR (*(volatile uint32_t*)0x01C28008)
#define UART0_LCR (*(volatile uint32_t*)0x01C2800C)
#define UART0_MCR (*(volatile uint32_t*)0x01C28010)
#define UART0_LSR (*(volatile uint32_t*)0x01C28014)
#define UART0_MSR (*(volatile uint32_t*)0x01C28018)
#define UART0_DLL (*(volatile uint32_t*)0x01C28000)
#define UART0_DLH (*(volatile uint32_t*)0x01C28004)

// UART 1
#define UART1_BASE 0x01C28400
#define UART1_RBR (*(volatile uint32_t*)0x01C28400)
#define UART1_THR (*(volatile uint32_t*)0x01C28400)
#define UART1_LSR (*(volatile uint32_t*)0x01C28414)

// GPIO 控制器
#define GPIO_BASE 0x01C20800
#define GPIO_PA_CFG0 (*(volatile uint32_t*)0x01C20800)
#define GPIO_PA_CFG1 (*(volatile uint32_t*)0x01C20804)
#define GPIO_PA_DAT (*(volatile uint32_t*)0x01C20810)
#define GPIO_PA_DRV0 (*(volatile uint32_t*)0x01C20814)
#define GPIO_PA_PUL0 (*(volatile uint32_t*)0x01C2081C)
#define GPIO_PB_CFG0 (*(volatile uint32_t*)0x01C20824)
#define GPIO_PB_DAT (*(volatile uint32_t*)0x01C20834)
#define GPIO_PC_CFG0 (*(volatile uint32_t*)0x01C20848)
#define GPIO_PC_DAT (*(volatile uint32_t*)0x01C20858)

// AVS 定时器
#define TIMER_BASE 0x01C20C00
#define TIMER_CNT0 (*(volatile uint32_t*)0x01C20C00)
#define TIMER_CNT1 (*(volatile uint32_t*)0x01C20C04)
#define TIMER_CTRL (*(volatile uint32_t*)0x01C20C08)
#define TIMER_INTV (*(volatile uint32_t*)0x01C20C0C)

// 时钟控制单元
#define CCU_BASE 0x01C20000
#define CCU_PLL1_CFG (*(volatile uint32_t*)0x01C20000)
#define CCU_PLL3_CFG (*(volatile uint32_t*)0x01C20010)
#define CCU_CPU_AXI_CFG (*(volatile uint32_t*)0x01C20050)
#define CCU_AHB1_APB1_CFG (*(volatile uint32_t*)0x01C20054)
#define CCU_APB2_CFG (*(volatile uint32_t*)0x01C20058)
#define CCU_BUS_GATE0 (*(volatile uint32_t*)0x01C20060)
#define CCU_BUS_GATE1 (*(volatile uint32_t*)0x01C20064)
#define CCU_BUS_GATE2 (*(volatile uint32_t*)0x01C20068)

void allwinner_h3_init(void);

#ifdef __cplusplus
}
#endif

#endif // ALLWINNER_H3_HPP
