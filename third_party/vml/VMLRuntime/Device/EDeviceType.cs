namespace VMLRuntime.Device
{
    /// <summary>
    /// 设备类型
    /// </summary>
    public enum EDeviceType
    {
        /// <summary>
        /// 控制台设备（标准输入输出）
        /// </summary>
        Console,
        
        /// <summary>
        /// 显示设备（VGA/图形）
        /// </summary>
        Display,
        
        /// <summary>
        /// 键盘设备
        /// </summary>
        Keyboard,
        
        /// <summary>
        /// 鼠标设备
        /// </summary>
        Mouse,
        
        /// <summary>
        /// 定时器设备
        /// </summary>
        Timer,
        
        /// <summary>
        /// 实时时钟设备
        /// </summary>
        RTC,
        
        /// <summary>
        /// 文件系统设备
        /// </summary>
        FileSystem,
        
        /// <summary>
        /// 存储设备
        /// </summary>
        Storage,
        
        /// <summary>
        /// 网络设备
        /// </summary>
        Network,
        
        /// <summary>
        /// 打印机设备
        /// </summary>
        Printer,
        
        /// <summary>
        /// 其他设备
        /// </summary>
        Other
    }
}
