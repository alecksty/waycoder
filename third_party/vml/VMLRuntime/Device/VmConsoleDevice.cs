using System;
using System.Text;

namespace VMLRuntime.Device
{
    /// <summary>
    /// 控制台设备 - 提供标准输入输出功能
    /// </summary>
    public class VmConsoleDevice : IDevice
    {
        private readonly EConsoleType _type;
        private EDeviceStatus _status;

        // POSIX termios 状态
        private int _c_iflag = 0x0100;   // ICRNL
        private int _c_oflag = 0x0003;   // OPOST | ONLCR
        private int _c_cflag = 0;
        private int _c_lflag = 0x0007;   // ECHO | ICANON | ISIG
        private byte[] _c_cc = new byte[8];

        private bool EchoEnabled => (_c_lflag & 0x0001) != 0;
        private bool CanonicalEnabled => (_c_lflag & 0x0002) != 0;
        
        /// <summary>
        /// 设备名称
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// 设备类型
        /// </summary>
        public EDeviceType Type => EDeviceType.Console;
        
        /// <summary>
        /// 设备状态
        /// </summary>
        public EDeviceStatus Status => _status;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="type">控制台设备类型</param>
        public VmConsoleDevice(EConsoleType type = EConsoleType.Standard)
        {
            _type = type;
            _status = EDeviceStatus.Closed;
            
            Name = type switch
            {
                EConsoleType.Stdin => "stdin",
                EConsoleType.Stdout => "stdout",
                EConsoleType.Stderr => "stderr",
                _ => "console"
            };
        }
        
        /// <summary>
        /// 打开设备
        /// </summary>
        /// <returns>是否成功</returns>
        public bool Open()
        {
            _status = EDeviceStatus.Open;
            return true;
        }
        
        /// <summary>
        /// 关闭设备
        /// </summary>
        public void Close()
        {
            _status = EDeviceStatus.Closed;
        }
        
        /// <summary>
        /// 读取数据
        /// </summary>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="count">要读取的字节数</param>
        /// <returns>实际读取的字节数</returns>
        public int Read(byte[] buffer, int offset, int count)
        {
            if (_status != EDeviceStatus.Open)
                return -1;
            
            // 只有标准输入设备可以读取
            if (_type != EConsoleType.Standard && _type != EConsoleType.Stdin)
                return -1;
            
            try
            {
                if (!Console.KeyAvailable) return 0;
                bool intercept = !EchoEnabled;
                var key = Console.ReadKey(intercept);
                if (count <= 0) return 0;
                buffer[offset] = (byte)key.KeyChar;
                return 1;
            }
            catch
            {
                return -1;
            }
        }
        
        /// <summary>
        /// 写入数据
        /// </summary>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="count">要写入的字节数</param>
        /// <returns>实际写入的字节数</returns>
        public int Write(byte[] buffer, int offset, int count)
        {
            if (_status != EDeviceStatus.Open)
                return -1;
            
            // 只有标准输出和标准错误设备可以写入
            if (_type != EConsoleType.Standard && 
                _type != EConsoleType.Stdout && 
                _type != EConsoleType.Stderr)
                return -1;
            
            try
            {
                var text = Encoding.UTF8.GetString(buffer, offset, count);
                
                // 如果是标准错误，使用错误输出
                if (_type == EConsoleType.Stderr)
                {
                    Console.Error.Write(text);
                }
                else
                {
                    Console.Write(text);
                }
                
                return count;
            }
            catch
            {
                return -1;
            }
        }
        
        /// <summary>
        /// 控制设备
        /// </summary>
        /// <param name="command">控制命令</param>
        /// <param name="data">命令数据</param>
        /// <returns>命令执行结果</returns>
        public int Control(int command, byte[] data)
        {
            if (_status != EDeviceStatus.Open)
                return -1;
            
            switch (command)
            {
                case 0: // 获取控制台信息
                    return GetConsoleInfo();

                case 1: // 清屏
                    Console.Clear();
                    return 0;

                case 2: // 设置光标位置
                    if (data != null && data.Length >= 8)
                    {
                        int x = BitConverter.ToInt32(data, 0);
                        int y = BitConverter.ToInt32(data, 4);
                        Console.SetCursorPosition(x, y);
                        return 0;
                    }
                    return -1;

                case 3: // 获取光标位置
                    if (data != null && data.Length >= 8)
                    {
                        int x = Console.CursorLeft;
                        int y = Console.CursorTop;
                        BitConverter.GetBytes(x).CopyTo(data, 0);
                        BitConverter.GetBytes(y).CopyTo(data, 4);
                        return 0;
                    }
                    return -1;

                case 4: // TCGETATTR — 获取终端属性
                    return GetTermios(data);

                case 5: // TCSETATTR — 设置终端属性
                    return SetTermios(data);

                case 6: // TCFLUSH — 刷新缓冲区
                    return 0; // C# 控制台无缓冲区刷新概念

                case 7: // GET_TERM_SIZE — 获取窗口大小
                    if (data != null && data.Length >= 8)
                    {
                        int w = Console.WindowWidth;
                        int h = Console.WindowHeight;
                        BitConverter.GetBytes(w).CopyTo(data, 0);
                        BitConverter.GetBytes(h).CopyTo(data, 4);
                        return 0;
                    }
                    return -1;

                case 8: // GET_TTY_NAME — 获取终端名称
                    if (data != null && data.Length > 0)
                    {
                        byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(
                            _type switch
                            {
                                EConsoleType.Stdin => "/dev/stdin",
                                EConsoleType.Stdout => "/dev/stdout",
                                EConsoleType.Stderr => "/dev/stderr",
                                _ => "/dev/console"
                            });
                        int copyLen = Math.Min(nameBytes.Length, data.Length - 1);
                        Array.Copy(nameBytes, 0, data, 0, copyLen);
                        data[copyLen] = 0; // null-terminate
                        return 0;
                    }
                    return -1;

                case 9: // ISATTY — 检查是否为终端
                    return 1;

                default:
                    return -1;
            }
        }
        
        /// <summary>
        /// 获取控制台信息
        /// </summary>
        /// <returns>控制台信息编码</returns>
        private int GetConsoleInfo()
        {
            int info = 0;

            // 位0: 是否支持颜色
            if (Console.BackgroundColor != ConsoleColor.Black || Console.ForegroundColor != ConsoleColor.Gray)
                info |= 1;

            // 位1: 是否支持光标移动
            info |= 2;

            // 位2: 是否支持清屏
            info |= 4;

            // 位3-7: 保留

            // 位8-15: 控制台宽度 (低8位)
            info |= (Math.Min(Console.WindowWidth, 255) << 8);

            // 位16-23: 控制台高度 (低8位)
            info |= (Math.Min(Console.WindowHeight, 255) << 16);

            return info;
        }

        /// <summary>
        /// 获取终端属性 (TCGETATTR), 序列化到 data 缓冲区
        /// 布局: c_iflag(4) c_oflag(4) c_cflag(4) c_lflag(4) c_cc(8) = 24 bytes
        /// </summary>
        private int GetTermios(byte[] data)
        {
            if (data == null || data.Length < 24)
                return -1;

            BitConverter.GetBytes(_c_iflag).CopyTo(data, 0);
            BitConverter.GetBytes(_c_oflag).CopyTo(data, 4);
            BitConverter.GetBytes(_c_cflag).CopyTo(data, 8);
            BitConverter.GetBytes(_c_lflag).CopyTo(data, 12);
            Array.Copy(_c_cc, 0, data, 16, 8);
            return 0;
        }

        /// <summary>
        /// 设置终端属性 (TCSETATTR), 从 data 缓冲区反序列化
        /// 布局: action(4) + c_iflag(4) c_oflag(4) c_cflag(4) c_lflag(4) c_cc(8) = 28 bytes
        /// </summary>
        private int SetTermios(byte[] data)
        {
            if (data == null || data.Length < 28)
                return -1;

            int action = BitConverter.ToInt32(data, 0);

            // 根据 action 处理 (TCSANOW=0, TCSADRAIN=1, TCSAFLUSH=2)
            // 当前实现: 所有 action 都立即生效

            _c_iflag = BitConverter.ToInt32(data, 4);
            _c_oflag = BitConverter.ToInt32(data, 8);
            _c_cflag = BitConverter.ToInt32(data, 12);
            _c_lflag = BitConverter.ToInt32(data, 16);
            Array.Copy(data, 20, _c_cc, 0, 8);

            return 0;
        }
    }
}
