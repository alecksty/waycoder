#ifndef MACINTOSH_128K_HPP
#define MACINTOSH_128K_HPP

// Macintosh-128K寄存器定义
// 生成自: Apple Computer/Macintosh/Macintosh-128K
// 版本: 1.0
// 日期: 2026-04-17


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Motorola 68000
// 位宽: 32位
// 时钟频率: 7998000 Hz

// 寄存器定义
// Data Register 0
#define D0 (*(volatile uint32_t*)0)

// Data Register 1
#define D1 (*(volatile uint32_t*)0)

// Data Register 2
#define D2 (*(volatile uint32_t*)0)

// Data Register 3
#define D3 (*(volatile uint32_t*)0)

// Data Register 4
#define D4 (*(volatile uint32_t*)0)

// Data Register 5
#define D5 (*(volatile uint32_t*)0)

// Data Register 6
#define D6 (*(volatile uint32_t*)0)

// Data Register 7
#define D7 (*(volatile uint32_t*)0)

// Address Register 0
#define A0 (*(volatile uint32_t*)0)

// Address Register 1
#define A1 (*(volatile uint32_t*)0)

// Address Register 2
#define A2 (*(volatile uint32_t*)0)

// Address Register 3
#define A3 (*(volatile uint32_t*)0)

// Address Register 4
#define A4 (*(volatile uint32_t*)0)

// Address Register 5
#define A5 (*(volatile uint32_t*)0)

// Address Register 6
#define A6 (*(volatile uint32_t*)0)

// Address Register 7 (SP)
#define A7 (*(volatile uint32_t*)0)

// Program Counter
#define PC (*(volatile uint32_t*)0)

// Status Register
#define SR (*(volatile uint16_t*)0)

// 外设定义
// Versatile Interface Adapter (6522)
#define VIA_BASE 
#define VIA_VIA_ORB (*(volatile uint8_t*)0x00E80000)
#define VIA_VIA_ORA (*(volatile uint8_t*)0x00E80001)
#define VIA_VIA_DDRB (*(volatile uint8_t*)0x00E80002)
#define VIA_VIA_DDRA (*(volatile uint8_t*)0x00E80003)
#define VIA_VIA_T1CL (*(volatile uint8_t*)0x00E80004)
#define VIA_VIA_T1CH (*(volatile uint8_t*)0x00E80005)
#define VIA_VIA_T1LL (*(volatile uint8_t*)0x00E80006)
#define VIA_VIA_T1LH (*(volatile uint8_t*)0x00E80007)
#define VIA_VIA_T2CL (*(volatile uint8_t*)0x00E80008)
#define VIA_VIA_T2CH (*(volatile uint8_t*)0x00E80009)
#define VIA_VIA_SR (*(volatile uint8_t*)0x00E8000A)
#define VIA_VIA_ACR (*(volatile uint8_t*)0x00E8000B)
#define VIA_VIA_PCR (*(volatile uint8_t*)0x00E8000C)
#define VIA_VIA_IFR (*(volatile uint8_t*)0x00E8000D)
#define VIA_VIA_IER (*(volatile uint8_t*)0x00E8000E)
#define VIA_VIA_ORA2 (*(volatile uint8_t*)0x00E8000F)

// Integrated Woz Machine (floppy controller)
#define IWM_BASE 
#define IWM_IWM_Q6 (*(volatile uint8_t*)0x00D00000)
#define IWM_IWM_Q7 (*(volatile uint8_t*)0x00D00002)
#define IWM_IWM_PH0 (*(volatile uint8_t*)0x00D00004)
#define IWM_IWM_PH1 (*(volatile uint8_t*)0x00D00006)
#define IWM_IWM_PH2 (*(volatile uint8_t*)0x00D00008)
#define IWM_IWM_PH3 (*(volatile uint8_t*)0x00D0000A)

// Zilog 8530 Serial Communications Controller
#define SCC_BASE 
#define SCC_SCC_CA (*(volatile uint8_t*)0x00500000)
#define SCC_SCC_DA (*(volatile uint8_t*)0x00500002)
#define SCC_SCC_CB (*(volatile uint8_t*)0x00500004)
#define SCC_SCC_DB (*(volatile uint8_t*)0x00500006)

// Built-in speaker
#define SOUND_BASE 
#define SOUND_SOUND_VOL (*(volatile uint8_t*)0x00E80100)
#define SOUND_SOUND_FREQ (*(volatile uint8_t*)0x00E80102)

// 中断向量定义
#define RESET_SP_VECTOR 0  // Reset (Initial SP)
#define RESET_PC_VECTOR 4  // Reset (Initial PC)
#define AUTOVECTOR1_VECTOR 24  // Auto vector 1
#define AUTOVECTOR2_VECTOR 25  // Auto vector 2
#define AUTOVECTOR3_VECTOR 26  // Auto vector 3
#define AUTOVECTOR4_VECTOR 27  // Auto vector 4
#define AUTOVECTOR5_VECTOR 28  // Auto vector 5
#define AUTOVECTOR6_VECTOR 29  // Auto vector 6
#define AUTOVECTOR7_VECTOR 30  // Auto vector 7
#define SPURIOUS_VECTOR 31  // Spurious interrupt

void macintosh_128k_init(void);

#ifdef __cplusplus
}
#endif

#endif // MACINTOSH_128K_HPP
