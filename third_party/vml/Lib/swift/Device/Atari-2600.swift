//
// MOS-6507 Register Definitions
// Generated from: Atari 2600 VCS main processor - MOS 6507 (simplified 6502) @ 1.19MHz with TIA and RIOT
// Version: 1.0
// Date: 2026-04-16
//

import Foundation

// MARK: - TIA (Television Interface Adaptor (Video + Audio + I/O))
let TIA_VSYNC: UInt8 = 0x0x00
let TIA_VBLANK: UInt8 = 0x0x01
let TIA_WSYNC: UInt8 = 0x0x02
let TIA_RSYNC: UInt8 = 0x0x03
let TIA_NUSIZ0: UInt8 = 0x0x04
let TIA_NUSIZ1: UInt8 = 0x0x05
let TIA_COLUP0: UInt8 = 0x0x06
let TIA_COLUP1: UInt8 = 0x0x07
let TIA_COLUPF: UInt8 = 0x0x08
let TIA_COLUBK: UInt8 = 0x0x09
let TIA_CTRLPF: UInt8 = 0x0x0A
let TIA_REFPL: UInt8 = 0x0x0B
let TIA_PF0: UInt8 = 0x0x0D
let TIA_PF1: UInt8 = 0x0x0E
let TIA_PF2: UInt8 = 0x0x0F
let TIA_RESP0: UInt8 = 0x0x10
let TIA_RESP1: UInt8 = 0x0x11
let TIA_RESM0: UInt8 = 0x0x12
let TIA_RESM1: UInt8 = 0x0x13
let TIA_RESBL: UInt8 = 0x0x14
let TIA_AUDC0: UInt8 = 0x0x15
let TIA_AUDC1: UInt8 = 0x0x16
let TIA_AUDF0: UInt8 = 0x0x17
let TIA_AUDF1: UInt8 = 0x0x18
let TIA_AUDV0: UInt8 = 0x0x19
let TIA_AUDV1: UInt8 = 0x0x1A
let TIA_GRP0: UInt8 = 0x0x1B
let TIA_GRP1: UInt8 = 0x0x1C
let TIA_DGRP0: UInt8 = 0x0x1D
let TIA_DGRP1: UInt8 = 0x0x1E
let TIA_ENAM0: UInt8 = 0x0x1F
let TIA_ENAM1: UInt8 = 0x0x20
let TIA_ENABL: UInt8 = 0x0x21
let TIA_HMP0: UInt8 = 0x0x22
let TIA_HMP1: UInt8 = 0x0x23
let TIA_HMM0: UInt8 = 0x0x24
let TIA_HMM1: UInt8 = 0x0x25
let TIA_HMBL: UInt8 = 0x0x26
let TIA_VDEL0: UInt8 = 0x0x27
let TIA_VDEL1: UInt8 = 0x0x28
let TIA_VDELBL: UInt8 = 0x0x29
let TIA_RESBB: UInt8 = 0x0x2A
let TIA_HMOVE: UInt8 = 0x0x2A
let TIA_HMCLR: UInt8 = 0x0x2B
let TIA_CXM0P: UInt8 = 0x0x30
let TIA_CXM1P: UInt8 = 0x0x31
let TIA_CXP0FB: UInt8 = 0x0x32
let TIA_CXP1FB: UInt8 = 0x0x33
let TIA_CXM0FB: UInt8 = 0x0x34
let TIA_CXM1FB: UInt8 = 0x0x35
let TIA_CXBLPF: UInt8 = 0x0x36
let TIA_CXPPMM: UInt8 = 0x0x37
let TIA_INPT0: UInt8 = 0x0x38
let TIA_INPT1: UInt8 = 0x0x39
let TIA_INPT2: UInt8 = 0x0x3A
let TIA_INPT3: UInt8 = 0x0x3B
let TIA_INPT4: UInt8 = 0x0x3C
let TIA_INPT5: UInt8 = 0x0x3D

// MARK: - RIOT (RAM, I/O, Timer (6532 RIOT))
let RIOT_SWCHA: UInt8 = 0x0x280
let RIOT_SWACNT: UInt8 = 0x0x281
let RIOT_SWCHB: UInt8 = 0x0x282
let RIOT_SWBCNT: UInt8 = 0x0x283
let RIOT_INTIM: UInt8 = 0x0x284
let RIOT_TIMINT: UInt8 = 0x0x285
let RIOT_TIM1T: UInt8 = 0x0x294
let RIOT_TIM8T: UInt8 = 0x0x295
let RIOT_TIM64T: UInt8 = 0x0x296
let RIOT_TIM1024T: UInt8 = 0x0x297

// MARK: - CONTROLLER1 (Controller Port 1 (Joystick))
let CONTROLLER1_SWCHA: UInt8 = 0x0x280

// MARK: - CONTROLLER2 (Controller Port 2 (Joystick))
let CONTROLLER2_SWCHA: UInt8 = 0x0x280

// MARK: - Interrupt Vectors
let IRQ_RESET: Int = 0

// MARK: - Memory Segments
let MEM_tia_regs: (start: UInt32, size: UInt32) = (0x0x0000, 128)
let MEM_riot_ram: (start: UInt32, size: UInt32) = (0x0x0080, 128)
let MEM_riot_io: (start: UInt32, size: UInt32) = (0x0x0280, 32)
let MEM_cart_rom: (start: UInt32, size: UInt32) = (0x0x1000, 4096)

// MARK: - Device Functions
func mos_6507_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
