--[[
  Commodore-64设备定义 - Lua模块
  生成自: Commodore/C64/Commodore-64
  版本: 1.0
  日期: 2026-04-17
  作者: VML Team
  描述: Commodore 64 - Best-selling 8-bit home computer with MOS 6510 CPU, VIC-II graphics, and SID audio
  CPU架构: MOS-6510
  位宽: 8位
  时钟频率: 1022727 Hz
]]

local Commodore_64 = {}

-- 设备信息
Commodore_64.DEVICE_NAME = "Commodore-64"
Commodore_64.MANUFACTURER = "Commodore"
Commodore_64.FAMILY = "C64"
Commodore_64.VERSION = "1.0"
Commodore_64.ARCHITECTURE = "MOS-6510"
Commodore_64.BITS = 8
Commodore_64.CLOCK_FREQUENCY = 1022727

-- 寄存器地址定义
Commodore_64.A_ADDR = 0x00  -- Accumulator
Commodore_64.X_ADDR = 0x01  -- X Index Register
Commodore_64.Y_ADDR = 0x02  -- Y Index Register
Commodore_64.SP_ADDR = 0x03  -- Stack Pointer
Commodore_64.PC_ADDR = 0x04  -- Program Counter
Commodore_64.P_ADDR = 0x06  -- Processor Status
Commodore_64.P_C_BIT = 0  -- Carry Flag
Commodore_64.P_Z_BIT = 1  -- Zero Flag
Commodore_64.P_I_BIT = 2  -- Interrupt Disable
Commodore_64.P_D_BIT = 3  -- Decimal Mode
Commodore_64.P_B_BIT = 4  -- Break Flag
Commodore_64.P_U_BIT = 5  -- Unused
Commodore_64.P_V_BIT = 6  -- Overflow Flag
Commodore_64.P_N_BIT = 7  -- Negative Flag
Commodore_64.PORT_ADDR = 0x00  -- I/O Port (6510 only: DDR + data)

-- 内存段定义
Commodore_64.RAM_START = 0x0000
Commodore_64.RAM_END = 0xFFFF
Commodore_64.RAM_SIZE = 65536  -- 64KB main RAM
Commodore_64.BASIC_ROM_START = 0xA000
Commodore_64.BASIC_ROM_END = 0xBFFF
Commodore_64.BASIC_ROM_SIZE = 8192  -- BASIC interpreter ROM
Commodore_64.KERNAL_ROM_START = 0xE000
Commodore_64.KERNAL_ROM_END = 0xFFFF
Commodore_64.KERNAL_ROM_SIZE = 8192  -- KERNAL operating system ROM
Commodore_64.CHAR_ROM_START = 0xD000
Commodore_64.CHAR_ROM_END = 0xDFFF
Commodore_64.CHAR_ROM_SIZE = 4096  -- Character generator ROM
Commodore_64.IO_RAM_START = 0xD000
Commodore_64.IO_RAM_END = 0xDFFF
Commodore_64.IO_RAM_SIZE = 4096  -- I/O + RAM window (switchable)

-- 外设定义
-- Video Interface Chip II - 6567/6569
Commodore_64.VICII_BASE = 0xD000
Commodore_64.VICII_SP0X_ADDR = 0xD000
Commodore_64.VICII_SP0Y_ADDR = 0xD001
Commodore_64.VICII_SP1X_ADDR = 0xD002
Commodore_64.VICII_SP1Y_ADDR = 0xD003
Commodore_64.VICII_SP2X_ADDR = 0xD004
Commodore_64.VICII_SP2Y_ADDR = 0xD005
Commodore_64.VICII_SP3X_ADDR = 0xD006
Commodore_64.VICII_SP3Y_ADDR = 0xD007
Commodore_64.VICII_SP4X_ADDR = 0xD008
Commodore_64.VICII_SP4Y_ADDR = 0xD009
Commodore_64.VICII_SP5X_ADDR = 0xD00A
Commodore_64.VICII_SP5Y_ADDR = 0xD00B
Commodore_64.VICII_SP6X_ADDR = 0xD00C
Commodore_64.VICII_SP6Y_ADDR = 0xD00D
Commodore_64.VICII_SP7X_ADDR = 0xD00E
Commodore_64.VICII_SP7Y_ADDR = 0xD00F
Commodore_64.VICII_MSIGX_ADDR = 0xD010
Commodore_64.VICII_SCROLY_ADDR = 0xD011
Commodore_64.VICII_SCROLX_ADDR = 0xD016
Commodore_64.VICII_YPSTOP_ADDR = 0xD012
Commodore_64.VICII_LPX_ADDR = 0xD013
Commodore_64.VICII_LPY_ADDR = 0xD014
Commodore_64.VICII_SPENA_ADDR = 0xD015
Commodore_64.VICII_CSPMC_ADDR = 0xD017
Commodore_64.VICII_MM0_ADDR = 0xD018
Commodore_64.VICII_VM01_ADDR = 0xD016
Commodore_64.VICII_VICBAS_ADDR = 0xD018
Commodore_64.VICII_IRQMASK_ADDR = 0xD019
Commodore_64.VICII_IRQST_ADDR = 0xD01A
Commodore_64.VICII_SPBGPR_ADDR = 0xD01B
Commodore_64.VICII_SPMC_ADDR = 0xD01C
Commodore_64.VICII_SP1C_ADDR = 0xD025
Commodore_64.VICII_SP2C_ADDR = 0xD026
Commodore_64.VICII_SPBC_ADDR = 0xD027
Commodore_64.VICII_SP1C0_ADDR = 0xD028
Commodore_64.VICII_SP2C0_ADDR = 0xD029
Commodore_64.VICII_SP3C0_ADDR = 0xD02A
Commodore_64.VICII_SP4C0_ADDR = 0xD02B
Commodore_64.VICII_SP5C0_ADDR = 0xD02C
Commodore_64.VICII_SP6C0_ADDR = 0xD02D
Commodore_64.VICII_SP7C0_ADDR = 0xD02E
Commodore_64.VICII_REG_FD_ADDR = 0xD01D
Commodore_64.VICII_BGCOL0_ADDR = 0xD021
Commodore_64.VICII_BGCOL1_ADDR = 0xD022
Commodore_64.VICII_BGCOL2_ADDR = 0xD023
Commodore_64.VICII_BGCOL3_ADDR = 0xD024
-- Sound Interface Device 6581/8580
Commodore_64.SID_BASE = 0xD400
Commodore_64.SID_FREQ1LO_ADDR = 0xD400
Commodore_64.SID_FREQ1HI_ADDR = 0xD401
Commodore_64.SID_PW1LO_ADDR = 0xD402
Commodore_64.SID_PW1HI_ADDR = 0xD403
Commodore_64.SID_CR1_ADDR = 0xD404
Commodore_64.SID_AD1_ADDR = 0xD405
Commodore_64.SID_SR1_ADDR = 0xD406
Commodore_64.SID_FREQ2LO_ADDR = 0xD407
Commodore_64.SID_FREQ2HI_ADDR = 0xD408
Commodore_64.SID_PW2LO_ADDR = 0xD409
Commodore_64.SID_PW2HI_ADDR = 0xD40A
Commodore_64.SID_CR2_ADDR = 0xD40B
Commodore_64.SID_AD2_ADDR = 0xD40C
Commodore_64.SID_SR2_ADDR = 0xD40D
Commodore_64.SID_FREQ3LO_ADDR = 0xD40E
Commodore_64.SID_FREQ3HI_ADDR = 0xD40F
Commodore_64.SID_PW3LO_ADDR = 0xD410
Commodore_64.SID_PW3HI_ADDR = 0xD411
Commodore_64.SID_CR3_ADDR = 0xD412
Commodore_64.SID_AD3_ADDR = 0xD413
Commodore_64.SID_SR3_ADDR = 0xD414
Commodore_64.SID_FCH_ADDR = 0xD415
Commodore_64.SID_FCL_ADDR = 0xD416
Commodore_64.SID_RES_FLT_ADDR = 0xD417
Commodore_64.SID_VOLUME_ADDR = 0xD418
Commodore_64.SID_POTX_ADDR = 0xD419
Commodore_64.SID_POTY_ADDR = 0xD41A
Commodore_64.SID_OSC3_ADDR = 0xD41B
Commodore_64.SID_ENV3_ADDR = 0xD41C
-- Complex Interface Adapter 1 - Keyboard/Serial
Commodore_64.CIA1_BASE = 0xDC00
Commodore_64.CIA1_PRA_ADDR = 0xDC00
Commodore_64.CIA1_PRB_ADDR = 0xDC01
Commodore_64.CIA1_DDRA_ADDR = 0xDC02
Commodore_64.CIA1_DDRB_ADDR = 0xDC03
Commodore_64.CIA1_TA_LO_ADDR = 0xDC04
Commodore_64.CIA1_TA_HI_ADDR = 0xDC05
Commodore_64.CIA1_TB_LO_ADDR = 0xDC06
Commodore_64.CIA1_TB_HI_ADDR = 0xDC07
Commodore_64.CIA1_TOD_TENTH_ADDR = 0xDC08
Commodore_64.CIA1_TOD_SEC_ADDR = 0xDC09
Commodore_64.CIA1_TOD_MIN_ADDR = 0xDC0A
Commodore_64.CIA1_TOD_HR_ADDR = 0xDC0B
Commodore_64.CIA1_SDR_ADDR = 0xDC0C
Commodore_64.CIA1_ICR_ADDR = 0xDC0D
Commodore_64.CIA1_CRA_ADDR = 0xDC0E
Commodore_64.CIA1_CRB_ADDR = 0xDC0F
-- Complex Interface Adapter 2 - Serial/Bus
Commodore_64.CIA2_BASE = 0xDD00
Commodore_64.CIA2_PRA_ADDR = 0xDD00
Commodore_64.CIA2_PRB_ADDR = 0xDD01
Commodore_64.CIA2_DDRA_ADDR = 0xDD02
Commodore_64.CIA2_DDRB_ADDR = 0xDD03
Commodore_64.CIA2_TA_LO_ADDR = 0xDD04
Commodore_64.CIA2_TA_HI_ADDR = 0xDD05
Commodore_64.CIA2_TB_LO_ADDR = 0xDD06
Commodore_64.CIA2_TB_HI_ADDR = 0xDD07
Commodore_64.CIA2_TOD_TENTH_ADDR = 0xDD08
Commodore_64.CIA2_TOD_SEC_ADDR = 0xDD09
Commodore_64.CIA2_TOD_MIN_ADDR = 0xDD0A
Commodore_64.CIA2_TOD_HR_ADDR = 0xDD0B
Commodore_64.CIA2_SDR_ADDR = 0xDD0C
Commodore_64.CIA2_ICR_ADDR = 0xDD0D
Commodore_64.CIA2_CRA_ADDR = 0xDD0E
Commodore_64.CIA2_CRB_ADDR = 0xDD0F
-- Color RAM (4-bit per char cell)
Commodore_64.COLORRAM_BASE = 0xD800
Commodore_64.COLORRAM_COLOR_ADDR = 0xD800
-- IEC Serial Bus (via CIA1)
Commodore_64.IEC_BASE = 0xDC00
Commodore_64.IEC_IEC_DATA_ADDR = 0xDC00
Commodore_64.IEC_IEC_CLOCK_ADDR = 0xDC01

-- 中断向量定义
Commodore_64.INT_RESET = 0  -- Power-on / Reset
Commodore_64.INT_NMI = 1  -- Non-Maskable Interrupt
Commodore_64.INT_IRQ = 2  -- IRQ (VIC raster / CIA timer)

-- 引脚定义
Commodore_64.PIN_VCC = 1  -- +5V Power
Commodore_64.PIN_GND = 2  -- Ground
Commodore_64.PIN_RESET = 3  -- System Reset
Commodore_64.PIN_CLK = 4  -- System Clock (~1MHz)
Commodore_64.PIN_DOTCLK = 5  -- VIC Dot Clock (8MHz NTSC / 7.8MHz PAL)
Commodore_64.PIN_AEC = 6  -- Address Enable Control (VIC steals cycles)
Commodore_64.PIN_BA = 7  -- Bus Available (from VIC)
Commodore_64.PIN_IRQ = 8  -- Interrupt Request
Commodore_64.PIN_NMI = 9  -- Non-Maskable Interrupt
Commodore_64.PIN_RWB = 10  -- Read/Write
Commodore_64.PIN_A0_A15 = 11  -- Address Bus
Commodore_64.PIN_D0_D7 = 12  -- Data Bus

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
        self.registers["PORT"] = {
            address = 0x00,
            size = 1,
            access = "rw",
            description = "I/O Port (6510 only: DDR + data)",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["VICII"] = {
            base = 0xD000,
            type = "video",
            description = "Video Interface Chip II - 6567/6569",
            registers = {}
        }
        
        local p = self.peripherals["VICII"]
        p.registers["SP0X"] = {
            address = 0xD000,
            size = 1,
            value = 0
        }
        p.registers["SP0Y"] = {
            address = 0xD001,
            size = 1,
            value = 0
        }
        p.registers["SP1X"] = {
            address = 0xD002,
            size = 1,
            value = 0
        }
        p.registers["SP1Y"] = {
            address = 0xD003,
            size = 1,
            value = 0
        }
        p.registers["SP2X"] = {
            address = 0xD004,
            size = 1,
            value = 0
        }
        p.registers["SP2Y"] = {
            address = 0xD005,
            size = 1,
            value = 0
        }
        p.registers["SP3X"] = {
            address = 0xD006,
            size = 1,
            value = 0
        }
        p.registers["SP3Y"] = {
            address = 0xD007,
            size = 1,
            value = 0
        }
        p.registers["SP4X"] = {
            address = 0xD008,
            size = 1,
            value = 0
        }
        p.registers["SP4Y"] = {
            address = 0xD009,
            size = 1,
            value = 0
        }
        p.registers["SP5X"] = {
            address = 0xD00A,
            size = 1,
            value = 0
        }
        p.registers["SP5Y"] = {
            address = 0xD00B,
            size = 1,
            value = 0
        }
        p.registers["SP6X"] = {
            address = 0xD00C,
            size = 1,
            value = 0
        }
        p.registers["SP6Y"] = {
            address = 0xD00D,
            size = 1,
            value = 0
        }
        p.registers["SP7X"] = {
            address = 0xD00E,
            size = 1,
            value = 0
        }
        p.registers["SP7Y"] = {
            address = 0xD00F,
            size = 1,
            value = 0
        }
        p.registers["MSIGX"] = {
            address = 0xD010,
            size = 1,
            value = 0
        }
        p.registers["SCROLY"] = {
            address = 0xD011,
            size = 1,
            value = 0
        }
        p.registers["SCROLX"] = {
            address = 0xD016,
            size = 1,
            value = 0
        }
        p.registers["YPSTOP"] = {
            address = 0xD012,
            size = 1,
            value = 0
        }
        p.registers["LPX"] = {
            address = 0xD013,
            size = 1,
            value = 0
        }
        p.registers["LPY"] = {
            address = 0xD014,
            size = 1,
            value = 0
        }
        p.registers["SPENA"] = {
            address = 0xD015,
            size = 1,
            value = 0
        }
        p.registers["CSPMC"] = {
            address = 0xD017,
            size = 1,
            value = 0
        }
        p.registers["MM0"] = {
            address = 0xD018,
            size = 1,
            value = 0
        }
        p.registers["VM01"] = {
            address = 0xD016,
            size = 1,
            value = 0
        }
        p.registers["VICBAS"] = {
            address = 0xD018,
            size = 1,
            value = 0
        }
        p.registers["IRQMASK"] = {
            address = 0xD019,
            size = 1,
            value = 0
        }
        p.registers["IRQST"] = {
            address = 0xD01A,
            size = 1,
            value = 0
        }
        p.registers["SPBGPR"] = {
            address = 0xD01B,
            size = 1,
            value = 0
        }
        p.registers["SPMC"] = {
            address = 0xD01C,
            size = 1,
            value = 0
        }
        p.registers["SP1C"] = {
            address = 0xD025,
            size = 1,
            value = 0
        }
        p.registers["SP2C"] = {
            address = 0xD026,
            size = 1,
            value = 0
        }
        p.registers["SPBC"] = {
            address = 0xD027,
            size = 1,
            value = 0
        }
        p.registers["SP1C0"] = {
            address = 0xD028,
            size = 1,
            value = 0
        }
        p.registers["SP2C0"] = {
            address = 0xD029,
            size = 1,
            value = 0
        }
        p.registers["SP3C0"] = {
            address = 0xD02A,
            size = 1,
            value = 0
        }
        p.registers["SP4C0"] = {
            address = 0xD02B,
            size = 1,
            value = 0
        }
        p.registers["SP5C0"] = {
            address = 0xD02C,
            size = 1,
            value = 0
        }
        p.registers["SP6C0"] = {
            address = 0xD02D,
            size = 1,
            value = 0
        }
        p.registers["SP7C0"] = {
            address = 0xD02E,
            size = 1,
            value = 0
        }
        p.registers["REG_FD"] = {
            address = 0xD01D,
            size = 1,
            value = 0
        }
        p.registers["BGCOL0"] = {
            address = 0xD021,
            size = 1,
            value = 0
        }
        p.registers["BGCOL1"] = {
            address = 0xD022,
            size = 1,
            value = 0
        }
        p.registers["BGCOL2"] = {
            address = 0xD023,
            size = 1,
            value = 0
        }
        p.registers["BGCOL3"] = {
            address = 0xD024,
            size = 1,
            value = 0
        }
        self.peripherals["SID"] = {
            base = 0xD400,
            type = "audio",
            description = "Sound Interface Device 6581/8580",
            registers = {}
        }
        
        local p = self.peripherals["SID"]
        p.registers["FREQ1LO"] = {
            address = 0xD400,
            size = 1,
            value = 0
        }
        p.registers["FREQ1HI"] = {
            address = 0xD401,
            size = 1,
            value = 0
        }
        p.registers["PW1LO"] = {
            address = 0xD402,
            size = 1,
            value = 0
        }
        p.registers["PW1HI"] = {
            address = 0xD403,
            size = 1,
            value = 0
        }
        p.registers["CR1"] = {
            address = 0xD404,
            size = 1,
            value = 0
        }
        p.registers["AD1"] = {
            address = 0xD405,
            size = 1,
            value = 0
        }
        p.registers["SR1"] = {
            address = 0xD406,
            size = 1,
            value = 0
        }
        p.registers["FREQ2LO"] = {
            address = 0xD407,
            size = 1,
            value = 0
        }
        p.registers["FREQ2HI"] = {
            address = 0xD408,
            size = 1,
            value = 0
        }
        p.registers["PW2LO"] = {
            address = 0xD409,
            size = 1,
            value = 0
        }
        p.registers["PW2HI"] = {
            address = 0xD40A,
            size = 1,
            value = 0
        }
        p.registers["CR2"] = {
            address = 0xD40B,
            size = 1,
            value = 0
        }
        p.registers["AD2"] = {
            address = 0xD40C,
            size = 1,
            value = 0
        }
        p.registers["SR2"] = {
            address = 0xD40D,
            size = 1,
            value = 0
        }
        p.registers["FREQ3LO"] = {
            address = 0xD40E,
            size = 1,
            value = 0
        }
        p.registers["FREQ3HI"] = {
            address = 0xD40F,
            size = 1,
            value = 0
        }
        p.registers["PW3LO"] = {
            address = 0xD410,
            size = 1,
            value = 0
        }
        p.registers["PW3HI"] = {
            address = 0xD411,
            size = 1,
            value = 0
        }
        p.registers["CR3"] = {
            address = 0xD412,
            size = 1,
            value = 0
        }
        p.registers["AD3"] = {
            address = 0xD413,
            size = 1,
            value = 0
        }
        p.registers["SR3"] = {
            address = 0xD414,
            size = 1,
            value = 0
        }
        p.registers["FCH"] = {
            address = 0xD415,
            size = 1,
            value = 0
        }
        p.registers["FCL"] = {
            address = 0xD416,
            size = 1,
            value = 0
        }
        p.registers["RES_FLT"] = {
            address = 0xD417,
            size = 1,
            value = 0
        }
        p.registers["VOLUME"] = {
            address = 0xD418,
            size = 1,
            value = 0
        }
        p.registers["POTX"] = {
            address = 0xD419,
            size = 1,
            value = 0
        }
        p.registers["POTY"] = {
            address = 0xD41A,
            size = 1,
            value = 0
        }
        p.registers["OSC3"] = {
            address = 0xD41B,
            size = 1,
            value = 0
        }
        p.registers["ENV3"] = {
            address = 0xD41C,
            size = 1,
            value = 0
        }
        self.peripherals["CIA1"] = {
            base = 0xDC00,
            type = "timer",
            description = "Complex Interface Adapter 1 - Keyboard/Serial",
            registers = {}
        }
        
        local p = self.peripherals["CIA1"]
        p.registers["PRA"] = {
            address = 0xDC00,
            size = 1,
            value = 0
        }
        p.registers["PRB"] = {
            address = 0xDC01,
            size = 1,
            value = 0
        }
        p.registers["DDRA"] = {
            address = 0xDC02,
            size = 1,
            value = 0
        }
        p.registers["DDRB"] = {
            address = 0xDC03,
            size = 1,
            value = 0
        }
        p.registers["TA_LO"] = {
            address = 0xDC04,
            size = 1,
            value = 0
        }
        p.registers["TA_HI"] = {
            address = 0xDC05,
            size = 1,
            value = 0
        }
        p.registers["TB_LO"] = {
            address = 0xDC06,
            size = 1,
            value = 0
        }
        p.registers["TB_HI"] = {
            address = 0xDC07,
            size = 1,
            value = 0
        }
        p.registers["TOD_TENTH"] = {
            address = 0xDC08,
            size = 1,
            value = 0
        }
        p.registers["TOD_SEC"] = {
            address = 0xDC09,
            size = 1,
            value = 0
        }
        p.registers["TOD_MIN"] = {
            address = 0xDC0A,
            size = 1,
            value = 0
        }
        p.registers["TOD_HR"] = {
            address = 0xDC0B,
            size = 1,
            value = 0
        }
        p.registers["SDR"] = {
            address = 0xDC0C,
            size = 1,
            value = 0
        }
        p.registers["ICR"] = {
            address = 0xDC0D,
            size = 1,
            value = 0
        }
        p.registers["CRA"] = {
            address = 0xDC0E,
            size = 1,
            value = 0
        }
        p.registers["CRB"] = {
            address = 0xDC0F,
            size = 1,
            value = 0
        }
        self.peripherals["CIA2"] = {
            base = 0xDD00,
            type = "timer",
            description = "Complex Interface Adapter 2 - Serial/Bus",
            registers = {}
        }
        
        local p = self.peripherals["CIA2"]
        p.registers["PRA"] = {
            address = 0xDD00,
            size = 1,
            value = 0
        }
        p.registers["PRB"] = {
            address = 0xDD01,
            size = 1,
            value = 0
        }
        p.registers["DDRA"] = {
            address = 0xDD02,
            size = 1,
            value = 0
        }
        p.registers["DDRB"] = {
            address = 0xDD03,
            size = 1,
            value = 0
        }
        p.registers["TA_LO"] = {
            address = 0xDD04,
            size = 1,
            value = 0
        }
        p.registers["TA_HI"] = {
            address = 0xDD05,
            size = 1,
            value = 0
        }
        p.registers["TB_LO"] = {
            address = 0xDD06,
            size = 1,
            value = 0
        }
        p.registers["TB_HI"] = {
            address = 0xDD07,
            size = 1,
            value = 0
        }
        p.registers["TOD_TENTH"] = {
            address = 0xDD08,
            size = 1,
            value = 0
        }
        p.registers["TOD_SEC"] = {
            address = 0xDD09,
            size = 1,
            value = 0
        }
        p.registers["TOD_MIN"] = {
            address = 0xDD0A,
            size = 1,
            value = 0
        }
        p.registers["TOD_HR"] = {
            address = 0xDD0B,
            size = 1,
            value = 0
        }
        p.registers["SDR"] = {
            address = 0xDD0C,
            size = 1,
            value = 0
        }
        p.registers["ICR"] = {
            address = 0xDD0D,
            size = 1,
            value = 0
        }
        p.registers["CRA"] = {
            address = 0xDD0E,
            size = 1,
            value = 0
        }
        p.registers["CRB"] = {
            address = 0xDD0F,
            size = 1,
            value = 0
        }
        self.peripherals["COLORRAM"] = {
            base = 0xD800,
            type = "memory",
            description = "Color RAM (4-bit per char cell)",
            registers = {}
        }
        
        local p = self.peripherals["COLORRAM"]
        p.registers["COLOR"] = {
            address = 0xD800,
            size = 1,
            value = 0
        }
        self.peripherals["IEC"] = {
            base = 0xDC00,
            type = "bus",
            description = "IEC Serial Bus (via CIA1)",
            registers = {}
        }
        
        local p = self.peripherals["IEC"]
        p.registers["IEC_DATA"] = {
            address = 0xDC00,
            size = 1,
            value = 0
        }
        p.registers["IEC_CLOCK"] = {
            address = 0xDC01,
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
