using System;

namespace VMLRuntime
{
    /// <summary>
    /// 系统调用处理器接口
    /// </summary>
    public interface ISystemCallHandler
    {
        /// <summary>
        /// 处理系统调用
        /// </summary>
        /// <param name="syscallNumber">系统调用号</param>
        /// <param name="registers">寄存器数组</param>
        /// <param name="memory">内存数组</param>
        /// <param name="pc">程序计数器（可修改）</param>
        /// <returns>是否处理了该系统调用</returns>
        bool HandleSyscall(int syscallNumber, int[] registers, byte[] memory, ref int pc);
    }

    /// <summary>
    /// 控制台输入输出接口
    /// </summary>
    public interface IConsoleIO
    {
        /// <summary>
        /// 输出字符串
        /// </summary>
        /// <param name="str">要输出的字符串</param>
        void WriteString(string str);

        /// <summary>
        /// 输出字符
        /// </summary>
        /// <param name="ch">要输出的字符</param>
        void WriteChar(char ch);

        /// <summary>
        /// 输出整数
        /// </summary>
        /// <param name="value">要输出的整数值</param>
        void WriteInt(int value);

        /// <summary>
        /// 读取字符串
        /// </summary>
        /// <returns>读取的字符串</returns>
        string ReadString();

        /// <summary>
        /// 检查是否有按键可用（非阻塞）
        /// </summary>
        /// <returns>是否有按键可用</returns>
        bool KeyAvailable();

        /// <summary>
        /// 读取字符
        /// </summary>
        /// <returns>读取的字符</returns>
        char ReadChar();

        /// <summary>
        /// 读取整数
        /// </summary>
        /// <returns>读取的整数值</returns>
        int ReadInt();

        /// <summary>
        /// 输出浮点数
        /// </summary>
        /// <param name="value">要输出的浮点数值</param>
        void WriteFloat(float value);

        /// <summary>
        /// 读取浮点数
        /// </summary>
        /// <returns>读取的浮点数值</returns>
        float ReadFloat();

        /// <summary>
        /// 输出十六进制整数
        /// </summary>
        /// <param name="value">要输出的整数值</param>
        void WriteHex(int value);
    }

    /// <summary>
    /// 设备管理器接口
    /// </summary>
    public interface IDeviceManager
    {
        /// <summary>
        /// 打开设备
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <returns>设备句柄</returns>
        int OpenDevice(string deviceName);

        /// <summary>
        /// 关闭设备
        /// </summary>
        /// <param name="handle">设备句柄</param>
        /// <returns>是否成功</returns>
        bool CloseDevice(int handle);

        /// <summary>
        /// 从设备读取数据
        /// </summary>
        /// <param name="handle">设备句柄</param>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="count">要读取的字节数</param>
        /// <returns>实际读取的字节数</returns>
        int ReadDevice(int handle, byte[] buffer, int offset, int count);

        /// <summary>
        /// 向设备写入数据
        /// </summary>
        /// <param name="handle">设备句柄</param>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="count">要写入的字节数</param>
        /// <returns>实际写入的字节数</returns>
        int WriteDevice(int handle, byte[] buffer, int offset, int count);

        /// <summary>
        /// 控制设备
        /// </summary>
        /// <param name="handle">设备句柄</param>
        /// <param name="command">控制命令</param>
        /// <param name="data">命令数据</param>
        /// <returns>执行结果</returns>
        int ControlDevice(int handle, int command, byte[] data);
    }
}