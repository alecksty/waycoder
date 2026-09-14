--[[
  NRF24L01设备定义 - Lua模块
  生成自: Nordic/RF/NRF24L01
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: nRF24L01+ 2.4GHz RF Transceiver (SPI, 2Mbps, 125-channel, 6-pipe)
  CPU架构: RF
  位宽: 8位
  时钟频率: 10000000 Hz
]]

local NRF24L01 = {}

-- 设备信息
NRF24L01.DEVICE_NAME = "NRF24L01"
NRF24L01.MANUFACTURER = "Nordic"
NRF24L01.FAMILY = "RF"
NRF24L01.VERSION = "1.0"
NRF24L01.ARCHITECTURE = "RF"
NRF24L01.BITS = 8
NRF24L01.CLOCK_FREQUENCY = 10000000

-- 外设定义
-- nRF24L01+ 2.4GHz Transceiver (SPI, 1.9V-3.6V)
NRF24L01.NRF24L01_BASE = 0x00
NRF24L01.NRF24L01_CONFIG_ADDR = 0x00
NRF24L01.NRF24L01_CONFIG_PWR_UP_BIT = 1  -- Power up (1=on)
NRF24L01.NRF24L01_CONFIG_PRIM_RX_BIT = 0  -- Primary RX mode (1=RX, 0=TX)
NRF24L01.NRF24L01_EN_AA_ADDR = 0x01
NRF24L01.NRF24L01_EN_RXADDR_ADDR = 0x02
NRF24L01.NRF24L01_SETUP_AW_ADDR = 0x03
NRF24L01.NRF24L01_SETUP_RETR_ADDR = 0x04
NRF24L01.NRF24L01_RF_CH_ADDR = 0x05
NRF24L01.NRF24L01_RF_SETUP_ADDR = 0x06
NRF24L01.NRF24L01_RF_SETUP_RF_PWR_BIT = 1  -- TX power: 00=-18dBm,01=-12dBm,10=-6dBm,11=0dBm
NRF24L01.NRF24L01_RF_SETUP_RF_DR_BIT = 3  -- Data rate: 0=1Mbps,1=2Mbps
NRF24L01.NRF24L01_STATUS_ADDR = 0x07
NRF24L01.NRF24L01_RX_PW_P0_ADDR = 0x11
NRF24L01.NRF24L01_FIFO_STATUS_ADDR = 0x17
NRF24L01.NRF24L01_TX_PAYLOAD_ADDR = 0xA0
NRF24L01.NRF24L01_RX_PAYLOAD_ADDR = 0x61

-- 设备类
function NRF24L01.new(memory_base)
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
        self.peripherals["NRF24L01"] = {
            base = 0x00,
            type = "SPI",
            description = "nRF24L01+ 2.4GHz Transceiver (SPI, 1.9V-3.6V)",
            registers = {}
        }
        
        local p = self.peripherals["NRF24L01"]
        p.registers["CONFIG"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["EN_AA"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["EN_RXADDR"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        p.registers["SETUP_AW"] = {
            address = 0x03,
            size = 1,
            value = 0
        }
        p.registers["SETUP_RETR"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        p.registers["RF_CH"] = {
            address = 0x05,
            size = 1,
            value = 0
        }
        p.registers["RF_SETUP"] = {
            address = 0x06,
            size = 1,
            value = 0
        }
        p.registers["STATUS"] = {
            address = 0x07,
            size = 1,
            value = 0
        }
        p.registers["RX_PW_P0"] = {
            address = 0x11,
            size = 1,
            value = 0
        }
        p.registers["FIFO_STATUS"] = {
            address = 0x17,
            size = 1,
            value = 0
        }
        p.registers["TX_PAYLOAD"] = {
            address = 0xA0,
            size = 32,
            value = 0
        }
        p.registers["RX_PAYLOAD"] = {
            address = 0x61,
            size = 32,
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
            name = NRF24L01.DEVICE_NAME,
            manufacturer = NRF24L01.MANUFACTURER,
            family = NRF24L01.FAMILY,
            version = NRF24L01.VERSION,
            architecture = NRF24L01.ARCHITECTURE,
            bits = NRF24L01.BITS,
            clock_frequency = NRF24L01.CLOCK_FREQUENCY
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
        return string.format("NRF24L01(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function NRF24L01.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function NRF24L01.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function NRF24L01.print_device_info(device)
    device = device or NRF24L01.new()
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

function NRF24L01.print_registers(device)
    device = device or NRF24L01.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            NRF24L01.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function NRF24L01.example()
    print("=== NRF24L01设备示例 ===")
    
    -- 创建设备实例
    local device = NRF24L01.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    NRF24L01.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    NRF24L01.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("NRF24L01.lua$") then
    NRF24L01.example()
end

return NRF24L01
