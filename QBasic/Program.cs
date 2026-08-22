// QBasic/Program.cs
// 程序入口：解析命令行参数，分派到 IDE、运行脚本或自测。
namespace QBasic;

public static class Program
{
    public const string Version = "0.1.0";

    public static int Main(string[] args)
    {
        try
        {
            return Run(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("qbasic: 未捕获异常: " + ex.Message);
            return 2;
        }
    }

    private static int Run(string[] args)
    {
        if (args.Length == 0)
        {
            return Ide.IdeApp.Run(null);
        }

        switch (args[0].ToLowerInvariant())
        {
            case "--test":
            case "-t":
                return SelfTest.RunAll() ? 0 : 1;

            case "--version":
            case "-v":
                Console.WriteLine("qbasic " + Version);
                return 0;

            case "--help":
            case "-h":
                PrintHelp();
                return 0;

            case "run":
                if (args.Length < 2)
                {
                    Console.Error.WriteLine("用法: qbasic run <file.bas>");
                    return 2;
                }
                return Compiler.Runner.RunFile(args[1]);

            case "ide":
                return Ide.IdeApp.Run(args.Length > 1 ? args[1] : null);

            default:
                // 直接传 .bas 文件也当作 run 处理
                if (args[0].EndsWith(".bas", StringComparison.OrdinalIgnoreCase))
                {
                    return Compiler.Runner.RunFile(args[0]);
                }
                Console.Error.WriteLine("未知命令: " + args[0]);
                PrintHelp();
                return 2;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("QBasic 风格开发环境 v" + Version);
        Console.WriteLine();
        Console.WriteLine("用法:");
        Console.WriteLine("  qbasic                启动 IDE");
        Console.WriteLine("  qbasic ide [file.bas] 启动 IDE 并打开文件");
        Console.WriteLine("  qbasic run <file.bas> 运行 BASIC 程序");
        Console.WriteLine("  qbasic --test         运行自测");
        Console.WriteLine("  qbasic --version      显示版本");
        Console.WriteLine("  qbasic --help         显示本帮助");
    }
}
