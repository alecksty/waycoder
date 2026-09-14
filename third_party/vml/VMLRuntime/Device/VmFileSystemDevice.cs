using System.Text;

namespace VMLRuntime.Device
{
    /// <summary>
    /// 文件系统设备 - 提供文件操作功能（简化版本）
    /// </summary>
    public class VmFileSystemDevice : IDevice
    {
        #region 属性

        private EDeviceStatus _status;
        private string _basePath;
        private readonly Dictionary<int, FileStream> _openFiles;
        private int _nextFileHandle;

        /// <summary>
        /// 设备名称
        /// </summary>
        public string Name => "fs";

        /// <summary>
        /// 设备类型
        /// </summary>
        public EDeviceType Type => EDeviceType.FileSystem;

        /// <summary>
        /// 设备状态
        /// </summary>
        public EDeviceStatus Status => _status;

        /// <summary>
        /// 基础路径
        /// </summary>
        public string BasePath => _basePath;

        #endregion

        #region 基本方法

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="basePath">基础路径</param>
        public VmFileSystemDevice(string basePath = "")
        {
            _status = EDeviceStatus.Closed;
            _basePath = string.IsNullOrEmpty(basePath) ? Directory.GetCurrentDirectory() : basePath;
            _openFiles = new Dictionary<int, FileStream>();
            _nextFileHandle = 1;
        }

        /// <summary>
        /// 打开设备
        /// </summary>
        /// <returns>是否成功</returns>
        public bool Open()
        {
            try
            {
                // 确保基础路径存在
                if (!Directory.Exists(_basePath))
                {
                    Directory.CreateDirectory(_basePath);
                }

                _status = EDeviceStatus.Open;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 关闭设备
        /// </summary>
        public void Close()
        {
            // 关闭所有打开的文件（安全遍历: 先复制 Keys 防止枚举期间修改）
            foreach (var fileHandle in _openFiles.Keys.ToList())
            {
                CloseFile(fileHandle);
            }

            _openFiles.Clear();

            _status = EDeviceStatus.Closed;
        }

        /// <summary>
        /// 读取文件系统数据（不支持直接读取）
        /// </summary>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="count">要读取的字节数</param>
        /// <returns>实际读取的字节数</returns>
        public int Read(byte[] buffer, int offset, int count)
        {
            // 文件系统设备不支持直接读取
            return -1;
        }

        /// <summary>
        /// 写入文件系统数据（不支持直接写入）
        /// </summary>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="count">要写入的字节数</param>
        /// <returns>实际写入的字节数</returns>
        public int Write(byte[] buffer, int offset, int count)
        {
            // 文件系统设备不支持直接写入
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
                case 0: // 打开文件
                    return OpenFile(data);

                case 1: // 关闭文件
                    return CloseFile(data);

                case 2: // 读取文件
                    return ReadFile(data);

                case 3: // 写入文件
                    return WriteFile(data);

                case 4: // 移动文件指针
                    return SeekFile(data);

                case 5: // 获取文件信息
                    return GetFileInfo(data);

                case 6: // 删除文件
                    return DeleteFile(data);

                case 7: // 创建目录
                    return CreateDirectory(data);

                case 8: // 删除目录
                    return DeleteDirectory(data);

                case 9: // 列出目录内容
                    return ListDirectory(data);

                default:
                    return -1;
            }
        }

        #endregion

        #region 专有方法

        /// <summary>
        /// 获取完整路径
        /// </summary>
        /// <param name="path">相对路径</param>
        /// <returns>完整路径</returns>
        private string GetFullPath(string path)
        {
            if (Path.IsPathRooted(path))
                return path;

            return Path.Combine(_basePath, path);
        }

        /// <summary>
        /// 打开文件
        /// </summary>
        /// <param name="data">文件数据 [mode, path...]</param>
        /// <returns>文件句柄，失败返回-1</returns>
        private int OpenFile(byte[] data)
        {
            if (data == null || data.Length < 2)
                return -1;

            var mode = data[0];
            var path = Encoding.UTF8.GetString(data, 1, data.Length - 1).TrimEnd('\0');

            if (string.IsNullOrEmpty(path))
                return -1;

            var fullPath = GetFullPath(path);

            try
            {
                FileMode fileMode;
                FileAccess fileAccess;

                switch (mode)
                {
                    case 0: // 只读
                        fileMode = FileMode.Open;
                        fileAccess = FileAccess.Read;
                        break;

                    case 1: // 只写（创建或截断）
                        fileMode = FileMode.Create;
                        fileAccess = FileAccess.Write;
                        break;

                    case 2: // 读写（创建或打开）
                        fileMode = FileMode.OpenOrCreate;
                        fileAccess = FileAccess.ReadWrite;
                        break;

                    case 3: // 追加
                        fileMode = FileMode.Append;
                        fileAccess = FileAccess.Write;
                        break;

                    default:
                        return -1;
                }

                FileStream fileStream = new FileStream(fullPath, fileMode, fileAccess);
                int handle = _nextFileHandle++;
                _openFiles[handle] = fileStream;

                return handle;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 关闭文件
        /// </summary>
        /// <param name="data">文件句柄</param>
        /// <returns>执行结果</returns>
        private int CloseFile(byte[] data)
        {
            if (data == null || data.Length < 4)
                return -1;

            var handle = BitConverter.ToInt32(data, 0);
            return CloseFile(handle);
        }

        /// <summary>
        /// 关闭文件
        /// </summary>
        /// <param name="handle">文件句柄</param>
        /// <returns>执行结果</returns>
        private int CloseFile(int handle)
        {
            if (!_openFiles.TryGetValue(handle, out var fileStream))
                return -1;

            try
            {
                fileStream.Close();
                _openFiles.Remove(handle);
                return 0;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 读取文件
        /// </summary>
        /// <param name="data">读取参数 [handle, offset, count, buffer...]</param>
        /// <returns>实际读取的字节数，失败返回-1</returns>
        private int ReadFile(byte[] data)
        {
            // 简化实现：在实际应用中需要更复杂的参数解析
            return -1;
        }

        /// <summary>
        /// 写入文件
        /// </summary>
        /// <param name="data">写入参数 [handle, offset, count, buffer...]</param>
        /// <returns>实际写入的字节数，失败返回-1</returns>
        private int WriteFile(byte[] data)
        {
            // 简化实现：在实际应用中需要更复杂的参数解析
            return -1;
        }

        /// <summary>
        /// 移动文件指针
        /// </summary>
        /// <param name="data">移动参数 [handle, offset, origin]</param>
        /// <returns>新的文件位置，失败返回-1</returns>
        private int SeekFile(byte[] data)
        {
            // 简化实现
            return -1;
        }

        /// <summary>
        /// 获取文件信息
        /// </summary>
        /// <param name="data">文件路径</param>
        /// <returns>执行结果</returns>
        private int GetFileInfo(byte[] data)
        {
            // 简化实现
            return -1;
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="data">文件路径</param>
        /// <returns>执行结果</returns>
        private int DeleteFile(byte[] data)
        {
            if (data == null || data.Length == 0)
                return -1;

            string path = Encoding.UTF8.GetString(data).TrimEnd('\0');
            string fullPath = GetFullPath(path);

            try
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return 0;
                }

                return -1;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 创建目录
        /// </summary>
        /// <param name="data">目录路径</param>
        /// <returns>执行结果</returns>
        private int CreateDirectory(byte[] data)
        {
            if (data == null || data.Length == 0)
                return -1;

            string path = Encoding.UTF8.GetString(data).TrimEnd('\0');
            string fullPath = GetFullPath(path);

            try
            {
                Directory.CreateDirectory(fullPath);
                return 0;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 删除目录
        /// </summary>
        /// <param name="data">目录路径</param>
        /// <returns>执行结果</returns>
        private int DeleteDirectory(byte[] data)
        {
            if (data == null || data.Length == 0)
                return -1;

            string path = Encoding.UTF8.GetString(data).TrimEnd('\0');
            string fullPath = GetFullPath(path);

            try
            {
                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true);
                    return 0;
                }

                return -1;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 列出目录内容
        /// </summary>
        /// <param name="data">目录路径和缓冲区</param>
        /// <returns>执行结果</returns>
        private int ListDirectory(byte[] data)
        {
            // 简化实现
            return -1;
        }

        #endregion
    }
}
