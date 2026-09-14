--[[
  Sega-Genesis设备定义 - Lua模块
  生成自: Sega/Genesis/Mega Drive/Sega-Genesis
  版本: 1.0
  日期: 2026-04-17
  作者: VML Team
  描述: Sega Genesis/Mega Drive 16-bit video game console with Motorola 68000 CPU
  CPU架构: Motorola 68000
  位宽: 32位
  时钟频率: 7670000 Hz
]]

local Sega_Genesis = {}

-- 设备信息
Sega_Genesis.DEVICE_NAME = "Sega-Genesis"
Sega_Genesis.MANUFACTURER = "Sega"
Sega_Genesis.FAMILY = "Genesis/Mega Drive"
Sega_Genesis.VERSION = "1.0"
Sega_Genesis.ARCHITECTURE = "Motorola 68000"
Sega_Genesis.BITS = 32
Sega_Genesis.CLOCK_FREQUENCY = 7670000

-- 寄存器地址定义
Sega_Genesis.D0_ADDR = 0  -- Data Register 0
Sega_Genesis.D1_ADDR = 0  -- Data Register 1
Sega_Genesis.D2_ADDR = 0  -- Data Register 2
Sega_Genesis.D3_ADDR = 0  -- Data Register 3
Sega_Genesis.D4_ADDR = 0  -- Data Register 4
Sega_Genesis.D5_ADDR = 0  -- Data Register 5
Sega_Genesis.D6_ADDR = 0  -- Data Register 6
Sega_Genesis.D7_ADDR = 0  -- Data Register 7
Sega_Genesis.A0_ADDR = 0  -- Address Register 0
Sega_Genesis.A1_ADDR = 0  -- Address Register 1
Sega_Genesis.A2_ADDR = 0  -- Address Register 2
Sega_Genesis.A3_ADDR = 0  -- Address Register 3
Sega_Genesis.A4_ADDR = 0  -- Address Register 4
Sega_Genesis.A5_ADDR = 0  -- Address Register 5
Sega_Genesis.A6_ADDR = 0  -- Address Register 6
Sega_Genesis.A7_ADDR = 0  -- Address Register 7 (SP)
Sega_Genesis.PC_ADDR = 0  -- Program Counter
Sega_Genesis.SR_ADDR = 0  -- Status Register

-- 外设定义
-- Video Display Processor (315-5313)
Sega_Genesis.VDP_BASE = 
Sega_Genesis.VDP_VDP_DATA_ADDR = 0xC00000
Sega_Genesis.VDP_VDP_CONTROL_ADDR = 0xC00004
Sega_Genesis.VDP_VDP_HVCOUNTER_ADDR = 0xC00008
Sega_Genesis.VDP_VDP_PSG_ADDR = 0xC00011
-- FM synthesis sound chip
Sega_Genesis.YM2612_BASE = 
Sega_Genesis.YM2612_YM2612_ADDR0_ADDR = 0xA04000
Sega_Genesis.YM2612_YM2612_DATA0_ADDR = 0xA04001
Sega_Genesis.YM2612_YM2612_ADDR1_ADDR = 0xA04002
Sega_Genesis.YM2612_YM2612_DATA1_ADDR = 0xA04003
-- I/O ports
Sega_Genesis.IOPORTS_BASE = 
Sega_Genesis.IOPORTS_IO_DATA1_ADDR = 0xA10002
Sega_Genesis.IOPORTS_IO_DATA2_ADDR = 0xA10004
Sega_Genesis.IOPORTS_IO_DATA3_ADDR = 0xA10006
Sega_Genesis.IOPORTS_IO_CTRL1_ADDR = 0xA10008
Sega_Genesis.IOPORTS_IO_CTRL2_ADDR = 0xA1000A
Sega_Genesis.IOPORTS_IO_CTRL3_ADDR = 0xA1000C
-- TradeMark Security System
Sega_Genesis.TMSS_BASE = 
Sega_Genesis.TMSS_TMSS_ADDR = 0xA14000
-- Z80 bus control
Sega_Genesis.Z80BUS_BASE = 
Sega_Genesis.Z80BUS_Z80_BUSREQ_ADDR = 0xA11100
Sega_Genesis.Z80BUS_Z80_RESET_ADDR = 0xA11200
Sega_Genesis.Z80BUS_Z80_YM2612_ADDR = 0xA04000

-- 中断向量定义
Sega_Genesis.INT_RESET_SP = 0  -- Reset (Initial SP)
Sega_Genesis.INT_RESET_PC = 4  -- Reset (Initial PC)
Sega_Genesis.INT_HBLANK = 24  -- Horizontal blank interrupt
Sega_Genesis.INT_VBLANK = 28  -- Vertical blank interrupt
Sega_Genesis.INT_EXTINT1 = 32  -- External interrupt 1
Sega_Genesis.INT_EXTINT2 = 36  -- External interrupt 2
Sega_Genesis.INT_EXTINT3 = 40  -- External interrupt 3
Sega_Genesis.INT_EXTINT4 = 44  -- External interrupt 4
Sega_Genesis.INT_EXTINT5 = 48  -- External interrupt 5
Sega_Genesis.INT_EXTINT6 = 52  -- External interrupt 6
Sega_Genesis.INT_EXTINT7 = 56  -- External interrupt 7

-- 设备类
function Sega_Genesis.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["D0"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Data Register 0",
            value = 0
        }
        self.registers["D1"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Data Register 1",
            value = 0
        }
        self.registers["D2"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Data Register 2",
            value = 0
        }
        self.registers["D3"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Data Register 3",
            value = 0
        }
        self.registers["D4"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Data Register 4",
            value = 0
        }
        self.registers["D5"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Data Register 5",
            value = 0
        }
        self.registers["D6"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Data Register 6",
            value = 0
        }
        self.registers["D7"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Data Register 7",
            value = 0
        }
        self.registers["A0"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Address Register 0",
            value = 0
        }
        self.registers["A1"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Address Register 1",
            value = 0
        }
        self.registers["A2"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Address Register 2",
            value = 0
        }
        self.registers["A3"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Address Register 3",
            value = 0
        }
        self.registers["A4"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Address Register 4",
            value = 0
        }
        self.registers["A5"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Address Register 5",
            value = 0
        }
        self.registers["A6"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Address Register 6",
            value = 0
        }
        self.registers["A7"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Address Register 7 (SP)",
            value = 0
        }
        self.registers["PC"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Program Counter",
            value = 0
        }
        self.registers["SR"] = {
            address = 0,
            size = 2,
            access = "rw",
            description = "Status Register",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["VDP"] = {
            base = ,
            type = "Video",
            description = "Video Display Processor (315-5313)",
            registers = {}
        }
        
        local p = self.peripherals["VDP"]
        p.registers["VDP_DATA"] = {
            address = 0xC00000,
            size = 2,
            value = 0
        }
        p.registers["VDP_CONTROL"] = {
            address = 0xC00004,
            size = 2,
            value = 0
        }
        p.registers["VDP_HVCOUNTER"] = {
            address = 0xC00008,
            size = 2,
            value = 0
        }
        p.registers["VDP_PSG"] = {
            address = 0xC00011,
            size = 1,
            value = 0
        }
        self.peripherals["YM2612"] = {
            base = ,
            type = "Audio",
            description = "FM synthesis sound chip",
            registers = {}
        }
        
        local p = self.peripherals["YM2612"]
        p.registers["YM2612_ADDR0"] = {
            address = 0xA04000,
            size = 1,
            value = 0
        }
        p.registers["YM2612_DATA0"] = {
            address = 0xA04001,
            size = 1,
            value = 0
        }
        p.registers["YM2612_ADDR1"] = {
            address = 0xA04002,
            size = 1,
            value = 0
        }
        p.registers["YM2612_DATA1"] = {
            address = 0xA04003,
            size = 1,
            value = 0
        }
        self.peripherals["IOPorts"] = {
            base = ,
            type = "IO",
            description = "I/O ports",
            registers = {}
        }
        
        local p = self.peripherals["IOPorts"]
        p.registers["IO_DATA1"] = {
            address = 0xA10002,
            size = 1,
            value = 0
        }
        p.registers["IO_DATA2"] = {
            address = 0xA10004,
            size = 1,
            value = 0
        }
        p.registers["IO_DATA3"] = {
            address = 0xA10006,
            size = 1,
            value = 0
        }
        p.registers["IO_CTRL1"] = {
            address = 0xA10008,
            size = 1,
            value = 0
        }
        p.registers["IO_CTRL2"] = {
            address = 0xA1000A,
            size = 1,
            value = 0
        }
        p.registers["IO_CTRL3"] = {
            address = 0xA1000C,
            size = 1,
            value = 0
        }
        self.peripherals["TMSS"] = {
            base = ,
            type = "Security",
            description = "TradeMark Security System",
            registers = {}
        }
        
        local p = self.peripherals["TMSS"]
        p.registers["TMSS"] = {
            address = 0xA14000,
            size = 1,
            value = 0
        }
        self.peripherals["Z80Bus"] = {
            base = ,
            type = "Bus",
            description = "Z80 bus control",
            registers = {}
        }
        
        local p = self.peripherals["Z80Bus"]
        p.registers["Z80_BUSREQ"] = {
            address = 0xA11100,
            size = 2,
            value = 0
        }
        p.registers["Z80_RESET"] = {
            address = 0xA11200,
            size = 2,
            value = 0
        }
        p.registers["Z80_YM2612"] = {
            address = 0xA04000,
            size = 4,
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
            name = Sega_Genesis.DEVICE_NAME,
            manufacturer = Sega_Genesis.MANUFACTURER,
            family = Sega_Genesis.FAMILY,
            version = Sega_Genesis.VERSION,
            architecture = Sega_Genesis.ARCHITECTURE,
            bits = Sega_Genesis.BITS,
            clock_frequency = Sega_Genesis.CLOCK_FREQUENCY
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
        return string.format("Sega_Genesis(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function Sega_Genesis.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function Sega_Genesis.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function Sega_Genesis.print_device_info(device)
    device = device or Sega_Genesis.new()
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

function Sega_Genesis.print_registers(device)
    device = device or Sega_Genesis.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            Sega_Genesis.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function Sega_Genesis.example()
    print("=== Sega-Genesis设备示例 ===")
    
    -- 创建设备实例
    local device = Sega_Genesis.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    Sega_Genesis.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["D0"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("D0", 0x55)
        print("写入 D0: " .. Sega_Genesis.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("D0")
        print("读取 D0: " .. Sega_Genesis.hex(value))
        
        -- 位操作
        device:set_bit("D0", 0, true)
        local bit0 = device:get_bit("D0", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    Sega_Genesis.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("Sega_Genesis.lua$") then
    Sega_Genesis.example()
end

return Sega_Genesis
