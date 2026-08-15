// =============================================================
// Program.cs —— qbasic 入口
//
// 命令行入口：ide（默认）/ run <file.bas> / --test / --version / --help。
// =============================================================
using QBasic.Compiler;
using QBasic.Ide;

namespace QBasic;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length > 0)
        {
            switch (args[0].ToLowerInvariant())
            {
                case "--version":
                case "-v":
                    Console.WriteLine("qbasic 0.1.0 (WayCoder QBasic)");
                    return 0;
                case "--help":
                case "-h":
                    PrintHelp();
                    return 0;
                case "--test":
                    return SelfTest.RunAll() ? 0 : 1;
                case "run":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("用法: qbasic run <file.bas>");
                        return 1;
                    }
                    return RunFile(args[1]);
                case "ide":
                    new Ide.Ide().Run();
                    return 0;
            }
        }
        new Ide.Ide().Run();
        return 0;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("QBasic —— 一个 DOS QBasic 风格开发环境 (WayCoder 实现)");
        Console.WriteLine();
        Console.WriteLine("用法:");
        Console.WriteLine("  qbasic [ide]            进入全屏 IDE（默认）");
        Console.WriteLine("  qbasic run <file.bas>   运行一个 BASIC 脚本");
        Console.WriteLine("  qbasic --test           运行全量自测");
        Console.WriteLine("  qbasic --version        显示版本");
        Console.WriteLine("  qbasic --help           显示帮助");
        Console.WriteLine();
        Console.WriteLine("IDE 快捷键:");
        Console.WriteLine("  F5        运行当前程序");
        Console.WriteLine("  Ctrl+C    停止程序");
        Console.WriteLine("  Alt+F/E/R 打开菜单");
    }

    private static int RunFile(string file)
    {
        if (!File.Exists(file))
        {
            Console.WriteLine($"找不到文件: {file}");
            return 1;
        }
        string src = File.ReadAllText(file);
        try
        {
            var lexer = new Lexer(src);
            var tokens = lexer.Tokenize();
            var parser = new Parser(tokens);
            var stmts = parser.ParseProgram();
            var gen = new CodeGen(parser.Routines, parser.DefFns, parser.Types);
            var chunk = gen.Compile(stmts, parser.Data);

            // 图形渲染 + 键盘输入挂接（GORILLA.BAS 需要终端图形 + 非阻塞按键）
            var gfx = new GfxDevice();
            var renderer = new TerminalGfx(gfx);
            var vm = new Vm(new ConsoleInputProvider(), new ConsoleOutput())
            {
                Gfx = gfx,
                RenderMode = true,
                Present = _ => renderer.Present(),
                Keys = new ConsoleKeyProvider(),
            };
            vm.Run(chunk);
            return 0;
        }
        catch (RuntimeError re)
        {
            Console.WriteLine($"运行时错误(第 {re.Line} 行): {re.Message}");
            return 1;
        }
        catch (Exception ex) when (ex is ParseException or CompileException)
        {
            Console.WriteLine($"错误: {ex.Message}");
            return 1;
        }
    }
}
