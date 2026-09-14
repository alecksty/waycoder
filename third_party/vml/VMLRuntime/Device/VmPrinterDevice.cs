using System;
using System.Collections.Generic;
using System.Text;

namespace VMLRuntime.Device
{
    /// <summary>
    /// 打印机设备 - 提供打印输出功能
    /// </summary>
    public class VmPrinterDevice : IDevice
    {
        #region 属性
        private EDeviceStatus _status;
        private readonly StringBuilder _printBuffer;
        private readonly Queue<byte> _dataBuffer;
        private int _paperWidth;
        private int _lineCount;
        private bool _paperOut;
        private bool _printerReady;
        private bool _printerError;
        
        /// <summary>
        /// 设备名称
        /// </summary>
        public string Name => "printer";
        
        /// <summary>
        /// 设备类型
        /// </summary>
        public EDeviceType Type => EDeviceType.Printer;
        
        /// <summary>
        /// 设备状态
        /// </summary>
        public EDeviceStatus Status => _status;
        
        /// <summary>
        /// 获取打印缓冲区内容
        /// </summary>
        public string PrintBuffer => _printBuffer.ToString();
        
        /// <summary>
        /// 获取行数
        /// </summary>
        public int LineCount => _lineCount;
        
        /// <summary>
        /// 是否缺纸
        /// </summary>
        public bool PaperOut => _paperOut;
        
        /// <summary>
        /// 打印机是否就绪
        /// </summary>
        public bool PrinterReady => _printerReady;
        
        /// <summary>
        /// 打印机是否有错误
        /// </summary>
        public bool PrinterError => _printerError;
        #endregion
        
        #region 基本方法
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="paperWidth">纸张宽度（字符数）</param>
        public VmPrinterDevice(int paperWidth = 80)
        {
            _status = EDeviceStatus.Closed;
            _printBuffer = new StringBuilder();
            _dataBuffer = new Queue<byte>();
            _paperWidth = paperWidth;
            _lineCount = 0;
            _paperOut = false;
            _printerReady = true;
            _printerError = false;
        }
        
        /// <summary>
        /// 打开设备
        /// </summary>
        /// <returns>是否成功</returns>
        public bool Open()
        {
            if (_printerError)
                return false;
                
            _status = EDeviceStatus.Open;
            _printerReady = true;
            return true;
        }
        
        /// <summary>
        /// 关闭设备
        /// </summary>
        public void Close()
        {
            _status = EDeviceStatus.Closed;
            _dataBuffer.Clear();
        }
        
        /// <summary>
        /// 读取数据（打印机通常不支持读取）
        /// </summary>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="count">要读取的字节数</param>
        /// <returns>实际读取的字节数</returns>
        public int Read(byte[] buffer, int offset, int count)
        {
            if (_status != EDeviceStatus.Open)
                return -1;
                
            // 打印机通常不支持读取，但可以返回状态信息
            if (count <= 0)
                return 0;
                
            // 如果有数据在缓冲区，返回数据
            int bytesRead = 0;
            for (int i = 0; i < count && _dataBuffer.Count > 0; i++)
            {
                buffer[offset + i] = _dataBuffer.Dequeue();
                bytesRead++;
            }
            
            return bytesRead;
        }
        
        /// <summary>
        /// 写入数据（打印输出）
        /// </summary>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="count">要写入的字节数</param>
        /// <returns>实际写入的字节数</returns>
        public int Write(byte[] buffer, int offset, int count)
        {
            if (_status != EDeviceStatus.Open)
                return -1;
                
            if (_paperOut || _printerError)
                return -1;
                
            try
            {
                var text = Encoding.UTF8.GetString(buffer, offset, count);
                ProcessPrintText(text);
                return count;
            }
            catch
            {
                _printerError = true;
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
                case 0: // 获取打印机状态
                    return GetPrinterStatus(data);
                    
                case 1: // 换行
                    _printBuffer.AppendLine();
                    _lineCount++;
                    return 0;
                    
                case 2: // 换页
                    _printBuffer.AppendLine(new string('-', _paperWidth));
                    _printBuffer.AppendLine();
                    _lineCount += 2;
                    return 0;
                    
                case 3: // 设置纸张宽度
                    if (data != null && data.Length >= 4)
                    {
                        int width = BitConverter.ToInt32(data, 0);
                        if (width > 0 && width <= 255)
                        {
                            _paperWidth = width;
                            return 0;
                        }
                    }
                    return -1;
                    
                case 4: // 获取纸张宽度
                    if (data != null && data.Length >= 4)
                    {
                        BitConverter.GetBytes(_paperWidth).CopyTo(data, 0);
                        return 0;
                    }
                    return -1;
                    
                case 5: // 清空打印缓冲区
                    _printBuffer.Clear();
                    _lineCount = 0;
                    return 0;
                    
                case 6: // 模拟缺纸
                    _paperOut = true;
                    return 0;
                    
                case 7: // 模拟装纸
                    _paperOut = false;
                    return 0;
                    
                case 8: // 模拟打印机错误
                    _printerError = true;
                    return 0;
                    
                case 9: // 清除打印机错误
                    _printerError = false;
                    return 0;
                    
                case 10: // 获取打印内容
                    return GetPrintContent(data);
                    
                default:
                    return -1;
            }
        }
        #endregion
        
        #region 专有方法
        /// <summary>
        /// 处理打印文本
        /// </summary>
        /// <param name="text">要打印的文本</param>
        private void ProcessPrintText(string text)
        {
            foreach (char ch in text)
            {
                if (ch == '\n')
                {
                    _printBuffer.AppendLine();
                    _lineCount++;
                }
                else if (ch == '\r')
                {
                    // 忽略回车符，Windows换行符是\r\n
                }
                else if (ch == '\f')
                {
                    // 换页符
                    _printBuffer.AppendLine(new string('-', _paperWidth));
                    _printBuffer.AppendLine();
                    _lineCount += 2;
                }
                else if (ch == '\t')
                {
                    // 制表符，转换为空格
                    _printBuffer.Append("    ");
                }
                else
                {
                    _printBuffer.Append(ch);
                }
            }
            
            // 同时输出到控制台（用于调试）
            Console.Write(text);
        }
        
        /// <summary>
        /// 获取打印机状态
        /// </summary>
        /// <param name="data">数据缓冲区</param>
        /// <returns>执行结果</returns>
        private int GetPrinterStatus(byte[] data)
        {
            if (data == null || data.Length < 4)
                return -1;
                
            int status = 0;
            
            // 位0: 打印机就绪
            if (_printerReady)
                status |= 1;
                
            // 位1: 缺纸
            if (_paperOut)
                status |= 2;
                
            // 位2: 打印机错误
            if (_printerError)
                status |= 4;
                
            // 位3: 缓冲区非空
            if (_printBuffer.Length > 0)
                status |= 8;
                
            // 位4-7: 保留
            
            // 位8-15: 行数（低8位）
            status |= (Math.Min(_lineCount, 255) << 8);
            
            // 位16-23: 纸张宽度
            status |= (Math.Min(_paperWidth, 255) << 16);
            
            BitConverter.GetBytes(status).CopyTo(data, 0);
            return 0;
        }
        
        /// <summary>
        /// 获取打印内容
        /// </summary>
        /// <param name="data">数据缓冲区</param>
        /// <returns>执行结果</returns>
        private int GetPrintContent(byte[] data)
        {
            if (data == null)
                return -1;
                
            string content = _printBuffer.ToString();
            byte[] contentBytes = Encoding.UTF8.GetBytes(content);
            
            if (data.Length < contentBytes.Length)
                return -1;
                
            Array.Copy(contentBytes, 0, data, 0, contentBytes.Length);
            return contentBytes.Length;
        }
        
        /// <summary>
        /// 清空打印缓冲区
        /// </summary>
        public void ClearBuffer()
        {
            _printBuffer.Clear();
            _lineCount = 0;
        }
        
        /// <summary>
        /// 获取打印内容字符串
        /// </summary>
        /// <returns>打印内容</returns>
        public string GetPrintContentString()
        {
            return _printBuffer.ToString();
        }
        #endregion
    }
}