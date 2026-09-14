namespace VMLRuntime.Device
{
    public interface IMmioDevice
    {
        /// <summary>
        /// 设备名称
        /// </summary>
        string Name { get; }
        /// <summary>
        /// MMIO 地址
        /// </summary>
        uint MmioStart { get; }
        /// <summary>
        /// MMIO 大小
        /// </summary>
        uint MmioSize { get; }
        /// <summary>
        /// MMIO 访问权限
        /// </summary>
        MmioAccess MmioAccess { get; }

        /// <summary>
        /// 读取 MMIO 数据
        /// </summary>
        /// <param name="offset">偏移量</param>
        /// <param name="size">大小</param>
        /// <returns></returns>
        ulong ReadMmio(uint offset, int size);

        /// <summary>
        /// 写入 MMIO 数据
        /// </summary>
        /// <param name="offset">偏移量</param>
        /// <param name="value">值</param>
        /// <param name="size">大小</param>
        void WriteMmio(uint offset, ulong value, int size);
    }
}
