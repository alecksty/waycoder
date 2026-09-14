--[[
  Commodore-PET设备定义 - Lua模块
  生成自: Commodore International/PET/Commodore-PET
  版本: 1.0
  日期: 2026-04-17
  作者: VML Team
  描述: Commodore PET 2001 personal computer with MOS 6502 CPU and built-in monitor
  CPU架构: MOS 6502
  位宽: 8位
  时钟频率: 1000000 Hz
]]

local Commodore_PET = {}

-- 设备信息
Commodore_PET.DEVICE_NAME = "Commodore-PET"
Commodore_PET.MANUFACTURER = "Commodore International"
Commodore_PET.FAMILY = "PET"
Commodore_PET.VERSION = "1.0"
Commodore_PET.ARCHITECTURE = "MOS 6502"
Commodore_PET.BITS = 8
Commodore_PET.CLOCK_FREQUENCY = 1000000

-- 寄存器地址定义
Commodore_PET.A_ADDR = 0  -- Accumulator
Commodore_PET.X_ADDR = 0  -- Index Register X
Commodore_PET.Y_ADDR = 0  -- Index Register Y
Commodore_PET.SP_ADDR = 0  -- Stack Pointer
Commodore_PET.PC_ADDR = 0  -- Program Counter
Commodore_PET.P_ADDR = 0  -- Status Register

-- 外设定义
-- Peripheral Interface Adapter 1 (6520)
Commodore_PET.PIA1_BASE = 
Commodore_PET.PIA1_PIA1_DDRA_ADDR = 0xE810
Commodore_PET.PIA1_PIA1_ORA_ADDR = 0xE811
Commodore_PET.PIA1_PIA1_DDRB_ADDR = 0xE812
Commodore_PET.PIA1_PIA1_ORB_ADDR = 0xE813
Commodore_PET.PIA1_PIA1_CRA_ADDR = 0xE814
Commodore_PET.PIA1_PIA1_CRB_ADDR = 0xE815
-- Peripheral Interface Adapter 2 (6520)
Commodore_PET.PIA2_BASE = 
Commodore_PET.PIA2_PIA2_DDRA_ADDR = 0xE820
Commodore_PET.PIA2_PIA2_ORA_ADDR = 0xE821
Commodore_PET.PIA2_PIA2_DDRB_ADDR = 0xE822
Commodore_PET.PIA2_PIA2_ORB_ADDR = 0xE823
Commodore_PET.PIA2_PIA2_CRA_ADDR = 0xE824
Commodore_PET.PIA2_PIA2_CRB_ADDR = 0xE825
-- Versatile Interface Adapter (6522)
Commodore_PET.VIA_BASE = 
Commodore_PET.VIA_VIA_ORB_ADDR = 0xE840
Commodore_PET.VIA_VIA_ORA_ADDR = 0xE841
Commodore_PET.VIA_VIA_DDRB_ADDR = 0xE842
Commodore_PET.VIA_VIA_DDRA_ADDR = 0xE843
Commodore_PET.VIA_VIA_T1CL_ADDR = 0xE844
Commodore_PET.VIA_VIA_T1CH_ADDR = 0xE845
Commodore_PET.VIA_VIA_T1LL_ADDR = 0xE846
Commodore_PET.VIA_VIA_T1LH_ADDR = 0xE847
Commodore_PET.VIA_VIA_T2CL_ADDR = 0xE848
Commodore_PET.VIA_VIA_T2CH_ADDR = 0xE849
Commodore_PET.VIA_VIA_SR_ADDR = 0xE84A
Commodore_PET.VIA_VIA_ACR_ADDR = 0xE84B
Commodore_PET.VIA_VIA_PCR_ADDR = 0xE84C
Commodore_PET.VIA_VIA_IFR_ADDR = 0xE84D
Commodore_PET.VIA_VIA_IER_ADDR = 0xE84E
-- CRT Controller (6545)
Commodore_PET.CRTC_BASE = 
Commodore_PET.CRTC_CRTC_ADDR_ADDR = 0xE880
Commodore_PET.CRTC_CRTC_DATA_ADDR = 0xE881
-- Cassette tape interface
Commodore_PET.CASSETTE_BASE = 
Commodore_PET.CASSETTE_CASS_MOTOR_ADDR = 0xE840
Commodore_PET.CASSETTE_CASS_WRITE_ADDR = 0xE842
Commodore_PET.CASSETTE_CASS_READ_ADDR = 0xE812
-- IEEE-488 bus interface
Commodore_PET.IEEE488_BASE = 
Commodore_PET.IEEE488_IEEE_DATA_ADDR = 0xE801
Commodore_PET.IEEE488_IEEE_STATUS_ADDR = 0xE802
Commodore_PET.IEEE488_IEEE_CONTROL_ADDR = 0xE803

-- 中断向量定义
Commodore_PET.INT_NMI = 65526  -- Non-maskable interrupt
Commodore_PET.INT_RESET = 65528  -- Reset vector
Commodore_PET.INT_IRQ = 65530  -- Interrupt request
Commodore_PET.INT_BRK = 65532  -- Break instruction

-- 设备类
function Commodore_PET.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["A"] = {
            address = 0,
            size = 1,
            access = "rw",
            description = "Accumulator",
            value = 0
        }
        self.registers["X"] = {
            address = 0,
            size = 1,
            access = "rw",
            description = "Index Register X",
            value = 0
        }
        self.registers["Y"] = {
            address = 0,
            size = 1,
            access = "rw",
            description = "Index Register Y",
            value = 0
        }
        self.registers["SP"] = {
            address = 0,
            size = 1,
            access = "rw",
            description = "Stack Pointer",
            value = 0
        }
        self.registers["PC"] = {
            address = 0,
            size = 2,
            access = "rw",
            description = "Program Counter",
            value = 0
        }
        self.registers["P"] = {
            address = 0,
            size = 1,
            access = "rw",
            description = "Status Register",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["PIA1"] = {
            base = ,
            type = "IO",
            description = "Peripheral Interface Adapter 1 (6520)",
            registers = {}
        }
        
        local p = self.peripherals["PIA1"]
        p.registers["PIA1_DDRA"] = {
            address = 0xE810,
            size = 1,
            value = 0
        }
        p.registers["PIA1_ORA"] = {
            address = 0xE811,
            size = 1,
            value = 0
        }
        p.registers["PIA1_DDRB"] = {
            address = 0xE812,
            size = 1,
            value = 0
        }
        p.registers["PIA1_ORB"] = {
            address = 0xE813,
            size = 1,
            value = 0
        }
        p.registers["PIA1_CRA"] = {
            address = 0xE814,
            size = 1,
            value = 0
        }
        p.registers["PIA1_CRB"] = {
            address = 0xE815,
            size = 1,
            value = 0
        }
        self.peripherals["PIA2"] = {
            base = ,
            type = "IO",
            description = "Peripheral Interface Adapter 2 (6520)",
            registers = {}
        }
        
        local p = self.peripherals["PIA2"]
        p.registers["PIA2_DDRA"] = {
            address = 0xE820,
            size = 1,
            value = 0
        }
        p.registers["PIA2_ORA"] = {
            address = 0xE821,
            size = 1,
            value = 0
        }
        p.registers["PIA2_DDRB"] = {
            address = 0xE822,
            size = 1,
            value = 0
        }
        p.registers["PIA2_ORB"] = {
            address = 0xE823,
            size = 1,
            value = 0
        }
        p.registers["PIA2_CRA"] = {
            address = 0xE824,
            size = 1,
            value = 0
        }
        p.registers["PIA2_CRB"] = {
            address = 0xE825,
            size = 1,
            value = 0
        }
        self.peripherals["VIA"] = {
            base = ,
            type = "IO",
            description = "Versatile Interface Adapter (6522)",
            registers = {}
        }
        
        local p = self.peripherals["VIA"]
        p.registers["VIA_ORB"] = {
            address = 0xE840,
            size = 1,
            value = 0
        }
        p.registers["VIA_ORA"] = {
            address = 0xE841,
            size = 1,
            value = 0
        }
        p.registers["VIA_DDRB"] = {
            address = 0xE842,
            size = 1,
            value = 0
        }
        p.registers["VIA_DDRA"] = {
            address = 0xE843,
            size = 1,
            value = 0
        }
        p.registers["VIA_T1CL"] = {
            address = 0xE844,
            size = 1,
            value = 0
        }
        p.registers["VIA_T1CH"] = {
            address = 0xE845,
            size = 1,
            value = 0
        }
        p.registers["VIA_T1LL"] = {
            address = 0xE846,
            size = 1,
            value = 0
        }
        p.registers["VIA_T1LH"] = {
            address = 0xE847,
            size = 1,
            value = 0
        }
        p.registers["VIA_T2CL"] = {
            address = 0xE848,
            size = 1,
            value = 0
        }
        p.registers["VIA_T2CH"] = {
            address = 0xE849,
            size = 1,
            value = 0
        }
        p.registers["VIA_SR"] = {
            address = 0xE84A,
            size = 1,
            value = 0
        }
        p.registers["VIA_ACR"] = {
            address = 0xE84B,
            size = 1,
            value = 0
        }
        p.registers["VIA_PCR"] = {
            address = 0xE84C,
            size = 1,
            value = 0
        }
        p.registers["VIA_IFR"] = {
            address = 0xE84D,
            size = 1,
            value = 0
        }
        p.registers["VIA_IER"] = {
            address = 0xE84E,
            size = 1,
            value = 0
        }
        self.peripherals["CRTC"] = {
            base = ,
            type = "Video",
            description = "CRT Controller (6545)",
            registers = {}
        }
        
        local p = self.peripherals["CRTC"]
        p.registers["CRTC_ADDR"] = {
            address = 0xE880,
            size = 1,
            value = 0
        }
        p.registers["CRTC_DATA"] = {
            address = 0xE881,
            size = 1,
            value = 0
        }
        self.peripherals["Cassette"] = {
            base = ,
            type = "Storage",
            description = "Cassette tape interface",
            registers = {}
        }
        
        local p = self.peripherals["Cassette"]
        p.registers["CASS_MOTOR"] = {
            address = 0xE840,
            size = 1,
            value = 0
        }
        p.registers["CASS_WRITE"] = {
            address = 0xE842,
            size = 1,
            value = 0
        }
        p.registers["CASS_READ"] = {
            address = 0xE812,
            size = 1,
            value = 0
        }
        self.peripherals["IEEE488"] = {
            base = ,
            type = "IO",
            description = "IEEE-488 bus interface",
            registers = {}
        }
        
        local p = self.peripherals["IEEE488"]
        p.registers["IEEE_DATA"] = {
            address = 0xE801,
            size = 1,
            value = 0
        }
        p.registers["IEEE_STATUS"] = {
            address = 0xE802,
            size = 1,
            value = 0
        }
        p.registers["IEEE_CONTROL"] = {
            address = 0xE803,
            size = 1,
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
            name = Commodore_PET.DEVICE_NAME,
            manufacturer = Commodore_PET.MANUFACTURER,
            family = Commodore_PET.FAMILY,
            version = Commodore_PET.VERSION,
            architecture = Commodore_PET.ARCHITECTURE,
            bits = Commodore_PET.BITS,
            clock_frequency = Commodore_PET.CLOCK_FREQUENCY
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
        return string.format("Commodore_PET(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function Commodore_PET.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function Commodore_PET.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function Commodore_PET.print_device_info(device)
    device = device or Commodore_PET.new()
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

function Commodore_PET.print_registers(device)
    device = device or Commodore_PET.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            Commodore_PET.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function Commodore_PET.example()
    print("=== Commodore-PET设备示例 ===")
    
    -- 创建设备实例
    local device = Commodore_PET.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    Commodore_PET.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["A"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("A", 0x55)
        print("写入 A: " .. Commodore_PET.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("A")
        print("读取 A: " .. Commodore_PET.hex(value))
        
        -- 位操作
        device:set_bit("A", 0, true)
        local bit0 = device:get_bit("A", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    Commodore_PET.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("Commodore_PET.lua$") then
    Commodore_PET.example()
end

return Commodore_PET
