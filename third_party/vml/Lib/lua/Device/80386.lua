--[[
  Intel 80386设备定义 - Lua模块
  生成自: Intel/x86/Intel 80386
  版本: 
  日期: 
  作者: 
  描述: Intel 80386 32-bit microprocessor with virtual 8086 mode and paging
  CPU架构: x86-32
  位宽: 0位
  时钟频率: 0 Hz
]]

local Intel 80386 = {}

-- 设备信息
Intel 80386.DEVICE_NAME = "Intel 80386"
Intel 80386.MANUFACTURER = "Intel"
Intel 80386.FAMILY = "x86"
Intel 80386.VERSION = ""
Intel 80386.ARCHITECTURE = "x86-32"
Intel 80386.BITS = 0
Intel 80386.CLOCK_FREQUENCY = 0

-- 外设定义
-- Programmable Interrupt Controller
Intel 80386._8259A_BASE = 
Intel 80386._8259A_ICW1_ADDR = 0x20
Intel 80386._8259A_ICW2_ADDR = 0x21
Intel 80386._8259A_ICW3_ADDR = 0x21
Intel 80386._8259A_ICW4_ADDR = 0x21
Intel 80386._8259A_OCW1_ADDR = 0x21
Intel 80386._8259A_OCW2_ADDR = 0x20
Intel 80386._8259A_OCW3_ADDR = 0x20
-- Programmable Interval Timer
Intel 80386._8253_BASE = 
Intel 80386._8253_COUNTER0_ADDR = 0x40
Intel 80386._8253_COUNTER1_ADDR = 0x41
Intel 80386._8253_COUNTER2_ADDR = 0x42
Intel 80386._8253_CONTROL_ADDR = 0x43
-- Direct Memory Access Controller
Intel 80386._8237_BASE = 
Intel 80386._8237_CHANNEL0_ADDR = 0x00
Intel 80386._8237_CHANNEL1_ADDR = 0x02
Intel 80386._8237_CHANNEL2_ADDR = 0x04
Intel 80386._8237_CHANNEL3_ADDR = 0x06
Intel 80386._8237_STATUS_ADDR = 0x08
Intel 80386._8237_COMMAND_ADDR = 0x08
Intel 80386._8237_REQUEST_ADDR = 0x09
Intel 80386._8237_MASK_ADDR = 0x0A
Intel 80386._8237_MODE_ADDR = 0x0B
Intel 80386._8237_FLIPFLOP_ADDR = 0x0C
Intel 80386._8237_TEMP_ADDR = 0x0D
Intel 80386._8237_MASTERCLEAR_ADDR = 0x0D
Intel 80386._8237_MASKALL_ADDR = 0x0F
-- Keyboard Controller
Intel 80386._8042_BASE = 
Intel 80386._8042_DATA_ADDR = 0x60
Intel 80386._8042_STATUS_ADDR = 0x64
-- Integrated System Peripheral
Intel 80386._82380_BASE = 
Intel 80386._82380_DMA_ADDR = 0x0000
Intel 80386._82380_INTERRUPT_ADDR = 0x0200
Intel 80386._82380_TIMER_ADDR = 0x0400
Intel 80386._82380_DRAM_ADDR = 0x0600
Intel 80386._82380_WAITSTATE_ADDR = 0x0800

-- 中断向量定义
Intel 80386.INT_DIVIDE_ERROR = 0  -- Division by zero or overflow
Intel 80386.INT_DEBUG_EXCEPTION = 1  -- Single-step or debug register access
Intel 80386.INT_NMI = 2  -- Non-maskable interrupt
Intel 80386.INT_BREAKPOINT = 3  -- INT 3 instruction
Intel 80386.INT_OVERFLOW = 4  -- INTO instruction with OF=1
Intel 80386.INT_BOUNDS_CHECK = 5  -- BOUND instruction
Intel 80386.INT_INVALID_OPCODE = 6  -- Undefined opcode
Intel 80386.INT_COPROCESSOR_NOT_AVAILABLE = 7  -- No math coprocessor
Intel 80386.INT_DOUBLE_FAULT = 8  -- Two exceptions in handler
Intel 80386.INT_COPROCESSOR_SEGMENT_OVERRUN = 9  -- Coprocessor operand beyond segment
Intel 80386.INT_INVALID_TSS = 10  -- Invalid Task State Segment
Intel 80386.INT_SEGMENT_NOT_PRESENT = 11  -- Segment not present
Intel 80386.INT_STACK_FAULT = 12  -- Stack segment limit violation
Intel 80386.INT_GENERAL_PROTECTION = 13  -- Memory access violation
Intel 80386.INT_PAGE_FAULT = 14  -- Page not present
Intel 80386.INT_COPROCESSOR_ERROR = 16  -- Math coprocessor error
Intel 80386.INT_ALIGNMENT_CHECK = 17  -- Unaligned memory access
Intel 80386.INT_IRQ0 = 32  -- Timer interrupt
Intel 80386.INT_IRQ1 = 33  -- Keyboard interrupt
Intel 80386.INT_IRQ2 = 34  -- Cascade to IRQ8-15
Intel 80386.INT_IRQ3 = 35  -- COM2 interrupt
Intel 80386.INT_IRQ4 = 36  -- COM1 interrupt
Intel 80386.INT_IRQ5 = 37  -- LPT2 interrupt
Intel 80386.INT_IRQ6 = 38  -- Floppy disk interrupt
Intel 80386.INT_IRQ7 = 39  -- LPT1 interrupt
Intel 80386.INT_IRQ8 = 40  -- Real-time clock interrupt
Intel 80386.INT_IRQ9 = 41  -- Redirected IRQ2
Intel 80386.INT_IRQ10 = 42  -- Reserved
Intel 80386.INT_IRQ11 = 43  -- Reserved
Intel 80386.INT_IRQ12 = 44  -- PS/2 mouse interrupt
Intel 80386.INT_IRQ13 = 45  -- Coprocessor interrupt
Intel 80386.INT_IRQ14 = 46  -- Primary IDE interrupt
Intel 80386.INT_IRQ15 = 47  -- Secondary IDE interrupt

-- 设备类
function Intel 80386.new(memory_base)
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
        p.registers["FlipFlop"] = {
            address = 0x0C,
            size = 8,
            value = 0
        }
        p.registers["Temp"] = {
            address = 0x0D,
            size = 8,
            value = 0
        }
        p.registers["MasterClear"] = {
            address = 0x0D,
            size = 8,
            value = 0
        }
        p.registers["MaskAll"] = {
            address = 0x0F,
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
        self.peripherals["82380"] = {
            base = ,
            type = "SystemController",
            description = "Integrated System Peripheral",
            registers = {}
        }
        
        local p = self.peripherals["82380"]
        p.registers["DMA"] = {
            address = 0x0000,
            size = 256,
            value = 0
        }
        p.registers["Interrupt"] = {
            address = 0x0200,
            size = 256,
            value = 0
        }
        p.registers["Timer"] = {
            address = 0x0400,
            size = 256,
            value = 0
        }
        p.registers["DRAM"] = {
            address = 0x0600,
            size = 256,
            value = 0
        }
        p.registers["WaitState"] = {
            address = 0x0800,
            size = 256,
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
            name = Intel 80386.DEVICE_NAME,
            manufacturer = Intel 80386.MANUFACTURER,
            family = Intel 80386.FAMILY,
            version = Intel 80386.VERSION,
            architecture = Intel 80386.ARCHITECTURE,
            bits = Intel 80386.BITS,
            clock_frequency = Intel 80386.CLOCK_FREQUENCY
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
        return string.format("Intel 80386(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function Intel 80386.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function Intel 80386.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function Intel 80386.print_device_info(device)
    device = device or Intel 80386.new()
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

function Intel 80386.print_registers(device)
    device = device or Intel 80386.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            Intel 80386.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function Intel 80386.example()
    print("=== Intel 80386设备示例 ===")
    
    -- 创建设备实例
    local device = Intel 80386.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    Intel 80386.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    Intel 80386.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("Intel 80386.lua$") then
    Intel 80386.example()
end

return Intel 80386
