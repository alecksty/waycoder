--[[
  MFRC522设备定义 - Lua模块
  生成自: NXP/RFID/MFRC522
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: MFRC522 13.56MHz RFID/NFC Reader (SPI, ISO 14443A, MIFARE)
  CPU架构: RFID
  位宽: 8位
  时钟频率: 10000000 Hz
]]

local MFRC522 = {}

-- 设备信息
MFRC522.DEVICE_NAME = "MFRC522"
MFRC522.MANUFACTURER = "NXP"
MFRC522.FAMILY = "RFID"
MFRC522.VERSION = "1.0"
MFRC522.ARCHITECTURE = "RFID"
MFRC522.BITS = 8
MFRC522.CLOCK_FREQUENCY = 10000000

-- 内存段定义
MFRC522.FIFO_START = 0x00
MFRC522.FIFO_END = 0x3F
MFRC522.FIFO_SIZE = 64  -- 64-byte FIFO buffer

-- 外设定义
-- MFRC522 NFC Reader (SPI, 3.3V, 13.56MHz)
MFRC522.MFRC522_BASE = 0x00
MFRC522.MFRC522_CMD_ADDR = 0x01
MFRC522.MFRC522_COM_IRQ_ADDR = 0x04
MFRC522.MFRC522_COM_IRQ_TX_IRQ_BIT = 6  -- Transmitter interrupt
MFRC522.MFRC522_COM_IRQ_RX_IRQ_BIT = 5  -- Receiver interrupt
MFRC522.MFRC522_COM_IRQ_IDLE_IRQ_BIT = 4  -- Idle interrupt
MFRC522.MFRC522_COM_IRQ_TIMER_IRQ_BIT = 0  -- Timer interrupt
MFRC522.MFRC522_COM_IRQ_EN_ADDR = 0x05
MFRC522.MFRC522_ERROR_ADDR = 0x06
MFRC522.MFRC522_STATUS2_ADDR = 0x08
MFRC522.MFRC522_FIFO_DATA_ADDR = 0x09
MFRC522.MFRC522_FIFO_LEVEL_ADDR = 0x0A
MFRC522.MFRC522_TX_CTRL_ADDR = 0x14
MFRC522.MFRC522_TX_ASK_ADDR = 0x15
MFRC522.MFRC522_MODE_ADDR = 0x11
MFRC522.MFRC522_VERSION_ADDR = 0x37

-- 设备类
function MFRC522.new(memory_base)
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
        self.peripherals["MFRC522"] = {
            base = 0x00,
            type = "SPI",
            description = "MFRC522 NFC Reader (SPI, 3.3V, 13.56MHz)",
            registers = {}
        }
        
        local p = self.peripherals["MFRC522"]
        p.registers["CMD"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["COM_IRQ"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        p.registers["COM_IRQ_EN"] = {
            address = 0x05,
            size = 1,
            value = 0
        }
        p.registers["ERROR"] = {
            address = 0x06,
            size = 1,
            value = 0
        }
        p.registers["STATUS2"] = {
            address = 0x08,
            size = 1,
            value = 0
        }
        p.registers["FIFO_DATA"] = {
            address = 0x09,
            size = 1,
            value = 0
        }
        p.registers["FIFO_LEVEL"] = {
            address = 0x0A,
            size = 1,
            value = 0
        }
        p.registers["TX_CTRL"] = {
            address = 0x14,
            size = 1,
            value = 0
        }
        p.registers["TX_ASK"] = {
            address = 0x15,
            size = 1,
            value = 0
        }
        p.registers["MODE"] = {
            address = 0x11,
            size = 1,
            value = 0
        }
        p.registers["VERSION"] = {
            address = 0x37,
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
            name = MFRC522.DEVICE_NAME,
            manufacturer = MFRC522.MANUFACTURER,
            family = MFRC522.FAMILY,
            version = MFRC522.VERSION,
            architecture = MFRC522.ARCHITECTURE,
            bits = MFRC522.BITS,
            clock_frequency = MFRC522.CLOCK_FREQUENCY
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
        return string.format("MFRC522(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function MFRC522.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function MFRC522.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function MFRC522.print_device_info(device)
    device = device or MFRC522.new()
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

function MFRC522.print_registers(device)
    device = device or MFRC522.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            MFRC522.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function MFRC522.example()
    print("=== MFRC522设备示例 ===")
    
    -- 创建设备实例
    local device = MFRC522.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    MFRC522.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    MFRC522.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("MFRC522.lua$") then
    MFRC522.example()
end

return MFRC522
