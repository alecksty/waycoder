--[[
  Macintosh-128K设备定义 - Lua模块
  生成自: Apple Computer/Macintosh/Macintosh-128K
  版本: 1.0
  日期: 2026-04-17
  作者: VML Team
  描述: Apple Macintosh 128K - First Macintosh - Motorola 68000, 128KB RAM, 512x342 display
  CPU架构: MC68000
  位宽: 32位
  时钟频率: 7833600 Hz
]]

local Macintosh_128K = {}

-- 设备信息
Macintosh_128K.DEVICE_NAME = "Macintosh-128K"
Macintosh_128K.MANUFACTURER = "Apple Computer"
Macintosh_128K.FAMILY = "Macintosh"
Macintosh_128K.VERSION = "1.0"
Macintosh_128K.ARCHITECTURE = "MC68000"
Macintosh_128K.BITS = 32
Macintosh_128K.CLOCK_FREQUENCY = 7833600

-- 寄存器地址定义
Macintosh_128K.D0_ADDR = 0x00  -- Data Register 0
Macintosh_128K.D1_ADDR = 0x04  -- Data Register 1
Macintosh_128K.D2_ADDR = 0x08  -- Data Register 2
Macintosh_128K.D3_ADDR = 0x0C  -- Data Register 3
Macintosh_128K.D4_ADDR = 0x10  -- Data Register 4
Macintosh_128K.D5_ADDR = 0x14  -- Data Register 5
Macintosh_128K.D6_ADDR = 0x18  -- Data Register 6
Macintosh_128K.D7_ADDR = 0x1C  -- Data Register 7
Macintosh_128K.A0_ADDR = 0x20  -- Address Register 0
Macintosh_128K.A1_ADDR = 0x24  -- Address Register 1
Macintosh_128K.A2_ADDR = 0x28  -- Address Register 2
Macintosh_128K.A3_ADDR = 0x2C  -- Address Register 3
Macintosh_128K.A4_ADDR = 0x30  -- Address Register 4
Macintosh_128K.A5_ADDR = 0x34  -- Address Register 5
Macintosh_128K.A6_ADDR = 0x38  -- Address Register 6
Macintosh_128K.A7_ADDR = 0x3C  -- Stack Pointer (USP)
Macintosh_128K.PC_ADDR = 0x40  -- Program Counter
Macintosh_128K.SR_ADDR = 0x44  -- Status Register
Macintosh_128K.SR_C_BIT = 0  -- Carry
Macintosh_128K.SR_V_BIT = 1  -- Overflow
Macintosh_128K.SR_Z_BIT = 2  -- Zero
Macintosh_128K.SR_N_BIT = 3  -- Negative
Macintosh_128K.SR_X_BIT = 4  -- Extend
Macintosh_128K.SR_I0_BIT = 8  -- Interrupt Mask 0
Macintosh_128K.SR_I1_BIT = 9  -- Interrupt Mask 1
Macintosh_128K.SR_I2_BIT = 10  -- Interrupt Mask 2
Macintosh_128K.SR_S_BIT = 13  -- Supervisor/User
Macintosh_128K.SR_T0_BIT = 14  -- Trace Mode 0
Macintosh_128K.SR_T1_BIT = 15  -- Trace Mode 1

-- 内存段定义
Macintosh_128K.RAM_START = 0x000000
Macintosh_128K.RAM_END = 0x01FFFF
Macintosh_128K.RAM_SIZE = 131072  -- Main RAM (128KB unified)
Macintosh_128K.ROM_START = 0x40000000
Macintosh_128K.ROM_END = 0x4001FFFF
Macintosh_128K.ROM_SIZE = 131072  -- Mac ROM (128KB)
Macintosh_128K.FRAMEBUFFER_START = 0x00400000
Macintosh_128K.FRAMEBUFFER_END = 0x00400555
Macintosh_128K.FRAMEBUFFER_SIZE = 1366  -- Screen bitmap (512x342x1 = 21792 bytes)
Macintosh_128K.FRAMEBUFFER2_START = 0x00410000
Macintosh_128K.FRAMEBUFFER2_END = 0x00410555
Macintosh_128K.FRAMEBUFFER2_SIZE = 1366  -- Shadow screen (double-buffering)
Macintosh_128K.VIA_START = 0x00E00000
Macintosh_128K.VIA_END = 0x00E0FFFF
Macintosh_128K.VIA_SIZE = 4096  -- VIA 6522 (I/O)
Macintosh_128K.SCC_START = 0x00F00000
Macintosh_128K.SCC_END = 0x00F0FFFF
Macintosh_128K.SCC_SIZE = 4096  -- SCC 8530 (serial)
Macintosh_128K.ADB_START = 0x01600000
Macintosh_128K.ADB_END = 0x0160FFFF
Macintosh_128K.ADB_SIZE = 4096  -- ADB bus
Macintosh_128K.IWM_START = 0x01E00000
Macintosh_128K.IWM_END = 0x01E0FFFF
Macintosh_128K.IWM_SIZE = 4096  -- IWM floppy controller

-- 外设定义
-- Versatile Interface Adapter 6522
Macintosh_128K.VIA_BASE = 0xE00000
Macintosh_128K.VIA_ORB_ADDR = 0xE00000
Macintosh_128K.VIA_ORA_ADDR = 0xE00002
Macintosh_128K.VIA_DDRB_ADDR = 0xE00004
Macintosh_128K.VIA_DDRA_ADDR = 0xE00006
Macintosh_128K.VIA_T1C_L_ADDR = 0xE00008
Macintosh_128K.VIA_T1C_H_ADDR = 0xE0000A
Macintosh_128K.VIA_T1L_L_ADDR = 0xE0000C
Macintosh_128K.VIA_T1L_H_ADDR = 0xE0000E
Macintosh_128K.VIA_T2C_L_ADDR = 0xE00010
Macintosh_128K.VIA_T2C_H_ADDR = 0xE00012
Macintosh_128K.VIA_SR_ADDR = 0xE00014
Macintosh_128K.VIA_ACR_ADDR = 0xE00016
Macintosh_128K.VIA_PCR_ADDR = 0xE00018
Macintosh_128K.VIA_IFR_ADDR = 0xE0001E
Macintosh_128K.VIA_IER_ADDR = 0xE0001E
-- SCC 8530 Serial Communications Controller
Macintosh_128K.SCC_BASE = 0xF00000
Macintosh_128K.SCC_SCC_CHA_B_ADDR = 0xF00000
Macintosh_128K.SCC_SCC_CHA_C_ADDR = 0xF00002
Macintosh_128K.SCC_SCC_CHB_D_ADDR = 0xF00004
Macintosh_128K.SCC_SCC_CHB_CT_ADDR = 0xF00006
-- Integrated Woz Machine - Floppy Disk Controller
Macintosh_128K.IWM_BASE = 0x1E00000
Macintosh_128K.IWM_IWM_DATA_ADDR = 0x1E00000
Macintosh_128K.IWM_IWM_MODE_ADDR = 0x1E00008
Macintosh_128K.IWM_IWM_Q6L_ADDR = 0x1E00020
Macintosh_128K.IWM_IWM_Q7L_ADDR = 0x1E00022
Macintosh_128K.IWM_IWM_Q6R_ADDR = 0x1E00024
Macintosh_128K.IWM_IWM_Q7R_ADDR = 0x1E00026
-- Video Graphics Controller (custom Apple chip)
Macintosh_128K.VGC_BASE = 0x00F20000
Macintosh_128K.VGC_VGC_MODE_ADDR = 0x00F20000
Macintosh_128K.VGC_VGC_START_HI_ADDR = 0x00F20002
Macintosh_128K.VGC_VGC_START_LO_ADDR = 0x00F20004
-- Apple Desktop Bus
Macintosh_128K.ADB_BASE = 0x01600000
Macintosh_128K.ADB_ADB_DATA_ADDR = 0x01600000
Macintosh_128K.ADB_ADB_STATUS_ADDR = 0x01600004
Macintosh_128K.ADB_ADB_CMD_ADDR = 0x01600008

-- 中断向量定义
Macintosh_128K.INT_RESET = 1  -- Reset Initial SP
Macintosh_128K.INT_RESET_PC = 2  -- Reset Initial PC
Macintosh_128K.INT_IRQ1 = 24  -- VIA interrupt (level 1)
Macintosh_128K.INT_IRQ2 = 25  -- SCC interrupt (level 2)
Macintosh_128K.INT_IRQ3 = 26  -- ADB / VIA (level 3)
Macintosh_128K.INT_IRQ4 = 27  -- ADB / VIA (level 4)

-- 引脚定义
Macintosh_128K.PIN_VCC = 1  -- +5V Power
Macintosh_128K.PIN_GND = 2  -- Ground
Macintosh_128K.PIN_CLK = 3  -- 16MHz master clock / 7.83MHz CPU clock
Macintosh_128K.PIN_FC0 = 4  -- Function Code 0
Macintosh_128K.PIN_FC1 = 5  -- Function Code 1
Macintosh_128K.PIN_FC2 = 6  -- Function Code 2
Macintosh_128K.PIN_AS = 7  -- Address Strobe
Macintosh_128K.PIN_UDS = 8  -- Upper Data Strobe
Macintosh_128K.PIN_LDS = 9  -- Lower Data Strobe
Macintosh_128K.PIN_RWB = 10  -- Read/Write
Macintosh_128K.PIN_DTACK = 11  -- Data Acknowledge
Macintosh_128K.PIN_BERR = 12  -- Bus Error
Macintosh_128K.PIN_BR = 13  -- Bus Request
Macintosh_128K.PIN_BG = 14  -- Bus Grant
Macintosh_128K.PIN_BGACK = 15  -- Bus Grant Acknowledge
Macintosh_128K.PIN_IPL0 = 16  -- Interrupt Priority 0
Macintosh_128K.PIN_IPL1 = 17  -- Interrupt Priority 1
Macintosh_128K.PIN_IPL2 = 18  -- Interrupt Priority 2
Macintosh_128K.PIN_RESET = 19  -- Reset
Macintosh_128K.PIN_HALT = 20  -- Halt
Macintosh_128K.PIN_A1_A23 = 21  -- Address Bus (24-bit)
Macintosh_128K.PIN_D0_D15 = 22  -- Data Bus (16-bit)

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
            address = 0x00,
            size = 4,
            access = "rw",
            description = "Data Register 0",
            value = 0
        }
        self.registers["D1"] = {
            address = 0x04,
            size = 4,
            access = "rw",
            description = "Data Register 1",
            value = 0
        }
        self.registers["D2"] = {
            address = 0x08,
            size = 4,
            access = "rw",
            description = "Data Register 2",
            value = 0
        }
        self.registers["D3"] = {
            address = 0x0C,
            size = 4,
            access = "rw",
            description = "Data Register 3",
            value = 0
        }
        self.registers["D4"] = {
            address = 0x10,
            size = 4,
            access = "rw",
            description = "Data Register 4",
            value = 0
        }
        self.registers["D5"] = {
            address = 0x14,
            size = 4,
            access = "rw",
            description = "Data Register 5",
            value = 0
        }
        self.registers["D6"] = {
            address = 0x18,
            size = 4,
            access = "rw",
            description = "Data Register 6",
            value = 0
        }
        self.registers["D7"] = {
            address = 0x1C,
            size = 4,
            access = "rw",
            description = "Data Register 7",
            value = 0
        }
        self.registers["A0"] = {
            address = 0x20,
            size = 4,
            access = "rw",
            description = "Address Register 0",
            value = 0
        }
        self.registers["A1"] = {
            address = 0x24,
            size = 4,
            access = "rw",
            description = "Address Register 1",
            value = 0
        }
        self.registers["A2"] = {
            address = 0x28,
            size = 4,
            access = "rw",
            description = "Address Register 2",
            value = 0
        }
        self.registers["A3"] = {
            address = 0x2C,
            size = 4,
            access = "rw",
            description = "Address Register 3",
            value = 0
        }
        self.registers["A4"] = {
            address = 0x30,
            size = 4,
            access = "rw",
            description = "Address Register 4",
            value = 0
        }
        self.registers["A5"] = {
            address = 0x34,
            size = 4,
            access = "rw",
            description = "Address Register 5",
            value = 0
        }
        self.registers["A6"] = {
            address = 0x38,
            size = 4,
            access = "rw",
            description = "Address Register 6",
            value = 0
        }
        self.registers["A7"] = {
            address = 0x3C,
            size = 4,
            access = "rw",
            description = "Stack Pointer (USP)",
            value = 0
        }
        self.registers["PC"] = {
            address = 0x40,
            size = 4,
            access = "r",
            description = "Program Counter",
            value = 0
        }
        self.registers["SR"] = {
            address = 0x44,
            size = 2,
            access = "rw",
            description = "Status Register",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["VIA"] = {
            base = 0xE00000,
            type = "gpio",
            description = "Versatile Interface Adapter 6522",
            registers = {}
        }
        
        local p = self.peripherals["VIA"]
        p.registers["ORB"] = {
            address = 0xE00000,
            size = 1,
            value = 0
        }
        p.registers["ORA"] = {
            address = 0xE00002,
            size = 1,
            value = 0
        }
        p.registers["DDRB"] = {
            address = 0xE00004,
            size = 1,
            value = 0
        }
        p.registers["DDRA"] = {
            address = 0xE00006,
            size = 1,
            value = 0
        }
        p.registers["T1C_L"] = {
            address = 0xE00008,
            size = 2,
            value = 0
        }
        p.registers["T1C_H"] = {
            address = 0xE0000A,
            size = 2,
            value = 0
        }
        p.registers["T1L_L"] = {
            address = 0xE0000C,
            size = 2,
            value = 0
        }
        p.registers["T1L_H"] = {
            address = 0xE0000E,
            size = 2,
            value = 0
        }
        p.registers["T2C_L"] = {
            address = 0xE00010,
            size = 2,
            value = 0
        }
        p.registers["T2C_H"] = {
            address = 0xE00012,
            size = 2,
            value = 0
        }
        p.registers["SR"] = {
            address = 0xE00014,
            size = 1,
            value = 0
        }
        p.registers["ACR"] = {
            address = 0xE00016,
            size = 1,
            value = 0
        }
        p.registers["PCR"] = {
            address = 0xE00018,
            size = 1,
            value = 0
        }
        p.registers["IFR"] = {
            address = 0xE0001E,
            size = 1,
            value = 0
        }
        p.registers["IER"] = {
            address = 0xE0001E,
            size = 1,
            value = 0
        }
        self.peripherals["SCC"] = {
            base = 0xF00000,
            type = "serial",
            description = "SCC 8530 Serial Communications Controller",
            registers = {}
        }
        
        local p = self.peripherals["SCC"]
        p.registers["SCC_CHA_B"] = {
            address = 0xF00000,
            size = 1,
            value = 0
        }
        p.registers["SCC_CHA_C"] = {
            address = 0xF00002,
            size = 1,
            value = 0
        }
        p.registers["SCC_CHB_D"] = {
            address = 0xF00004,
            size = 1,
            value = 0
        }
        p.registers["SCC_CHB_CT"] = {
            address = 0xF00006,
            size = 1,
            value = 0
        }
        self.peripherals["IWM"] = {
            base = 0x1E00000,
            type = "storage",
            description = "Integrated Woz Machine - Floppy Disk Controller",
            registers = {}
        }
        
        local p = self.peripherals["IWM"]
        p.registers["IWM_DATA"] = {
            address = 0x1E00000,
            size = 1,
            value = 0
        }
        p.registers["IWM_MODE"] = {
            address = 0x1E00008,
            size = 1,
            value = 0
        }
        p.registers["IWM_Q6L"] = {
            address = 0x1E00020,
            size = 1,
            value = 0
        }
        p.registers["IWM_Q7L"] = {
            address = 0x1E00022,
            size = 1,
            value = 0
        }
        p.registers["IWM_Q6R"] = {
            address = 0x1E00024,
            size = 1,
            value = 0
        }
        p.registers["IWM_Q7R"] = {
            address = 0x1E00026,
            size = 1,
            value = 0
        }
        self.peripherals["VGC"] = {
            base = 0x00F20000,
            type = "video",
            description = "Video Graphics Controller (custom Apple chip)",
            registers = {}
        }
        
        local p = self.peripherals["VGC"]
        p.registers["VGC_MODE"] = {
            address = 0x00F20000,
            size = 1,
            value = 0
        }
        p.registers["VGC_START_HI"] = {
            address = 0x00F20002,
            size = 1,
            value = 0
        }
        p.registers["VGC_START_LO"] = {
            address = 0x00F20004,
            size = 1,
            value = 0
        }
        self.peripherals["ADB"] = {
            base = 0x01600000,
            type = "bus",
            description = "Apple Desktop Bus",
            registers = {}
        }
        
        local p = self.peripherals["ADB"]
        p.registers["ADB_DATA"] = {
            address = 0x01600000,
            size = 1,
            value = 0
        }
        p.registers["ADB_STATUS"] = {
            address = 0x01600004,
            size = 1,
            value = 0
        }
        p.registers["ADB_CMD"] = {
            address = 0x01600008,
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
