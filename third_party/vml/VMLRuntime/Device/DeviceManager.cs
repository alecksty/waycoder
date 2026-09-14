using System;
using System.Collections.Generic;

namespace VMLRuntime.Device
{
    /// <summary>
    /// 设备管理器 - 统一管理所有设备
    /// </summary>
    public class DeviceManager
    {
        private static DeviceManager? _instance;
        private static readonly object _instanceLock = new();
        private readonly Dictionary<string, IDevice> _devices;
        private readonly Dictionary<int, IDevice> _deviceHandles;
        private readonly MmioIntervalTree _mmioTree = new();
        private readonly object _lock = new();
        private int _nextHandle;
        
        /// <summary>
        /// 单例实例
        /// </summary>
        public static DeviceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLock)
                    {
                        _instance ??= new DeviceManager();
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// 设备数量
        /// </summary>
        public int DeviceCount => _devices.Count;
        
        /// <summary>MMIO 路由树</summary>
        public MmioIntervalTree MmioTree => _mmioTree;
        
        private DeviceManager()
        {
            _devices = new Dictionary<string, IDevice>();
            _deviceHandles = new Dictionary<int, IDevice>();
            _nextHandle = 1;
            
            // 注册默认设备
            RegisterDefaultDevices();
        }
        
        /// <summary>
        /// 注册默认设备
        /// </summary>
        private void RegisterDefaultDevices()
        {
            // 注册控制台设备
            RegisterDevice("console", new VmConsoleDevice());
            
            // 注册标准输入设备
            RegisterDevice("stdin", new VmConsoleDevice(EConsoleType.Stdin));
            
            // 注册标准输出设备
            RegisterDevice("stdout", new VmConsoleDevice(EConsoleType.Stdout));
            
            // 注册标准错误设备
            RegisterDevice("stderr", new VmConsoleDevice(EConsoleType.Stderr));
            
            // 注册VGA显示设备
            RegisterDevice("vga", new VmDisplayDevice());
            
            // 注册键盘设备
            RegisterDevice("kbd", new VmKeyboardDevice());
            
            // 注册鼠标设备
            RegisterDevice("mouse", new VmMouseDevice());
            
            // 注册定时器设备
            RegisterDevice("timer", new VmTimerDevice());
            
            // 注册实时时钟设备
            RegisterDevice("rtc", new VmRtcDevice());
            
            // 注册文件系统设备
            RegisterDevice("fs", new VmFileSystemDevice());
        }
        
        /// <summary>
        /// 注册设备
        /// </summary>
        /// <param name="name">设备名称</param>
        /// <param name="device">设备实例</param>
        /// <returns>是否成功（如果名称已存在则返回false）</returns>
        public bool RegisterDevice(string name, IDevice device)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("设备名称不能为空", nameof(name));
            
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            
            lock (_lock)
            {
                if (_devices.ContainsKey(name))
                    return false;
                
                _devices[name] = device;
                return true;
            }
        }

        /// <summary>
        /// 注册或替换设备（如果名称已存在则替换）
        /// </summary>
        public void RegisterOrReplaceDevice(string name, IDevice device)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("设备名称不能为空", nameof(name));
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            lock (_lock)
            {
                // 如果已存在同名设备，先从 MMIO 树中移除旧设备
                if (_devices.TryGetValue(name, out var old) && old is IMmioDevice oldMmio)
                    _mmioTree.Remove(oldMmio);

                _devices[name] = device;

                // 如果新设备支持 MMIO，自动注册到 MMIO 路由
                if (device is IMmioDevice newMmio)
                    _mmioTree.Insert(newMmio.MmioStart, newMmio.MmioSize, newMmio);
            }
        }
        
        /// <summary>
        /// 注销设备
        /// </summary>
        /// <param name="name">设备名称</param>
        /// <returns>是否成功</returns>
        public bool UnregisterDevice(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("设备名称不能为空", nameof(name));

            lock (_lock)
            {
                if (!_devices.TryGetValue(name, out var device))
                    return false;

                if (device.Status == EDeviceStatus.Open)
                    device.Close();

                // 同时清理 MMIO 路由
                if (device is IMmioDevice mmioDevice)
                    _mmioTree.Remove(mmioDevice);

                return _devices.Remove(name);
            }
        }
        
        /// <summary>
        /// 获取设备
        /// </summary>
        /// <param name="name">设备名称</param>
        /// <returns>设备实例，如果不存在返回null</returns>
        public IDevice? GetDevice(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("设备名称不能为空", nameof(name));
            
            return _devices.TryGetValue(name, out var device) ? device : null;
        }
        
        /// <summary>
        /// 打开设备
        /// </summary>
        /// <param name="name">设备名称</param>
        /// <returns>设备句柄，如果失败返回-1</returns>
        public int OpenDevice(string name)
        {
            var device = GetDevice(name);
            if (device == null)
                return -1;
            
            if (!device.Open())
                return -1;
            
            lock (_lock)
            {
                int handle = _nextHandle++;
                _deviceHandles[handle] = device;
                return handle;
            }
        }
        
        /// <summary>
        /// 通过句柄获取设备
        /// </summary>
        /// <param name="handle">设备句柄</param>
        /// <returns>设备实例，如果不存在返回null</returns>
        public IDevice? GetDeviceByHandle(int handle)
        {
            return _deviceHandles.TryGetValue(handle, out var device) ? device : null;
        }
        
        /// <summary>
        /// 关闭设备
        /// </summary>
        /// <param name="handle">设备句柄</param>
        /// <returns>是否成功</returns>
        public bool CloseDevice(int handle)
        {
            lock (_lock)
            {
                if (!_deviceHandles.TryGetValue(handle, out var device))
                    return false;
                
                device.Close();
                return _deviceHandles.Remove(handle);
            }
        }
        
        /// <summary>
        /// 读取设备数据
        /// </summary>
        /// <param name="handle">设备句柄</param>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="count">要读取的字节数</param>
        /// <returns>实际读取的字节数，如果失败返回-1</returns>
        public int ReadDevice(int handle, byte[] buffer, int offset, int count)
        {
            if (!_deviceHandles.TryGetValue(handle, out var device))
                return -1;
            
            if (device.Status != EDeviceStatus.Open)
                return -1;
            
            return device.Read(buffer, offset, count);
        }
        
        /// <summary>
        /// 写入设备数据
        /// </summary>
        /// <param name="handle">设备句柄</param>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="count">要写入的字节数</param>
        /// <returns>实际写入的字节数，如果失败返回-1</returns>
        public int WriteDevice(int handle, byte[] buffer, int offset, int count)
        {
            if (!_deviceHandles.TryGetValue(handle, out var device))
                return -1;
            
            if (device.Status != EDeviceStatus.Open)
                return -1;
            
            return device.Write(buffer, offset, count);
        }
        
        /// <summary>
        /// 控制设备
        /// </summary>
        /// <param name="handle">设备句柄</param>
        /// <param name="command">控制命令</param>
        /// <param name="data">命令数据</param>
        /// <returns>命令执行结果，如果失败返回-1</returns>
        public int ControlDevice(int handle, int command, byte[] data)
        {
            if (!_deviceHandles.TryGetValue(handle, out var device))
                return -1;
            
            if (device.Status != EDeviceStatus.Open)
                return -1;
            
            return device.Control(command, data);
        }
        
        /// <summary>注册 MMIO 设备到地址路由</summary>
        public void RegisterMmio(IMmioDevice device)
        {
            if (device.MmioSize == 0) return;
            lock (_lock) { _mmioTree.Insert(device.MmioStart, device.MmioSize, device); }
        }

        /// <summary>注销 MMIO 设备地址路由</summary>
        public void UnregisterMmio(IMmioDevice device)
        {
            lock (_lock) { _mmioTree.Remove(device); }
        }

        /// <summary>尝试路由 MMIO 读操作。返回 true 表示已由设备处理。</summary>
        public bool TryRouteMmioRead(uint address, int size, out ulong value)
        {
            var dev = _mmioTree.Find(address);
            if (dev != null)
            {
                if ((dev.MmioAccess & MmioAccess.Read) == 0)
                    throw new VmlMemoryException($"MMIO 读取拒绝: 设备 [0x{dev.MmioStart:X8}+0x{dev.MmioSize:X8}] 不可读", address);
                value = dev.ReadMmio(address - dev.MmioStart, size);
                return true;
            }
            value = 0;
            return false;
        }

        /// <summary>尝试路由 MMIO 写操作。返回 true 表示已由设备处理。</summary>
        int _mmioCount = 0;
        /// <summary>启用 MMIO 调试追踪（每个 VM 实例独立）</summary>
        public bool DebugTraceEnabled { get; set; } = false;
        bool _debugTraceEnabled => DebugTraceEnabled;
        public bool TryRouteMmioWrite(uint address, ulong value, int size)
        {
            var dev = _mmioTree.Find(address);
            if (dev != null)
            {
                if ((dev.MmioAccess & MmioAccess.Write) == 0)
                    throw new VmlMemoryException($"MMIO 写入拒绝: 设备 [0x{dev.MmioStart:X8}+0x{dev.MmioSize:X8}] 不可写", address);
                dev.WriteMmio(address - dev.MmioStart, value, size);
                // Debug-only MMIO trace (first 20 non-zero writes)
                if (_debugTraceEnabled) {
                    _mmioCount++;
                    if (value != 0 && _mmioCount <= 20)
                        Console.Error.WriteLine($"[MMIO W!] addr=0x{address:X6} val=0x{value:X2} cnt={_mmioCount}");
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取所有设备名称
        /// </summary>
        /// <returns>设备名称列表</returns>
        public List<string> GetAllDeviceNames()
        {
            return new List<string>(_devices.Keys);
        }
        
        /// <summary>
        /// 获取所有打开的设备的句柄
        /// </summary>
        /// <returns>设备句柄列表</returns>
        public List<int> GetAllOpenDeviceHandles()
        {
            return new List<int>(_deviceHandles.Keys);
        }
        
        /// <summary>
        /// 重置设备管理器（主要用于测试）
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                foreach (var handle in _deviceHandles)
                {
                    handle.Value.Close();
                }
                
                _devices.Clear();
                _deviceHandles.Clear();
                _mmioTree.Clear();
                _nextHandle = 1;
                
                RegisterDefaultDevices();
            }
        }

        /// <summary>
        /// 根据设备名称查找设备
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <returns>设备实例，如果不存在返回null</returns>
        public IDevice? FindDevice(string deviceName)
        {
            return _devices.GetValueOrDefault(deviceName);
        }
    }
}
