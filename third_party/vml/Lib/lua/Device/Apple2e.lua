--[[
  Apple-IIe设备定义 - Lua模块
  生成自: Apple Computer/Apple II/Apple-IIe
  版本: 1.0
  日期: 2026-04-17
  作者: VML Team
  描述: Apple II Enhanced - 8-bit personal computer with MOS 6502 CPU
  CPU架构: MOS-6502
  位宽: 8位
  时钟频率: 1021800 Hz
]]

local Apple_IIe = {}

-- 设备信息
Apple_IIe.DEVICE_NAME = "Apple-IIe"
Apple_IIe.MANUFACTURER = "Apple Computer"
Apple_IIe.FAMILY = "Apple II"
Apple_IIe.VERSION = "1.0"
Apple_IIe.ARCHITECTURE = "MOS-6502"
Apple_IIe.BITS = 8
Apple_IIe.CLOCK_FREQUENCY = 1021800

-- 寄存器地址定义
Apple_IIe.A_ADDR = 0x00  -- Accumulator
Apple_IIe.X_ADDR = 0x01  -- X Index Register
Apple_IIe.Y_ADDR = 0x02  -- Y Index Register
Apple_IIe.SP_ADDR = 0x03  -- Stack Pointer
Apple_IIe.PC_ADDR = 0x04  -- Program Counter
Apple_IIe.P_ADDR = 0x06  -- Processor Status
Apple_IIe.P_C_BIT = 0  -- Carry Flag
Apple_IIe.P_Z_BIT = 1  -- Zero Flag
Apple_IIe.P_I_BIT = 2  -- Interrupt Disable
Apple_IIe.P_D_BIT = 3  -- Decimal Mode
Apple_IIe.P_B_BIT = 4  -- Break Command
Apple_IIe.P_U_BIT = 5  -- Unused
Apple_IIe.P_V_BIT = 6  -- Overflow Flag
Apple_IIe.P_N_BIT = 7  -- Negative Flag

-- 内存段定义
Apple_IIe.MAIN_RAM_START = 0x0000
Apple_IIe.MAIN_RAM_END = 0xBFFF
Apple_IIe.MAIN_RAM_SIZE = 49152  -- Main RAM (48KB base, up to 64KB with slot RAM)
Apple_IIe.TEXT_RAM_START = 0x0400
Apple_IIe.TEXT_RAM_END = 0x07FF
Apple_IIe.TEXT_RAM_SIZE = 1024  -- Text screen buffer (40x24)
Apple_IIe.HIRES_RAM_START = 0x2000
Apple_IIe.HIRES_RAM_END = 0x5FFF
Apple_IIe.HIRES_RAM_SIZE = 16384  -- High-resolution graphics buffer
Apple_IIe.AUX_RAM_START = 0x0400
Apple_IIe.AUX_RAM_END = 0x09FF
Apple_IIe.AUX_RAM_SIZE = 1536  -- 80-column text auxiliary RAM
Apple_IIe.MONITOR_ROM_START = 0xC100
Apple_IIe.MONITOR_ROM_END = 0xCFFF
Apple_IIe.MONITOR_ROM_SIZE = 3840  -- Monitor ROM (applesoft/Integer)
Apple_IIe.BASIC_ROM_START = 0xD000
Apple_IIe.BASIC_ROM_END = 0xFFFF
Apple_IIe.BASIC_ROM_SIZE = 12288  -- Applesoft BASIC ROM
Apple_IIe.SLOT_ROM_START = 0xC100
Apple_IIe.SLOT_ROM_END = 0xC7FF
Apple_IIe.SLOT_ROM_SIZE = 768  -- Expansion Slot ROM
Apple_IIe.MMIO_START = 0xC080
Apple_IIe.MMIO_END = 0xC0FF
Apple_IIe.MMIO_SIZE = 128  -- I/O Select (slot space)

-- 外设定义
-- Versatile Interface Adapter (6522)
Apple_IIe.VIA_BASE = 0xC000
Apple_IIe.VIA_ORB_ADDR = 0xC000
Apple_IIe.VIA_ORA_ADDR = 0xC001
Apple_IIe.VIA_DDRB_ADDR = 0xC002
Apple_IIe.VIA_DDRA_ADDR = 0xC003
Apple_IIe.VIA_T1C_ADDR = 0xC004
Apple_IIe.VIA_T1L_ADDR = 0xC006
Apple_IIe.VIA_T2C_ADDR = 0xC008
Apple_IIe.VIA_SR_ADDR = 0xC00A
Apple_IIe.VIA_ACR_ADDR = 0xC00B
Apple_IIe.VIA_PCR_ADDR = 0xC00C
Apple_IIe.VIA_IFG_ADDR = 0xC00D
Apple_IIe.VIA_IER_ADDR = 0xC00E
Apple_IIe.VIA_ORA_NH_ADDR = 0xC00F
-- Peripheral Interface Adapter (6520)
Apple_IIe.PIA_BASE = 0xC010
Apple_IIe.PIA_PA_ADDR = 0xC010
Apple_IIe.PIA_PB_ADDR = 0xC011
Apple_IIe.PIA_DDRA_ADDR = 0xC012
Apple_IIe.PIA_DDRB_ADDR = 0xC013
Apple_IIe.PIA_CA1_ADDR = 0xC014
Apple_IIe.PIA_CA2_ADDR = 0xC015
Apple_IIe.PIA_CB1_ADDR = 0xC016
Apple_IIe.PIA_CB2_ADDR = 0xC017
-- Keyboard (via PIA)
Apple_IIe.KBD_BASE = 0xC000
Apple_IIe.KBD_KEYDATA_ADDR = 0xC000
Apple_IIe.KBD_KEYSTROBE_ADDR = 0xC010
Apple_IIe.KBD_KBDCTRL_ADDR = 0xC025
Apple_IIe.KBD_KBDERR_ADDR = 0xC026
-- Speaker
Apple_IIe.SPEAKER_BASE = 0xC030
Apple_IIe.SPEAKER_SPKR_ADDR = 0xC030
-- Game I/O Port
Apple_IIe.GAME_PORT_BASE = 0xC050
Apple_IIe.GAME_PORT_GAME_SW0_ADDR = 0xC061
Apple_IIe.GAME_PORT_GAME_SW1_ADDR = 0xC062
Apple_IIe.GAME_PORT_GAME_AN0_ADDR = 0xC064
Apple_IIe.GAME_PORT_GAME_AN1_ADDR = 0xC065
Apple_IIe.GAME_PORT_GAME_AN2_ADDR = 0xC066
Apple_IIe.GAME_PORT_GAME_AN3_ADDR = 0xC067
Apple_IIe.GAME_PORT_GAME_TRIG_ADDR = 0xC070
-- Disk II Controller
Apple_IIe.DISKII_BASE = 0xC0E0
Apple_IIe.DISKII_PHASE0_ADDR = 0xC0E0
Apple_IIe.DISKII_PHASE1_ADDR = 0xC0E1
Apple_IIe.DISKII_PHASE2_ADDR = 0xC0E2
Apple_IIe.DISKII_PHASE3_ADDR = 0xC0E3
Apple_IIe.DISKII_Q6L_ADDR = 0xC0EC
Apple_IIe.DISKII_Q7L_ADDR = 0xC0ED
Apple_IIe.DISKII_Q6R_ADDR = 0xC0EE
Apple_IIe.DISKII_Q7R_ADDR = 0xC0EF
-- Video Display Generator
Apple_IIe.VIDEO_BASE = 0xC050
Apple_IIe.VIDEO_TXTCLR_ADDR = 0xC050
Apple_IIe.VIDEO_MIXCLR_ADDR = 0xC051
Apple_IIe.VIDEO_TXTPAGE2_ADDR = 0xC054
Apple_IIe.VIDEO_TXTPAGE1_ADDR = 0xC055
Apple_IIe.VIDEO_LORES_ADDR = 0xC056
Apple_IIe.VIDEO_HIRES_ADDR = 0xC057
Apple_IIe.VIDEO_DHIRESON_ADDR = 0xC05E
Apple_IIe.VIDEO_AN0_ADDR = 0xC058
Apple_IIe.VIDEO_AN1_ADDR = 0xC059
Apple_IIe.VIDEO_AN2_ADDR = 0xC05A
Apple_IIe.VIDEO_AN3_ADDR = 0xC05B
Apple_IIe.VIDEO__80STORE_ADDR = 0xC000
-- RAM Read/Write Control
Apple_IIe.RAMRD_BASE = 0xC080
Apple_IIe.RAMRD_INTCXROM_ADDR = 0xCFFF

-- 中断向量定义
Apple_IIe.INT_RESET = 0  -- Power-on Reset
Apple_IIe.INT_NMI = 1  -- Non-Maskable Interrupt (from VIA)
Apple_IIe.INT_IRQ = 2  -- IRQ from VIA/timer/slot
Apple_IIe.INT_BRK = 3  -- BRK Instruction

-- 引脚定义
Apple_IIe.PIN_VCC = 1  -- +5V Power
Apple_IIe.PIN_GND = 2  -- Ground
Apple_IIe.PIN_RESET = 3  -- System Reset
Apple_IIe.PIN_CLK = 4  -- System Clock (1.023MHz NTSC)
Apple_IIe.PIN_RDY = 5  -- CPU Ready
Apple_IIe.PIN_NMI = 6  -- Non-Maskable Interrupt
Apple_IIe.PIN_IRQ = 7  -- Interrupt Request
Apple_IIe.PIN_SO = 8  -- Set Overflow
Apple_IIe.PIN_RWB = 9  -- Read/Write Bar
Apple_IIe.PIN_SYNC = 10  -- Instruction Sync
Apple_IIe.PIN_A0_A15 = 11  -- Address Bus (16-bit)
Apple_IIe.PIN_D0_D7 = 12  -- Data Bus (8-bit)
Apple_IIe.PIN_PHASE0 = 13  -- Phase 0 (4MHz system)
Apple_IIe.PIN_PHASE1 = 14  -- Phase 1
Apple_IIe.PIN_PHASE2 = 15  -- Phase 2
Apple_IIe.PIN_PHASE3 = 16  -- Phase 3

-- 设备类
function Apple_IIe.new(memory_base)
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
        self.registers["X"] = {
            address = 0x01,
            size = 1,
            access = "rw",
            description = "X Index Register",
            value = 0
        }
        self.registers["Y"] = {
            address = 0x02,
            size = 1,
            access = "rw",
            description = "Y Index Register",
            value = 0
        }
        self.registers["SP"] = {
            address = 0x03,
            size = 1,
            access = "rw",
            description = "Stack Pointer",
            value = 0
        }
        self.registers["PC"] = {
            address = 0x04,
            size = 2,
            access = "rw",
            description = "Program Counter",
            value = 0
        }
        self.registers["P"] = {
            address = 0x06,
            size = 1,
            access = "rw",
            description = "Processor Status",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["VIA"] = {
            base = 0xC000,
            type = "timer",
            description = "Versatile Interface Adapter (6522)",
            registers = {}
        }
        
        local p = self.peripherals["VIA"]
        p.registers["ORB"] = {
            address = 0xC000,
            size = 1,
            value = 0
        }
        p.registers["ORA"] = {
            address = 0xC001,
            size = 1,
            value = 0
        }
        p.registers["DDRB"] = {
            address = 0xC002,
            size = 1,
            value = 0
        }
        p.registers["DDRA"] = {
            address = 0xC003,
            size = 1,
            value = 0
        }
        p.registers["T1C"] = {
            address = 0xC004,
            size = 2,
            value = 0
        }
        p.registers["T1L"] = {
            address = 0xC006,
            size = 2,
            value = 0
        }
        p.registers["T2C"] = {
            address = 0xC008,
            size = 2,
            value = 0
        }
        p.registers["SR"] = {
            address = 0xC00A,
            size = 1,
            value = 0
        }
        p.registers["ACR"] = {
            address = 0xC00B,
            size = 1,
            value = 0
        }
        p.registers["PCR"] = {
            address = 0xC00C,
            size = 1,
            value = 0
        }
        p.registers["IFG"] = {
            address = 0xC00D,
            size = 1,
            value = 0
        }
        p.registers["IER"] = {
            address = 0xC00E,
            size = 1,
            value = 0
        }
        p.registers["ORA_NH"] = {
            address = 0xC00F,
            size = 1,
            value = 0
        }
        self.peripherals["PIA"] = {
            base = 0xC010,
            type = "gpio",
            description = "Peripheral Interface Adapter (6520)",
            registers = {}
        }
        
        local p = self.peripherals["PIA"]
        p.registers["PA"] = {
            address = 0xC010,
            size = 1,
            value = 0
        }
        p.registers["PB"] = {
            address = 0xC011,
            size = 1,
            value = 0
        }
        p.registers["DDRA"] = {
            address = 0xC012,
            size = 1,
            value = 0
        }
        p.registers["DDRB"] = {
            address = 0xC013,
            size = 1,
            value = 0
        }
        p.registers["CA1"] = {
            address = 0xC014,
            size = 1,
            value = 0
        }
        p.registers["CA2"] = {
            address = 0xC015,
            size = 1,
            value = 0
        }
        p.registers["CB1"] = {
            address = 0xC016,
            size = 1,
            value = 0
        }
        p.registers["CB2"] = {
            address = 0xC017,
            size = 1,
            value = 0
        }
        self.peripherals["KBD"] = {
            base = 0xC000,
            type = "input",
            description = "Keyboard (via PIA)",
            registers = {}
        }
        
        local p = self.peripherals["KBD"]
        p.registers["KEYDATA"] = {
            address = 0xC000,
            size = 1,
            value = 0
        }
        p.registers["KEYSTROBE"] = {
            address = 0xC010,
            size = 1,
            value = 0
        }
        p.registers["KBDCTRL"] = {
            address = 0xC025,
            size = 1,
            value = 0
        }
        p.registers["KBDERR"] = {
            address = 0xC026,
            size = 1,
            value = 0
        }
        self.peripherals["SPEAKER"] = {
            base = 0xC030,
            type = "audio",
            description = "Speaker",
            registers = {}
        }
        
        local p = self.peripherals["SPEAKER"]
        p.registers["SPKR"] = {
            address = 0xC030,
            size = 1,
            value = 0
        }
        self.peripherals["GAME_PORT"] = {
            base = 0xC050,
            type = "input",
            description = "Game I/O Port",
            registers = {}
        }
        
        local p = self.peripherals["GAME_PORT"]
        p.registers["GAME_SW0"] = {
            address = 0xC061,
            size = 1,
            value = 0
        }
        p.registers["GAME_SW1"] = {
            address = 0xC062,
            size = 1,
            value = 0
        }
        p.registers["GAME_AN0"] = {
            address = 0xC064,
            size = 1,
            value = 0
        }
        p.registers["GAME_AN1"] = {
            address = 0xC065,
            size = 1,
            value = 0
        }
        p.registers["GAME_AN2"] = {
            address = 0xC066,
            size = 1,
            value = 0
        }
        p.registers["GAME_AN3"] = {
            address = 0xC067,
            size = 1,
            value = 0
        }
        p.registers["GAME_TRIG"] = {
            address = 0xC070,
            size = 1,
            value = 0
        }
        self.peripherals["DISKII"] = {
            base = 0xC0E0,
            type = "storage",
            description = "Disk II Controller",
            registers = {}
        }
        
        local p = self.peripherals["DISKII"]
        p.registers["PHASE0"] = {
            address = 0xC0E0,
            size = 1,
            value = 0
        }
        p.registers["PHASE1"] = {
            address = 0xC0E1,
            size = 1,
            value = 0
        }
        p.registers["PHASE2"] = {
            address = 0xC0E2,
            size = 1,
            value = 0
        }
        p.registers["PHASE3"] = {
            address = 0xC0E3,
            size = 1,
            value = 0
        }
        p.registers["Q6L"] = {
            address = 0xC0EC,
            size = 1,
            value = 0
        }
        p.registers["Q7L"] = {
            address = 0xC0ED,
            size = 1,
            value = 0
        }
        p.registers["Q6R"] = {
            address = 0xC0EE,
            size = 1,
            value = 0
        }
        p.registers["Q7R"] = {
            address = 0xC0EF,
            size = 1,
            value = 0
        }
        self.peripherals["VIDEO"] = {
            base = 0xC050,
            type = "video",
            description = "Video Display Generator",
            registers = {}
        }
        
        local p = self.peripherals["VIDEO"]
        p.registers["TXTCLR"] = {
            address = 0xC050,
            size = 1,
            value = 0
        }
        p.registers["MIXCLR"] = {
            address = 0xC051,
            size = 1,
            value = 0
        }
        p.registers["TXTPAGE2"] = {
            address = 0xC054,
            size = 1,
            value = 0
        }
        p.registers["TXTPAGE1"] = {
            address = 0xC055,
            size = 1,
            value = 0
        }
        p.registers["LORES"] = {
            address = 0xC056,
            size = 1,
            value = 0
        }
        p.registers["HIRES"] = {
            address = 0xC057,
            size = 1,
            value = 0
        }
        p.registers["DHIRESON"] = {
            address = 0xC05E,
            size = 1,
            value = 0
        }
        p.registers["AN0"] = {
            address = 0xC058,
            size = 1,
            value = 0
        }
        p.registers["AN1"] = {
            address = 0xC059,
            size = 1,
            value = 0
        }
        p.registers["AN2"] = {
            address = 0xC05A,
            size = 1,
            value = 0
        }
        p.registers["AN3"] = {
            address = 0xC05B,
            size = 1,
            value = 0
        }
        p.registers["80STORE"] = {
            address = 0xC000,
            size = 1,
            value = 0
        }
        self.peripherals["RAMRD"] = {
            base = 0xC080,
            type = "memory",
            description = "RAM Read/Write Control",
            registers = {}
        }
        
        local p = self.peripherals["RAMRD"]
        p.registers["INTCXROM"] = {
            address = 0xCFFF,
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
            name = Apple_IIe.DEVICE_NAME,
            manufacturer = Apple_IIe.MANUFACTURER,
            family = Apple_IIe.FAMILY,
            version = Apple_IIe.VERSION,
            architecture = Apple_IIe.ARCHITECTURE,
            bits = Apple_IIe.BITS,
            clock_frequency = Apple_IIe.CLOCK_FREQUENCY
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
        return string.format("Apple_IIe(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function Apple_IIe.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function Apple_IIe.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function Apple_IIe.print_device_info(device)
    device = device or Apple_IIe.new()
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

function Apple_IIe.print_registers(device)
    device = device or Apple_IIe.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            Apple_IIe.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function Apple_IIe.example()
    print("=== Apple-IIe设备示例 ===")
    
    -- 创建设备实例
    local device = Apple_IIe.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    Apple_IIe.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["A"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("A", 0x55)
        print("写入 A: " .. Apple_IIe.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("A")
        print("读取 A: " .. Apple_IIe.hex(value))
        
        -- 位操作
        device:set_bit("A", 0, true)
        local bit0 = device:get_bit("A", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    Apple_IIe.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("Apple_IIe.lua$") then
    Apple_IIe.example()
end

return Apple_IIe
