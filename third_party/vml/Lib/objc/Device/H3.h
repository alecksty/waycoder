// Allwinner H3 设备定义 - Objective-C 头文件
// 生成自: Allwinner/H-Series/Allwinner H3
// 版本: 1.0
// 日期: 2026-04-29
// 作者: VML Team
// 描述: 32-bit ARM Cortex-A7 Quad-core SoC with 512KB L2 Cache, 1.6GHz, Mali-400 GPU
// CPU架构: ARM-Cortex-A7
// 位宽: 32位
// 时钟频率: 1200000000 Hz

#ifndef ALLWINNER H3_DEVICE_H
#define ALLWINNER H3_DEVICE_H

#import <Foundation/Foundation.h>

// 外设定义
// UART 0 (debug console)
#define UART0_BASE 0x01C28000
#define UART0_RBR_ADDR 0x00
#define UART0_THR_ADDR 0x00
#define UART0_IER_ADDR 0x04
#define UART0_IIR_ADDR 0x08
#define UART0_FCR_ADDR 0x08
#define UART0_LCR_ADDR 0x0C
#define UART0_MCR_ADDR 0x10
#define UART0_LSR_ADDR 0x14
#define UART0_MSR_ADDR 0x18
#define UART0_DLL_ADDR 0x00
#define UART0_DLH_ADDR 0x04
// UART 1
#define UART1_BASE 0x01C28400
#define UART1_RBR_ADDR 0x00
#define UART1_THR_ADDR 0x00
#define UART1_LSR_ADDR 0x14
// GPIO 控制器
#define GPIO_BASE 0x01C20800
#define GPIO_PA_CFG0_ADDR 0x00
#define GPIO_PA_CFG1_ADDR 0x04
#define GPIO_PA_DAT_ADDR 0x10
#define GPIO_PA_DRV0_ADDR 0x14
#define GPIO_PA_PUL0_ADDR 0x1C
#define GPIO_PB_CFG0_ADDR 0x24
#define GPIO_PB_DAT_ADDR 0x34
#define GPIO_PC_CFG0_ADDR 0x48
#define GPIO_PC_DAT_ADDR 0x58
// AVS 定时器
#define TIMER_BASE 0x01C20C00
#define TIMER_CNT0_ADDR 0x00
#define TIMER_CNT1_ADDR 0x04
#define TIMER_CTRL_ADDR 0x08
#define TIMER_INTV_ADDR 0x0C
// 时钟控制单元
#define CCU_BASE 0x01C20000
#define CCU_PLL1_CFG_ADDR 0x000
#define CCU_PLL3_CFG_ADDR 0x010
#define CCU_CPU_AXI_CFG_ADDR 0x050
#define CCU_AHB1_APB1_CFG_ADDR 0x054
#define CCU_APB2_CFG_ADDR 0x058
#define CCU_BUS_GATE0_ADDR 0x060
#define CCU_BUS_GATE1_ADDR 0x064
#define CCU_BUS_GATE2_ADDR 0x068

#endif /* ALLWINNER H3_DEVICE_H */
