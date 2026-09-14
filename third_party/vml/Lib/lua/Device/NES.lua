--[[
  Nintendo Entertainment System设备定义 - Lua模块
  生成自: Nintendo/NES/Nintendo Entertainment System
  版本: 
  日期: 
  作者: 
  描述: Nintendo Entertainment System (NES/Famicom) 8-bit video game console
  CPU架构: 6502
  位宽: 0位
  时钟频率: 0 Hz
]]

local Nintendo Entertainment System = {}

-- 设备信息
Nintendo Entertainment System.DEVICE_NAME = "Nintendo Entertainment System"
Nintendo Entertainment System.MANUFACTURER = "Nintendo"
Nintendo Entertainment System.FAMILY = "NES"
Nintendo Entertainment System.VERSION = ""
Nintendo Entertainment System.ARCHITECTURE = "6502"
Nintendo Entertainment System.BITS = 0
Nintendo Entertainment System.CLOCK_FREQUENCY = 0

-- 外设定义
-- Picture Processing Unit (Ricoh 2C02)
Nintendo Entertainment System.PPU_BASE = 
Nintendo Entertainment System.PPU_PPUCTRL_ADDR = 0x2000
Nintendo Entertainment System.PPU_PPUCTRL_NMI_BIT = 7  -- VBlank NMI enable
Nintendo Entertainment System.PPU_PPUCTRL_MASTERSLAVE_BIT = 6  -- Master/slave select
Nintendo Entertainment System.PPU_PPUCTRL_SPRITESIZE_BIT = 5  -- Sprite size (0=8x8, 1=8x16)
Nintendo Entertainment System.PPU_PPUCTRL_BGPATTERN_BIT = 4  -- Background pattern table address
Nintendo Entertainment System.PPU_PPUCTRL_SPRITEPATTERN_BIT = 3  -- Sprite pattern table address
Nintendo Entertainment System.PPU_PPUCTRL_VRAMINCREMENT_BIT = 2  -- VRAM address increment (0=1, 1=32)
Nintendo Entertainment System.PPU_PPUCTRL_NAMETABLE_BIT = 0  -- Nametable address
Nintendo Entertainment System.PPU_PPUMASK_ADDR = 0x2001
Nintendo Entertainment System.PPU_PPUMASK_EMPHASIZEBLUE_BIT = 7  -- Emphasize blue
Nintendo Entertainment System.PPU_PPUMASK_EMPHASIZEGREEN_BIT = 6  -- Emphasize green
Nintendo Entertainment System.PPU_PPUMASK_EMPHASIZERED_BIT = 5  -- Emphasize red
Nintendo Entertainment System.PPU_PPUMASK_SHOWSPRITES_BIT = 4  -- Show sprites
Nintendo Entertainment System.PPU_PPUMASK_SHOWBACKGROUND_BIT = 3  -- Show background
Nintendo Entertainment System.PPU_PPUMASK_SHOWLEFTSPRITES_BIT = 2  -- Show sprites in left 8 pixels
Nintendo Entertainment System.PPU_PPUMASK_SHOWLEFTBACKGROUND_BIT = 1  -- Show background in left 8 pixels
Nintendo Entertainment System.PPU_PPUMASK_GRAYSCALE_BIT = 0  -- Grayscale mode
Nintendo Entertainment System.PPU_PPUSTATUS_ADDR = 0x2002
Nintendo Entertainment System.PPU_PPUSTATUS_VBLANK_BIT = 7  -- VBlank started
Nintendo Entertainment System.PPU_PPUSTATUS_SPRITE0HIT_BIT = 6  -- Sprite 0 hit
Nintendo Entertainment System.PPU_PPUSTATUS_SPRITEOVERFLOW_BIT = 5  -- Sprite overflow
Nintendo Entertainment System.PPU_OAMADDR_ADDR = 0x2003
Nintendo Entertainment System.PPU_OAMDATA_ADDR = 0x2004
Nintendo Entertainment System.PPU_PPUSCROLL_ADDR = 0x2005
Nintendo Entertainment System.PPU_PPUADDR_ADDR = 0x2006
Nintendo Entertainment System.PPU_PPUDATA_ADDR = 0x2007
Nintendo Entertainment System.PPU_OAMDMA_ADDR = 0x4014
-- Audio Processing Unit (Ricoh 2A03)
Nintendo Entertainment System.APU_BASE = 
Nintendo Entertainment System.APU_SQ1_VOL_ADDR = 0x4000
Nintendo Entertainment System.APU_SQ1_VOL_DUTY_BIT = 6  -- Duty cycle
Nintendo Entertainment System.APU_SQ1_VOL_LENGTHCOUNTERHALT_BIT = 5  -- Length counter halt/envelope loop
Nintendo Entertainment System.APU_SQ1_VOL_CONSTANTVOLUME_BIT = 4  -- Constant volume
Nintendo Entertainment System.APU_SQ1_VOL_VOLUME_BIT = 0  -- Volume/envelope period
Nintendo Entertainment System.APU_SQ1_SWEEP_ADDR = 0x4001
Nintendo Entertainment System.APU_SQ1_SWEEP_ENABLED_BIT = 7  -- Sweep enabled
Nintendo Entertainment System.APU_SQ1_SWEEP_PERIOD_BIT = 4  -- Sweep period
Nintendo Entertainment System.APU_SQ1_SWEEP_NEGATE_BIT = 3  -- Sweep negate
Nintendo Entertainment System.APU_SQ1_SWEEP_SHIFT_BIT = 0  -- Sweep shift amount
Nintendo Entertainment System.APU_SQ1_LO_ADDR = 0x4002
Nintendo Entertainment System.APU_SQ1_HI_ADDR = 0x4003
Nintendo Entertainment System.APU_SQ1_HI_LENGTHCOUNTER_BIT = 3  -- Length counter load
Nintendo Entertainment System.APU_SQ1_HI_TIMERHIGH_BIT = 0  -- Timer high bits
Nintendo Entertainment System.APU_SQ2_VOL_ADDR = 0x4004
Nintendo Entertainment System.APU_SQ2_SWEEP_ADDR = 0x4005
Nintendo Entertainment System.APU_SQ2_LO_ADDR = 0x4006
Nintendo Entertainment System.APU_SQ2_HI_ADDR = 0x4007
Nintendo Entertainment System.APU_TRI_LINEAR_ADDR = 0x4008
Nintendo Entertainment System.APU_TRI_LINEAR_CONTROL_BIT = 7  -- Length counter halt/linear counter control
Nintendo Entertainment System.APU_TRI_LINEAR_PERIOD_BIT = 0  -- Linear counter load
Nintendo Entertainment System.APU_TRI_LO_ADDR = 0x400A
Nintendo Entertainment System.APU_TRI_HI_ADDR = 0x400B
Nintendo Entertainment System.APU_NOISE_VOL_ADDR = 0x400C
Nintendo Entertainment System.APU_NOISE_LO_ADDR = 0x400E
Nintendo Entertainment System.APU_NOISE_LO_MODE_BIT = 7  -- Noise mode
Nintendo Entertainment System.APU_NOISE_LO_PERIOD_BIT = 0  -- Noise period
Nintendo Entertainment System.APU_NOISE_HI_ADDR = 0x400F
Nintendo Entertainment System.APU_DMC_FREQ_ADDR = 0x4010
Nintendo Entertainment System.APU_DMC_FREQ_IRQ_BIT = 7  -- IRQ enable
Nintendo Entertainment System.APU_DMC_FREQ_LOOP_BIT = 6  -- Loop flag
Nintendo Entertainment System.APU_DMC_FREQ_FREQUENCY_BIT = 0  -- Frequency index
Nintendo Entertainment System.APU_DMC_RAW_ADDR = 0x4011
Nintendo Entertainment System.APU_DMC_START_ADDR = 0x4012
Nintendo Entertainment System.APU_DMC_LEN_ADDR = 0x4013
Nintendo Entertainment System.APU_OAMDMA_ADDR = 0x4014
Nintendo Entertainment System.APU_APUSTATUS_ADDR = 0x4015
Nintendo Entertainment System.APU_APUSTATUS_DMCINTERRUPT_BIT = 7  -- DMC interrupt flag
Nintendo Entertainment System.APU_APUSTATUS_FRAMEINTERRUPT_BIT = 6  -- Frame interrupt flag
Nintendo Entertainment System.APU_APUSTATUS_DMCENABLED_BIT = 4  -- DMC enabled
Nintendo Entertainment System.APU_APUSTATUS_NOISEENABLED_BIT = 3  -- Noise enabled
Nintendo Entertainment System.APU_APUSTATUS_TRIANGLEENABLED_BIT = 2  -- Triangle enabled
Nintendo Entertainment System.APU_APUSTATUS_SQUARE2ENABLED_BIT = 1  -- Square 2 enabled
Nintendo Entertainment System.APU_APUSTATUS_SQUARE1ENABLED_BIT = 0  -- Square 1 enabled
Nintendo Entertainment System.APU_APUFRAME_ADDR = 0x4017
Nintendo Entertainment System.APU_APUFRAME_MODE_BIT = 7  -- Frame counter mode
Nintendo Entertainment System.APU_APUFRAME_IRQINHIBIT_BIT = 6  -- IRQ inhibit
-- Controller Interface
Nintendo Entertainment System.CONTROLLER_BASE = 
Nintendo Entertainment System.CONTROLLER_JOY1_ADDR = 0x4016
Nintendo Entertainment System.CONTROLLER_JOY1_A_BIT = 7  -- A button
Nintendo Entertainment System.CONTROLLER_JOY1_B_BIT = 6  -- B button
Nintendo Entertainment System.CONTROLLER_JOY1_SELECT_BIT = 5  -- Select button
Nintendo Entertainment System.CONTROLLER_JOY1_START_BIT = 4  -- Start button
Nintendo Entertainment System.CONTROLLER_JOY1_UP_BIT = 3  -- Up direction
Nintendo Entertainment System.CONTROLLER_JOY1_DOWN_BIT = 2  -- Down direction
Nintendo Entertainment System.CONTROLLER_JOY1_LEFT_BIT = 1  -- Left direction
Nintendo Entertainment System.CONTROLLER_JOY1_RIGHT_BIT = 0  -- Right direction
Nintendo Entertainment System.CONTROLLER_JOY2_ADDR = 0x4017
-- Memory Mapper (Cartridge)
Nintendo Entertainment System.MAPPER_BASE = 
Nintendo Entertainment System.MAPPER_PRGROM_ADDR = 0
Nintendo Entertainment System.MAPPER_CHRROM_ADDR = 0
Nintendo Entertainment System.MAPPER_PRGRAM_ADDR = 0
Nintendo Entertainment System.MAPPER_CHRRAM_ADDR = 0

-- 中断向量定义
Nintendo Entertainment System.INT_NMI = 65530  -- Non-maskable interrupt (VBlank)
Nintendo Entertainment System.INT_RESET = 65532  -- Reset vector
Nintendo Entertainment System.INT_IRQ = 65534  -- Interrupt request

-- 设备类
function Nintendo Entertainment System.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["PPU"] = {
            base = ,
            type = "Video",
            description = "Picture Processing Unit (Ricoh 2C02)",
            registers = {}
        }
        
        local p = self.peripherals["PPU"]
        p.registers["PPUCTRL"] = {
            address = 0x2000,
            size = 8,
            value = 0
        }
        p.registers["PPUMASK"] = {
            address = 0x2001,
            size = 8,
            value = 0
        }
        p.registers["PPUSTATUS"] = {
            address = 0x2002,
            size = 8,
            value = 0
        }
        p.registers["OAMADDR"] = {
            address = 0x2003,
            size = 8,
            value = 0
        }
        p.registers["OAMDATA"] = {
            address = 0x2004,
            size = 8,
            value = 0
        }
        p.registers["PPUSCROLL"] = {
            address = 0x2005,
            size = 8,
            value = 0
        }
        p.registers["PPUADDR"] = {
            address = 0x2006,
            size = 8,
            value = 0
        }
        p.registers["PPUDATA"] = {
            address = 0x2007,
            size = 8,
            value = 0
        }
        p.registers["OAMDMA"] = {
            address = 0x4014,
            size = 8,
            value = 0
        }
        self.peripherals["APU"] = {
            base = ,
            type = "Audio",
            description = "Audio Processing Unit (Ricoh 2A03)",
            registers = {}
        }
        
        local p = self.peripherals["APU"]
        p.registers["SQ1_VOL"] = {
            address = 0x4000,
            size = 8,
            value = 0
        }
        p.registers["SQ1_SWEEP"] = {
            address = 0x4001,
            size = 8,
            value = 0
        }
        p.registers["SQ1_LO"] = {
            address = 0x4002,
            size = 8,
            value = 0
        }
        p.registers["SQ1_HI"] = {
            address = 0x4003,
            size = 8,
            value = 0
        }
        p.registers["SQ2_VOL"] = {
            address = 0x4004,
            size = 8,
            value = 0
        }
        p.registers["SQ2_SWEEP"] = {
            address = 0x4005,
            size = 8,
            value = 0
        }
        p.registers["SQ2_LO"] = {
            address = 0x4006,
            size = 8,
            value = 0
        }
        p.registers["SQ2_HI"] = {
            address = 0x4007,
            size = 8,
            value = 0
        }
        p.registers["TRI_LINEAR"] = {
            address = 0x4008,
            size = 8,
            value = 0
        }
        p.registers["TRI_LO"] = {
            address = 0x400A,
            size = 8,
            value = 0
        }
        p.registers["TRI_HI"] = {
            address = 0x400B,
            size = 8,
            value = 0
        }
        p.registers["NOISE_VOL"] = {
            address = 0x400C,
            size = 8,
            value = 0
        }
        p.registers["NOISE_LO"] = {
            address = 0x400E,
            size = 8,
            value = 0
        }
        p.registers["NOISE_HI"] = {
            address = 0x400F,
            size = 8,
            value = 0
        }
        p.registers["DMC_FREQ"] = {
            address = 0x4010,
            size = 8,
            value = 0
        }
        p.registers["DMC_RAW"] = {
            address = 0x4011,
            size = 8,
            value = 0
        }
        p.registers["DMC_START"] = {
            address = 0x4012,
            size = 8,
            value = 0
        }
        p.registers["DMC_LEN"] = {
            address = 0x4013,
            size = 8,
            value = 0
        }
        p.registers["OAMDMA"] = {
            address = 0x4014,
            size = 8,
            value = 0
        }
        p.registers["APUSTATUS"] = {
            address = 0x4015,
            size = 8,
            value = 0
        }
        p.registers["APUFRAME"] = {
            address = 0x4017,
            size = 8,
            value = 0
        }
        self.peripherals["Controller"] = {
            base = ,
            type = "Input",
            description = "Controller Interface",
            registers = {}
        }
        
        local p = self.peripherals["Controller"]
        p.registers["JOY1"] = {
            address = 0x4016,
            size = 8,
            value = 0
        }
        p.registers["JOY2"] = {
            address = 0x4017,
            size = 8,
            value = 0
        }
        self.peripherals["Mapper"] = {
            base = ,
            type = "Memory",
            description = "Memory Mapper (Cartridge)",
            registers = {}
        }
        
        local p = self.peripherals["Mapper"]
        p.registers["PRGROM"] = {
            address = 0,
            size = 0,
            value = 0
        }
        p.registers["CHRROM"] = {
            address = 0,
            size = 0,
            value = 0
        }
        p.registers["PRGRAM"] = {
            address = 0,
            size = 0,
            value = 0
        }
        p.registers["CHRRAM"] = {
            address = 0,
            size = 0,
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
            name = Nintendo Entertainment System.DEVICE_NAME,
            manufacturer = Nintendo Entertainment System.MANUFACTURER,
            family = Nintendo Entertainment System.FAMILY,
            version = Nintendo Entertainment System.VERSION,
            architecture = Nintendo Entertainment System.ARCHITECTURE,
            bits = Nintendo Entertainment System.BITS,
            clock_frequency = Nintendo Entertainment System.CLOCK_FREQUENCY
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
        return string.format("Nintendo Entertainment System(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function Nintendo Entertainment System.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function Nintendo Entertainment System.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function Nintendo Entertainment System.print_device_info(device)
    device = device or Nintendo Entertainment System.new()
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

function Nintendo Entertainment System.print_registers(device)
    device = device or Nintendo Entertainment System.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            Nintendo Entertainment System.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function Nintendo Entertainment System.example()
    print("=== Nintendo Entertainment System设备示例 ===")
    
    -- 创建设备实例
    local device = Nintendo Entertainment System.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    Nintendo Entertainment System.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    Nintendo Entertainment System.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("Nintendo Entertainment System.lua$") then
    Nintendo Entertainment System.example()
end

return Nintendo Entertainment System
