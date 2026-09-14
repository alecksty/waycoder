--[[
  TMS320F280049设备定义 - Lua模块
  生成自: Texas Instruments/C2000/TMS320F280049
  版本: 1.0
  日期: 2026-04-28
  作者: VML Team
  描述: 32-bit C28x DSP + CLA MCU with 256KB Flash, 100KB RAM, 100MHz
  CPU架构: C28x-DSP
  位宽: 32位
  时钟频率: 100000000 Hz
]]

local TMS320F280049 = {}

-- 设备信息
TMS320F280049.DEVICE_NAME = "TMS320F280049"
TMS320F280049.MANUFACTURER = "Texas Instruments"
TMS320F280049.FAMILY = "C2000"
TMS320F280049.VERSION = "1.0"
TMS320F280049.ARCHITECTURE = "C28x-DSP"
TMS320F280049.BITS = 32
TMS320F280049.CLOCK_FREQUENCY = 100000000

-- 寄存器地址定义
TMS320F280049.AL_ADDR = 0x00  -- Accumulator Low
TMS320F280049.AH_ADDR = 0x02  -- Accumulator High
TMS320F280049.PH_ADDR = 0x04  -- Product High
TMS320F280049.PL_ADDR = 0x06  -- Product Low
TMS320F280049.TREG_ADDR = 0x08  -- Temporary Register
TMS320F280049.AR0_ADDR = 0x0A  -- 
TMS320F280049.AR1_ADDR = 0x0C  -- 
TMS320F280049.ST0_ADDR = 0x20  -- Status 0
TMS320F280049.ST1_ADDR = 0x22  -- Status 1
TMS320F280049.PC_ADDR = 0x24  -- Program Counter
TMS320F280049.SP_ADDR = 0x26  -- Stack Pointer

-- 内存段定义
TMS320F280049.FLASH_START = 0x080000
TMS320F280049.FLASH_END = 0x0BFFFF
TMS320F280049.FLASH_SIZE = 262144  -- 
TMS320F280049.SRAM_LS_START = 0x008000
TMS320F280049.SRAM_LS_END = 0x00BFFF
TMS320F280049.SRAM_LS_SIZE = 16384  -- Local Shared RAM
TMS320F280049.SRAM_GS_START = 0x00C000
TMS320F280049.SRAM_GS_END = 0x01FFFF
TMS320F280049.SRAM_GS_SIZE = 81920  -- Global Shared RAM
TMS320F280049.PERIPHERAL_START = 0x400000
TMS320F280049.PERIPHERAL_END = 0x40FFFF
TMS320F280049.PERIPHERAL_SIZE = 65536  -- 

-- 外设定义
-- PLL Clock Control
TMS320F280049.PLL_BASE = 0x5C10
TMS320F280049.PLL_SYSPLLCTL1_ADDR = 0x00
TMS320F280049.PLL_SYSPLLCTL2_ADDR = 0x02
TMS320F280049.PLL_CLKSRCCTL1_ADDR = 0x04
TMS320F280049.PLL_CLKSRCCTL2_ADDR = 0x06
-- GPIO Control Registers
TMS320F280049.GPIO_CTRL_BASE = 0x7C00
TMS320F280049.GPIO_CTRL_GPACTRL_ADDR = 0x00
TMS320F280049.GPIO_CTRL_GPAQSEL1_ADDR = 0x02
TMS320F280049.GPIO_CTRL_GPAQSEL2_ADDR = 0x04
TMS320F280049.GPIO_CTRL_GPAMUX1_ADDR = 0x06
TMS320F280049.GPIO_CTRL_GPAMUX2_ADDR = 0x08
TMS320F280049.GPIO_CTRL_GPADIR_ADDR = 0x0A
TMS320F280049.GPIO_CTRL_GPAPUD_ADDR = 0x0C
-- GPIO Data Registers
TMS320F280049.GPIO_DATA_BASE = 0x7F00
TMS320F280049.GPIO_DATA_GPADAT_ADDR = 0x00
TMS320F280049.GPIO_DATA_GPASET_ADDR = 0x02
TMS320F280049.GPIO_DATA_GPACLEAR_ADDR = 0x04
TMS320F280049.GPIO_DATA_GPATOGGLE_ADDR = 0x06
TMS320F280049.GPIO_DATA_GPBDAT_ADDR = 0x08
TMS320F280049.GPIO_DATA_GPBSET_ADDR = 0x0A
TMS320F280049.GPIO_DATA_GPBCLEAR_ADDR = 0x0C
TMS320F280049.GPIO_DATA_GPBTOGGLE_ADDR = 0x0E
-- GPIO B Control
TMS320F280049.GPIO_B_CTRL_BASE = 0x7C20
TMS320F280049.GPIO_B_CTRL_GPBMUX1_ADDR = 0x00
TMS320F280049.GPIO_B_CTRL_GPBMUX2_ADDR = 0x02
TMS320F280049.GPIO_B_CTRL_GPBDIR_ADDR = 0x04
TMS320F280049.GPIO_B_CTRL_GPBPUD_ADDR = 0x06
-- SCI-A UART
TMS320F280049.SCI_A_BASE = 0x7320
TMS320F280049.SCI_A_SCICCR_ADDR = 0x00
TMS320F280049.SCI_A_SCICTL1_ADDR = 0x02
TMS320F280049.SCI_A_SCIBAUD_ADDR = 0x04
TMS320F280049.SCI_A_SCIRXBUF_ADDR = 0x0A
TMS320F280049.SCI_A_SCITXBUF_ADDR = 0x0C

-- 中断向量定义
TMS320F280049.INT_RESET = 1  -- 
TMS320F280049.INT_SCIA_RX = 8  -- SCI-A Receive Interrupt
TMS320F280049.INT_SCIA_TX = 9  -- SCI-A Transmit Interrupt

-- 设备类
function TMS320F280049.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["AL"] = {
            address = 0x00,
            size = 2,
            access = "rw",
            description = "Accumulator Low",
            value = 0
        }
        self.registers["AH"] = {
            address = 0x02,
            size = 2,
            access = "rw",
            description = "Accumulator High",
            value = 0
        }
        self.registers["PH"] = {
            address = 0x04,
            size = 2,
            access = "rw",
            description = "Product High",
            value = 0
        }
        self.registers["PL"] = {
            address = 0x06,
            size = 2,
            access = "rw",
            description = "Product Low",
            value = 0
        }
        self.registers["TREG"] = {
            address = 0x08,
            size = 2,
            access = "rw",
            description = "Temporary Register",
            value = 0
        }
        self.registers["AR0"] = {
            address = 0x0A,
            size = 2,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["AR1"] = {
            address = 0x0C,
            size = 2,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["ST0"] = {
            address = 0x20,
            size = 2,
            access = "rw",
            description = "Status 0",
            value = 0
        }
        self.registers["ST1"] = {
            address = 0x22,
            size = 2,
            access = "rw",
            description = "Status 1",
            value = 0
        }
        self.registers["PC"] = {
            address = 0x24,
            size = 2,
            access = "rw",
            description = "Program Counter",
            value = 0
        }
        self.registers["SP"] = {
            address = 0x26,
            size = 2,
            access = "rw",
            description = "Stack Pointer",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["PLL"] = {
            base = 0x5C10,
            type = "ClockControl",
            description = "PLL Clock Control",
            registers = {}
        }
        
        local p = self.peripherals["PLL"]
        p.registers["SYSPLLCTL1"] = {
            address = 0x00,
            size = 2,
            value = 0
        }
        p.registers["SYSPLLCTL2"] = {
            address = 0x02,
            size = 2,
            value = 0
        }
        p.registers["CLKSRCCTL1"] = {
            address = 0x04,
            size = 2,
            value = 0
        }
        p.registers["CLKSRCCTL2"] = {
            address = 0x06,
            size = 2,
            value = 0
        }
        self.peripherals["GPIO_CTRL"] = {
            base = 0x7C00,
            type = "GPIO",
            description = "GPIO Control Registers",
            registers = {}
        }
        
        local p = self.peripherals["GPIO_CTRL"]
        p.registers["GPACTRL"] = {
            address = 0x00,
            size = 2,
            value = 0
        }
        p.registers["GPAQSEL1"] = {
            address = 0x02,
            size = 2,
            value = 0
        }
        p.registers["GPAQSEL2"] = {
            address = 0x04,
            size = 2,
            value = 0
        }
        p.registers["GPAMUX1"] = {
            address = 0x06,
            size = 2,
            value = 0
        }
        p.registers["GPAMUX2"] = {
            address = 0x08,
            size = 2,
            value = 0
        }
        p.registers["GPADIR"] = {
            address = 0x0A,
            size = 2,
            value = 0
        }
        p.registers["GPAPUD"] = {
            address = 0x0C,
            size = 2,
            value = 0
        }
        self.peripherals["GPIO_DATA"] = {
            base = 0x7F00,
            type = "GPIO",
            description = "GPIO Data Registers",
            registers = {}
        }
        
        local p = self.peripherals["GPIO_DATA"]
        p.registers["GPADAT"] = {
            address = 0x00,
            size = 2,
            value = 0
        }
        p.registers["GPASET"] = {
            address = 0x02,
            size = 2,
            value = 0
        }
        p.registers["GPACLEAR"] = {
            address = 0x04,
            size = 2,
            value = 0
        }
        p.registers["GPATOGGLE"] = {
            address = 0x06,
            size = 2,
            value = 0
        }
        p.registers["GPBDAT"] = {
            address = 0x08,
            size = 2,
            value = 0
        }
        p.registers["GPBSET"] = {
            address = 0x0A,
            size = 2,
            value = 0
        }
        p.registers["GPBCLEAR"] = {
            address = 0x0C,
            size = 2,
            value = 0
        }
        p.registers["GPBTOGGLE"] = {
            address = 0x0E,
            size = 2,
            value = 0
        }
        self.peripherals["GPIO_B_CTRL"] = {
            base = 0x7C20,
            type = "GPIO",
            description = "GPIO B Control",
            registers = {}
        }
        
        local p = self.peripherals["GPIO_B_CTRL"]
        p.registers["GPBMUX1"] = {
            address = 0x00,
            size = 2,
            value = 0
        }
        p.registers["GPBMUX2"] = {
            address = 0x02,
            size = 2,
            value = 0
        }
        p.registers["GPBDIR"] = {
            address = 0x04,
            size = 2,
            value = 0
        }
        p.registers["GPBPUD"] = {
            address = 0x06,
            size = 2,
            value = 0
        }
        self.peripherals["SCI_A"] = {
            base = 0x7320,
            type = "UART",
            description = "SCI-A UART",
            registers = {}
        }
        
        local p = self.peripherals["SCI_A"]
        p.registers["SCICCR"] = {
            address = 0x00,
            size = 2,
            value = 0
        }
        p.registers["SCICTL1"] = {
            address = 0x02,
            size = 2,
            value = 0
        }
        p.registers["SCIBAUD"] = {
            address = 0x04,
            size = 2,
            value = 0
        }
        p.registers["SCIRXBUF"] = {
            address = 0x0A,
            size = 2,
            value = 0
        }
        p.registers["SCITXBUF"] = {
            address = 0x0C,
            size = 2,
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
            name = TMS320F280049.DEVICE_NAME,
            manufacturer = TMS320F280049.MANUFACTURER,
            family = TMS320F280049.FAMILY,
            version = TMS320F280049.VERSION,
            architecture = TMS320F280049.ARCHITECTURE,
            bits = TMS320F280049.BITS,
            clock_frequency = TMS320F280049.CLOCK_FREQUENCY
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
        return string.format("TMS320F280049(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function TMS320F280049.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function TMS320F280049.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function TMS320F280049.print_device_info(device)
    device = device or TMS320F280049.new()
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

function TMS320F280049.print_registers(device)
    device = device or TMS320F280049.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            TMS320F280049.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function TMS320F280049.example()
    print("=== TMS320F280049设备示例 ===")
    
    -- 创建设备实例
    local device = TMS320F280049.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    TMS320F280049.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["AL"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("AL", 0x55)
        print("写入 AL: " .. TMS320F280049.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("AL")
        print("读取 AL: " .. TMS320F280049.hex(value))
        
        -- 位操作
        device:set_bit("AL", 0, true)
        local bit0 = device:get_bit("AL", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    TMS320F280049.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("TMS320F280049.lua$") then
    TMS320F280049.example()
end

return TMS320F280049
