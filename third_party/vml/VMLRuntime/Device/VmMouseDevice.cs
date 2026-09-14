using System;

namespace VMLRuntime.Device
{
    /// <summary>
    /// 鼠标设备 - 提供鼠标输入功能
    /// </summary>
    public class VmMouseDevice : IDevice, IMmioDevice
    {
        #region 属性
        private EDeviceStatus _status;
        private int           _x;
        private int           _y;
        private int           _buttons;
        private int           _wheel;

        /// <summary>
        /// 设备名称
        /// </summary>
        public string Name => "mouse";

        /// <summary>
        /// 设备类型
        /// </summary>
        public EDeviceType Type => EDeviceType.Mouse;

        /// <summary>
        /// 设备状态
        /// </summary>
        public EDeviceStatus Status => _status;

        /// <summary>MMIO: 0x300=X, 0x302=Y, 0x304=buttons, 实际地址可配置</summary>
        public uint MmioStart { get; private set; } = 0x300;
        public uint MmioSize => 6;
        public MmioAccess MmioAccess => MmioAccess.ReadWrite;

        /// <summary>设置 MMIO 基址</summary>
        public void SetMmioBase(uint baseAddr)
        {
            MmioStart = baseAddr;
        }

        /// <summary>
        /// 鼠标X坐标
        /// </summary>
        public int X => _x;

        /// <summary>
        /// 鼠标Y坐标
        /// </summary>
        public int Y => _y;

        /// <summary>
        /// 鼠标按钮状态
        /// </summary>
        public int Buttons => _buttons;

        /// <summary>
        /// 鼠标滚轮状态
        /// </summary>
        public int Wheel => _wheel;
        #endregion

        #region 基本方法
        /// <summary>
        /// 构造函数
        /// </summary>
        public VmMouseDevice()
        {
            _status  = EDeviceStatus.Closed;
            _x       = 0;
            _y       = 0;
            _buttons = 0;
            _wheel   = 0;
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
        /// 读取鼠标数据
        /// </summary>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="count">要读取的字节数</param>
        /// <returns>实际读取的字节数</returns>
        public int Read(byte[] buffer, int offset, int count)
        {
            if (_status != EDeviceStatus.Open)
                return -1;

            // 鼠标数据格式：4字节X + 4字节Y + 1字节按钮 + 1字节滚轮 = 10字节
            int bytesToRead = Math.Min(count, 10);

            if (bytesToRead >= 4)
            {
                BitConverter.GetBytes(_x).CopyTo(buffer, offset);
            }

            if (bytesToRead >= 8)
            {
                BitConverter.GetBytes(_y).CopyTo(buffer, offset + 4);
            }

            if (bytesToRead >= 9)
            {
                buffer[offset + 8] = (byte)_buttons;
            }

            if (bytesToRead >= 10)
            {
                buffer[offset + 9] = (byte)_wheel;
            }

            return bytesToRead;
        }

        /// <summary>
        /// 写入数据（鼠标设备不支持写入）
        /// </summary>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="count">要写入的字节数</param>
        /// <returns>实际写入的字节数</returns>
        public int Write(byte[] buffer, int offset, int count)
        {
            // 鼠标设备不支持写入
            return -1;
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
                case 0: // 获取鼠标状态
                    return GetMouseStatus(data);

                case 1: // 设置鼠标位置
                    return SetMousePosition(data);

                case 2: // 设置鼠标按钮状态
                    return SetMouseButtons(data);

                case 3: // 设置鼠标滚轮状态
                    return SetMouseWheel(data);

                case 4: // 模拟鼠标移动
                    return SimulateMouseMove(data);

                case 5: // 模拟鼠标点击
                    return SimulateMouseClick(data);

                default:
                    return -1;
            }
        }
        #endregion

        #region 专有方法
        /// <summary>
        /// 获取鼠标状态
        /// </summary>
        /// <param name="data">数据缓冲区</param>
        /// <returns>执行结果</returns>
        private int GetMouseStatus(byte[] data)
        {
            if (data == null || data.Length < 10)
                return -1;

            BitConverter.GetBytes(_x).CopyTo(data, 0);
            BitConverter.GetBytes(_y).CopyTo(data, 4);
            data[8] = (byte)_buttons;
            data[9] = (byte)_wheel;

            return 0;
        }

        /// <summary>
        /// 设置鼠标位置
        /// </summary>
        /// <param name="data">位置数据 [x, y]</param>
        /// <returns>执行结果</returns>
        private int SetMousePosition(byte[] data)
        {
            if (data == null || data.Length < 8)
                return -1;

            _x = BitConverter.ToInt32(data, 0);
            _y = BitConverter.ToInt32(data, 4);

            return 0;
        }

        /// <summary>
        /// 设置鼠标位置
        /// 坐标范围：[x >= 0, y >= 0]
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>执行结果</returns>
        public int SetPosition(ushort x, ushort y)
        {
            // if (x < 0 || y < 0)
            //     return -1;

            _x = x;
            _y = y;
            return 0;
        }

        /// <summary>
        /// 设置鼠标按钮状态
        /// </summary>
        /// <param name="data">按钮状态数据</param>
        /// <returns>执行结果</returns>
        private int GetMouseButtons(byte[] data)
        {
            if (data == null || data.Length < 1)
                return -1;

            data[0] = (byte)_buttons;
            return 0;
        }

        /// <summary>
        /// 设置鼠标按钮状态
        /// </summary>
        /// <param name="data">按钮状态数据</param>
        /// <returns>执行结果</returns>
        private int SetMouseButtons(byte[] data)
        {
            if (data == null || data.Length < 1)
                return -1;

            _buttons = data[0];
            return 0;
        }
        
        public ulong ReadMmio(uint offset, int size)
        {
            switch (offset)
            {
                case 0: return (ushort)_x;      // X 坐标
                case 2: return (ushort)_y;      // Y 坐标
                case 4: return (byte)_buttons;  // 按钮状态
                default: return 0;
            }
        }

        public void WriteMmio(uint offset, ulong value, int size)
        {
            switch (offset)
            {
                case 0: _x = (short)(value & 0xFFFF); break;
                case 2: _y = (short)(value & 0xFFFF); break;
                case 4: _buttons = (byte)(value & 0xFF); break;
            }
        }

        /// <summary>
        /// 设置鼠标滚轮状态
        /// </summary>
        /// <param name="data">滚轮状态数据</param>
        /// <returns>执行结果</returns>
        private int SetMouseWheel(byte[] data)
        {
            if (data == null || data.Length < 1)
                return -1;

            _wheel = data[0];
            return 0;
        }

        /// <summary>
        /// 模拟鼠标移动
        /// </summary>
        /// <param name="data">移动数据 [deltaX, deltaY]</param>
        /// <returns>执行结果</returns>
        private int SimulateMouseMove(byte[] data)
        {
            if (data == null || data.Length < 8)
                return -1;

            int deltaX = BitConverter.ToInt32(data, 0);
            int deltaY = BitConverter.ToInt32(data, 4);

            _x += deltaX;
            _y += deltaY;

            // 限制坐标范围（假设屏幕大小为1024x768）
            _x = Math.Max(0, Math.Min(_x, 1023));
            _y = Math.Max(0, Math.Min(_y, 767));

            return 0;
        }

        /// <summary>
        /// 模拟鼠标点击
        /// </summary>
        /// <param name="data">点击数据 [button, state]</param>
        /// <returns>执行结果</returns>
        private int SimulateMouseClick(byte[] data)
        {
            if (data == null || data.Length < 2)
                return -1;

            var button = data[0];
            var state  = data[1];

            if (state == 1) // 按下
            {
                _buttons |= (1 << button);
            }
            else // 释放
            {
                _buttons &= ~(1 << button);
            }

            return 0;
        }
        #endregion
    }
}
