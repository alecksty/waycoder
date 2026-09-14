using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using VMLPlugins.Interfaces;

namespace VMLPlugins
{
    /// <summary>
    /// 插件管理器
    /// </summary>
    public class PluginManager
    {
        private readonly Dictionary<string, IFrontendCompiler>  _frontendCompilers   = new();
        private readonly Dictionary<string, IBackendTranslator> _backendTranslators  = new();
        private readonly Dictionary<string, string>             _extensionToCompiler = new();

        // 性能统计
        private int _frontendCompilerLookups  = 0;
        private int _backendTranslatorLookups = 0;
        private int _fileNameLookups          = 0;

        /// <summary>
        /// 已注册的前端编译器数量
        /// </summary>
        public int FrontendCompilerCount => _frontendCompilers.Count;

        /// <summary>
        /// 已注册的后端翻译器数量
        /// </summary>
        public int BackendTranslatorCount => _backendTranslators.Count;

        /// <summary>
        /// 静默模式 — 不输出每插件注册信息
        /// </summary>
        public bool Quiet { get; set; }

        /// <summary>
        /// 前端编译器查找次数
        /// </summary>
        public int FrontendCompilerLookups => _frontendCompilerLookups;

        /// <summary>
        /// 后端翻译器查找次数
        /// </summary>
        public int BackendTranslatorLookups => _backendTranslatorLookups;

        /// <summary>
        /// 文件名查找次数
        /// </summary>
        public int FileNameLookups => _fileNameLookups;

        /// <summary>
        /// 重置性能统计
        /// </summary>
        public void ResetPerformanceStats()
        {
            _frontendCompilerLookups  = 0;
            _backendTranslatorLookups = 0;
            _fileNameLookups          = 0;
        }

        /// <summary>
        /// 注册前端编译器
        /// </summary>
        /// <param name="compiler">编译器实例</param>
        /// <returns>如果成功注册返回true，如果已存在返回false</returns>
        public bool RegisterFrontendCompiler(IFrontendCompiler compiler)
        {
            if (compiler == null)
                throw new ArgumentNullException(nameof(compiler));

            var name = compiler.Name.ToLower();
            if (_frontendCompilers.ContainsKey(name))
            {
                // 检查是否是同一个实例
                var existing = _frontendCompilers[name];
                if (existing.GetType() == compiler.GetType())
                {
                    return false; // 相同的插件类型，不重复注册
                }
                Console.WriteLine($"警告: 前端编译器 '{name}' 已存在，将被覆盖");
            }

            _frontendCompilers[name] = compiler;

            // 注册文件扩展名
            var extensions = compiler.SupportedExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var ext in extensions)
            {
                var cleanExt = ext.Trim().ToLower();
                if (!_extensionToCompiler.ContainsKey(cleanExt))
                {
                    _extensionToCompiler[cleanExt] = name;
                }
                else
                {
                    Console.WriteLine($"警告: 扩展名 '{cleanExt}' 已注册给编译器 '{_extensionToCompiler[cleanExt]}'，跳过注册");
                }
            }

            if (!Quiet) Console.WriteLine($"已注册前端编译器: {compiler.Name} ({compiler.Description})");
            return true;
        }

        /// <summary>
        /// 注册后端翻译器
        /// </summary>
        /// <param name="translator">翻译器实例</param>
        /// <returns>如果成功注册返回true，如果已存在返回false</returns>
        public bool RegisterBackendTranslator(IBackendTranslator translator)
        {
            if (translator == null)
                throw new ArgumentNullException(nameof(translator));

            var arch = translator.TargetArchitecture.ToLower();
            if (_backendTranslators.ContainsKey(arch))
            {
                // 检查是否是同一个实例
                var existing = _backendTranslators[arch];
                if (existing.GetType() == translator.GetType())
                {
                    return false; // 相同的插件类型，不重复注册
                }
                Console.WriteLine($"警告: 后端翻译器 '{arch}' 已存在，将被覆盖");
            }

            _backendTranslators[arch] = translator;
            if (!Quiet) Console.WriteLine($"已注册后端翻译器: {translator.TargetArchitecture} ({translator.Description})");
            return true;
        }

        /// <summary>
        /// 根据文件名获取编译器
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>编译器实例，如果未找到返回null</returns>
        public IFrontendCompiler? GetCompilerByFileName(string fileName)
        {
            _fileNameLookups++;
            var extension = Path.GetExtension(fileName).ToLower();
            if (string.IsNullOrEmpty(extension))
                return null;

            if (_extensionToCompiler.TryGetValue(extension, out var compilerName))
            {
                return GetFrontendCompiler(compilerName);
            }

            return null;
        }

        /// <summary>
        /// 获取前端编译器
        /// </summary>
        /// <param name="name">编译器名称</param>
        /// <returns>编译器实例</returns>
        public IFrontendCompiler? GetFrontendCompiler(string name)
        {
            _frontendCompilerLookups++;
            var key = name.ToLower();
            return _frontendCompilers.TryGetValue(key, out var compiler) ? compiler : null;
        }

        /// <summary>
        /// 获取后端翻译器
        /// </summary>
        /// <param name="architecture">目标架构名称</param>
        /// <returns>翻译器实例</returns>
        public IBackendTranslator? GetBackendTranslator(string architecture)
        {
            _backendTranslatorLookups++;
            var key = architecture.ToLower();
            return _backendTranslators.TryGetValue(key, out var translator) ? translator : null;
        }

        /// <summary>
        /// 获取所有前端编译器
        /// </summary>
        /// <returns>编译器列表</returns>
        public IEnumerable<IFrontendCompiler> GetAllFrontendCompilers()
        {
            return _frontendCompilers.Values;
        }

        /// <summary>
        /// 获取所有后端翻译器
        /// </summary>
        /// <returns>翻译器列表</returns>
        public IEnumerable<IBackendTranslator> GetAllBackendTranslators()
        {
            return _backendTranslators.Values;
        }

        /// <summary>
        /// 从程序集加载插件
        /// </summary>
        /// <param name="assemblyPath">程序集路径</param>
        [UnconditionalSuppressMessage("trim", "IL2026", Justification = "Plugin system requires dynamic assembly loading from external assemblies")]
        public void LoadPluginsFromAssembly(string assemblyPath)
        {
            try
            {
                var assembly = Assembly.LoadFrom(assemblyPath);
                LoadPluginsFromAssembly(assembly);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载插件程序集失败: {assemblyPath}");
                Console.WriteLine($"错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 从程序集加载插件
        /// </summary>
        /// <param name="assembly">程序集</param>
        [UnconditionalSuppressMessage("trim", "IL2026", Justification = "Plugin system requires reflection to discover compiler/translator types")]
        [UnconditionalSuppressMessage("trim", "IL2072", Justification = "Plugin types implement IFrontendCompiler/IBackendTranslator and are preserved")]
        public void LoadPluginsFromAssembly(Assembly assembly)
        {
            try
            {
                var types = assembly.GetTypes();

                // 加载前端编译器
                var frontendTypes = types.Where(t =>
                    typeof(IFrontendCompiler).IsAssignableFrom(t) &&
                    !t.IsInterface                                &&
                    !t.IsAbstract);

                foreach (var type in frontendTypes)
                {
                    try
                    {
                        var compiler = Activator.CreateInstance(type) as IFrontendCompiler;
                        if (compiler != null)
                        {
                            if (RegisterFrontendCompiler(compiler))
                            {
                                Console.WriteLine($"已注册前端编译器: {compiler.Name}");
                            }
                            else
                            {
                                Console.WriteLine($"前端编译器 {compiler.Name} 已存在，跳过注册");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"创建前端编译器实例失败: {type.FullName}");
                        Console.WriteLine($"错误: {ex.Message}");
                    }
                }

                // 加载后端翻译器
                var backendTypes = types.Where(t =>
                    typeof(IBackendTranslator).IsAssignableFrom(t) &&
                    !t.IsInterface                                 &&
                    !t.IsAbstract);

                foreach (var type in backendTypes)
                {
                    try
                    {
                        var translator = Activator.CreateInstance(type) as IBackendTranslator;
                        if (translator != null)
                        {
                            if (RegisterBackendTranslator(translator))
                            {
                                Console.WriteLine($"已注册后端翻译器: {translator.TargetArchitecture}");
                            }
                            else
                            {
                                Console.WriteLine($"后端翻译器 {translator.TargetArchitecture} 已存在，跳过注册");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"创建后端翻译器实例失败: {type.FullName}");
                        Console.WriteLine($"错误: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"从程序集加载插件失败: {assembly.FullName}");
                Console.WriteLine($"错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 从目录加载所有插件
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        [UnconditionalSuppressMessage("trim", "IL2026", Justification = "Plugin system requires dynamic assembly loading from external assemblies")]
        public void LoadPluginsFromDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine($"插件目录不存在: {directoryPath}");
                return;
            }

            var dllFiles = Directory.GetFiles(directoryPath, "*.dll");
            foreach (var dllFile in dllFiles)
            {
                try
                {
                    // 尝试从文件加载程序集
                    var assembly = Assembly.LoadFrom(dllFile);
                    LoadPluginsFromAssembly(assembly);
                }
                catch (Exception ex)
                {
                    // 如果程序集已经加载，尝试从已加载的程序集中查找
                    var assemblyName = Path.GetFileNameWithoutExtension(dllFile);
                    var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                                                  .FirstOrDefault(a => a.GetName().Name == assemblyName);

                    if (loadedAssembly != null)
                    {
                        Console.WriteLine($"程序集 {assemblyName} 已加载，使用已加载的版本");
                        LoadPluginsFromAssembly(loadedAssembly);
                    }
                    else
                    {
                        Console.WriteLine($"加载插件程序集失败: {dllFile}");
                        Console.WriteLine($"错误: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 计算字符串的显示宽度 (CJK字符按2宽度)
        /// </summary>
        public static int DisplayWidth(string s)
        {
            int w = 0;
            foreach (char c in s)
            {
                if (c >= 0x4E00 && c <= 0x9FFF || c >= 0x3000 && c <= 0x303F || c >= 0xFF00 && c <= 0xFFEF)
                    w += 2;
                else
                    w += 1;
            }
            return w;
        }

        public static string PadVisual(string s, int totalWidth)
        {
            int dw = DisplayWidth(s);
            int pad = totalWidth - dw;
            return pad > 0 ? s + new string(' ', pad) : s;
        }

        /// <summary>
        /// 显示已注册的插件信息
        /// </summary>
        public void ShowPluginInfo()
        {
            Console.WriteLine("=== 已注册插件 ===");

            // 前端编译器表格
            var compilers = _frontendCompilers.Values.ToList();
            if (compilers.Count > 0)
            {
                int nameW = compilers.Max(c => DisplayWidth(c.Name));
                int descW = compilers.Max(c => DisplayWidth(c.Description));
                int extW = compilers.Max(c => DisplayWidth(c.SupportedExtensions));
                nameW = Math.Max(nameW, DisplayWidth("名称"));
                descW = Math.Max(descW, DisplayWidth("描述"));
                extW = Math.Max(extW, DisplayWidth("扩展名"));

                Console.WriteLine($"\n前端编译器 ({compilers.Count}):");
                Console.WriteLine($"  {PadVisual("名称", nameW)}  {PadVisual("描述", descW)}  {PadVisual("扩展名", extW)}");
                Console.WriteLine($"  {new string('-', nameW)}  {new string('-', descW)}  {new string('-', extW)}");
                foreach (var c in compilers)
                    Console.WriteLine($"  {PadVisual(c.Name, nameW)}  {PadVisual(c.Description, descW)}  {PadVisual(c.SupportedExtensions, extW)}");
            }

            // 后端翻译器表格
            var translators = _backendTranslators.Values.ToList();
            if (translators.Count > 0)
            {
                int archW = translators.Max(t => DisplayWidth(t.TargetArchitecture));
                int descW = translators.Max(t => DisplayWidth(t.Description));
                archW = Math.Max(archW, DisplayWidth("架构"));
                descW = Math.Max(descW, DisplayWidth("描述"));

                Console.WriteLine($"\n后端翻译器 ({translators.Count}):");
                Console.WriteLine($"  {PadVisual("架构", archW)}  {PadVisual("描述", descW)}  位数");
                Console.WriteLine($"  {new string('-', archW)}  {new string('-', descW)}  ----");
                foreach (var t in translators)
                    Console.WriteLine($"  {PadVisual(t.TargetArchitecture, archW)}  {PadVisual(t.Description, descW)}  {t.SupportedBits}");
            }

            Console.WriteLine($"\n总计: {FrontendCompilerCount} 个前端编译器, {BackendTranslatorCount} 个后端翻译器");
        }

        /// <summary>
        /// 显示性能统计信息
        /// </summary>
        public void ShowPerformanceStats()
        {
            Console.WriteLine("=== 插件系统性能统计 ===");
            Console.WriteLine($"前端编译器查找次数: {_frontendCompilerLookups}");
            Console.WriteLine($"后端翻译器查找次数: {_backendTranslatorLookups}");
            Console.WriteLine($"文件名查找次数: {_fileNameLookups}");
            Console.WriteLine($"总查找次数: {_frontendCompilerLookups + _backendTranslatorLookups + _fileNameLookups}");
        }
    }
}
