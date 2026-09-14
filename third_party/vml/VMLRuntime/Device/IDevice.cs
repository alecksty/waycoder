namespace VMLRuntime.Device
{
    /// <summary>
    /// 设备接口 - 所有设备必须实现的统一接口
    /// </summary>
    public interface IDevice
    {
        /// <summary>
        /// 设备名称
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// 设备类型
        /// </summary>
        EDeviceType Type { get; }
        
        /// <summary>
        /// 设备状态
        /// </summary>
        EDeviceStatus Status { get; }
        
        /// <summary>
        /// 打开设备
        /// </summary>
        /// <returns>是否成功</returns>
        bool Open();
        
        /// <summary>
        /// 关闭设备
        /// </summary>
        void Close();
        
        /// <summary>
        /// 读取数据
        /// </summary>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="count">要读取的字节数</param>
        /// <returns>实际读取的字节数</returns>
        int Read(byte[] buffer, int offset, int count);
        
        /// <summary>
        /// 写入数据
        /// </summary>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="count">要写入的字节数</param>
        /// <returns>实际写入的字节数</returns>
        int Write(byte[] buffer, int offset, int count);
        
        /// <summary>
        /// 控制设备（特殊操作）
        /// </summary>
        /// <param name="command">控制命令</param>
        /// <param name="data">命令数据</param>
        /// <returns>命令执行结果</returns>
        int Control(int command, byte[] data);
    }
}
