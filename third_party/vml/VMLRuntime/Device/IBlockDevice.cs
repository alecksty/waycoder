namespace VMLRuntime.Device
{
    /// <summary>
    /// 块设备接口 - 用于处理块数据存储的设备
    /// </summary>
    public interface IBlockDevice : IDevice
    {
        /// <summary>
        /// 块大小（字节）
        /// </summary>
        int BlockSize { get; }
        
        /// <summary>
        /// 块数量
        /// </summary>
        int BlockCount { get; }
        
        /// <summary>
        /// 读取一个块
        /// </summary>
        /// <param name="blockNumber">块号</param>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="offset">缓冲区偏移量</param>
        /// <returns>是否成功</returns>
        bool ReadBlock(int blockNumber, byte[] buffer, int offset);
        
        /// <summary>
        /// 写入一个块
        /// </summary>
        /// <param name="blockNumber">块号</param>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="offset">缓冲区偏移量</param>
        /// <returns>是否成功</returns>
        bool WriteBlock(int blockNumber, byte[] buffer, int offset);
        
        /// <summary>
        /// 擦除一个块
        /// </summary>
        /// <param name="blockNumber">块号</param>
        /// <returns>是否成功</returns>
        bool EraseBlock(int blockNumber);
    }
}