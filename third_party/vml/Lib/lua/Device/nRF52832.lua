--[[
  nRF52832设备定义 - Lua模块
  生成自: Nordic/nRF52/nRF52832
  版本: 1.0
  日期: 2026-04-28
  作者: VML Team
  描述: 32-bit ARM Cortex-M4F BLE SoC with 512KB Flash, 64KB RAM, 64MHz
  CPU架构: ARM-Cortex-M4F
  位宽: 32位
  时钟频率: 64000000 Hz
]]

local nRF52832 = {}

-- 设备信息
nRF52832.DEVICE_NAME = "nRF52832"
nRF52832.MANUFACTURER = "Nordic"
nRF52832.FAMILY = "nRF52"
nRF52832.VERSION = "1.0"
nRF52832.ARCHITECTURE = "ARM-Cortex-M4F"
nRF52832.BITS = 32
nRF52832.CLOCK_FREQUENCY = 64000000

-- 寄存器地址定义
nRF52832.R0_ADDR = 0x00  -- 
nRF52832.R1_ADDR = 0x04  -- 
nRF52832.R2_ADDR = 0x08  -- 
nRF52832.R3_ADDR = 0x0C  -- 
nRF52832.SP_ADDR = 0x34  -- 
nRF52832.LR_ADDR = 0x38  -- 
nRF52832.PC_ADDR = 0x3C  -- 

-- 内存段定义
nRF52832.FLASH_START = 0x00000000
nRF52832.FLASH_END = 0x0007FFFF
nRF52832.FLASH_SIZE = 524288  -- 
nRF52832.SRAM_START = 0x20000000
nRF52832.SRAM_END = 0x2000FFFF
nRF52832.SRAM_SIZE = 65536  -- 
nRF52832.PERIPHERAL_START = 0x40000000
nRF52832.PERIPHERAL_END = 0x400FFFFF
nRF52832.PERIPHERAL_SIZE = 1048576  -- 
nRF52832.FICR_START = 0x10000000
nRF52832.FICR_END = 0x10000FFF
nRF52832.FICR_SIZE = 4096  -- Factory Information Configuration Registers

-- 外设定义
-- General Purpose I/O Port 0
nRF52832.GPIO_P0_BASE = 0x50000000
nRF52832.GPIO_P0_OUT_ADDR = 0x504
nRF52832.GPIO_P0_OUTSET_ADDR = 0x508
nRF52832.GPIO_P0_OUTCLR_ADDR = 0x50C
nRF52832.GPIO_P0_IN_ADDR = 0x510
nRF52832.GPIO_P0_DIR_ADDR = 0x514
nRF52832.GPIO_P0_DIRSET_ADDR = 0x518
nRF52832.GPIO_P0_DIRCLR_ADDR = 0x51C
-- Power Control
nRF52832.POWER_BASE = 0x40000000
nRF52832.POWER_DCDCEN_ADDR = 0x1C4
nRF52832.POWER_RAMSTATUS_ADDR = 0x268
-- Clock Control
nRF52832.CLOCK_BASE = 0x40000000
nRF52832.CLOCK_HFCLKSTART_ADDR = 0x108
nRF52832.CLOCK_HFCLKSTARTED_ADDR = 0x208

-- 中断向量定义
nRF52832.INT_RESET = 0  -- 
nRF52832.INT_SVCALL = 11  -- 

-- 设备类
function nRF52832.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["R0"] = {
            address = 0x00,
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R1"] = {
            address = 0x04,
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R2"] = {
            address = 0x08,
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R3"] = {
            address = 0x0C,
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["SP"] = {
            address = 0x34,
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["LR"] = {
            address = 0x38,
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["PC"] = {
            address = 0x3C,
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["GPIO_P0"] = {
            base = 0x50000000,
            type = "GPIO",
            description = "General Purpose I/O Port 0",
            registers = {}
        }
        
        local p = self.peripherals["GPIO_P0"]
        p.registers["OUT"] = {
            address = 0x504,
            size = 4,
            value = 0
        }
        p.registers["OUTSET"] = {
            address = 0x508,
            size = 4,
            value = 0
        }
        p.registers["OUTCLR"] = {
            address = 0x50C,
            size = 4,
            value = 0
        }
        p.registers["IN"] = {
            address = 0x510,
            size = 4,
            value = 0
        }
        p.registers["DIR"] = {
            address = 0x514,
            size = 4,
            value = 0
        }
        p.registers["DIRSET"] = {
            address = 0x518,
            size = 4,
            value = 0
        }
        p.registers["DIRCLR"] = {
            address = 0x51C,
            size = 4,
            value = 0
        }
        self.peripherals["POWER"] = {
            base = 0x40000000,
            type = "PowerControl",
            description = "Power Control",
            registers = {}
        }
        
        local p = self.peripherals["POWER"]
        p.registers["DCDCEN"] = {
            address = 0x1C4,
            size = 4,
            value = 0
        }
        p.registers["RAMSTATUS"] = {
            address = 0x268,
            size = 4,
            value = 0
        }
        self.peripherals["CLOCK"] = {
            base = 0x40000000,
            type = "ClockControl",
            description = "Clock Control",
            registers = {}
        }
        
        local p = self.peripherals["CLOCK"]
        p.registers["HFCLKSTART"] = {
            address = 0x108,
            size = 4,
            value = 0
        }
        p.registers["HFCLKSTARTED"] = {
            address = 0x208,
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
            name = nRF52832.DEVICE_NAME,
            manufacturer = nRF52832.MANUFACTURER,
            family = nRF52832.FAMILY,
            version = nRF52832.VERSION,
            architecture = nRF52832.ARCHITECTURE,
            bits = nRF52832.BITS,
            clock_frequency = nRF52832.CLOCK_FREQUENCY
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
        return string.format("nRF52832(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function nRF52832.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function nRF52832.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function nRF52832.print_device_info(device)
    device = device or nRF52832.new()
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

function nRF52832.print_registers(device)
    device = device or nRF52832.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            nRF52832.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function nRF52832.example()
    print("=== nRF52832设备示例 ===")
    
    -- 创建设备实例
    local device = nRF52832.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    nRF52832.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["R0"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("R0", 0x55)
        print("写入 R0: " .. nRF52832.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("R0")
        print("读取 R0: " .. nRF52832.hex(value))
        
        -- 位操作
        device:set_bit("R0", 0, true)
        local bit0 = device:get_bit("R0", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    nRF52832.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("nRF52832.lua$") then
    nRF52832.example()
end

return nRF52832
