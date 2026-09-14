--[[
  EFR32MG24设备定义 - Lua模块
  生成自: Silicon Labs/EFR32/EFR32MG24
  版本: 1.0
  日期: 2026-04-28
  作者: VML Team
  描述: 32-bit ARM Cortex-M33 MCU with 1536KB Flash, 256KB RAM, 78MHz, Zigbee/Thread/Matter
  CPU架构: ARM-Cortex-M33
  位宽: 32位
  时钟频率: 78000000 Hz
]]

local EFR32MG24 = {}

-- 设备信息
EFR32MG24.DEVICE_NAME = "EFR32MG24"
EFR32MG24.MANUFACTURER = "Silicon Labs"
EFR32MG24.FAMILY = "EFR32"
EFR32MG24.VERSION = "1.0"
EFR32MG24.ARCHITECTURE = "ARM-Cortex-M33"
EFR32MG24.BITS = 32
EFR32MG24.CLOCK_FREQUENCY = 78000000

-- 寄存器地址定义
EFR32MG24.R0_ADDR = 0x00  -- 
EFR32MG24.R1_ADDR = 0x04  -- 
EFR32MG24.R2_ADDR = 0x08  -- 
EFR32MG24.R3_ADDR = 0x0C  -- 
EFR32MG24.R4_ADDR = 0x10  -- 
EFR32MG24.R5_ADDR = 0x14  -- 
EFR32MG24.SP_ADDR = 0x34  -- 
EFR32MG24.LR_ADDR = 0x38  -- 
EFR32MG24.PC_ADDR = 0x3C  -- 

-- 内存段定义
EFR32MG24.FLASH_START = 0x08000000
EFR32MG24.FLASH_END = 0x0817FFFF
EFR32MG24.FLASH_SIZE = 1572864  -- 
EFR32MG24.SRAM_START = 0x20000000
EFR32MG24.SRAM_END = 0x2003FFFF
EFR32MG24.SRAM_SIZE = 262144  -- 
EFR32MG24.PERIPHERAL_START = 0x40000000
EFR32MG24.PERIPHERAL_END = 0x4007FFFF
EFR32MG24.PERIPHERAL_SIZE = 524288  -- 

-- 外设定义
-- Clock Management Unit
EFR32MG24.CMU_BASE = 0x40080000
EFR32MG24.CMU_CTRL_ADDR = 0x00
EFR32MG24.CMU_HFCORECLKCFG_ADDR = 0x08
EFR32MG24.CMU_HFPERCLKEN0_ADDR = 0x10
EFR32MG24.CMU_HFPERCLKEN0_GPIOEN_BIT = 4  -- GPIO clock enable
EFR32MG24.CMU_HFPERCLKEN0_USART0EN_BIT = 12  -- USART0 clock enable
EFR32MG24.CMU_HFPERCLKEN0_USART1EN_BIT = 13  -- USART1 clock enable
EFR32MG24.CMU_LFBCLKEN0_ADDR = 0x20
-- GPIO Controller
EFR32MG24.GPIO_BASE = 0x40088000
EFR32MG24.GPIO_PORT_A_CTRL_ADDR = 0x00
EFR32MG24.GPIO_PORT_B_CTRL_ADDR = 0x04
EFR32MG24.GPIO_PORT_C_CTRL_ADDR = 0x08
EFR32MG24.GPIO_PORT_D_CTRL_ADDR = 0x0C
EFR32MG24.GPIO_MODEL_ADDR = 0x10
EFR32MG24.GPIO_MODEH_ADDR = 0x14
EFR32MG24.GPIO_DOUT_ADDR = 0x1C
EFR32MG24.GPIO_DOUTSET_ADDR = 0x20
EFR32MG24.GPIO_DOUTCLR_ADDR = 0x24
EFR32MG24.GPIO_DOUTTGL_ADDR = 0x28
EFR32MG24.GPIO_DIN_ADDR = 0x2C
-- GPIO Port A extended
EFR32MG24.GPIO_PA_BASE = 0x40088400
EFR32MG24.GPIO_PA_PA_CFG_ADDR = 0x00
EFR32MG24.GPIO_PA_PA_PINOUT_ADDR = 0x04
-- GPIO Port B extended
EFR32MG24.GPIO_PB_BASE = 0x40088800
EFR32MG24.GPIO_PB_PB_CFG_ADDR = 0x00
-- USART 0
EFR32MG24.USART0_BASE = 0x40060000
EFR32MG24.USART0_CTRL_ADDR = 0x00
EFR32MG24.USART0_CMD_ADDR = 0x04
EFR32MG24.USART0_STATUS_ADDR = 0x08
EFR32MG24.USART0_RXDATA_ADDR = 0x0C
EFR32MG24.USART0_TXDATA_ADDR = 0x10
EFR32MG24.USART0_CLKDIV_ADDR = 0x14

-- 中断向量定义
EFR32MG24.INT_RESET = 0  -- 
EFR32MG24.INT_SVCALL = 11  -- 
EFR32MG24.INT_USART0_RX = 12  -- USART0 Receive Interrupt
EFR32MG24.INT_USART0_TX = 13  -- USART0 Transmit Interrupt

-- 设备类
function EFR32MG24.new(memory_base)
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
        self.peripherals["CMU"] = {
            base = 0x40080000,
            type = "ClockControl",
            description = "Clock Management Unit",
            registers = {}
        }
        
        local p = self.peripherals["CMU"]
        p.registers["CTRL"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["HFCORECLKCFG"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["HFPERCLKEN0"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["LFBCLKEN0"] = {
            address = 0x20,
            size = 4,
            value = 0
        }
        self.peripherals["GPIO"] = {
            base = 0x40088000,
            type = "GPIO",
            description = "GPIO Controller",
            registers = {}
        }
        
        local p = self.peripherals["GPIO"]
        p.registers["PORT_A_CTRL"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["PORT_B_CTRL"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["PORT_C_CTRL"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["PORT_D_CTRL"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["MODEL"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["MODEH"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["DOUT"] = {
            address = 0x1C,
            size = 4,
            value = 0
        }
        p.registers["DOUTSET"] = {
            address = 0x20,
            size = 4,
            value = 0
        }
        p.registers["DOUTCLR"] = {
            address = 0x24,
            size = 4,
            value = 0
        }
        p.registers["DOUTTGL"] = {
            address = 0x28,
            size = 4,
            value = 0
        }
        p.registers["DIN"] = {
            address = 0x2C,
            size = 4,
            value = 0
        }
        self.peripherals["GPIO_PA"] = {
            base = 0x40088400,
            type = "GPIO",
            description = "GPIO Port A extended",
            registers = {}
        }
        
        local p = self.peripherals["GPIO_PA"]
        p.registers["PA_CFG"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["PA_PINOUT"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        self.peripherals["GPIO_PB"] = {
            base = 0x40088800,
            type = "GPIO",
            description = "GPIO Port B extended",
            registers = {}
        }
        
        local p = self.peripherals["GPIO_PB"]
        p.registers["PB_CFG"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        self.peripherals["USART0"] = {
            base = 0x40060000,
            type = "UART",
            description = "USART 0",
            registers = {}
        }
        
        local p = self.peripherals["USART0"]
        p.registers["CTRL"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CMD"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["STATUS"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["RXDATA"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["TXDATA"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["CLKDIV"] = {
            address = 0x14,
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
            name = EFR32MG24.DEVICE_NAME,
            manufacturer = EFR32MG24.MANUFACTURER,
            family = EFR32MG24.FAMILY,
            version = EFR32MG24.VERSION,
            architecture = EFR32MG24.ARCHITECTURE,
            bits = EFR32MG24.BITS,
            clock_frequency = EFR32MG24.CLOCK_FREQUENCY
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
        return string.format("EFR32MG24(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function EFR32MG24.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function EFR32MG24.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function EFR32MG24.print_device_info(device)
    device = device or EFR32MG24.new()
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

function EFR32MG24.print_registers(device)
    device = device or EFR32MG24.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            EFR32MG24.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function EFR32MG24.example()
    print("=== EFR32MG24设备示例 ===")
    
    -- 创建设备实例
    local device = EFR32MG24.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    EFR32MG24.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["R0"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("R0", 0x55)
        print("写入 R0: " .. EFR32MG24.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("R0")
        print("读取 R0: " .. EFR32MG24.hex(value))
        
        -- 位操作
        device:set_bit("R0", 0, true)
        local bit0 = device:get_bit("R0", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    EFR32MG24.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("EFR32MG24.lua$") then
    EFR32MG24.example()
end

return EFR32MG24
