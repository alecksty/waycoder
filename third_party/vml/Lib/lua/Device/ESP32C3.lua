--[[
  ESP32-C3设备定义 - Lua模块
  生成自: Espressif/ESP32-C/ESP32-C3
  版本: 1.0
  日期: 2026-04-28
  作者: VML Team
  描述: 32-bit RISC-V single-core WiFi + BLE SoC, 160MHz, 400KB SRAM
  CPU架构: RISC-V
  位宽: 32位
  时钟频率: 160000000 Hz
]]

local ESP32_C3 = {}

-- 设备信息
ESP32_C3.DEVICE_NAME = "ESP32-C3"
ESP32_C3.MANUFACTURER = "Espressif"
ESP32_C3.FAMILY = "ESP32-C"
ESP32_C3.VERSION = "1.0"
ESP32_C3.ARCHITECTURE = "RISC-V"
ESP32_C3.BITS = 32
ESP32_C3.CLOCK_FREQUENCY = 160000000

-- 寄存器地址定义
ESP32_C3.X1_ADDR = 0x04  -- Return Address
ESP32_C3.X2_ADDR = 0x08  -- Stack Pointer (SP)
ESP32_C3.X3_ADDR = 0x0C  -- Global Pointer (GP)
ESP32_C3.X8_ADDR = 0x20  -- Frame Pointer (FP)
ESP32_C3.X10_ADDR = 0x28  -- Function Argument (A0)
ESP32_C3.X11_ADDR = 0x2C  -- Function Argument (A1)
ESP32_C3.PC_ADDR = 0x3C  -- Program Counter

-- 内存段定义
ESP32_C3.FLASH_START = 0x42000000
ESP32_C3.FLASH_END = 0x427FFFFF
ESP32_C3.FLASH_SIZE = 8388608  -- Flash via Cache
ESP32_C3.SRAM_START = 0x3FC80000
ESP32_C3.SRAM_END = 0x3FCE3FFF
ESP32_C3.SRAM_SIZE = 409600  -- Internal SRAM
ESP32_C3.PERIPHERAL_START = 0x60000000
ESP32_C3.PERIPHERAL_END = 0x600FFFFF
ESP32_C3.PERIPHERAL_SIZE = 1048576  -- 

-- 外设定义
-- General Purpose I/O
ESP32_C3.GPIO_BASE = 0x60004000
ESP32_C3.GPIO_OUT_ADDR = 0x04
ESP32_C3.GPIO_OUT_W1TS_ADDR = 0x08
ESP32_C3.GPIO_OUT_W1TC_ADDR = 0x0C
ESP32_C3.GPIO_IN_ADDR = 0x10
ESP32_C3.GPIO_ENABLE_ADDR = 0x20
ESP32_C3.GPIO_ENABLE_W1TS_ADDR = 0x24
ESP32_C3.GPIO_ENABLE_W1TC_ADDR = 0x28
-- I/O MUX
ESP32_C3.IO_MUX_BASE = 0x60009000
ESP32_C3.IO_MUX_GPIO0_ADDR = 0x00
ESP32_C3.IO_MUX_GPIO1_ADDR = 0x04
ESP32_C3.IO_MUX_GPIO2_ADDR = 0x08
ESP32_C3.IO_MUX_GPIO3_ADDR = 0x0C
-- RTC Control
ESP32_C3.RTC_CNTL_BASE = 0x60008000
ESP32_C3.RTC_CNTL_OPTIONS0_ADDR = 0x00
ESP32_C3.RTC_CNTL_CLK_CONF_ADDR = 0x30

-- 中断向量定义
ESP32_C3.INT_RESET = 1  -- 
ESP32_C3.INT_MACHINESOFTWARE = 3  -- 
ESP32_C3.INT_MACHINETIMER = 7  -- 
ESP32_C3.INT_MACHINEEXTERNAL = 11  -- 

-- 设备类
function ESP32_C3.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["x1"] = {
            address = 0x04,
            size = 4,
            access = "rw",
            description = "Return Address",
            value = 0
        }
        self.registers["x2"] = {
            address = 0x08,
            size = 4,
            access = "rw",
            description = "Stack Pointer (SP)",
            value = 0
        }
        self.registers["x3"] = {
            address = 0x0C,
            size = 4,
            access = "rw",
            description = "Global Pointer (GP)",
            value = 0
        }
        self.registers["x8"] = {
            address = 0x20,
            size = 4,
            access = "rw",
            description = "Frame Pointer (FP)",
            value = 0
        }
        self.registers["x10"] = {
            address = 0x28,
            size = 4,
            access = "rw",
            description = "Function Argument (A0)",
            value = 0
        }
        self.registers["x11"] = {
            address = 0x2C,
            size = 4,
            access = "rw",
            description = "Function Argument (A1)",
            value = 0
        }
        self.registers["pc"] = {
            address = 0x3C,
            size = 4,
            access = "rw",
            description = "Program Counter",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["GPIO"] = {
            base = 0x60004000,
            type = "GPIO",
            description = "General Purpose I/O",
            registers = {}
        }
        
        local p = self.peripherals["GPIO"]
        p.registers["OUT"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["OUT_W1TS"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["OUT_W1TC"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["IN"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["ENABLE"] = {
            address = 0x20,
            size = 4,
            value = 0
        }
        p.registers["ENABLE_W1TS"] = {
            address = 0x24,
            size = 4,
            value = 0
        }
        p.registers["ENABLE_W1TC"] = {
            address = 0x28,
            size = 4,
            value = 0
        }
        self.peripherals["IO_MUX"] = {
            base = 0x60009000,
            type = "IOMUX",
            description = "I/O MUX",
            registers = {}
        }
        
        local p = self.peripherals["IO_MUX"]
        p.registers["GPIO0"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["GPIO1"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["GPIO2"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["GPIO3"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        self.peripherals["RTC_CNTL"] = {
            base = 0x60008000,
            type = "ResetClock",
            description = "RTC Control",
            registers = {}
        }
        
        local p = self.peripherals["RTC_CNTL"]
        p.registers["OPTIONS0"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CLK_CONF"] = {
            address = 0x30,
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
            name = ESP32_C3.DEVICE_NAME,
            manufacturer = ESP32_C3.MANUFACTURER,
            family = ESP32_C3.FAMILY,
            version = ESP32_C3.VERSION,
            architecture = ESP32_C3.ARCHITECTURE,
            bits = ESP32_C3.BITS,
            clock_frequency = ESP32_C3.CLOCK_FREQUENCY
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
        return string.format("ESP32_C3(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function ESP32_C3.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function ESP32_C3.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function ESP32_C3.print_device_info(device)
    device = device or ESP32_C3.new()
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

function ESP32_C3.print_registers(device)
    device = device or ESP32_C3.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            ESP32_C3.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function ESP32_C3.example()
    print("=== ESP32-C3设备示例 ===")
    
    -- 创建设备实例
    local device = ESP32_C3.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    ESP32_C3.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["x1"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("x1", 0x55)
        print("写入 x1: " .. ESP32_C3.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("x1")
        print("读取 x1: " .. ESP32_C3.hex(value))
        
        -- 位操作
        device:set_bit("x1", 0, true)
        local bit0 = device:get_bit("x1", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    ESP32_C3.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("ESP32_C3.lua$") then
    ESP32_C3.example()
end

return ESP32_C3
