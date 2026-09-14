using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using VMLAssembler;
using System.IO;

namespace VMLRuntime
{
    public partial class VmRuntime
    {
        // ======== FFI / 动态库 SYSCALL 实现 ========

        private void ExecuteDLOpen()
        {
            // AOT 模式下 FFI 不可用，提前返回友好错误
            if (!EnsureFfiAvailable())
            {
                registers[0] = 0; // 返回 null handle
                return;
            }
            string path = ReadStringFromMemory(registers[0]);
            try
            {
                IntPtr handle;
                // 绝对路径 → 直接加载 (跳过搜索链)
                if (Path.IsPathRooted(path) && File.Exists(path))
                {
                    handle = NativeLibrary.Load(path);
                }
                else
                {
                    string baseName = NormalizeLibraryPath(path);
                    handle = TryLoadWithArchFallback(baseName);
                }
                int id = _nextNativeHandle++;
                _nativeHandles[id] = handle;
                registers[0] = id;
            }
            catch (Exception)
            {
                registers[0] = ErrorCodes.DLL_LOAD_FAILED;
            }
        }

        /// <summary>
        /// 规范化库路径: 剥离已知共享库扩展名 (.dylib/.so/.dll)，
        /// 让搜索链根据平台自动追加正确的扩展名。
        /// 保留目录部分 (如 "Lib/dynamic/mylib.so" → "Lib/dynamic/mylib")
        /// </summary>
        private static string NormalizeLibraryPath(string path)
        {
            string[] knownExtensions = { ".dylib", ".so", ".dll" };
            foreach (var ext in knownExtensions)
            {
                if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    // 确保扩展名后面不是路经分隔符 (即扩展名是真正的后缀，而非目录名)
                    int lastSep = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
                    if (path.Length - ext.Length > lastSep + 1)
                        return path.Substring(0, path.Length - ext.Length);
                }
            }
            return path;
        }

        /// <summary>
        /// 动态库搜索优先级:
        ///   1. 当前应用目录
        ///   2. VML_LIB_PATH 环境变量 (分号/冒号分隔)
        ///   3. $VML_HOME/Lib/dynamic/ (VML 安装根目录)
        ///   4. 系统库路径 (/lib, /usr/lib, /usr/local/lib, C:\Windows\System32 等)
        ///   5. 系统 PATH 环境变量
        ///
        /// 每个目录尝试架构后缀回退: name_x64 → name_x86 → name (不含后缀)
        /// 平台扩展名自动匹配: .dylib (macOS), .so (Linux), .dll (Windows)
        /// </summary>
        private static IntPtr TryLoadWithArchFallback(string baseName)
        {
            var suffixes = new List<string>();
            if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                suffixes.AddRange(new[] { "_x64", "_amd64", "" });
            else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                suffixes.AddRange(new[] { "_arm64", "_aarch64", "" });
            else if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
                suffixes.AddRange(new[] { "_x86", "" });
            else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm)
                suffixes.AddRange(new[] { "_arm", "" });
            else
                suffixes.Add("");

            // 平台扩展名: macOS 需要 .dylib, Linux 需要 .so, Windows 自动处理
            var extSuffixes = new List<string> { "" };
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                extSuffixes.Add(".dylib");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                extSuffixes.Add(".so");
            // Windows: NativeLibrary.TryLoad 自动追加 .dll

            // ──── 构建搜索目录列表 ────
            var searchDirs = new List<string>();

            // 1. 当前应用目录
            searchDirs.Add(AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory);

            // 2. VML_LIB_PATH 环境变量
            string? vmlLibPath = Environment.GetEnvironmentVariable("VML_LIB_PATH");
            if (!string.IsNullOrEmpty(vmlLibPath))
            {
                char sep = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';
                foreach (var p in vmlLibPath.Split(sep, StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = p.Trim();
                    if (Directory.Exists(trimmed))
                        searchDirs.Add(trimmed);
                }
            }

            // 3. VML_HOME/Lib/dynamic/ (VML 安装根目录下的动态库)
            string? vmlHome = Environment.GetEnvironmentVariable("VML_HOME")
                ?? Environment.GetEnvironmentVariable("VML_PATH")
                ?? Environment.GetEnvironmentVariable("VML_ROOT");
            if (!string.IsNullOrEmpty(vmlHome))
            {
                var vmlDynamicDir = Path.Combine(vmlHome, "Lib", "dynamic");
                if (Directory.Exists(vmlDynamicDir))
                    searchDirs.Add(vmlDynamicDir);
                // 也搜 VML_HOME 本身 (直接放 .so/.dll/.dylib 的情况)
                if (Directory.Exists(vmlHome) && !searchDirs.Contains(vmlHome))
                    searchDirs.Add(vmlHome);
            }

            // 4. 系统库路径 (按平台)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                searchDirs.Add(Path.Combine(windir, "System32"));
                searchDirs.Add(Path.Combine(windir, "SysWOW64"));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                searchDirs.Add("/usr/lib");
                searchDirs.Add("/usr/local/lib");
                searchDirs.Add("/opt/homebrew/lib");
            }
            else // Linux
            {
                searchDirs.Add("/lib");
                searchDirs.Add("/usr/lib");
                searchDirs.Add("/usr/local/lib");
                // 多架构支持
                if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                    searchDirs.Add("/lib/x86_64-linux-gnu");
                else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                    searchDirs.Add("/lib/aarch64-linux-gnu");
                else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm)
                    searchDirs.Add("/lib/arm-linux-gnueabihf");
            }

            // 5. 系统 PATH 环境变量
            string? sysPath = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(sysPath))
            {
                char pathSep = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';
                foreach (var p in sysPath.Split(pathSep, StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = p.Trim();
                    if (Directory.Exists(trimmed) && !searchDirs.Contains(trimmed))
                        searchDirs.Add(trimmed);
                }
            }

            // ──── 如果 baseName 已包含目录路径，先在该目录搜索 ────
            string? containedDir = null;
            string fileNameOnly = baseName;
            int lastIdx = Math.Max(baseName.LastIndexOf('/'), baseName.LastIndexOf('\\'));
            if (lastIdx >= 0)
            {
                containedDir = baseName.Substring(0, lastIdx);
                fileNameOnly = baseName.Substring(lastIdx + 1);
            }

            // 带目录的路径优先在原指定目录搜索
            var orderedDirs = new List<string>(searchDirs);
            if (containedDir != null)
                orderedDirs.Insert(0, containedDir);

            // ──── 遍历搜索目录 × 架构后缀 × 平台扩展名 ────
            foreach (var dir in orderedDirs)
            {
                string dirPart = string.IsNullOrEmpty(dir) ? "" : dir;
                foreach (var sfx in suffixes)
                {
                    foreach (var ext in extSuffixes)
                    {
                        string name = string.IsNullOrEmpty(dirPart)
                            ? fileNameOnly + sfx + ext
                            : Path.Combine(dirPart, fileNameOnly + sfx + ext);
                        if (NativeLibrary.TryLoad(name, out IntPtr handle))
                            return handle;
                    }
                }
            }

            // 兜底: 直接传给 OS 加载器 (搜索系统默认路径)
            return NativeLibrary.Load(baseName);
        }

        private void ExecuteDLSym()
        {
            int id = registers[0];
            if (!_nativeHandles.TryGetValue(id, out IntPtr handle))
            {
                registers[0] = ErrorCodes.DLL_INVALID_HANDLE;
                return;
            }
            string name = ReadStringFromMemory(registers[1]);
            if (NativeLibrary.TryGetExport(handle, name, out IntPtr funcPtr))
            {
                int funcId = _nextNativeHandle++;
                _nativeFuncPtrs[funcId] = funcPtr;
                registers[0] = funcId;
            }
            else
            {
                registers[0] = ErrorCodes.DLL_SYMBOL_NOT_FOUND;
            }
        }

        private void ExecuteDLClose()
        {
            int id = registers[0];
            if (!_nativeHandles.TryGetValue(id, out IntPtr handle))
            {
                registers[0] = ErrorCodes.DLL_INVALID_HANDLE;
                return;
            }
            NativeLibrary.Free(handle);
            _nativeHandles.Remove(id);
            registers[0] = ErrorCodes.SUCCESS;
        }

        private IntPtr ReadNativeString(int addr)
        {
            int maxLen = Math.Min(4096, memory.Length - addr);
            int len = 0;
            while (len < maxLen && memory[addr + len] != 0)
                len++;
            byte[] bytes = new byte[len + 1];
            Array.Copy(memory, addr, bytes, 0, len);
            IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            return ptr;
        }

        private void ExecuteGetPlatform()
        {
            int platformId;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                platformId = 0;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                platformId = 1;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                platformId = 2;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
                platformId = 3;
            else
                platformId = -1;

            // 高 16 位 = 架构编码: 0=未知, 1=x86, 2=x64, 3=arm64, 4=arm32
            int arch = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X86 => 1,
                Architecture.X64 => 2,
                Architecture.Arm64 => 3,
                Architecture.Arm => 4,
                _ => 0
            };
            registers[0] = platformId | (arch << 16);
        }

        // ====== NativeCallEx: 统一类型数组 FFI (SYSCALL #376) ======

        /// <summary>FFI 参数类型码，与 VML 编译器侧一致</summary>
        private const int FFI_INT32      = 0;
        private const int FFI_FLOAT32    = 1;
        private const int FFI_INT64      = 2;
        private const int FFI_FLOAT64    = 3;
        private const int FFI_STRING     = 4;
        private const int FFI_STRUCT     = 5; // struct by-value
        private const int FFI_STRUCT_PTR = 6; // struct pointer (VML addr)

        /// <summary>委托缓存: 类型签名 key → Delegate (避免重复 DynamicInvoke 开销)</summary>
        private readonly Dictionary<string, Delegate> _ffiDelegateCache = new();

        // DynamicMethod + Emit 创建非泛型委托类型 (Marshal.GetDelegateForFunctionPointer 不接受泛型类型)
        // 延迟初始化: AOT 模式下 DLOpen 不可用，避免静态构造崩溃
        private static System.Reflection.Emit.ModuleBuilder? _moduleBuilder;
        private static readonly Dictionary<string, Type> _ffiDelegateTypes = new();
        private static int _delegateTypeCounter;
        private static bool _ffiAvailable;
        private static bool _ffiChecked;

        /// <summary>检查 FFI 是否可用（非 Native AOT 且运行时支持 Emit）</summary>
        private static bool EnsureFfiAvailable()
        {
            if (_ffiChecked) return _ffiAvailable;
            _ffiChecked = true;
            try
            {
                if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported)
                {
                    Console.Error.WriteLine("[VML] FFI/DLOpen 在 Native AOT 模式下不可用（需要动态代码生成）");
                    return _ffiAvailable = false;
                }
                var asmName = new System.Reflection.AssemblyName("FfiDelegates");
                var asmBuilder = System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(asmName, System.Reflection.Emit.AssemblyBuilderAccess.Run);
                _moduleBuilder = asmBuilder.DefineDynamicModule("FfiDelegates");
                return _ffiAvailable = true;
            }
            catch (PlatformNotSupportedException)
            {
                Console.Error.WriteLine("[VML] FFI/DLOpen 在当前平台不可用");
                return _ffiAvailable = false;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[VML] FFI 初始化失败: {ex.Message}");
                return _ffiAvailable = false;
            }
        }

        [UnconditionalSuppressMessage("trim", "IL2026", Justification = "FFI 动态委托: Delegate.CreateDelegate 运行时反射创建，无法被静态分析")]
        [UnconditionalSuppressMessage("trim", "IL2111", Justification = "FFI 动态委托: MulticastDelegate 构造器通过反射访问，目标方法由运行时保证")]
        private static Type GetFfiDelegateType(Type[] paramTypes, Type retType)
        {
            // 构建缓存 key
            string key = retType.Name + "(" + string.Join(",", System.Linq.Enumerable.Select(paramTypes, t => t.Name)) + ")";
            if (_ffiDelegateTypes.TryGetValue(key, out Type? cached))
                return cached;

            EnsureFfiAvailable();
            if (_moduleBuilder == null)
                throw new PlatformNotSupportedException("FFI 委托类型创建需要动态代码生成，Native AOT 不支持");

            var tb = _moduleBuilder.DefineType(
                "FfiDelegate_" + Interlocked.Increment(ref _delegateTypeCounter),
                System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Sealed,
                typeof(MulticastDelegate));

            var ctor = tb.DefineConstructor(
                System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.HideBySig |
                System.Reflection.MethodAttributes.SpecialName | System.Reflection.MethodAttributes.RTSpecialName,
                System.Reflection.CallingConventions.Standard,
                new[] { typeof(object), typeof(IntPtr) });
            ctor.SetImplementationFlags(System.Reflection.MethodImplAttributes.Runtime);

            var invoke = tb.DefineMethod("Invoke",
                System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.HideBySig |
                System.Reflection.MethodAttributes.NewSlot | System.Reflection.MethodAttributes.Virtual,
                retType, paramTypes);
            invoke.SetImplementationFlags(System.Reflection.MethodImplAttributes.Runtime);

            // 设置 Stdcall 调用约定（跨平台一致，Windows 上 key 为 StdCall）
            var unmanagedAttrCtor = typeof(UnmanagedFunctionPointerAttribute).GetConstructor(
                new[] { typeof(CallingConvention) });
            if (unmanagedAttrCtor != null)
            {
                var attrBuilder = new System.Reflection.Emit.CustomAttributeBuilder(
                    unmanagedAttrCtor, new object[] { CallingConvention.StdCall });
                tb.SetCustomAttribute(attrBuilder);
            }

            Type result = tb.CreateType()!;
            _ffiDelegateTypes[key] = result;
            return result;
        }

        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050:FFI requires dynamic delegate types")]
        private void ExecuteNativeCallEx()
        {
            int funcId = registers[0];
            int argsPtr     = registers[1];
            int typeDescPtr = registers[2];
            int argCount    = Math.Min(registers[3], 12);
            if (!_nativeFuncPtrs.TryGetValue(funcId, out IntPtr funcPtr))
            {
                registers[0] = ErrorCodes.DLL_INVALID_HANDLE;
                return;
            }
            int retTypeCode = registers[4] & 0xFF;

            if (argsPtr < 0 || typeDescPtr < 0 ||
                argsPtr + argCount * 8 > memory.Length ||
                typeDescPtr + argCount * 4 > memory.Length)
            {
                registers[0] = ErrorCodes.MEMORY_OUT_OF_BOUNDS;
                return;
            }

            // ——— 阶段 0: 预扫描类型描述符，计算展开后的原生参数数量 ———
            var typeCodes = new int[argCount];
            var typeSizes = new int[argCount]; // FFI_STRUCT 的字节大小
            int expandedArgCount = 0;
            int argsByteOffset0 = 0;
            for (int i = 0; i < argCount; i++)
            {
                int tw = GetMemory(typeDescPtr + i * 4);
                int tc = tw & 0xFF;
                typeCodes[i] = tc;
                switch (tc)
                {
                    case FFI_INT32:   expandedArgCount++; argsByteOffset0 += 4; break;
                    case FFI_FLOAT32: expandedArgCount++; argsByteOffset0 += 4; break;
                    case FFI_INT64:   expandedArgCount++; argsByteOffset0 += 8; break;
                    case FFI_FLOAT64: expandedArgCount++; argsByteOffset0 += 8; break;
                    case FFI_STRING:  expandedArgCount++; argsByteOffset0 += 4; break;
                    case FFI_STRUCT_PTR: expandedArgCount++; argsByteOffset0 += 4; break;
                    case FFI_STRUCT:
                    {
                        int sz = (tw >> 8) & 0xFFFF;
                        if (sz == 0) sz = 4;
                        typeSizes[i] = sz;
                        if (sz <= 8)      expandedArgCount += 1;       // 打包为 1 个 long
                        else if (sz <= 16) expandedArgCount += 2;       // 拆分为 2 个 long
                        else               expandedArgCount += 1;       // >16 字节：ABI 传指针
                        argsByteOffset0 += sz;
                        break;
                    }
                    default: typeSizes[i] = 0; break;
                }
            }

            // ——— 阶段 1: 读取类型描述符和参数值 ———
            var paramTypes     = new Type[expandedArgCount];
            var argValues      = new object?[expandedArgCount];
            var nativeStrings  = new IntPtr[expandedArgCount];
            var nativeStructs  = new IntPtr[expandedArgCount];
            var structSizes    = new int[expandedArgCount];
            var structVmlAddrs = new int[expandedArgCount];
            var isStructPtr    = new bool[expandedArgCount];
            int argsByteOffset = 0;
            int nativeIdx = 0;

            for (int i = 0; i < argCount; i++)
            {
                int typeWord = GetMemory(typeDescPtr + i * 4);
                int typeCode = typeCodes[i];

                switch (typeCode)
                {
                    case FFI_INT32:
                        paramTypes[nativeIdx] = typeof(int);
                        argValues[nativeIdx]  = GetMemory(argsPtr + argsByteOffset);
                        argsByteOffset += 4;
                        nativeIdx++;
                        break;

                    case FFI_FLOAT32:
                    {
                        int bits = GetMemory(argsPtr + argsByteOffset);
                        paramTypes[nativeIdx] = typeof(float);
                        argValues[nativeIdx]  = BitConverter.Int32BitsToSingle(bits);
                        argsByteOffset += 4;
                        nativeIdx++;
                        break;
                    }

                    case FFI_INT64:
                    {
                        int lo = GetMemory(argsPtr + argsByteOffset);
                        int hi = GetMemory(argsPtr + argsByteOffset + 4);
                        paramTypes[nativeIdx] = typeof(long);
                        argValues[nativeIdx]  = (long)lo | ((long)hi << 32);
                        argsByteOffset += 8;
                        nativeIdx++;
                        break;
                    }

                    case FFI_FLOAT64:
                    {
                        int lo = GetMemory(argsPtr + argsByteOffset);
                        int hi = GetMemory(argsPtr + argsByteOffset + 4);
                        long bits = (long)lo | ((long)hi << 32);
                        paramTypes[nativeIdx] = typeof(double);
                        argValues[nativeIdx]  = BitConverter.Int64BitsToDouble(bits);
                        argsByteOffset += 8;
                        nativeIdx++;
                        break;
                    }

                    case FFI_STRING:
                    {
                        int strAddr = GetMemory(argsPtr + argsByteOffset);
                        IntPtr nativeStr = ReadNativeString(strAddr);
                        nativeStrings[nativeIdx] = nativeStr;
                        paramTypes[nativeIdx] = typeof(IntPtr);
                        argValues[nativeIdx]  = nativeStr;
                        argsByteOffset += 4;
                        nativeIdx++;
                        break;
                    }

                    case FFI_STRUCT:
                    {
                        int structSize = typeSizes[i];
                        if (structSize == 0) structSize = 4;

                        if (structSize <= 8)
                        {
                            // 打包为单个 long (x86_64: 一个寄存器)
                            long packed = 0;
                            for (int b = 0; b < structSize; b++)
                                packed |= (long)memory[argsPtr + argsByteOffset + b] << (b * 8);
                            paramTypes[nativeIdx] = typeof(long);
                            argValues[nativeIdx] = packed;
                            nativeIdx++;
                        }
                        else if (structSize <= 16)
                        {
                            // 拆分为两个 long (x86_64: 两个寄存器)
                            long lo = 0, hi = 0;
                            for (int b = 0; b < 8 && b < structSize; b++)
                                lo |= (long)memory[argsPtr + argsByteOffset + b] << (b * 8);
                            for (int b = 8; b < structSize; b++)
                                hi |= (long)memory[argsPtr + argsByteOffset + b] << ((b - 8) * 8);
                            paramTypes[nativeIdx] = typeof(long);
                            argValues[nativeIdx] = lo;
                            nativeIdx++;
                            paramTypes[nativeIdx] = typeof(long);
                            argValues[nativeIdx] = hi;
                            nativeIdx++;
                        }
                        else
                        {
                            // >16 字节: ABI 传指针 (调用者分配栈空间)
                            IntPtr nativePtr = Marshal.AllocHGlobal(structSize);
                            Marshal.Copy(memory, argsPtr + argsByteOffset, nativePtr, structSize);
                            nativeStructs[nativeIdx] = nativePtr;
                            structSizes[nativeIdx] = structSize;
                            paramTypes[nativeIdx] = typeof(IntPtr);
                            argValues[nativeIdx] = nativePtr;
                            nativeIdx++;
                        }
                        argsByteOffset += structSize;
                        break;
                    }

                    case FFI_STRUCT_PTR:
                    {
                        int structSize = typeSizes[i];
                        if (structSize == 0) structSize = (typeWord >> 8) & 0xFFFF;
                        if (structSize == 0) structSize = 4;
                        int vmlAddr = GetMemory(argsPtr + argsByteOffset);
                        IntPtr nativePtr = Marshal.AllocHGlobal(structSize);
                        if (vmlAddr >= 0 && vmlAddr + structSize <= memory.Length)
                        {
                            Marshal.Copy(memory, vmlAddr, nativePtr, structSize);
                        }
                        nativeStructs[nativeIdx] = nativePtr;
                        structSizes[nativeIdx] = structSize;
                        structVmlAddrs[nativeIdx] = vmlAddr;
                        isStructPtr[nativeIdx] = true;
                        paramTypes[nativeIdx] = typeof(IntPtr);
                        argValues[nativeIdx] = nativePtr;
                        argsByteOffset += 4;
                        nativeIdx++;
                        break;
                    }

                    default:
                        registers[0] = ErrorCodes.NOT_SUPPORTED;
                        return;
                }
            }

            try
            {
                // ——— 阶段 2: 确定返回类型 ———
                Type retType = retTypeCode switch
                {
                    FFI_INT32   => typeof(int),
                    FFI_INT64   => typeof(long),
                    FFI_FLOAT32 => typeof(float),
                    FFI_FLOAT64 => typeof(double),
                    _           => typeof(void) // 0 或未知 → void
                };

                // ——— 阶段 3: 构建或获取缓存的委托 ———
                string sigKey = $"{funcId}:{retType.Name}:" + string.Join(",", System.Linq.Enumerable.Select(paramTypes, t => t.Name));
                if (!_ffiDelegateCache.TryGetValue(sigKey, out Delegate? del))
                {
                    Type delegateType = GetFfiDelegateType(paramTypes, retType);
del = Marshal.GetDelegateForFunctionPointer(funcPtr, delegateType);
                    _ffiDelegateCache[sigKey] = del;
                }

                // ——— 阶段 4: 调用 ———
                object? result = del.DynamicInvoke(argValues);

                // ——— 阶段 5: 存储返回值 ———
                switch (retTypeCode)
                {
                    case FFI_INT32:
                        registers[0] = result is int ri ? ri : Convert.ToInt32(result);
                        break;
                    case FFI_INT64:
                    {
                        long rl = result is long rll ? rll : Convert.ToInt64(result);
                        registers[0] = (int)(rl & 0xFFFFFFFF);
                        registers[1] = (int)((rl >> 32) & 0xFFFFFFFF);
                        break;
                    }
                    case FFI_FLOAT32:
                        registers[0] = BitConverter.SingleToInt32Bits(
                            result is float rf ? rf : Convert.ToSingle(result));
                        break;
                    case FFI_FLOAT64:
                    {
                        double rd = result is double rdd ? rdd : Convert.ToDouble(result);
                        long bits = BitConverter.DoubleToInt64Bits(rd);
                        registers[0] = (int)(bits & 0xFFFFFFFF);
                        registers[1] = (int)((bits >> 32) & 0xFFFFFFFF);
                        break;
                    }
                    default: // void
                        registers[0] = ErrorCodes.SUCCESS;
                        break;
                }
            }
            catch
            {
                registers[0] = ErrorCodes.DLL_CALL_ERROR;
            }
            finally
            {
                // 释放原生字符串
                for (int i = 0; i < expandedArgCount; i++)
                {
                    if (nativeStrings[i] != IntPtr.Zero)
                        Marshal.FreeHGlobal(nativeStrings[i]);
                }
                // 回拷 + 释放原生 struct 内存
                for (int i = 0; i < expandedArgCount; i++)
                {
                    if (nativeStructs[i] != IntPtr.Zero)
                    {
                        if (isStructPtr[i] && structVmlAddrs[i] >= 0
                            && structVmlAddrs[i] + structSizes[i] <= memory.Length)
                        {
                            Marshal.Copy(nativeStructs[i], memory, structVmlAddrs[i], structSizes[i]);
                        }
                        Marshal.FreeHGlobal(nativeStructs[i]);
                    }
                }
            }
        }

        /// <summary>
        /// SYSCALL #373 — NativeCall(funcId, argsPtr, argCount, flags) -> result
        /// 简化版: 所有参数为 int32，返回 int32 或 float
        /// </summary>
        private void ExecuteNativeCall()
        {
            int funcId = registers[0];
            int argsPtr = registers[1];
            int argCount = Math.Min(registers[2], 12);
            int flags = registers[3];

            if (!_nativeFuncPtrs.TryGetValue(funcId, out IntPtr funcPtr))
            {
                registers[0] = ErrorCodes.DLL_INVALID_HANDLE;
                return;
            }

            if (argsPtr < 0 || argsPtr + argCount * 4 > memory.Length)
            {
                registers[0] = ErrorCodes.MEMORY_OUT_OF_BOUNDS;
                return;
            }

            // 读取参数 (每个 4 字节 int32)
            var args = new int[argCount];
            for (int i = 0; i < argCount; i++)
                args[i] = GetMemory(argsPtr + i * 4);

            bool returnsFloat = (flags & 2) != 0;

            try
            {
                // 通过 NativeCallEx 相同的委托机制调用
                // 构建类型描述符: 所有参数为 FFI_INT32(1), 返回类型依 flags
                var typeDesc = new int[argCount + 1];
                for (int i = 0; i < argCount; i++)
                    typeDesc[i] = 1; // FFI_INT32
                typeDesc[argCount] = returnsFloat ? 3 : 1; // FFI_FLOAT32 : FFI_INT32

                // 委托给 NativeCallEx 的实现逻辑
                CallNativeWithTypeDesc(funcPtr, argsPtr, typeDesc, argCount, returnsFloat);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NativeCall error: {ex.Message}");
                registers[0] = ErrorCodes.DLL_CALL_ERROR;
            }
        }

        /// <summary>
        /// SYSCALL #375 — NativeCallF(funcId, floatArgsPtr, argCount, flags) -> result
        /// 浮点参数版: 所有参数为 float32 (IEEE 754)
        /// </summary>
        private void ExecuteNativeCallF()
        {
            int funcId = registers[0];
            int argsPtr = registers[1];
            int argCount = Math.Min(registers[2], 8);
            int flags = registers[3];

            if (!_nativeFuncPtrs.TryGetValue(funcId, out IntPtr funcPtr))
            {
                registers[0] = ErrorCodes.DLL_INVALID_HANDLE;
                return;
            }

            if (argsPtr < 0 || argsPtr + argCount * 4 > memory.Length)
            {
                registers[0] = ErrorCodes.MEMORY_OUT_OF_BOUNDS;
                return;
            }

            bool voidReturn = (flags & 1) != 0;

            try
            {
                // 读取 float 参数，通过 Marshal 调用
                var floatArgs = new float[argCount];
                for (int i = 0; i < argCount; i++)
                {
                    int bits = GetMemory(argsPtr + i * 4);
                    floatArgs[i] = BitConverter.Int32BitsToSingle(bits);
                }

                // 简单实现: 仅支持 0-4 个 float 参数的函数
                object? result;
                if (argCount == 0) result = Marshal.GetDelegateForFunctionPointer<Func<float>>(funcPtr)();
                else if (argCount == 1) result = Marshal.GetDelegateForFunctionPointer<Func<float, float>>(funcPtr)(floatArgs[0]);
                else if (argCount == 2) result = Marshal.GetDelegateForFunctionPointer<Func<float, float, float>>(funcPtr)(floatArgs[0], floatArgs[1]);
                else if (argCount == 3) result = Marshal.GetDelegateForFunctionPointer<Func<float, float, float, float>>(funcPtr)(floatArgs[0], floatArgs[1], floatArgs[2]);
                else if (argCount == 4) result = Marshal.GetDelegateForFunctionPointer<Func<float, float, float, float, float>>(funcPtr)(floatArgs[0], floatArgs[1], floatArgs[2], floatArgs[3]);
                else { registers[0] = ErrorCodes.INVALID_PARAMETER; return; }

                if (!voidReturn && result is float f)
                    registers[0] = BitConverter.SingleToInt32Bits(f);
                else
                    registers[0] = ErrorCodes.SUCCESS;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NativeCallF error: {ex.Message}");
                registers[0] = ErrorCodes.DLL_CALL_ERROR;
            }
        }

        /// <summary>
        /// 辅助方法: 用类型描述符调用原生函数 (供 NativeCall 复用 NativeCallEx 的调用逻辑)
        /// </summary>
        private void CallNativeWithTypeDesc(IntPtr funcPtr, int argsPtr, int[] typeDesc, int argCount, bool returnsFloat)
        {
            // 简化路径: 构造全 int32 参数的原生调用
            var nativeArgs = new object?[argCount];
            var handles = new IntPtr[argCount];

            try
            {
                for (int i = 0; i < argCount; i++)
                    nativeArgs[i] = GetMemory(argsPtr + i * 4);

                // 动态调用
                var argTypes = new Type[argCount];
                for (int i = 0; i < argCount; i++)
                    argTypes[i] = typeof(int);

                Type delegateType;
                if (returnsFloat)
                {
                    delegateType = argCount switch
                    {
                        0 => typeof(Func<float>),
                        1 => typeof(Func<int, float>),
                        2 => typeof(Func<int, int, float>),
                        3 => typeof(Func<int, int, int, float>),
                        4 => typeof(Func<int, int, int, int, float>),
                        5 => typeof(Func<int, int, int, int, int, float>),
                        6 => typeof(Func<int, int, int, int, int, int, float>),
                        7 => typeof(Func<int, int, int, int, int, int, int, float>),
                        8 => typeof(Func<int, int, int, int, int, int, int, int, float>),
                        _ => typeof(Func<int, int, int, int, int, int, int, int, int, int, int, int, float>)
                    };
                }
                else
                {
                    delegateType = argCount switch
                    {
                        0 => typeof(Func<int>),
                        1 => typeof(Func<int, int>),
                        2 => typeof(Func<int, int, int>),
                        3 => typeof(Func<int, int, int, int>),
                        4 => typeof(Func<int, int, int, int, int>),
                        5 => typeof(Func<int, int, int, int, int, int>),
                        6 => typeof(Func<int, int, int, int, int, int, int>),
                        7 => typeof(Func<int, int, int, int, int, int, int, int>),
                        8 => typeof(Func<int, int, int, int, int, int, int, int, int>),
                        _ => typeof(Func<int, int, int, int, int, int, int, int, int, int, int, int, int>)
                    };
                }

#pragma warning disable IL3050 // FFI 需要动态代码, 已在 EnsureFfiAvailable 中检查 AOT
                var del = Marshal.GetDelegateForFunctionPointer(funcPtr, delegateType);
#pragma warning restore IL3050
                var result = del.DynamicInvoke(nativeArgs);

                if (returnsFloat && result is float f)
                    registers[0] = BitConverter.SingleToInt32Bits(f);
                else if (!returnsFloat && result is int i)
                    registers[0] = i;
                else
                    registers[0] = ErrorCodes.SUCCESS;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CallNativeWithTypeDesc error: {ex.Message}");
                registers[0] = ErrorCodes.DLL_CALL_ERROR;
            }
        }
    }
}
