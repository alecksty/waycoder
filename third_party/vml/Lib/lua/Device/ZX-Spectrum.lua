--[[
  ZX-Spectrum设备定义 - Lua模块
  生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum
  版本: 1.0
  日期: 2026-04-17
  作者: VML Team
  描述: ZX Spectrum 48K home computer with Z80 CPU, 48KB RAM, and color graphics
  CPU架构: Zilog Z80
  位宽: 8位
  时钟频率: 3500000 Hz
]]

local ZX_Spectrum = {}

-- 设备信息
ZX_Spectrum.DEVICE_NAME = "ZX-Spectrum"
ZX_Spectrum.MANUFACTURER = "Sinclair Research"
ZX_Spectrum.FAMILY = "ZX Spectrum"
ZX_Spectrum.VERSION = "1.0"
ZX_Spectrum.ARCHITECTURE = "Zilog Z80"
ZX_Spectrum.BITS = 8
ZX_Spectrum.CLOCK_FREQUENCY = 3500000

-- 寄存器地址定义
ZX_Spectrum.A_ADDR = 0  -- Accumulator
ZX_Spectrum.F_ADDR = 0  -- Flags
ZX_Spectrum.B_ADDR = 0  -- B
ZX_Spectrum.C_ADDR = 0  -- C
ZX_Spectrum.D_ADDR = 0  -- D
ZX_Spectrum.E_ADDR = 0  -- E
ZX_Spectrum.H_ADDR = 0  -- H
ZX_Spectrum.L_ADDR = 0  -- L
ZX_Spectrum.IX_ADDR = 0  -- Index Register X
ZX_Spectrum.IY_ADDR = 0  -- Index Register Y
ZX_Spectrum.SP_ADDR = 0  -- Stack Pointer
ZX_Spectrum.PC_ADDR = 0  -- Program Counter
ZX_Spectrum.I_ADDR = 0  -- Interrupt Vector
ZX_Spectrum.R_ADDR = 0  -- Memory Refresh
ZX_Spectrum.AF_ADDR = 0  -- Alternate AF
ZX_Spectrum.BC_ADDR = 0  -- Alternate BC
ZX_Spectrum.DE_ADDR = 0  -- Alternate DE
ZX_Spectrum.HL_ADDR = 0  -- Alternate HL

-- 外设定义
-- Uncommitted Logic Array (video and I/O)
ZX_Spectrum.ULA_BASE = 
ZX_Spectrum.ULA_ULA_PORT_FE_ADDR = 0xFE
ZX_Spectrum.ULA_ULA_BORDER_ADDR = 0xFE
ZX_Spectrum.ULA_ULA_BEEPER_ADDR = 0xFE
ZX_Spectrum.ULA_ULA_MIC_ADDR = 0xFE
-- General Instruments AY-3-8912 sound chip
ZX_Spectrum.AY_3_8912_BASE = 
ZX_Spectrum.AY_3_8912_AY_REG_SEL_ADDR = 0xFFFD
ZX_Spectrum.AY_3_8912_AY_DATA_ADDR = 0xBFFD
ZX_Spectrum.AY_3_8912_AY_READ_ADDR = 0xFFFD
-- 40-key rubber keyboard
ZX_Spectrum.KEYBOARD_BASE = 
ZX_Spectrum.KEYBOARD_KEY_ROW0_ADDR = 0xFEFE
ZX_Spectrum.KEYBOARD_KEY_ROW1_ADDR = 0xFDFE
ZX_Spectrum.KEYBOARD_KEY_ROW2_ADDR = 0xFBFE
ZX_Spectrum.KEYBOARD_KEY_ROW3_ADDR = 0xF7FE
ZX_Spectrum.KEYBOARD_KEY_ROW4_ADDR = 0xEFFE
ZX_Spectrum.KEYBOARD_KEY_ROW5_ADDR = 0xDFFE
ZX_Spectrum.KEYBOARD_KEY_ROW6_ADDR = 0xBFFE
ZX_Spectrum.KEYBOARD_KEY_ROW7_ADDR = 0x7FFE
-- Kempston joystick interface
ZX_Spectrum.KEMPSTON_BASE = 
ZX_Spectrum.KEMPSTON_KEMPSTON_JOY_ADDR = 0x1F
-- ZX Interface 1 (RS-232 and Microdrive)
ZX_Spectrum.INTERFACE1_BASE = 
ZX_Spectrum.INTERFACE1_IF1_STATUS_ADDR = 0x1FFD
ZX_Spectrum.INTERFACE1_IF1_DATA_ADDR = 0x3FFD
-- ZX Interface 2 (joystick and ROM cartridge)
ZX_Spectrum.INTERFACE2_BASE = 
ZX_Spectrum.INTERFACE2_IF2_JOY1_ADDR = 0x1F
ZX_Spectrum.INTERFACE2_IF2_JOY2_ADDR = 0x37

-- 中断向量定义
ZX_Spectrum.INT_IM1 = 56  -- Interrupt Mode 1
ZX_Spectrum.INT_RST_00 = 0  -- Restart 00h
ZX_Spectrum.INT_RST_08 = 8  -- Restart 08h
ZX_Spectrum.INT_RST_10 = 16  -- Restart 10h
ZX_Spectrum.INT_RST_18 = 24  -- Restart 18h
ZX_Spectrum.INT_RST_20 = 32  -- Restart 20h
ZX_Spectrum.INT_RST_28 = 40  -- Restart 28h
ZX_Spectrum.INT_RST_30 = 48  -- Restart 30h
ZX_Spectrum.INT_RST_38 = 56  -- Restart 38h

-- 设备类
function ZX_Spectrum.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["A"] = {
            address = 0,
            size = 1,
            access = "rw",
            description = "Accumulator",
            value = 0
        }
        self.registers["F"] = {
            address = 0,
            size = 1,
            access = "rw",
            description = "Flags",
            value = 0
        }
        self.registers["B"] = {
            address = 0,
            size = 1,
            access = "rw",
            description = "B",
            value = 0
        }
        self.registers["C"] = {
            address = 0,
            size = 1,
            access = "rw",
            description = "C",
            value = 0
        }
        self.registers["D"] = {
            address = 0,
            size = 1,
            access = "rw",
            description = "D",
            value = 0
        }
        self.registers["E"] = {
            address = 0,
            size = 1,
            access = "rw",
            description = "E",
            value = 0
        }
        self.registers["H"] = {
            address = 0,
            size = 1,
            access = "rw",
            description = "H",
            value = 0
        }
        self.registers["L"] = {
            address = 0,
            size = 1,
            access = "rw",
            description = "L",
            value = 0
        }
        self.registers["IX"] = {
            address = 0,
            size = 2,
            access = "rw",
            description = "Index Register X",
            value = 0
        }
        self.registers["IY"] = {
            address = 0,
            size = 2,
            access = "rw",
            description = "Index Register Y",
            value = 0
        }
        self.registers["SP"] = {
            address = 0,
            size = 2,
            access = "rw",
            description = "Stack Pointer",
            value = 0
        }
        self.registers["PC"] = {
            address = 0,
            size = 2,
            access = "rw",
            description = "Program Counter",
            value = 0
        }
        self.registers["I"] = {
            address = 0,
            size = 1,
            access = "rw",
            description = "Interrupt Vector",
            value = 0
        }
        self.registers["R"] = {
            address = 0,
            size = 1,
            access = "rw",
            description = "Memory Refresh",
            value = 0
        }
        self.registers["AF'"] = {
            address = 0,
            size = 2,
            access = "rw",
            description = "Alternate AF",
            value = 0
        }
        self.registers["BC'"] = {
            address = 0,
            size = 2,
            access = "rw",
            description = "Alternate BC",
            value = 0
        }
        self.registers["DE'"] = {
            address = 0,
            size = 2,
            access = "rw",
            description = "Alternate DE",
            value = 0
        }
        self.registers["HL'"] = {
            address = 0,
            size = 2,
            access = "rw",
            description = "Alternate HL",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["ULA"] = {
            base = ,
            type = "Video",
            description = "Uncommitted Logic Array (video and I/O)",
            registers = {}
        }
        
        local p = self.peripherals["ULA"]
        p.registers["ULA_PORT_FE"] = {
            address = 0xFE,
            size = 1,
            value = 0
        }
        p.registers["ULA_BORDER"] = {
            address = 0xFE,
            size = 1,
            value = 0
        }
        p.registers["ULA_BEEPER"] = {
            address = 0xFE,
            size = 1,
            value = 0
        }
        p.registers["ULA_MIC"] = {
            address = 0xFE,
            size = 1,
            value = 0
        }
        self.peripherals["AY-3-8912"] = {
            base = ,
            type = "Audio",
            description = "General Instruments AY-3-8912 sound chip",
            registers = {}
        }
        
        local p = self.peripherals["AY-3-8912"]
        p.registers["AY_REG_SEL"] = {
            address = 0xFFFD,
            size = 1,
            value = 0
        }
        p.registers["AY_DATA"] = {
            address = 0xBFFD,
            size = 1,
            value = 0
        }
        p.registers["AY_READ"] = {
            address = 0xFFFD,
            size = 1,
            value = 0
        }
        self.peripherals["Keyboard"] = {
            base = ,
            type = "Input",
            description = "40-key rubber keyboard",
            registers = {}
        }
        
        local p = self.peripherals["Keyboard"]
        p.registers["KEY_ROW0"] = {
            address = 0xFEFE,
            size = 1,
            value = 0
        }
        p.registers["KEY_ROW1"] = {
            address = 0xFDFE,
            size = 1,
            value = 0
        }
        p.registers["KEY_ROW2"] = {
            address = 0xFBFE,
            size = 1,
            value = 0
        }
        p.registers["KEY_ROW3"] = {
            address = 0xF7FE,
            size = 1,
            value = 0
        }
        p.registers["KEY_ROW4"] = {
            address = 0xEFFE,
            size = 1,
            value = 0
        }
        p.registers["KEY_ROW5"] = {
            address = 0xDFFE,
            size = 1,
            value = 0
        }
        p.registers["KEY_ROW6"] = {
            address = 0xBFFE,
            size = 1,
            value = 0
        }
        p.registers["KEY_ROW7"] = {
            address = 0x7FFE,
            size = 1,
            value = 0
        }
        self.peripherals["Kempston"] = {
            base = ,
            type = "Input",
            description = "Kempston joystick interface",
            registers = {}
        }
        
        local p = self.peripherals["Kempston"]
        p.registers["KEMPSTON_JOY"] = {
            address = 0x1F,
            size = 1,
            value = 0
        }
        self.peripherals["Interface1"] = {
            base = ,
            type = "Storage",
            description = "ZX Interface 1 (RS-232 and Microdrive)",
            registers = {}
        }
        
        local p = self.peripherals["Interface1"]
        p.registers["IF1_STATUS"] = {
            address = 0x1FFD,
            size = 1,
            value = 0
        }
        p.registers["IF1_DATA"] = {
            address = 0x3FFD,
            size = 1,
            value = 0
        }
        self.peripherals["Interface2"] = {
            base = ,
            type = "Storage",
            description = "ZX Interface 2 (joystick and ROM cartridge)",
            registers = {}
        }
        
        local p = self.peripherals["Interface2"]
        p.registers["IF2_JOY1"] = {
            address = 0x1F,
            size = 1,
            value = 0
        }
        p.registers["IF2_JOY2"] = {
            address = 0x37,
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
            name = ZX_Spectrum.DEVICE_NAME,
            manufacturer = ZX_Spectrum.MANUFACTURER,
            family = ZX_Spectrum.FAMILY,
            version = ZX_Spectrum.VERSION,
            architecture = ZX_Spectrum.ARCHITECTURE,
            bits = ZX_Spectrum.BITS,
            clock_frequency = ZX_Spectrum.CLOCK_FREQUENCY
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
        return string.format("ZX_Spectrum(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function ZX_Spectrum.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function ZX_Spectrum.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function ZX_Spectrum.print_device_info(device)
    device = device or ZX_Spectrum.new()
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

function ZX_Spectrum.print_registers(device)
    device = device or ZX_Spectrum.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            ZX_Spectrum.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function ZX_Spectrum.example()
    print("=== ZX-Spectrum设备示例 ===")
    
    -- 创建设备实例
    local device = ZX_Spectrum.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    ZX_Spectrum.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["A"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("A", 0x55)
        print("写入 A: " .. ZX_Spectrum.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("A")
        print("读取 A: " .. ZX_Spectrum.hex(value))
        
        -- 位操作
        device:set_bit("A", 0, true)
        local bit0 = device:get_bit("A", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    ZX_Spectrum.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("ZX_Spectrum.lua$") then
    ZX_Spectrum.example()
end

return ZX_Spectrum
