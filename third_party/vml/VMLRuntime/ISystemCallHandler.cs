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
        /// 输入源是不是**已经给不出东西了**（而且**以后也不会再有**）——
        /// 脚本化输入读完为 `true`，**压根没有输入源**也为 `true`，
        /// 交互式（有源、用户在敲）为 `false`。默认实现返回 `false`，所以已有的实现不必改。
        ///
        /// ⚠ **两个消费者，需求相反，靠谁在问来区分**（这一条是加 `#14` 时才理清的）：
        /// · `SYSCALL #14`（stdio 的 `getchar`）把 `true` 当成 **`EOF`(-1)**；
        /// · `SYSCALL #5`（`conio` 的 `getch`）把 `true` 当成**空行**（`0x0A`，DOS 的"确认"）。
        /// 所以"没有输入源"必须报 `true` —— 报 `false` 的话阻塞读会**空转到超时**
        /// （实测：桌面不给 `--stdin` 时每次读键耗满 `TimeoutSeconds`，最后
        /// `VM execution cancelled`），而那时**没有任何输入会到来**，正确语义就是 EOF。
        /// ⚠ **但真有源的交互实现绝不能报 `true`**（手机端 `MauiVml.CaptureIo` 是反例的边界：
        /// 它两个源都没有才报 `true`）—— 否则每个交互程序一读键就立刻收到 EOF 退出。
        ///
        /// ⚠ **为什么需要它**：`SYSCALL #5` 的*阻塞*模式在"没键"时是**死等**
        /// （`Thread.Sleep(10)` 转圈），交互式下这是对的（就该等用户敲），
        /// 但**脚本化输入**下就成了永久挂起 —— 实测 `cases/16-conio-key.c`
        /// 第 5 次 `getch()`（等 DOS 语义的"空行确认"）直接卡到超时。
        ///
        /// 原先靠 `KeyAvailable()` **恒返回 true** 掩盖了这个洞，代价是
        /// `conio.kbhit()` 彻底失效 ⇒ `cmatrix` / `tty-clock` 那类
        /// "`timeout(0)` + 非阻塞 `wgetch`"的老程序**一帧都画不出来**
        /// （见 `Lib/shared/src/conio.c` 里 kbhit 的说明）。
        /// 现在把两件事**分开**：`KeyAvailable()` 如实回答"此刻有没有键"，
        /// 本属性回答"还有没有可能来键"。
        /// </summary>
        bool InputExhausted => false;

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