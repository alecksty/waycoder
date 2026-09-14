using System;
using System.Diagnostics;

namespace VMLRuntime.Device
{
    /// <summary>
    /// 定时器设备 - 提供定时和计时功能
    /// </summary>
    public class VmTimerDevice : IDevice
    {
        private EDeviceStatus _status;
        private readonly Stopwatch _stopwatch;
        private long _interval; // 定时器间隔（毫秒）
        private long _lastTick;
        private bool _enabled;
        
        /// <summary>
        /// 设备名称
        /// </summary>
        public string Name => "timer";
        
        /// <summary>
        /// 设备类型
        /// </summary>
        public EDeviceType Type => EDeviceType.Timer;
        
        /// <summary>
        /// 设备状态
        /// </summary>
        public EDeviceStatus Status => _status;
        
        /// <summary>
        /// 定时器是否启用
        /// </summary>
        public bool Enabled => _enabled;
        
        /// <summary>
        /// 定时器间隔（毫秒）
        /// </summary>
        public long Interval => _interval;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public VmTimerDevice()
        {
            _status = EDeviceStatus.Closed;
            _stopwatch = new Stopwatch();
            _interval = 1000; // 默认1秒
            _lastTick = 0;
            _enabled = false;
        }
        
        /// <summary>
        /// 打开设备
        /// </summary>
        /// <returns>是否成功</returns>
        public bool Open()
        {
            _status = EDeviceStatus.Open;
            _stopwatch.Start();
            return true;
        }
        
        /// <summary>
        /// 关闭设备
        /// </summary>
        public void Close()
        {
            _status = EDeviceStatus.Closed;
            _stopwatch.Stop();
            _enabled = false;
        }
        
        /// <summary>
        /// 读取定时器数据
        /// </summary>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="count">要读取的字节数</param>
        /// <returns>实际读取的字节数</returns>
        public int Read(byte[] buffer, int offset, int count)
        {
            if (_status != EDeviceStatus.Open)
                return -1;
            
            // 定时器数据格式：8字节时间戳 + 1字节状态 = 9字节
            int bytesToRead = Math.Min(count, 9);
            
            if (bytesToRead >= 8)
            {
                long timestamp = _stopwatch.ElapsedMilliseconds;
                BitConverter.GetBytes(timestamp).CopyTo(buffer, offset);
            }
            
            if (bytesToRead >= 9)
            {
                byte status = 0;
                if (_enabled) status |= 1;
                if (CheckTimerTick()) status |= 2;
                
                buffer[offset + 8] = status;
            }
            
            return bytesToRead;
        }
        
        /// <summary>
        /// 写入数据（定时器设备不支持写入）
        /// </summary>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="count">要写入的字节数</param>
        /// <returns>实际写入的字节数</returns>
        public int Write(byte[] buffer, int offset, int count)
        {
            // 定时器设备不支持写入
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
                case 0: // 获取定时器状态
                    return GetTimerStatus(data);
                    
                case 1: // 启动定时器
                    return StartTimer(data);
                    
                case 2: // 停止定时器
                    return StopTimer();
                    
                case 3: // 设置定时器间隔
                    return SetInterval(data);
                    
                case 4: // 重置定时器
                    return ResetTimer();
                    
                case 5: // 检查定时器是否触发
                    return CheckTimer(data);
                    
                default:
                    return -1;
            }
        }
        
        /// <summary>
        /// 获取定时器状态
        /// </summary>
        /// <param name="data">数据缓冲区</param>
        /// <returns>执行结果</returns>
        private int GetTimerStatus(byte[] data)
        {
            if (data == null || data.Length < 17)
                return -1;
            
            long elapsed = _stopwatch.ElapsedMilliseconds;
            BitConverter.GetBytes(elapsed).CopyTo(data, 0);
            BitConverter.GetBytes(_interval).CopyTo(data, 8);
            
            byte status = 0;
            if (_enabled) status |= 1;
            if (_stopwatch.IsRunning) status |= 2;
            
            data[16] = status;
            
            return 0;
        }
        
        /// <summary>
        /// 启动定时器
        /// </summary>
        /// <param name="data">启动参数</param>
        /// <returns>执行结果</returns>
        private int StartTimer(byte[] data)
        {
            _enabled = true;
            _lastTick = _stopwatch.ElapsedMilliseconds;
            return 0;
        }
        
        /// <summary>
        /// 停止定时器
        /// </summary>
        /// <returns>执行结果</returns>
        private int StopTimer()
        {
            _enabled = false;
            return 0;
        }
        
        /// <summary>
        /// 设置定时器间隔
        /// </summary>
        /// <param name="data">间隔数据</param>
        /// <returns>执行结果</returns>
        private int SetInterval(byte[] data)
        {
            if (data == null || data.Length < 8)
                return -1;
            
            long newInterval = BitConverter.ToInt64(data, 0);
            if (newInterval <= 0)
                return -1;
            
            _interval = newInterval;
            return 0;
        }
        
        /// <summary>
        /// 重置定时器
        /// </summary>
        /// <returns>执行结果</returns>
        private int ResetTimer()
        {
            _stopwatch.Restart();
            _lastTick = 0;
            return 0;
        }
        
        /// <summary>
        /// 检查定时器是否触发
        /// </summary>
        /// <param name="data">数据缓冲区</param>
        /// <returns>执行结果</returns>
        private int CheckTimer(byte[] data)
        {
            if (data == null || data.Length < 1)
                return -1;
            
            bool triggered = CheckTimerTick();
            data[0] = (byte)(triggered ? 1 : 0);
            
            return 0;
        }
        
        /// <summary>
        /// 检查定时器是否触发
        /// </summary>
        /// <returns>是否触发</returns>
        private bool CheckTimerTick()
        {
            if (!_enabled || _interval <= 0)
                return false;
            
            long currentTime = _stopwatch.ElapsedMilliseconds;
            long elapsed = currentTime - _lastTick;
            
            if (elapsed >= _interval)
            {
                _lastTick = currentTime;
                return true;
            }
            
            return false;
        }
    }
}
