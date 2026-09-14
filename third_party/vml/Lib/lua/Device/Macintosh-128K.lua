--[[
  Macintosh-128K设备定义 - Lua模块
  生成自: Apple Computer/Macintosh/Macintosh-128K
  版本: 1.0
  日期: 2026-04-17
  作者: VML Team
  描述: Original Macintosh 128K with Motorola 68000 CPU, 128KB RAM, and 9-inch monochrome display
  CPU架构: Motorola 68000
  位宽: 32位
  时钟频率: 7998000 Hz
]]

local Macintosh_128K = {}

-- 设备信息
Macintosh_128K.DEVICE_NAME = "Macintosh-128K"
Macintosh_128K.MANUFACTURER = "Apple Computer"
Macintosh_128K.FAMILY = "Macintosh"
Macintosh_128K.VERSION = "1.0"
Macintosh_128K.ARCHITECTURE = "Motorola 68000"
Macintosh_128K.BITS = 32
Macintosh_128K.CLOCK_FREQUENCY = 7998000

-- 寄存器地址定义
Macintosh_128K.D0_ADDR = 0  -- Data Register 0
Macintosh_128K.D1_ADDR = 0  -- Data Register 1
Macintosh_128K.D2_ADDR = 0  -- Data Register 2
Macintosh_128K.D3_ADDR = 0  -- Data Register 3
Macintosh_128K.D4_ADDR = 0  -- Data Register 4
Macintosh_128K.D5_ADDR = 0  -- Data Register 5
Macintosh_128K.D6_ADDR = 0  -- Data Register 6
Macintosh_128K.D7_ADDR = 0  -- Data Register 7
Macintosh_128K.A0_ADDR = 0  -- Address Register 0
Macintosh_128K.A1_ADDR = 0  -- Address Register 1
Macintosh_128K.A2_ADDR = 0  -- Address Register 2
Macintosh_128K.A3_ADDR = 0  -- Address Register 3
Macintosh_128K.A4_ADDR = 0  -- Address Register 4
Macintosh_128K.A5_ADDR = 0  -- Address Register 5
Macintosh_128K.A6_ADDR = 0  -- Address Register 6
Macintosh_128K.A7_ADDR = 0  -- Address Register 7 (SP)
Macintosh_128K.PC_ADDR = 0  -- Program Counter
Macintosh_128K.SR_ADDR = 0  -- Status Register

-- 外设定义
-- Versatile Interface Adapter (6522)
Macintosh_128K.VIA_BASE = 
Macintosh_128K.VIA_VIA_ORB_ADDR = 0xE80000
Macintosh_128K.VIA_VIA_ORA_ADDR = 0xE80001
Macintosh_128K.VIA_VIA_DDRB_ADDR = 0xE80002
Macintosh_128K.VIA_VIA_DDRA_ADDR = 0xE80003
Macintosh_128K.VIA_VIA_T1CL_ADDR = 0xE80004
Macintosh_128K.VIA_VIA_T1CH_ADDR = 0xE80005
Macintosh_128K.VIA_VIA_T1LL_ADDR = 0xE80006
Macintosh_128K.VIA_VIA_T1LH_ADDR = 0xE80007
Macintosh_128K.VIA_VIA_T2CL_ADDR = 0xE80008
Macintosh_128K.VIA_VIA_T2CH_ADDR = 0xE80009
Macintosh_128K.VIA_VIA_SR_ADDR = 0xE8000A
Macintosh_128K.VIA_VIA_ACR_ADDR = 0xE8000B
Macintosh_128K.VIA_VIA_PCR_ADDR = 0xE8000C
Macintosh_128K.VIA_VIA_IFR_ADDR = 0xE8000D
Macintosh_128K.VIA_VIA_IER_ADDR = 0xE8000E
Macintosh_128K.VIA_VIA_ORA2_ADDR = 0xE8000F
-- Integrated Woz Machine (floppy controller)
Macintosh_128K.IWM_BASE = 
Macintosh_128K.IWM_IWM_Q6_ADDR = 0xD00000
Macintosh_128K.IWM_IWM_Q7_ADDR = 0xD00002
Macintosh_128K.IWM_IWM_PH0_ADDR = 0xD00004
Macintosh_128K.IWM_IWM_PH1_ADDR = 0xD00006
Macintosh_128K.IWM_IWM_PH2_ADDR = 0xD00008
Macintosh_128K.IWM_IWM_PH3_ADDR = 0xD0000A
-- Zilog 8530 Serial Communications Controller
Macintosh_128K.SCC_BASE = 
Macintosh_128K.SCC_SCC_CA_ADDR = 0x500000
Macintosh_128K.SCC_SCC_DA_ADDR = 0x500002
Macintosh_128K.SCC_SCC_CB_ADDR = 0x500004
Macintosh_128K.SCC_SCC_DB_ADDR = 0x500006
-- Built-in speaker
Macintosh_128K.SOUND_BASE = 
Macintosh_128K.SOUND_SOUND_VOL_ADDR = 0xE80100
Macintosh_128K.SOUND_SOUND_FREQ_ADDR = 0xE80102

-- 中断向量定义
Macintosh_128K.INT_RESET_SP = 0  -- Reset (Initial SP)
Macintosh_128K.INT_RESET_PC = 4  -- Reset (Initial PC)
Macintosh_128K.INT_AUTOVECTOR1 = 24  -- Auto vector 1
Macintosh_128K.INT_AUTOVECTOR2 = 25  -- Auto vector 2
Macintosh_128K.INT_AUTOVECTOR3 = 26  -- Auto vector 3
Macintosh_128K.INT_AUTOVECTOR4 = 27  -- Auto vector 4
Macintosh_128K.INT_AUTOVECTOR5 = 28  -- Auto vector 5
Macintosh_128K.INT_AUTOVECTOR6 = 29  -- Auto vector 6
Macintosh_128K.INT_AUTOVECTOR7 = 30  -- Auto vector 7
Macintosh_128K.INT_SPURIOUS = 31  -- Spurious interrupt

-- 设备类
function Macintosh_128K.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["D0"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Data Register 0",
            value = 0
        }
        self.registers["D1"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Data Register 1",
            value = 0
        }
        self.registers["D2"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Data Register 2",
            value = 0
        }
        self.registers["D3"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Data Register 3",
            value = 0
        }
        self.registers["D4"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Data Register 4",
            value = 0
        }
        self.registers["D5"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Data Register 5",
            value = 0
        }
        self.registers["D6"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Data Register 6",
            value = 0
        }
        self.registers["D7"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Data Register 7",
            value = 0
        }
        self.registers["A0"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Address Register 0",
            value = 0
        }
        self.registers["A1"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Address Register 1",
            value = 0
        }
        self.registers["A2"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Address Register 2",
            value = 0
        }
        self.registers["A3"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Address Register 3",
            value = 0
        }
        self.registers["A4"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Address Register 4",
            value = 0
        }
        self.registers["A5"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Address Register 5",
            value = 0
        }
        self.registers["A6"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Address Register 6",
            value = 0
        }
        self.registers["A7"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Address Register 7 (SP)",
            value = 0
        }
        self.registers["PC"] = {
            address = 0,
            size = 4,
            access = "rw",
            description = "Program Counter",
            value = 0
        }
        self.registers["SR"] = {
            address = 0,
            size = 2,
            access = "rw",
            description = "Status Register",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["VIA"] = {
            base = ,
            type = "IO",
            description = "Versatile Interface Adapter (6522)",
            registers = {}
        }
        
        local p = self.peripherals["VIA"]
        p.registers["VIA_ORB"] = {
            address = 0xE80000,
            size = 1,
            value = 0
        }
        p.registers["VIA_ORA"] = {
            address = 0xE80001,
            size = 1,
            value = 0
        }
        p.registers["VIA_DDRB"] = {
            address = 0xE80002,
            size = 1,
            value = 0
        }
        p.registers["VIA_DDRA"] = {
            address = 0xE80003,
            size = 1,
            value = 0
        }
        p.registers["VIA_T1CL"] = {
            address = 0xE80004,
            size = 1,
            value = 0
        }
        p.registers["VIA_T1CH"] = {
            address = 0xE80005,
            size = 1,
            value = 0
        }
        p.registers["VIA_T1LL"] = {
            address = 0xE80006,
            size = 1,
            value = 0
        }
        p.registers["VIA_T1LH"] = {
            address = 0xE80007,
            size = 1,
            value = 0
        }
        p.registers["VIA_T2CL"] = {
            address = 0xE80008,
            size = 1,
            value = 0
        }
        p.registers["VIA_T2CH"] = {
            address = 0xE80009,
            size = 1,
            value = 0
        }
        p.registers["VIA_SR"] = {
            address = 0xE8000A,
            size = 1,
            value = 0
        }
        p.registers["VIA_ACR"] = {
            address = 0xE8000B,
            size = 1,
            value = 0
        }
        p.registers["VIA_PCR"] = {
            address = 0xE8000C,
            size = 1,
            value = 0
        }
        p.registers["VIA_IFR"] = {
            address = 0xE8000D,
            size = 1,
            value = 0
        }
        p.registers["VIA_IER"] = {
            address = 0xE8000E,
            size = 1,
            value = 0
        }
        p.registers["VIA_ORA2"] = {
            address = 0xE8000F,
            size = 1,
            value = 0
        }
        self.peripherals["IWM"] = {
            base = ,
            type = "Storage",
            description = "Integrated Woz Machine (floppy controller)",
            registers = {}
        }
        
        local p = self.peripherals["IWM"]
        p.registers["IWM_Q6"] = {
            address = 0xD00000,
            size = 1,
            value = 0
        }
        p.registers["IWM_Q7"] = {
            address = 0xD00002,
            size = 1,
            value = 0
        }
        p.registers["IWM_PH0"] = {
            address = 0xD00004,
            size = 1,
            value = 0
        }
        p.registers["IWM_PH1"] = {
            address = 0xD00006,
            size = 1,
            value = 0
        }
        p.registers["IWM_PH2"] = {
            address = 0xD00008,
            size = 1,
            value = 0
        }
        p.registers["IWM_PH3"] = {
            address = 0xD0000A,
            size = 1,
            value = 0
        }
        self.peripherals["SCC"] = {
            base = ,
            type = "Serial",
            description = "Zilog 8530 Serial Communications Controller",
            registers = {}
        }
        
        local p = self.peripherals["SCC"]
        p.registers["SCC_CA"] = {
            address = 0x500000,
            size = 1,
            value = 0
        }
        p.registers["SCC_DA"] = {
            address = 0x500002,
            size = 1,
            value = 0
        }
        p.registers["SCC_CB"] = {
            address = 0x500004,
            size = 1,
            value = 0
        }
        p.registers["SCC_DB"] = {
            address = 0x500006,
            size = 1,
            value = 0
        }
        self.peripherals["Sound"] = {
            base = ,
            type = "Audio",
            description = "Built-in speaker",
            registers = {}
        }
        
        local p = self.peripherals["Sound"]
        p.registers["SOUND_VOL"] = {
            address = 0xE80100,
            size = 1,
            value = 0
        }
        p.registers["SOUND_FREQ"] = {
            address = 0xE80102,
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
            name = Macintosh_128K.DEVICE_NAME,
            manufacturer = Macintosh_128K.MANUFACTURER,
            family = Macintosh_128K.FAMILY,
            version = Macintosh_128K.VERSION,
            architecture = Macintosh_128K.ARCHITECTURE,
            bits = Macintosh_128K.BITS,
            clock_frequency = Macintosh_128K.CLOCK_FREQUENCY
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
        return string.format("Macintosh_128K(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function Macintosh_128K.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function Macintosh_128K.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function Macintosh_128K.print_device_info(device)
    device = device or Macintosh_128K.new()
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

function Macintosh_128K.print_registers(device)
    device = device or Macintosh_128K.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            Macintosh_128K.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function Macintosh_128K.example()
    print("=== Macintosh-128K设备示例 ===")
    
    -- 创建设备实例
    local device = Macintosh_128K.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    Macintosh_128K.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["D0"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("D0", 0x55)
        print("写入 D0: " .. Macintosh_128K.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("D0")
        print("读取 D0: " .. Macintosh_128K.hex(value))
        
        -- 位操作
        device:set_bit("D0", 0, true)
        local bit0 = device:get_bit("D0", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    Macintosh_128K.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("Macintosh_128K.lua$") then
    Macintosh_128K.example()
end

return Macintosh_128K
