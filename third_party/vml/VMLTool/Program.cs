#nullable disable
using VMLPlugins;
using VMLPlugins.Interfaces;
using VMLAssembler;
using VMLRuntime;
using VMLTranslators;
using VMLToHex.Assemblers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VMLTool
{
    public static partial class Program
    {
        private static PluginManager _pluginManager = new PluginManager();

        /// <summary>
        /// 主程序入口
        /// </summary>
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                InitializePlugins(quiet: true);
                ShowShortHint();
                return;
            }

            // LSP 模式 — stdin/stdout JSON-RPC
            if (args[0] == "--lsp")
            {
                Environment.Exit(LspServer.Run());
            }

            // JSON 诊断模式 — 单文件诊断输出
            if (args[0] == "--diagnostics" && args.Length > 1)
            {
                var diagArgs = new string[args.Length - 1];
                Array.Copy(args, 1, diagArgs, 0, diagArgs.Length);
                Environment.Exit(DiagnosticsCommand.Run(diagArgs));
            }

            // 初始化插件管理器（静默模式）
            InitializePlugins(quiet: true);

            // 加载 XML 配置文件 (vmltool.config.xml)
            var config = VmlToolConfig.Load();

            // 解析命令行参数 (CLI 参数覆盖配置文件值)
            var parser = new CommandLineParser();
            parser.Parse(args);

            var options = CommandLineOptions.FromParser(parser);

            // 调试信息（可选）
            if (options.Verbose || args.Length > 0 && (args[0] == "-v" || args[0] == "--verbose"))
            {
                parser.ShowDebugInfo();
                options.ShowDebugInfo();
            }

            // 处理特殊命令
            if (options.ShowHelp)
            {
                _pluginManager.ShowPluginInfo();
                Console.WriteLine();
                ShowHelp();
                return;
            }

            if (options.ShowPlugins)
            {
                _pluginManager.ShowPluginInfo();
                return;
            }

            if (options.Version)
            {
                ShowVersion();
                return;
            }

            if (options.RunRepl)
            {
                Repl();
                return;
            }

            // 验证选项
            if (!options.Validate())
            {
                Console.WriteLine("错误: 缺少必要的参数");
                ShowHelp();
                return;
            }

            try
            {
                // 根据 -o 输出文件扩展名自动推断操作
                AutoDetectByExtension(options);

                // 根据标志执行相应操作
                if (options.PreprocessOnly)
                {
                    ExecutePreprocess(options);
                }
                else if (options.CompileOnly)
                {
                    ExecuteCompile(options);
                }
                else if (options.AssembleOnly)
                {
                    ExecuteAssemble(options);
                }
                else if (options.TranslateOnly)
                {
                    ExecuteTranslate(options);
                }
                else if (options.LinkOnly)
                {
                    ExecuteLink(options);
                }
                else if (options.RunOnly)
                {
                    ExecuteRun(options);
                }
                else if (!string.IsNullOrEmpty(options.VmbCommand))
                {
                    ExecuteVmb(options);
                }
                else if (!string.IsNullOrEmpty(options.InputFile))
                {
                    // 默认：有输入文件就编译（带 -t 则构建）
                    ExecuteCompile(options);
                }
                else if (options.CreateExe && !string.IsNullOrEmpty(options.InputFile))
                {
                    ExecuteCompile(options);
                }
                else
                {
                    Console.WriteLine("请指定操作: -c(编译) -a(汇编) -t(翻译) -p(预处理) -l(链接) -r(运行)");
                    ShowHelp();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"内部错误: {ex.InnerException.Message}");
                }
            }
        }

        /// <summary>
        /// 初始化插件
        /// </summary>
        private static void InitializePlugins(bool quiet = false)
        {
            _pluginManager.Quiet = quiet;
            if (!quiet) Console.WriteLine("初始化插件系统...");

#if STATIC_LINK
            // 静态链接模式：直接注册所有编译器和转译器
            if (!quiet) Console.WriteLine("使用静态链接模式...");
            StaticLink.StaticLinkInitializer.RegisterAll(_pluginManager);
#else
            // 动态加载模式：通过反射加载插件
            LoadBuiltinPlugins();

            // 加载外部插件（从Plugins目录）
            if (Directory.Exists("Plugins"))
            {
                _pluginManager.LoadPluginsFromDirectory("Plugins");
            }
#endif
        }

        /// <summary>
        /// 加载内置插件
        /// </summary>
        private static void LoadBuiltinPlugins()
        {
            try
            {
                Console.WriteLine("加载内置插件...");

                // 尝试加载实际的编译器插件
                TryLoadActualPlugins();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载内置插件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 尝试加载实际的编译器插件
        /// </summary>
        private static void TryLoadActualPlugins()
        {
            try
            {
                // 获取当前程序集目录
                var currentDir = AppDomain.CurrentDomain.BaseDirectory;
                Console.WriteLine($"当前目录: {currentDir}");

                // 从Plugins目录加载插件
                var pluginsDir = Path.Combine(currentDir, "Plugins");
                if (Directory.Exists(pluginsDir))
                {
                    Console.WriteLine($"从插件目录加载: {pluginsDir}");
                    _pluginManager.LoadPluginsFromDirectory(pluginsDir);
                }
                else
                {
                    Console.WriteLine($"插件目录不存在: {pluginsDir}");
                    Console.WriteLine("请先运行 CopyPlugins.ps1 脚本复制插件到 Plugins 目录");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载实际插件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 尝试加载外部插件
        /// </summary>
        private static void TryLoadExternalPlugins()
        {
            // 已弃用，插件现在通过 TryLoadActualPlugins() 从 Plugins 目录加载
            // 保留此方法以保持代码结构，但不执行任何操作
        }

        /// <summary>
        /// 显示插件信息
        /// </summary>
        private static void ShowPlugins()
        {
            _pluginManager.ShowPluginInfo();
        }

        /// <summary>
        /// 显示版本信息
        /// </summary>
        private static void ShowVersion()
        {
            Console.WriteLine(VMLPlugins.VersionInfo.Product(VMLPlugins.Localization.CurrentLang == "en" ? "VML Toolchain" : "VML 工具链"));
            Console.WriteLine("多前端编译器 + 统一IR + 多后端翻译器架构");
            Console.WriteLine("支持语言: C, BASIC, Pascal, Ladder, Python, Forth, Lua, Rust, Go, Java, JavaScript, Swift, C#, C++, Kotlin, Scheme, Ruby, Dart, ObjC, R, D, Fortran");
            Console.WriteLine("支持架构: 6502, Z80, 8051, AVR, PIC, MSP430, PIC24, ARM-CM, X86, MIPS, RISC-V, 68000, PowerPC, SPARC, JVM, .NET, Wasm");
        }

        /// <summary>
        /// 显示帮助信息
        /// </summary>
        private static void ShowShortHint()
        {
            Console.WriteLine(VMLPlugins.VersionInfo.Product(VMLPlugins.Localization.Get("lang_name") == "English" ? "VML Toolchain" : "VML 工具链"));
            Console.WriteLine(VMLPlugins.Localization.Get("help.compilers",
                _pluginManager.FrontendCompilerCount, _pluginManager.BackendTranslatorCount));
            Console.WriteLine(VMLPlugins.Localization.Get("help.short.usage"));
            Console.WriteLine(VMLPlugins.Localization.Get("help.short.hint"));
        }

        private static void ShowHelp()
        {
            string H(string key) => VMLPlugins.Localization.Get(key);
            Console.WriteLine(VMLPlugins.VersionInfo.Product(H("lang_name") == "English" ? "VML Toolchain" : "VML 工具链") + " — " + H("tool.description"));
            Console.WriteLine();
            Console.WriteLine(H("help.title"));
            Console.WriteLine();
            Console.WriteLine(H("help.section.actions"));
            Console.WriteLine($"  -c                        {H("help.action.compile")}");
            Console.WriteLine($"  -S                        {H("help.action.to_asm")}");
            Console.WriteLine($"  -E                        {H("help.action.preprocess")}");
            Console.WriteLine($"  -a, --assemble            {H("help.action.assemble")}");
            Console.WriteLine($"  -T, --translate           {H("help.action.translate")}");
            Console.WriteLine($"  -k, --link                {H("help.action.link")}");
            Console.WriteLine($"  -r, --run                 {H("help.action.run")}");
            Console.WriteLine($"  -e, --exe                 {H("help.action.exe")}");
            Console.WriteLine();
            Console.WriteLine(H("help.section.common"));
            Console.WriteLine($"  -o, --output <file>      {H("help.opt.output")}");
            Console.WriteLine($"  -x <lang>                 {H("help.opt.lang")}");
            Console.WriteLine($"  -t, --target <arch>       {H("help.opt.target")}");
            Console.WriteLine($"  -f, --format <fmt>        {H("help.opt.format")}");
            Console.WriteLine($"  -O0, -O1, -O2             {H("help.opt.optimize")}");
            Console.WriteLine($"  -d, --dump <file>         {H("help.opt.dump")}");
            Console.WriteLine($"  --dump-progress           {H("help.opt.dump_progress")}");
            Console.WriteLine($"  --dump-preprocess         {H("help.opt.dump_preprocess")}");
            Console.WriteLine($"  --dump-call               {H("help.opt.dump_call")}");
            Console.WriteLine($"  -K, --save-temps          {H("help.opt.savetemps")}");
            Console.WriteLine();
            Console.WriteLine(H("help.section.gcc"));
            Console.WriteLine($"  -D <macro[=value]>        {H("help.opt.define")}");
            Console.WriteLine($"  -U <macro>                {H("help.opt.undef")}");
            Console.WriteLine($"  -I, --include <path>      {H("help.opt.include")}");
            Console.WriteLine($"  -L, --library <path>      {H("help.opt.library")}");
            Console.WriteLine($"  -l <name>                 {H("help.opt.linklib")}");
            Console.WriteLine($"  -std=<standard>           {H("help.opt.std")}");
            Console.WriteLine($"  -g                        {H("help.opt.debug")}");
            Console.WriteLine($"  --timeout, -to <seconds>  {H("help.opt.timeout")}");
            Console.WriteLine($"  --config <path>           {H("help.opt.config")}");
            Console.WriteLine($"  -Wall, -Werror            {H("help.opt.warn")}");
            Console.WriteLine($"  -static, -shared          {H("help.opt.linkmode")}");
            Console.WriteLine();
            Console.WriteLine(H("help.section.target"));
            Console.WriteLine($"  -m, --mode mcu|os         {H("help.opt.mode")}");
            Console.WriteLine($"  -mr, --ram k|m|g          {H("help.opt.ram")}");
            Console.WriteLine($"  -ss, --stack-size <bytes> {H("help.opt.stack")}");
            Console.WriteLine($"  -R, --runtime <rid>       {H("help.opt.runtime")}");
            Console.WriteLine($"  --soft-float, --sf        {H("help.opt.soft_float")}");
            Console.WriteLine($"  --no-float, --nf          {H("help.opt.no_float")}");
            Console.WriteLine($"  --int64 <mode>            {H("help.opt.int64")}");
            Console.WriteLine();
            Console.WriteLine(H("help.section.other"));
            Console.WriteLine($"  -h, --help                {H("help.opt.help")}");
            Console.WriteLine($"  -v, -V, --version         {H("help.opt.version")}");
            Console.WriteLine($"  --verbose                 {H("help.opt.verbose")}");
            Console.WriteLine($"  -i, --repl                {H("help.opt.repl")}");
            Console.WriteLine($"  -P, --plugins             {H("help.opt.plugins")}");
            Console.WriteLine($"  --lsp                     Start Language Server Protocol (stdin/stdout JSON-RPC)");
            Console.WriteLine($"  --diagnostics <file>      Output JSON diagnostics for editor integration");
            Console.WriteLine($"  VML_LANG=en               {H("help.opt.lang_ui")}");
            Console.WriteLine();
            Console.WriteLine("Shell 自动补全:");
            Console.WriteLine("  source Scripts/vmltool-completion.bash    # Bash");
            Console.WriteLine("  . Scripts/vmltool-completion.ps1          # PowerShell");
            Console.WriteLine();
            Console.WriteLine(H("help.section.examples"));
            Console.WriteLine($"  vmltool main.c -o program.vml                  # {H("help.example.c")}");
            Console.WriteLine($"  vmltool main.bas -o program.vml                # BASIC → VML");
            Console.WriteLine($"  vmltool -c main.vml -o out.hex -t arm-cm       # {H("help.example.hex")}");
        }

        /// <summary>
        /// 启动REPL交互环境
        /// </summary>
        private static void Repl()
        {
            Console.WriteLine("VML REPL 交互环境");
            Console.WriteLine("输入 'exit' 或 'quit' 退出");
            Console.WriteLine("输入 'help' 显示帮助");
            Console.WriteLine();

            var assembler = new VmlAssembler();
            var vm = new VmRuntime();

            while (true)
            {
                Console.Write("vml> ");
                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                var trimmed = input.Trim().ToLower();
                if (trimmed == "exit" || trimmed == "quit")
                    break;

                if (trimmed == "help")
                {
                    Console.WriteLine("可用命令:");
                    Console.WriteLine("  exit/quit - 退出REPL");
                    Console.WriteLine("  help - 显示此帮助");
                    Console.WriteLine("  clear - 清空虚拟机状态");
                    Console.WriteLine("  其他输入将被解释为VML汇编代码并执行");
                    continue;
                }

                if (trimmed == "clear")
                {
                    vm = new VmRuntime();
                    Console.WriteLine("虚拟机状态已清空");
                    continue;
                }

                try
                {
                    // 尝试汇编和执行单行代码
                    var program = assembler.Assemble(input);
                    vm.LoadProgram(program);
                    vm.Run();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"错误: {ex.Message}");
                }
            }

            Console.WriteLine("再见!");
        }
    }
}
