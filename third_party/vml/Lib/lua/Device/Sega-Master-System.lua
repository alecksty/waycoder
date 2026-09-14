--[[
  Sega-Master-System设备定义 - Lua模块
  生成自: Sega/Master System/Sega-Master-System
  版本: 1.0
  日期: 2026-04-17
  作者: VML Team
  描述: Sega Master System 8-bit video game console with Z80 CPU
  CPU架构: Zilog Z80
  位宽: 8位
  时钟频率: 3579545 Hz
]]

local Sega_Master_System = {}

-- 设备信息
Sega_Master_System.DEVICE_NAME = "Sega-Master-System"
Sega_Master_System.MANUFACTURER = "Sega"
Sega_Master_System.FAMILY = "Master System"
Sega_Master_System.VERSION = "1.0"
Sega_Master_System.ARCHITECTURE = "Zilog Z80"
Sega_Master_System.BITS = 8
Sega_Master_System.CLOCK_FREQUENCY = 3579545

-- 寄存器地址定义
Sega_Master_System.A_ADDR = 0  -- Accumulator
Sega_Master_System.F_ADDR = 0  -- Flags
Sega_Master_System.B_ADDR = 0  -- B
Sega_Master_System.C_ADDR = 0  -- C
Sega_Master_System.D_ADDR = 0  -- D
Sega_Master_System.E_ADDR = 0  -- E
Sega_Master_System.H_ADDR = 0  -- H
Sega_Master_System.L_ADDR = 0  -- L
Sega_Master_System.IX_ADDR = 0  -- Index Register X
Sega_Master_System.IY_ADDR = 0  -- Index Register Y
Sega_Master_System.SP_ADDR = 0  -- Stack Pointer
Sega_Master_System.PC_ADDR = 0  -- Program Counter
Sega_Master_System.I_ADDR = 0  -- Interrupt Vector
Sega_Master_System.R_ADDR = 0  -- Memory Refresh

-- 外设定义
-- Video Display Processor (TMS9918A)
Sega_Master_System.VDP_BASE = 
Sega_Master_System.VDP_VDP_DATA_ADDR = 0xBE
Sega_Master_System.VDP_VDP_ADDR_ADDR = 0xBF
Sega_Master_System.VDP_VDP_STATUS_ADDR = 0xBF
-- Programmable Sound Generator (SN76489)
Sega_Master_System.PSG_BASE = 
Sega_Master_System.PSG_PSG_DATA_ADDR = 0x7F
-- I/O ports
Sega_Master_System.IO_BASE = 
Sega_Master_System.IO_IO_PORT_A_ADDR = 0xDC
Sega_Master_System.IO_IO_PORT_B_ADDR = 0xDD
Sega_Master_System.IO_IO_PORT_MISC_ADDR = 0xDE
Sega_Master_System.IO_IO_PORT_VDP_ADDR = 0xDF
-- Memory mapper
Sega_Master_System.MEMORYMAPPER_BASE = 
Sega_Master_System.MEMORYMAPPER_MAPPER_0_ADDR = 0xFFFC
Sega_Master_System.MEMORYMAPPER_MAPPER_1_ADDR = 0xFFFD
Sega_Master_System.MEMORYMAPPER_MAPPER_2_ADDR = 0xFFFE
Sega_Master_System.MEMORYMAPPER_MAPPER_3_ADDR = 0xFFFF
-- FM Sound Unit (optional)
Sega_Master_System.FMUNIT_BASE = 
Sega_Master_System.FMUNIT_FM_ADDR_ADDR = 0xF0
Sega_Master_System.FMUNIT_FM_DATA_ADDR = 0xF1
Sega_Master_System.FMUNIT_FM_DETECT_ADDR = 0xF2

-- 中断向量定义
Sega_Master_System.INT_RST_00 = 0  -- Restart 00h
Sega_Master_System.INT_IM1 = 56  -- Interrupt Mode 1
Sega_Master_System.INT_VBLANK = 56  -- Vertical blank interrupt
Sega_Master_System.INT_LINE = 100  -- Line interrupt

-- 设备类
function Sega_Master_System.new(memory_base)
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
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["VDP"] = {
            base = ,
            type = "Video",
            description = "Video Display Processor (TMS9918A)",
            registers = {}
        }
        
        local p = self.peripherals["VDP"]
        p.registers["VDP_DATA"] = {
            address = 0xBE,
            size = 1,
            value = 0
        }
        p.registers["VDP_ADDR"] = {
            address = 0xBF,
            size = 1,
            value = 0
        }
        p.registers["VDP_STATUS"] = {
            address = 0xBF,
            size = 1,
            value = 0
        }
        self.peripherals["PSG"] = {
            base = ,
            type = "Audio",
            description = "Programmable Sound Generator (SN76489)",
            registers = {}
        }
        
        local p = self.peripherals["PSG"]
        p.registers["PSG_DATA"] = {
            address = 0x7F,
            size = 1,
            value = 0
        }
        self.peripherals["IO"] = {
            base = ,
            type = "IO",
            description = "I/O ports",
            registers = {}
        }
        
        local p = self.peripherals["IO"]
        p.registers["IO_PORT_A"] = {
            address = 0xDC,
            size = 1,
            value = 0
        }
        p.registers["IO_PORT_B"] = {
            address = 0xDD,
            size = 1,
            value = 0
        }
        p.registers["IO_PORT_MISC"] = {
            address = 0xDE,
            size = 1,
            value = 0
        }
        p.registers["IO_PORT_VDP"] = {
            address = 0xDF,
            size = 1,
            value = 0
        }
        self.peripherals["MemoryMapper"] = {
            base = ,
            type = "Memory",
            description = "Memory mapper",
            registers = {}
        }
        
        local p = self.peripherals["MemoryMapper"]
        p.registers["MAPPER_0"] = {
            address = 0xFFFC,
            size = 1,
            value = 0
        }
        p.registers["MAPPER_1"] = {
            address = 0xFFFD,
            size = 1,
            value = 0
        }
        p.registers["MAPPER_2"] = {
            address = 0xFFFE,
            size = 1,
            value = 0
        }
        p.registers["MAPPER_3"] = {
            address = 0xFFFF,
            size = 1,
            value = 0
        }
        self.peripherals["FMUnit"] = {
            base = ,
            type = "Audio",
            description = "FM Sound Unit (optional)",
            registers = {}
        }
        
        local p = self.peripherals["FMUnit"]
        p.registers["FM_ADDR"] = {
            address = 0xF0,
            size = 1,
            value = 0
        }
        p.registers["FM_DATA"] = {
            address = 0xF1,
            size = 1,
            value = 0
        }
        p.registers["FM_DETECT"] = {
            address = 0xF2,
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
            name = Sega_Master_System.DEVICE_NAME,
            manufacturer = Sega_Master_System.MANUFACTURER,
            family = Sega_Master_System.FAMILY,
            version = Sega_Master_System.VERSION,
            architecture = Sega_Master_System.ARCHITECTURE,
            bits = Sega_Master_System.BITS,
            clock_frequency = Sega_Master_System.CLOCK_FREQUENCY
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
        return string.format("Sega_Master_System(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function Sega_Master_System.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function Sega_Master_System.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function Sega_Master_System.print_device_info(device)
    device = device or Sega_Master_System.new()
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

function Sega_Master_System.print_registers(device)
    device = device or Sega_Master_System.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            Sega_Master_System.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function Sega_Master_System.example()
    print("=== Sega-Master-System设备示例 ===")
    
    -- 创建设备实例
    local device = Sega_Master_System.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    Sega_Master_System.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["A"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("A", 0x55)
        print("写入 A: " .. Sega_Master_System.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("A")
        print("读取 A: " .. Sega_Master_System.hex(value))
        
        -- 位操作
        device:set_bit("A", 0, true)
        local bit0 = device:get_bit("A", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    Sega_Master_System.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("Sega_Master_System.lua$") then
    Sega_Master_System.example()
end

return Sega_Master_System
