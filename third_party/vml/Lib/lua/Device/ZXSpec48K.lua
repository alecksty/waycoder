--[[
  ZX-Spectrum-48K设备定义 - Lua模块
  生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum-48K
  版本: 1.0
  日期: 2026-04-17
  作者: VML Team
  描述: Sinclair ZX Spectrum 48K - Iconic British 8-bit home computer with Z80A CPU and ULA graphics
  CPU架构: Z80A
  位宽: 8位
  时钟频率: 3500000 Hz
]]

local ZX_Spectrum_48K = {}

-- 设备信息
ZX_Spectrum_48K.DEVICE_NAME = "ZX-Spectrum-48K"
ZX_Spectrum_48K.MANUFACTURER = "Sinclair Research"
ZX_Spectrum_48K.FAMILY = "ZX Spectrum"
ZX_Spectrum_48K.VERSION = "1.0"
ZX_Spectrum_48K.ARCHITECTURE = "Z80A"
ZX_Spectrum_48K.BITS = 8
ZX_Spectrum_48K.CLOCK_FREQUENCY = 3500000

-- 寄存器地址定义
ZX_Spectrum_48K.A_ADDR = 0x00  -- Accumulator
ZX_Spectrum_48K.F_ADDR = 0x01  -- Flags Register
ZX_Spectrum_48K.F_C_BIT = 0  -- Carry
ZX_Spectrum_48K.F_N_BIT = 1  -- Add/Subtract
ZX_Spectrum_48K.F_PV_BIT = 2  -- Parity/Overflow
ZX_Spectrum_48K.F_H_BIT = 4  -- Half Carry
ZX_Spectrum_48K.F_Z_BIT = 6  -- Zero
ZX_Spectrum_48K.F_S_BIT = 7  -- Sign
ZX_Spectrum_48K.B_ADDR = 0x02  -- B Register
ZX_Spectrum_48K.C_ADDR = 0x03  -- C Register
ZX_Spectrum_48K.D_ADDR = 0x04  -- D Register
ZX_Spectrum_48K.E_ADDR = 0x05  -- E Register
ZX_Spectrum_48K.H_ADDR = 0x06  -- H Register
ZX_Spectrum_48K.L_ADDR = 0x07  -- L Register
ZX_Spectrum_48K.AF_ADDR = 0x08  -- Alternate AF
ZX_Spectrum_48K.BC_ADDR = 0x0A  -- Alternate BC
ZX_Spectrum_48K.DE_ADDR = 0x0C  -- Alternate DE
ZX_Spectrum_48K.HL_ADDR = 0x0E  -- Alternate HL
ZX_Spectrum_48K.I_ADDR = 0x10  -- Interrupt Vector Register
ZX_Spectrum_48K.R_ADDR = 0x11  -- Refresh Counter
ZX_Spectrum_48K.IX_ADDR = 0x12  -- Index X
ZX_Spectrum_48K.IY_ADDR = 0x14  -- Index Y
ZX_Spectrum_48K.SP_ADDR = 0x16  -- Stack Pointer
ZX_Spectrum_48K.PC_ADDR = 0x18  -- Program Counter

-- 内存段定义
ZX_Spectrum_48K.ROM_START = 0x0000
ZX_Spectrum_48K.ROM_END = 0x3FFF
ZX_Spectrum_48K.ROM_SIZE = 16384  -- 48KB ZX Spectrum ROM (BASIC + monitor)
ZX_Spectrum_48K.VIDEO_RAM_START = 0x4000
ZX_Spectrum_48K.VIDEO_RAM_END = 0x57FF
ZX_Spectrum_48K.VIDEO_RAM_SIZE = 6144  -- Display file (256x192 bitmap)
ZX_Spectrum_48K.ATTR_RAM_START = 0x5800
ZX_Spectrum_48K.ATTR_RAM_END = 0x5AFF
ZX_Spectrum_48K.ATTR_RAM_SIZE = 768  -- Attribute file (32x24 color cells)
ZX_Spectrum_48K.USER_RAM_START = 0x5B00
ZX_Spectrum_48K.USER_RAM_END = 0xFFFF
ZX_Spectrum_48K.USER_RAM_SIZE = 40960  -- User RAM (40KB)

-- 外设定义
-- Uncommitted Logic Array - Sinclair custom IC
ZX_Spectrum_48K.ULA_BASE = 0xFE
ZX_Spectrum_48K.ULA_BORDER_ADDR = 0xFE
ZX_Spectrum_48K.ULA_KBD_ROW0_ADDR = 0xFE
ZX_Spectrum_48K.ULA_KBD_ROW1_ADDR = 0xFE
ZX_Spectrum_48K.ULA_KBD_ROW2_ADDR = 0xFE
ZX_Spectrum_48K.ULA_KBD_ROW3_ADDR = 0xFE
ZX_Spectrum_48K.ULA_KBD_ROW4_ADDR = 0xFE
ZX_Spectrum_48K.ULA_KBD_ROW5_ADDR = 0xFE
ZX_Spectrum_48K.ULA_KBD_ROW6_ADDR = 0xFE
ZX_Spectrum_48K.ULA_KBD_ROW7_ADDR = 0xFE
ZX_Spectrum_48K.ULA_KBD_ROW8_ADDR = 0xFE
-- Keyboard Matrix (40 keys, 8 rows x 5 cols)
ZX_Spectrum_48K.KEYBOARD_BASE = 0xFE
ZX_Spectrum_48K.KEYBOARD_KBD_IN_ADDR = 0xFE
-- Internal Beeper
ZX_Spectrum_48K.BEEPER_BASE = 0xFE
ZX_Spectrum_48K.BEEPER_BEEP_ADDR = 0xFE
-- Tape Interface
ZX_Spectrum_48K.TAPE_BASE = 0xFE
ZX_Spectrum_48K.TAPE_EAR_IN_ADDR = 0xFE
ZX_Spectrum_48K.TAPE_MIC_OUT_ADDR = 0xFE
-- Kempston Joystick Interface
ZX_Spectrum_48K.JOYSTICK_BASE = 0xF7FE
ZX_Spectrum_48K.JOYSTICK_KEMPSTON_ADDR = 0xF7FE

-- 中断向量定义
ZX_Spectrum_48K.INT_RESET = 0  -- Power-on / Reset
ZX_Spectrum_48K.INT_NMI = 1  -- Non-Maskable Interrupt (BREAK key)
ZX_Spectrum_48K.INT_INT = 2  -- Maskable Interrupt (ULA vertical blank, 50Hz)

-- 引脚定义
ZX_Spectrum_48K.PIN_VCC = 1  -- +5V Power
ZX_Spectrum_48K.PIN_GND = 2  -- Ground
ZX_Spectrum_48K.PIN_CLK = 3  -- Z80 Clock (3.5MHz)
ZX_Spectrum_48K.PIN_M1 = 4  -- Machine Cycle 1
ZX_Spectrum_48K.PIN_MREQ = 5  -- Memory Request
ZX_Spectrum_48K.PIN_IORQ = 6  -- I/O Request
ZX_Spectrum_48K.PIN_RD = 7  -- Read
ZX_Spectrum_48K.PIN_WR = 8  -- Write
ZX_Spectrum_48K.PIN_HALT = 9  -- Halt State
ZX_Spectrum_48K.PIN_BUSAK = 10  -- Bus Acknowledge
ZX_Spectrum_48K.PIN_WAIT = 11  -- Wait State (ULA inserts)
ZX_Spectrum_48K.PIN_INT = 12  -- Interrupt Request
ZX_Spectrum_48K.PIN_NMI = 13  -- Non-Maskable Interrupt
ZX_Spectrum_48K.PIN_RESET = 14  -- Reset
ZX_Spectrum_48K.PIN_A0_A15 = 15  -- Address Bus (16-bit)
ZX_Spectrum_48K.PIN_D0_D7 = 16  -- Data Bus (8-bit)

-- 设备类
function ZX_Spectrum_48K.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["A"] = {
            address = 0x00,
            size = 1,
            access = "rw",
            description = "Accumulator",
            value = 0
        }
        self.registers["F"] = {
            address = 0x01,
            size = 1,
            access = "rw",
            description = "Flags Register",
            value = 0
        }
        self.registers["B"] = {
            address = 0x02,
            size = 1,
            access = "rw",
            description = "B Register",
            value = 0
        }
        self.registers["C"] = {
            address = 0x03,
            size = 1,
            access = "rw",
            description = "C Register",
            value = 0
        }
        self.registers["D"] = {
            address = 0x04,
            size = 1,
            access = "rw",
            description = "D Register",
            value = 0
        }
        self.registers["E"] = {
            address = 0x05,
            size = 1,
            access = "rw",
            description = "E Register",
            value = 0
        }
        self.registers["H"] = {
            address = 0x06,
            size = 1,
            access = "rw",
            description = "H Register",
            value = 0
        }
        self.registers["L"] = {
            address = 0x07,
            size = 1,
            access = "rw",
            description = "L Register",
            value = 0
        }
        self.registers["AF'"] = {
            address = 0x08,
            size = 2,
            access = "rw",
            description = "Alternate AF",
            value = 0
        }
        self.registers["BC'"] = {
            address = 0x0A,
            size = 2,
            access = "rw",
            description = "Alternate BC",
            value = 0
        }
        self.registers["DE'"] = {
            address = 0x0C,
            size = 2,
            access = "rw",
            description = "Alternate DE",
            value = 0
        }
        self.registers["HL'"] = {
            address = 0x0E,
            size = 2,
            access = "rw",
            description = "Alternate HL",
            value = 0
        }
        self.registers["I"] = {
            address = 0x10,
            size = 1,
            access = "rw",
            description = "Interrupt Vector Register",
            value = 0
        }
        self.registers["R"] = {
            address = 0x11,
            size = 1,
            access = "rw",
            description = "Refresh Counter",
            value = 0
        }
        self.registers["IX"] = {
            address = 0x12,
            size = 2,
            access = "rw",
            description = "Index X",
            value = 0
        }
        self.registers["IY"] = {
            address = 0x14,
            size = 2,
            access = "rw",
            description = "Index Y",
            value = 0
        }
        self.registers["SP"] = {
            address = 0x16,
            size = 2,
            access = "rw",
            description = "Stack Pointer",
            value = 0
        }
        self.registers["PC"] = {
            address = 0x18,
            size = 2,
            access = "rw",
            description = "Program Counter",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["ULA"] = {
            base = 0xFE,
            type = "video",
            description = "Uncommitted Logic Array - Sinclair custom IC",
            registers = {}
        }
        
        local p = self.peripherals["ULA"]
        p.registers["BORDER"] = {
            address = 0xFE,
            size = 1,
            value = 0
        }
        p.registers["KBD_ROW0"] = {
            address = 0xFE,
            size = 1,
            value = 0
        }
        p.registers["KBD_ROW1"] = {
            address = 0xFE,
            size = 1,
            value = 0
        }
        p.registers["KBD_ROW2"] = {
            address = 0xFE,
            size = 1,
            value = 0
        }
        p.registers["KBD_ROW3"] = {
            address = 0xFE,
            size = 1,
            value = 0
        }
        p.registers["KBD_ROW4"] = {
            address = 0xFE,
            size = 1,
            value = 0
        }
        p.registers["KBD_ROW5"] = {
            address = 0xFE,
            size = 1,
            value = 0
        }
        p.registers["KBD_ROW6"] = {
            address = 0xFE,
            size = 1,
            value = 0
        }
        p.registers["KBD_ROW7"] = {
            address = 0xFE,
            size = 1,
            value = 0
        }
        p.registers["KBD_ROW8"] = {
            address = 0xFE,
            size = 1,
            value = 0
        }
        self.peripherals["KEYBOARD"] = {
            base = 0xFE,
            type = "input",
            description = "Keyboard Matrix (40 keys, 8 rows x 5 cols)",
            registers = {}
        }
        
        local p = self.peripherals["KEYBOARD"]
        p.registers["KBD_IN"] = {
            address = 0xFE,
            size = 1,
            value = 0
        }
        self.peripherals["BEEPER"] = {
            base = 0xFE,
            type = "audio",
            description = "Internal Beeper",
            registers = {}
        }
        
        local p = self.peripherals["BEEPER"]
        p.registers["BEEP"] = {
            address = 0xFE,
            size = 1,
            value = 0
        }
        self.peripherals["TAPE"] = {
            base = 0xFE,
            type = "storage",
            description = "Tape Interface",
            registers = {}
        }
        
        local p = self.peripherals["TAPE"]
        p.registers["EAR_IN"] = {
            address = 0xFE,
            size = 1,
            value = 0
        }
        p.registers["MIC_OUT"] = {
            address = 0xFE,
            size = 1,
            value = 0
        }
        self.peripherals["JOYSTICK"] = {
            base = 0xF7FE,
            type = "input",
            description = "Kempston Joystick Interface",
            registers = {}
        }
        
        local p = self.peripherals["JOYSTICK"]
        p.registers["KEMPSTON"] = {
            address = 0xF7FE,
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
            name = ZX_Spectrum_48K.DEVICE_NAME,
            manufacturer = ZX_Spectrum_48K.MANUFACTURER,
            family = ZX_Spectrum_48K.FAMILY,
            version = ZX_Spectrum_48K.VERSION,
            architecture = ZX_Spectrum_48K.ARCHITECTURE,
            bits = ZX_Spectrum_48K.BITS,
            clock_frequency = ZX_Spectrum_48K.CLOCK_FREQUENCY
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
        return string.format("ZX_Spectrum_48K(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function ZX_Spectrum_48K.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function ZX_Spectrum_48K.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function ZX_Spectrum_48K.print_device_info(device)
    device = device or ZX_Spectrum_48K.new()
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

function ZX_Spectrum_48K.print_registers(device)
    device = device or ZX_Spectrum_48K.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            ZX_Spectrum_48K.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function ZX_Spectrum_48K.example()
    print("=== ZX-Spectrum-48K设备示例 ===")
    
    -- 创建设备实例
    local device = ZX_Spectrum_48K.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    ZX_Spectrum_48K.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["A"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("A", 0x55)
        print("写入 A: " .. ZX_Spectrum_48K.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("A")
        print("读取 A: " .. ZX_Spectrum_48K.hex(value))
        
        -- 位操作
        device:set_bit("A", 0, true)
        local bit0 = device:get_bit("A", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    ZX_Spectrum_48K.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("ZX_Spectrum_48K.lua$") then
    ZX_Spectrum_48K.example()
end

return ZX_Spectrum_48K
