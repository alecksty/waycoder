--[[
  Allwinner H3设备定义 - Lua模块
  生成自: Allwinner/H-Series/Allwinner H3
  版本: 1.0
  日期: 2026-04-29
  作者: VML Team
  描述: 32-bit ARM Cortex-A7 Quad-core SoC with 512KB L2 Cache, 1.6GHz, Mali-400 GPU
  CPU架构: ARM-Cortex-A7
  位宽: 32位
  时钟频率: 1200000000 Hz
]]

local Allwinner H3 = {}

-- 设备信息
Allwinner H3.DEVICE_NAME = "Allwinner H3"
Allwinner H3.MANUFACTURER = "Allwinner"
Allwinner H3.FAMILY = "H-Series"
Allwinner H3.VERSION = "1.0"
Allwinner H3.ARCHITECTURE = "ARM-Cortex-A7"
Allwinner H3.BITS = 32
Allwinner H3.CLOCK_FREQUENCY = 1200000000

-- 外设定义
-- UART 0 (debug console)
Allwinner H3.UART0_BASE = 0x01C28000
Allwinner H3.UART0_RBR_ADDR = 0x00
Allwinner H3.UART0_THR_ADDR = 0x00
Allwinner H3.UART0_IER_ADDR = 0x04
Allwinner H3.UART0_IIR_ADDR = 0x08
Allwinner H3.UART0_FCR_ADDR = 0x08
Allwinner H3.UART0_LCR_ADDR = 0x0C
Allwinner H3.UART0_MCR_ADDR = 0x10
Allwinner H3.UART0_LSR_ADDR = 0x14
Allwinner H3.UART0_MSR_ADDR = 0x18
Allwinner H3.UART0_DLL_ADDR = 0x00
Allwinner H3.UART0_DLH_ADDR = 0x04
-- UART 1
Allwinner H3.UART1_BASE = 0x01C28400
Allwinner H3.UART1_RBR_ADDR = 0x00
Allwinner H3.UART1_THR_ADDR = 0x00
Allwinner H3.UART1_LSR_ADDR = 0x14
-- GPIO 控制器
Allwinner H3.GPIO_BASE = 0x01C20800
Allwinner H3.GPIO_PA_CFG0_ADDR = 0x00
Allwinner H3.GPIO_PA_CFG1_ADDR = 0x04
Allwinner H3.GPIO_PA_DAT_ADDR = 0x10
Allwinner H3.GPIO_PA_DRV0_ADDR = 0x14
Allwinner H3.GPIO_PA_PUL0_ADDR = 0x1C
Allwinner H3.GPIO_PB_CFG0_ADDR = 0x24
Allwinner H3.GPIO_PB_DAT_ADDR = 0x34
Allwinner H3.GPIO_PC_CFG0_ADDR = 0x48
Allwinner H3.GPIO_PC_DAT_ADDR = 0x58
-- AVS 定时器
Allwinner H3.TIMER_BASE = 0x01C20C00
Allwinner H3.TIMER_CNT0_ADDR = 0x00
Allwinner H3.TIMER_CNT1_ADDR = 0x04
Allwinner H3.TIMER_CTRL_ADDR = 0x08
Allwinner H3.TIMER_INTV_ADDR = 0x0C
-- 时钟控制单元
Allwinner H3.CCU_BASE = 0x01C20000
Allwinner H3.CCU_PLL1_CFG_ADDR = 0x000
Allwinner H3.CCU_PLL3_CFG_ADDR = 0x010
Allwinner H3.CCU_CPU_AXI_CFG_ADDR = 0x050
Allwinner H3.CCU_AHB1_APB1_CFG_ADDR = 0x054
Allwinner H3.CCU_APB2_CFG_ADDR = 0x058
Allwinner H3.CCU_BUS_GATE0_ADDR = 0x060
Allwinner H3.CCU_BUS_GATE1_ADDR = 0x064
Allwinner H3.CCU_BUS_GATE2_ADDR = 0x068

-- 设备类
function Allwinner H3.new(memory_base)
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
        self.peripherals["UART0"] = {
            base = 0x01C28000,
            type = "uart",
            description = "UART 0 (debug console)",
            registers = {}
        }
        
        local p = self.peripherals["UART0"]
        p.registers["RBR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["THR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["IER"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["IIR"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["FCR"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["LCR"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["MCR"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["LSR"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["MSR"] = {
            address = 0x18,
            size = 4,
            value = 0
        }
        p.registers["DLL"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["DLH"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        self.peripherals["UART1"] = {
            base = 0x01C28400,
            type = "uart",
            description = "UART 1",
            registers = {}
        }
        
        local p = self.peripherals["UART1"]
        p.registers["RBR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["THR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["LSR"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        self.peripherals["GPIO"] = {
            base = 0x01C20800,
            type = "gpio",
            description = "GPIO 控制器",
            registers = {}
        }
        
        local p = self.peripherals["GPIO"]
        p.registers["PA_CFG0"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["PA_CFG1"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["PA_DAT"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["PA_DRV0"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["PA_PUL0"] = {
            address = 0x1C,
            size = 4,
            value = 0
        }
        p.registers["PB_CFG0"] = {
            address = 0x24,
            size = 4,
            value = 0
        }
        p.registers["PB_DAT"] = {
            address = 0x34,
            size = 4,
            value = 0
        }
        p.registers["PC_CFG0"] = {
            address = 0x48,
            size = 4,
            value = 0
        }
        p.registers["PC_DAT"] = {
            address = 0x58,
            size = 4,
            value = 0
        }
        self.peripherals["TIMER"] = {
            base = 0x01C20C00,
            type = "timer",
            description = "AVS 定时器",
            registers = {}
        }
        
        local p = self.peripherals["TIMER"]
        p.registers["CNT0"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CNT1"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["CTRL"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["INTV"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        self.peripherals["CCU"] = {
            base = 0x01C20000,
            type = "clock",
            description = "时钟控制单元",
            registers = {}
        }
        
        local p = self.peripherals["CCU"]
        p.registers["PLL1_CFG"] = {
            address = 0x000,
            size = 4,
            value = 0
        }
        p.registers["PLL3_CFG"] = {
            address = 0x010,
            size = 4,
            value = 0
        }
        p.registers["CPU_AXI_CFG"] = {
            address = 0x050,
            size = 4,
            value = 0
        }
        p.registers["AHB1_APB1_CFG"] = {
            address = 0x054,
            size = 4,
            value = 0
        }
        p.registers["APB2_CFG"] = {
            address = 0x058,
            size = 4,
            value = 0
        }
        p.registers["BUS_GATE0"] = {
            address = 0x060,
            size = 4,
            value = 0
        }
        p.registers["BUS_GATE1"] = {
            address = 0x064,
            size = 4,
            value = 0
        }
        p.registers["BUS_GATE2"] = {
            address = 0x068,
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
            name = Allwinner H3.DEVICE_NAME,
            manufacturer = Allwinner H3.MANUFACTURER,
            family = Allwinner H3.FAMILY,
            version = Allwinner H3.VERSION,
            architecture = Allwinner H3.ARCHITECTURE,
            bits = Allwinner H3.BITS,
            clock_frequency = Allwinner H3.CLOCK_FREQUENCY
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
        return string.format("Allwinner H3(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function Allwinner H3.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function Allwinner H3.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function Allwinner H3.print_device_info(device)
    device = device or Allwinner H3.new()
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

function Allwinner H3.print_registers(device)
    device = device or Allwinner H3.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            Allwinner H3.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function Allwinner H3.example()
    print("=== Allwinner H3设备示例 ===")
    
    -- 创建设备实例
    local device = Allwinner H3.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    Allwinner H3.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    Allwinner H3.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("Allwinner H3.lua$") then
    Allwinner H3.example()
end

return Allwinner H3
