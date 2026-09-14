--[[
  NEO6M设备定义 - Lua模块
  生成自: u-blox/GPS/NEO6M
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: NEO-6M GPS Module (UART, 50-channel, -162dBm tracking)
  CPU架构: GPS
  位宽: 8位
  时钟频率: 9600 Hz
]]

local NEO6M = {}

-- 设备信息
NEO6M.DEVICE_NAME = "NEO6M"
NEO6M.MANUFACTURER = "u-blox"
NEO6M.FAMILY = "GPS"
NEO6M.VERSION = "1.0"
NEO6M.ARCHITECTURE = "GPS"
NEO6M.BITS = 8
NEO6M.CLOCK_FREQUENCY = 9600

-- 外设定义
-- NEO-6M GPS Module (UART 9600bps, 3.3V-5V)
NEO6M.NEO6M_BASE = 0x00
NEO6M.NEO6M_LATITUDE_ADDR = 0x00
NEO6M.NEO6M_LONGITUDE_ADDR = 0x04
NEO6M.NEO6M_ALTITUDE_ADDR = 0x08
NEO6M.NEO6M_SPEED_ADDR = 0x0C
NEO6M.NEO6M_HEADING_ADDR = 0x0E
NEO6M.NEO6M_SATELLITES_ADDR = 0x10
NEO6M.NEO6M_HDOP_ADDR = 0x11
NEO6M.NEO6M_FIX_TYPE_ADDR = 0x13
NEO6M.NEO6M_DATE_ADDR = 0x14
NEO6M.NEO6M_TIME_ADDR = 0x18
NEO6M.NEO6M_VALID_ADDR = 0x1C

-- 设备类
function NEO6M.new(memory_base)
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
        self.peripherals["NEO6M"] = {
            base = 0x00,
            type = "UART",
            description = "NEO-6M GPS Module (UART 9600bps, 3.3V-5V)",
            registers = {}
        }
        
        local p = self.peripherals["NEO6M"]
        p.registers["LATITUDE"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["LONGITUDE"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["ALTITUDE"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["SPEED"] = {
            address = 0x0C,
            size = 2,
            value = 0
        }
        p.registers["HEADING"] = {
            address = 0x0E,
            size = 2,
            value = 0
        }
        p.registers["SATELLITES"] = {
            address = 0x10,
            size = 1,
            value = 0
        }
        p.registers["HDOP"] = {
            address = 0x11,
            size = 2,
            value = 0
        }
        p.registers["FIX_TYPE"] = {
            address = 0x13,
            size = 1,
            value = 0
        }
        p.registers["DATE"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["TIME"] = {
            address = 0x18,
            size = 4,
            value = 0
        }
        p.registers["VALID"] = {
            address = 0x1C,
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
            name = NEO6M.DEVICE_NAME,
            manufacturer = NEO6M.MANUFACTURER,
            family = NEO6M.FAMILY,
            version = NEO6M.VERSION,
            architecture = NEO6M.ARCHITECTURE,
            bits = NEO6M.BITS,
            clock_frequency = NEO6M.CLOCK_FREQUENCY
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
        return string.format("NEO6M(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function NEO6M.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function NEO6M.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function NEO6M.print_device_info(device)
    device = device or NEO6M.new()
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

function NEO6M.print_registers(device)
    device = device or NEO6M.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            NEO6M.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function NEO6M.example()
    print("=== NEO6M设备示例 ===")
    
    -- 创建设备实例
    local device = NEO6M.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    NEO6M.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    NEO6M.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("NEO6M.lua$") then
    NEO6M.example()
end

return NEO6M
