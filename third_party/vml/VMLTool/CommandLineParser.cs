using System;
using System.Collections.Generic;
using System.Linq;
using VMLPlugins;

namespace VMLTool
{
    /// <summary>
    /// 命令行参数解析器
    /// </summary>
    public class CommandLineParser
    {
        private readonly Dictionary<string, string> _arguments = new Dictionary<string, string>();
        private readonly List<string> _positionalArgs = new List<string>();
        private readonly HashSet<string> _flags = new HashSet<string>();

        /// <summary>
        /// 解析命令行参数
        /// </summary>
        public void Parse(string[] args)
        {
            _arguments.Clear();
            _positionalArgs.Clear();
            _flags.Clear();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (arg.StartsWith("--"))
                {
                    // 长格式参数: --key value 或 --key=value
                    string key = arg.Substring(2);

                    // 独立长标志（不消费下一个参数）
                    var longStandalone = new HashSet<string>(StringComparer.Ordinal)
                    {
                        "help", "plugins", "repl", "compile", "assemble",
                        "translate", "link", "run", "exe", "verbose", "quiet",
                        "version", "save-temps", "static", "shared",
                        "soft-float", "sf", "no-float", "nf",
                        "Wall", "Wextra", "Werror", "no-stdlib", "no-shared",
                    };
                    
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-") && !longStandalone.Contains(key))
                    {
                        // --key value 格式 (仅当不是独立标志时)
                        _arguments[key] = args[++i];
                    }
                    else if (key.Contains('='))
                    {
                        // --key=value 格式
                        var parts = key.Split('=', 2);
                        _arguments[parts[0]] = parts[1];
                    }
                    else
                    {
                        // 标志参数（无值）
                        _flags.Add(key);
                    }
                }
                else if (arg.StartsWith("-"))
                {
                    // 短格式参数: -k value 或 -k 或 -O2/-mr/-ss
                    string key = arg.Substring(1);

                    // 已知的多字符短参数 (大小写不敏感词条)
                    var multiCharShort = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        "mr", "ss", "sf", "nf",
                        "O0", "O1", "O2", "O3", "Os", "Oz",
                    };

                    var standaloneFlags = new HashSet<string>(StringComparer.Ordinal)
                    {
                        "c", "S", "E", "a", "T", "k", "r", "e",
                        "g", "K", "P", "v", "V", "h", "i",
                        "Wall", "Wextra", "Werror", "static", "shared",
                        "soft-float", "no-float",
                    };
                    
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-") && !standaloneFlags.Contains(key))
                    {
                        _arguments[key] = args[++i];
                    }
                    else if (standaloneFlags.Contains(key) || multiCharShort.Contains(key))
                    {
                        _flags.Add(key);
                    }
                    else if (key.Length == 1)
                    {
                        _flags.Add(key);
                    }
                    else
                    {
                        // 多个短标志组合: -abc 相当于 -a -b -c
                        foreach (char c in key)
                        {
                            _flags.Add(c.ToString());
                        }
                    }
                }
                else
                {
                    // 位置参数
                    _positionalArgs.Add(arg);
                }
            }
        }

        /// <summary>
        /// 获取参数值
        /// </summary>
        public string? GetArgument(string key)
        {
            return _arguments.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// 获取参数值，如果不存在则返回默认值
        /// </summary>
        public string GetArgument(string key, string defaultValue)
        {
            return GetArgument(key) ?? defaultValue;
        }

        /// <summary>
        /// 检查是否存在参数
        /// </summary>
        public bool HasArgument(string key)
        {
            return _arguments.ContainsKey(key);
        }

        /// <summary>
        /// 检查是否存在标志
        /// </summary>
        public bool HasFlag(string flag)
        {
            return _flags.Contains(flag);
        }

        /// <summary>
        /// 获取位置参数
        /// </summary>
        public List<string> GetPositionalArguments()
        {
            return new List<string>(_positionalArgs);
        }

        /// <summary>
        /// 获取位置参数数量
        /// </summary>
        public int PositionalArgumentCount => _positionalArgs.Count;

        /// <summary>
        /// 获取所有参数
        /// </summary>
        public Dictionary<string, string> GetAllArguments()
        {
            return new Dictionary<string, string>(_arguments);
        }

        /// <summary>
        /// 获取所有标志
        /// </summary>
        public HashSet<string> GetAllFlags()
        {
            return new HashSet<string>(_flags);
        }

        /// <summary>
        /// 判断是否为空（无参数）
        /// </summary>
        public bool IsEmpty => _arguments.Count == 0 && _positionalArgs.Count == 0 && _flags.Count == 0;

        /// <summary>
        /// 显示解析结果（用于调试）
        /// </summary>
        public void ShowDebugInfo()
        {
            Console.WriteLine("=== 命令行参数解析结果 ===");
            
            Console.WriteLine("位置参数:");
            foreach (var arg in _positionalArgs)
            {
                Console.WriteLine($"  {arg}");
            }
            
            Console.WriteLine("键值参数:");
            foreach (var kvp in _arguments)
            {
                Console.WriteLine($"  {kvp.Key} = {kvp.Value}");
            }
            
            Console.WriteLine("标志:");
            foreach (var flag in _flags)
            {
                Console.WriteLine($"  {flag}");
            }
            
            Console.WriteLine("=========================");
        }
    }

    /// <summary>
    /// 命令行选项
    /// </summary>
    public class CommandLineOptions
    {
        public string? Command { get; set; }
        public string? InputFile { get; set; }
        public List<string> InputFiles { get; set; } = new List<string>();
        public string? OutputFile { get; set; }
        public string? Language { get; set; }                       // --lang / -x
        public string TargetArchitecture { get; set; } = "";     // -t / --target (仅显式指定时翻译)
        public List<string> LinkFiles { get; set; } = new();
        public bool GcSections { get; set; } = true;  // 链接时死代码消除 (默认开启)
        public bool ShowHelp { get; set; }
        public bool ShowPlugins { get; set; }
        public bool RunRepl { get; set; }
        public bool PreprocessOnly { get; set; }
        public bool LinkOnly { get; set; }
        public bool AssembleOnly { get; set; }
        public bool TranslateOnly { get; set; }
        public bool CompileOnly { get; set; }
        public bool RunOnly { get; set; }
        public string? VmbCommand { get; set; }
        public bool Verbose { get; set; }
        public bool Quiet { get; set; }
        public bool Version { get; set; }
        public int OptimizationLevel { get; set; } = 0;
        public bool CreateExe { get; set; }
        public string? ExeRuntime { get; set; }
        public string? DumpFile { get; set; }

        // 库和包含路径
        public List<string> LibraryPaths { get; set; } = new();
        public List<string> LibraryNames { get; set; } = new();    // -l <name> (GCC兼容)
        public List<string> IncludePaths { get; set; } = new();

        /// <summary>编译/运行超时秒数 (0=不限制，默认60)</summary>
        public int TimeoutSeconds { get; set; } = 60;

        /// <summary>vmltool.config.xml 配置文件路径 (CLI指定，优先级最高)</summary>
        public string? ConfigFile { get; set; }
        
        // GCC 风格选项
        public List<string> Defines { get; set; } = new();        // -D
        public List<string> Undefines { get; set; } = new();      // -U
        public int WarningLevel { get; set; } = 0;                // -Wall / -w
        public bool WarningsAsErrors { get; set; }                // -Werror
        public bool DebugMode { get; set; }                       // -g
        public bool DumpProgress { get; set; }                     // --dump-progress
        public bool PrepareLog { get; set; }                       // --dump-prepare
        public bool DumpPreprocess { get; set; }                   // --dump-preprocess
        public bool DumpCall { get; set; }                         // --dump-call
        public bool DumpLink { get; set; }                        // --dump-link
        public string? LanguageStandard { get; set; }              // -std=
        public string? BasicType { get; set; }                    // --basictype (qbasic/turbobasic/...)
        public string? PascalType { get; set; }                   // --pascaltype (turbo/delphi/...)
        public bool StaticLink { get; set; }                      // -static
        public bool SharedLink { get; set; }                      // -shared
        public bool SaveTemps { get; set; }                       // -save-temps
        public string? OutputFormat { get; set; }                   // -f / --format (hex/elf/exe/bin/s19)
        public string? AsmOutput { get; set; }                      // -S / --asm 输出目标汇编文件
        
        // MCU/OS 模式
        public TargetMode TargetMode { get; set; } = TargetMode.MCU;

        // 内存级别
        public MemoryLevel MemoryLevel { get; set; } = MemoryLevel.RAM_M;
        public int? StackSize { get; set; }      // --stack-size (字节)，null=自动

        // 浮点处理模式
        public Float32Mode Float32Mode { get; set; } = Float32Mode.Hard;
        public Float64Mode Float64Mode { get; set; } = Float64Mode.Hard;

        // 64位处理模式
        public Int64Mode Int64Mode { get; set; } = Int64Mode.Hard;

        // 源文件注释
        public bool SourceComment { get; set; } = true;

        // 库包含选项
        public bool AutoIncludeStdLib { get; set; } = true;
        public bool UseSharedLibrary { get; set; } = true;

        /// <summary>
        /// 从解析器创建选项
        /// </summary>
        public static CommandLineOptions FromParser(CommandLineParser parser)
        {
            var options = new CommandLineOptions();

            // 处理标志
            options.ShowHelp = parser.HasFlag("h") || parser.HasFlag("help");
            options.ShowPlugins = parser.HasFlag("P") || parser.HasFlag("plugins");
            options.RunRepl = parser.HasFlag("i") || parser.HasFlag("repl");
            options.Verbose = parser.HasFlag("verbose");
            options.Quiet = parser.HasFlag("q") || parser.HasFlag("quiet");
            options.Version = parser.HasFlag("v") || parser.HasFlag("V") || parser.HasFlag("version");
            options.DebugMode = parser.HasFlag("g");
            options.DumpProgress = parser.HasFlag("dump-progress");
            options.PrepareLog = parser.HasFlag("dump-prepare");
            options.DumpPreprocess = parser.HasFlag("dump-preprocess");
            options.DumpCall = parser.HasFlag("dump-call");
            options.DumpLink = parser.HasFlag("dump-link");

            // -Wall / -Wextra / -Werror / -w
            if (parser.HasFlag("Wall")) options.WarningLevel = 1;
            if (parser.HasFlag("Wextra")) options.WarningLevel = 2;
            if (parser.HasFlag("Werror")) options.WarningsAsErrors = true;
            if (parser.HasFlag("w")) options.WarningLevel = 0;

            // -std=...
            options.LanguageStandard = parser.GetArgument("std");
            options.BasicType = parser.GetArgument("basictype");
            options.PascalType = parser.GetArgument("pascaltype");

            // -D name[=value] 宏定义
            var defineStr = parser.GetArgument("D");
            if (!string.IsNullOrEmpty(defineStr))
            {
                foreach (var d in defineStr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                    options.Defines.Add(d.Trim());
            }

            // -U name 取消宏定义
            var undefStr = parser.GetArgument("U");
            if (!string.IsNullOrEmpty(undefStr))
            {
                foreach (var u in undefStr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                    options.Undefines.Add(u.Trim());
            }

            // -l <name> 库链接 (GCC兼容)
            var libNames = parser.GetArgument("l");
            if (!string.IsNullOrEmpty(libNames))
            {
                foreach (var n in libNames.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                    options.LibraryNames.Add(n.Trim());
            }

            // -static / -shared
            options.StaticLink = parser.HasFlag("static");
            options.SharedLink = parser.HasFlag("shared");

            // -E 预处理 (GCC兼容)
            if (parser.HasFlag("E")) { options.PreprocessOnly = true; options.Command = "preprocess"; }

            // -c / --compile 编译 (GCC兼容)
            if (parser.HasFlag("c") || parser.HasFlag("compile")) { options.CompileOnly = true; options.Command = "compile"; }

            // -S 编译到汇编 (GCC兼容, 停止在汇编阶段) — 保留
            if (parser.HasFlag("S")) { options.CompileOnly = true; options.Command = "compile"; }

            // -K / --save-temps
            // -a / --assemble 汇编 VML
            if (parser.HasFlag("a") || parser.HasFlag("assemble")) { options.AssembleOnly = true; options.Command = "assemble"; }

            // -T / --translate 翻译 VML
            if (parser.HasFlag("T") || parser.HasFlag("translate")) { options.TranslateOnly = true; options.Command = "translate"; }

            // -r / --run 运行
            if (parser.HasFlag("r") || parser.HasFlag("run")) { options.RunOnly = true; options.Command = "run"; }

            // -e / --exe 打包
            if (parser.HasFlag("e") || parser.HasFlag("exe")) { options.CreateExe = true; }

            // -k / --link 链接
            if (parser.HasFlag("k") || parser.HasFlag("link")) { options.LinkOnly = true; options.Command = "link"; }

            options.SaveTemps = parser.HasFlag("K") || parser.HasFlag("save-temps");

            // -f / --format (hex/elf/exe/bin/s19/dump)
            options.OutputFormat = parser.GetArgument("f") ?? parser.GetArgument("format");

            // --asm 输出目标汇编文件 (GCC -S 已用于编译到汇编)
            options.AsmOutput = parser.GetArgument("asm");

            // -d / --dump 指令dump
            options.DumpFile = options.DumpFile ?? parser.GetArgument("d") ?? parser.GetArgument("dump");

            // -m / --mode mcu/os (编译目标模式)
            var modeStr = parser.GetArgument("m") ?? parser.GetArgument("mode");
            if (!string.IsNullOrEmpty(modeStr))
            {
                options.TargetMode = modeStr.Equals("os", StringComparison.OrdinalIgnoreCase) ? TargetMode.OS : TargetMode.MCU;
            }

            // -mr / --ram k|m|g (内存级别)
            options.MemoryLevel = TargetConfig.ParseMemoryLevel(parser.GetArgument("mr") ?? parser.GetArgument("ram"));

            // -ss / --stack-size <bytes> (自定义栈大小，默认自动)
            var ss = parser.GetArgument("ss") ?? parser.GetArgument("stack-size");
            if (int.TryParse(ss, out int stackSize) && stackSize > 0)
                options.StackSize = stackSize;

            // --float32 (32位浮点模式: hard/soft/none, 默认hard)
            var f32 = parser.GetArgument("float32");
            if (!string.IsNullOrEmpty(f32))
            {
                options.Float32Mode = f32.ToLower() switch
                {
                    "hard" => VMLPlugins.Float32Mode.Hard,
                    "soft" => VMLPlugins.Float32Mode.Soft,
                    "none" or "off" => VMLPlugins.Float32Mode.None,
                    _ => VMLPlugins.Float32Mode.Hard,
                };
            }
            // 兼容旧 --soft-float / --no-float
            if (parser.HasFlag("soft-float") || parser.HasFlag("sf"))
                options.Float32Mode = VMLPlugins.Float32Mode.Soft;
            else if (parser.HasFlag("no-float") || parser.HasFlag("nf"))
                options.Float32Mode = VMLPlugins.Float32Mode.None;

            // --float64 (64位浮点模式: hard/soft/none, 默认soft)
            var f64 = parser.GetArgument("float64");
            if (!string.IsNullOrEmpty(f64))
            {
                options.Float64Mode = f64.ToLower() switch
                {
                    "hard" => VMLPlugins.Float64Mode.Hard,
                    "soft" => VMLPlugins.Float64Mode.Soft,
                    "none" or "off" => VMLPlugins.Float64Mode.None,
                    _ => VMLPlugins.Float64Mode.Soft,
                };
            }

            // --int64 (64位整数模式: hard/soft/none, 默认soft)
            var i64 = parser.GetArgument("int64");
            if (!string.IsNullOrEmpty(i64))
            {
                options.Int64Mode = i64.ToLower() switch
                {
                    "hard" or "native" => VMLPlugins.Int64Mode.Hard,
                    "soft" or "library" => VMLPlugins.Int64Mode.Soft,
                    "none" or "off" => VMLPlugins.Int64Mode.None,
                    _ => VMLPlugins.Int64Mode.Soft,
                };
            }

            // --source-comment / --no-source-comment (源码行注释)
            if (parser.HasFlag("no-source-comment"))
                options.SourceComment = false;
            else if (parser.HasFlag("source-comment"))
                options.SourceComment = true;

            // --timeout / -to <seconds> (编译/运行超时，0=不限)
            var timeoutStr = parser.GetArgument("timeout") ?? parser.GetArgument("to");
            if (!string.IsNullOrEmpty(timeoutStr) && int.TryParse(timeoutStr, out var tSec) && tSec >= 0)
                options.TimeoutSeconds = tSec;

            // --config <path> (配置文件，优先级: CLI > ./ > $VML_HOME)
            options.ConfigFile = parser.GetArgument("config");

            var optLevel = parser.GetArgument("O") ?? parser.GetArgument("optimize");
            if (!string.IsNullOrEmpty(optLevel))
            {
                if (int.TryParse(optLevel, out int level))
                {
                    options.OptimizationLevel = level;
                }
            }
            else if (parser.HasFlag("O1") || parser.HasFlag("O"))
            {
                options.OptimizationLevel = 1;
            }
            else if (parser.HasFlag("O2"))
            {
                options.OptimizationLevel = 2;
            }
            else if (parser.HasFlag("O0"))
            {
                options.OptimizationLevel = 0;
            }

            // 处理参数
            options.InputFile = parser.GetArgument("input") ?? parser.GetArgument("i") ?? parser.GetArgument("c") ?? parser.GetArgument("compile");
            
            // GCC 兼容: 无 -c 标志时, 第一个位置参数作为输入文件
            if (string.IsNullOrEmpty(options.InputFile))
            {
                var pos = parser.GetPositionalArguments();
                if (pos.Count > 0 && !pos[0].StartsWith("-"))
                    options.InputFile = pos[0];
            }
            
            // 收集输入文件列表（-c 后的所有位置参数和指定文件）
            options.InputFiles.Clear();
            var mainInput = options.InputFile;
            if (!string.IsNullOrEmpty(mainInput))
                options.InputFiles.AddRange(mainInput.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
            
            // 如果有编译标志，将位置参数也作为输入文件
            if (parser.HasFlag("c") || parser.HasFlag("compile"))
            {
                foreach (var pos in parser.GetPositionalArguments())
                {
                    if (!pos.StartsWith("-") && !options.InputFiles.Contains(pos) &&
                        options.InputFile != pos && options.OutputFile != pos &&
                        options.TargetArchitecture != pos)
                        options.InputFiles.Add(pos);
                }
            }
            // 链接模式: 位置参数作为链接文件
            if (parser.HasFlag("k") || parser.HasFlag("link"))
            {
                options.LinkFiles.Clear();
                foreach (var pos in parser.GetPositionalArguments())
                {
                    if (!pos.StartsWith("-") && options.OutputFile != pos)
                        options.LinkFiles.Add(pos);
                }
                // 所有位置参数都是链接文件 (不需要单独的 InputFile)
                options.InputFile = null;
            }
            options.OutputFile = parser.GetArgument("output") ?? parser.GetArgument("o");
            options.Language = parser.GetArgument("x") ?? parser.GetArgument("lang");
            options.TargetArchitecture = parser.GetArgument("target") ?? parser.GetArgument("t") ?? options.TargetArchitecture;
            
            // 处理库路径和包含路径
            options.AutoIncludeStdLib = !parser.HasFlag("no-link");
            options.UseSharedLibrary = !parser.HasFlag("no-shared") && !parser.HasFlag("no-link");
            
            // 解析库路径（支持多个-L参数）
            var libPaths = parser.GetArgument("L") ?? parser.GetArgument("library");
            if (!string.IsNullOrEmpty(libPaths))
            {
                foreach (var path in libPaths.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    options.LibraryPaths.Add(path.Trim());
                }
            }
            
            // 解析包含路径（支持多个-I参数）
            var includePaths = parser.GetArgument("I") ?? parser.GetArgument("include");
            if (!string.IsNullOrEmpty(includePaths))
            {
                foreach (var path in includePaths.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    options.IncludePaths.Add(path.Trim());
                }
            }

                // 可执行文件打包
            options.CreateExe = parser.HasFlag("exe") || parser.HasFlag("e");
            options.ExeRuntime = parser.GetArgument("R") ?? parser.GetArgument("runtime");
            options.DumpFile = options.DumpFile ?? parser.GetArgument("dump");

            // 处理命令（通过标志触发）
            if (parser.HasFlag("c") || parser.HasFlag("compile"))
            {
                options.CompileOnly = true; options.Command = "compile";
            }
            else if (parser.HasFlag("a") || parser.HasFlag("assemble"))
            {
                options.AssembleOnly = true; options.Command = "assemble";
            }
            else if (parser.HasFlag("T") || parser.HasFlag("translate"))
            {
                options.TranslateOnly = true; options.Command = "translate";
            }
            else if (parser.HasFlag("p") || parser.HasFlag("preprocess"))
            {
                options.PreprocessOnly = true; options.Command = "preprocess";
            }
            else if (parser.HasFlag("k") || parser.HasFlag("link"))
            {
                options.LinkOnly = true; options.Command = "link";
                if (parser.HasFlag("no-gc-sections")) options.GcSections = false;
            }
            else if (parser.HasFlag("r") || parser.HasFlag("run"))
            {
                options.RunOnly = true; options.Command = "run";
            }
            else if (parser.HasFlag("vmb"))
            {
                options.VmbCommand = parser.GetArgument("vmb");
            }
                        
            // 如果没有指定标志但有输入文件，默认为编译
            if (!options.CompileOnly && !options.AssembleOnly && !options.TranslateOnly
                && !options.PreprocessOnly && !options.LinkOnly && !options.RunOnly
                && string.IsNullOrEmpty(options.VmbCommand)
                && !string.IsNullOrEmpty(options.InputFile))
            {
                options.CompileOnly = true;
                options.Command = "compile";
            }

            return options;
        }

        /// <summary>
        /// 验证选项
        /// </summary>
        public bool Validate()
        {
            if (ShowHelp || ShowPlugins || RunRepl || Version)
            {
                return true; // 这些命令不需要输入文件
            }

            switch (Command)
            {
                case "compile":
                case "assemble":
                case "preprocess":
                case "run":
                    return !string.IsNullOrEmpty(InputFile);
                    
                case "translate":
                    return !string.IsNullOrEmpty(InputFile) && !string.IsNullOrEmpty(TargetArchitecture);
                    
                case "link":
                    return (LinkFiles.Count > 0 || !string.IsNullOrEmpty(InputFile)) && !string.IsNullOrEmpty(OutputFile);
                    
                case "vmb":
                    return !string.IsNullOrEmpty(InputFile);
                    
                default:
                    return false;
            }
        }

        /// <summary>
        /// 显示选项信息（用于调试）
        /// </summary>
        public void ShowDebugInfo()
        {
            Console.WriteLine("=== 命令行选项 ===");
            Console.WriteLine($"命令: {Command}");
            Console.WriteLine($"输入文件: {InputFile}");
            Console.WriteLine($"输出文件: {OutputFile}");
            Console.WriteLine($"语言: {Language}");
            Console.WriteLine($"目标架构: {TargetArchitecture}");
            Console.WriteLine($"显示帮助: {ShowHelp}");
            Console.WriteLine($"显示插件: {ShowPlugins}");
            Console.WriteLine($"运行REPL: {RunRepl}");
            Console.WriteLine($"详细模式: {Verbose}");
            Console.WriteLine($"安静模式: {Quiet}");
            Console.WriteLine($"版本: {Version}");
            Console.WriteLine($"优化级别: {OptimizationLevel}");
            Console.WriteLine($"调试模式: {DebugMode}");
            Console.WriteLine($"警告级别: {WarningLevel}");
            Console.WriteLine($"警告即错误: {WarningsAsErrors}");
            Console.WriteLine($"语言标准: {LanguageStandard}");
            Console.WriteLine($"宏定义: {string.Join(", ", Defines)}");
            Console.WriteLine($"取消宏: {string.Join(", ", Undefines)}");
            Console.WriteLine($"库路径: {string.Join(", ", LibraryPaths)}");
            Console.WriteLine($"库名称: {string.Join(", ", LibraryNames)}");
            Console.WriteLine($"包含路径: {string.Join(", ", IncludePaths)}");
            Console.WriteLine($"静态链接: {StaticLink}");
            Console.WriteLine($"共享链接: {SharedLink}");
            Console.WriteLine($"保留中间: {SaveTemps}");
            Console.WriteLine($"链接文件: {string.Join(", ", LinkFiles)}");
            Console.WriteLine("==================");
        }
    }
}