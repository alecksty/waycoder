namespace VMLRuntime
{
    /// <summary>
    /// VML运行时错误码定义
    /// </summary>
    public static class ErrorCodes
    {
        // ============================================
        // 通用错误码 (0-99)
        // ============================================
        
        /// <summary>
        /// 成功
        /// </summary>
        public const int SUCCESS = 0;
        
        /// <summary>
        /// 通用失败
        /// </summary>
        public const int FAILURE = -1;
        
        /// <summary>
        /// 无效参数
        /// </summary>
        public const int INVALID_PARAMETER = -2;
        
        /// <summary>
        /// 内存不足
        /// </summary>
        public const int OUT_OF_MEMORY = -3;
        
        /// <summary>
        /// 权限不足
        /// </summary>
        public const int PERMISSION_DENIED = -4;
        
        /// <summary>
        /// 资源忙
        /// </summary>
        public const int RESOURCE_BUSY = -5;
        
        /// <summary>
        /// 资源不存在
        /// </summary>
        public const int RESOURCE_NOT_FOUND = -6;
        
        /// <summary>
        /// 不支持的操作
        /// </summary>
        public const int NOT_SUPPORTED = -7;
        /// <summary>
        /// 文件操作错误
        /// </summary>
        public const int FILE_ERROR = -8;

        /// <summary>
        /// 内部错误
        /// </summary>
        public const int INTERNAL_ERROR = -9;

        /// <summary>
        /// 超时
        /// </summary>
        public const int TIMEOUT = -10;
        
        // ============================================
        // 系统调用错误码 (100-199)
        // ============================================
        
        /// <summary>
        /// 未知的系统调用
        /// </summary>
        public const int UNKNOWN_SYSCALL = -100;
        
        /// <summary>
        /// 系统调用参数错误
        /// </summary>
        public const int SYSCALL_PARAM_ERROR = -101;
        
        /// <summary>
        /// 系统调用权限不足
        /// </summary>
        public const int SYSCALL_PERMISSION_DENIED = -102;
        
        // ============================================
        // 文件操作错误码 (200-299)
        // ============================================
        
        /// <summary>
        /// 文件不存在
        /// </summary>
        public const int FILE_NOT_FOUND = -200;
        
        /// <summary>
        /// 文件已存在
        /// </summary>
        public const int FILE_ALREADY_EXISTS = -201;
        
        /// <summary>
        /// 文件访问被拒绝
        /// </summary>
        public const int FILE_ACCESS_DENIED = -202;
        
        /// <summary>
        /// 文件已满
        /// </summary>
        public const int FILE_FULL = -203;
        
        /// <summary>
        /// 文件损坏
        /// </summary>
        public const int FILE_CORRUPTED = -204;
        
        /// <summary>
        /// 文件系统错误
        /// </summary>
        public const int FILESYSTEM_ERROR = -205;
        
        /// <summary>
        /// 无效的文件句柄
        /// </summary>
        public const int INVALID_FILE_HANDLE = -206;
        
        /// <summary>
        /// 文件读取错误
        /// </summary>
        public const int FILE_READ_ERROR = -207;
        
        /// <summary>
        /// 文件写入错误
        /// </summary>
        public const int FILE_WRITE_ERROR = -208;
        
        /// <summary>
        /// 文件定位错误
        /// </summary>
        public const int FILE_SEEK_ERROR = -209;
        
        /// <summary>
        /// 文件关闭错误
        /// </summary>
        public const int FILE_CLOSE_ERROR = -210;
        
        /// <summary>
        /// 文件忙（被占用）
        /// </summary>
        public const int FILE_BUSY = -211;
        
        /// <summary>
        /// 文件控制错误
        /// </summary>
        public const int FILE_CONTROL_ERROR = -212;
        
        // ============================================
        // 设备操作错误码 (300-399)
        // ============================================
        
        /// <summary>
        /// 设备不存在
        /// </summary>
        public const int DEVICE_NOT_FOUND = -300;
        
        /// <summary>
        /// 设备忙
        /// </summary>
        public const int DEVICE_BUSY = -301;
        
        /// <summary>
        /// 设备错误
        /// </summary>
        public const int DEVICE_ERROR = -302;
        
        /// <summary>
        /// 设备超时
        /// </summary>
        public const int DEVICE_TIMEOUT = -303;
        
        /// <summary>
        /// 无效的设备句柄
        /// </summary>
        public const int INVALID_DEVICE_HANDLE = -304;
        
        /// <summary>
        /// 设备不支持的操作
        /// </summary>
        public const int DEVICE_NOT_SUPPORTED = -305;
        
        /// <summary>
        /// 设备配置错误
        /// </summary>
        public const int DEVICE_CONFIG_ERROR = -306;
        
        // ============================================
        // 内存操作错误码 (400-499)
        // ============================================
        
        /// <summary>
        /// 内存访问越界
        /// </summary>
        public const int MEMORY_OUT_OF_BOUNDS = -400;
        
        /// <summary>
        /// 内存分配失败
        /// </summary>
        public const int MEMORY_ALLOC_FAILED = -401;
        
        /// <summary>
        /// 内存释放失败
        /// </summary>
        public const int MEMORY_FREE_FAILED = -402;
        
        /// <summary>
        /// 内存保护错误
        /// </summary>
        public const int MEMORY_PROTECTION_ERROR = -403;
        
        /// <summary>
        /// 内存对齐错误
        /// </summary>
        public const int MEMORY_ALIGNMENT_ERROR = -404;
        
        // ============================================
        // 网络操作错误码 (500-599)
        // ============================================
        
        /// <summary>
        /// 网络连接失败
        /// </summary>
        public const int NETWORK_CONNECT_FAILED = -500;
        
        /// <summary>
        /// 网络断开
        /// </summary>
        public const int NETWORK_DISCONNECTED = -501;
        
        /// <summary>
        /// 网络超时
        /// </summary>
        public const int NETWORK_TIMEOUT = -502;
        
        /// <summary>
        /// 网络协议错误
        /// </summary>
        public const int NETWORK_PROTOCOL_ERROR = -503;
        
        /// <summary>
        /// 网络地址错误
        /// </summary>
        public const int NETWORK_ADDRESS_ERROR = -504;
        
        /// <summary>
        /// 网络端口错误
        /// </summary>
        public const int NETWORK_PORT_ERROR = -505;
        
        // ============================================
        // 进程/线程错误码 (600-699)
        // ============================================
        
        /// <summary>
        /// 进程不存在
        /// </summary>
        public const int PROCESS_NOT_FOUND = -600;
        
        /// <summary>
        /// 进程权限不足
        /// </summary>
        public const int PROCESS_PERMISSION_DENIED = -601;
        
        /// <summary>
        /// 进程已终止
        /// </summary>
        public const int PROCESS_TERMINATED = -602;
        
        /// <summary>
        /// 进程资源不足
        /// </summary>
        public const int PROCESS_RESOURCE_LIMIT = -603;
        
        /// <summary>
        /// 线程创建失败
        /// </summary>
        public const int THREAD_CREATE_FAILED = -604;
        
        /// <summary>
        /// 线程终止失败
        /// </summary>
        public const int THREAD_TERMINATE_FAILED = -605;
        
        /// <summary>
        /// 线程同步错误
        /// </summary>
        public const int THREAD_SYNC_ERROR = -606;

        // ============================================
        // FFI / 动态库错误码 (700-799)
        // ============================================

        public const int DLL_LOAD_FAILED = -700;
        public const int DLL_SYMBOL_NOT_FOUND = -701;
        public const int DLL_CALL_ERROR = -702;
        public const int DLL_UNLOAD_FAILED = -703;
        public const int DLL_INVALID_HANDLE = -704;

        // ============================================
        // 工具函数
        // ============================================
        
        /// <summary>
        /// 获取错误码的描述
        /// </summary>
        /// <param name="errorCode">错误码</param>
        /// <returns>错误描述</returns>
        public static string GetDescription(int errorCode)
        {
            return errorCode switch
            {
                SUCCESS => "操作成功",
                FAILURE => "操作失败",
                INVALID_PARAMETER => "无效参数",
                OUT_OF_MEMORY => "内存不足",
                PERMISSION_DENIED => "权限不足",
                RESOURCE_BUSY => "资源忙",
                RESOURCE_NOT_FOUND => "资源不存在",
                NOT_SUPPORTED => "不支持的操作",
                FILE_ERROR => "文件操作错误",
                TIMEOUT => "操作超时",
                INTERNAL_ERROR => "内部错误",
                
                UNKNOWN_SYSCALL => "未知的系统调用",
                SYSCALL_PARAM_ERROR => "系统调用参数错误",
                SYSCALL_PERMISSION_DENIED => "系统调用权限不足",
                
                FILE_NOT_FOUND => "文件不存在",
                FILE_ALREADY_EXISTS => "文件已存在",
                FILE_ACCESS_DENIED => "文件访问被拒绝",
                FILE_FULL => "文件已满",
                FILE_CORRUPTED => "文件损坏",
                FILESYSTEM_ERROR => "文件系统错误",
                INVALID_FILE_HANDLE => "无效的文件句柄",
                FILE_READ_ERROR => "文件读取错误",
                FILE_WRITE_ERROR => "文件写入错误",
                FILE_SEEK_ERROR => "文件定位错误",
                FILE_CLOSE_ERROR => "文件关闭错误",
                FILE_BUSY => "文件忙（被占用）",
                FILE_CONTROL_ERROR => "文件控制错误",
                
                DEVICE_NOT_FOUND => "设备不存在",
                DEVICE_BUSY => "设备忙",
                DEVICE_ERROR => "设备错误",
                DEVICE_TIMEOUT => "设备超时",
                INVALID_DEVICE_HANDLE => "无效的设备句柄",
                DEVICE_NOT_SUPPORTED => "设备不支持的操作",
                DEVICE_CONFIG_ERROR => "设备配置错误",
                
                MEMORY_OUT_OF_BOUNDS => "内存访问越界",
                MEMORY_ALLOC_FAILED => "内存分配失败",
                MEMORY_FREE_FAILED => "内存释放失败",
                MEMORY_PROTECTION_ERROR => "内存保护错误",
                MEMORY_ALIGNMENT_ERROR => "内存对齐错误",
                
                NETWORK_CONNECT_FAILED => "网络连接失败",
                NETWORK_DISCONNECTED => "网络断开",
                NETWORK_TIMEOUT => "网络超时",
                NETWORK_PROTOCOL_ERROR => "网络协议错误",
                NETWORK_ADDRESS_ERROR => "网络地址错误",
                NETWORK_PORT_ERROR => "网络端口错误",
                
                PROCESS_NOT_FOUND => "进程不存在",
                PROCESS_PERMISSION_DENIED => "进程权限不足",
                PROCESS_TERMINATED => "进程已终止",
                PROCESS_RESOURCE_LIMIT => "进程资源不足",
                THREAD_CREATE_FAILED => "线程创建失败",
                THREAD_TERMINATE_FAILED => "线程终止失败",
                THREAD_SYNC_ERROR => "线程同步错误",

                DLL_LOAD_FAILED => "动态库加载失败",
                DLL_SYMBOL_NOT_FOUND => "动态库符号未找到",
                DLL_CALL_ERROR => "动态库调用错误",
                DLL_UNLOAD_FAILED => "动态库卸载失败",
                DLL_INVALID_HANDLE => "无效的动态库句柄",

                _ => $"未知错误码: {errorCode}"
            };
        }
        
        /// <summary>
        /// 检查错误码是否表示成功
        /// </summary>
        /// <param name="errorCode">错误码</param>
        /// <returns>是否成功</returns>
        public static bool IsSuccess(int errorCode) => errorCode >= 0;
        
        /// <summary>
        /// 检查错误码是否表示失败
        /// </summary>
        /// <param name="errorCode">错误码</param>
        /// <returns>是否失败</returns>
        public static bool IsFailure(int errorCode) => errorCode < 0;
        
        /// <summary>
        /// 获取错误类别
        /// </summary>
        /// <param name="errorCode">错误码</param>
        /// <returns>错误类别</returns>
        public static string GetCategory(int errorCode)
        {
            if (errorCode >= 0) return "Success";
            
            int absCode = Math.Abs(errorCode);
            
            if (absCode < 100) return "General";
            if (absCode < 200) return "SystemCall";
            if (absCode < 300) return "File";
            if (absCode < 400) return "Device";
            if (absCode < 500) return "Memory";
            if (absCode < 600) return "Network";
            if (absCode < 700) return "Process";
            if (absCode < 800) return "FFI";

            return "Unknown";
        }
    }
}