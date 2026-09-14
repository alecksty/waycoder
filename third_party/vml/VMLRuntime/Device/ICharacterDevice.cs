using System;

namespace VMLRuntime.Device
{
    /// <summary>
    /// 字符设备接口 - 用于处理字符流输入输出的设备
    /// </summary>
    public interface ICharacterDevice : IDevice
    {
        /// <summary>
        /// 读取一个字符
        /// </summary>
        /// <returns>读取的字符，-1表示无数据</returns>
        int ReadChar();
        
        /// <summary>
        /// 写入一个字符
        /// </summary>
        /// <param name="ch">要写入的字符</param>
        /// <returns>是否成功</returns>
        bool WriteChar(char ch);
        
        /// <summary>
        /// 检查是否有字符可读
        /// </summary>
        /// <returns>是否有字符可读</returns>
        bool HasChar();
        
        /// <summary>
        /// 获取设备信息
        /// </summary>
        /// <returns>设备信息字符串</returns>
        string GetDeviceInfo();
    }
}