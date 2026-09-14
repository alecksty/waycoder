--[[
  i.MX RT1062设备定义 - Lua模块
  生成自: NXP/i.MX RT/i.MX RT1062
  版本: 1.0
  日期: 2026-04-29
  作者: VML Team
  描述: 32-bit ARM Cortex-M7 MCU with 1MB SRAM, 600MHz, crossover processor
  CPU架构: ARM-Cortex-M7
  位宽: 32位
  时钟频率: 528000000 Hz
]]

local i.MX RT1062 = {}

-- 设备信息
i.MX RT1062.DEVICE_NAME = "i.MX RT1062"
i.MX RT1062.MANUFACTURER = "NXP"
i.MX RT1062.FAMILY = "i.MX RT"
i.MX RT1062.VERSION = "1.0"
i.MX RT1062.ARCHITECTURE = "ARM-Cortex-M7"
i.MX RT1062.BITS = 32
i.MX RT1062.CLOCK_FREQUENCY = 528000000

-- 外设定义
-- LPUART 1
i.MX RT1062.UART1_BASE = 0x40184000
i.MX RT1062.UART1_VERID_ADDR = 0x000
i.MX RT1062.UART1_CTRL_ADDR = 0x010
i.MX RT1062.UART1_STAT_ADDR = 0x014
i.MX RT1062.UART1_DATA_ADDR = 0x01C
i.MX RT1062.UART1_BAUD_ADDR = 0x024
-- LPUART 2
i.MX RT1062.UART2_BASE = 0x40188000
i.MX RT1062.UART2_CTRL_ADDR = 0x010
i.MX RT1062.UART2_STAT_ADDR = 0x014
i.MX RT1062.UART2_DATA_ADDR = 0x01C
i.MX RT1062.UART2_BAUD_ADDR = 0x024
-- GPIO 1
i.MX RT1062.GPIO1_BASE = 0x401B8000
i.MX RT1062.GPIO1_DR_ADDR = 0x000
i.MX RT1062.GPIO1_GDIR_ADDR = 0x004
i.MX RT1062.GPIO1_PSR_ADDR = 0x008
i.MX RT1062.GPIO1_ICR1_ADDR = 0x00C
i.MX RT1062.GPIO1_ICR2_ADDR = 0x010
i.MX RT1062.GPIO1_IMR_ADDR = 0x014
i.MX RT1062.GPIO1_ISR_ADDR = 0x018
i.MX RT1062.GPIO1_EDGE_SEL_ADDR = 0x01C
-- GPT 定时器 1
i.MX RT1062.GPT1_BASE = 0x401EC000
i.MX RT1062.GPT1_CR_ADDR = 0x000
i.MX RT1062.GPT1_PR_ADDR = 0x004
i.MX RT1062.GPT1_SR_ADDR = 0x008
i.MX RT1062.GPT1_IR_ADDR = 0x00C
i.MX RT1062.GPT1_OCR1_ADDR = 0x010
i.MX RT1062.GPT1_CNT_ADDR = 0x024
-- USB OTG 1
i.MX RT1062.USB1_BASE = 0x402E0000
i.MX RT1062.USB1_ID_ADDR = 0x000
i.MX RT1062.USB1_OTGSC_ADDR = 0x00C
i.MX RT1062.USB1_USBCMD_ADDR = 0x100
i.MX RT1062.USB1_PORTSC1_ADDR = 0x184

-- 设备类
function i.MX RT1062.new(memory_base)
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
        self.peripherals["UART1"] = {
            base = 0x40184000,
            type = "uart",
            description = "LPUART 1",
            registers = {}
        }
        
        local p = self.peripherals["UART1"]
        p.registers["VERID"] = {
            address = 0x000,
            size = 4,
            value = 0
        }
        p.registers["CTRL"] = {
            address = 0x010,
            size = 4,
            value = 0
        }
        p.registers["STAT"] = {
            address = 0x014,
            size = 4,
            value = 0
        }
        p.registers["DATA"] = {
            address = 0x01C,
            size = 4,
            value = 0
        }
        p.registers["BAUD"] = {
            address = 0x024,
            size = 4,
            value = 0
        }
        self.peripherals["UART2"] = {
            base = 0x40188000,
            type = "uart",
            description = "LPUART 2",
            registers = {}
        }
        
        local p = self.peripherals["UART2"]
        p.registers["CTRL"] = {
            address = 0x010,
            size = 4,
            value = 0
        }
        p.registers["STAT"] = {
            address = 0x014,
            size = 4,
            value = 0
        }
        p.registers["DATA"] = {
            address = 0x01C,
            size = 4,
            value = 0
        }
        p.registers["BAUD"] = {
            address = 0x024,
            size = 4,
            value = 0
        }
        self.peripherals["GPIO1"] = {
            base = 0x401B8000,
            type = "gpio",
            description = "GPIO 1",
            registers = {}
        }
        
        local p = self.peripherals["GPIO1"]
        p.registers["DR"] = {
            address = 0x000,
            size = 4,
            value = 0
        }
        p.registers["GDIR"] = {
            address = 0x004,
            size = 4,
            value = 0
        }
        p.registers["PSR"] = {
            address = 0x008,
            size = 4,
            value = 0
        }
        p.registers["ICR1"] = {
            address = 0x00C,
            size = 4,
            value = 0
        }
        p.registers["ICR2"] = {
            address = 0x010,
            size = 4,
            value = 0
        }
        p.registers["IMR"] = {
            address = 0x014,
            size = 4,
            value = 0
        }
        p.registers["ISR"] = {
            address = 0x018,
            size = 4,
            value = 0
        }
        p.registers["EDGE_SEL"] = {
            address = 0x01C,
            size = 4,
            value = 0
        }
        self.peripherals["GPT1"] = {
            base = 0x401EC000,
            type = "timer",
            description = "GPT 定时器 1",
            registers = {}
        }
        
        local p = self.peripherals["GPT1"]
        p.registers["CR"] = {
            address = 0x000,
            size = 4,
            value = 0
        }
        p.registers["PR"] = {
            address = 0x004,
            size = 4,
            value = 0
        }
        p.registers["SR"] = {
            address = 0x008,
            size = 4,
            value = 0
        }
        p.registers["IR"] = {
            address = 0x00C,
            size = 4,
            value = 0
        }
        p.registers["OCR1"] = {
            address = 0x010,
            size = 4,
            value = 0
        }
        p.registers["CNT"] = {
            address = 0x024,
            size = 4,
            value = 0
        }
        self.peripherals["USB1"] = {
            base = 0x402E0000,
            type = "usb",
            description = "USB OTG 1",
            registers = {}
        }
        
        local p = self.peripherals["USB1"]
        p.registers["ID"] = {
            address = 0x000,
            size = 4,
            value = 0
        }
        p.registers["OTGSC"] = {
            address = 0x00C,
            size = 4,
            value = 0
        }
        p.registers["USBCMD"] = {
            address = 0x100,
            size = 4,
            value = 0
        }
        p.registers["PORTSC1"] = {
            address = 0x184,
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
            name = i.MX RT1062.DEVICE_NAME,
            manufacturer = i.MX RT1062.MANUFACTURER,
            family = i.MX RT1062.FAMILY,
            version = i.MX RT1062.VERSION,
            architecture = i.MX RT1062.ARCHITECTURE,
            bits = i.MX RT1062.BITS,
            clock_frequency = i.MX RT1062.CLOCK_FREQUENCY
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
        return string.format("i.MX RT1062(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function i.MX RT1062.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function i.MX RT1062.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function i.MX RT1062.print_device_info(device)
    device = device or i.MX RT1062.new()
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

function i.MX RT1062.print_registers(device)
    device = device or i.MX RT1062.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            i.MX RT1062.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function i.MX RT1062.example()
    print("=== i.MX RT1062设备示例 ===")
    
    -- 创建设备实例
    local device = i.MX RT1062.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    i.MX RT1062.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    i.MX RT1062.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("i.MX RT1062.lua$") then
    i.MX RT1062.example()
end

return i.MX RT1062
