--[[
  ESP8266设备定义 - Lua模块
  生成自: Espressif Systems/ESP8266/ESP8266
  版本: 
  日期: 
  作者: 
  描述: Espressif ESP8266 Wi-Fi SoC with integrated TCP/IP stack
  CPU架构: Xtensa LX106
  位宽: 0位
  时钟频率: 0 Hz
]]

local ESP8266 = {}

-- 设备信息
ESP8266.DEVICE_NAME = "ESP8266"
ESP8266.MANUFACTURER = "Espressif Systems"
ESP8266.FAMILY = "ESP8266"
ESP8266.VERSION = ""
ESP8266.ARCHITECTURE = "Xtensa LX106"
ESP8266.BITS = 0
ESP8266.CLOCK_FREQUENCY = 0

-- 外设定义
-- Wi-Fi 802.11 b/g/n
ESP8266.WIFI_BASE = 
ESP8266.WIFI_WIFI_MAC_ADDR = 0x60000800
ESP8266.WIFI_WIFI_MODE_ADDR = 0x60000804
ESP8266.WIFI_WIFI_CHANNEL_ADDR = 0x60000808
ESP8266.WIFI_WIFI_RATE_ADDR = 0x6000080C
-- Universal Asynchronous Receiver/Transmitter 0
ESP8266.UART0_BASE = 
ESP8266.UART0_UART0_FIFO_ADDR = 0x60000000
ESP8266.UART0_UART0_INT_RAW_ADDR = 0x60000004
ESP8266.UART0_UART0_INT_ST_ADDR = 0x60000008
ESP8266.UART0_UART0_INT_ENA_ADDR = 0x6000000C
ESP8266.UART0_UART0_INT_CLR_ADDR = 0x60000010
ESP8266.UART0_UART0_CLKDIV_ADDR = 0x60000014
ESP8266.UART0_UART0_AUTOBAUD_ADDR = 0x60000018
ESP8266.UART0_UART0_STATUS_ADDR = 0x6000001C
ESP8266.UART0_UART0_CONF0_ADDR = 0x60000020
ESP8266.UART0_UART0_CONF1_ADDR = 0x60000024
ESP8266.UART0_UART0_LOWPULSE_ADDR = 0x60000028
ESP8266.UART0_UART0_HIGHPULSE_ADDR = 0x6000002C
ESP8266.UART0_UART0_RXD_CNT_ADDR = 0x60000030
-- Serial Peripheral Interface
ESP8266.SPI_BASE = 
ESP8266.SPI_SPI_CMD_ADDR = 0x60000200
ESP8266.SPI_SPI_ADDR_ADDR = 0x60000204
ESP8266.SPI_SPI_CTRL_ADDR = 0x60000208
ESP8266.SPI_SPI_RD_STATUS_ADDR = 0x6000020C
ESP8266.SPI_SPI_CTRL2_ADDR = 0x60000210
ESP8266.SPI_SPI_CLOCK_ADDR = 0x60000214
ESP8266.SPI_SPI_USER_ADDR = 0x60000218
ESP8266.SPI_SPI_USER1_ADDR = 0x6000021C
ESP8266.SPI_SPI_USER2_ADDR = 0x60000220
ESP8266.SPI_SPI_W0_ADDR = 0x60000280
-- Inter-Integrated Circuit
ESP8266.I2C_BASE = 
ESP8266.I2C_I2C_SCL_LOW_ADDR = 0x60000C00
ESP8266.I2C_I2C_SCL_HIGH_ADDR = 0x60000C04
ESP8266.I2C_I2C_SDA_HOLD_ADDR = 0x60000C08
ESP8266.I2C_I2C_SCL_START_HOLD_ADDR = 0x60000C0C
ESP8266.I2C_I2C_SCL_STOP_HOLD_ADDR = 0x60000C10
ESP8266.I2C_I2C_INT_RAW_ADDR = 0x60000C14
ESP8266.I2C_I2C_INT_ST_ADDR = 0x60000C18
ESP8266.I2C_I2C_INT_ENA_ADDR = 0x60000C1C
ESP8266.I2C_I2C_INT_CLR_ADDR = 0x60000C20
ESP8266.I2C_I2C_CMD_ADDR = 0x60000C24
ESP8266.I2C_I2C_FIFO_DATA_ADDR = 0x60000C28
ESP8266.I2C_I2C_FIFO_CNT_ADDR = 0x60000C2C
-- General Purpose I/O
ESP8266.GPIO_BASE = 
ESP8266.GPIO_GPIO_OUT_ADDR = 0x60000300
ESP8266.GPIO_GPIO_OUT_W1TS_ADDR = 0x60000304
ESP8266.GPIO_GPIO_OUT_W1TC_ADDR = 0x60000308
ESP8266.GPIO_GPIO_ENABLE_ADDR = 0x6000030C
ESP8266.GPIO_GPIO_ENABLE_W1TS_ADDR = 0x60000310
ESP8266.GPIO_GPIO_ENABLE_W1TC_ADDR = 0x60000314
ESP8266.GPIO_GPIO_IN_ADDR = 0x60000318
ESP8266.GPIO_GPIO_STATUS_ADDR = 0x6000031C
ESP8266.GPIO_GPIO_STATUS_W1TS_ADDR = 0x60000320
ESP8266.GPIO_GPIO_STATUS_W1TC_ADDR = 0x60000324
ESP8266.GPIO_GPIO_PIN_ADDR = 0x60000328
-- Hardware Timer
ESP8266.TIMER_BASE = 
ESP8266.TIMER_TIMER_LOAD_ADDR = 0x60000600
ESP8266.TIMER_TIMER_COUNT_ADDR = 0x60000604
ESP8266.TIMER_TIMER_CTRL_ADDR = 0x60000608
ESP8266.TIMER_TIMER_INT_ADDR = 0x6000060C
ESP8266.TIMER_TIMER_ALARM_ADDR = 0x60000610
-- Analog-to-Digital Converter
ESP8266.ADC_BASE = 
ESP8266.ADC_ADC_CTRL_ADDR = 0x60000E00
ESP8266.ADC_ADC_DATA_ADDR = 0x60000E04
-- Pulse Width Modulation
ESP8266.PWM_BASE = 
ESP8266.PWM_PWM_CTRL_ADDR = 0x60000F00
ESP8266.PWM_PWM_PERIOD_ADDR = 0x60000F04
ESP8266.PWM_PWM_DUTY_ADDR = 0x60000F08

-- 中断向量定义
ESP8266.INT_NMI = 1  -- Non-maskable interrupt
ESP8266.INT_LEVEL1 = 3  -- Level 1 interrupt
ESP8266.INT_LEVEL2 = 4  -- Level 2 interrupt
ESP8266.INT_LEVEL3 = 5  -- Level 3 interrupt
ESP8266.INT_LEVEL4 = 6  -- Level 4 interrupt
ESP8266.INT_LEVEL5 = 7  -- Level 5 interrupt
ESP8266.INT_TIMER0 = 8  -- Timer 0 interrupt
ESP8266.INT_TIMER1 = 9  -- Timer 1 interrupt
ESP8266.INT_UART0 = 10  -- UART0 interrupt
ESP8266.INT_UART1 = 11  -- UART1 interrupt
ESP8266.INT_GPIO = 12  -- GPIO interrupt
ESP8266.INT_PWM = 13  -- PWM interrupt
ESP8266.INT_I2C = 14  -- I2C interrupt
ESP8266.INT_SPI = 15  -- SPI interrupt
ESP8266.INT_ADC = 16  -- ADC interrupt
ESP8266.INT_WIFI = 17  -- Wi-Fi interrupt
ESP8266.INT_RTC = 18  -- RTC interrupt

-- 设备类
function ESP8266.new(memory_base)
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
        self.peripherals["WiFi"] = {
            base = ,
            type = "Wireless",
            description = "Wi-Fi 802.11 b/g/n",
            registers = {}
        }
        
        local p = self.peripherals["WiFi"]
        p.registers["WIFI_MAC"] = {
            address = 0x60000800,
            size = 32,
            value = 0
        }
        p.registers["WIFI_MODE"] = {
            address = 0x60000804,
            size = 32,
            value = 0
        }
        p.registers["WIFI_CHANNEL"] = {
            address = 0x60000808,
            size = 32,
            value = 0
        }
        p.registers["WIFI_RATE"] = {
            address = 0x6000080C,
            size = 32,
            value = 0
        }
        self.peripherals["UART0"] = {
            base = ,
            type = "UART",
            description = "Universal Asynchronous Receiver/Transmitter 0",
            registers = {}
        }
        
        local p = self.peripherals["UART0"]
        p.registers["UART0_FIFO"] = {
            address = 0x60000000,
            size = 32,
            value = 0
        }
        p.registers["UART0_INT_RAW"] = {
            address = 0x60000004,
            size = 32,
            value = 0
        }
        p.registers["UART0_INT_ST"] = {
            address = 0x60000008,
            size = 32,
            value = 0
        }
        p.registers["UART0_INT_ENA"] = {
            address = 0x6000000C,
            size = 32,
            value = 0
        }
        p.registers["UART0_INT_CLR"] = {
            address = 0x60000010,
            size = 32,
            value = 0
        }
        p.registers["UART0_CLKDIV"] = {
            address = 0x60000014,
            size = 32,
            value = 0
        }
        p.registers["UART0_AUTOBAUD"] = {
            address = 0x60000018,
            size = 32,
            value = 0
        }
        p.registers["UART0_STATUS"] = {
            address = 0x6000001C,
            size = 32,
            value = 0
        }
        p.registers["UART0_CONF0"] = {
            address = 0x60000020,
            size = 32,
            value = 0
        }
        p.registers["UART0_CONF1"] = {
            address = 0x60000024,
            size = 32,
            value = 0
        }
        p.registers["UART0_LOWPULSE"] = {
            address = 0x60000028,
            size = 32,
            value = 0
        }
        p.registers["UART0_HIGHPULSE"] = {
            address = 0x6000002C,
            size = 32,
            value = 0
        }
        p.registers["UART0_RXD_CNT"] = {
            address = 0x60000030,
            size = 32,
            value = 0
        }
        self.peripherals["SPI"] = {
            base = ,
            type = "SPI",
            description = "Serial Peripheral Interface",
            registers = {}
        }
        
        local p = self.peripherals["SPI"]
        p.registers["SPI_CMD"] = {
            address = 0x60000200,
            size = 32,
            value = 0
        }
        p.registers["SPI_ADDR"] = {
            address = 0x60000204,
            size = 32,
            value = 0
        }
        p.registers["SPI_CTRL"] = {
            address = 0x60000208,
            size = 32,
            value = 0
        }
        p.registers["SPI_RD_STATUS"] = {
            address = 0x6000020C,
            size = 32,
            value = 0
        }
        p.registers["SPI_CTRL2"] = {
            address = 0x60000210,
            size = 32,
            value = 0
        }
        p.registers["SPI_CLOCK"] = {
            address = 0x60000214,
            size = 32,
            value = 0
        }
        p.registers["SPI_USER"] = {
            address = 0x60000218,
            size = 32,
            value = 0
        }
        p.registers["SPI_USER1"] = {
            address = 0x6000021C,
            size = 32,
            value = 0
        }
        p.registers["SPI_USER2"] = {
            address = 0x60000220,
            size = 32,
            value = 0
        }
        p.registers["SPI_W0"] = {
            address = 0x60000280,
            size = 32,
            value = 0
        }
        self.peripherals["I2C"] = {
            base = ,
            type = "I2C",
            description = "Inter-Integrated Circuit",
            registers = {}
        }
        
        local p = self.peripherals["I2C"]
        p.registers["I2C_SCL_LOW"] = {
            address = 0x60000C00,
            size = 32,
            value = 0
        }
        p.registers["I2C_SCL_HIGH"] = {
            address = 0x60000C04,
            size = 32,
            value = 0
        }
        p.registers["I2C_SDA_HOLD"] = {
            address = 0x60000C08,
            size = 32,
            value = 0
        }
        p.registers["I2C_SCL_START_HOLD"] = {
            address = 0x60000C0C,
            size = 32,
            value = 0
        }
        p.registers["I2C_SCL_STOP_HOLD"] = {
            address = 0x60000C10,
            size = 32,
            value = 0
        }
        p.registers["I2C_INT_RAW"] = {
            address = 0x60000C14,
            size = 32,
            value = 0
        }
        p.registers["I2C_INT_ST"] = {
            address = 0x60000C18,
            size = 32,
            value = 0
        }
        p.registers["I2C_INT_ENA"] = {
            address = 0x60000C1C,
            size = 32,
            value = 0
        }
        p.registers["I2C_INT_CLR"] = {
            address = 0x60000C20,
            size = 32,
            value = 0
        }
        p.registers["I2C_CMD"] = {
            address = 0x60000C24,
            size = 32,
            value = 0
        }
        p.registers["I2C_FIFO_DATA"] = {
            address = 0x60000C28,
            size = 32,
            value = 0
        }
        p.registers["I2C_FIFO_CNT"] = {
            address = 0x60000C2C,
            size = 32,
            value = 0
        }
        self.peripherals["GPIO"] = {
            base = ,
            type = "GPIO",
            description = "General Purpose I/O",
            registers = {}
        }
        
        local p = self.peripherals["GPIO"]
        p.registers["GPIO_OUT"] = {
            address = 0x60000300,
            size = 32,
            value = 0
        }
        p.registers["GPIO_OUT_W1TS"] = {
            address = 0x60000304,
            size = 32,
            value = 0
        }
        p.registers["GPIO_OUT_W1TC"] = {
            address = 0x60000308,
            size = 32,
            value = 0
        }
        p.registers["GPIO_ENABLE"] = {
            address = 0x6000030C,
            size = 32,
            value = 0
        }
        p.registers["GPIO_ENABLE_W1TS"] = {
            address = 0x60000310,
            size = 32,
            value = 0
        }
        p.registers["GPIO_ENABLE_W1TC"] = {
            address = 0x60000314,
            size = 32,
            value = 0
        }
        p.registers["GPIO_IN"] = {
            address = 0x60000318,
            size = 32,
            value = 0
        }
        p.registers["GPIO_STATUS"] = {
            address = 0x6000031C,
            size = 32,
            value = 0
        }
        p.registers["GPIO_STATUS_W1TS"] = {
            address = 0x60000320,
            size = 32,
            value = 0
        }
        p.registers["GPIO_STATUS_W1TC"] = {
            address = 0x60000324,
            size = 32,
            value = 0
        }
        p.registers["GPIO_PIN"] = {
            address = 0x60000328,
            size = 32,
            value = 0
        }
        self.peripherals["Timer"] = {
            base = ,
            type = "Timer",
            description = "Hardware Timer",
            registers = {}
        }
        
        local p = self.peripherals["Timer"]
        p.registers["TIMER_LOAD"] = {
            address = 0x60000600,
            size = 32,
            value = 0
        }
        p.registers["TIMER_COUNT"] = {
            address = 0x60000604,
            size = 32,
            value = 0
        }
        p.registers["TIMER_CTRL"] = {
            address = 0x60000608,
            size = 32,
            value = 0
        }
        p.registers["TIMER_INT"] = {
            address = 0x6000060C,
            size = 32,
            value = 0
        }
        p.registers["TIMER_ALARM"] = {
            address = 0x60000610,
            size = 32,
            value = 0
        }
        self.peripherals["ADC"] = {
            base = ,
            type = "ADC",
            description = "Analog-to-Digital Converter",
            registers = {}
        }
        
        local p = self.peripherals["ADC"]
        p.registers["ADC_CTRL"] = {
            address = 0x60000E00,
            size = 32,
            value = 0
        }
        p.registers["ADC_DATA"] = {
            address = 0x60000E04,
            size = 32,
            value = 0
        }
        self.peripherals["PWM"] = {
            base = ,
            type = "PWM",
            description = "Pulse Width Modulation",
            registers = {}
        }
        
        local p = self.peripherals["PWM"]
        p.registers["PWM_CTRL"] = {
            address = 0x60000F00,
            size = 32,
            value = 0
        }
        p.registers["PWM_PERIOD"] = {
            address = 0x60000F04,
            size = 32,
            value = 0
        }
        p.registers["PWM_DUTY"] = {
            address = 0x60000F08,
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
            name = ESP8266.DEVICE_NAME,
            manufacturer = ESP8266.MANUFACTURER,
            family = ESP8266.FAMILY,
            version = ESP8266.VERSION,
            architecture = ESP8266.ARCHITECTURE,
            bits = ESP8266.BITS,
            clock_frequency = ESP8266.CLOCK_FREQUENCY
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
        return string.format("ESP8266(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function ESP8266.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function ESP8266.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function ESP8266.print_device_info(device)
    device = device or ESP8266.new()
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

function ESP8266.print_registers(device)
    device = device or ESP8266.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            ESP8266.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function ESP8266.example()
    print("=== ESP8266设备示例 ===")
    
    -- 创建设备实例
    local device = ESP8266.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    ESP8266.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    ESP8266.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("ESP8266.lua$") then
    ESP8266.example()
end

return ESP8266
