--[[
  LPC54606设备定义 - Lua模块
  生成自: NXP/LPC/LPC54606
  版本: 1.0
  日期: 2026-04-28
  作者: VML Team
  描述: 32-bit ARM Cortex-M4 MCU with 256KB Flash, 136KB SRAM, 180MHz
  CPU架构: ARM-Cortex-M4
  位宽: 32位
  时钟频率: 180000000 Hz
]]

local LPC54606 = {}

-- 设备信息
LPC54606.DEVICE_NAME = "LPC54606"
LPC54606.MANUFACTURER = "NXP"
LPC54606.FAMILY = "LPC"
LPC54606.VERSION = "1.0"
LPC54606.ARCHITECTURE = "ARM-Cortex-M4"
LPC54606.BITS = 32
LPC54606.CLOCK_FREQUENCY = 180000000

-- 寄存器地址定义
LPC54606.R0_ADDR = 0x00  -- 
LPC54606.R1_ADDR = 0x04  -- 
LPC54606.R2_ADDR = 0x08  -- 
LPC54606.R3_ADDR = 0x0C  -- 
LPC54606.R4_ADDR = 0x10  -- 
LPC54606.R5_ADDR = 0x14  -- 
LPC54606.SP_ADDR = 0x34  -- 
LPC54606.LR_ADDR = 0x38  -- 
LPC54606.PC_ADDR = 0x3C  -- 

-- 内存段定义
LPC54606.FLASH_START = 0x00000000
LPC54606.FLASH_END = 0x0003FFFF
LPC54606.FLASH_SIZE = 262144  -- 
LPC54606.SRAM_START = 0x20000000
LPC54606.SRAM_END = 0x20021FFF
LPC54606.SRAM_SIZE = 139264  -- 
LPC54606.PERIPHERAL_START = 0x40000000
LPC54606.PERIPHERAL_END = 0x401FFFFF
LPC54606.PERIPHERAL_SIZE = 2097152  -- 

-- 外设定义
-- System Control
LPC54606.SYSCON_BASE = 0x40000000
LPC54606.SYSCON_SYSAHBCLKCTRL_ADDR = 0x80
LPC54606.SYSCON_MAINCLKSEL_ADDR = 0x04
LPC54606.SYSCON_MAINCLKUEN_ADDR = 0x08
LPC54606.SYSCON_SYSPLLCTRL_ADDR = 0x0C
-- General Purpose I/O
LPC54606.GPIO_BASE = 0x400F4000
LPC54606.GPIO_DIR0_ADDR = 0x0000
LPC54606.GPIO_PIN0_ADDR = 0x1000
LPC54606.GPIO_SET0_ADDR = 0x2000
LPC54606.GPIO_CLR0_ADDR = 0x3000
LPC54606.GPIO_NOT0_ADDR = 0x4000
LPC54606.GPIO_DIR1_ADDR = 0x0004
LPC54606.GPIO_PIN1_ADDR = 0x1004
LPC54606.GPIO_SET1_ADDR = 0x2004
LPC54606.GPIO_CLR1_ADDR = 0x3004
LPC54606.GPIO_NOT1_ADDR = 0x4004
-- USART0
LPC54606.USART0_BASE = 0x40086000
LPC54606.USART0_CFG_ADDR = 0x00
LPC54606.USART0_CTRL_ADDR = 0x04
LPC54606.USART0_STAT_ADDR = 0x08
LPC54606.USART0_TXDAT_ADDR = 0x10
LPC54606.USART0_RXDAT_ADDR = 0x14
LPC54606.USART0_BRG_ADDR = 0x20

-- 中断向量定义
LPC54606.INT_RESET = 0  -- 
LPC54606.INT_SVCALL = 11  -- 
LPC54606.INT_USART0 = 24  -- USART0 Interrupt

-- 设备类
function LPC54606.new(memory_base)
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
        self.registers["R4"] = {
            address = 0x10,
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R5"] = {
            address = 0x14,
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
        self.peripherals["SYSCON"] = {
            base = 0x40000000,
            type = "SystemControl",
            description = "System Control",
            registers = {}
        }
        
        local p = self.peripherals["SYSCON"]
        p.registers["SYSAHBCLKCTRL"] = {
            address = 0x80,
            size = 4,
            value = 0
        }
        p.registers["MAINCLKSEL"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["MAINCLKUEN"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["SYSPLLCTRL"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        self.peripherals["GPIO"] = {
            base = 0x400F4000,
            type = "GPIO",
            description = "General Purpose I/O",
            registers = {}
        }
        
        local p = self.peripherals["GPIO"]
        p.registers["DIR0"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["PIN0"] = {
            address = 0x1000,
            size = 4,
            value = 0
        }
        p.registers["SET0"] = {
            address = 0x2000,
            size = 4,
            value = 0
        }
        p.registers["CLR0"] = {
            address = 0x3000,
            size = 4,
            value = 0
        }
        p.registers["NOT0"] = {
            address = 0x4000,
            size = 4,
            value = 0
        }
        p.registers["DIR1"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["PIN1"] = {
            address = 0x1004,
            size = 4,
            value = 0
        }
        p.registers["SET1"] = {
            address = 0x2004,
            size = 4,
            value = 0
        }
        p.registers["CLR1"] = {
            address = 0x3004,
            size = 4,
            value = 0
        }
        p.registers["NOT1"] = {
            address = 0x4004,
            size = 4,
            value = 0
        }
        self.peripherals["USART0"] = {
            base = 0x40086000,
            type = "UART",
            description = "USART0",
            registers = {}
        }
        
        local p = self.peripherals["USART0"]
        p.registers["CFG"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CTRL"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["STAT"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["TXDAT"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["RXDAT"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["BRG"] = {
            address = 0x20,
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
            name = LPC54606.DEVICE_NAME,
            manufacturer = LPC54606.MANUFACTURER,
            family = LPC54606.FAMILY,
            version = LPC54606.VERSION,
            architecture = LPC54606.ARCHITECTURE,
            bits = LPC54606.BITS,
            clock_frequency = LPC54606.CLOCK_FREQUENCY
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
        return string.format("LPC54606(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function LPC54606.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function LPC54606.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function LPC54606.print_device_info(device)
    device = device or LPC54606.new()
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

function LPC54606.print_registers(device)
    device = device or LPC54606.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            LPC54606.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function LPC54606.example()
    print("=== LPC54606设备示例 ===")
    
    -- 创建设备实例
    local device = LPC54606.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    LPC54606.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["R0"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("R0", 0x55)
        print("写入 R0: " .. LPC54606.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("R0")
        print("读取 R0: " .. LPC54606.hex(value))
        
        -- 位操作
        device:set_bit("R0", 0, true)
        local bit0 = device:get_bit("R0", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    LPC54606.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("LPC54606.lua$") then
    LPC54606.example()
end

return LPC54606
