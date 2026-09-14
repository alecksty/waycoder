--[[
  MOS-6507设备定义 - Lua模块
  生成自: MOS Technology/MOS-6502/MOS-6507
  版本: 1.0
  日期: 2026-04-16
  作者: VML Team
  描述: Atari 2600 VCS main processor - MOS 6507 (simplified 6502) @ 1.19MHz with TIA and RIOT
  CPU架构: MOS-6507
  位宽: 8位
  时钟频率: 1190000 Hz
]]

local MOS_6507 = {}

-- 设备信息
MOS_6507.DEVICE_NAME = "MOS-6507"
MOS_6507.MANUFACTURER = "MOS Technology"
MOS_6507.FAMILY = "MOS-6502"
MOS_6507.VERSION = "1.0"
MOS_6507.ARCHITECTURE = "MOS-6507"
MOS_6507.BITS = 8
MOS_6507.CLOCK_FREQUENCY = 1190000

-- 寄存器地址定义
MOS_6507.A_ADDR = 0x00  -- Accumulator
MOS_6507.X_ADDR = 0x01  -- X Index
MOS_6507.Y_ADDR = 0x02  -- Y Index
MOS_6507.SP_ADDR = 0x03  -- Stack Pointer (6-bit, 128-byte stack)
MOS_6507.PC_ADDR = 0x04  -- Program Counter (16-bit)
MOS_6507.P_ADDR = 0x06  -- Processor Status
MOS_6507.P_N_BIT = 7  -- Negative
MOS_6507.P_V_BIT = 6  -- Overflow
MOS_6507.P_B_BIT = 4  -- Break
MOS_6507.P_D_BIT = 3  -- Decimal Mode (N/A on 6507)
MOS_6507.P_I_BIT = 2  -- Interrupt Disable
MOS_6507.P_Z_BIT = 1  -- Zero
MOS_6507.P_C_BIT = 0  -- Carry

-- 内存段定义
MOS_6507.TIA_REGS_START = 0x0000
MOS_6507.TIA_REGS_END = 0x007F
MOS_6507.TIA_REGS_SIZE = 128  -- TIA Registers
MOS_6507.RIOT_RAM_START = 0x0080
MOS_6507.RIOT_RAM_END = 0x00FF
MOS_6507.RIOT_RAM_SIZE = 128  -- RIOT 128byte RAM mirrored
MOS_6507.RIOT_IO_START = 0x0280
MOS_6507.RIOT_IO_END = 0x029F
MOS_6507.RIOT_IO_SIZE = 32  -- RIOT I/O Registers (SWCHA/SWACNT/SWCHB/SWBCNT/INTIM)
MOS_6507.CART_ROM_START = 0x1000
MOS_6507.CART_ROM_END = 0x1FFF
MOS_6507.CART_ROM_SIZE = 4096  -- Cartridge ROM (4KB, bank-switched)

-- 外设定义
-- Television Interface Adaptor (Video + Audio + I/O)
MOS_6507.TIA_BASE = 0x0000
MOS_6507.TIA_VSYNC_ADDR = 0x00
MOS_6507.TIA_VBLANK_ADDR = 0x01
MOS_6507.TIA_VBLANK_D7_BIT = 7  -- Inhibit D7 (1=disable D7 output to PB7)
MOS_6507.TIA_VBLANK_D6_BIT = 6  -- Inhibit D6 (1=disable D6 output to PB6)
MOS_6507.TIA_VBLANK_D5_BIT = 5  -- Inhibit D5 (1=disable D5 output to PB5)
MOS_6507.TIA_VBLANK_D4_BIT = 4  -- Inhibit D4 (1=disable D4 output to PB4)
MOS_6507.TIA_VBLANK_D3_BIT = 3  -- Inhibit D3 (1=disable D3 output to PB3)
MOS_6507.TIA_VBLANK_D2_BIT = 2  -- Inhibit D2 (1=disable D2 output to PB2)
MOS_6507.TIA_VBLANK_D1_BIT = 1  -- Inhibit D1 (1=disable D1 output to PB1)
MOS_6507.TIA_VBLANK_D0_BIT = 0  -- Inhibit D0 (1=disable D0 output to PB0)
MOS_6507.TIA_VBLANK_VBW_BIT = 5  -- Vertical Blank Enable (1=set VBLANK)
MOS_6507.TIA_VBLANK_VBL_BIT = 1  -- Vertical Blank Set (1=V-Blank active)
MOS_6507.TIA_VBLANK_RESBL_BIT = 0  -- Reset Blank (1=allow VSYNC/VBLANK reset on clock)
MOS_6507.TIA_WSYNC_ADDR = 0x02
MOS_6507.TIA_RSYNC_ADDR = 0x03
MOS_6507.TIA_NUSIZ0_ADDR = 0x04
MOS_6507.TIA_NUSIZ0_NUSIZ_BIT = 0  -- Number/Size Code (0-7)
MOS_6507.TIA_NUSIZ0_MISSILE_SIZE_BIT = 0  -- Missile Size
MOS_6507.TIA_NUSIZ0_RESM0_BIT = 6  -- Reset M0
MOS_6507.TIA_NUSIZ0_RESM1_BIT = 7  -- Reset M1
MOS_6507.TIA_NUSIZ1_ADDR = 0x05
MOS_6507.TIA_COLUP0_ADDR = 0x06
MOS_6507.TIA_COLUP1_ADDR = 0x07
MOS_6507.TIA_COLUPF_ADDR = 0x08
MOS_6507.TIA_COLUBK_ADDR = 0x09
MOS_6507.TIA_CTRLPF_ADDR = 0x0A
MOS_6507.TIA_CTRLPF_DELL_BIT = 0  -- Delay Playfield L (Reflected/Left score)
MOS_6507.TIA_CTRLPF_BALL_SIZE_BIT = 0  -- Ball Size (0=1, 1=2, 2=3, 3=4, 4=5, 5=6, 6=7, 7=8 clocks)
MOS_6507.TIA_CTRLPF_REF_BIT = 5  -- Reflect (1=mirror playfield)
MOS_6507.TIA_CTRLPF_SCORE_BIT = 6  -- Score Mode (1=use player colors for L/R halves)
MOS_6507.TIA_CTRLPF_DELBL_BIT = 7  -- Delay Ball (1=delay ball 1 clock)
MOS_6507.TIA_REFPL_ADDR = 0x0B
MOS_6507.TIA_PF0_ADDR = 0x0D
MOS_6507.TIA_PF1_ADDR = 0x0E
MOS_6507.TIA_PF2_ADDR = 0x0F
MOS_6507.TIA_RESP0_ADDR = 0x10
MOS_6507.TIA_RESP1_ADDR = 0x11
MOS_6507.TIA_RESM0_ADDR = 0x12
MOS_6507.TIA_RESM1_ADDR = 0x13
MOS_6507.TIA_RESBL_ADDR = 0x14
MOS_6507.TIA_AUDC0_ADDR = 0x15
MOS_6507.TIA_AUDC0_VOL_BIT = 0  -- Volume (0-15)
MOS_6507.TIA_AUDC0_TONE_BIT = 0  -- Tone Divisor (5-bit counter)
MOS_6507.TIA_AUDC1_ADDR = 0x16
MOS_6507.TIA_AUDF0_ADDR = 0x17
MOS_6507.TIA_AUDF1_ADDR = 0x18
MOS_6507.TIA_AUDV0_ADDR = 0x19
MOS_6507.TIA_AUDV1_ADDR = 0x1A
MOS_6507.TIA_GRP0_ADDR = 0x1B
MOS_6507.TIA_GRP1_ADDR = 0x1C
MOS_6507.TIA_DGRP0_ADDR = 0x1D
MOS_6507.TIA_DGRP1_ADDR = 0x1E
MOS_6507.TIA_ENAM0_ADDR = 0x1F
MOS_6507.TIA_ENAM1_ADDR = 0x20
MOS_6507.TIA_ENABL_ADDR = 0x21
MOS_6507.TIA_HMP0_ADDR = 0x22
MOS_6507.TIA_HMP1_ADDR = 0x23
MOS_6507.TIA_HMM0_ADDR = 0x24
MOS_6507.TIA_HMM1_ADDR = 0x25
MOS_6507.TIA_HMBL_ADDR = 0x26
MOS_6507.TIA_VDEL0_ADDR = 0x27
MOS_6507.TIA_VDEL1_ADDR = 0x28
MOS_6507.TIA_VDELBL_ADDR = 0x29
MOS_6507.TIA_RESBB_ADDR = 0x2A
MOS_6507.TIA_HMOVE_ADDR = 0x2A
MOS_6507.TIA_HMCLR_ADDR = 0x2B
MOS_6507.TIA_CXM0P_ADDR = 0x30
MOS_6507.TIA_CXM1P_ADDR = 0x31
MOS_6507.TIA_CXP0FB_ADDR = 0x32
MOS_6507.TIA_CXP1FB_ADDR = 0x33
MOS_6507.TIA_CXM0FB_ADDR = 0x34
MOS_6507.TIA_CXM1FB_ADDR = 0x35
MOS_6507.TIA_CXBLPF_ADDR = 0x36
MOS_6507.TIA_CXPPMM_ADDR = 0x37
MOS_6507.TIA_INPT0_ADDR = 0x38
MOS_6507.TIA_INPT1_ADDR = 0x39
MOS_6507.TIA_INPT2_ADDR = 0x3A
MOS_6507.TIA_INPT3_ADDR = 0x3B
MOS_6507.TIA_INPT4_ADDR = 0x3C
MOS_6507.TIA_INPT5_ADDR = 0x3D
-- RAM, I/O, Timer (6532 RIOT)
MOS_6507.RIOT_BASE = 0x0080
MOS_6507.RIOT_SWCHA_ADDR = 0x280
MOS_6507.RIOT_SWACNT_ADDR = 0x281
MOS_6507.RIOT_SWCHB_ADDR = 0x282
MOS_6507.RIOT_SWCHB_RESET_BIT = 1  -- Game Reset Switch (0=pressed)
MOS_6507.RIOT_SWCHB_SELECT_BIT = 2  -- Game Select Switch (0=pressed)
MOS_6507.RIOT_SWCHB_DIFFB_BIT = 3  -- Difficulty B (0=hard, 1=easy)
MOS_6507.RIOT_SWCHB_DIFFA_BIT = 4  -- Difficulty A (0=hard, 1=easy)
MOS_6507.RIOT_SWBCNT_ADDR = 0x283
MOS_6507.RIOT_INTIM_ADDR = 0x284
MOS_6507.RIOT_TIMINT_ADDR = 0x285
MOS_6507.RIOT_TIM1T_ADDR = 0x294
MOS_6507.RIOT_TIM8T_ADDR = 0x295
MOS_6507.RIOT_TIM64T_ADDR = 0x296
MOS_6507.RIOT_TIM1024T_ADDR = 0x297
-- Controller Port 1 (Joystick)
MOS_6507.CONTROLLER1_BASE = 0x280
MOS_6507.CONTROLLER1_SWCHA_ADDR = 0x280
-- Controller Port 2 (Joystick)
MOS_6507.CONTROLLER2_BASE = 0x281
MOS_6507.CONTROLLER2_SWCHA_ADDR = 0x280

-- 中断向量定义
MOS_6507.INT_RESET = 0  -- Power-On Reset

-- 引脚定义
MOS_6507.PIN_VSS = 1  -- Ground
MOS_6507.PIN_VCC = 2  -- Power Supply
MOS_6507.PIN_PHI0 = 3  -- Clock Input (1.19MHz NTSC / 1.18MHz PAL)
MOS_6507.PIN_RESET = 4  -- Reset (active low)
MOS_6507.PIN_A0 = 5  -- Address Bus Bit 0
MOS_6507.PIN_A1 = 6  -- Address Bus Bit 1
MOS_6507.PIN_A2 = 7  -- Address Bus Bit 2
MOS_6507.PIN_A3 = 8  -- Address Bus Bit 3
MOS_6507.PIN_A4 = 9  -- Address Bus Bit 4
MOS_6507.PIN_A5 = 10  -- Address Bus Bit 5
MOS_6507.PIN_A6 = 11  -- Address Bus Bit 6
MOS_6507.PIN_A7 = 12  -- Address Bus Bit 7
MOS_6507.PIN_A8 = 13  -- Address Bus Bit 8
MOS_6507.PIN_A9 = 14  -- Address Bus Bit 9
MOS_6507.PIN_A10 = 15  -- Address Bus Bit 10
MOS_6507.PIN_A11 = 16  -- Address Bus Bit 11
MOS_6507.PIN_A12 = 17  -- Address Bus Bit 12
MOS_6507.PIN_D0 = 18  -- Data Bus Bit 0
MOS_6507.PIN_D1 = 19  -- Data Bus Bit 1
MOS_6507.PIN_D2 = 20  -- Data Bus Bit 2
MOS_6507.PIN_D3 = 21  -- Data Bus Bit 3
MOS_6507.PIN_D4 = 22  -- Data Bus Bit 4
MOS_6507.PIN_D5 = 23  -- Data Bus Bit 5
MOS_6507.PIN_D6 = 24  -- Data Bus Bit 6
MOS_6507.PIN_D7 = 25  -- Data Bus Bit 7
MOS_6507.PIN_RDY = 26  -- Ready (stops CPU on read)
MOS_6507.PIN_R_W = 27  -- Read/Write (1=Read, 0=Write)
MOS_6507.PIN_NC = 28  -- Not Connected

-- 设备类
function MOS_6507.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["A"] = {
            address = 0x00,
            size = 1,
            access = "rw",
            description = "Accumulator",
            value = 0
        }
        self.registers["X"] = {
            address = 0x01,
            size = 1,
            access = "rw",
            description = "X Index",
            value = 0
        }
        self.registers["Y"] = {
            address = 0x02,
            size = 1,
            access = "rw",
            description = "Y Index",
            value = 0
        }
        self.registers["SP"] = {
            address = 0x03,
            size = 1,
            access = "rw",
            description = "Stack Pointer (6-bit, 128-byte stack)",
            value = 0
        }
        self.registers["PC"] = {
            address = 0x04,
            size = 2,
            access = "rw",
            description = "Program Counter (16-bit)",
            value = 0
        }
        self.registers["P"] = {
            address = 0x06,
            size = 1,
            access = "rw",
            description = "Processor Status",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["TIA"] = {
            base = 0x0000,
            type = "video",
            description = "Television Interface Adaptor (Video + Audio + I/O)",
            registers = {}
        }
        
        local p = self.peripherals["TIA"]
        p.registers["VSYNC"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["VBLANK"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["WSYNC"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        p.registers["RSYNC"] = {
            address = 0x03,
            size = 1,
            value = 0
        }
        p.registers["NUSIZ0"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        p.registers["NUSIZ1"] = {
            address = 0x05,
            size = 1,
            value = 0
        }
        p.registers["COLUP0"] = {
            address = 0x06,
            size = 1,
            value = 0
        }
        p.registers["COLUP1"] = {
            address = 0x07,
            size = 1,
            value = 0
        }
        p.registers["COLUPF"] = {
            address = 0x08,
            size = 1,
            value = 0
        }
        p.registers["COLUBK"] = {
            address = 0x09,
            size = 1,
            value = 0
        }
        p.registers["CTRLPF"] = {
            address = 0x0A,
            size = 1,
            value = 0
        }
        p.registers["REFPL"] = {
            address = 0x0B,
            size = 1,
            value = 0
        }
        p.registers["PF0"] = {
            address = 0x0D,
            size = 1,
            value = 0
        }
        p.registers["PF1"] = {
            address = 0x0E,
            size = 1,
            value = 0
        }
        p.registers["PF2"] = {
            address = 0x0F,
            size = 1,
            value = 0
        }
        p.registers["RESP0"] = {
            address = 0x10,
            size = 1,
            value = 0
        }
        p.registers["RESP1"] = {
            address = 0x11,
            size = 1,
            value = 0
        }
        p.registers["RESM0"] = {
            address = 0x12,
            size = 1,
            value = 0
        }
        p.registers["RESM1"] = {
            address = 0x13,
            size = 1,
            value = 0
        }
        p.registers["RESBL"] = {
            address = 0x14,
            size = 1,
            value = 0
        }
        p.registers["AUDC0"] = {
            address = 0x15,
            size = 1,
            value = 0
        }
        p.registers["AUDC1"] = {
            address = 0x16,
            size = 1,
            value = 0
        }
        p.registers["AUDF0"] = {
            address = 0x17,
            size = 1,
            value = 0
        }
        p.registers["AUDF1"] = {
            address = 0x18,
            size = 1,
            value = 0
        }
        p.registers["AUDV0"] = {
            address = 0x19,
            size = 1,
            value = 0
        }
        p.registers["AUDV1"] = {
            address = 0x1A,
            size = 1,
            value = 0
        }
        p.registers["GRP0"] = {
            address = 0x1B,
            size = 1,
            value = 0
        }
        p.registers["GRP1"] = {
            address = 0x1C,
            size = 1,
            value = 0
        }
        p.registers["DGRP0"] = {
            address = 0x1D,
            size = 1,
            value = 0
        }
        p.registers["DGRP1"] = {
            address = 0x1E,
            size = 1,
            value = 0
        }
        p.registers["ENAM0"] = {
            address = 0x1F,
            size = 1,
            value = 0
        }
        p.registers["ENAM1"] = {
            address = 0x20,
            size = 1,
            value = 0
        }
        p.registers["ENABL"] = {
            address = 0x21,
            size = 1,
            value = 0
        }
        p.registers["HMP0"] = {
            address = 0x22,
            size = 1,
            value = 0
        }
        p.registers["HMP1"] = {
            address = 0x23,
            size = 1,
            value = 0
        }
        p.registers["HMM0"] = {
            address = 0x24,
            size = 1,
            value = 0
        }
        p.registers["HMM1"] = {
            address = 0x25,
            size = 1,
            value = 0
        }
        p.registers["HMBL"] = {
            address = 0x26,
            size = 1,
            value = 0
        }
        p.registers["VDEL0"] = {
            address = 0x27,
            size = 1,
            value = 0
        }
        p.registers["VDEL1"] = {
            address = 0x28,
            size = 1,
            value = 0
        }
        p.registers["VDELBL"] = {
            address = 0x29,
            size = 1,
            value = 0
        }
        p.registers["RESBB"] = {
            address = 0x2A,
            size = 1,
            value = 0
        }
        p.registers["HMOVE"] = {
            address = 0x2A,
            size = 1,
            value = 0
        }
        p.registers["HMCLR"] = {
            address = 0x2B,
            size = 1,
            value = 0
        }
        p.registers["CXM0P"] = {
            address = 0x30,
            size = 1,
            value = 0
        }
        p.registers["CXM1P"] = {
            address = 0x31,
            size = 1,
            value = 0
        }
        p.registers["CXP0FB"] = {
            address = 0x32,
            size = 1,
            value = 0
        }
        p.registers["CXP1FB"] = {
            address = 0x33,
            size = 1,
            value = 0
        }
        p.registers["CXM0FB"] = {
            address = 0x34,
            size = 1,
            value = 0
        }
        p.registers["CXM1FB"] = {
            address = 0x35,
            size = 1,
            value = 0
        }
        p.registers["CXBLPF"] = {
            address = 0x36,
            size = 1,
            value = 0
        }
        p.registers["CXPPMM"] = {
            address = 0x37,
            size = 1,
            value = 0
        }
        p.registers["INPT0"] = {
            address = 0x38,
            size = 1,
            value = 0
        }
        p.registers["INPT1"] = {
            address = 0x39,
            size = 1,
            value = 0
        }
        p.registers["INPT2"] = {
            address = 0x3A,
            size = 1,
            value = 0
        }
        p.registers["INPT3"] = {
            address = 0x3B,
            size = 1,
            value = 0
        }
        p.registers["INPT4"] = {
            address = 0x3C,
            size = 1,
            value = 0
        }
        p.registers["INPT5"] = {
            address = 0x3D,
            size = 1,
            value = 0
        }
        self.peripherals["RIOT"] = {
            base = 0x0080,
            type = "system",
            description = "RAM, I/O, Timer (6532 RIOT)",
            registers = {}
        }
        
        local p = self.peripherals["RIOT"]
        p.registers["SWCHA"] = {
            address = 0x280,
            size = 1,
            value = 0
        }
        p.registers["SWACNT"] = {
            address = 0x281,
            size = 1,
            value = 0
        }
        p.registers["SWCHB"] = {
            address = 0x282,
            size = 1,
            value = 0
        }
        p.registers["SWBCNT"] = {
            address = 0x283,
            size = 1,
            value = 0
        }
        p.registers["INTIM"] = {
            address = 0x284,
            size = 1,
            value = 0
        }
        p.registers["TIMINT"] = {
            address = 0x285,
            size = 1,
            value = 0
        }
        p.registers["TIM1T"] = {
            address = 0x294,
            size = 1,
            value = 0
        }
        p.registers["TIM8T"] = {
            address = 0x295,
            size = 1,
            value = 0
        }
        p.registers["TIM64T"] = {
            address = 0x296,
            size = 1,
            value = 0
        }
        p.registers["TIM1024T"] = {
            address = 0x297,
            size = 1,
            value = 0
        }
        self.peripherals["CONTROLLER1"] = {
            base = 0x280,
            type = "input",
            description = "Controller Port 1 (Joystick)",
            registers = {}
        }
        
        local p = self.peripherals["CONTROLLER1"]
        p.registers["SWCHA"] = {
            address = 0x280,
            size = 1,
            value = 0
        }
        self.peripherals["CONTROLLER2"] = {
            base = 0x281,
            type = "input",
            description = "Controller Port 2 (Joystick)",
            registers = {}
        }
        
        local p = self.peripherals["CONTROLLER2"]
        p.registers["SWCHA"] = {
            address = 0x280,
            size = 1,
            value = 0
        }
    end
    
    -- 读取寄存器
    function self:read_register(name)
        local reg = self.registers[name]
        if reg then
            return reg.value
        end
        error("寄存器 " .. name .. " 不存在")
    end
    
    -- 写入寄存器
    function self:write_register(name, value)
        local reg = self.registers[name]
        if reg then
            local max_value = bit.lshift(1, reg.size * 8) - 1
            if value < 0 or value > max_value then
                error("值 " .. value .. " 超出范围 [0, " .. max_value .. "]")
            end
            reg.value = value
        else
            error("寄存器 " .. name .. " 不存在")
        end
    end
    
    -- 设置位
    function self:set_bit(register_name, bit, value)
        local reg = self.registers[register_name]
        if reg then
            if value then
                reg.value = bit.bor(reg.value, bit.lshift(1, bit))
            else
                reg.value = bit.band(reg.value, bit.bnot(bit.lshift(1, bit)))
            end
        else
            error("寄存器 " .. register_name .. " 不存在")
        end
    end
    
    -- 获取位
    function self:get_bit(register_name, bit)
        local reg = self.registers[register_name]
        if reg then
            return bit.band(bit.rshift(reg.value, bit), 1) == 1
        end
        error("寄存器 " .. register_name .. " 不存在")
    end
    
    -- 获取设备信息
    function self:get_device_info()
        return {
            name = MOS_6507.DEVICE_NAME,
            manufacturer = MOS_6507.MANUFACTURER,
            family = MOS_6507.FAMILY,
            version = MOS_6507.VERSION,
            architecture = MOS_6507.ARCHITECTURE,
            bits = MOS_6507.BITS,
            clock_frequency = MOS_6507.CLOCK_FREQUENCY
        }
    end
    
    -- 获取寄存器信息
    function self:get_register_info(name)
        return self.registers[name]
    end
    
    -- 获取外设信息
    function self:get_peripheral_info(name)
        return self.peripherals[name]
    end
    
    -- 重置设备
    function self:reset()
        for _, reg in pairs(self.registers) do
            reg.value = 0
        end
        
        for _, peripheral in pairs(self.peripherals) do
            for _, reg in pairs(peripheral.registers) do
                reg.value = 0
            end
        end
    end
    
    -- 字符串表示
    function self:__tostring()
        local info = self:get_device_info()
        return string.format("MOS_6507(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function MOS_6507.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function MOS_6507.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function MOS_6507.print_device_info(device)
    device = device or MOS_6507.new()
    local info = device:get_device_info()
    
    print("设备信息:")
    print("  名称: " .. info.name)
    print("  厂商: " .. info.manufacturer)
    print("  系列: " .. info.family)
    print("  版本: " .. info.version)
    print("  架构: " .. info.architecture)
    print("  位宽: " .. info.bits)
    print("  时钟: " .. info.clock_frequency .. " Hz")
end

function MOS_6507.print_registers(device)
    device = device or MOS_6507.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            MOS_6507.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function MOS_6507.example()
    print("=== MOS-6507设备示例 ===")
    
    -- 创建设备实例
    local device = MOS_6507.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    MOS_6507.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["A"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("A", 0x55)
        print("写入 A: " .. MOS_6507.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("A")
        print("读取 A: " .. MOS_6507.hex(value))
        
        -- 位操作
        device:set_bit("A", 0, true)
        local bit0 = device:get_bit("A", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    MOS_6507.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("MOS_6507.lua$") then
    MOS_6507.example()
end

return MOS_6507
