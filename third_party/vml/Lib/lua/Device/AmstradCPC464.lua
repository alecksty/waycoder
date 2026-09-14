--[[
  Amstrad-CPC-464设备定义 - Lua模块
  生成自: Amstrad/CPC/Amstrad-CPC-464
  版本: 1.0
  日期: 2026-04-17
  作者: VML Team
  描述: Amstrad CPC 464 - British 8-bit home computer with Z80 CPU and built-in cassette recorder
  CPU架构: Z80A
  位宽: 8位
  时钟频率: 4000000 Hz
]]

local Amstrad_CPC_464 = {}

-- 设备信息
Amstrad_CPC_464.DEVICE_NAME = "Amstrad-CPC-464"
Amstrad_CPC_464.MANUFACTURER = "Amstrad"
Amstrad_CPC_464.FAMILY = "CPC"
Amstrad_CPC_464.VERSION = "1.0"
Amstrad_CPC_464.ARCHITECTURE = "Z80A"
Amstrad_CPC_464.BITS = 8
Amstrad_CPC_464.CLOCK_FREQUENCY = 4000000

-- 寄存器地址定义
Amstrad_CPC_464.A_ADDR = 0x00  -- Accumulator
Amstrad_CPC_464.F_ADDR = 0x01  -- Flags
Amstrad_CPC_464.F_C_BIT = 0  -- Carry
Amstrad_CPC_464.F_N_BIT = 1  -- Subtract
Amstrad_CPC_464.F_PV_BIT = 2  -- Parity/Overflow
Amstrad_CPC_464.F_H_BIT = 4  -- Half Carry
Amstrad_CPC_464.F_Z_BIT = 6  -- Zero
Amstrad_CPC_464.F_S_BIT = 7  -- Sign
Amstrad_CPC_464.B_ADDR = 0x02  -- B Register
Amstrad_CPC_464.C_ADDR = 0x03  -- C Register
Amstrad_CPC_464.D_ADDR = 0x04  -- D Register
Amstrad_CPC_464.E_ADDR = 0x05  -- E Register
Amstrad_CPC_464.H_ADDR = 0x06  -- H Register
Amstrad_CPC_464.L_ADDR = 0x07  -- L Register
Amstrad_CPC_464.AF_ADDR = 0x08  -- Alternate AF
Amstrad_CPC_464.BC_ADDR = 0x0A  -- Alternate BC
Amstrad_CPC_464.DE_ADDR = 0x0C  -- Alternate DE
Amstrad_CPC_464.HL_ADDR = 0x0E  -- Alternate HL
Amstrad_CPC_464.I_ADDR = 0x10  -- Interrupt Vector
Amstrad_CPC_464.R_ADDR = 0x11  -- Refresh
Amstrad_CPC_464.IX_ADDR = 0x12  -- Index X
Amstrad_CPC_464.IY_ADDR = 0x14  -- Index Y
Amstrad_CPC_464.SP_ADDR = 0x16  -- Stack Pointer
Amstrad_CPC_464.PC_ADDR = 0x18  -- Program Counter

-- 内存段定义
Amstrad_CPC_464.LOWER_ROM_START = 0x0000
Amstrad_CPC_464.LOWER_ROM_END = 0x3FFF
Amstrad_CPC_464.LOWER_ROM_SIZE = 16384  -- Lower ROM (AMSDOS / CP/M)
Amstrad_CPC_464.RAM_BANK0_START = 0x0000
Amstrad_CPC_464.RAM_BANK0_END = 0x3FFF
Amstrad_CPC_464.RAM_BANK0_SIZE = 16384  -- Lower RAM bank (switchable)
Amstrad_CPC_464.RAM_MAIN_START = 0x4000
Amstrad_CPC_464.RAM_MAIN_END = 0xBFFF
Amstrad_CPC_464.RAM_MAIN_SIZE = 32768  -- Main RAM (32KB)
Amstrad_CPC_464.UPPER_ROM_START = 0xC000
Amstrad_CPC_464.UPPER_ROM_END = 0xFFFF
Amstrad_CPC_464.UPPER_ROM_SIZE = 16384  -- Upper ROM (BASIC)

-- 外设定义
-- Gate Array - Custom ASIC (video/sound/RAM control)
Amstrad_CPC_464.GA_BASE = 0x7F00
Amstrad_CPC_464.GA_GA_MR_ADDR = 0x7F00
Amstrad_CPC_464.GA_GA_IR_ADDR = 0x7F01
Amstrad_CPC_464.GA_GA_R1_ADDR = 0x7F02
Amstrad_CPC_464.GA_GA_R2_ADDR = 0x7F03
Amstrad_CPC_464.GA_GA_R3_ADDR = 0x7F04
Amstrad_CPC_464.GA_GA_R4_ADDR = 0x7F05
Amstrad_CPC_464.GA_GA_R5_ADDR = 0x7F06
Amstrad_CPC_464.GA_GA_R6_ADDR = 0x7F07
Amstrad_CPC_464.GA_GA_R7_ADDR = 0x7F08
-- CRT Controller 6845 - Video timing
Amstrad_CPC_464.CRTC_BASE = 0xBC00
Amstrad_CPC_464.CRTC_CRTC_REG_ADDR = 0xBC00
Amstrad_CPC_464.CRTC_CRTC_DATA_ADDR = 0xBD00
Amstrad_CPC_464.CRTC_CRTC_H_TOTAL_ADDR = 0xBC01
Amstrad_CPC_464.CRTC_CRTC_H_DISP_ADDR = 0xBC02
Amstrad_CPC_464.CRTC_CRTC_HSYNC_POS_ADDR = 0xBC03
Amstrad_CPC_464.CRTC_CRTC_HSYNC_WIDTH_ADDR = 0xBC04
Amstrad_CPC_464.CRTC_CRTC_V_TOTAL_ADDR = 0xBC05
Amstrad_CPC_464.CRTC_CRTC_V_TOTAL_ADJ_ADDR = 0xBC06
Amstrad_CPC_464.CRTC_CRTC_V_DISP_ADDR = 0xBC07
Amstrad_CPC_464.CRTC_CRTC_VSYNC_POS_ADDR = 0xBC08
Amstrad_CPC_464.CRTC_CRTC_INTERLACE_ADDR = 0xBC09
Amstrad_CPC_464.CRTC_CRTC_CURSOR_START_ADDR = 0xBC0A
Amstrad_CPC_464.CRTC_CRTC_CURSOR_END_ADDR = 0xBC0B
Amstrad_CPC_464.CRTC_CRTC_SA_HI_ADDR = 0xBC0C
Amstrad_CPC_464.CRTC_CRTC_SA_LO_ADDR = 0xBC0D
Amstrad_CPC_464.CRTC_CRTC_CURSOR_HI_ADDR = 0xBC0E
Amstrad_CPC_464.CRTC_CRTC_CURSOR_LO_ADDR = 0xBC0F
-- AY-3-8912 Programmable Sound Generator
Amstrad_CPC_464.PSG_BASE = 0xF400
Amstrad_CPC_464.PSG_PSG_REG_ADDR = 0xF400
Amstrad_CPC_464.PSG_PSG_DATA_ADDR = 0xF600
Amstrad_CPC_464.PSG_FREQ_A_LO_ADDR = 0xF400
Amstrad_CPC_464.PSG_FREQ_A_HI_ADDR = 0xF401
Amstrad_CPC_464.PSG_FREQ_B_LO_ADDR = 0xF402
Amstrad_CPC_464.PSG_FREQ_B_HI_ADDR = 0xF403
Amstrad_CPC_464.PSG_FREQ_C_LO_ADDR = 0xF404
Amstrad_CPC_464.PSG_FREQ_C_HI_ADDR = 0xF405
Amstrad_CPC_464.PSG_NOISE_FREQ_ADDR = 0xF406
Amstrad_CPC_464.PSG_ENABLE_ADDR = 0xF407
Amstrad_CPC_464.PSG_VOL_A_ADDR = 0xF408
Amstrad_CPC_464.PSG_VOL_B_ADDR = 0xF409
Amstrad_CPC_464.PSG_VOL_C_ADDR = 0xF40A
Amstrad_CPC_464.PSG_ENV_FREQ_LO_ADDR = 0xF40B
Amstrad_CPC_464.PSG_ENV_FREQ_HI_ADDR = 0xF40C
Amstrad_CPC_464.PSG_ENV_SHAPE_ADDR = 0xF40D
Amstrad_CPC_464.PSG_PORT_A_ADDR = 0xF40E
Amstrad_CPC_464.PSG_PORT_B_ADDR = 0xF40F
-- WD1772 Floppy Disk Controller (via expansion)
Amstrad_CPC_464.FDC_BASE = 0xF800
Amstrad_CPC_464.FDC_FDC_STATUS_ADDR = 0xF8E0
Amstrad_CPC_464.FDC_FDC_COMMAND_ADDR = 0xF8E0
Amstrad_CPC_464.FDC_FDC_TRACK_ADDR = 0xF8E1
Amstrad_CPC_464.FDC_FDC_SECTOR_ADDR = 0xF8E2
Amstrad_CPC_464.FDC_FDC_DATA_ADDR = 0xF8E3
-- Centronics Parallel Printer Port
Amstrad_CPC_464.PRINTER_BASE = 0xEE
Amstrad_CPC_464.PRINTER_PRN_DATA_ADDR = 0xEE
Amstrad_CPC_464.PRINTER_PRN_STROBE_ADDR = 0xEF

-- 中断向量定义
Amstrad_CPC_464.INT_RESET = 0  -- Power-on / Reset
Amstrad_CPC_464.INT_NMI = 1  -- Non-Maskable Interrupt
Amstrad_CPC_464.INT_INT = 2  -- Gate Array interrupt (50Hz vertical blank)

-- 引脚定义
Amstrad_CPC_464.PIN_VCC = 1  -- +5V Power
Amstrad_CPC_464.PIN_GND = 2  -- Ground
Amstrad_CPC_464.PIN_CLK = 3  -- Z80 Clock (4MHz)
Amstrad_CPC_464.PIN_A0_A15 = 4  -- Address Bus
Amstrad_CPC_464.PIN_D0_D7 = 5  -- Data Bus
Amstrad_CPC_464.PIN_MREQ = 6  -- Memory Request
Amstrad_CPC_464.PIN_IORQ = 7  -- I/O Request
Amstrad_CPC_464.PIN_RD = 8  -- Read
Amstrad_CPC_464.PIN_WR = 9  -- Write
Amstrad_CPC_464.PIN_INT = 10  -- Interrupt Request
Amstrad_CPC_464.PIN_NMI = 11  -- Non-Maskable Interrupt
Amstrad_CPC_464.PIN_RESET = 12  -- Reset

-- 设备类
function Amstrad_CPC_464.new(memory_base)
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
        self.registers["F"] = {
            address = 0x01,
            size = 1,
            access = "rw",
            description = "Flags",
            value = 0
        }
        self.registers["B"] = {
            address = 0x02,
            size = 1,
            access = "rw",
            description = "B Register",
            value = 0
        }
        self.registers["C"] = {
            address = 0x03,
            size = 1,
            access = "rw",
            description = "C Register",
            value = 0
        }
        self.registers["D"] = {
            address = 0x04,
            size = 1,
            access = "rw",
            description = "D Register",
            value = 0
        }
        self.registers["E"] = {
            address = 0x05,
            size = 1,
            access = "rw",
            description = "E Register",
            value = 0
        }
        self.registers["H"] = {
            address = 0x06,
            size = 1,
            access = "rw",
            description = "H Register",
            value = 0
        }
        self.registers["L"] = {
            address = 0x07,
            size = 1,
            access = "rw",
            description = "L Register",
            value = 0
        }
        self.registers["AF'"] = {
            address = 0x08,
            size = 2,
            access = "rw",
            description = "Alternate AF",
            value = 0
        }
        self.registers["BC'"] = {
            address = 0x0A,
            size = 2,
            access = "rw",
            description = "Alternate BC",
            value = 0
        }
        self.registers["DE'"] = {
            address = 0x0C,
            size = 2,
            access = "rw",
            description = "Alternate DE",
            value = 0
        }
        self.registers["HL'"] = {
            address = 0x0E,
            size = 2,
            access = "rw",
            description = "Alternate HL",
            value = 0
        }
        self.registers["I"] = {
            address = 0x10,
            size = 1,
            access = "rw",
            description = "Interrupt Vector",
            value = 0
        }
        self.registers["R"] = {
            address = 0x11,
            size = 1,
            access = "rw",
            description = "Refresh",
            value = 0
        }
        self.registers["IX"] = {
            address = 0x12,
            size = 2,
            access = "rw",
            description = "Index X",
            value = 0
        }
        self.registers["IY"] = {
            address = 0x14,
            size = 2,
            access = "rw",
            description = "Index Y",
            value = 0
        }
        self.registers["SP"] = {
            address = 0x16,
            size = 2,
            access = "rw",
            description = "Stack Pointer",
            value = 0
        }
        self.registers["PC"] = {
            address = 0x18,
            size = 2,
            access = "rw",
            description = "Program Counter",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["GA"] = {
            base = 0x7F00,
            type = "video",
            description = "Gate Array - Custom ASIC (video/sound/RAM control)",
            registers = {}
        }
        
        local p = self.peripherals["GA"]
        p.registers["GA_MR"] = {
            address = 0x7F00,
            size = 1,
            value = 0
        }
        p.registers["GA_IR"] = {
            address = 0x7F01,
            size = 1,
            value = 0
        }
        p.registers["GA_R1"] = {
            address = 0x7F02,
            size = 1,
            value = 0
        }
        p.registers["GA_R2"] = {
            address = 0x7F03,
            size = 1,
            value = 0
        }
        p.registers["GA_R3"] = {
            address = 0x7F04,
            size = 1,
            value = 0
        }
        p.registers["GA_R4"] = {
            address = 0x7F05,
            size = 1,
            value = 0
        }
        p.registers["GA_R5"] = {
            address = 0x7F06,
            size = 1,
            value = 0
        }
        p.registers["GA_R6"] = {
            address = 0x7F07,
            size = 1,
            value = 0
        }
        p.registers["GA_R7"] = {
            address = 0x7F08,
            size = 1,
            value = 0
        }
        self.peripherals["CRTC"] = {
            base = 0xBC00,
            type = "video",
            description = "CRT Controller 6845 - Video timing",
            registers = {}
        }
        
        local p = self.peripherals["CRTC"]
        p.registers["CRTC_REG"] = {
            address = 0xBC00,
            size = 1,
            value = 0
        }
        p.registers["CRTC_DATA"] = {
            address = 0xBD00,
            size = 1,
            value = 0
        }
        p.registers["CRTC_H_TOTAL"] = {
            address = 0xBC01,
            size = 1,
            value = 0
        }
        p.registers["CRTC_H_DISP"] = {
            address = 0xBC02,
            size = 1,
            value = 0
        }
        p.registers["CRTC_HSYNC_POS"] = {
            address = 0xBC03,
            size = 1,
            value = 0
        }
        p.registers["CRTC_HSYNC_WIDTH"] = {
            address = 0xBC04,
            size = 1,
            value = 0
        }
        p.registers["CRTC_V_TOTAL"] = {
            address = 0xBC05,
            size = 1,
            value = 0
        }
        p.registers["CRTC_V_TOTAL_ADJ"] = {
            address = 0xBC06,
            size = 1,
            value = 0
        }
        p.registers["CRTC_V_DISP"] = {
            address = 0xBC07,
            size = 1,
            value = 0
        }
        p.registers["CRTC_VSYNC_POS"] = {
            address = 0xBC08,
            size = 1,
            value = 0
        }
        p.registers["CRTC_INTERLACE"] = {
            address = 0xBC09,
            size = 1,
            value = 0
        }
        p.registers["CRTC_CURSOR_START"] = {
            address = 0xBC0A,
            size = 1,
            value = 0
        }
        p.registers["CRTC_CURSOR_END"] = {
            address = 0xBC0B,
            size = 1,
            value = 0
        }
        p.registers["CRTC_SA_HI"] = {
            address = 0xBC0C,
            size = 1,
            value = 0
        }
        p.registers["CRTC_SA_LO"] = {
            address = 0xBC0D,
            size = 1,
            value = 0
        }
        p.registers["CRTC_CURSOR_HI"] = {
            address = 0xBC0E,
            size = 1,
            value = 0
        }
        p.registers["CRTC_CURSOR_LO"] = {
            address = 0xBC0F,
            size = 1,
            value = 0
        }
        self.peripherals["PSG"] = {
            base = 0xF400,
            type = "audio",
            description = "AY-3-8912 Programmable Sound Generator",
            registers = {}
        }
        
        local p = self.peripherals["PSG"]
        p.registers["PSG_REG"] = {
            address = 0xF400,
            size = 1,
            value = 0
        }
        p.registers["PSG_DATA"] = {
            address = 0xF600,
            size = 1,
            value = 0
        }
        p.registers["FREQ_A_LO"] = {
            address = 0xF400,
            size = 1,
            value = 0
        }
        p.registers["FREQ_A_HI"] = {
            address = 0xF401,
            size = 1,
            value = 0
        }
        p.registers["FREQ_B_LO"] = {
            address = 0xF402,
            size = 1,
            value = 0
        }
        p.registers["FREQ_B_HI"] = {
            address = 0xF403,
            size = 1,
            value = 0
        }
        p.registers["FREQ_C_LO"] = {
            address = 0xF404,
            size = 1,
            value = 0
        }
        p.registers["FREQ_C_HI"] = {
            address = 0xF405,
            size = 1,
            value = 0
        }
        p.registers["NOISE_FREQ"] = {
            address = 0xF406,
            size = 1,
            value = 0
        }
        p.registers["ENABLE"] = {
            address = 0xF407,
            size = 1,
            value = 0
        }
        p.registers["VOL_A"] = {
            address = 0xF408,
            size = 1,
            value = 0
        }
        p.registers["VOL_B"] = {
            address = 0xF409,
            size = 1,
            value = 0
        }
        p.registers["VOL_C"] = {
            address = 0xF40A,
            size = 1,
            value = 0
        }
        p.registers["ENV_FREQ_LO"] = {
            address = 0xF40B,
            size = 1,
            value = 0
        }
        p.registers["ENV_FREQ_HI"] = {
            address = 0xF40C,
            size = 1,
            value = 0
        }
        p.registers["ENV_SHAPE"] = {
            address = 0xF40D,
            size = 1,
            value = 0
        }
        p.registers["PORT_A"] = {
            address = 0xF40E,
            size = 1,
            value = 0
        }
        p.registers["PORT_B"] = {
            address = 0xF40F,
            size = 1,
            value = 0
        }
        self.peripherals["FDC"] = {
            base = 0xF800,
            type = "storage",
            description = "WD1772 Floppy Disk Controller (via expansion)",
            registers = {}
        }
        
        local p = self.peripherals["FDC"]
        p.registers["FDC_STATUS"] = {
            address = 0xF8E0,
            size = 1,
            value = 0
        }
        p.registers["FDC_COMMAND"] = {
            address = 0xF8E0,
            size = 1,
            value = 0
        }
        p.registers["FDC_TRACK"] = {
            address = 0xF8E1,
            size = 1,
            value = 0
        }
        p.registers["FDC_SECTOR"] = {
            address = 0xF8E2,
            size = 1,
            value = 0
        }
        p.registers["FDC_DATA"] = {
            address = 0xF8E3,
            size = 1,
            value = 0
        }
        self.peripherals["PRINTER"] = {
            base = 0xEE,
            type = "output",
            description = "Centronics Parallel Printer Port",
            registers = {}
        }
        
        local p = self.peripherals["PRINTER"]
        p.registers["PRN_DATA"] = {
            address = 0xEE,
            size = 1,
            value = 0
        }
        p.registers["PRN_STROBE"] = {
            address = 0xEF,
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
            name = Amstrad_CPC_464.DEVICE_NAME,
            manufacturer = Amstrad_CPC_464.MANUFACTURER,
            family = Amstrad_CPC_464.FAMILY,
            version = Amstrad_CPC_464.VERSION,
            architecture = Amstrad_CPC_464.ARCHITECTURE,
            bits = Amstrad_CPC_464.BITS,
            clock_frequency = Amstrad_CPC_464.CLOCK_FREQUENCY
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
        return string.format("Amstrad_CPC_464(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function Amstrad_CPC_464.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function Amstrad_CPC_464.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function Amstrad_CPC_464.print_device_info(device)
    device = device or Amstrad_CPC_464.new()
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

function Amstrad_CPC_464.print_registers(device)
    device = device or Amstrad_CPC_464.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            Amstrad_CPC_464.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function Amstrad_CPC_464.example()
    print("=== Amstrad-CPC-464设备示例 ===")
    
    -- 创建设备实例
    local device = Amstrad_CPC_464.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    Amstrad_CPC_464.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["A"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("A", 0x55)
        print("写入 A: " .. Amstrad_CPC_464.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("A")
        print("读取 A: " .. Amstrad_CPC_464.hex(value))
        
        -- 位操作
        device:set_bit("A", 0, true)
        local bit0 = device:get_bit("A", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    Amstrad_CPC_464.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("Amstrad_CPC_464.lua$") then
    Amstrad_CPC_464.example()
end

return Amstrad_CPC_464
