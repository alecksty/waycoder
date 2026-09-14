using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VMLPlugins;

namespace CompilerBase
{
    /// <summary>CLI 参数解析结果</summary>
    public class CompilerCliArgs
    {
        public List<string> SourceFiles { get; set; } = new();
        public List<string> IncludePaths { get; set; } = new();
        public List<string> LibraryPaths { get; set; } = new();
        public string OutputFile { get; set; } = "";
        public int TimeoutSeconds { get; set; }
    }

    /// <summary>CLI 参数解析共享方法</summary>
    public static class CompilerCliParser
    {
        /// <summary>解析所有编译器共享的 CLI 参数。extraParser 委托可解析子类特有参数，成功返回 true。</summary>
        public static CompilerCliArgs ParseCommonArgs(string[] args, Func<string[], int, bool> extraParser = null)
        {
            var result = new CompilerCliArgs();

            for (int i = 0; i < args.Length; i++)
            {
                if (extraParser?.Invoke(args, i) == true)
                    continue;

                switch (args[i])
                {
                    case "-h":
                    case "--help":
                        result.SourceFiles.Clear();
                        return result;
                    case "-I" when i + 1 < args.Length:
                        result.IncludePaths.Add(args[++i]);
                        break;
                    case "-L" when i + 1 < args.Length:
                        result.LibraryPaths.Add(args[++i]);
                        break;
                    case "-o" when i + 1 < args.Length:
                        result.OutputFile = args[++i];
                        break;
                    case "--timeout" when i + 1 < args.Length:
                        result.TimeoutSeconds = int.Parse(args[++i]);
                        break;
                    case "-D" when i + 1 < args.Length:
                        CompilerOptionsContext.Current.Defines.Add(args[++i]);
                        break;
                    case "--target" when i + 1 < args.Length:
                        var t = args[++i];
                        CompilerOptionsContext.Current.TargetMode = t.Equals("os", StringComparison.OrdinalIgnoreCase)
                            ? TargetMode.OS : TargetMode.MCU;
                        break;
                    case "--ram" when i + 1 < args.Length:
                        CompilerOptionsContext.Current.MemoryLevel = TargetConfig.ParseMemoryLevel(args[++i]);
                        break;
                    case "--stack-size" when i + 1 < args.Length:
                    case "--stack" when i + 1 < args.Length:
                        if (int.TryParse(args[++i], out int ss) && ss > 0)
                            CompilerOptionsContext.Current.StackSize = ss;
                        break;
                    default:
                        if (!args[i].StartsWith("-"))
                            result.SourceFiles.Add(args[i]);
                        else
                            Console.WriteLine($"警告: 未知参数: {args[i]}");
                        break;
                }
            }

            return result;
        }

        /// <summary>添加默认标准库路径</summary>
        public static void AddDefaultLibPath(List<string> libraryPaths, string defaultLibSubDir)
        {
            var defaultLibPath = Path.Combine(Directory.GetCurrentDirectory(), "Lib", defaultLibSubDir);
            if (Directory.Exists(defaultLibPath))
                libraryPaths.Add(defaultLibPath);
        }

        /// <summary>打印所有编译器共享的帮助选项</summary>
        public static void PrintCommonUsage()
        {
            Console.WriteLine("  -I <path>      添加源码搜索路径");
            Console.WriteLine("  -L <path>      添加VML库文件搜索路径");
            Console.WriteLine("  -o <file>      指定输出文件名");
            Console.WriteLine("  --timeout <s>  设置编译超时秒数");
            Console.WriteLine("  --target mcu|os       编译目标模式（mcu=跳过OS特性，默认; os=全部特性）");
            Console.WriteLine("  --ram k|m|g           内存级别（k=KB, m=MB默认, g=GB）");
            Console.WriteLine("  --stack-size <bytes>  手动指定栈大小（默认自动根据--ram分配）");
        }

        /// <summary>带超时的异步编译（泛型版本）</summary>
        public static async Task<T> CompileWithTimeout<T>(Func<T> compileFunc, int timeoutSeconds)
        {
            // 捕获当前线程的 CompilerOptions, 因为 Task.Run 会切换线程，[ThreadStatic] 不传递
            var capturedOptions = CompilerOptionsContext.Current;
            Func<T> wrappedFunc = () => CompilerOptionsContext.RunWith(capturedOptions, compileFunc);

            if (timeoutSeconds > 0)
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                try
                {
                    return await Task.Run(wrappedFunc, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"错误: 编译超时 ({timeoutSeconds}秒)");
                    return default;
                }
            }
            return await Task.Run(wrappedFunc);
        }
    }
}
