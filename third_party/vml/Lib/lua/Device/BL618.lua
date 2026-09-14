--[[
  BL618设备定义 - Lua模块
  生成自: Bouffalo Lab/BL6/BL618
  版本: 1.0
  日期: 2026-04-28
  作者: VML Team
  描述: 32-bit RISC-V RV32IMAFC WiFi6 + BLE SoC with 4MB Flash, 512KB SRAM, 480MHz
  CPU架构: RISC-V
  位宽: 32位
  时钟频率: 320000000 Hz
]]

local BL618 = {}

-- 设备信息
BL618.DEVICE_NAME = "BL618"
BL618.MANUFACTURER = "Bouffalo Lab"
BL618.FAMILY = "BL6"
BL618.VERSION = "1.0"
BL618.ARCHITECTURE = "RISC-V"
BL618.BITS = 32
BL618.CLOCK_FREQUENCY = 320000000

-- 寄存器地址定义
BL618.X1_ADDR = 0x04  -- Return Address
BL618.X2_ADDR = 0x08  -- Stack Pointer (SP)
BL618.X3_ADDR = 0x0C  -- Global Pointer (GP)
BL618.X8_ADDR = 0x20  -- Frame Pointer (FP)
BL618.X10_ADDR = 0x28  -- Function Argument (A0)
BL618.X11_ADDR = 0x2C  -- Function Argument (A1)
BL618.PC_ADDR = 0x3C  -- Program Counter

-- 内存段定义
BL618.FLASH_START = 0x20000000
BL618.FLASH_END = 0x203FFFFF
BL618.FLASH_SIZE = 4194304  -- 
BL618.SRAM_HPSYS_START = 0x22000000
BL618.SRAM_HPSYS_END = 0x22003FFF
BL618.SRAM_HPSYS_SIZE = 16384  -- 
BL618.SRAM_DTCM_START = 0x22010000
BL618.SRAM_DTCM_END = 0x22017FFF
BL618.SRAM_DTCM_SIZE = 32768  -- DTCM
BL618.SRAM_SYS_START = 0x22020000
BL618.SRAM_SYS_END = 0x2208FFFF
BL618.SRAM_SYS_SIZE = 458752  -- 
BL618.PERIPHERAL_START = 0x30000000
BL618.PERIPHERAL_END = 0x300FFFFF
BL618.PERIPHERAL_SIZE = 1048576  -- 

-- 外设定义
-- Global Control (Clock and Reset)
BL618.GLB_BASE = 0x30000000
BL618.GLB_GLB_CLK_EN_ADDR = 0x10
BL618.GLB_GLB_CLK_EN_GPIO_CLK_EN_BIT = 6  -- GPIO clock enable
BL618.GLB_GLB_CLK_EN_UART0_CLK_EN_BIT = 12  -- UART0 clock enable
BL618.GLB_GLB_SYS_CLK_CTRL_ADDR = 0x14
BL618.GLB_GLB_PLL_CTRL_ADDR = 0x1C
-- GPIO Port A
BL618.GPIO_P0_BASE = 0x30007000
BL618.GPIO_P0_GPIO_CFG0_ADDR = 0x00
BL618.GPIO_P0_GPIO_CFG1_ADDR = 0x04
BL618.GPIO_P0_GPIO_OE_ADDR = 0x08
BL618.GPIO_P0_GPIO_OUT_ADDR = 0x0C
BL618.GPIO_P0_GPIO_IN_ADDR = 0x10
BL618.GPIO_P0_GPIO_SET_ADDR = 0x14
BL618.GPIO_P0_GPIO_CLR_ADDR = 0x18
BL618.GPIO_P0_GPIO_TOG_ADDR = 0x1C
-- GPIO Port B
BL618.GPIO_P1_BASE = 0x30007200
BL618.GPIO_P1_GPIO_CFG0_ADDR = 0x00
BL618.GPIO_P1_GPIO_CFG1_ADDR = 0x04
BL618.GPIO_P1_GPIO_OE_ADDR = 0x08
BL618.GPIO_P1_GPIO_OUT_ADDR = 0x0C
BL618.GPIO_P1_GPIO_IN_ADDR = 0x10
BL618.GPIO_P1_GPIO_SET_ADDR = 0x14
BL618.GPIO_P1_GPIO_CLR_ADDR = 0x18
BL618.GPIO_P1_GPIO_TOG_ADDR = 0x1C
-- UART 0
BL618.UART0_BASE = 0x30002000
BL618.UART0_UART_CR_ADDR = 0x00
BL618.UART0_UART_BRR_ADDR = 0x04
BL618.UART0_UART_TDR_ADDR = 0x08
BL618.UART0_UART_RDR_ADDR = 0x0C
BL618.UART0_UART_SR_ADDR = 0x10

-- 中断向量定义
BL618.INT_RESET = 1  -- 
BL618.INT_MACHINESOFTWARE = 3  -- 
BL618.INT_MACHINETIMER = 7  -- 
BL618.INT_MACHINEEXTERNAL = 11  -- 
BL618.INT_UART0 = 20  -- UART0 Interrupt

-- 设备类
function BL618.new(memory_base)
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
        self.peripherals["GLB"] = {
            base = 0x30000000,
            type = "ClockControl",
            description = "Global Control (Clock and Reset)",
            registers = {}
        }
        
        local p = self.peripherals["GLB"]
        p.registers["GLB_CLK_EN"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["GLB_SYS_CLK_CTRL"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["GLB_PLL_CTRL"] = {
            address = 0x1C,
            size = 4,
            value = 0
        }
        self.peripherals["GPIO_P0"] = {
            base = 0x30007000,
            type = "GPIO",
            description = "GPIO Port A",
            registers = {}
        }
        
        local p = self.peripherals["GPIO_P0"]
        p.registers["GPIO_CFG0"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["GPIO_CFG1"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["GPIO_OE"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["GPIO_OUT"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["GPIO_IN"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["GPIO_SET"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["GPIO_CLR"] = {
            address = 0x18,
            size = 4,
            value = 0
        }
        p.registers["GPIO_TOG"] = {
            address = 0x1C,
            size = 4,
            value = 0
        }
        self.peripherals["GPIO_P1"] = {
            base = 0x30007200,
            type = "GPIO",
            description = "GPIO Port B",
            registers = {}
        }
        
        local p = self.peripherals["GPIO_P1"]
        p.registers["GPIO_CFG0"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["GPIO_CFG1"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["GPIO_OE"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["GPIO_OUT"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["GPIO_IN"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["GPIO_SET"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["GPIO_CLR"] = {
            address = 0x18,
            size = 4,
            value = 0
        }
        p.registers["GPIO_TOG"] = {
            address = 0x1C,
            size = 4,
            value = 0
        }
        self.peripherals["UART0"] = {
            base = 0x30002000,
            type = "UART",
            description = "UART 0",
            registers = {}
        }
        
        local p = self.peripherals["UART0"]
        p.registers["UART_CR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["UART_BRR"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["UART_TDR"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["UART_RDR"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["UART_SR"] = {
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
            name = BL618.DEVICE_NAME,
            manufacturer = BL618.MANUFACTURER,
            family = BL618.FAMILY,
            version = BL618.VERSION,
            architecture = BL618.ARCHITECTURE,
            bits = BL618.BITS,
            clock_frequency = BL618.CLOCK_FREQUENCY
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
        return string.format("BL618(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function BL618.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function BL618.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function BL618.print_device_info(device)
    device = device or BL618.new()
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

function BL618.print_registers(device)
    device = device or BL618.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            BL618.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function BL618.example()
    print("=== BL618设备示例 ===")
    
    -- 创建设备实例
    local device = BL618.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    BL618.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["x1"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("x1", 0x55)
        print("写入 x1: " .. BL618.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("x1")
        print("读取 x1: " .. BL618.hex(value))
        
        -- 位操作
        device:set_bit("x1", 0, true)
        local bit0 = device:get_bit("x1", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    BL618.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("BL618.lua$") then
    BL618.example()
end

return BL618
