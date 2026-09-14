using System;
using System.Collections.Generic;

namespace VMLRuntime.Device
{
    /// <summary>
    /// 键盘设备 - 提供键盘输入功能
    /// </summary>
    public class VmKeyboardDevice : IDevice, IMmioDevice
    {
        #region 属性
        private          EDeviceStatus _status;
        private readonly object        _lock = new();
        private readonly Queue<byte>   _keyBuffer;
        private          byte          _statusReg;

        /// <summary>
        /// 设备名称
        /// </summary>
        public string Name => "kbd";

        /// <summary>
        /// 设备类型
        /// </summary>
        public EDeviceType Type => EDeviceType.Keyboard;

        /// <summary>
        /// 设备状态
        /// </summary>
        public EDeviceStatus Status => _status;

        /// <summary>MMIO: data=base, status=base+offset, 实际地址可配置</summary>
        public uint MmioStart { get; private set; } = 0x60;
        public uint MmioSize => 32; // 足够覆盖 Apple II (0x10) 和 PC (0x04) 的状态偏移
        public MmioAccess MmioAccess => MmioAccess.ReadWrite;
        private uint _statusOffset = 4; // PC: status at data+4, Apple II: data+0x10

        /// <summary>设置 MMIO 基址和数据/状态偏移</summary>
        public void SetMmioBase(uint baseAddr, uint statusOffset = 4)
        {
            MmioStart = baseAddr;
            _statusOffset = statusOffset;
        }
        #endregion

        #region 基本方法
        /// <summary>
        /// 构造函数
        /// </summary>
        public VmKeyboardDevice()
        {
            _status    = EDeviceStatus.Closed;
            _keyBuffer = new Queue<byte>();
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
            lock (_lock) { _keyBuffer.Clear(); }
        }

        /// <summary>
        /// 清空键盘缓冲区
        /// </summary>
        public void ClearBuffer()
        {
            lock (_lock) { _keyBuffer.Clear(); }
        }

        /// <summary>由 ConsoleEmulator 调用，将按键压入设备队列</summary>
        public bool HasKey()
        {
            lock (_lock) { return _keyBuffer.Count > 0; }
        }

        public byte ReadKey()
        {
            lock (_lock) { return _keyBuffer.TryDequeue(out var key) ? key : (byte)0; }
        }

        public void EnqueueKey(byte keyCode)
        {
            lock (_lock)
            {
                if (_keyBuffer.Count < 256)
                {
                    _keyBuffer.Enqueue(keyCode);
                    _statusReg = 0x01;
                }
            }
        }

        /// <summary>入队扩展键（双字节: 0 + 扫描码，如方向键/F键）</summary>
        public void EnqueueExtendedKey(byte scanCode)
        {
            lock (_lock)
            {
                if (_keyBuffer.Count < 254)
                {
                    _keyBuffer.Enqueue(0);
                    _keyBuffer.Enqueue(scanCode);
                    _statusReg = 0x01;
                }
            }
        }

        /// <summary>将 ConsoleKey 映射为 QBasic 扫描码并入队（扩展键）</summary>
        public void EnqueueConsoleKey(ConsoleKey key)
        {
            byte? scan = key switch
            {
                ConsoleKey.UpArrow => 72,
                ConsoleKey.DownArrow => 80,
                ConsoleKey.LeftArrow => 75,
                ConsoleKey.RightArrow => 77,
                ConsoleKey.F1 => 59, ConsoleKey.F2 => 60, ConsoleKey.F3 => 61,
                ConsoleKey.F4 => 62, ConsoleKey.F5 => 63, ConsoleKey.F6 => 64,
                ConsoleKey.F7 => 65, ConsoleKey.F8 => 66, ConsoleKey.F9 => 67,
                ConsoleKey.F10 => 68, ConsoleKey.F11 => 133, ConsoleKey.F12 => 134,
                ConsoleKey.Home => 71, ConsoleKey.End => 79,
                ConsoleKey.PageUp => 73, ConsoleKey.PageDown => 81,
                ConsoleKey.Insert => 82, ConsoleKey.Delete => 83,
                ConsoleKey.Escape => 1,
                _ => null
            };
            if (scan.HasValue)
                EnqueueExtendedKey(scan.Value);
        }

        /// <summary>入队字符串（每个字符依次入队，末尾可选换行）</summary>
        public void EnqueueString(string text, bool addNewline = false)
        {
            lock (_lock)
            {
                foreach (char c in text)
                {
                    if (_keyBuffer.Count >= 256) break;
                    _keyBuffer.Enqueue((byte)c);
                }
                if (addNewline && _keyBuffer.Count < 256)
                    _keyBuffer.Enqueue((byte)'\n');
                if (_keyBuffer.Count > 0) _statusReg = 0x01;
            }
        }

        public ulong ReadMmio(uint offset, int size)
        {
            lock (_lock)
            {
                if (offset == 0) // 数据寄存器
                {
                    if (_keyBuffer.TryDequeue(out var key))
                    {
                        _statusReg = 0;
                        return key;
                    }
                    return 0;
                }
            }
            if (offset == _statusOffset) // 状态/选通清除寄存器
            {
                lock (_lock)
                {
                    // Apple II: 读 0xC010 清除选通并返回按键值
                    if (_statusOffset == 0x10 && _keyBuffer.Count > 0)
                    {
                        var k = _keyBuffer.Dequeue();
                        return k;
                    }
                }
                return _statusReg;
            }
            return 0;
        }

        public void WriteMmio(uint offset, ulong value, int size)
        {
            switch (offset)
            {
                case 0: // 数据寄存器 — 模拟器注入按键
                    lock (_lock)
                    {
                        if (_keyBuffer.Count < 256)
                        {
                            _keyBuffer.Enqueue((byte)(value & 0xFF));
                            _statusReg = 0x01;
                        }
                    }
                    break;
                case 4: // 状态寄存器 — 模拟器清除/设置状态
                    _statusReg = (byte)(value & 0xFF);
                    break;
            }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (_status != EDeviceStatus.Open)
                return -1;

            CheckKeyAvailable();

            int bytesRead = 0;
            lock (_lock)
            {
                for (int i = 0; i < count && _keyBuffer.Count > 0; i++)
                {
                    buffer[offset + i] = _keyBuffer.Dequeue();
                    bytesRead++;
                }
            }

            return bytesRead;
        }

        public int Write(byte[] buffer, int offset, int count)
        {
            return -1;
        }

        public int Control(int command, byte[] data)
        {
            if (_status != EDeviceStatus.Open)
                return -1;

            switch (command)
            {
                case 0: return GetKeyboardStatus(data);
                case 1: return CheckKey(data);
                case 2: _keyBuffer.Clear(); return 0;
                case 3: return SetLEDs(data);
                default: return -1;
            }
        }
        #endregion

        #region 专有方法
        private void CheckKeyAvailable()
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                lock (_lock)
                {
                    _keyBuffer.Enqueue((byte)key.KeyChar);

                    if (key.Key != ConsoleKey.NoName && (int)key.Key > 255)
                    {
                        _keyBuffer.Enqueue(0);
                        _keyBuffer.Enqueue((byte)((int)key.Key & 0xFF));
                        _keyBuffer.Enqueue((byte)(((int)key.Key >> 8) & 0xFF));
                    }
                }
            }
        }

        private int GetKeyboardStatus(byte[] data)
        {
            if (data == null || data.Length < 4)
                return -1;

            int status = 0;
            lock (_lock) { if (_keyBuffer.Count > 0) status |= 1; }
            if (Console.KeyAvailable)
                status |= 1;
            if (OperatingSystem.IsWindows())
            {
                if (Console.CapsLock)
                    status |= 2;
                if (Console.NumberLock)
                    status |= 4;
            }

            int keyCount;
            lock (_lock) { keyCount = Math.Min(_keyBuffer.Count, 15); }
            status |= (keyCount << 4);

            BitConverter.GetBytes(status).CopyTo(data, 0);
            return 0;
        }

        private int CheckKey(byte[] data)
        {
            if (data == null || data.Length < 1)
                return -1;

            CheckKeyAvailable();
            lock (_lock) { data[0] = (byte)(_keyBuffer.Count > 0 ? 1 : 0); }
            return 0;
        }

        private int SetLEDs(byte[] data)
        {
            if (data == null || data.Length < 1)
                return -1;
            return 0;
        }
        #endregion
    }
}
