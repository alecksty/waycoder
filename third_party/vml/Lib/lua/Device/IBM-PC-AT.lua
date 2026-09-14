--[[
  IBM PC/AT设备定义 - Lua模块
  生成自: IBM/IBM PC/IBM PC/AT
  版本: 
  日期: 
  作者: 
  描述: IBM Personal Computer/Advanced Technology (Model 5170)
  CPU架构: x86-16
  位宽: 0位
  时钟频率: 0 Hz
]]

local IBM PC/AT = {}

-- 设备信息
IBM PC/AT.DEVICE_NAME = "IBM PC/AT"
IBM PC/AT.MANUFACTURER = "IBM"
IBM PC/AT.FAMILY = "IBM PC"
IBM PC/AT.VERSION = ""
IBM PC/AT.ARCHITECTURE = "x86-16"
IBM PC/AT.BITS = 0
IBM PC/AT.CLOCK_FREQUENCY = 0

-- 外设定义
-- Programmable Interrupt Controller
IBM PC/AT._8259A_BASE = 
IBM PC/AT._8259A_ICW1_ADDR = 0x20
IBM PC/AT._8259A_ICW2_ADDR = 0x21
IBM PC/AT._8259A_ICW3_ADDR = 0x21
IBM PC/AT._8259A_ICW4_ADDR = 0x21
IBM PC/AT._8259A_OCW1_ADDR = 0x21
IBM PC/AT._8259A_OCW2_ADDR = 0x20
IBM PC/AT._8259A_OCW3_ADDR = 0x20
-- Programmable Interval Timer
IBM PC/AT._8253_BASE = 
IBM PC/AT._8253_COUNTER0_ADDR = 0x40
IBM PC/AT._8253_COUNTER1_ADDR = 0x41
IBM PC/AT._8253_COUNTER2_ADDR = 0x42
IBM PC/AT._8253_CONTROL_ADDR = 0x43
-- Direct Memory Access Controller
IBM PC/AT._8237_BASE = 
IBM PC/AT._8237_CHANNEL0_ADDR = 0x00
IBM PC/AT._8237_CHANNEL1_ADDR = 0x02
IBM PC/AT._8237_CHANNEL2_ADDR = 0x04
IBM PC/AT._8237_CHANNEL3_ADDR = 0x06
IBM PC/AT._8237_STATUS_ADDR = 0x08
IBM PC/AT._8237_COMMAND_ADDR = 0x08
IBM PC/AT._8237_REQUEST_ADDR = 0x09
IBM PC/AT._8237_MASK_ADDR = 0x0A
IBM PC/AT._8237_MODE_ADDR = 0x0B
-- Keyboard Controller
IBM PC/AT._8042_BASE = 
IBM PC/AT._8042_DATA_ADDR = 0x60
IBM PC/AT._8042_STATUS_ADDR = 0x64
-- Real-Time Clock with CMOS RAM
IBM PC/AT.CMOS_BASE = 
IBM PC/AT.CMOS_ADDRESS_ADDR = 0x70
IBM PC/AT.CMOS_DATA_ADDR = 0x71
-- Floppy Disk Controller
IBM PC/AT.FDC_BASE = 
IBM PC/AT.FDC_SRA_ADDR = 0x3F2
IBM PC/AT.FDC_MSR_ADDR = 0x3F4
IBM PC/AT.FDC_DATA_ADDR = 0x3F5
IBM PC/AT.FDC_DIR_ADDR = 0x3F7
IBM PC/AT.FDC_CCR_ADDR = 0x3F7
-- Hard Disk Controller (ST-506/412)
IBM PC/AT.HDC_BASE = 
IBM PC/AT.HDC_DATA_ADDR = 0x1F0
IBM PC/AT.HDC_ERROR_ADDR = 0x1F1
IBM PC/AT.HDC_SECTORCOUNT_ADDR = 0x1F2
IBM PC/AT.HDC_SECTORNUMBER_ADDR = 0x1F3
IBM PC/AT.HDC_CYLINDERLOW_ADDR = 0x1F4
IBM PC/AT.HDC_CYLINDERHIGH_ADDR = 0x1F5
IBM PC/AT.HDC_DRIVEHEAD_ADDR = 0x1F6
IBM PC/AT.HDC_STATUS_ADDR = 0x1F7
IBM PC/AT.HDC_COMMAND_ADDR = 0x1F7
-- Color Graphics Adapter
IBM PC/AT.CGA_BASE = 
IBM PC/AT.CGA_CRTC_INDEX_ADDR = 0x3D4
IBM PC/AT.CGA_CRTC_DATA_ADDR = 0x3D5
IBM PC/AT.CGA_MODECONTROL_ADDR = 0x3D8
IBM PC/AT.CGA_COLORSELECT_ADDR = 0x3D9
IBM PC/AT.CGA_STATUS_ADDR = 0x3DA
-- Enhanced Graphics Adapter
IBM PC/AT.EGA_BASE = 
IBM PC/AT.EGA_CRTC_INDEX_ADDR = 0x3D4
IBM PC/AT.EGA_CRTC_DATA_ADDR = 0x3D5
IBM PC/AT.EGA_FEATURECONTROL_ADDR = 0x3DA
IBM PC/AT.EGA_GRAPHICS1POS_ADDR = 0x3CC
IBM PC/AT.EGA_GRAPHICS2POS_ADDR = 0x3CA
IBM PC/AT.EGA_SEQUENCERINDEX_ADDR = 0x3C4
IBM PC/AT.EGA_SEQUENCERDATA_ADDR = 0x3C5
IBM PC/AT.EGA_GRAPHICSINDEX_ADDR = 0x3CE
IBM PC/AT.EGA_GRAPHICSDATA_ADDR = 0x3CF
IBM PC/AT.EGA_ATTRIBUTEINDEX_ADDR = 0x3C0
IBM PC/AT.EGA_ATTRIBUTEDATA_ADDR = 0x3C1
-- Video Graphics Array
IBM PC/AT.VGA_BASE = 
IBM PC/AT.VGA_CRTC_INDEX_ADDR = 0x3D4
IBM PC/AT.VGA_CRTC_DATA_ADDR = 0x3D5
IBM PC/AT.VGA_INPUTSTATUS1_ADDR = 0x3DA
IBM PC/AT.VGA_FEATURECONTROL_ADDR = 0x3DA
IBM PC/AT.VGA_MISCOUTPUT_ADDR = 0x3C2
IBM PC/AT.VGA_SEQUENCERINDEX_ADDR = 0x3C4
IBM PC/AT.VGA_SEQUENCERDATA_ADDR = 0x3C5
IBM PC/AT.VGA_GRAPHICSINDEX_ADDR = 0x3CE
IBM PC/AT.VGA_GRAPHICSDATA_ADDR = 0x3CF
IBM PC/AT.VGA_ATTRIBUTEINDEX_ADDR = 0x3C0
IBM PC/AT.VGA_ATTRIBUTEDATA_ADDR = 0x3C1
IBM PC/AT.VGA_DACMASK_ADDR = 0x3C6
IBM PC/AT.VGA_DACREADINDEX_ADDR = 0x3C7
IBM PC/AT.VGA_DACWRITEINDEX_ADDR = 0x3C8
IBM PC/AT.VGA_DACDATA_ADDR = 0x3C9
-- Game Port
IBM PC/AT.GAMEPORT_BASE = 
IBM PC/AT.GAMEPORT_DATA_ADDR = 0x201
-- Parallel Printer Port
IBM PC/AT.PARALLELPORT_BASE = 
IBM PC/AT.PARALLELPORT_DATA_ADDR = 0x378
IBM PC/AT.PARALLELPORT_STATUS_ADDR = 0x379
IBM PC/AT.PARALLELPORT_CONTROL_ADDR = 0x37A
-- Serial Communications Port
IBM PC/AT.SERIALPORT_BASE = 
IBM PC/AT.SERIALPORT_DATA_ADDR = 0x3F8
IBM PC/AT.SERIALPORT_IER_ADDR = 0x3F9
IBM PC/AT.SERIALPORT_IIR_ADDR = 0x3FA
IBM PC/AT.SERIALPORT_LCR_ADDR = 0x3FB
IBM PC/AT.SERIALPORT_MCR_ADDR = 0x3FC
IBM PC/AT.SERIALPORT_LSR_ADDR = 0x3FD
IBM PC/AT.SERIALPORT_MSR_ADDR = 0x3FE
IBM PC/AT.SERIALPORT_SCR_ADDR = 0x3FF
-- PC Speaker
IBM PC/AT.SPEAKER_BASE = 
IBM PC/AT.SPEAKER_CONTROL_ADDR = 0x61

-- 中断向量定义
IBM PC/AT.INT_DIVIDE_ERROR = 0  -- Division by zero
IBM PC/AT.INT_SINGLE_STEP = 1  -- Debug single step
IBM PC/AT.INT_NMI = 2  -- Non-maskable interrupt
IBM PC/AT.INT_BREAKPOINT = 3  -- INT 3 instruction
IBM PC/AT.INT_OVERFLOW = 4  -- INTO instruction
IBM PC/AT.INT_PRINT_SCREEN = 5  -- Print screen key
IBM PC/AT.INT_IRQ0 = 8  -- Timer interrupt
IBM PC/AT.INT_IRQ1 = 9  -- Keyboard interrupt
IBM PC/AT.INT_IRQ2 = 10  -- Cascade to IRQ8-15
IBM PC/AT.INT_IRQ3 = 11  -- COM2 interrupt
IBM PC/AT.INT_IRQ4 = 12  -- COM1 interrupt
IBM PC/AT.INT_IRQ5 = 13  -- LPT2 interrupt
IBM PC/AT.INT_IRQ6 = 14  -- Floppy disk interrupt
IBM PC/AT.INT_IRQ7 = 15  -- LPT1 interrupt
IBM PC/AT.INT_IRQ8 = 112  -- Real-time clock interrupt
IBM PC/AT.INT_IRQ9 = 113  -- Redirected IRQ2
IBM PC/AT.INT_IRQ10 = 114  -- Reserved
IBM PC/AT.INT_IRQ11 = 115  -- Reserved
IBM PC/AT.INT_IRQ12 = 116  -- PS/2 mouse interrupt
IBM PC/AT.INT_IRQ13 = 117  -- Coprocessor interrupt
IBM PC/AT.INT_IRQ14 = 118  -- Primary IDE interrupt
IBM PC/AT.INT_IRQ15 = 119  -- Secondary IDE interrupt
IBM PC/AT.INT_VIDEO_SERVICES = 16  -- Video BIOS services
IBM PC/AT.INT_DISK_SERVICES = 19  -- Disk BIOS services
IBM PC/AT.INT_DOS_SERVICES = 21  -- DOS function calls

-- 设备类
function IBM PC/AT.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["8259A"] = {
            base = ,
            type = "InterruptController",
            description = "Programmable Interrupt Controller",
            registers = {}
        }
        
        local p = self.peripherals["8259A"]
        p.registers["ICW1"] = {
            address = 0x20,
            size = 8,
            value = 0
        }
        p.registers["ICW2"] = {
            address = 0x21,
            size = 8,
            value = 0
        }
        p.registers["ICW3"] = {
            address = 0x21,
            size = 8,
            value = 0
        }
        p.registers["ICW4"] = {
            address = 0x21,
            size = 8,
            value = 0
        }
        p.registers["OCW1"] = {
            address = 0x21,
            size = 8,
            value = 0
        }
        p.registers["OCW2"] = {
            address = 0x20,
            size = 8,
            value = 0
        }
        p.registers["OCW3"] = {
            address = 0x20,
            size = 8,
            value = 0
        }
        self.peripherals["8253"] = {
            base = ,
            type = "Timer",
            description = "Programmable Interval Timer",
            registers = {}
        }
        
        local p = self.peripherals["8253"]
        p.registers["Counter0"] = {
            address = 0x40,
            size = 8,
            value = 0
        }
        p.registers["Counter1"] = {
            address = 0x41,
            size = 8,
            value = 0
        }
        p.registers["Counter2"] = {
            address = 0x42,
            size = 8,
            value = 0
        }
        p.registers["Control"] = {
            address = 0x43,
            size = 8,
            value = 0
        }
        self.peripherals["8237"] = {
            base = ,
            type = "DMA",
            description = "Direct Memory Access Controller",
            registers = {}
        }
        
        local p = self.peripherals["8237"]
        p.registers["Channel0"] = {
            address = 0x00,
            size = 16,
            value = 0
        }
        p.registers["Channel1"] = {
            address = 0x02,
            size = 16,
            value = 0
        }
        p.registers["Channel2"] = {
            address = 0x04,
            size = 16,
            value = 0
        }
        p.registers["Channel3"] = {
            address = 0x06,
            size = 16,
            value = 0
        }
        p.registers["Status"] = {
            address = 0x08,
            size = 8,
            value = 0
        }
        p.registers["Command"] = {
            address = 0x08,
            size = 8,
            value = 0
        }
        p.registers["Request"] = {
            address = 0x09,
            size = 8,
            value = 0
        }
        p.registers["Mask"] = {
            address = 0x0A,
            size = 8,
            value = 0
        }
        p.registers["Mode"] = {
            address = 0x0B,
            size = 8,
            value = 0
        }
        self.peripherals["8042"] = {
            base = ,
            type = "KeyboardController",
            description = "Keyboard Controller",
            registers = {}
        }
        
        local p = self.peripherals["8042"]
        p.registers["Data"] = {
            address = 0x60,
            size = 8,
            value = 0
        }
        p.registers["Status"] = {
            address = 0x64,
            size = 8,
            value = 0
        }
        self.peripherals["CMOS"] = {
            base = ,
            type = "RTC",
            description = "Real-Time Clock with CMOS RAM",
            registers = {}
        }
        
        local p = self.peripherals["CMOS"]
        p.registers["Address"] = {
            address = 0x70,
            size = 8,
            value = 0
        }
        p.registers["Data"] = {
            address = 0x71,
            size = 8,
            value = 0
        }
        self.peripherals["FDC"] = {
            base = ,
            type = "Floppy",
            description = "Floppy Disk Controller",
            registers = {}
        }
        
        local p = self.peripherals["FDC"]
        p.registers["SRA"] = {
            address = 0x3F2,
            size = 8,
            value = 0
        }
        p.registers["MSR"] = {
            address = 0x3F4,
            size = 8,
            value = 0
        }
        p.registers["DATA"] = {
            address = 0x3F5,
            size = 8,
            value = 0
        }
        p.registers["DIR"] = {
            address = 0x3F7,
            size = 8,
            value = 0
        }
        p.registers["CCR"] = {
            address = 0x3F7,
            size = 8,
            value = 0
        }
        self.peripherals["HDC"] = {
            base = ,
            type = "HardDisk",
            description = "Hard Disk Controller (ST-506/412)",
            registers = {}
        }
        
        local p = self.peripherals["HDC"]
        p.registers["Data"] = {
            address = 0x1F0,
            size = 16,
            value = 0
        }
        p.registers["Error"] = {
            address = 0x1F1,
            size = 8,
            value = 0
        }
        p.registers["SectorCount"] = {
            address = 0x1F2,
            size = 8,
            value = 0
        }
        p.registers["SectorNumber"] = {
            address = 0x1F3,
            size = 8,
            value = 0
        }
        p.registers["CylinderLow"] = {
            address = 0x1F4,
            size = 8,
            value = 0
        }
        p.registers["CylinderHigh"] = {
            address = 0x1F5,
            size = 8,
            value = 0
        }
        p.registers["DriveHead"] = {
            address = 0x1F6,
            size = 8,
            value = 0
        }
        p.registers["Status"] = {
            address = 0x1F7,
            size = 8,
            value = 0
        }
        p.registers["Command"] = {
            address = 0x1F7,
            size = 8,
            value = 0
        }
        self.peripherals["CGA"] = {
            base = ,
            type = "Video",
            description = "Color Graphics Adapter",
            registers = {}
        }
        
        local p = self.peripherals["CGA"]
        p.registers["CRTC_Index"] = {
            address = 0x3D4,
            size = 8,
            value = 0
        }
        p.registers["CRTC_Data"] = {
            address = 0x3D5,
            size = 8,
            value = 0
        }
        p.registers["ModeControl"] = {
            address = 0x3D8,
            size = 8,
            value = 0
        }
        p.registers["ColorSelect"] = {
            address = 0x3D9,
            size = 8,
            value = 0
        }
        p.registers["Status"] = {
            address = 0x3DA,
            size = 8,
            value = 0
        }
        self.peripherals["EGA"] = {
            base = ,
            type = "Video",
            description = "Enhanced Graphics Adapter",
            registers = {}
        }
        
        local p = self.peripherals["EGA"]
        p.registers["CRTC_Index"] = {
            address = 0x3D4,
            size = 8,
            value = 0
        }
        p.registers["CRTC_Data"] = {
            address = 0x3D5,
            size = 8,
            value = 0
        }
        p.registers["FeatureControl"] = {
            address = 0x3DA,
            size = 8,
            value = 0
        }
        p.registers["Graphics1Pos"] = {
            address = 0x3CC,
            size = 8,
            value = 0
        }
        p.registers["Graphics2Pos"] = {
            address = 0x3CA,
            size = 8,
            value = 0
        }
        p.registers["SequencerIndex"] = {
            address = 0x3C4,
            size = 8,
            value = 0
        }
        p.registers["SequencerData"] = {
            address = 0x3C5,
            size = 8,
            value = 0
        }
        p.registers["GraphicsIndex"] = {
            address = 0x3CE,
            size = 8,
            value = 0
        }
        p.registers["GraphicsData"] = {
            address = 0x3CF,
            size = 8,
            value = 0
        }
        p.registers["AttributeIndex"] = {
            address = 0x3C0,
            size = 8,
            value = 0
        }
        p.registers["AttributeData"] = {
            address = 0x3C1,
            size = 8,
            value = 0
        }
        self.peripherals["VGA"] = {
            base = ,
            type = "Video",
            description = "Video Graphics Array",
            registers = {}
        }
        
        local p = self.peripherals["VGA"]
        p.registers["CRTC_Index"] = {
            address = 0x3D4,
            size = 8,
            value = 0
        }
        p.registers["CRTC_Data"] = {
            address = 0x3D5,
            size = 8,
            value = 0
        }
        p.registers["InputStatus1"] = {
            address = 0x3DA,
            size = 8,
            value = 0
        }
        p.registers["FeatureControl"] = {
            address = 0x3DA,
            size = 8,
            value = 0
        }
        p.registers["MiscOutput"] = {
            address = 0x3C2,
            size = 8,
            value = 0
        }
        p.registers["SequencerIndex"] = {
            address = 0x3C4,
            size = 8,
            value = 0
        }
        p.registers["SequencerData"] = {
            address = 0x3C5,
            size = 8,
            value = 0
        }
        p.registers["GraphicsIndex"] = {
            address = 0x3CE,
            size = 8,
            value = 0
        }
        p.registers["GraphicsData"] = {
            address = 0x3CF,
            size = 8,
            value = 0
        }
        p.registers["AttributeIndex"] = {
            address = 0x3C0,
            size = 8,
            value = 0
        }
        p.registers["AttributeData"] = {
            address = 0x3C1,
            size = 8,
            value = 0
        }
        p.registers["DACMask"] = {
            address = 0x3C6,
            size = 8,
            value = 0
        }
        p.registers["DACReadIndex"] = {
            address = 0x3C7,
            size = 8,
            value = 0
        }
        p.registers["DACWriteIndex"] = {
            address = 0x3C8,
            size = 8,
            value = 0
        }
        p.registers["DACData"] = {
            address = 0x3C9,
            size = 8,
            value = 0
        }
        self.peripherals["GamePort"] = {
            base = ,
            type = "Game",
            description = "Game Port",
            registers = {}
        }
        
        local p = self.peripherals["GamePort"]
        p.registers["Data"] = {
            address = 0x201,
            size = 8,
            value = 0
        }
        self.peripherals["ParallelPort"] = {
            base = ,
            type = "Parallel",
            description = "Parallel Printer Port",
            registers = {}
        }
        
        local p = self.peripherals["ParallelPort"]
        p.registers["Data"] = {
            address = 0x378,
            size = 8,
            value = 0
        }
        p.registers["Status"] = {
            address = 0x379,
            size = 8,
            value = 0
        }
        p.registers["Control"] = {
            address = 0x37A,
            size = 8,
            value = 0
        }
        self.peripherals["SerialPort"] = {
            base = ,
            type = "Serial",
            description = "Serial Communications Port",
            registers = {}
        }
        
        local p = self.peripherals["SerialPort"]
        p.registers["Data"] = {
            address = 0x3F8,
            size = 8,
            value = 0
        }
        p.registers["IER"] = {
            address = 0x3F9,
            size = 8,
            value = 0
        }
        p.registers["IIR"] = {
            address = 0x3FA,
            size = 8,
            value = 0
        }
        p.registers["LCR"] = {
            address = 0x3FB,
            size = 8,
            value = 0
        }
        p.registers["MCR"] = {
            address = 0x3FC,
            size = 8,
            value = 0
        }
        p.registers["LSR"] = {
            address = 0x3FD,
            size = 8,
            value = 0
        }
        p.registers["MSR"] = {
            address = 0x3FE,
            size = 8,
            value = 0
        }
        p.registers["SCR"] = {
            address = 0x3FF,
            size = 8,
            value = 0
        }
        self.peripherals["Speaker"] = {
            base = ,
            type = "Audio",
            description = "PC Speaker",
            registers = {}
        }
        
        local p = self.peripherals["Speaker"]
        p.registers["Control"] = {
            address = 0x61,
            size = 8,
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
            name = IBM PC/AT.DEVICE_NAME,
            manufacturer = IBM PC/AT.MANUFACTURER,
            family = IBM PC/AT.FAMILY,
            version = IBM PC/AT.VERSION,
            architecture = IBM PC/AT.ARCHITECTURE,
            bits = IBM PC/AT.BITS,
            clock_frequency = IBM PC/AT.CLOCK_FREQUENCY
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
        return string.format("IBM PC/AT(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function IBM PC/AT.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function IBM PC/AT.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function IBM PC/AT.print_device_info(device)
    device = device or IBM PC/AT.new()
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

function IBM PC/AT.print_registers(device)
    device = device or IBM PC/AT.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            IBM PC/AT.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function IBM PC/AT.example()
    print("=== IBM PC/AT设备示例 ===")
    
    -- 创建设备实例
    local device = IBM PC/AT.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    IBM PC/AT.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    IBM PC/AT.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("IBM PC/AT.lua$") then
    IBM PC/AT.example()
end

return IBM PC/AT
