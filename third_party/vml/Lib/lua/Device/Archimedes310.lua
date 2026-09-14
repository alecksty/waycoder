--[[
  Acorn-Archimedes-A310设备定义 - Lua模块
  生成自: Acorn Computers/Archimedes/Acorn-Archimedes-A310
  版本: 1.0
  日期: 2026-04-17
  作者: VML Team
  描述: Acorn Archimedes A310 - First ARM-based home computer with RISC OS, ARM250 @ 26MHz
  CPU架构: ARM250
  位宽: 32位
  时钟频率: 26000000 Hz
]]

local Acorn_Archimedes_A310 = {}

-- 设备信息
Acorn_Archimedes_A310.DEVICE_NAME = "Acorn-Archimedes-A310"
Acorn_Archimedes_A310.MANUFACTURER = "Acorn Computers"
Acorn_Archimedes_A310.FAMILY = "Archimedes"
Acorn_Archimedes_A310.VERSION = "1.0"
Acorn_Archimedes_A310.ARCHITECTURE = "ARM250"
Acorn_Archimedes_A310.BITS = 32
Acorn_Archimedes_A310.CLOCK_FREQUENCY = 26000000

-- 寄存器地址定义
Acorn_Archimedes_A310.R0_ADDR = 0x00  -- General Purpose Register 0
Acorn_Archimedes_A310.R1_ADDR = 0x04  -- General Purpose Register 1
Acorn_Archimedes_A310.R2_ADDR = 0x08  -- General Purpose Register 2
Acorn_Archimedes_A310.R3_ADDR = 0x0C  -- General Purpose Register 3
Acorn_Archimedes_A310.R4_ADDR = 0x10  -- General Purpose Register 4
Acorn_Archimedes_A310.R5_ADDR = 0x14  -- General Purpose Register 5
Acorn_Archimedes_A310.R6_ADDR = 0x18  -- General Purpose Register 6
Acorn_Archimedes_A310.R7_ADDR = 0x1C  -- General Purpose Register 7
Acorn_Archimedes_A310.R8_ADDR = 0x20  -- General Purpose Register 8
Acorn_Archimedes_A310.R9_ADDR = 0x24  -- General Purpose Register 9
Acorn_Archimedes_A310.R10_ADDR = 0x28  -- General Purpose Register 10
Acorn_Archimedes_A310.R11_ADDR = 0x2C  -- General Purpose Register 11 (fp)
Acorn_Archimedes_A310.R12_ADDR = 0x30  -- General Purpose Register 12
Acorn_Archimedes_A310.SP_ADDR = 0x34  -- Stack Pointer (R13)
Acorn_Archimedes_A310.LR_ADDR = 0x38  -- Link Register (R14)
Acorn_Archimedes_A310.PC_ADDR = 0x3C  -- Program Counter (R15)
Acorn_Archimedes_A310.PSR_ADDR = 0x40  -- Processor Status Register
Acorn_Archimedes_A310.PSR_MODE_BIT = 0  -- Mode bits (0-4)
Acorn_Archimedes_A310.PSR_T_BIT = 5  -- Thumb state
Acorn_Archimedes_A310.PSR_F_BIT = 6  -- FIQ disable
Acorn_Archimedes_A310.PSR_I_BIT = 7  -- IRQ disable
Acorn_Archimedes_A310.PSR_V_BIT = 28  -- Overflow
Acorn_Archimedes_A310.PSR_C_BIT = 29  -- Carry
Acorn_Archimedes_A310.PSR_Z_BIT = 30  -- Zero
Acorn_Archimedes_A310.PSR_N_BIT = 31  -- Negative

-- 内存段定义
Acorn_Archimedes_A310.ROM_START = 0x00000000
Acorn_Archimedes_A310.ROM_END = 0x0007FFFF
Acorn_Archimedes_A310.ROM_SIZE = 524288  -- RISC OS ROM (512KB)
Acorn_Archimedes_A310.RAM_START = 0x00080000
Acorn_Archimedes_A310.RAM_END = 0x003FFFFF
Acorn_Archimedes_A310.RAM_SIZE = 3932160  -- Main RAM (up to 4MB)
Acorn_Archimedes_A310.VRAM_START = 0x00400000
Acorn_Archimedes_A310.VRAM_END = 0x007FFFFF
Acorn_Archimedes_A310.VRAM_SIZE = 4194304  -- Video RAM (4MB, VIDC)
Acorn_Archimedes_A310.IO_START = 0x03000000
Acorn_Archimedes_A310.IO_END = 0x0301FFFF
Acorn_Archimedes_A310.IO_SIZE = 131072  -- I/O controller (IOC)
Acorn_Archimedes_A310.MEMC_START = 0x03200000
Acorn_Archimedes_A310.MEMC_END = 0x0320FFFF
Acorn_Archimedes_A310.MEMC_SIZE = 4096  -- Memory Controller (MEMC)
Acorn_Archimedes_A310.VIDC_START = 0x03400000
Acorn_Archimedes_A310.VIDC_END = 0x0340FFFF
Acorn_Archimedes_A310.VIDC_SIZE = 4096  -- Video Controller (VIDC)
Acorn_Archimedes_A310.IOMD_START = 0x03300000
Acorn_Archimedes_A310.IOMD_END = 0x0330FFFF
Acorn_Archimedes_A310.IOMD_SIZE = 4096  -- I/O and Memory DMA

-- 外设定义
-- I/O Controller (IOC) - Interrupt/Keyboard/RTC
Acorn_Archimedes_A310.IOC_BASE = 0x03000000
Acorn_Archimedes_A310.IOC_IOC_TIMER1_ADDR = 0x03000000
Acorn_Archimedes_A310.IOC_IOC_TIMER2_ADDR = 0x03000004
Acorn_Archimedes_A310.IOC_IOC_IOSEL_ADDR = 0x03000008
Acorn_Archimedes_A310.IOC_IOC_IRQST_ADDR = 0x0300000C
Acorn_Archimedes_A310.IOC_IOC_IRQLATCH_ADDR = 0x03000010
Acorn_Archimedes_A310.IOC_IOC_FIQST_ADDR = 0x03000014
Acorn_Archimedes_A310.IOC_IOC_FIQEN_ADDR = 0x03000018
Acorn_Archimedes_A310.IOC_IOC_IRQEN_ADDR = 0x0300001C
Acorn_Archimedes_A310.IOC_IOC_KBDDATA_ADDR = 0x03000020
Acorn_Archimedes_A310.IOC_IOC_KBDCR_ADDR = 0x03000024
Acorn_Archimedes_A310.IOC_IOC_RTCDR_ADDR = 0x03000028
Acorn_Archimedes_A310.IOC_IOC_RTCCR_ADDR = 0x0300002C
Acorn_Archimedes_A310.IOC_IOC_PRST_ADDR = 0x03000030
Acorn_Archimedes_A310.IOC_IOC_PORTA_ADDR = 0x03000034
Acorn_Archimedes_A310.IOC_IOC_PORTB_ADDR = 0x03000038
Acorn_Archimedes_A310.IOC_IOC_PORTC_ADDR = 0x0300003C
-- Memory Controller (MEMC1)
Acorn_Archimedes_A310.MEMC_BASE = 0x03200000
Acorn_Archimedes_A310.MEMC_MEMC_PT_ADDR = 0x03200000
Acorn_Archimedes_A310.MEMC_MEMC_CTRL_ADDR = 0x03200004
Acorn_Archimedes_A310.MEMC_MEMC_DRAM_ADDR = 0x03200008
Acorn_Archimedes_A310.MEMC_MEMC_ERR_ADDR = 0x0320000C
-- Video Controller - VIDC1
Acorn_Archimedes_A310.VIDC_BASE = 0x03400000
Acorn_Archimedes_A310.VIDC_VIDC_PALETTE_ADDR = 0x03400000
Acorn_Archimedes_A310.VIDC_VIDC_STARTL_ADDR = 0x03400004
Acorn_Archimedes_A310.VIDC_VIDC_STARTH_ADDR = 0x03400008
Acorn_Archimedes_A310.VIDC_VIDC_CONFIG_ADDR = 0x0340000C
Acorn_Archimedes_A310.VIDC_VIDC_HDISP_ADDR = 0x03400010
Acorn_Archimedes_A310.VIDC_VIDC_VDISP_ADDR = 0x03400014
Acorn_Archimedes_A310.VIDC_VIDC_HSYNC_ADDR = 0x03400018
Acorn_Archimedes_A310.VIDC_VIDC_VSYNC_ADDR = 0x0340001C
Acorn_Archimedes_A310.VIDC_VIDC_BORDER_ADDR = 0x03400020
Acorn_Archimedes_A310.VIDC_VIDC_CURSOR_ADDR = 0x03400024
Acorn_Archimedes_A310.VIDC_VIDC_SOUND_ADDR = 0x03400028
-- Intel 82710 Floppy Disk Controller
Acorn_Archimedes_A310.FDC_BASE = 0x03010000
Acorn_Archimedes_A310.FDC_FDC_STATUS_ADDR = 0x03010000
Acorn_Archimedes_A310.FDC_FDC_COMMAND_ADDR = 0x03010000
Acorn_Archimedes_A310.FDC_FDC_TRACK_ADDR = 0x03010004
Acorn_Archimedes_A310.FDC_FDC_SECTOR_ADDR = 0x03010008
Acorn_Archimedes_A310.FDC_FDC_DATA_ADDR = 0x0301000C
-- Serial Port (via IOC)
Acorn_Archimedes_A310.SERIAL_BASE = 0x03010010
Acorn_Archimedes_A310.SERIAL_SERIAL_TX_ADDR = 0x03010010
Acorn_Archimedes_A310.SERIAL_SERIAL_RX_ADDR = 0x03010014
Acorn_Archimedes_A310.SERIAL_SERIAL_CTRL_ADDR = 0x03010018

-- 中断向量定义
Acorn_Archimedes_A310.INT_RESET = 0  -- Reset
Acorn_Archimedes_A310.INT_UND = 1  -- Undefined instruction
Acorn_Archimedes_A310.INT_SWI = 2  -- Software Interrupt (SWI/SVC)
Acorn_Archimedes_A310.INT_PABORT = 3  -- Prefetch Abort
Acorn_Archimedes_A310.INT_DABORT = 4  -- Data Abort
Acorn_Archimedes_A310.INT_ADDRESS = 5  -- Address Exception
Acorn_Archimedes_A310.INT_IRQ = 6  -- IRQ interrupt (IOC)
Acorn_Archimedes_A310.INT_FIQ = 7  -- FIQ interrupt (VIDC)

-- 引脚定义
Acorn_Archimedes_A310.PIN_VCC = 1  -- +5V Power
Acorn_Archimedes_A310.PIN_GND = 2  -- Ground
Acorn_Archimedes_A310.PIN_CLK = 3  -- ARM clock (26MHz)
Acorn_Archimedes_A310.PIN_NRESET = 4  -- Reset (active low)
Acorn_Archimedes_A310.PIN_NMREQ = 5  -- Memory Request (active low)
Acorn_Archimedes_A310.PIN_NIORQ = 6  -- I/O Request (active low)
Acorn_Archimedes_A310.PIN_NRW = 7  -- Read/Write (0=write, 1=read)
Acorn_Archimedes_A310.PIN_MAS0 = 8  -- Master address bit 0
Acorn_Archimedes_A310.PIN_MAS1 = 9  -- Master address bit 1
Acorn_Archimedes_A310.PIN_MAS2 = 10  -- Master address bit 2
Acorn_Archimedes_A310.PIN_LOCK = 11  -- Bus lock
Acorn_Archimedes_A310.PIN_NMREQ = 12  -- Memory request (active low)
Acorn_Archimedes_A310.PIN_NWAIT = 13  -- Wait state (active low)
Acorn_Archimedes_A310.PIN_NIRQLINE = 14  -- IRQ line (active low)
Acorn_Archimedes_A310.PIN_NFIRQLINE = 15  -- FIQ line (active low)
Acorn_Archimedes_A310.PIN_A1_A25 = 16  -- Address Bus (26-bit)
Acorn_Archimedes_A310.PIN_D0_D31 = 17  -- Data Bus (32-bit)

-- 设备类
function Acorn_Archimedes_A310.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["R0"] = {
            address = 0x00,
            size = 4,
            access = "rw",
            description = "General Purpose Register 0",
            value = 0
        }
        self.registers["R1"] = {
            address = 0x04,
            size = 4,
            access = "rw",
            description = "General Purpose Register 1",
            value = 0
        }
        self.registers["R2"] = {
            address = 0x08,
            size = 4,
            access = "rw",
            description = "General Purpose Register 2",
            value = 0
        }
        self.registers["R3"] = {
            address = 0x0C,
            size = 4,
            access = "rw",
            description = "General Purpose Register 3",
            value = 0
        }
        self.registers["R4"] = {
            address = 0x10,
            size = 4,
            access = "rw",
            description = "General Purpose Register 4",
            value = 0
        }
        self.registers["R5"] = {
            address = 0x14,
            size = 4,
            access = "rw",
            description = "General Purpose Register 5",
            value = 0
        }
        self.registers["R6"] = {
            address = 0x18,
            size = 4,
            access = "rw",
            description = "General Purpose Register 6",
            value = 0
        }
        self.registers["R7"] = {
            address = 0x1C,
            size = 4,
            access = "rw",
            description = "General Purpose Register 7",
            value = 0
        }
        self.registers["R8"] = {
            address = 0x20,
            size = 4,
            access = "rw",
            description = "General Purpose Register 8",
            value = 0
        }
        self.registers["R9"] = {
            address = 0x24,
            size = 4,
            access = "rw",
            description = "General Purpose Register 9",
            value = 0
        }
        self.registers["R10"] = {
            address = 0x28,
            size = 4,
            access = "rw",
            description = "General Purpose Register 10",
            value = 0
        }
        self.registers["R11"] = {
            address = 0x2C,
            size = 4,
            access = "rw",
            description = "General Purpose Register 11 (fp)",
            value = 0
        }
        self.registers["R12"] = {
            address = 0x30,
            size = 4,
            access = "rw",
            description = "General Purpose Register 12",
            value = 0
        }
        self.registers["SP"] = {
            address = 0x34,
            size = 4,
            access = "rw",
            description = "Stack Pointer (R13)",
            value = 0
        }
        self.registers["LR"] = {
            address = 0x38,
            size = 4,
            access = "rw",
            description = "Link Register (R14)",
            value = 0
        }
        self.registers["PC"] = {
            address = 0x3C,
            size = 4,
            access = "rw",
            description = "Program Counter (R15)",
            value = 0
        }
        self.registers["PSR"] = {
            address = 0x40,
            size = 4,
            access = "rw",
            description = "Processor Status Register",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["IOC"] = {
            base = 0x03000000,
            type = "controller",
            description = "I/O Controller (IOC) - Interrupt/Keyboard/RTC",
            registers = {}
        }
        
        local p = self.peripherals["IOC"]
        p.registers["IOC_TIMER1"] = {
            address = 0x03000000,
            size = 4,
            value = 0
        }
        p.registers["IOC_TIMER2"] = {
            address = 0x03000004,
            size = 4,
            value = 0
        }
        p.registers["IOC_IOSEL"] = {
            address = 0x03000008,
            size = 4,
            value = 0
        }
        p.registers["IOC_IRQST"] = {
            address = 0x0300000C,
            size = 4,
            value = 0
        }
        p.registers["IOC_IRQLATCH"] = {
            address = 0x03000010,
            size = 4,
            value = 0
        }
        p.registers["IOC_FIQST"] = {
            address = 0x03000014,
            size = 4,
            value = 0
        }
        p.registers["IOC_FIQEN"] = {
            address = 0x03000018,
            size = 4,
            value = 0
        }
        p.registers["IOC_IRQEN"] = {
            address = 0x0300001C,
            size = 4,
            value = 0
        }
        p.registers["IOC_KBDDATA"] = {
            address = 0x03000020,
            size = 4,
            value = 0
        }
        p.registers["IOC_KBDCR"] = {
            address = 0x03000024,
            size = 4,
            value = 0
        }
        p.registers["IOC_RTCDR"] = {
            address = 0x03000028,
            size = 4,
            value = 0
        }
        p.registers["IOC_RTCCR"] = {
            address = 0x0300002C,
            size = 4,
            value = 0
        }
        p.registers["IOC_PRST"] = {
            address = 0x03000030,
            size = 4,
            value = 0
        }
        p.registers["IOC_PORTA"] = {
            address = 0x03000034,
            size = 4,
            value = 0
        }
        p.registers["IOC_PORTB"] = {
            address = 0x03000038,
            size = 4,
            value = 0
        }
        p.registers["IOC_PORTC"] = {
            address = 0x0300003C,
            size = 4,
            value = 0
        }
        self.peripherals["MEMC"] = {
            base = 0x03200000,
            type = "memory",
            description = "Memory Controller (MEMC1)",
            registers = {}
        }
        
        local p = self.peripherals["MEMC"]
        p.registers["MEMC_PT"] = {
            address = 0x03200000,
            size = 4,
            value = 0
        }
        p.registers["MEMC_CTRL"] = {
            address = 0x03200004,
            size = 4,
            value = 0
        }
        p.registers["MEMC_DRAM"] = {
            address = 0x03200008,
            size = 4,
            value = 0
        }
        p.registers["MEMC_ERR"] = {
            address = 0x0320000C,
            size = 4,
            value = 0
        }
        self.peripherals["VIDC"] = {
            base = 0x03400000,
            type = "video",
            description = "Video Controller - VIDC1",
            registers = {}
        }
        
        local p = self.peripherals["VIDC"]
        p.registers["VIDC_PALETTE"] = {
            address = 0x03400000,
            size = 4,
            value = 0
        }
        p.registers["VIDC_STARTL"] = {
            address = 0x03400004,
            size = 4,
            value = 0
        }
        p.registers["VIDC_STARTH"] = {
            address = 0x03400008,
            size = 4,
            value = 0
        }
        p.registers["VIDC_CONFIG"] = {
            address = 0x0340000C,
            size = 4,
            value = 0
        }
        p.registers["VIDC_HDISP"] = {
            address = 0x03400010,
            size = 4,
            value = 0
        }
        p.registers["VIDC_VDISP"] = {
            address = 0x03400014,
            size = 4,
            value = 0
        }
        p.registers["VIDC_HSYNC"] = {
            address = 0x03400018,
            size = 4,
            value = 0
        }
        p.registers["VIDC_VSYNC"] = {
            address = 0x0340001C,
            size = 4,
            value = 0
        }
        p.registers["VIDC_BORDER"] = {
            address = 0x03400020,
            size = 4,
            value = 0
        }
        p.registers["VIDC_CURSOR"] = {
            address = 0x03400024,
            size = 4,
            value = 0
        }
        p.registers["VIDC_SOUND"] = {
            address = 0x03400028,
            size = 4,
            value = 0
        }
        self.peripherals["FDC"] = {
            base = 0x03010000,
            type = "storage",
            description = "Intel 82710 Floppy Disk Controller",
            registers = {}
        }
        
        local p = self.peripherals["FDC"]
        p.registers["FDC_STATUS"] = {
            address = 0x03010000,
            size = 1,
            value = 0
        }
        p.registers["FDC_COMMAND"] = {
            address = 0x03010000,
            size = 1,
            value = 0
        }
        p.registers["FDC_TRACK"] = {
            address = 0x03010004,
            size = 1,
            value = 0
        }
        p.registers["FDC_SECTOR"] = {
            address = 0x03010008,
            size = 1,
            value = 0
        }
        p.registers["FDC_DATA"] = {
            address = 0x0301000C,
            size = 1,
            value = 0
        }
        self.peripherals["SERIAL"] = {
            base = 0x03010010,
            type = "serial",
            description = "Serial Port (via IOC)",
            registers = {}
        }
        
        local p = self.peripherals["SERIAL"]
        p.registers["SERIAL_TX"] = {
            address = 0x03010010,
            size = 1,
            value = 0
        }
        p.registers["SERIAL_RX"] = {
            address = 0x03010014,
            size = 1,
            value = 0
        }
        p.registers["SERIAL_CTRL"] = {
            address = 0x03010018,
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
            name = Acorn_Archimedes_A310.DEVICE_NAME,
            manufacturer = Acorn_Archimedes_A310.MANUFACTURER,
            family = Acorn_Archimedes_A310.FAMILY,
            version = Acorn_Archimedes_A310.VERSION,
            architecture = Acorn_Archimedes_A310.ARCHITECTURE,
            bits = Acorn_Archimedes_A310.BITS,
            clock_frequency = Acorn_Archimedes_A310.CLOCK_FREQUENCY
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
        return string.format("Acorn_Archimedes_A310(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function Acorn_Archimedes_A310.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function Acorn_Archimedes_A310.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function Acorn_Archimedes_A310.print_device_info(device)
    device = device or Acorn_Archimedes_A310.new()
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

function Acorn_Archimedes_A310.print_registers(device)
    device = device or Acorn_Archimedes_A310.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            Acorn_Archimedes_A310.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function Acorn_Archimedes_A310.example()
    print("=== Acorn-Archimedes-A310设备示例 ===")
    
    -- 创建设备实例
    local device = Acorn_Archimedes_A310.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    Acorn_Archimedes_A310.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["R0"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("R0", 0x55)
        print("写入 R0: " .. Acorn_Archimedes_A310.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("R0")
        print("读取 R0: " .. Acorn_Archimedes_A310.hex(value))
        
        -- 位操作
        device:set_bit("R0", 0, true)
        local bit0 = device:get_bit("R0", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    Acorn_Archimedes_A310.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("Acorn_Archimedes_A310.lua$") then
    Acorn_Archimedes_A310.example()
end

return Acorn_Archimedes_A310
