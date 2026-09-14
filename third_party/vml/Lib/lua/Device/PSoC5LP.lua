--[[
  CY8C5888LTI-LP097设备定义 - Lua模块
  生成自: Cypress (Infineon)/PSoC/CY8C5888LTI-LP097
  版本: 1.0
  日期: 2026-04-29
  作者: VML Team
  描述: 32-bit ARM Cortex-M3 PSoC 5LP with 256KB Flash, 64KB SRAM, 80MHz, UDB
  CPU架构: ARM-Cortex-M3
  位宽: 32位
  时钟频率: 80000000 Hz
]]

local CY8C5888LTI_LP097 = {}

-- 设备信息
CY8C5888LTI_LP097.DEVICE_NAME = "CY8C5888LTI-LP097"
CY8C5888LTI_LP097.MANUFACTURER = "Cypress (Infineon)"
CY8C5888LTI_LP097.FAMILY = "PSoC"
CY8C5888LTI_LP097.VERSION = "1.0"
CY8C5888LTI_LP097.ARCHITECTURE = "ARM-Cortex-M3"
CY8C5888LTI_LP097.BITS = 32
CY8C5888LTI_LP097.CLOCK_FREQUENCY = 80000000

-- 外设定义
-- SCB UART (可编程)
CY8C5888LTI_LP097.UART_BASE = 0x40050000
CY8C5888LTI_LP097.UART_CTRL_ADDR = 0x00
CY8C5888LTI_LP097.UART_STATUS_ADDR = 0x04
CY8C5888LTI_LP097.UART_TX_DATA_ADDR = 0x08
CY8C5888LTI_LP097.UART_RX_DATA_ADDR = 0x0C
-- SCB I2C
CY8C5888LTI_LP097.I2C_BASE = 0x40051000
CY8C5888LTI_LP097.I2C_CTRL_ADDR = 0x00
CY8C5888LTI_LP097.I2C_STATUS_ADDR = 0x04
CY8C5888LTI_LP097.I2C_TX_DATA_ADDR = 0x08
CY8C5888LTI_LP097.I2C_RX_DATA_ADDR = 0x0C
-- TCPWM 定时器
CY8C5888LTI_LP097.TIMER_BASE = 0x40060000
CY8C5888LTI_LP097.TIMER_CTRL_ADDR = 0x00
CY8C5888LTI_LP097.TIMER_STATUS_ADDR = 0x04
CY8C5888LTI_LP097.TIMER_CNT_ADDR = 0x08
CY8C5888LTI_LP097.TIMER_PERIOD_ADDR = 0x0C
CY8C5888LTI_LP097.TIMER_CC_ADDR = 0x10
-- DelSig ADC 20-bit
CY8C5888LTI_LP097.ADC_BASE = 0x40100000
CY8C5888LTI_LP097.ADC_CTRL_ADDR = 0x00
CY8C5888LTI_LP097.ADC_STATUS_ADDR = 0x04
CY8C5888LTI_LP097.ADC_DATA_ADDR = 0x08
CY8C5888LTI_LP097.ADC_CLOCK_ADDR = 0x10
-- GPIO 端口
CY8C5888LTI_LP097.GPIO_BASE = 0x40040000
CY8C5888LTI_LP097.GPIO_DR_ADDR = 0x00
CY8C5888LTI_LP097.GPIO_PS_ADDR = 0x04
CY8C5888LTI_LP097.GPIO_IE_ADDR = 0x08
CY8C5888LTI_LP097.GPIO_DM_ADDR = 0x0C
-- USB 控制器
CY8C5888LTI_LP097.USB_BASE = 0x40080000
CY8C5888LTI_LP097.USB_CR0_ADDR = 0x00
CY8C5888LTI_LP097.USB_CR1_ADDR = 0x04
CY8C5888LTI_LP097.USB_STAT_ADDR = 0x08

-- 设备类
function CY8C5888LTI_LP097.new(memory_base)
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
        self.peripherals["UART"] = {
            base = 0x40050000,
            type = "uart",
            description = "SCB UART (可编程)",
            registers = {}
        }
        
        local p = self.peripherals["UART"]
        p.registers["CTRL"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["STATUS"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["TX_DATA"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["RX_DATA"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        self.peripherals["I2C"] = {
            base = 0x40051000,
            type = "i2c",
            description = "SCB I2C",
            registers = {}
        }
        
        local p = self.peripherals["I2C"]
        p.registers["CTRL"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["STATUS"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["TX_DATA"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["RX_DATA"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        self.peripherals["TIMER"] = {
            base = 0x40060000,
            type = "timer",
            description = "TCPWM 定时器",
            registers = {}
        }
        
        local p = self.peripherals["TIMER"]
        p.registers["CTRL"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["STATUS"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["CNT"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["PERIOD"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["CC"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        self.peripherals["ADC"] = {
            base = 0x40100000,
            type = "adc",
            description = "DelSig ADC 20-bit",
            registers = {}
        }
        
        local p = self.peripherals["ADC"]
        p.registers["CTRL"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["STATUS"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["DATA"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["CLOCK"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        self.peripherals["GPIO"] = {
            base = 0x40040000,
            type = "gpio",
            description = "GPIO 端口",
            registers = {}
        }
        
        local p = self.peripherals["GPIO"]
        p.registers["DR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["PS"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["IE"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["DM"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        self.peripherals["USB"] = {
            base = 0x40080000,
            type = "usb",
            description = "USB 控制器",
            registers = {}
        }
        
        local p = self.peripherals["USB"]
        p.registers["CR0"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CR1"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["STAT"] = {
            address = 0x08,
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
            name = CY8C5888LTI_LP097.DEVICE_NAME,
            manufacturer = CY8C5888LTI_LP097.MANUFACTURER,
            family = CY8C5888LTI_LP097.FAMILY,
            version = CY8C5888LTI_LP097.VERSION,
            architecture = CY8C5888LTI_LP097.ARCHITECTURE,
            bits = CY8C5888LTI_LP097.BITS,
            clock_frequency = CY8C5888LTI_LP097.CLOCK_FREQUENCY
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
        return string.format("CY8C5888LTI_LP097(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function CY8C5888LTI_LP097.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function CY8C5888LTI_LP097.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function CY8C5888LTI_LP097.print_device_info(device)
    device = device or CY8C5888LTI_LP097.new()
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

function CY8C5888LTI_LP097.print_registers(device)
    device = device or CY8C5888LTI_LP097.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            CY8C5888LTI_LP097.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function CY8C5888LTI_LP097.example()
    print("=== CY8C5888LTI-LP097设备示例 ===")
    
    -- 创建设备实例
    local device = CY8C5888LTI_LP097.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    CY8C5888LTI_LP097.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    CY8C5888LTI_LP097.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("CY8C5888LTI_LP097.lua$") then
    CY8C5888LTI_LP097.example()
end

return CY8C5888LTI_LP097
