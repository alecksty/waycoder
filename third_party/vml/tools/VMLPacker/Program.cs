using System.Runtime.InteropServices;
using VMLAssembler;
using VMLRuntime;

namespace VMLPacker
{
    internal static class Program
    {
        /// <summary>
        /// 主程序入口
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static int Main(string[] args)
        {
            // 如果作为打包EXE运行（有嵌入式 main.vml），直接执行
            try
            {
                var asm = typeof(Program).Assembly;
                using var stream = asm.GetManifestResourceStream("VMLPacker.main.vml");
                if (stream != null)
                {
                    using var reader = new System.IO.StreamReader(stream);
                    var vmlSource = reader.ReadToEnd();
                    if (!string.IsNullOrWhiteSpace(vmlSource))
                    {
                        var assembler = new VmlAssembler();
                        var basePath = Environment.GetEnvironmentVariable("VML_HOME")
                            ?? Environment.GetEnvironmentVariable("VML_TOOL_PATH")
                            ?? AppContext.BaseDirectory;
                        var program = assembler.AssembleWithIncludes(vmlSource, basePath);
                        var vm = new VmRuntime();
                        vm.LoadProgram(program);
                        vm.Run();
                        Console.Out.Flush();  // 确保自执行 EXE 输出不丢失
                        return vm.ExitCode;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[VMLPacker] 打包EXE执行失败: {ex.Message}");
                return 1;
            }

            if (args.Length == 0 || args[0] == "-h" || args[0] == "--help")
            {
                Help();
                return 0;
            }

            if (args[0] == "-v" || args[0] == "-V" || args[0] == "--version")
            {
                Console.WriteLine("VMLPacker v1.63.1");
                return 0;
            }

            string? inputFile  = null;
            string? outputFile = null;
            bool    toVmb      = false;
            bool    toExe      = false;
            bool    runAfter   = false;
            string? exeRuntime = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-o" && i + 1 < args.Length) outputFile = args[++i];
                else if (args[i] == "--vmb") toVmb                     = true;
                else if (args[i] == "--exe") { toExe = true; if (i + 1 < args.Length && !args[i + 1].StartsWith("-")) exeRuntime = args[++i]; }
                else if (args[i] == "--run") runAfter                       = true;
                else if (args[i] == "-r" && i + 1 < args.Length) exeRuntime = args[++i];
                else if (!args[i].StartsWith("-")) inputFile                = args[i];
            }

            if (inputFile == null) { Console.Error.WriteLine("错误: 未指定输入文件"); return 1; }
            if (!File.Exists(inputFile)) { Console.Error.WriteLine($"错误: 文件不存在: {inputFile}"); return 1; }

            try
            {
                VmlProgram program;

                if (inputFile.EndsWith(".vmb"))
                    program = VmlProgram.LoadFromVmbFile(inputFile);
                else if (inputFile.EndsWith(".vml"))
                {
                    var source   = File.ReadAllText(inputFile);
                    var asm      = new VmlAssembler();
                    var basePath = Path.GetDirectoryName(Path.GetFullPath(inputFile)) ?? ".";
                    program = asm.AssembleWithIncludes(source, basePath);
                }
                else
                {
                    Console.Error.WriteLine($"错误: 不支持的文件格式: {inputFile} (支持 .vml 或 .vmb)");
                    return 1;
                }

                Console.WriteLine($"已加载: {inputFile} ({program.Instructions.Count} 条指令)");

                if (toVmb || (!toExe && outputFile != null))
                {
                    var vmbPath  = outputFile ?? Path.ChangeExtension(inputFile, ".vmb");
                    var vmbBytes = program.ToVmbBytes();
                    File.WriteAllBytes(vmbPath, vmbBytes);
                    Console.WriteLine($"VMB 输出: {vmbPath} ({vmbBytes.Length} 字节)");
                }

                if (toExe)
                {
                    var exePath = outputFile ?? Path.ChangeExtension(inputFile, ".exe");
                    var runtime = exeRuntime ?? RuntimeInformation.RuntimeIdentifier;
                    CreateExecutable(program, exePath, runtime);
                    Console.WriteLine($"EXE 输出: {exePath}");
                }

            if (runAfter)
            {
                var vm = new VmRuntime();
                vm.LoadProgram(program);
                Console.WriteLine("执行中...");
                vm.Run();
                Console.WriteLine("程序执行完毕");
                return vm.ExitCode;
            }
            return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"错误: {ex.Message}");
                return 1;
            }
        }

        static void CreateExecutable(VmlProgram program, string outputPath, string runtime)
        {
            // 将 VMB 写入 main.vml，通过 dotnet publish 生成真正的独立 EXE
            var vmbBytes = program.ToVmbBytes();

            // 写入 main.vml 作为嵌入式资源
            var vmlSource = program.ToVmlTextWithIncludes();
            var prjDir = AppContext.BaseDirectory;
            // 开发模式: 找到 VMLPacker 项目目录
            var devProjDir = Path.GetFullPath(Path.Combine(prjDir, "..", "..", ".."));
            if (Directory.Exists(Path.Combine(devProjDir, "VMLPacker", "VMLPacker.csproj")))
                prjDir = Path.Combine(devProjDir, "VMLPacker");
            else if (File.Exists(Path.Combine(prjDir, "VMLPacker.csproj")))
            { /* prjDir 正确 */ }
            else
            {
                // 发布模式: 尝试从工具目录找到项目
                var srcDir = Path.GetFullPath(Path.Combine(prjDir, "..", "..", "..", ".."));
                if (Directory.Exists(Path.Combine(srcDir, "VMLPacker")))
                    prjDir = Path.Combine(srcDir, "VMLPacker");
                else
                {
                    // 最后的尝试: 从工具目录的 VMLPacker 子目录
                    var toolsPacker = Path.Combine(Path.GetDirectoryName(prjDir) ?? ".", "VMLPacker");
                    if (Directory.Exists(toolsPacker))
                        prjDir = toolsPacker;
                }
            }

            var mainVmlPath = Path.Combine(prjDir, "main.vml");
            File.WriteAllText(mainVmlPath, vmlSource);

            var publishDir = Path.Combine(Path.GetTempPath(), "vml_exe_" + Guid.NewGuid().ToString("N")[..8]);
            var csproj = Path.Combine(prjDir, "VMLPacker.csproj");

            if (!File.Exists(csproj))
            {
                // 如果找不到项目文件，生成自解压脚本作为后备
                CreateScriptExecutable(vmbBytes, outputPath, runtime);
                return;
            }

            Console.WriteLine("正在生成独立可执行文件...");
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"publish \"{csproj}\" -c Release -r {runtime} -o \"{publishDir}\" --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=true -p:TrimMode=partial -p:DebugType=None -p:DebugSymbols=false",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var proc = System.Diagnostics.Process.Start(psi)!;
            var stderrSb = new System.Text.StringBuilder();
            proc.ErrorDataReceived += (_, e) => { if (e.Data != null) stderrSb.AppendLine(e.Data); };
            proc.OutputDataReceived += (_, _) => { }; // 消费 stdout 避免管道满
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            const int timeoutMs = 300_000;
            if (!proc.WaitForExit(timeoutMs))
            {
                try { proc.Kill(entireProcessTree: true); } catch { }
                Console.WriteLine($"publish 超时 ({timeoutMs / 1000}秒)，切换到回退方案");
                CreateScriptExecutable(vmbBytes, outputPath, runtime);
                return;
            }

            if (proc.ExitCode != 0)
            {
                Console.WriteLine($"publish 失败，切换到回退方案: {stderrSb}");
                CreateScriptExecutable(vmbBytes, outputPath, runtime);
                return;
            }

            var exeName = runtime.StartsWith("win") ? "VMLPacker.exe" : "VMLPacker";
            var publishedExe = Path.Combine(publishDir, exeName);
            if (File.Exists(publishedExe))
            {
                var dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.Copy(publishedExe, outputPath, true);
                if (!runtime.StartsWith("win"))
#pragma warning disable CA1416
                    File.SetUnixFileMode(outputPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                        | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
#pragma warning restore CA1416
                Console.WriteLine($"  生成: {outputPath} ({new FileInfo(outputPath).Length} 字节)");
                // 清理临时发布目录
                try { Directory.Delete(publishDir, true); } catch { }
            }
            else
            {
                Console.WriteLine("publish 未生成预期输出，切换到脚本模式");
                CreateScriptExecutable(vmbBytes, outputPath, runtime);
            }
        }

        /// <summary>
        /// 后备方案: 复制 vmlrun + 尾部附加 VMB 数据（与 VMLTool 统一方案一致）
        /// 取代 base64 脚本(.bat/.sh)方案，生成真正的原生可执行文件
        /// </summary>
        static void CreateScriptExecutable(byte[] vmbBytes, string outputPath, string runtime)
        {
            // 查找 vmlrun 二进制
            var baseDir = AppContext.BaseDirectory;
            var exeName = runtime.StartsWith("win") ? "vmlrun.exe" : "vmlrun";

            var vmlrunPath = Path.Combine(baseDir, exeName);
            if (!File.Exists(vmlrunPath))
            {
                // 尝试父目录的 bin/<rid>/
                var ridDir = Path.Combine(baseDir, runtime);
                vmlrunPath = Path.Combine(ridDir, exeName);
            }
            if (!File.Exists(vmlrunPath))
            {
                // 尝试当前目录
                vmlrunPath = Path.Combine(Directory.GetCurrentDirectory(), exeName);
            }

            if (!File.Exists(vmlrunPath))
            {
                Console.WriteLine($"  警告: 未找到 {exeName}，无法生成独立可执行文件");
                Console.WriteLine($"  请将 {exeName} 放置到 {baseDir}");
                return;
            }

            // 复制 vmlrun + 尾部附加 VMB
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.Copy(vmlrunPath, outputPath, true);

            using (var fs = new FileStream(outputPath, FileMode.Append, FileAccess.Write))
            {
                fs.Write(vmbBytes, 0, vmbBytes.Length);
                var sizeBytes = BitConverter.GetBytes((uint)vmbBytes.Length);
                fs.Write(sizeBytes, 0, 4);
                var magic = new byte[] { (byte)'V', (byte)'M', (byte)'B', (byte)'E', (byte)'X', (byte)'E', 0x01, 0x00 };
                fs.Write(magic, 0, 8);
            }

            if (!runtime.StartsWith("win"))
            {
#pragma warning disable CA1416
                File.SetUnixFileMode(outputPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
#pragma warning restore CA1416
            }

            Console.WriteLine($"  生成: {outputPath} ({new FileInfo(outputPath).Length:#,0} 字节, VMB {vmbBytes.Length:#,0} 字节) [vmlrun]");
        }

        static void Help()
        {
            Console.WriteLine("VMLPacker — VML 程序打包工具");
            Console.WriteLine();
            Console.WriteLine("用法:");
            Console.WriteLine("  vmlpacker <input.vml>              # 汇编并运行");
            Console.WriteLine("  vmlpacker <input.vml> -o out.vmb   # 打包为 VMB");
            Console.WriteLine("  vmlpacker <input.vml> --vmb        # 打包为 VMB");
            Console.WriteLine("  vmlpacker <input.vml> --exe        # 生成独立可执行文件");
            Console.WriteLine("  vmlpacker <input.vml> --run         # 汇编并运行");
            Console.WriteLine();
            Console.WriteLine("选项:");
            Console.WriteLine("  -o <file>        输出文件");
            Console.WriteLine("  --vmb            打包为 VMB 二进制格式");
            Console.WriteLine("  --exe [runtime]  生成独立可执行文件 (默认自动检测当前平台)");
            Console.WriteLine("  -r <runtime>     指定目标平台 (win-x64/linux-x64/osx-x64/osx-arm64)");
            Console.WriteLine("  --run            运行程序");
            Console.WriteLine("  --vmb            打包为 VMB 二进制格式");
            Console.WriteLine("  -o <file>        输出文件路径");
            Console.WriteLine("  -h, --help       显示帮助");
            Console.WriteLine();
            Console.WriteLine("示例:");
            Console.WriteLine("  vmlpacker app.vml --exe          # 打包为当前平台可执行文件");
            Console.WriteLine("  vmlpacker app.vml --exe linux-x64  # 交叉打包为 Linux 可执行文件");
            Console.WriteLine("  vmlpacker app.vml -r win-x64 --exe # 指定 Windows 平台");
        }
    }
}
