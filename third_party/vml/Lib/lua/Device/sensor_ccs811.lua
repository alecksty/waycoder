--[[
  CCS811设备定义 - Lua模块
  生成自: AMS/ScioSense/Sensor/CCS811
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: CCS811 VOC/eCO2 Air Quality Sensor (I2C, 400-8192ppm CO2, 0-1187ppb TVOC)
  CPU架构: Sensor
  位宽: 16位
  时钟频率: 400000 Hz
]]

local CCS811 = {}

-- 设备信息
CCS811.DEVICE_NAME = "CCS811"
CCS811.MANUFACTURER = "AMS/ScioSense"
CCS811.FAMILY = "Sensor"
CCS811.VERSION = "1.0"
CCS811.ARCHITECTURE = "Sensor"
CCS811.BITS = 16
CCS811.CLOCK_FREQUENCY = 400000

-- 外设定义
-- CCS811 Air Quality Sensor (0x5A/0x5B, 1.8V-3.6V)
CCS811.CCS811_BASE = 0x5A
CCS811.CCS811_STATUS_ADDR = 0x00
CCS811.CCS811_MEAS_MODE_ADDR = 0x01
CCS811.CCS811_ALG_RESULT_ADDR = 0x02
CCS811.CCS811_ECO2_ADDR = 0x02
CCS811.CCS811_TVOC_ADDR = 0x04
CCS811.CCS811_RAW_DATA_ADDR = 0x06
CCS811.CCS811_BASELINE_ADDR = 0x0B
CCS811.CCS811_HW_ID_ADDR = 0x20
CCS811.CCS811_ERROR_ID_ADDR = 0xE0
CCS811.CCS811_APP_START_ADDR = 0xF4
CCS811.CCS811_SW_RESET_ADDR = 0xFF

-- 中断向量定义
CCS811.INT_INT = 0  -- Data ready / interrupt pin

-- 引脚定义
CCS811.PIN_WAKE = 1  -- Wake pin (active low)

-- 设备类
function CCS811.new(memory_base)
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
        self.peripherals["CCS811"] = {
            base = 0x5A,
            type = "I2C",
            description = "CCS811 Air Quality Sensor (0x5A/0x5B, 1.8V-3.6V)",
            registers = {}
        }
        
        local p = self.peripherals["CCS811"]
        p.registers["STATUS"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["MEAS_MODE"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["ALG_RESULT"] = {
            address = 0x02,
            size = 4,
            value = 0
        }
        p.registers["ECO2"] = {
            address = 0x02,
            size = 2,
            value = 0
        }
        p.registers["TVOC"] = {
            address = 0x04,
            size = 2,
            value = 0
        }
        p.registers["RAW_DATA"] = {
            address = 0x06,
            size = 2,
            value = 0
        }
        p.registers["BASELINE"] = {
            address = 0x0B,
            size = 2,
            value = 0
        }
        p.registers["HW_ID"] = {
            address = 0x20,
            size = 1,
            value = 0
        }
        p.registers["ERROR_ID"] = {
            address = 0xE0,
            size = 1,
            value = 0
        }
        p.registers["APP_START"] = {
            address = 0xF4,
            size = 1,
            value = 0
        }
        p.registers["SW_RESET"] = {
            address = 0xFF,
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
            name = CCS811.DEVICE_NAME,
            manufacturer = CCS811.MANUFACTURER,
            family = CCS811.FAMILY,
            version = CCS811.VERSION,
            architecture = CCS811.ARCHITECTURE,
            bits = CCS811.BITS,
            clock_frequency = CCS811.CLOCK_FREQUENCY
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
        return string.format("CCS811(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function CCS811.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function CCS811.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function CCS811.print_device_info(device)
    device = device or CCS811.new()
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

function CCS811.print_registers(device)
    device = device or CCS811.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            CCS811.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function CCS811.example()
    print("=== CCS811设备示例 ===")
    
    -- 创建设备实例
    local device = CCS811.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    CCS811.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    CCS811.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("CCS811.lua$") then
    CCS811.example()
end

return CCS811
