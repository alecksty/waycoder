using System;

namespace VMLRuntime.Device
{
    /// <summary>
    /// RTC设备 - 提供实时时钟功能
    /// </summary>
    public class VmRtcDevice : IDevice
    {
        private EDeviceStatus _status;
        private DateTime _currentTime;
        private bool _is24HourFormat;
        
        /// <summary>
        /// 设备名称
        /// </summary>
        public string Name => "rtc";
        
        /// <summary>
        /// 设备类型
        /// </summary>
        public EDeviceType Type => EDeviceType.RTC;
        
        /// <summary>
        /// 设备状态
        /// </summary>
        public EDeviceStatus Status => _status;
        
        /// <summary>
        /// 当前时间
        /// </summary>
        public DateTime CurrentTime => _currentTime;
        
        /// <summary>
        /// 是否使用24小时制
        /// </summary>
        public bool Is24HourFormat => _is24HourFormat;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public VmRtcDevice()
        {
            _status = EDeviceStatus.Closed;
            _currentTime = DateTime.Now;
            _is24HourFormat = true;
        }
        
        /// <summary>
        /// 打开设备
        /// </summary>
        /// <returns>是否成功</returns>
        public bool Open()
        {
            _status = EDeviceStatus.Open;
            UpdateTime();
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
        /// 读取RTC数据
        /// </summary>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="count">要读取的字节数</param>
        /// <returns>实际读取的字节数</returns>
        public int Read(byte[] buffer, int offset, int count)
        {
            if (_status != EDeviceStatus.Open)
                return -1;
            
            UpdateTime();
            
            // RTC数据格式：4字节年 + 1字节月 + 1字节日 + 1字节时 + 1字节分 + 1字节秒 + 1字节星期 = 10字节
            int bytesToRead = Math.Min(count, 10);
            
            if (bytesToRead >= 4)
            {
                BitConverter.GetBytes(_currentTime.Year).CopyTo(buffer, offset);
            }
            
            if (bytesToRead >= 5)
            {
                buffer[offset + 4] = (byte)_currentTime.Month;
            }
            
            if (bytesToRead >= 6)
            {
                buffer[offset + 5] = (byte)_currentTime.Day;
            }
            
            if (bytesToRead >= 7)
            {
                int hour = _currentTime.Hour;
                if (!_is24HourFormat && hour > 12)
                    hour -= 12;
                buffer[offset + 6] = (byte)hour;
            }
            
            if (bytesToRead >= 8)
            {
                buffer[offset + 7] = (byte)_currentTime.Minute;
            }
            
            if (bytesToRead >= 9)
            {
                buffer[offset + 8] = (byte)_currentTime.Second;
            }
            
            if (bytesToRead >= 10)
            {
                buffer[offset + 9] = (byte)((int)_currentTime.DayOfWeek);
            }
            
            return bytesToRead;
        }
        
        /// <summary>
        /// 写入RTC数据（设置时间）
        /// </summary>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="count">要写入的字节数</param>
        /// <returns>实际写入的字节数</returns>
        public int Write(byte[] buffer, int offset, int count)
        {
            if (_status != EDeviceStatus.Open)
                return -1;
            
            // 需要至少10字节来设置完整时间
            if (count < 10)
                return -1;
            
            try
            {
                int year = BitConverter.ToInt32(buffer, offset);
                byte month = buffer[offset + 4];
                byte day = buffer[offset + 5];
                byte hour = buffer[offset + 6];
                byte minute = buffer[offset + 7];
                byte second = buffer[offset + 8];
                
                // 验证时间有效性
                if (year < 1900 || year > 2100 ||
                    month < 1 || month > 12 ||
                    day < 1 || day > 31 ||
                    hour < 0 || hour > 23 ||
                    minute < 0 || minute > 59 ||
                    second < 0 || second > 59)
                {
                    return -1;
                }
                
                _currentTime = new DateTime(year, month, day, hour, minute, second);
                return 10;
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
                case 0: // 获取完整时间信息
                    return GetFullTimeInfo(data);
                    
                case 1: // 设置时间格式
                    return SetTimeFormat(data);
                    
                case 2: // 获取时间戳
                    return GetTimestamp(data);
                    
                case 3: // 设置闹钟
                    return SetAlarm(data);
                    
                case 4: // 清除闹钟
                    return ClearAlarm();
                    
                case 5: // 获取日期字符串
                    return GetDateString(data);
                    
                case 6: // 获取时间字符串
                    return GetTimeString(data);
                    
                default:
                    return -1;
            }
        }
        
        /// <summary>
        /// 更新时间
        /// </summary>
        private void UpdateTime()
        {
            _currentTime = DateTime.Now;
        }
        
        /// <summary>
        /// 获取完整时间信息
        /// </summary>
        /// <param name="data">数据缓冲区</param>
        /// <returns>执行结果</returns>
        private int GetFullTimeInfo(byte[] data)
        {
            if (data == null || data.Length < 20)
                return -1;
            
            UpdateTime();
            
            // 写入年、月、日、时、分、秒、毫秒、星期
            BitConverter.GetBytes(_currentTime.Year).CopyTo(data, 0);
            data[4] = (byte)_currentTime.Month;
            data[5] = (byte)_currentTime.Day;
            data[6] = (byte)_currentTime.Hour;
            data[7] = (byte)_currentTime.Minute;
            data[8] = (byte)_currentTime.Second;
            data[9] = (byte)_currentTime.Millisecond;
            data[10] = (byte)((int)_currentTime.DayOfWeek);
            
            // 写入时间格式标志
            data[11] = (byte)(_is24HourFormat ? 1 : 0);
            
            // 写入时间戳（从1970-01-01开始的秒数）
            long timestamp = (long)(_currentTime - new DateTime(1970, 1, 1)).TotalSeconds;
            BitConverter.GetBytes(timestamp).CopyTo(data, 12);
            
            return 0;
        }
        
        /// <summary>
        /// 设置时间格式
        /// </summary>
        /// <param name="data">格式数据</param>
        /// <returns>执行结果</returns>
        private int SetTimeFormat(byte[] data)
        {
            if (data == null || data.Length < 1)
                return -1;
            
            _is24HourFormat = data[0] != 0;
            return 0;
        }
        
        /// <summary>
        /// 获取时间戳
        /// </summary>
        /// <param name="data">数据缓冲区</param>
        /// <returns>执行结果</returns>
        private int GetTimestamp(byte[] data)
        {
            if (data == null || data.Length < 8)
                return -1;
            
            UpdateTime();
            long timestamp = (long)(_currentTime - new DateTime(1970, 1, 1)).TotalSeconds;
            BitConverter.GetBytes(timestamp).CopyTo(data, 0);
            
            return 0;
        }
        
        /// <summary>
        /// 设置闹钟
        /// </summary>
        /// <param name="data">闹钟数据</param>
        /// <returns>执行结果</returns>
        private int SetAlarm(byte[] data)
        {
            // 简化实现：在实际硬件中，这里会设置硬件闹钟
            // 在模拟环境中，我们只记录闹钟时间
            return 0;
        }
        
        /// <summary>
        /// 清除闹钟
        /// </summary>
        /// <returns>执行结果</returns>
        private int ClearAlarm()
        {
            // 简化实现：清除闹钟
            return 0;
        }
        
        /// <summary>
        /// 获取日期字符串
        /// </summary>
        /// <param name="data">数据缓冲区</param>
        /// <returns>执行结果</returns>
        private int GetDateString(byte[] data)
        {
            if (data == null)
                return -1;
            
            UpdateTime();
            string dateStr = _currentTime.ToString("yyyy-MM-dd");
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(dateStr);
            
            if (data.Length < bytes.Length + 1)
                return -1;
            
            Array.Copy(bytes, 0, data, 0, bytes.Length);
            data[bytes.Length] = 0; // 字符串结束符
            
            return bytes.Length + 1;
        }
        
        /// <summary>
        /// 获取时间字符串
        /// </summary>
        /// <param name="data">数据缓冲区</param>
        /// <returns>执行结果</returns>
        private int GetTimeString(byte[] data)
        {
            if (data == null)
                return -1;
            
            UpdateTime();
            string timeStr = _is24HourFormat 
                ? _currentTime.ToString("HH:mm:ss") 
                : _currentTime.ToString("hh:mm:ss tt");
            
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(timeStr);
            
            if (data.Length < bytes.Length + 1)
                return -1;
            
            Array.Copy(bytes, 0, data, 0, bytes.Length);
            data[bytes.Length] = 0; // 字符串结束符
            
            return bytes.Length + 1;
        }
    }
}
