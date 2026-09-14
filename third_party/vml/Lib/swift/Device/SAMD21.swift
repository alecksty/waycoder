//
// SAMD21 Register Definitions
// Generated from: Atmel SAM D21 ARM Cortex-M0+ based microcontroller
// Version: 
// Date: 
//

import Foundation

// MARK: - PM (Power Manager)
let PM_PM_CTRL: UInt64 = 0x0x40000400

// MARK: - SYSCTRL (System Controller)
let SYSCTRL_SYSCTRL_INTENCLR: UInt32 = 0x0x40000800

// MARK: - GCLK (Generic Clock Generator)
let GCLK_GCLK_CTRL: UInt64 = 0x0x40000C00

// MARK: - WDT (Watchdog Timer)
let WDT_WDT_CTRL: UInt64 = 0x0x40001000

// MARK: - RTC (Real-Time Clock)
let RTC_RTC_CTRL: UInt32 = 0x0x40001400

// MARK: - EIC (External Interrupt Controller)
let EIC_EIC_CTRL: UInt64 = 0x0x40001800

// MARK: - SERCOM0 (Serial Communication Interface 0)
let SERCOM0_SERCOM0_I2CM_CTRLA: UInt32 = 0x0x42000800

// MARK: - ADC (Analog-to-Digital Converter)
let ADC_ADC_CTRLA: UInt64 = 0x0x42002000

// MARK: - DAC (Digital-to-Analog Converter)
let DAC_DAC_CTRLA: UInt64 = 0x0x42002400

// MARK: - PORT (General Purpose I/O)
let PORT_PORT_DIR: UInt32 = 0x0x41004400

// MARK: - TC0 (Timer/Counter 0)
let TC0_TC0_CTRLA: UInt32 = 0x0x42002800

// MARK: - USB (USB Device Controller)
let USB_USB_CTRLA: UInt64 = 0x0x41005000

// MARK: - Interrupt Vectors
let IRQ_Reset: Int = 0
let IRQ_NonMaskableInt: Int = 1
let IRQ_HardFault: Int = 2
let IRQ_SVCall: Int = 3
let IRQ_PendSV: Int = 4
let IRQ_SysTick: Int = 5
let IRQ_PM: Int = 6
let IRQ_SYSCTRL: Int = 7
let IRQ_WDT: Int = 8
let IRQ_RTC: Int = 9
let IRQ_EIC: Int = 10
let IRQ_NVMCTRL: Int = 11
let IRQ_DMAC: Int = 12
let IRQ_USB: Int = 13
let IRQ_EVSYS: Int = 14
let IRQ_SERCOM0: Int = 15
let IRQ_SERCOM1: Int = 16
let IRQ_SERCOM2: Int = 17
let IRQ_SERCOM3: Int = 18
let IRQ_SERCOM4: Int = 19
let IRQ_SERCOM5: Int = 20
let IRQ_TCC0: Int = 21
let IRQ_TCC1: Int = 22
let IRQ_TCC2: Int = 23
let IRQ_TC3: Int = 24
let IRQ_TC4: Int = 25
let IRQ_TC5: Int = 26
let IRQ_TC6: Int = 27
let IRQ_TC7: Int = 28
let IRQ_ADC: Int = 29
let IRQ_AC: Int = 30
let IRQ_DAC: Int = 31
let IRQ_PTC: Int = 32
let IRQ_I2S: Int = 33

// MARK: - Memory Segments

// MARK: - Device Functions
func samd21_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
