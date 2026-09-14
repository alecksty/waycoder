--[[
  Commodore-64设备定义 - Lua模块
  生成自: Commodore International/Commodore 64/Commodore-64
  版本: 1.0
  日期: 2026-04-17
  作者: VML Team
  描述: Commodore 64 home computer with MOS 6510 CPU, 64KB RAM, and SID sound chip
  CPU架构: MOS 6510
  位宽: 8位
  时钟频率: 985248 Hz
]]

local Commodore_64 = {}

-- 设备信息
Commodore_64.DEVICE_NAME = "Commodore-64"
Commodore_64.MANUFACTURER = "Commodore International"
Commodore_64.FAMILY = "Commodore 64"
Commodore_64.VERSION = "1.0"
Commodore_64.ARCHITECTURE = "MOS 6510"
Commodore_64.BITS = 8
Commodore_64.CLOCK_FREQUENCY = 985248

-- 寄存器地址定义
Commodore_64.A_ADDR = 0  -- Accumulator
Commodore_64.X_ADDR = 0  -- Index Register X
Commodore_64.Y_ADDR = 0  -- Index Register Y
Commodore_64.SP_ADDR = 0  -- Stack Pointer
Commodore_64.PC_ADDR = 0  -- Program Counter
Commodore_64.P_ADDR = 0  -- Status Register
Commodore_64.PORT_ADDR = 1  -- I/O Port (6510 specific)

-- 外设定义
-- Video Interface Chip II
Commodore_64.VIC_II_BASE = 
Commodore_64.VIC_II_VIC_CTRL1_ADDR = 0xD011
Commodore_64.VIC_II_VIC_CTRL2_ADDR = 0xD016
Commodore_64.VIC_II_VIC_RASTER_ADDR = 0xD012
Commodore_64.VIC_II_VIC_MEMPTR_ADDR = 0xD018
Commodore_64.VIC_II_VIC_IRQ_ADDR = 0xD019
Commodore_64.VIC_II_VIC_IRQMASK_ADDR = 0xD01A
Commodore_64.VIC_II_VIC_BORDER_ADDR = 0xD020
Commodore_64.VIC_II_VIC_BG0_ADDR = 0xD021
Commodore_64.VIC_II_VIC_BG1_ADDR = 0xD022
Commodore_64.VIC_II_VIC_BG2_ADDR = 0xD023
Commodore_64.VIC_II_VIC_BG3_ADDR = 0xD024
Commodore_64.VIC_II_VIC_SPRITE0_X_ADDR = 0xD000
Commodore_64.VIC_II_VIC_SPRITE0_Y_ADDR = 0xD001
Commodore_64.VIC_II_VIC_SPRITE1_X_ADDR = 0xD002
Commodore_64.VIC_II_VIC_SPRITE1_Y_ADDR = 0xD003
-- Sound Interface Device (6581)
Commodore_64.SID_BASE = 
Commodore_64.SID_SID_VOICE1_FREQ_LO_ADDR = 0xD400
Commodore_64.SID_SID_VOICE1_FREQ_HI_ADDR = 0xD401
Commodore_64.SID_SID_VOICE1_PW_LO_ADDR = 0xD402
Commodore_64.SID_SID_VOICE1_PW_HI_ADDR = 0xD403
Commodore_64.SID_SID_VOICE1_CTRL_ADDR = 0xD404
Commodore_64.SID_SID_VOICE1_AD_ADDR = 0xD405
Commodore_64.SID_SID_VOICE1_SR_ADDR = 0xD406
Commodore_64.SID_SID_VOICE2_FREQ_LO_ADDR = 0xD407
Commodore_64.SID_SID_VOICE2_FREQ_HI_ADDR = 0xD408
Commodore_64.SID_SID_VOICE2_PW_LO_ADDR = 0xD409
Commodore_64.SID_SID_VOICE2_PW_HI_ADDR = 0xD40A
Commodore_64.SID_SID_VOICE2_CTRL_ADDR = 0xD40B
Commodore_64.SID_SID_VOICE2_AD_ADDR = 0xD40C
Commodore_64.SID_SID_VOICE2_SR_ADDR = 0xD40D
Commodore_64.SID_SID_VOICE3_FREQ_LO_ADDR = 0xD40E
Commodore_64.SID_SID_VOICE3_FREQ_HI_ADDR = 0xD40F
Commodore_64.SID_SID_VOICE3_PW_LO_ADDR = 0xD410
Commodore_64.SID_SID_VOICE3_PW_HI_ADDR = 0xD411
Commodore_64.SID_SID_VOICE3_CTRL_ADDR = 0xD412
Commodore_64.SID_SID_VOICE3_AD_ADDR = 0xD413
Commodore_64.SID_SID_VOICE3_SR_ADDR = 0xD414
Commodore_64.SID_SID_FILTER_CUTOFF_LO_ADDR = 0xD415
Commodore_64.SID_SID_FILTER_CUTOFF_HI_ADDR = 0xD416
Commodore_64.SID_SID_FILTER_CTRL_ADDR = 0xD417
Commodore_64.SID_SID_VOLUME_ADDR = 0xD418
Commodore_64.SID_SID_POTX_ADDR = 0xD419
Commodore_64.SID_SID_POTY_ADDR = 0xD41A
Commodore_64.SID_SID_OSC3_ADDR = 0xD41B
Commodore_64.SID_SID_ENV3_ADDR = 0xD41C
-- Complex Interface Adapter 1 (6526)
Commodore_64.CIA1_BASE = 
Commodore_64.CIA1_CIA1_PRA_ADDR = 0xDC00
Commodore_64.CIA1_CIA1_PRB_ADDR = 0xDC01
Commodore_64.CIA1_CIA1_DDRA_ADDR = 0xDC02
Commodore_64.CIA1_CIA1_DDRB_ADDR = 0xDC03
Commodore_64.CIA1_CIA1_TALO_ADDR = 0xDC04
Commodore_64.CIA1_CIA1_TAHI_ADDR = 0xDC05
Commodore_64.CIA1_CIA1_TBLO_ADDR = 0xDC06
Commodore_64.CIA1_CIA1_TBHI_ADDR = 0xDC07
Commodore_64.CIA1_CIA1_TODTEN_ADDR = 0xDC08
Commodore_64.CIA1_CIA1_TODSEC_ADDR = 0xDC09
Commodore_64.CIA1_CIA1_TODMIN_ADDR = 0xDC0A
Commodore_64.CIA1_CIA1_TODHR_ADDR = 0xDC0B
Commodore_64.CIA1_CIA1_SDR_ADDR = 0xDC0C
Commodore_64.CIA1_CIA1_ICR_ADDR = 0xDC0D
Commodore_64.CIA1_CIA1_CRA_ADDR = 0xDC0E
Commodore_64.CIA1_CIA1_CRB_ADDR = 0xDC0F
-- Complex Interface Adapter 2 (6526)
Commodore_64.CIA2_BASE = 
Commodore_64.CIA2_CIA2_PRA_ADDR = 0xDD00
Commodore_64.CIA2_CIA2_PRB_ADDR = 0xDD01
Commodore_64.CIA2_CIA2_DDRA_ADDR = 0xDD02
Commodore_64.CIA2_CIA2_DDRB_ADDR = 0xDD03
Commodore_64.CIA2_CIA2_TALO_ADDR = 0xDD04
Commodore_64.CIA2_CIA2_TAHI_ADDR = 0xDD05
Commodore_64.CIA2_CIA2_TBLO_ADDR = 0xDD06
Commodore_64.CIA2_CIA2_TBHI_ADDR = 0xDD07
Commodore_64.CIA2_CIA2_TODTEN_ADDR = 0xDD08
Commodore_64.CIA2_CIA2_TODSEC_ADDR = 0xDD09
Commodore_64.CIA2_CIA2_TODMIN_ADDR = 0xDD0A
Commodore_64.CIA2_CIA2_TODHR_ADDR = 0xDD0B
Commodore_64.CIA2_CIA2_SDR_ADDR = 0xDD0C
Commodore_64.CIA2_CIA2_ICR_ADDR = 0xDD0D
Commodore_64.CIA2_CIA2_CRA_ADDR = 0xDD0E
Commodore_64.CIA2_CIA2_CRB_ADDR = 0xDD0F

-- 中断向量定义
Commodore_64.INT_IRQ = 65532  -- Maskable Interrupt
Commodore_64.INT_NMI = 65534  -- Non-Maskable Interrupt
Commodore_64.INT_RESET = 65526  -- Reset Vector

-- 设备类
function Commodore_64.new(memory_base)
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
        self.registers["PORT"] = {
            address = 1,
            size = 1,
            access = "rw",
            description = "I/O Port (6510 specific)",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["VIC-II"] = {
            base = ,
            type = "Video",
            description = "Video Interface Chip II",
            registers = {}
        }
        
        local p = self.peripherals["VIC-II"]
        p.registers["VIC_CTRL1"] = {
            address = 0xD011,
            size = 1,
            value = 0
        }
        p.registers["VIC_CTRL2"] = {
            address = 0xD016,
            size = 1,
            value = 0
        }
        p.registers["VIC_RASTER"] = {
            address = 0xD012,
            size = 1,
            value = 0
        }
        p.registers["VIC_MEMPTR"] = {
            address = 0xD018,
            size = 1,
            value = 0
        }
        p.registers["VIC_IRQ"] = {
            address = 0xD019,
            size = 1,
            value = 0
        }
        p.registers["VIC_IRQMASK"] = {
            address = 0xD01A,
            size = 1,
            value = 0
        }
        p.registers["VIC_BORDER"] = {
            address = 0xD020,
            size = 1,
            value = 0
        }
        p.registers["VIC_BG0"] = {
            address = 0xD021,
            size = 1,
            value = 0
        }
        p.registers["VIC_BG1"] = {
            address = 0xD022,
            size = 1,
            value = 0
        }
        p.registers["VIC_BG2"] = {
            address = 0xD023,
            size = 1,
            value = 0
        }
        p.registers["VIC_BG3"] = {
            address = 0xD024,
            size = 1,
            value = 0
        }
        p.registers["VIC_SPRITE0_X"] = {
            address = 0xD000,
            size = 1,
            value = 0
        }
        p.registers["VIC_SPRITE0_Y"] = {
            address = 0xD001,
            size = 1,
            value = 0
        }
        p.registers["VIC_SPRITE1_X"] = {
            address = 0xD002,
            size = 1,
            value = 0
        }
        p.registers["VIC_SPRITE1_Y"] = {
            address = 0xD003,
            size = 1,
            value = 0
        }
        self.peripherals["SID"] = {
            base = ,
            type = "Audio",
            description = "Sound Interface Device (6581)",
            registers = {}
        }
        
        local p = self.peripherals["SID"]
        p.registers["SID_VOICE1_FREQ_LO"] = {
            address = 0xD400,
            size = 1,
            value = 0
        }
        p.registers["SID_VOICE1_FREQ_HI"] = {
            address = 0xD401,
            size = 1,
            value = 0
        }
        p.registers["SID_VOICE1_PW_LO"] = {
            address = 0xD402,
            size = 1,
            value = 0
        }
        p.registers["SID_VOICE1_PW_HI"] = {
            address = 0xD403,
            size = 1,
            value = 0
        }
        p.registers["SID_VOICE1_CTRL"] = {
            address = 0xD404,
            size = 1,
            value = 0
        }
        p.registers["SID_VOICE1_AD"] = {
            address = 0xD405,
            size = 1,
            value = 0
        }
        p.registers["SID_VOICE1_SR"] = {
            address = 0xD406,
            size = 1,
            value = 0
        }
        p.registers["SID_VOICE2_FREQ_LO"] = {
            address = 0xD407,
            size = 1,
            value = 0
        }
        p.registers["SID_VOICE2_FREQ_HI"] = {
            address = 0xD408,
            size = 1,
            value = 0
        }
        p.registers["SID_VOICE2_PW_LO"] = {
            address = 0xD409,
            size = 1,
            value = 0
        }
        p.registers["SID_VOICE2_PW_HI"] = {
            address = 0xD40A,
            size = 1,
            value = 0
        }
        p.registers["SID_VOICE2_CTRL"] = {
            address = 0xD40B,
            size = 1,
            value = 0
        }
        p.registers["SID_VOICE2_AD"] = {
            address = 0xD40C,
            size = 1,
            value = 0
        }
        p.registers["SID_VOICE2_SR"] = {
            address = 0xD40D,
            size = 1,
            value = 0
        }
        p.registers["SID_VOICE3_FREQ_LO"] = {
            address = 0xD40E,
            size = 1,
            value = 0
        }
        p.registers["SID_VOICE3_FREQ_HI"] = {
            address = 0xD40F,
            size = 1,
            value = 0
        }
        p.registers["SID_VOICE3_PW_LO"] = {
            address = 0xD410,
            size = 1,
            value = 0
        }
        p.registers["SID_VOICE3_PW_HI"] = {
            address = 0xD411,
            size = 1,
            value = 0
        }
        p.registers["SID_VOICE3_CTRL"] = {
            address = 0xD412,
            size = 1,
            value = 0
        }
        p.registers["SID_VOICE3_AD"] = {
            address = 0xD413,
            size = 1,
            value = 0
        }
        p.registers["SID_VOICE3_SR"] = {
            address = 0xD414,
            size = 1,
            value = 0
        }
        p.registers["SID_FILTER_CUTOFF_LO"] = {
            address = 0xD415,
            size = 1,
            value = 0
        }
        p.registers["SID_FILTER_CUTOFF_HI"] = {
            address = 0xD416,
            size = 1,
            value = 0
        }
        p.registers["SID_FILTER_CTRL"] = {
            address = 0xD417,
            size = 1,
            value = 0
        }
        p.registers["SID_VOLUME"] = {
            address = 0xD418,
            size = 1,
            value = 0
        }
        p.registers["SID_POTX"] = {
            address = 0xD419,
            size = 1,
            value = 0
        }
        p.registers["SID_POTY"] = {
            address = 0xD41A,
            size = 1,
            value = 0
        }
        p.registers["SID_OSC3"] = {
            address = 0xD41B,
            size = 1,
            value = 0
        }
        p.registers["SID_ENV3"] = {
            address = 0xD41C,
            size = 1,
            value = 0
        }
        self.peripherals["CIA1"] = {
            base = ,
            type = "IO",
            description = "Complex Interface Adapter 1 (6526)",
            registers = {}
        }
        
        local p = self.peripherals["CIA1"]
        p.registers["CIA1_PRA"] = {
            address = 0xDC00,
            size = 1,
            value = 0
        }
        p.registers["CIA1_PRB"] = {
            address = 0xDC01,
            size = 1,
            value = 0
        }
        p.registers["CIA1_DDRA"] = {
            address = 0xDC02,
            size = 1,
            value = 0
        }
        p.registers["CIA1_DDRB"] = {
            address = 0xDC03,
            size = 1,
            value = 0
        }
        p.registers["CIA1_TALO"] = {
            address = 0xDC04,
            size = 1,
            value = 0
        }
        p.registers["CIA1_TAHI"] = {
            address = 0xDC05,
            size = 1,
            value = 0
        }
        p.registers["CIA1_TBLO"] = {
            address = 0xDC06,
            size = 1,
            value = 0
        }
        p.registers["CIA1_TBHI"] = {
            address = 0xDC07,
            size = 1,
            value = 0
        }
        p.registers["CIA1_TODTEN"] = {
            address = 0xDC08,
            size = 1,
            value = 0
        }
        p.registers["CIA1_TODSEC"] = {
            address = 0xDC09,
            size = 1,
            value = 0
        }
        p.registers["CIA1_TODMIN"] = {
            address = 0xDC0A,
            size = 1,
            value = 0
        }
        p.registers["CIA1_TODHR"] = {
            address = 0xDC0B,
            size = 1,
            value = 0
        }
        p.registers["CIA1_SDR"] = {
            address = 0xDC0C,
            size = 1,
            value = 0
        }
        p.registers["CIA1_ICR"] = {
            address = 0xDC0D,
            size = 1,
            value = 0
        }
        p.registers["CIA1_CRA"] = {
            address = 0xDC0E,
            size = 1,
            value = 0
        }
        p.registers["CIA1_CRB"] = {
            address = 0xDC0F,
            size = 1,
            value = 0
        }
        self.peripherals["CIA2"] = {
            base = ,
            type = "IO",
            description = "Complex Interface Adapter 2 (6526)",
            registers = {}
        }
        
        local p = self.peripherals["CIA2"]
        p.registers["CIA2_PRA"] = {
            address = 0xDD00,
            size = 1,
            value = 0
        }
        p.registers["CIA2_PRB"] = {
            address = 0xDD01,
            size = 1,
            value = 0
        }
        p.registers["CIA2_DDRA"] = {
            address = 0xDD02,
            size = 1,
            value = 0
        }
        p.registers["CIA2_DDRB"] = {
            address = 0xDD03,
            size = 1,
            value = 0
        }
        p.registers["CIA2_TALO"] = {
            address = 0xDD04,
            size = 1,
            value = 0
        }
        p.registers["CIA2_TAHI"] = {
            address = 0xDD05,
            size = 1,
            value = 0
        }
        p.registers["CIA2_TBLO"] = {
            address = 0xDD06,
            size = 1,
            value = 0
        }
        p.registers["CIA2_TBHI"] = {
            address = 0xDD07,
            size = 1,
            value = 0
        }
        p.registers["CIA2_TODTEN"] = {
            address = 0xDD08,
            size = 1,
            value = 0
        }
        p.registers["CIA2_TODSEC"] = {
            address = 0xDD09,
            size = 1,
            value = 0
        }
        p.registers["CIA2_TODMIN"] = {
            address = 0xDD0A,
            size = 1,
            value = 0
        }
        p.registers["CIA2_TODHR"] = {
            address = 0xDD0B,
            size = 1,
            value = 0
        }
        p.registers["CIA2_SDR"] = {
            address = 0xDD0C,
            size = 1,
            value = 0
        }
        p.registers["CIA2_ICR"] = {
            address = 0xDD0D,
            size = 1,
            value = 0
        }
        p.registers["CIA2_CRA"] = {
            address = 0xDD0E,
            size = 1,
            value = 0
        }
        p.registers["CIA2_CRB"] = {
            address = 0xDD0F,
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
            name = Commodore_64.DEVICE_NAME,
            manufacturer = Commodore_64.MANUFACTURER,
            family = Commodore_64.FAMILY,
            version = Commodore_64.VERSION,
            architecture = Commodore_64.ARCHITECTURE,
            bits = Commodore_64.BITS,
            clock_frequency = Commodore_64.CLOCK_FREQUENCY
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
        return string.format("Commodore_64(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function Commodore_64.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function Commodore_64.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function Commodore_64.print_device_info(device)
    device = device or Commodore_64.new()
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

function Commodore_64.print_registers(device)
    device = device or Commodore_64.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            Commodore_64.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function Commodore_64.example()
    print("=== Commodore-64设备示例 ===")
    
    -- 创建设备实例
    local device = Commodore_64.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    Commodore_64.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["A"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("A", 0x55)
        print("写入 A: " .. Commodore_64.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("A")
        print("读取 A: " .. Commodore_64.hex(value))
        
        -- 位操作
        device:set_bit("A", 0, true)
        local bit0 = device:get_bit("A", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    Commodore_64.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("Commodore_64.lua$") then
    Commodore_64.example()
end

return Commodore_64
