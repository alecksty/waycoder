--[[
  NUC1262SE设备定义 - Lua模块
  生成自: Nuvoton/NuMicro/NUC1262SE
  版本: 1.0
  日期: 2026-04-29
  作者: VML Team
  描述: 32-bit ARM Cortex-M4F MCU with 512KB Flash, 96KB SRAM, 72MHz, USB
  CPU架构: ARM-Cortex-M4F
  位宽: 32位
  时钟频率: 72000000 Hz
]]

local NUC1262SE = {}

-- 设备信息
NUC1262SE.DEVICE_NAME = "NUC1262SE"
NUC1262SE.MANUFACTURER = "Nuvoton"
NUC1262SE.FAMILY = "NuMicro"
NUC1262SE.VERSION = "1.0"
NUC1262SE.ARCHITECTURE = "ARM-Cortex-M4F"
NUC1262SE.BITS = 32
NUC1262SE.CLOCK_FREQUENCY = 72000000

-- 设备类
function NUC1262SE.new(memory_base)
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
            name = NUC1262SE.DEVICE_NAME,
            manufacturer = NUC1262SE.MANUFACTURER,
            family = NUC1262SE.FAMILY,
            version = NUC1262SE.VERSION,
            architecture = NUC1262SE.ARCHITECTURE,
            bits = NUC1262SE.BITS,
            clock_frequency = NUC1262SE.CLOCK_FREQUENCY
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
        return string.format("NUC1262SE(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function NUC1262SE.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function NUC1262SE.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function NUC1262SE.print_device_info(device)
    device = device or NUC1262SE.new()
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

function NUC1262SE.print_registers(device)
    device = device or NUC1262SE.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            NUC1262SE.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function NUC1262SE.example()
    print("=== NUC1262SE设备示例 ===")
    
    -- 创建设备实例
    local device = NUC1262SE.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    NUC1262SE.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    NUC1262SE.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("NUC1262SE.lua$") then
    NUC1262SE.example()
end

return NUC1262SE
