#ifndef COMMODORE_PET_HPP
#define COMMODORE_PET_HPP

// Commodore-PET寄存器定义
// 生成自: Commodore International/PET/Commodore-PET
// 版本: 1.0
// 日期: 2026-04-17


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: MOS 6502
// 位宽: 8位
// 时钟频率: 1000000 Hz

// 寄存器定义
// Accumulator
#define A (*(volatile uint8_t*)0)

// Index Register X
#define X (*(volatile uint8_t*)0)

// Index Register Y
#define Y (*(volatile uint8_t*)0)

// Stack Pointer
#define SP (*(volatile uint8_t*)0)

// Program Counter
#define PC (*(volatile uint16_t*)0)

// Status Register
#define P (*(volatile uint8_t*)0)

// 外设定义
// Peripheral Interface Adapter 1 (6520)
#define PIA1_BASE 
#define PIA1_PIA1_DDRA (*(volatile uint8_t*)0x0000E810)
#define PIA1_PIA1_ORA (*(volatile uint8_t*)0x0000E811)
#define PIA1_PIA1_DDRB (*(volatile uint8_t*)0x0000E812)
#define PIA1_PIA1_ORB (*(volatile uint8_t*)0x0000E813)
#define PIA1_PIA1_CRA (*(volatile uint8_t*)0x0000E814)
#define PIA1_PIA1_CRB (*(volatile uint8_t*)0x0000E815)

// Peripheral Interface Adapter 2 (6520)
#define PIA2_BASE 
#define PIA2_PIA2_DDRA (*(volatile uint8_t*)0x0000E820)
#define PIA2_PIA2_ORA (*(volatile uint8_t*)0x0000E821)
#define PIA2_PIA2_DDRB (*(volatile uint8_t*)0x0000E822)
#define PIA2_PIA2_ORB (*(volatile uint8_t*)0x0000E823)
#define PIA2_PIA2_CRA (*(volatile uint8_t*)0x0000E824)
#define PIA2_PIA2_CRB (*(volatile uint8_t*)0x0000E825)

// Versatile Interface Adapter (6522)
#define VIA_BASE 
#define VIA_VIA_ORB (*(volatile uint8_t*)0x0000E840)
#define VIA_VIA_ORA (*(volatile uint8_t*)0x0000E841)
#define VIA_VIA_DDRB (*(volatile uint8_t*)0x0000E842)
#define VIA_VIA_DDRA (*(volatile uint8_t*)0x0000E843)
#define VIA_VIA_T1CL (*(volatile uint8_t*)0x0000E844)
#define VIA_VIA_T1CH (*(volatile uint8_t*)0x0000E845)
#define VIA_VIA_T1LL (*(volatile uint8_t*)0x0000E846)
#define VIA_VIA_T1LH (*(volatile uint8_t*)0x0000E847)
#define VIA_VIA_T2CL (*(volatile uint8_t*)0x0000E848)
#define VIA_VIA_T2CH (*(volatile uint8_t*)0x0000E849)
#define VIA_VIA_SR (*(volatile uint8_t*)0x0000E84A)
#define VIA_VIA_ACR (*(volatile uint8_t*)0x0000E84B)
#define VIA_VIA_PCR (*(volatile uint8_t*)0x0000E84C)
#define VIA_VIA_IFR (*(volatile uint8_t*)0x0000E84D)
#define VIA_VIA_IER (*(volatile uint8_t*)0x0000E84E)

// CRT Controller (6545)
#define CRTC_BASE 
#define CRTC_CRTC_ADDR (*(volatile uint8_t*)0x0000E880)
#define CRTC_CRTC_DATA (*(volatile uint8_t*)0x0000E881)

// Cassette tape interface
#define CASSETTE_BASE 
#define CASSETTE_CASS_MOTOR (*(volatile uint8_t*)0x0000E840)
#define CASSETTE_CASS_WRITE (*(volatile uint8_t*)0x0000E842)
#define CASSETTE_CASS_READ (*(volatile uint8_t*)0x0000E812)

// IEEE-488 bus interface
#define IEEE488_BASE 
#define IEEE488_IEEE_DATA (*(volatile uint8_t*)0x0000E801)
#define IEEE488_IEEE_STATUS (*(volatile uint8_t*)0x0000E802)
#define IEEE488_IEEE_CONTROL (*(volatile uint8_t*)0x0000E803)

// 中断向量定义
#define NMI_VECTOR 65526  // Non-maskable interrupt
#define RESET_VECTOR 65528  // Reset vector
#define IRQ_VECTOR 65530  // Interrupt request
#define BRK_VECTOR 65532  // Break instruction

void commodore_pet_init(void);

#ifdef __cplusplus
}
#endif

#endif // COMMODORE_PET_HPP
