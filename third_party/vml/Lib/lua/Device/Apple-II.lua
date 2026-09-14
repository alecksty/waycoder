--[[
  Apple-II设备定义 - Lua模块
  生成自: Apple Computer/Apple II/Apple-II
  版本: 1.0
  日期: 2026-04-17
  作者: VML Team
  描述: Apple II personal computer with MOS 6502 CPU, 48KB RAM, and color graphics
  CPU架构: MOS 6502
  位宽: 8位
  时钟频率: 1023000 Hz
]]

local Apple_II = {}

-- 设备信息
Apple_II.DEVICE_NAME = "Apple-II"
Apple_II.MANUFACTURER = "Apple Computer"
Apple_II.FAMILY = "Apple II"
Apple_II.VERSION = "1.0"
Apple_II.ARCHITECTURE = "MOS 6502"
Apple_II.BITS = 8
Apple_II.CLOCK_FREQUENCY = 1023000

-- 寄存器地址定义
Apple_II.A_ADDR = 0  -- Accumulator
Apple_II.X_ADDR = 0  -- Index Register X
Apple_II.Y_ADDR = 0  -- Index Register Y
Apple_II.SP_ADDR = 0  -- Stack Pointer
Apple_II.PC_ADDR = 0  -- Program Counter
Apple_II.P_ADDR = 0  -- Status Register

-- 外设定义
-- Apple II keyboard
Apple_II.KEYBOARD_BASE = 
Apple_II.KEYBOARD_KBD_ADDR = 0xC000
Apple_II.KEYBOARD_KBDSTRB_ADDR = 0xC010
-- Built-in speaker
Apple_II.SPEAKER_BASE = 
Apple_II.SPEAKER_SPKR_ADDR = 0xC030
-- Cassette tape interface
Apple_II.CASSETTE_BASE = 
Apple_II.CASSETTE_TAPEIN_ADDR = 0xC060
Apple_II.CASSETTE_TAPEOUT_ADDR = 0xC020
-- Game controller port
Apple_II.GAMEPORT_BASE = 
Apple_II.GAMEPORT_PADDLE0_ADDR = 0xC064
Apple_II.GAMEPORT_PADDLE1_ADDR = 0xC065
Apple_II.GAMEPORT_PADDLE2_ADDR = 0xC066
Apple_II.GAMEPORT_PADDLE3_ADDR = 0xC067
Apple_II.GAMEPORT_BUTTON0_ADDR = 0xC061
Apple_II.GAMEPORT_BUTTON1_ADDR = 0xC062
-- Disk II controller
Apple_II.DISKCONTROLLER_BASE = 
Apple_II.DISKCONTROLLER_DISKUNIT_ADDR = 0xC0E0
Apple_II.DISKCONTROLLER_DISKCMD_ADDR = 0xC0E8
Apple_II.DISKCONTROLLER_DISKSTAT_ADDR = 0xC0E9
Apple_II.DISKCONTROLLER_DISKDATA_ADDR = 0xC0EA

-- 中断向量定义
Apple_II.INT_NMI = 65526  -- Non-maskable interrupt
Apple_II.INT_RESET = 65528  -- Reset vector
Apple_II.INT_IRQ = 65530  -- Interrupt request
Apple_II.INT_BRK = 65532  -- Break instruction

-- 设备类
function Apple_II.new(memory_base)
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
        self.registers["X"] = {
            address = 0,
            size = 1,
            access = "rw",
            description = "Index Register X",
            value = 0
        }
        self.registers["Y"] = {
            address = 0,
            size = 1,
            access = "rw",
            description = "Index Register Y",
            value = 0
        }
        self.registers["SP"] = {
            address = 0,
            size = 1,
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
        self.registers["P"] = {
            address = 0,
            size = 1,
            access = "rw",
            description = "Status Register",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["Keyboard"] = {
            base = ,
            type = "Input",
            description = "Apple II keyboard",
            registers = {}
        }
        
        local p = self.peripherals["Keyboard"]
        p.registers["KBD"] = {
            address = 0xC000,
            size = 1,
            value = 0
        }
        p.registers["KBDSTRB"] = {
            address = 0xC010,
            size = 1,
            value = 0
        }
        self.peripherals["Speaker"] = {
            base = ,
            type = "Audio",
            description = "Built-in speaker",
            registers = {}
        }
        
        local p = self.peripherals["Speaker"]
        p.registers["SPKR"] = {
            address = 0xC030,
            size = 1,
            value = 0
        }
        self.peripherals["Cassette"] = {
            base = ,
            type = "Storage",
            description = "Cassette tape interface",
            registers = {}
        }
        
        local p = self.peripherals["Cassette"]
        p.registers["TAPEIN"] = {
            address = 0xC060,
            size = 1,
            value = 0
        }
        p.registers["TAPEOUT"] = {
            address = 0xC020,
            size = 1,
            value = 0
        }
        self.peripherals["GamePort"] = {
            base = ,
            type = "Input",
            description = "Game controller port",
            registers = {}
        }
        
        local p = self.peripherals["GamePort"]
        p.registers["PADDLE0"] = {
            address = 0xC064,
            size = 1,
            value = 0
        }
        p.registers["PADDLE1"] = {
            address = 0xC065,
            size = 1,
            value = 0
        }
        p.registers["PADDLE2"] = {
            address = 0xC066,
            size = 1,
            value = 0
        }
        p.registers["PADDLE3"] = {
            address = 0xC067,
            size = 1,
            value = 0
        }
        p.registers["BUTTON0"] = {
            address = 0xC061,
            size = 1,
            value = 0
        }
        p.registers["BUTTON1"] = {
            address = 0xC062,
            size = 1,
            value = 0
        }
        self.peripherals["DiskController"] = {
            base = ,
            type = "Storage",
            description = "Disk II controller",
            registers = {}
        }
        
        local p = self.peripherals["DiskController"]
        p.registers["DISKUNIT"] = {
            address = 0xC0E0,
            size = 1,
            value = 0
        }
        p.registers["DISKCMD"] = {
            address = 0xC0E8,
            size = 1,
            value = 0
        }
        p.registers["DISKSTAT"] = {
            address = 0xC0E9,
            size = 1,
            value = 0
        }
        p.registers["DISKDATA"] = {
            address = 0xC0EA,
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
            name = Apple_II.DEVICE_NAME,
            manufacturer = Apple_II.MANUFACTURER,
            family = Apple_II.FAMILY,
            version = Apple_II.VERSION,
            architecture = Apple_II.ARCHITECTURE,
            bits = Apple_II.BITS,
            clock_frequency = Apple_II.CLOCK_FREQUENCY
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
        return string.format("Apple_II(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function Apple_II.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function Apple_II.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function Apple_II.print_device_info(device)
    device = device or Apple_II.new()
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

function Apple_II.print_registers(device)
    device = device or Apple_II.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            Apple_II.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function Apple_II.example()
    print("=== Apple-II设备示例 ===")
    
    -- 创建设备实例
    local device = Apple_II.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    Apple_II.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["A"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("A", 0x55)
        print("写入 A: " .. Apple_II.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("A")
        print("读取 A: " .. Apple_II.hex(value))
        
        -- 位操作
        device:set_bit("A", 0, true)
        local bit0 = device:get_bit("A", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    Apple_II.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("Apple_II.lua$") then
    Apple_II.example()
end

return Apple_II
