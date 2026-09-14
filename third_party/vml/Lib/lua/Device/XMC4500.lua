--[[
  XMC4500设备定义 - Lua模块
  生成自: Infineon/XMC4000/XMC4500
  版本: 1.0
  日期: 2026-04-28
  作者: VML Team
  描述: 32-bit ARM Cortex-M4 Industrial MCU with 1MB Flash, 160KB RAM, 120MHz
  CPU架构: ARM-Cortex-M4
  位宽: 32位
  时钟频率: 120000000 Hz
]]

local XMC4500 = {}

-- 设备信息
XMC4500.DEVICE_NAME = "XMC4500"
XMC4500.MANUFACTURER = "Infineon"
XMC4500.FAMILY = "XMC4000"
XMC4500.VERSION = "1.0"
XMC4500.ARCHITECTURE = "ARM-Cortex-M4"
XMC4500.BITS = 32
XMC4500.CLOCK_FREQUENCY = 120000000

-- 寄存器地址定义
XMC4500.R0_ADDR = 0x00  -- 
XMC4500.R1_ADDR = 0x04  -- 
XMC4500.R2_ADDR = 0x08  -- 
XMC4500.R3_ADDR = 0x0C  -- 
XMC4500.R4_ADDR = 0x10  -- 
XMC4500.R5_ADDR = 0x14  -- 
XMC4500.SP_ADDR = 0x34  -- 
XMC4500.LR_ADDR = 0x38  -- 
XMC4500.PC_ADDR = 0x3C  -- 

-- 内存段定义
XMC4500.FLASH_START = 0x08000000
XMC4500.FLASH_END = 0x080FFFFF
XMC4500.FLASH_SIZE = 1048576  -- 
XMC4500.SRAM_START = 0x1FF00000
XMC4500.SRAM_END = 0x1FF0FFFF
XMC4500.SRAM_SIZE = 65536  -- 
XMC4500.SRAM_COM_START = 0x20000000
XMC4500.SRAM_COM_END = 0x20007FFF
XMC4500.SRAM_COM_SIZE = 32768  -- Communication Memory
XMC4500.SRAM_CPU_START = 0x20010000
XMC4500.SRAM_CPU_END = 0x2001FFFF
XMC4500.SRAM_CPU_SIZE = 65536  -- CPU SRAM
XMC4500.PERIPHERAL_START = 0x40000000
XMC4500.PERIPHERAL_END = 0x4FFFFFFF
XMC4500.PERIPHERAL_SIZE = 268435456  -- 

-- 外设定义
-- System Control Unit
XMC4500.SCU_BASE = 0x40020000
XMC4500.SCU_CLKCR_ADDR = 0x00
XMC4500.SCU_CLKCR_PCLK_SEL_BIT = 0  -- CPU clock selection
XMC4500.SCU_CLKCR_FBKDIV_BIT = 16  -- Feedback divider
XMC4500.SCU_PLLCONFIG_ADDR = 0x04
XMC4500.SCU_OSCHPCTRL_ADDR = 0x08
XMC4500.SCU_CGATSET0_ADDR = 0x20
XMC4500.SCU_CGATSET0_CG_GATE_GPIO_BIT = 4  -- GPIO gate enable
XMC4500.SCU_CGATCLR0_ADDR = 0x24
-- Port 0
XMC4500.PORT0_BASE = 0x48000000
XMC4500.PORT0_OUT_ADDR = 0x00
XMC4500.PORT0_OMR_ADDR = 0x04
XMC4500.PORT0_IOCR0_ADDR = 0x10
XMC4500.PORT0_IOCR4_ADDR = 0x14
XMC4500.PORT0_IOCR8_ADDR = 0x18
XMC4500.PORT0_IOCR12_ADDR = 0x1C
XMC4500.PORT0_IN_ADDR = 0x24
-- Port 1
XMC4500.PORT1_BASE = 0x48010000
XMC4500.PORT1_OUT_ADDR = 0x00
XMC4500.PORT1_OMR_ADDR = 0x04
XMC4500.PORT1_IOCR0_ADDR = 0x10
XMC4500.PORT1_IOCR4_ADDR = 0x14
XMC4500.PORT1_IOCR8_ADDR = 0x18
XMC4500.PORT1_IOCR12_ADDR = 0x1C
XMC4500.PORT1_IN_ADDR = 0x24
-- Port 2
XMC4500.PORT2_BASE = 0x48020000
XMC4500.PORT2_OUT_ADDR = 0x00
XMC4500.PORT2_OMR_ADDR = 0x04
XMC4500.PORT2_IOCR0_ADDR = 0x10
XMC4500.PORT2_IOCR4_ADDR = 0x14
XMC4500.PORT2_IN_ADDR = 0x24
-- Universal Serial Interface 0 (UART)
XMC4500.USIC0_BASE = 0x48030000
XMC4500.USIC0_CCR_ADDR = 0x00
XMC4500.USIC0_PCR_ADDR = 0x04
XMC4500.USIC0_RBUF_ADDR = 0x08
XMC4500.USIC0_TBUF_ADDR = 0x0C
XMC4500.USIC0_BRG_ADDR = 0x10

-- 中断向量定义
XMC4500.INT_RESET = 0  -- 
XMC4500.INT_SVCALL = 11  -- 
XMC4500.INT_USIC0_SR0 = 12  -- USIC0 Service Request 0

-- 设备类
function XMC4500.new(memory_base)
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
        self.peripherals["SCU"] = {
            base = 0x40020000,
            type = "ClockControl",
            description = "System Control Unit",
            registers = {}
        }
        
        local p = self.peripherals["SCU"]
        p.registers["CLKCR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["PLLCONFIG"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["OSCHPCTRL"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["CGATSET0"] = {
            address = 0x20,
            size = 4,
            value = 0
        }
        p.registers["CGATCLR0"] = {
            address = 0x24,
            size = 4,
            value = 0
        }
        self.peripherals["PORT0"] = {
            base = 0x48000000,
            type = "GPIO",
            description = "Port 0",
            registers = {}
        }
        
        local p = self.peripherals["PORT0"]
        p.registers["OUT"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["OMR"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["IOCR0"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["IOCR4"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["IOCR8"] = {
            address = 0x18,
            size = 4,
            value = 0
        }
        p.registers["IOCR12"] = {
            address = 0x1C,
            size = 4,
            value = 0
        }
        p.registers["IN"] = {
            address = 0x24,
            size = 4,
            value = 0
        }
        self.peripherals["PORT1"] = {
            base = 0x48010000,
            type = "GPIO",
            description = "Port 1",
            registers = {}
        }
        
        local p = self.peripherals["PORT1"]
        p.registers["OUT"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["OMR"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["IOCR0"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["IOCR4"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["IOCR8"] = {
            address = 0x18,
            size = 4,
            value = 0
        }
        p.registers["IOCR12"] = {
            address = 0x1C,
            size = 4,
            value = 0
        }
        p.registers["IN"] = {
            address = 0x24,
            size = 4,
            value = 0
        }
        self.peripherals["PORT2"] = {
            base = 0x48020000,
            type = "GPIO",
            description = "Port 2",
            registers = {}
        }
        
        local p = self.peripherals["PORT2"]
        p.registers["OUT"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["OMR"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["IOCR0"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["IOCR4"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["IN"] = {
            address = 0x24,
            size = 4,
            value = 0
        }
        self.peripherals["USIC0"] = {
            base = 0x48030000,
            type = "UART",
            description = "Universal Serial Interface 0 (UART)",
            registers = {}
        }
        
        local p = self.peripherals["USIC0"]
        p.registers["CCR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["PCR"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["RBUF"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["TBUF"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["BRG"] = {
            address = 0x10,
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
            name = XMC4500.DEVICE_NAME,
            manufacturer = XMC4500.MANUFACTURER,
            family = XMC4500.FAMILY,
            version = XMC4500.VERSION,
            architecture = XMC4500.ARCHITECTURE,
            bits = XMC4500.BITS,
            clock_frequency = XMC4500.CLOCK_FREQUENCY
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
        return string.format("XMC4500(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function XMC4500.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function XMC4500.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function XMC4500.print_device_info(device)
    device = device or XMC4500.new()
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

function XMC4500.print_registers(device)
    device = device or XMC4500.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            XMC4500.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function XMC4500.example()
    print("=== XMC4500设备示例 ===")
    
    -- 创建设备实例
    local device = XMC4500.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    XMC4500.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["R0"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("R0", 0x55)
        print("写入 R0: " .. XMC4500.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("R0")
        print("读取 R0: " .. XMC4500.hex(value))
        
        -- 位操作
        device:set_bit("R0", 0, true)
        local bit0 = device:get_bit("R0", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    XMC4500.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("XMC4500.lua$") then
    XMC4500.example()
end

return XMC4500
