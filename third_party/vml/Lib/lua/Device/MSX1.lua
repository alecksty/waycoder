--[[
  MSX1设备定义 - Lua模块
  生成自: Various (ASCII/Awanaga/MSX Association)/MSX/MSX1
  版本: 1.0
  日期: 2026-04-17
  作者: VML Team
  描述: MSX - Standardized 8-bit home computer with Z80A CPU, TMS9918A graphics, and AY-3-8910 audio
  CPU架构: Z80A
  位宽: 8位
  时钟频率: 3579545 Hz
]]

local MSX1 = {}

-- 设备信息
MSX1.DEVICE_NAME = "MSX1"
MSX1.MANUFACTURER = "Various (ASCII/Awanaga/MSX Association)"
MSX1.FAMILY = "MSX"
MSX1.VERSION = "1.0"
MSX1.ARCHITECTURE = "Z80A"
MSX1.BITS = 8
MSX1.CLOCK_FREQUENCY = 3579545

-- 寄存器地址定义
MSX1.A_ADDR = 0x00  -- Accumulator
MSX1.F_ADDR = 0x01  -- Flags
MSX1.F_C_BIT = 0  -- Carry
MSX1.F_N_BIT = 1  -- Subtract
MSX1.F_PV_BIT = 2  -- Parity/Overflow
MSX1.F_H_BIT = 4  -- Half Carry
MSX1.F_Z_BIT = 6  -- Zero
MSX1.F_S_BIT = 7  -- Sign
MSX1.B_ADDR = 0x02  -- B Register
MSX1.C_ADDR = 0x03  -- C Register
MSX1.D_ADDR = 0x04  -- D Register
MSX1.E_ADDR = 0x05  -- E Register
MSX1.H_ADDR = 0x06  -- H Register
MSX1.L_ADDR = 0x07  -- L Register
MSX1.AF_ADDR = 0x08  -- Alternate AF
MSX1.BC_ADDR = 0x0A  -- Alternate BC
MSX1.DE_ADDR = 0x0C  -- Alternate DE
MSX1.HL_ADDR = 0x0E  -- Alternate HL
MSX1.I_ADDR = 0x10  -- Interrupt Vector
MSX1.R_ADDR = 0x11  -- Refresh
MSX1.IX_ADDR = 0x12  -- Index X
MSX1.IY_ADDR = 0x14  -- Index Y (usually = 0xF38F)
MSX1.SP_ADDR = 0x16  -- Stack Pointer
MSX1.PC_ADDR = 0x18  -- Program Counter

-- 内存段定义
MSX1.SLOT0_ROM_START = 0x0000
MSX1.SLOT0_ROM_END = 0x7FFF
MSX1.SLOT0_ROM_SIZE = 32768  -- Cartridge/SUB-ROM / Main-ROM
MSX1.SYSROM_START = 0x0000
MSX1.SYSROM_END = 0x3FFF
MSX1.SYSROM_SIZE = 16384  -- MSX-BIOS ROM
MSX1.EXTROM_START = 0x4000
MSX1.EXTROM_END = 0x7FFF
MSX1.EXTROM_SIZE = 16384  -- Extension ROM (cartridge)
MSX1.MAIN_RAM_START = 0x4000
MSX1.MAIN_RAM_END = 0xC000
MSX1.MAIN_RAM_SIZE = 32768  -- Main RAM (32KB working area)
MSX1.WORK_RAM_START = 0xC000
MSX1.WORK_RAM_END = 0xFFFF
MSX1.WORK_RAM_SIZE = 16384  -- Work RAM (16KB)
MSX1.SYSVAR_START = 0xF000
MSX1.SYSVAR_END = 0xFCA0
MSX1.SYSVAR_SIZE = 3232  -- System variables area
MSX1.SLOTS_START = 0x8000
MSX1.SLOTS_END = 0xFFFF
MSX1.SLOTS_SIZE = 32768  -- Slot-mapped memory

-- 外设定义
-- TMS9918A Video Display Processor
MSX1.VDP_BASE = 0x98
MSX1.VDP_VDP_REG0_ADDR = 0x99
MSX1.VDP_VDP_REG1_ADDR = 0x99
MSX1.VDP_VDP_REG2_ADDR = 0x99
MSX1.VDP_VDP_REG3_ADDR = 0x99
MSX1.VDP_VDP_REG4_ADDR = 0x99
MSX1.VDP_VDP_REG5_ADDR = 0x99
MSX1.VDP_VDP_REG6_ADDR = 0x99
MSX1.VDP_VDP_REG7_ADDR = 0x99
MSX1.VDP_VDP_STATUS_ADDR = 0x99
MSX1.VDP_VDP_DATA_ADDR = 0x98
MSX1.VDP_VDP_POT_ADDR = 0x98
-- AY-3-8910 Programmable Sound Generator
MSX1.PSG_BASE = 0xA0
MSX1.PSG_PSG_REG_ADDR = 0xA1
MSX1.PSG_PSG_DATA_ADDR = 0xA3
MSX1.PSG_FREQ_A_LO_ADDR = 0xA0
MSX1.PSG_FREQ_A_HI_ADDR = 0xA1
MSX1.PSG_FREQ_B_LO_ADDR = 0xA2
MSX1.PSG_FREQ_B_HI_ADDR = 0xA3
MSX1.PSG_FREQ_C_LO_ADDR = 0xA4
MSX1.PSG_FREQ_C_HI_ADDR = 0xA5
MSX1.PSG_NOISE_FREQ_ADDR = 0xA6
MSX1.PSG_ENABLE_ADDR = 0xA7
MSX1.PSG_VOL_A_ADDR = 0xA8
MSX1.PSG_VOL_B_ADDR = 0xA9
MSX1.PSG_VOL_C_ADDR = 0xAA
MSX1.PSG_ENV_FREQ_LO_ADDR = 0xAB
MSX1.PSG_ENV_FREQ_HI_ADDR = 0xAC
MSX1.PSG_ENV_SHAPE_ADDR = 0xAD
MSX1.PSG_PORT_A_ADDR = 0xAE
MSX1.PSG_PORT_B_ADDR = 0xAF
-- PPI 8255 Programmable Peripheral Interface
MSX1.PPI_BASE = 0xA8
MSX1.PPI_PPI_PA_ADDR = 0xA8
MSX1.PPI_PPI_PB_ADDR = 0xA9
MSX1.PPI_PPI_PC_ADDR = 0xAA
MSX1.PPI_PPI_CTRL_ADDR = 0xAB
-- MSX Slot Expansion System
MSX1.SLOTEXP_BASE = 0x0000
MSX1.SLOTEXP_SLOT0_ADDR = 0xFCC0
MSX1.SLOTEXP_SLOT1_ADDR = 0xFCC1
MSX1.SLOTEXP_SLOT2_ADDR = 0xFCC2
MSX1.SLOTEXP_SLOT3_ADDR = 0xFCC3
MSX1.SLOTEXP_EXPTBL0_ADDR = 0xFCC4
MSX1.SLOTEXP_EXPTBL1_ADDR = 0xFCC5
MSX1.SLOTEXP_EXPTBL2_ADDR = 0xFCC6
MSX1.SLOTEXP_EXPTBL3_ADDR = 0xFCC7

-- 中断向量定义
MSX1.INT_RESET = 0  -- Power-on / Reset
MSX1.INT_NMI = 1  -- Non-Maskable Interrupt
MSX1.INT_INT = 2  -- VDP Vertical Interrupt (frame)

-- 引脚定义
MSX1.PIN_VCC = 1  -- +5V Power
MSX1.PIN_GND = 2  -- Ground
MSX1.PIN_CLK = 3  -- Z80 Clock (3.58MHz)
MSX1.PIN_A0_A15 = 4  -- Address Bus
MSX1.PIN_D0_D7 = 5  -- Data Bus
MSX1.PIN_MREQ = 6  -- Memory Request
MSX1.PIN_IORQ = 7  -- I/O Request
MSX1.PIN_RD = 8  -- Read
MSX1.PIN_WR = 9  -- Write
MSX1.PIN_INT = 10  -- Interrupt Request
MSX1.PIN_NMI = 11  -- Non-Maskable Interrupt
MSX1.PIN_RESET = 12  -- Reset
MSX1.PIN_SLTSL = 13  -- Slot select (for memory mapping)
MSX1.PIN_WAIT = 14  -- Wait (for slow I/O)

-- 设备类
function MSX1.new(memory_base)
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
            description = "Flags",
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
            description = "Interrupt Vector",
            value = 0
        }
        self.registers["R"] = {
            address = 0x11,
            size = 1,
            access = "rw",
            description = "Refresh",
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
            description = "Index Y (usually = 0xF38F)",
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
        self.peripherals["VDP"] = {
            base = 0x98,
            type = "video",
            description = "TMS9918A Video Display Processor",
            registers = {}
        }
        
        local p = self.peripherals["VDP"]
        p.registers["VDP_REG0"] = {
            address = 0x99,
            size = 1,
            value = 0
        }
        p.registers["VDP_REG1"] = {
            address = 0x99,
            size = 1,
            value = 0
        }
        p.registers["VDP_REG2"] = {
            address = 0x99,
            size = 1,
            value = 0
        }
        p.registers["VDP_REG3"] = {
            address = 0x99,
            size = 1,
            value = 0
        }
        p.registers["VDP_REG4"] = {
            address = 0x99,
            size = 1,
            value = 0
        }
        p.registers["VDP_REG5"] = {
            address = 0x99,
            size = 1,
            value = 0
        }
        p.registers["VDP_REG6"] = {
            address = 0x99,
            size = 1,
            value = 0
        }
        p.registers["VDP_REG7"] = {
            address = 0x99,
            size = 1,
            value = 0
        }
        p.registers["VDP_STATUS"] = {
            address = 0x99,
            size = 1,
            value = 0
        }
        p.registers["VDP_DATA"] = {
            address = 0x98,
            size = 1,
            value = 0
        }
        p.registers["VDP_POT"] = {
            address = 0x98,
            size = 1,
            value = 0
        }
        self.peripherals["PSG"] = {
            base = 0xA0,
            type = "audio",
            description = "AY-3-8910 Programmable Sound Generator",
            registers = {}
        }
        
        local p = self.peripherals["PSG"]
        p.registers["PSG_REG"] = {
            address = 0xA1,
            size = 1,
            value = 0
        }
        p.registers["PSG_DATA"] = {
            address = 0xA3,
            size = 1,
            value = 0
        }
        p.registers["FREQ_A_LO"] = {
            address = 0xA0,
            size = 1,
            value = 0
        }
        p.registers["FREQ_A_HI"] = {
            address = 0xA1,
            size = 1,
            value = 0
        }
        p.registers["FREQ_B_LO"] = {
            address = 0xA2,
            size = 1,
            value = 0
        }
        p.registers["FREQ_B_HI"] = {
            address = 0xA3,
            size = 1,
            value = 0
        }
        p.registers["FREQ_C_LO"] = {
            address = 0xA4,
            size = 1,
            value = 0
        }
        p.registers["FREQ_C_HI"] = {
            address = 0xA5,
            size = 1,
            value = 0
        }
        p.registers["NOISE_FREQ"] = {
            address = 0xA6,
            size = 1,
            value = 0
        }
        p.registers["ENABLE"] = {
            address = 0xA7,
            size = 1,
            value = 0
        }
        p.registers["VOL_A"] = {
            address = 0xA8,
            size = 1,
            value = 0
        }
        p.registers["VOL_B"] = {
            address = 0xA9,
            size = 1,
            value = 0
        }
        p.registers["VOL_C"] = {
            address = 0xAA,
            size = 1,
            value = 0
        }
        p.registers["ENV_FREQ_LO"] = {
            address = 0xAB,
            size = 1,
            value = 0
        }
        p.registers["ENV_FREQ_HI"] = {
            address = 0xAC,
            size = 1,
            value = 0
        }
        p.registers["ENV_SHAPE"] = {
            address = 0xAD,
            size = 1,
            value = 0
        }
        p.registers["PORT_A"] = {
            address = 0xAE,
            size = 1,
            value = 0
        }
        p.registers["PORT_B"] = {
            address = 0xAF,
            size = 1,
            value = 0
        }
        self.peripherals["PPI"] = {
            base = 0xA8,
            type = "gpio",
            description = "PPI 8255 Programmable Peripheral Interface",
            registers = {}
        }
        
        local p = self.peripherals["PPI"]
        p.registers["PPI_PA"] = {
            address = 0xA8,
            size = 1,
            value = 0
        }
        p.registers["PPI_PB"] = {
            address = 0xA9,
            size = 1,
            value = 0
        }
        p.registers["PPI_PC"] = {
            address = 0xAA,
            size = 1,
            value = 0
        }
        p.registers["PPI_CTRL"] = {
            address = 0xAB,
            size = 1,
            value = 0
        }
        self.peripherals["SLOTEXP"] = {
            base = 0x0000,
            type = "bus",
            description = "MSX Slot Expansion System",
            registers = {}
        }
        
        local p = self.peripherals["SLOTEXP"]
        p.registers["SLOT0"] = {
            address = 0xFCC0,
            size = 1,
            value = 0
        }
        p.registers["SLOT1"] = {
            address = 0xFCC1,
            size = 1,
            value = 0
        }
        p.registers["SLOT2"] = {
            address = 0xFCC2,
            size = 1,
            value = 0
        }
        p.registers["SLOT3"] = {
            address = 0xFCC3,
            size = 1,
            value = 0
        }
        p.registers["EXPTBL0"] = {
            address = 0xFCC4,
            size = 1,
            value = 0
        }
        p.registers["EXPTBL1"] = {
            address = 0xFCC5,
            size = 1,
            value = 0
        }
        p.registers["EXPTBL2"] = {
            address = 0xFCC6,
            size = 1,
            value = 0
        }
        p.registers["EXPTBL3"] = {
            address = 0xFCC7,
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
            name = MSX1.DEVICE_NAME,
            manufacturer = MSX1.MANUFACTURER,
            family = MSX1.FAMILY,
            version = MSX1.VERSION,
            architecture = MSX1.ARCHITECTURE,
            bits = MSX1.BITS,
            clock_frequency = MSX1.CLOCK_FREQUENCY
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
        return string.format("MSX1(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function MSX1.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function MSX1.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function MSX1.print_device_info(device)
    device = device or MSX1.new()
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

function MSX1.print_registers(device)
    device = device or MSX1.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            MSX1.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function MSX1.example()
    print("=== MSX1设备示例 ===")
    
    -- 创建设备实例
    local device = MSX1.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    MSX1.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["A"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("A", 0x55)
        print("写入 A: " .. MSX1.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("A")
        print("读取 A: " .. MSX1.hex(value))
        
        -- 位操作
        device:set_bit("A", 0, true)
        local bit0 = device:get_bit("A", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    MSX1.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("MSX1.lua$") then
    MSX1.example()
end

return MSX1
