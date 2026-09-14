--[[
  MPU6050设备定义 - Lua模块
  生成自: InvenSense/TDK/Sensor/MPU6050
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: 6-Axis MEMS Accelerometer and Gyroscope (I2C)
  CPU架构: Sensor
  位宽: 8位
  时钟频率: 400000 Hz
]]

local MPU6050 = {}

-- 设备信息
MPU6050.DEVICE_NAME = "MPU6050"
MPU6050.MANUFACTURER = "InvenSense/TDK"
MPU6050.FAMILY = "Sensor"
MPU6050.VERSION = "1.0"
MPU6050.ARCHITECTURE = "Sensor"
MPU6050.BITS = 8
MPU6050.CLOCK_FREQUENCY = 400000

-- 内存段定义
MPU6050.PACKAGE_START = 0x00
MPU6050.PACKAGE_END = 0x00
MPU6050.PACKAGE_SIZE = 24  -- QFN-24 (4x4x0.9mm)

-- 外设定义
-- MPU6050 IMU (0x68/0x69, 2.375V-3.46V)
MPU6050.MPU6050_BASE = 0x68
MPU6050.MPU6050_SMPLRT_DIV_ADDR = 0x19
MPU6050.MPU6050_CONFIG_ADDR = 0x1A
MPU6050.MPU6050_CONFIG_DLPF_CFG_BIT = 0  -- Digital low-pass filter configuration
MPU6050.MPU6050_GYRO_CONFIG_ADDR = 0x1B
MPU6050.MPU6050_GYRO_CONFIG_FS_SEL_BIT = 3  -- Gyro full scale: 0=±250, 1=±500, 2=±1000, 3=±2000 °/s
MPU6050.MPU6050_ACCEL_CONFIG_ADDR = 0x1C
MPU6050.MPU6050_ACCEL_CONFIG_AFS_SEL_BIT = 3  -- Accel full scale: 0=±2g, 1=±4g, 2=±8g, 3=±16g
MPU6050.MPU6050_ACCEL_XOUT_H_ADDR = 0x3B
MPU6050.MPU6050_ACCEL_XOUT_L_ADDR = 0x3C
MPU6050.MPU6050_ACCEL_YOUT_H_ADDR = 0x3D
MPU6050.MPU6050_ACCEL_YOUT_L_ADDR = 0x3E
MPU6050.MPU6050_ACCEL_ZOUT_H_ADDR = 0x3F
MPU6050.MPU6050_ACCEL_ZOUT_L_ADDR = 0x40
MPU6050.MPU6050_TEMP_OUT_H_ADDR = 0x41
MPU6050.MPU6050_TEMP_OUT_L_ADDR = 0x42
MPU6050.MPU6050_GYRO_XOUT_H_ADDR = 0x43
MPU6050.MPU6050_GYRO_XOUT_L_ADDR = 0x44
MPU6050.MPU6050_GYRO_YOUT_H_ADDR = 0x45
MPU6050.MPU6050_GYRO_YOUT_L_ADDR = 0x46
MPU6050.MPU6050_GYRO_ZOUT_H_ADDR = 0x47
MPU6050.MPU6050_GYRO_ZOUT_L_ADDR = 0x48
MPU6050.MPU6050_PWR_MGMT_1_ADDR = 0x6B
MPU6050.MPU6050_PWR_MGMT_1_DEVICE_RESET_BIT = 7  -- 1=Reset all internal registers
MPU6050.MPU6050_PWR_MGMT_1_SLEEP_BIT = 6  -- 1=Sleep mode
MPU6050.MPU6050_PWR_MGMT_1_CYCLE_BIT = 5  -- 1=Cycle mode
MPU6050.MPU6050_PWR_MGMT_1_CLKSEL_BIT = 0  -- Clock source select
MPU6050.MPU6050_WHO_AM_I_ADDR = 0x75

-- 设备类
function MPU6050.new(memory_base)
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
        self.peripherals["MPU6050"] = {
            base = 0x68,
            type = "I2C",
            description = "MPU6050 IMU (0x68/0x69, 2.375V-3.46V)",
            registers = {}
        }
        
        local p = self.peripherals["MPU6050"]
        p.registers["SMPLRT_DIV"] = {
            address = 0x19,
            size = 1,
            value = 0
        }
        p.registers["CONFIG"] = {
            address = 0x1A,
            size = 1,
            value = 0
        }
        p.registers["GYRO_CONFIG"] = {
            address = 0x1B,
            size = 1,
            value = 0
        }
        p.registers["ACCEL_CONFIG"] = {
            address = 0x1C,
            size = 1,
            value = 0
        }
        p.registers["ACCEL_XOUT_H"] = {
            address = 0x3B,
            size = 1,
            value = 0
        }
        p.registers["ACCEL_XOUT_L"] = {
            address = 0x3C,
            size = 1,
            value = 0
        }
        p.registers["ACCEL_YOUT_H"] = {
            address = 0x3D,
            size = 1,
            value = 0
        }
        p.registers["ACCEL_YOUT_L"] = {
            address = 0x3E,
            size = 1,
            value = 0
        }
        p.registers["ACCEL_ZOUT_H"] = {
            address = 0x3F,
            size = 1,
            value = 0
        }
        p.registers["ACCEL_ZOUT_L"] = {
            address = 0x40,
            size = 1,
            value = 0
        }
        p.registers["TEMP_OUT_H"] = {
            address = 0x41,
            size = 1,
            value = 0
        }
        p.registers["TEMP_OUT_L"] = {
            address = 0x42,
            size = 1,
            value = 0
        }
        p.registers["GYRO_XOUT_H"] = {
            address = 0x43,
            size = 1,
            value = 0
        }
        p.registers["GYRO_XOUT_L"] = {
            address = 0x44,
            size = 1,
            value = 0
        }
        p.registers["GYRO_YOUT_H"] = {
            address = 0x45,
            size = 1,
            value = 0
        }
        p.registers["GYRO_YOUT_L"] = {
            address = 0x46,
            size = 1,
            value = 0
        }
        p.registers["GYRO_ZOUT_H"] = {
            address = 0x47,
            size = 1,
            value = 0
        }
        p.registers["GYRO_ZOUT_L"] = {
            address = 0x48,
            size = 1,
            value = 0
        }
        p.registers["PWR_MGMT_1"] = {
            address = 0x6B,
            size = 1,
            value = 0
        }
        p.registers["WHO_AM_I"] = {
            address = 0x75,
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
            name = MPU6050.DEVICE_NAME,
            manufacturer = MPU6050.MANUFACTURER,
            family = MPU6050.FAMILY,
            version = MPU6050.VERSION,
            architecture = MPU6050.ARCHITECTURE,
            bits = MPU6050.BITS,
            clock_frequency = MPU6050.CLOCK_FREQUENCY
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
        return string.format("MPU6050(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function MPU6050.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function MPU6050.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function MPU6050.print_device_info(device)
    device = device or MPU6050.new()
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

function MPU6050.print_registers(device)
    device = device or MPU6050.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            MPU6050.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function MPU6050.example()
    print("=== MPU6050设备示例 ===")
    
    -- 创建设备实例
    local device = MPU6050.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    MPU6050.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    MPU6050.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("MPU6050.lua$") then
    MPU6050.example()
end

return MPU6050
