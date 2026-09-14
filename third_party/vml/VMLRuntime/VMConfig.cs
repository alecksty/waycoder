using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.Serialization;
using VMLRuntime.Device;

namespace VMLRuntime
{
    /// <summary>
    /// 虚拟机配置
    /// </summary>
    public class VmConfig
    {
        /// <summary>
        /// 内存大小（字节），默认1MB，最大2GB
        /// </summary>
        private int _memorySize = 1024 * 1024;
        public int MemorySize
        {
            get => _memorySize;
            set => _memorySize = Math.Clamp(value, 64 * 1024, int.MaxValue);
        }

        /// <summary>
        /// 栈大小（字节），默认64KB，最大2GB
        /// </summary>
        private int _stackSize = 64 * 1024;
        public int StackSize
        {
            get => _stackSize;
            set => _stackSize = Math.Clamp(value, 1024, _memorySize / 2);
        }

        /// <summary>
        /// 栈顶地址（SP 初始值），0 = 自动计算（MemorySize - 4 或 StackSize）
        /// </summary>
        public int StackTop { get; set; } = 0;

        /// <summary>
        /// 程序起始地址（PC 初始值），默认 0
        /// </summary>
        public int ProgramStart { get; set; } = 0;

        /// <summary>
        /// VGA显存起始地址，默认0xB8000
        /// </summary>
        public int VgaStartAddress { get; set; } = 0xB8000; // VGA显存起始地址

        /// <summary>
        /// VGA显存大小（字节），默认256K
        /// </summary>
        public int VgaSize { get; set; } = 256 * 1024; // 256K显存

        /// <summary>
        /// VGA显示配置
        /// </summary>
        public VgaDisplayConfig VgaDisplay { get; set; } = new();

        /// <summary>
        /// 键盘数据寄存器地址，默认0x60
        /// </summary>
        public int KeyboardDataAddress { get; set; } = 0x60; // 键盘数据寄存器

        /// <summary>内存映射保护区域（从 profile 加载）</summary>
        public List<VmMemoryMapEntry> MemoryMap { get; set; } = new();

        /// <summary>
        /// 键盘状态寄存器地址，默认0x64
        /// </summary>
        public int KeyboardStatusAddress { get; set; } = 0x64; // 键盘状态寄存器

        /// <summary>
        /// 鼠标X坐标地址，默认0x300
        /// </summary>
        public int MouseXAddress { get; set; } = 0x300; // 鼠标X坐标

        /// <summary>
        /// 鼠标Y坐标地址，默认0x302
        /// </summary>
        public int MouseYAddress { get; set; } = 0x302; // 鼠标Y坐标

        /// <summary>
        /// 鼠标按钮状态地址，默认0x304
        /// </summary>
        public int MouseButtonAddress { get; set; } = 0x304; // 鼠标按钮状态

        /// <summary>
        /// 数据段/堆起始地址，默认0x400（IVT之后）
        /// IBM-PC 设为 0x8000 以保护低32KB系统区域（字库/寄存器/调色板）
        /// </summary>
        public int DataBase { get; set; } = 0x400;

        /// <summary>
        /// CPU 速度（指令/秒），0 = 全速无延时
        /// </summary>
        public int CpuSpeed { get; set; } = 0;

        /// <summary>
        /// 定时器中断间隔（指令条数），0 = 禁用。仅 OS 模式生效。
        /// </summary>
        public int TimerInterruptInterval { get; set; } = 0;

        /// <summary>
        /// 从设备描述文件加载配置
        /// </summary>
        public static VmConfig LoadFromProfile(string profileName)
        {
            var profile = DeviceProfile.LoadByName(profileName);
            return profile?.ToVmConfig() ?? new VmConfig();
        }

        /// <summary>
        /// 从文件加载配置
        /// </summary>
        public static VmConfig LoadFromFile(string configPath)
        {
            if (File.Exists(configPath))
            {
                try
                {
                    // 首先读取文件内容
                    string content = File.ReadAllText(configPath);
                    
                    // 手动解析XML，处理十六进制值
                    var config = new VmConfig();
                    
                    // 解析MemorySize
                    var memorySizeStart = content.IndexOf("<MemorySize>") + 12;
                    var memorySizeEnd = content.IndexOf("</MemorySize>", memorySizeStart);
                    if (memorySizeStart > 12 && memorySizeEnd > memorySizeStart)
                    {
                        var memorySizeStr = content.Substring(memorySizeStart, memorySizeEnd - memorySizeStart);
                        config.MemorySize = ParseInt(memorySizeStr);
                    }
                    
                    // 解析VgaStartAddress
                    var vgaStartAddressStart = content.IndexOf("<VgaStartAddress>") + 18;
                    var vgaStartAddressEnd = content.IndexOf("</VgaStartAddress>", vgaStartAddressStart);
                    if (vgaStartAddressStart > 18 && vgaStartAddressEnd > vgaStartAddressStart)
                    {
                        string vgaStartAddressStr = content.Substring(vgaStartAddressStart, vgaStartAddressEnd - vgaStartAddressStart);
                        config.VgaStartAddress = ParseInt(vgaStartAddressStr);
                    }
                    
                    // 解析VgaSize
                    var vgaSizeStart = content.IndexOf("<VgaSize>") + 9;
                    var vgaSizeEnd = content.IndexOf("</VgaSize>", vgaSizeStart);
                    if (vgaSizeStart > 9 && vgaSizeEnd > vgaSizeStart)
                    {
                        string vgaSizeStr = content.Substring(vgaSizeStart, vgaSizeEnd - vgaSizeStart);
                        config.VgaSize = ParseInt(vgaSizeStr);
                    }
                    
                    // 解析VgaDisplay
                    var vgaDisplayStart = content.IndexOf("<VgaDisplay>") + 13;
                    var vgaDisplayEnd = content.IndexOf("</VgaDisplay>", vgaDisplayStart);
                    if (vgaDisplayStart > 13 && vgaDisplayEnd > vgaDisplayStart)
                    {
                        var vgaDisplayContent = content.Substring(vgaDisplayStart, vgaDisplayEnd - vgaDisplayStart);
                        
                        // 解析Width
                        var widthStart = vgaDisplayContent.IndexOf("<Width>") + 7;
                        var widthEnd = vgaDisplayContent.IndexOf("</Width>", widthStart);
                        if (widthStart > 7 && widthEnd > widthStart)
                        {
                            string widthStr = vgaDisplayContent.Substring(widthStart, widthEnd - widthStart);
                            config.VgaDisplay.Width = ParseInt(widthStr);
                        }
                        
                        // 解析Height
                        var heightStart = vgaDisplayContent.IndexOf("<Height>") + 8;
                        var heightEnd = vgaDisplayContent.IndexOf("</Height>", heightStart);
                        if (heightStart > 8 && heightEnd > heightStart)
                        {
                            string heightStr = vgaDisplayContent.Substring(heightStart, heightEnd - heightStart);
                            config.VgaDisplay.Height = ParseInt(heightStr);
                        }
                        
                        // 解析Mode
                        var modeStart = vgaDisplayContent.IndexOf("<Mode>") + 6;
                        var modeEnd = vgaDisplayContent.IndexOf("</Mode>", modeStart);
                        if (modeStart > 6 && modeEnd > modeStart)
                        {
                            string modeStr = vgaDisplayContent.Substring(modeStart, modeEnd - modeStart);
                            config.VgaDisplay.Mode = ParseInt(modeStr);
                        }
                    }
                    
                    // 解析KeyboardDataAddress
                    var keyboardDataAddressStart = content.IndexOf("<KeyboardDataAddress>") + 22;
                    var keyboardDataAddressEnd = content.IndexOf("</KeyboardDataAddress>", keyboardDataAddressStart);
                    if (keyboardDataAddressStart > 22 && keyboardDataAddressEnd > keyboardDataAddressStart)
                    {
                        string keyboardDataAddressStr = content.Substring(keyboardDataAddressStart, keyboardDataAddressEnd - keyboardDataAddressStart);
                        config.KeyboardDataAddress = ParseInt(keyboardDataAddressStr);
                    }
                    
                    // 解析KeyboardStatusAddress
                    var keyboardStatusAddressStart = content.IndexOf("<KeyboardStatusAddress>") + 24;
                    var keyboardStatusAddressEnd = content.IndexOf("</KeyboardStatusAddress>", keyboardStatusAddressStart);
                    if (keyboardStatusAddressStart > 24 && keyboardStatusAddressEnd > keyboardStatusAddressStart)
                    {
                        string keyboardStatusAddressStr = content.Substring(keyboardStatusAddressStart, keyboardStatusAddressEnd - keyboardStatusAddressStart);
                        config.KeyboardStatusAddress = ParseInt(keyboardStatusAddressStr);
                    }
                    
                    // 解析MouseXAddress
                    var mouseXAddressStart = content.IndexOf("<MouseXAddress>") + 15;
                    var mouseXAddressEnd = content.IndexOf("</MouseXAddress>", mouseXAddressStart);
                    if (mouseXAddressStart > 15 && mouseXAddressEnd > mouseXAddressStart)
                    {
                        string mouseXAddressStr = content.Substring(mouseXAddressStart, mouseXAddressEnd - mouseXAddressStart);
                        config.MouseXAddress = ParseInt(mouseXAddressStr);
                    }
                    
                    // 解析MouseYAddress
                    var mouseYAddressStart = content.IndexOf("<MouseYAddress>") + 15;
                    var mouseYAddressEnd = content.IndexOf("</MouseYAddress>", mouseYAddressStart);
                    if (mouseYAddressStart > 15 && mouseYAddressEnd > mouseYAddressStart)
                    {
                        string mouseYAddressStr = content.Substring(mouseYAddressStart, mouseYAddressEnd - mouseYAddressStart);
                        config.MouseYAddress = ParseInt(mouseYAddressStr);
                    }
                    
                    // 解析MouseButtonAddress
                    var mouseButtonAddressStart = content.IndexOf("<MouseButtonAddress>") + 20;
                    var mouseButtonAddressEnd = content.IndexOf("</MouseButtonAddress>", mouseButtonAddressStart);
                    if (mouseButtonAddressStart > 20 && mouseButtonAddressEnd > mouseButtonAddressStart)
                    {
                        string mouseButtonAddressStr = content.Substring(mouseButtonAddressStart, mouseButtonAddressEnd - mouseButtonAddressStart);
                        config.MouseButtonAddress = ParseInt(mouseButtonAddressStr);
                    }
                    
                    return config;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"加载配置文件失败: {ex.Message}");
                }
            }
            return new VmConfig();
        }

        /// <summary>
        /// 保存配置到文件
        /// </summary>
        /// <param name="configPath">配置文件路径</param>
        [UnconditionalSuppressMessage("trim", "IL2026", Justification = "VmConfig type is preserved by direct usage throughout the runtime")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "XmlSerializer requires dynamic code; config saving is optional and graceful degradation is in place")]
        public void SaveToFile(string configPath)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(VmConfig));
                var namespaces = new XmlSerializerNamespaces();
                namespaces.Add(string.Empty, string.Empty);
                using var writer = new StreamWriter(configPath);
                serializer.Serialize(writer, this, namespaces);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存配置文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取默认配置文件路径
        /// </summary>
        /// <returns>默认配置文件路径</returns>
        public static string GetDefaultConfigPath()
        {
            // 首先尝试使用当前目录
            var currentDirConfig = Path.Combine(Directory.GetCurrentDirectory(), "VMConfig.xml");
            if (File.Exists(currentDirConfig) || Directory.Exists(Directory.GetCurrentDirectory()))
            {
                return currentDirConfig;
            }
            
            // 如果当前目录不可用，尝试使用临时目录
            var tempDirConfig = Path.Combine(Path.GetTempPath(), "VML", "vm_config.xml");
            var tempDir = Path.GetDirectoryName(tempDirConfig);
            if (tempDir != null)
            {
                try
                {
                    Directory.CreateDirectory(tempDir);
                    return tempDirConfig;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"创建临时目录失败: {ex.Message}");
                }
            }
            
            // 作为最后手段，尝试使用ApplicationData目录
            try
            {
                var configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VML");
                Directory.CreateDirectory(configDir);
                return Path.Combine(configDir, "vm_config.xml");
            }
            catch
            {
                // 如果所有路径都失败，返回当前目录的配置文件
                return "VMConfig.xml";
            }
        }

        /// <summary>
        /// 解析字符串为整数，支持0x开头的十六进制
        /// </summary>
        /// <param name="value">要解析的字符串</param>
        /// <returns>解析后的整数值</returns>
        public static int ParseInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            value = value.Trim();
            
            // 检查是否为十六进制格式（0x或0X开头）
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToInt32(value.Substring(2), 16);
            }
            // 检查是否为十六进制格式（x或X开头，可能是解析错误导致的）
            else if (value.StartsWith("x", StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToInt32(value.Substring(1), 16);
            }
            
            // 否则按十进制解析
            return int.Parse(value);
        }
    }

    /// <summary>
    /// VGA显示配置
    /// </summary>
    public class VgaDisplayConfig
    {
        /// <summary>
        /// VGA显示宽度，默认80（字符模式）或320（图形模式）
        /// </summary>
        public int Width { get; set; } = 80; // 80列

        /// <summary>
        /// VGA显示高度，默认25（字符模式）或240（图形模式）
        /// </summary>
        public int Height { get; set; } = 25; // 25行

        /// <summary>
        /// VGA显示模式，0=字符模式，1=图形模式（320x240x24bit color）
        /// </summary>
        public int Mode { get; set; } = 0; // 默认字符模式

        /// <summary>
        /// 字符模式下每字符字节数。PC VGA=2（字符+属性），Apple II/C64=1（纯字符），默认2
        /// </summary>
        public int BytesPerCell { get; set; } = 2;

        /// <summary>
        /// 获取VGA显示宽度（兼容旧版本）
        /// </summary>
        [XmlIgnore]
        public int VgaWidth 
        { 
            get => Width; 
            set => Width = value; 
        }

        /// <summary>
        /// 获取VGA显示高度（兼容旧版本）
        /// </summary>
        [XmlIgnore]
        public int VgaHeight 
        { 
            get => Height; 
            set => Height = value; 
        }

        /// <summary>
        /// 获取VGA显示模式（兼容旧版本）
        /// </summary>
        [XmlIgnore]
        public int VgaMode 
        { 
            get => Mode; 
            set => Mode = value; 
        }
    }

    /// <summary>内存映射保护区域——定义一段内存的访问权限</summary>
    public class VmMemoryMapEntry
    {
        public int Start { get; set; }
        public int Size { get; set; }
        public bool Readable { get; set; } = true;
        public bool Writable { get; set; }
        public bool Executable { get; set; }
        public string Description { get; set; } = "";
    }
}
