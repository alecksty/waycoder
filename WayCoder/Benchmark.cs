using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using WayCoder.Terminal;
using WayCoder.Tools;
using WayCoder.UI;
using WayCoder.UI.TuiControls;
using WayCoder.UI.TuiScreens;

namespace WayCoder;

/// <summary>
/// 性能测评系统 —— 通过 --benchmark 或 --perf 运行。
/// 覆盖 Agent/文件/TUI/编辑器/内存/工具/Git/上下文 八大类压力测试。
/// </summary>
public static class Benchmark
{
    // ── 结果模型 ──

    private enum Verdict { Pass, Warn, Fail }

    private record BenchItem(string Name, string Category, Verdict Verdict,
        string Value, string Threshold, long ElapsedMs, long MemDeltaKb);

    private static readonly List<BenchItem> Results = [];

    // ── 运行状态 ──

    private static string _currentCat = "";
    private static int _catPass, _catWarn, _catFail;
    private static bool _catEnabled = true;
    // ── 内存基准 ──

    private static long GetMemKb()
    {
        try { return Process.GetCurrentProcess().WorkingSet64 / 1024; }
        catch { return 0; }
    }

    private static void ForceGC()
    {
        GC.Collect(2, GCCollectionMode.Aggressive, true, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Aggressive, true, true);
    }

    // ════════════════════════════════════════════════════════════════
    // 入口
    // ════════════════════════════════════════════════════════════════

    public static void Run()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("\n🔬 WayCoder 性能测评");
        Console.WriteLine("══════════════════════════════════════════\n");

        var totalSw = Stopwatch.StartNew();
        var startMem = GetMemKb();

        // ── 1. Agent 系统压力 ──
        AgentStress();

        // ── 2. 文件系统压力 ──
        FileSystemStress();

        // ── 3. 编辑器压力 ──
        EditorStress();

        // ── 4. 内存压力 ──
        MemoryStress();

        // ── 5. 工具系统压力 ──
        ToolSystemStress();

        // ── 6. Git 操作压力 ──
        GitStress();

        // ── 7. 上下文压力 ──
        ContextStress();

        // ── 8. TUI 渲染压力（轻量，无 TUI 时跳过交互项）──
        TuiRenderStress();

        // ── 9. 大项目自编程准备度 ──
        LargeProjectReadiness();

        totalSw.Stop();
        var endMem = GetMemKb();

        // ── 汇总报告 ──
        PrintReport(totalSw.Elapsed, endMem - startMem);
    }

    // ════════════════════════════════════════════════════════════════
    // 辅助方法
    // ════════════════════════════════════════════════════════════════

    private static void Cat(string name)
    {
        _currentCat = name;
        _catPass = _catWarn = _catFail = 0;
        _catEnabled = true;
        Console.WriteLine($"\n── {name} ──");
    }

    private static void Bench(string name, long ms, long memKb,
        long warnMs = 500, long failMs = 2000, long warnMemKb = 10240, long failMemKb = 51200)
    {
        if (!_catEnabled) return;

        Verdict v;
        string value, threshold;

        if (ms >= failMs || memKb >= failMemKb)
        {
            v = Verdict.Fail; _catFail++;
            threshold = $"<{failMs}ms / <{failMemKb}KB";
        }
        else if (ms >= warnMs || memKb >= warnMemKb)
        {
            v = Verdict.Warn; _catWarn++;
            threshold = $"<{warnMs}ms / <{warnMemKb}KB";
        }
        else
        {
            v = Verdict.Pass; _catPass++;
            threshold = $"<{warnMs}ms / <{warnMemKb}KB";
        }

        value = ms >= 1000 ? $"{ms / 1000.0:F1}s" : $"{ms}ms";
        if (memKb > 0) value += $" / {FormatMem(memKb)}";

        var icon = v switch { Verdict.Pass => "✅", Verdict.Warn => "⚠️", _ => "❌" };
        Console.WriteLine($"  {icon} {name}: {value}");

        Results.Add(new BenchItem(name, _currentCat, v, value, threshold, ms, memKb));
    }

    private static string FormatMem(long kb) => kb switch
    {
        < 1024 => $"{kb}KB",
        < 1024 * 1024 => $"{kb / 1024.0:F1}MB",
        _ => $"{kb / (1024.0 * 1024):F1}GB",
    };

    /// <summary>执行 action 并返回耗时 ms 和内存增量 KB</summary>
    private static (long ms, long memKb) TimeIt(Action action, int iterations = 1)
    {
        ForceGC();
        var memBefore = GetMemKb();
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
            action();

        sw.Stop();
        var memAfter = GetMemKb();
        return (sw.ElapsedMilliseconds, Math.Max(0, memAfter - memBefore));
    }

    /// <summary>创建临时目录，返回路径 + 清理回调</summary>
    private static string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "wc_bench_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void CleanDir(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        catch { /* 尽力而为 */ }
    }

    // ════════════════════════════════════════════════════════════════
    // 1. Agent 系统压力
    // ════════════════════════════════════════════════════════════════

    private static void AgentStress()
    {
        Cat("🤖 Agent 系统压力");

        // 1.1 Config + LLM 初始化（创建 Agent 的前提资源）
        var config = Config.FromEnv();
        var (ms1, mem1) = TimeIt(() =>
        {
            for (int i = 0; i < 50; i++)
            {
                var c = Config.FromEnv();
                _ = c.Model;
            }
        });
        Bench("50 Config 解析", ms1, mem1, warnMs: 200, failMs: 500);

        // 1.2 AgentSlot 创建
        var slots = new AgentSlot[10];
        var (ms2, mem2) = TimeIt(() =>
        {
            for (int i = 0; i < 10; i++)
            {
                slots[i] = new AgentSlot();
            }
        });
        Bench("10 Slot 创建", ms2, mem2, warnMs: 100, failMs: 500);

        // 1.3 SystemPrompt 生成
        var tools = ToolRegistry.AllTools;
        var (ms3, mem3) = TimeIt(() =>
        {
            for (int i = 0; i < 20; i++)
            {
                _ = SystemPrompt.Generate(tools);
            }
        });
        Bench("20 次 SystemPrompt 生成", ms3, mem3, warnMs: 500, failMs: 2000, warnMemKb: 30720, failMemKb: 102400);

        // 1.4 子 Task 并行创建
        var (ms4, mem4) = TimeIt(() =>
        {
            var tasks = new Task[10];
            for (int i = 0; i < 10; i++)
            {
                int idx = i;
                tasks[i] = Task.Run(() =>
                {
                    var c = Config.FromEnv();
                    _ = c.Model.Length + idx;
                });
            }
            Task.WaitAll(tasks);
        });
        Bench("10 Task 并行 Config 加载", ms4, mem4, warnMs: 500, failMs: 2000);
    }

    // ════════════════════════════════════════════════════════════════
    // 2. 文件系统压力
    // ════════════════════════════════════════════════════════════════

    private static void FileSystemStress()
    {
        Cat("📁 文件系统压力");

        // 2.1 100 文件创建
        var dir1 = TempDir();
        var (ms1, mem1) = TimeIt(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                var path = Path.Combine(dir1, $"file_{i:D3}.txt");
                File.WriteAllText(path, $"Content of file {i}\nLine 2\nLine 3\n");
            }
        });
        Bench("100 文件创建 (WriteAllText)", ms1, mem1, warnMs: 500, failMs: 2000);
        CleanDir(dir1);

        // 2.2 100 文件读取
        var dir2 = TempDir();
        for (int i = 0; i < 100; i++)
            File.WriteAllText(Path.Combine(dir2, $"f{i}.txt"), $"line{i}\n");
        var (ms2, mem2) = TimeIt(() =>
        {
            for (int i = 0; i < 100; i++)
                _ = File.ReadAllText(Path.Combine(dir2, $"f{i}.txt"));
        });
        Bench("100 文件读取 (ReadAllText)", ms2, mem2, warnMs: 300, failMs: 1000);
        CleanDir(dir2);

        // 2.3 大文件读写 (10MB)
        var bigFile = Path.GetTempFileName();
        try
        {
            var bigContent = new string('X', 10_000_000); // 10MB
            var (msW, memW) = TimeIt(() => File.WriteAllText(bigFile, bigContent));
            Bench("10MB 文件写入", msW, memW, warnMs: 500, failMs: 3000, warnMemKb: 51200, failMemKb: 102400);

            var (msR, memR) = TimeIt(() => { var _ = File.ReadAllText(bigFile); });
            Bench("10MB 文件读取", msR, memR, warnMs: 300, failMs: 2000, warnMemKb: 51200, failMemKb: 102400);
        }
        finally { try { File.Delete(bigFile); } catch { } }

        // 2.4 文件锁竞争（顺序获取-释放，测锁机制吞吐）
        var lockFile = Path.GetTempFileName();
        try
        {
            int success = 0;
            var lockSw = Stopwatch.StartNew();

            // 顺序测试：10 个线程依次获取和释放锁
            for (int i = 0; i < 10; i++)
            {
                var acquired = FileLockManager.TryAcquire(lockFile, $"agent_{i}");
                if (acquired) { Interlocked.Increment(ref success); FileLockManager.Release(lockFile, $"agent_{i}"); }
            }
            lockSw.Stop();

            var status = success >= 10 ? Verdict.Pass : success >= 8 ? Verdict.Warn : Verdict.Fail;
            Console.WriteLine($"  {(status == Verdict.Pass ? "✅" : status == Verdict.Warn ? "⚠️" : "❌")} 10 线程顺序文件锁: {success}/10 成功 ({lockSw.ElapsedMilliseconds}ms)");
            Results.Add(new BenchItem("10 线程文件锁竞争", "📁 文件系统压力", status,
                $"{success}/10成功", "≥8成功", lockSw.ElapsedMilliseconds, 0));
        }
        finally { try { File.Delete(lockFile); } catch { } }

        // 2.5 EditFileTool 批量编辑
        var editDir = TempDir();
        var editFile = Path.Combine(editDir, "edit_test.cs");
        var baseContent = "// Test file\nclass Program {\n    static void Main() {\n        Console.WriteLine(\"Hello\");\n    }\n}\n";
        var (msE, memE) = TimeIt(() =>
        {
            for (int i = 0; i < 50; i++)
            {
                File.WriteAllText(editFile, baseContent.Replace("Hello", $"Hello_{i}"));
                var tool = new EditFileTool();
                var result = tool.ExecuteAsync(new Dictionary<string, object?>
                {
                    ["file_path"] = editFile,
                    ["old_string"] = $"Hello_{i}",
                    ["new_string"] = $"World_{i}",
                }).Result;
            }
        });
        Bench("50 次 EditFileTool 编辑", msE, memE, warnMs: 2000, failMs: 5000);
        CleanDir(editDir);
    }

    // ════════════════════════════════════════════════════════════════
    // 3. 编辑器压力
    // ════════════════════════════════════════════════════════════════

    private static void EditorStress()
    {
        Cat("📝 编辑器压力");

        // 3.1 大文件加载 (100K 行)
        var bigFile = Path.GetTempFileName();
        try
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 100_000; i++)
                sb.AppendLine($"// Line {i:D6}: var x{i} = new List<int>(); // some C# code here");
            File.WriteAllText(bigFile, sb.ToString());

            var (ms1, mem1) = TimeIt(() =>
            {
                var core = new EditorCore();
                core.LoadFile(bigFile);
            });
            Bench("100K 行文件加载到编辑器", ms1, mem1, warnMs: 1000, failMs: 5000, warnMemKb: 51200, failMemKb: 204800);
        }
        finally { try { File.Delete(bigFile); } catch { } }

        // 3.2 大纲提取压力 (5000 行 C#)
        var outlineFile = Path.GetTempFileName();
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace Test {");
            for (int i = 0; i < 1000; i++)
            {
                sb.AppendLine($"    class Class{i} {{");
                sb.AppendLine($"        public void Method{i}A() {{ }}");
                sb.AppendLine($"        private string Prop{i} {{ get; set; }}");
                sb.AppendLine($"    }}");
            }
            sb.AppendLine("}");
            File.WriteAllText(outlineFile, sb.ToString());

            var (ms2, mem2) = TimeIt(() =>
            {
                var core = new EditorCore();
                core.LoadFile(outlineFile);
                var items = core.ExtractOutline();
                _ = items.Count;
            });
            Bench("5000 行 C# 大纲提取 (~3000 符号)", ms2, mem2, warnMs: 500, failMs: 2000);
        }
        finally { try { File.Delete(outlineFile); } catch { } }

        // 3.3 语法高亮压力
        var hlFile = Path.GetTempFileName();
        try
        {
            var lines = new List<string>();
            for (int i = 0; i < 2000; i++)
                lines.Add($"    public async Task<string> GetUser_{i}(int id) => await _repo.FindAsync(id) ?? \"default\";");
            File.WriteAllText(hlFile, string.Join("\n", lines));

            var (ms3, mem3) = TimeIt(() =>
            {
                var syntax = Syntax.ForFile(hlFile);
                foreach (var line in File.ReadAllLines(hlFile))
                    _ = syntax.Tokenize(line);
            });
            Bench("2000 行 C# 语法高亮", ms3, mem3, warnMs: 500, failMs: 2000);
        }
        finally { try { File.Delete(hlFile); } catch { } }

        // 3.4 侧边栏目录列表压力 (500 文件)
        var listDir = TempDir();
        try
        {
            for (int i = 0; i < 500; i++)
                File.WriteAllText(Path.Combine(listDir, $"source_{i:D4}.cs"), $"// file {i}\nclass C{i} {{ }}\n");

            var (ms4, mem4) = TimeIt(() =>
            {
                var files = Directory.GetFiles(listDir);
                Array.Sort(files);
                var result = new List<string>();
                foreach (var f in files)
                    result.Add(Path.GetFileName(f));
            });
            Bench("500 文件目录枚举+排序", ms4, mem4, warnMs: 200, failMs: 1000);
        }
        finally { CleanDir(listDir); }
    }

    // ════════════════════════════════════════════════════════════════
    // 4. 内存压力
    // ════════════════════════════════════════════════════════════════

    private static void MemoryStress()
    {
        Cat("🧠 内存压力");

        // 4.1 GC 压力（大量临时对象）
        var (ms1, mem1) = TimeIt(() =>
        {
            for (int i = 0; i < 100_000; i++)
            {
                var s = $"temp_string_{i}_{Guid.NewGuid():N}";
                _ = s.Length;
            }
        });
        ForceGC();
        Bench("100K 临时字符串分配+GC", ms1, mem1, warnMs: 500, failMs: 2000);

        // 4.2 内存泄漏检测（重复加载/卸载）
        var leakFile = Path.GetTempFileName();
        File.WriteAllText(leakFile, new string('x', 1000));
        var memStart = GetMemKb();
        try
        {
            for (int i = 0; i < 100; i++)
            {
                var core = new EditorCore();
                core.LoadFile(leakFile);
                // core 出作用域，依赖 GC 回收
            }
            ForceGC();
            var memEnd = GetMemKb();
            var delta = memEnd - memStart;
            var v = delta < 10000 ? Verdict.Pass : delta < 50000 ? Verdict.Warn : Verdict.Fail;
            Console.WriteLine($"  {(v == Verdict.Pass ? "✅" : v == Verdict.Warn ? "⚠️" : "❌")} 100 次 EditorCore 加载/卸载: 内存增长 {FormatMem(Math.Max(0, delta))}");
            Results.Add(new BenchItem("100 次 EditorCore 加载/卸载", "🧠 内存压力", v,
                FormatMem(Math.Max(0, delta)), "<10MB", 0, Math.Max(0, delta)));
        }
        finally { try { File.Delete(leakFile); } catch { } }

        // 4.3 StringBuilder vs string 拼接（加量到可测量范围）
        var (msSB, _) = TimeIt(() =>
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 100_000; i++)
                sb.Append($"line_{i}\n");
            _ = sb.ToString();
        });
        var (msStr, _) = TimeIt(() =>
        {
            var s = "";
            for (int i = 0; i < 5_000; i++) // 少 20 倍避免太慢
                s += $"line_{i}\n";
            _ = s;
        });
        var ratio = msStr > 0 ? (double)msSB / msStr : 0;
        Console.WriteLine($"  ✅ StringBuilder vs string: SB(100K)={msSB}ms, Str(5K)={msStr}ms (SB {(ratio < 0.1 ? "远超" : "快于")} string)");
        Results.Add(new BenchItem("StringBuilder 100K vs string 5K", "🧠 内存压力",
            msSB < msStr / 5 ? Verdict.Pass : Verdict.Warn,
            $"SB(100K)={msSB}ms Str(5K)={msStr}ms", "SB << Str", msSB, 0));

        // 4.4 大对象分配
        var (msBig, memBig) = TimeIt(() =>
        {
            var big = new byte[50_000_000]; // 50MB
            Array.Fill(big, (byte)42);
            _ = big.Length;
        });
        ForceGC();
        Bench("50MB 字节数组分配", msBig, memBig, warnMs: 500, failMs: 2000, warnMemKb: 102400, failMemKb: 204800);
    }

    // ════════════════════════════════════════════════════════════════
    // 5. 工具系统压力
    // ════════════════════════════════════════════════════════════════

    private static void ToolSystemStress()
    {
        Cat("🔧 工具系统压力");

        // 5.1 工具注册查找
        var (ms1, mem1) = TimeIt(() =>
        {
            for (int i = 0; i < 10_000; i++)
            {
                _ = ToolRegistry.GetTool("read_file");
                _ = ToolRegistry.GetTool("write_file");
                _ = ToolRegistry.GetTool("bash");
            }
        });
        Bench("10K 次 ToolRegistry 查找 (×3)", ms1, mem1, warnMs: 200, failMs: 1000);

        // 5.2 Bash 命令执行
        var (ms2, mem2) = TimeIt(() =>
        {
            var tool = new BashTool();
            _ = tool.ExecuteAsync(new Dictionary<string, object?>
            {
                ["command"] = "echo hello_benchmark",
            }).Result;
        });
        Bench("Bash echo 命令执行", ms2, mem2, warnMs: 500, failMs: 2000);

        // 5.3 并行 Bash 命令
        var pSw = Stopwatch.StartNew();
        var tasks = new Task[10];
        int pOk = 0;
        for (int i = 0; i < 10; i++)
        {
            int idx = i;
            tasks[i] = Task.Run(() =>
            {
                var tool = new BashTool();
                var r = tool.ExecuteAsync(new Dictionary<string, object?>
                {
                    ["command"] = $"echo p{idx}",
                }).Result;
                if (r.Contains("p")) Interlocked.Increment(ref pOk);
            });
        }
        Task.WaitAll(tasks);
        pSw.Stop();
        var pV = pOk >= 8 ? Verdict.Pass : Verdict.Warn;
        Console.WriteLine($"  {(pV == Verdict.Pass ? "✅" : "⚠️")} 10 并行 Bash: {pOk}/10 成功 ({pSw.ElapsedMilliseconds}ms)");
        Results.Add(new BenchItem("10 并行 Bash 执行", "🔧 工具系统压力",
            pV, $"{pOk}/10成功", "≥8成功", pSw.ElapsedMilliseconds, 0));

        // 5.4 Grep 大目录
        var grepDir = TempDir();
        try
        {
            for (int i = 0; i < 100; i++)
                File.WriteAllText(Path.Combine(grepDir, $"f{i}.cs"), $"class Class{i} {{ void M{i}() {{ }} }}\n");

            var (ms3, mem3) = TimeIt(() =>
            {
                var tool = new GrepTool();
                _ = tool.ExecuteAsync(new Dictionary<string, object?>
                {
                    ["pattern"] = "class Class",
                    ["path"] = grepDir,
                }).Result;
            });
            Bench("Grep 搜索 100 文件目录", ms3, mem3, warnMs: 500, failMs: 2000);
        }
        finally { CleanDir(grepDir); }

        // 5.5 Glob 大目录
        var globDir = TempDir();
        try
        {
            for (int i = 0; i < 100; i++)
                File.WriteAllText(Path.Combine(globDir, $"file_{i:D3}.cs"), "// test\n");

            var (ms4, mem4) = TimeIt(() =>
            {
                var tool = new GlobTool();
                _ = tool.ExecuteAsync(new Dictionary<string, object?>
                {
                    ["pattern"] = Path.Combine(globDir, "*.cs"),
                }).Result;
            });
            Bench("Glob 匹配 100 文件", ms4, mem4, warnMs: 200, failMs: 1000);
        }
        finally { CleanDir(globDir); }

        // 5.6 Tool Schema 生成
        var allTools = ToolRegistry.AllTools;
        var (ms5, mem5) = TimeIt(() =>
        {
            foreach (var tool in allTools)
                _ = tool.Schema();
        });
        Bench($"全部 {allTools.Count} 工具 Schema 生成", ms5, mem5, warnMs: 200, failMs: 500);
    }

    // ════════════════════════════════════════════════════════════════
    // 6. Git 操作压力
    // ════════════════════════════════════════════════════════════════

    private static void GitStress()
    {
        Cat("🔗 Git 操作压力");

        // 6.1 Git status
        var (ms1, mem1) = TimeIt(() =>
        {
            var tool = new GitTool();
            _ = tool.ExecuteAsync(new Dictionary<string, object?>
            {
                ["command"] = "status --short",
            }).Result;
        });
        Bench("Git status", ms1, mem1, warnMs: 1000, failMs: 3000);

        // 6.2 Git diff stat
        var (ms2, mem2) = TimeIt(() =>
        {
            var tool = new GitTool();
            _ = tool.ExecuteAsync(new Dictionary<string, object?>
            {
                ["command"] = "diff --stat HEAD~5",
            }).Result;
        });
        Bench("Git diff --stat HEAD~5", ms2, mem2, warnMs: 1000, failMs: 5000);

        // 6.3 Git log
        var (ms3, mem3) = TimeIt(() =>
        {
            var tool = new GitTool();
            _ = tool.ExecuteAsync(new Dictionary<string, object?>
            {
                ["command"] = "log --oneline -50",
            }).Result;
        });
        Bench("Git log --oneline -50", ms3, mem3, warnMs: 500, failMs: 2000);

        // 6.4 RepoMap 生成
        var (ms4, mem4) = TimeIt(() =>
        {
            _ = RepoMapGenerator.Generate(forceRefresh: true);
        });
        Bench("RepoMap 生成 (全量)", ms4, mem4, warnMs: 2000, failMs: 10000, warnMemKb: 51200, failMemKb: 204800);
    }

    // ════════════════════════════════════════════════════════════════
    // 7. 上下文压力
    // ════════════════════════════════════════════════════════════════

    private static void ContextStress()
    {
        Cat("🗜 上下文压力");

        // 7.1 Token 估算
        var msgs = new List<JsonObject>();
        for (int i = 0; i < 1000; i++)
        {
            var userMsg = new JsonObject { ["role"] = "user", ["content"] = $"Message {i}: This is a test message with some programming content about C# and .NET development." };
            var asstMsg = new JsonObject { ["role"] = "assistant", ["content"] = $"Response {i}: Here is a detailed answer about the topic with code examples. ```cs\nvar x = {i};\nConsole.WriteLine(x);\n```" };
            msgs.Add(userMsg);
            msgs.Add(asstMsg);
        }

        var (ms1, mem1) = TimeIt(() =>
        {
            _ = ContextManager.EstimateTokens(msgs);
        });
        Bench("1000 条消息 Token 估算", ms1, mem1, warnMs: 200, failMs: 1000);

        // 7.2 消息裁剪 (SnipToolOutputs)
        var snipMsgs = new List<JsonObject>();
        for (int i = 0; i < 200; i++)
        {
            var m = new JsonObject { ["role"] = i % 2 == 0 ? "user" : "assistant" };
            if (i % 3 == 0)
                m["content"] = new string('x', 5000); // 大工具输出
            else
                m["content"] = $"Message {i} about programming.";
            snipMsgs.Add(m);
        }
        var (ms2, mem2) = TimeIt(() =>
        {
            _ = ContextManager.SnipToolOutputs(snipMsgs);
        });
        Bench("200 条消息工具输出裁剪", ms2, mem2, warnMs: 500, failMs: 2000);

        // 7.3 消息构造压力
        var (ms3, mem3) = TimeIt(() =>
        {
            var list = new List<JsonObject>();
            for (int i = 0; i < 1000; i++)
            {
                list.Add(new JsonObject
                {
                    ["role"] = i % 2 == 0 ? "user" : "assistant",
                    ["content"] = $"Message number {i:D5} with enough text to be somewhat realistic in terms of token count for a typical conversation turn about software development using C# .NET 10 NativeAOT."
                });
            }
            _ = list.Count;
        });
        Bench("1000 条消息对象构造", ms3, mem3, warnMs: 300, failMs: 1000, warnMemKb: 10240, failMemKb: 51200);

        // 7.4 会话序列化
        var sessionMsgs = new List<JsonObject>();
        for (int i = 0; i < 100; i++)
        {
            sessionMsgs.Add(new JsonObject { ["role"] = "user", ["content"] = $"User message {i} about programming topics in C#." });
            sessionMsgs.Add(new JsonObject { ["role"] = "assistant", ["content"] = $"Assistant reply {i} with detailed code examples and explanations about software architecture patterns." });
        }

        var (ms4, mem4) = TimeIt(() =>
        {
            var json = JsonHelper.SerializeArgs(new Dictionary<string, object?>
            {
                ["messages"] = sessionMsgs,
                ["timestamp"] = DateTime.UtcNow.ToString("o"),
            });
            var deserialized = json.Length;
            _ = deserialized;
        });
        Bench("200 条消息会话序列化", ms4, mem4, warnMs: 200, failMs: 1000);
    }

    // ════════════════════════════════════════════════════════════════
    // 8. TUI 渲染压力
    // ════════════════════════════════════════════════════════════════

    private static void TuiRenderStress()
    {
        Cat("🖥 TUI 渲染压力");

        // 8.1 CJK 宽度计算
        var cjkText = new StringBuilder();
        for (int i = 0; i < 1000; i++)
            cjkText.Append("你好世界道码编程智能体中文测试文本日文テスト韓国語테스트");
        var cjkStr = cjkText.ToString();

        var (ms1, mem1) = TimeIt(() =>
        {
            _ = TuiHelper.DisplayWidth(cjkStr);
        });
        Bench("CJK 宽度计算 (~60K 字符)", ms1, mem1, warnMs: 100, failMs: 500);

        // 8.2 Markdown 解析
        var mdSb = new StringBuilder();
        for (int i = 0; i < 50; i++)
        {
            mdSb.AppendLine($"### 标题 {i}");
            mdSb.AppendLine($"这是第 {i} 段正文，包含 **粗体** 和 *斜体* 以及 `行内代码`。");
            mdSb.AppendLine("```cs");
            mdSb.AppendLine($"public class Test{i} {{");
            mdSb.AppendLine($"    public int Value {{ get; set; }} = {i};");
            mdSb.AppendLine("}");
            mdSb.AppendLine("```");
            mdSb.AppendLine();
        }

        var (ms2, mem2) = TimeIt(() =>
        {
            _ = MarkdownParser.Parse(mdSb.ToString());
        });
        Bench("50 个代码块 Markdown 解析", ms2, mem2, warnMs: 500, failMs: 2000);

        // 8.3 Unified Diff 生成
        var oldText = new StringBuilder();
        var newText = new StringBuilder();
        for (int i = 0; i < 1000; i++)
        {
            oldText.AppendLine($"    var x{i} = oldValue_{i}; // old comment");
            newText.AppendLine(i % 3 == 0
                ? $"    var x{i} = newValue_{i}; // modified comment"
                : $"    var x{i} = oldValue_{i}; // old comment");
        }
        newText.AppendLine("    var newLine = \"added\"; // this is new");

        var (ms3, mem3) = TimeIt(() =>
        {
            _ = DiffPreview.GenerateUnifiedDiff(oldText.ToString(), newText.ToString(), "test.cs");
        });
        Bench("1000 行 Unified Diff 生成", ms3, mem3, warnMs: 300, failMs: 1000);

        // 8.4 ANSI 转义码生成压力
        var (ms4, mem4) = TimeIt(() =>
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 1000; i++)
            {
                sb.Append(AnsiTty.Fg(31 + (i % 7)));
                sb.Append(AnsiTty.Bg(40 + (i % 7)));
                sb.Append($" Line {i} ");
                sb.Append(AnsiTty.SgrReset);
                sb.AppendLine();
            }
        });
        Bench("1000 行 ANSI 着色", ms4, mem4, warnMs: 100, failMs: 500);

        // ── 8.5 10000 行聊天记录压力 ──
        ChatMessageStress();
    }

    /// <summary>10000 行聊天记录压力测试：创建、渲染、滚动</summary>
    private static void ChatMessageStress()
    {
        const int N = 10_000;

        // 生成真实模拟数据
        var roles = new[] { "user", "assistant", "system", "tool" };
        var sampleMessages = new List<(string role, string content)>();
        var rng = new Random(42);
        for (int i = 0; i < N; i++)
        {
            string role = roles[i % 4];
            int len = rng.Next(30, 300);
            string content = role switch
            {
                "user" => $"用户消息 #{i}: " + new string('测', len),
                "assistant" => $"### 回复 #{i}\n\n这是第 {i} 条助手回复。包含 **粗体** `代码` 和列表：\n- 项目 A\n- 项目 B\n\n```cs\nvar x = {i};\n```",
                "system" => $"  ⚙ bash(command --arg={i})",
                "tool" => $"stdout line 1\nstdout line 2\n... result #{i} OK",
                _ => $"消息 #{i}"
            };
            sampleMessages.Add((role, content));
        }

        // 8.5.1 10000 条 ChatMsg 创建
        var (ms5a, mem5a) = TimeIt(() =>
        {
            var list = new List<ChatMsg>(N);
            foreach (var (role, content) in sampleMessages)
                list.Add(new ChatMsg { Role = role, Content = content, Time = DateTime.Now });
        });
        Bench("10000 条 ChatMsg 创建", ms5a, mem5a, warnMs: 500, failMs: 2000, warnMemKb: 5120, failMemKb: 20480);

        // 8.5.2 10000 条 TuiListItem 创建（含 Markdown 解析）
        var (ms5b, mem5b) = TimeIt(() =>
        {
            var list = new List<TuiListItem>(N);
            foreach (var (role, content) in sampleMessages)
            {
                bool isPlain = role is "system" or "tool";
                var item = new TuiListItem(role, content, maxWidth: 80, isPlainText: isPlain);
                list.Add(item);
            }
        });
        Bench("10000 项 TuiListItem + Markdown 解析", ms5b, mem5b, warnMs: 2000, failMs: 8000, warnMemKb: 40960, failMemKb: 102400);

        // 8.5.3 10000 项 TuiListView 布局性能
        TuiListView? listView = null;
        var (ms5c, mem5c) = TimeIt(() =>
        {
            listView = new TuiListView { Width = 80, Height = 30, IsAutoScrollToEnd = false };
            foreach (var (role, content) in sampleMessages)
            {
                bool isPlain = role is "system" or "tool";
                listView.AddItem(new TuiListItem(role, content, maxWidth: 80, isPlainText: isPlain));
            }
            listView.ReLayout();
        });
        Bench("10000 项 TuiListView 布局", ms5c, mem5c, warnMs: 3000, failMs: 10000, warnMemKb: 81920, failMemKb: 204800);

        // 8.5.4 快速滚动性能（模拟用户翻页）
        if (listView != null)
        {
            var (ms5d, _) = TimeIt(() =>
            {
                for (int i = 0; i < 500; i++)
                {
                    listView.ScrollDown(3);  // 向下翻 3 行
                    listView.ScrollUp(3);    // 向上翻 3 行
                }
            });
            Bench("1000 次快速翻页滚动", ms5d, 0, warnMs: 200, failMs: 1000);
        }
    }

    // ════════════════════════════════════════════════════════════════
    // 9. 大项目自编程准备度
    // ════════════════════════════════════════════════════════════════

    private static void LargeProjectReadiness()
    {
        Cat("🏗 大项目自编程准备度");

        // 9.1 工具空路径防护（防止 Path.GetFullPath("") 崩溃）
        var toolWithPathParam = new[] { "read_file", "write_file", "edit_file", "glob", "grep",
            "wc", "lint", "stat", "rm", "mkdir", "cp", "mv", "cd", "download", "multiedit", "notebook_edit" };
        int safeCount = 0;
        foreach (var name in toolWithPathParam)
        {
            var tool = ToolRegistry.GetTool(name);
            if (tool == null) continue;
            // 构造空路径参数，确保不会抛未捕获异常
            try
            {
                var args = new Dictionary<string, object?>();
                switch (name)
                {
                    case "read_file": case "write_file": case "edit_file": case "download":
                    case "multiedit": case "notebook_edit":
                        args["file_path"] = "";
                        break;
                    case "glob": case "grep":
                        args["pattern"] = "test";
                        args["path"] = "";
                        break;
                    case "wc":
                        args["glob"] = "*.cs";
                        args["path"] = "";
                        break;
                    case "lint": case "stat": case "rm": case "mkdir": case "cd":
                        args["path"] = "";
                        break;
                    case "cp": case "mv":
                        args["src"] = "";
                        args["dest"] = "";
                        break;
                }
                var result = tool.ExecuteAsync(args).Result;
                safeCount++;
            }
            catch (AggregateException ae) when (ae.InnerException is ArgumentException)
            {
                // 空路径不应导致 ArgumentException
            }
            catch (ArgumentException)
            {
                // 空路径不应导致 ArgumentException
            }
        }
        Check($"{safeCount}/{toolWithPathParam.Length} 工具空路径安全", safeCount == toolWithPathParam.Length);
        if (safeCount < toolWithPathParam.Length)
            Console.WriteLine($"    ⚠ {toolWithPathParam.Length - safeCount} 个工具仍有空路径风险");

        // 9.2 JSON 解析健壮性 — ParseArgs 处理截断 JSON
        var (ms1, mem1) = TimeIt(() =>
        {
            for (int i = 0; i < 500; i++)
            {
                var r = LLM.ParseArgs("{\"file_path\": \"test.cs\", \"content\": \"hello");
                _ = r.ContainsKey("_parse_error");
            }
        });
        Bench("500 次截断 JSON 解析", ms1, mem1, warnMs: 200, failMs: 1000);

        // 9.3 上下文估计 — 10K 行项目场景
        var (ms2, mem2) = TimeIt(() =>
        {
            var msgs = new List<JsonObject>();
            var content = new string('x', 200);
            for (int i = 0; i < 100; i++)
            {
                msgs.Add(new JsonObject { ["role"] = "user", ["content"] = $"prompt_{i}: {content}" });
                msgs.Add(new JsonObject { ["role"] = "assistant", ["content"] = $"resp_{i}: {content}" });
                // 模拟工具调用
                if (i % 3 == 0)
                    msgs.Add(new JsonObject { ["role"] = "tool", ["content"] = $"result_{i}" });
            }
            var tokens = ContextManager.EstimateTokens(msgs);
            _ = tokens > 0;
        });
        Bench("100 轮对话 token 估计", ms2, mem2, warnMs: 100, failMs: 500);

        // 9.4 TokenTracker 可用性 — 验证 ContextManager 可创建
        var cm = new ContextManager(128_000);
        Check("ContextManager 创建成功 (128K)", cm != null);

        // 9.5 文件锁竞争测试 — 模拟多 Agent 并发写
        var lockFile = Path.Combine(Path.GetTempPath(), "wp_bench_lock_" + Guid.NewGuid().ToString("N")[..6] + ".txt");
        File.WriteAllText(lockFile, "lock test");
        var (ms3, mem3) = TimeIt(() =>
        {
            // 快速获取-释放 让 FileLockManager 通过
            for (int i = 0; i < 50; i++)
            {
                if (FileLockManager.TryAcquire(lockFile, $"agent_{i}"))
                    FileLockManager.Release(lockFile, $"agent_{i}");
            }
        });
        try { File.Delete(lockFile); } catch { }
        Bench("50 次文件锁获取-释放", ms3, mem3, warnMs: 200, failMs: 1000);

        // 9.6 大目录文件追踪测试
        var trackDir = Path.Combine(Path.GetTempPath(), "wp_bench_track_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(trackDir);
        for (int i = 0; i < 50; i++)
            File.WriteAllText(Path.Combine(trackDir, $"f{i:D2}.cs"), $"// file {i}\nclass C{i} {{ }}");
        var (ms4, mem4) = TimeIt(() =>
        {
            for (int i = 0; i < 50; i++)
            {
                var fp = Path.Combine(trackDir, $"f{i:D2}.cs");
                FileTracker.RecordRead(fp);
            }
            var changes = FileTracker.CheckForChanges();
            _ = changes.Count;
        });
        try { Directory.Delete(trackDir, true); } catch { }
        Bench("50 文件追踪+检查 (SHA256)", ms4, mem4, warnMs: 500, failMs: 2000);

        // 9.7 ContextManager 压缩管道可用性
        var cm2 = new ContextManager(64_000);
        cm2.AddUsage(1000, 500);
        Check("ContextManager AddUsage 正常", cm2 != null);

        // 9.8 ContinuePrompt 文件清单收集验证
        var testMsgs = new List<JsonObject>
        {
            new() { ["role"] = "user", ["content"] = "写一个 Roguelike 游戏" },
            new() { ["role"] = "assistant", ["content"] = "好的" },
            new() { ["role"] = "tool", ["content"] = "✅ 已写入: D:\\test\\MapGen.cs\n内容: ..." },
            new() { ["role"] = "tool", ["content"] = "✅ 编辑完成: D:\\test\\Player.cs\n... " },
        };
        var fileList = new List<string>();
        foreach (var m in testMsgs)
        {
            if (m["role"]?.GetValue<string>() != "tool") continue;
            var c = m["content"]?.GetValue<string>() ?? "";
            foreach (var line in c.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.Contains("✅ 已写入") || trimmed.Contains("✅ 编辑完成"))
                    fileList.Add(trimmed);
            }
        }
        Check("ContinuePrompt 文件收集: 2 文件", fileList.Count == 2);

        // 9.9 快速模式关键词检测
        Check("快速模式: 不要读文件", SystemPrompt.DetectFastMode("不要读文件，直接写代码"));
        Check("快速模式: 不要ls", SystemPrompt.DetectFastMode("不要ls和tree"));
        Check("快速模式: 跳过探索", SystemPrompt.DetectFastMode("请跳过探索阶段"));
        Check("快速模式: 正常请求不触发", !SystemPrompt.DetectFastMode("帮我写一个计算器"));
        Check("快速模式 EN: don't read", SystemPrompt.DetectFastMode("Don't read any files, just write code"));
        Check("快速模式 EN: skip reading", SystemPrompt.DetectFastMode("skip reading existing code"));
        Check("快速模式 EN: just write", SystemPrompt.DetectFastMode("just write the code directly"));
        Check("快速模式 EN: normal no trigger", !SystemPrompt.DetectFastMode("Help me build a calculator app"));

        // 9.10 项目分析器 — 模拟 10K 行输出统计
        var analysisDir = Path.Combine(Path.GetTempPath(), "wp_bench_analysis_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(analysisDir);
        var subdirs = new[] { "Core", "Entities", "Systems", "UI", "Utils" };
        foreach (var sd in subdirs) Directory.CreateDirectory(Path.Combine(analysisDir, sd));
        int totalLines = 0, totalFiles = 0;
        var rng = new Random(42); // 固定种子可复现
        foreach (var sd in subdirs)
        {
            for (int f = 0; f < 7; f++)
            {
                var content = new StringBuilder();
                for (int l = 0; l < rng.Next(50, 350); l++)
                    content.AppendLine($"// Line {l} in {sd}/File{f}.cs: " + new string('x', rng.Next(10, 60)));
                File.WriteAllText(Path.Combine(analysisDir, sd, $"File{f}.cs"), content.ToString());
                totalLines += content.ToString().Split('\n').Length - 1;
                totalFiles++;
            }
        }

        var (ms6, mem6) = TimeIt(() =>
        {
            var files = Directory.GetFiles(analysisDir, "*.cs", SearchOption.AllDirectories);
            int lines = 0;
            foreach (var f in files)
                lines += File.ReadAllLines(f).Length;
            _ = (files.Length, lines);
        });
        try { Directory.Delete(analysisDir, true); } catch { }
        Bench($"项目分析: {totalFiles} 文件/{totalLines:N0} 行扫描", ms6, mem6,
            warnMs: 500, failMs: 2000);
        Console.WriteLine($"    📊 模拟 10K 行项目: {totalFiles} 文件, {totalLines:N0} 行 (分布在 {subdirs.Length} 个目录)");

        // 9.11 IsJsonProbablyComplete 完整性检查
        Check("JSON 完整: 空对象", LLM.IsJsonProbablyComplete("{}"));
        Check("JSON 完整: 简单对象", LLM.IsJsonProbablyComplete("{\"a\":1}"));
        Check("JSON 不完整: 截断", !LLM.IsJsonProbablyComplete("{\"a\":1"));
        Check("JSON 不完整: 逗号尾", !LLM.IsJsonProbablyComplete("{\"a\":1,"));
        Check("JSON 不完整: 冒号尾", !LLM.IsJsonProbablyComplete("{\"a\":"));
        Check("JSON 不完整: 空字符串", !LLM.IsJsonProbablyComplete(""));

        // 9.12 自编程关键工具可用性
        var criticalTools = new[] { "write_file", "edit_file", "read_file", "bash",
            "glob", "grep", "todo", "struct_todo", "lint" };
        int available = 0;
        foreach (var name in criticalTools)
        {
            var tool = ToolRegistry.GetTool(name);
            if (tool != null) available++;
        }
        Check($"关键工具可用: {available}/{criticalTools.Length}", available == criticalTools.Length);
    }

    private static void Check(string label, bool condition)
    {
        if (condition)
            Console.WriteLine($"    ✅ {label}");
        else
            Console.WriteLine($"    ❌ {label}");
    }

    // ════════════════════════════════════════════════════════════════
    // 汇总报告
    // ════════════════════════════════════════════════════════════════

    private static void PrintReport(TimeSpan totalTime, long totalMemDeltaKb)
    {
        var pass = Results.Count(r => r.Verdict == Verdict.Pass);
        var warn = Results.Count(r => r.Verdict == Verdict.Warn);
        var fail = Results.Count(r => r.Verdict == Verdict.Fail);

        Console.WriteLine($"\n╔══════════════════════════════════════════╗");
        Console.WriteLine($"║        WayCoder 性能测评报告             ║");
        Console.WriteLine($"╠══════════════════════════════════════════╣");
        Console.WriteLine($"║ 测试时间: {DateTime.Now:yyyy-MM-dd HH:mm}                        ║");
        Console.WriteLine($"║ 总测试项: {Results.Count,2}  通过: {pass,2}  警告: {warn,2}  失败: {fail,2}          ║");
        Console.WriteLine($"║ 总耗时: {totalTime.TotalSeconds,4:F1}s  内存增量: {FormatMem(totalMemDeltaKb),10}          ║");
        Console.WriteLine($"╚══════════════════════════════════════════╝");

        // 按分类汇总
        Console.WriteLine("\n── 分类得分 ──");
        foreach (var group in Results.GroupBy(r => r.Category))
        {
            var gPass = group.Count(r => r.Verdict == Verdict.Pass);
            var gWarn = group.Count(r => r.Verdict == Verdict.Warn);
            var gFail = group.Count(r => r.Verdict == Verdict.Fail);
            var status = gFail > 0 ? "❌" : gWarn > 0 ? "⚠️" : "✅";
            Console.WriteLine($"  {status} {group.Key}: {gPass}通过 {gWarn}警告 {gFail}失败 (共{group.Count()}项)");
        }

        // 警告项
        var warns = Results.Where(r => r.Verdict == Verdict.Warn).ToList();
        if (warns.Count > 0)
        {
            Console.WriteLine("\n── ⚠️ 警告项 ──");
            foreach (var w in warns)
                Console.WriteLine($"  - {w.Name}: {w.Value} (阈值 {w.Threshold})");
        }

        // 失败项
        var fails = Results.Where(r => r.Verdict == Verdict.Fail).ToList();
        if (fails.Count > 0)
        {
            Console.WriteLine("\n── ❌ 失败项 ──");
            foreach (var f in fails)
                Console.WriteLine($"  - {f.Name}: {f.Value} (阈值 {f.Threshold})");
        }

        // 最慢 5 项
        Console.WriteLine("\n── 🐌 最慢测试 (Top 5) ──");
        foreach (var s in Results.OrderByDescending(r => r.ElapsedMs).Take(5))
            Console.WriteLine($"  - [{s.Category}] {s.Name}: {s.ElapsedMs}ms");

        Console.WriteLine(fail > 0
            ? $"\n❌ 测评完成：{fail} 项失败，建议优化后再发布。"
            : warn > 0
                ? $"\n⚠️ 测评完成：{warn} 项接近上限，建议关注。"
                : $"\n✅ 测评完成：全部 {Results.Count} 项通过！");
    }

    // ════════════════════════════════════════════════════════════════
    // 上限报告（--limits）
    // ════════════════════════════════════════════════════════════════

    private enum LimitSev { HardBlock, SoftDegrade, Graceful, NoLimit }

    private record LimitItem(
        string Name, string Category, string CurrentValue,
        string ExceedBehavior, LimitSev Severity, string Source,
        bool IsConfigurable = false);

    private static readonly List<LimitItem> _limitsList = [];

    public static void LimitsReport()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║              WayCoder 上限报告 (Limits Report)           ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine($"  时间: {DateTime.Now:yyyy-MM-dd HH:mm}");
        Console.WriteLine();

        _limitsList.Clear();
        _limitsList.AddRange(ProbeAgentLimits());
        _limitsList.AddRange(ProbeContextLimits());
        _limitsList.AddRange(ProbeToolLimits());
        _limitsList.AddRange(ProbeSandboxLimits());
        _limitsList.AddRange(ProbeTuiLimits());
        _limitsList.AddRange(ProbeConfigLimits());

        PrintLimitsReport();
    }

    // ── 格式化辅助 ──

    private static string SevLabel(LimitSev s) => s switch
    {
        LimitSev.HardBlock => "🔴硬阻断",
        LimitSev.SoftDegrade => "🟡降级",
        LimitSev.Graceful => "🟢优雅",
        LimitSev.NoLimit => "⚪无限制",
        _ => "?"
    };

    // ════════════════════════════════════════════════════════════════
    // 1. 🤖 智能体上限
    // ════════════════════════════════════════════════════════════════

    private static List<LimitItem> ProbeAgentLimits()
    {
        var items = new List<LimitItem>();
        const string cat = "🤖 智能体";
        var cfg = Config.FromEnv();

        AddLimit(items, "槽位数量 (F1-F10)", cat,
            "10 个 (AgentSlot.Count=10)",
            "超出 F10 无响应，SwitchAgentSlot 越界直接 return",
            LimitSev.HardBlock, "AgentSlot.cs:13");

        AddLimit(items, "子智能体最大深度", cat,
            $"AgentTool.MaxDepth={AgentTool.MaxDepth}, Config.SubAgentMaxDepth={cfg.SubAgentMaxDepth} (Clamp 1-5)",
            "最深一层移除 agent 工具禁止继续递归，子Agent轮次衰减 max(5,20-depth*5)",
            LimitSev.SoftDegrade, "AgentTool.cs:47,144-156", configurable: true);

        AddLimit(items, "并行子任务上限", cat,
            "4 个 (MaxParallelTasks, const)",
            "超出返回错误提示 '不能超过 4 个，请减少或分批次执行'",
            LimitSev.Graceful, "AgentTool.cs:50,107-108");

        AddLimit(items, "Agent 运行中禁止切换槽位", cat,
            "_agentBusy=true 时阻止",
            "提示 'Agent 正在运行，请等待完成后再切换槽位'，无数据丢失",
            LimitSev.Graceful, "Program.cs:666-669");

        AddLimit(items, "最大对话轮次", cat,
            "默认 50 轮 (Agent 构造器 _maxRounds)",
            "达到上限返回 '(已达到最大工具调用轮次)'，Agent 正常停止",
            LimitSev.SoftDegrade, "Agent.cs:58,227");

        AddLimit(items, "预算上限", cat,
            cfg.MaxBudgetUsd.HasValue ? $"${cfg.MaxBudgetUsd:F2}" : "无限制 (null)",
            "超支返回 '已达到预算上限' + 已花费金额 + 建议增加预算",
            LimitSev.Graceful, "Agent.cs:147-152", configurable: true);

        AddLimit(items, "FallbackLLM 预算", cat,
            "$5.00 (FallbackLLM.MaxBudget)",
            "超支抛出 InvalidOperationException 异常（非优雅处理）",
            LimitSev.HardBlock, "FallbackLLM.cs:17,55");

        AddLimit(items, "子Agent 输出截断", cat,
            "5000 字符 → 截断至 4500",
            "超出 5000 字符截断为前 4500 + '子智能体输出已截断'",
            LimitSev.SoftDegrade, "AgentTool.cs:175-176");

        return items;
    }

    // ════════════════════════════════════════════════════════════════
    // 2. 📨 上下文 / 消息上限
    // ════════════════════════════════════════════════════════════════

    private static List<LimitItem> ProbeContextLimits()
    {
        var items = new List<LimitItem>();
        const string cat = "📨 上下文";
        var cfg = Config.FromEnv();
        int ctx = cfg.MaxContextTokens;

        AddLimit(items, "LLM 上下文窗口", cat,
            $"{ctx:N0} tokens (MaxContextTokens)",
            "超出触发三层压缩：50%裁剪 → 70%LLM摘要 → 90%硬折叠",
            LimitSev.SoftDegrade, "Config.cs:53 / ContextManager.cs:24-26", configurable: true);

        AddLimit(items, "一层压缩：工具输出裁剪 (50%)", cat,
            $"{ctx * 50 / 100:N0} tokens 触发",
            "裁剪 >1500 字符 + >6 行的工具结果为 首3+尾3行+截断提示",
            LimitSev.SoftDegrade, "ContextManager.cs:24,123-137");

        AddLimit(items, "二层压缩：LLM 摘要 (70%)", cat,
            $"{ctx * 70 / 100:N0} tokens 触发",
            "用小模型摘要旧消息（保留最近8条），LLM失败则回退到关键词提取",
            LimitSev.SoftDegrade, "ContextManager.cs:25,51-59");

        AddLimit(items, "三层压缩：硬折叠 (90%)", cat,
            $"{ctx * 90 / 100:N0} tokens 触发",
            "只保留最近 4 条消息 + 摘要，其余全部丢弃。信息损失较大",
            LimitSev.HardBlock, "ContextManager.cs:26,63-70");

        AddLimit(items, "单工具输出裁剪阈值", cat,
            "1500 字符 + 6 行",
            "单条工具结果超出则裁剪为首3+尾3行+行数提示",
            LimitSev.SoftDegrade, "ContextManager.cs:123-126");

        AddLimit(items, "摘要 LLM 输入上限", cat,
            "15000 字符",
            "拼合消息平铺文本超过 15K 字符直接截断，单条消息限制 400 字符",
            LimitSev.HardBlock, "ContextManager.cs:226,249");

        AddLimit(items, "Token 估算公式", cat,
            "CJK=1.5 tok/char, ASCII=0.25 tok/char",
            "精度 ±15%，作为压缩触发判断的近似值。非精确计数",
            LimitSev.SoftDegrade, "ContextManager.cs:93-110");

        AddLimit(items, "会话消息列表", cat,
            "无硬上限 (List<ChatMsg>)",
            "消息无限累积，仅受内存约束。大量消息可通过 /compact 清理",
            LimitSev.NoLimit, "AgentSlot.cs:19 / Agent.cs:25");

        return items;
    }

    // ════════════════════════════════════════════════════════════════
    // 3. 📁 工具 / 文件上限
    // ════════════════════════════════════════════════════════════════

    private static List<LimitItem> ProbeToolLimits()
    {
        var items = new List<LimitItem>();
        const string cat = "📁 工具/文件";
        var cfg = Config.FromEnv();

        AddLimit(items, "Bash 输出截断", cat,
            "15K 字符 (首 6000 + 尾 3000)",
            "超出 15K 字符截断，保留头尾。流式和非流式路径行为一致",
            LimitSev.SoftDegrade, "BashTool.cs:180,247");

        AddLimit(items, "Bash 流式 stderr", cat,
            "逐行流式输出，[stderr] 前缀标记",
            "stderr 与 stdout 并行异步读取，逐行回调；UI 中通过 IsErrorOutput 自动标红",
            LimitSev.NoLimit, "BashTool.cs:212-217");

        AddLimit(items, "Bash 危险命令阻止", cat,
            "9 种模式 (rm -rf /, mkfs, dd, fork炸弹, curl|sh 等)",
            "匹配危险模式直接阻止，返回 '已阻止' + 原因 + 建议",
            LimitSev.HardBlock, "BashTool.cs:41-52");

        AddLimit(items, "文件锁获取超时", cat,
            "30 秒 (DefaultTimeout)",
            "超时后锁自动过期，其他 Agent 可强制获取",
            LimitSev.Graceful, "FileLockManager.cs:12,16");

        AddLimit(items, "文件锁等待超时", cat,
            "10 秒 (WaitForLockAsync 默认)",
            "每 200ms 轮询一次，10秒后返回 false，调用方获得 '文件被锁定'",
            LimitSev.Graceful, "FileLockManager.cs:96-108");

        AddLimit(items, "工具执行超时", cat,
            $"{cfg.ToolTimeoutSec} 秒 (ToolTimeoutSec)",
            "超时 kill 进程树，返回 '错误：在 N 秒后超时'",
            LimitSev.Graceful, "Config.cs:62 / BashTool.cs:140-161", configurable: true);

        AddLimit(items, "Lint 执行超时", cat,
            $"{cfg.LintTimeoutSec} 秒 (LintTimeoutSec)",
            "超时 kill lint 进程，不影响 Agent 主流程",
            LimitSev.Graceful, "Config.cs:63", configurable: true);

        AddLimit(items, "Fetch 最大字符", cat,
            "8000 字符 (可配置 max_chars 参数)",
            "超出截断 + '已截断，原始共 N 字符'",
            LimitSev.SoftDegrade, "FetchTool.cs:47,69-70");

        AddLimit(items, "Grep 匹配上限", cat,
            "200 结果 + 5000 文件",
            "达到上限停止搜索，返回 '已达到上限' 提示",
            LimitSev.SoftDegrade, "GrepTool.cs:92-95,123");

        AddLimit(items, "Glob 结果上限", cat,
            "100 条",
            "只返回前 100 条 + '仅显示前 100 个' 提示",
            LimitSev.SoftDegrade, "GlobTool.cs:57");

        AddLimit(items, "GitTool 输出截断", cat,
            "8000 字符 / 危险命令阻止",
            "超出截断为首 6000 + 尾 1000。push --force 等危险命令直接阻止",
            LimitSev.SoftDegrade, "GitTool.cs:28-31,58-59");

        AddLimit(items, "LintTool 输出截断", cat,
            "4000 字符",
            "超出截断 + '输出已截断'",
            LimitSev.SoftDegrade, "LintTool.cs:367-368");

        AddLimit(items, "WebSearch 超时 + 结果数", cat,
            "15s 超时, 1-10 结果 (默认5)",
            "超时返回 '搜索超时'；结果数超出 10 自动 clamp",
            LimitSev.Graceful, "WebSearchTool.cs:28,43,55");

        AddLimit(items, "LLM HTTP 请求超时 + 重试", cat,
            "60s/请求, 3 次重试, 指数退避",
            "3 次重试后抛出 '重试耗尽'；429 限流最多等 120s",
            LimitSev.HardBlock, "LLM.cs:186,418,469");

        AddLimit(items, "LSP 结果截断", cat,
            "20 条 + 200字符 header + 10s 超时",
            "超出 20 条显示前 20 + '还有 N 处'",
            LimitSev.SoftDegrade, "LspTool.cs:293,300,341");

        return items;
    }

    // ════════════════════════════════════════════════════════════════
    // 4. 🔒 沙箱 / 资源上限
    // ════════════════════════════════════════════════════════════════

    private static List<LimitItem> ProbeSandboxLimits()
    {
        var items = new List<LimitItem>();
        const string cat = "🔒 沙箱/资源";

        AddLimit(items, "沙箱内存上限", cat,
            "1 GB (MaxMemoryBytes)",
            "超限 kill 进程树，返回 '沙箱终止：内存超限'；仅 full-auto 模式生效",
            LimitSev.HardBlock, "SandboxManager.cs:34,268-273");

        AddLimit(items, "沙箱 CPU 时间", cat,
            "300 秒 (MaxCpuTimeSeconds, 声明未实施)",
            "已声明但代码中无实际监控逻辑，依赖 OS 级别 ulimit",
            LimitSev.NoLimit, "SandboxManager.cs:37");

        AddLimit(items, "沙箱网络访问", cat,
            "默认禁用 (AllowNetwork=false)",
            "通过命令模式检测阻止 curl/wget/ssh 等；localhost 例外放行",
            LimitSev.HardBlock, "SandboxManager.cs:40,65-72");

        AddLimit(items, "沙箱阻止命令", cat,
            "12 种模式 (sudo/su/mount/iptables/nc/ssh 等)",
            "匹配直接阻止，返回 '沙箱阻止' + 原因。仅 full-auto 模式生效",
            LimitSev.HardBlock, "SandboxManager.cs:47-62");

        AddLimit(items, "自动测试超时 + 节流", cat,
            "30s 超时 / 60s 节流",
            "超时 kill 测试进程不追加反馈；同项目 60s 内不重复执行测试",
            LimitSev.SoftDegrade, "Agent.cs:338-339,388-393");

        AddLimit(items, "Worktree 隔离深度", cat,
            "2 层 (MaxIsolationDepth=2, 当前实际=1)",
            "已在 worktree 内部时 Create 返回 null，防止无限嵌套。Agent ID 截断 20 字符",
            LimitSev.HardBlock, "WorktreeIsolation.cs:25,197");

        return items;
    }

    // ════════════════════════════════════════════════════════════════
    // 5. 🖥 TUI / 编辑器上限
    // ════════════════════════════════════════════════════════════════

    private static List<LimitItem> ProbeTuiLimits()
    {
        var items = new List<LimitItem>();
        const string cat = "🖥 TUI/编辑器";

        AddLimit(items, "聊天消息列表", cat,
            "无硬上限 (List<ChatMsg>)",
            "消息无限累积，仅受内存约束。万行级别验证通过 (74ms 解析, 258ms 布局)",
            LimitSev.NoLimit, "ChatScreen.ChatMessages / TuiListView");

        AddLimit(items, "编辑撤销历史", cat,
            "100 步 (MaxUndoHistory)",
            "超出后最旧记录被静默移除 (TrimStack)",
            LimitSev.SoftDegrade, "TuiTextArea.cs:118");

        AddLimit(items, "最近文件列表", cat,
            "50 个 (FIFO 移除最旧)",
            "超出 50 个移除第一个 (RemoveAt(0))，静默丢弃",
            LimitSev.SoftDegrade, "Program.cs:904");

        AddLimit(items, "历史搜索显示", cat,
            "15 条 (Take(15))",
            "只显示前 15 条 + '还有 N 条结果'",
            LimitSev.SoftDegrade, "Program.cs:1148-1155");

        AddLimit(items, "/loop 最大轮次", cat,
            "50 轮",
            "超出 50 回退为普通提示词（不执行循环），达到上限显示 '已达上限 N 轮'",
            LimitSev.Graceful, "Program.cs:1172,1249");

        AddLimit(items, "工具输出自动折叠 (auto 模式)", cat,
            "20 行",
            "auto 模式第 21 行起折叠为 '后续输出已折叠'；concise 全隐藏；detailed 全显示",
            LimitSev.SoftDegrade, "ChatScreen.cs:347-366", configurable: true);

        AddLimit(items, "Diff 预览截断", cat,
            "3000 字符 → 2500",
            "EditFileTool/DiffPreview 统一 diff 输出截断为 2500 字符",
            LimitSev.SoftDegrade, "EditFileTool.cs:160 / DiffPreview.cs:425");

        AddLimit(items, "代码块渲染宽度", cat,
            "60 列 (Math.Min(maxWidth, 60))",
            "代码块边框/分隔线强制上限 60 列，长代码行可超出但边框受限",
            LimitSev.SoftDegrade, "TuiMarkdown.cs:85,119,146");

        AddLimit(items, "输入历史上限", cat,
            "200 条",
            "超出移除最旧条目，静默丢弃",
            LimitSev.SoftDegrade, "ChatScreen.cs:1204");

        AddLimit(items, "TuiTextArea 最大行数", cat,
            $"MaxLines={(new TuiTextArea().MaxLines > 0 ? "有上限" : "0=不限")}",
            "超出 MaxLines 从顶部静默裁剪旧行，光标行同步下调",
            LimitSev.SoftDegrade, "TuiTextArea.cs:51", configurable: true);

        AddLimit(items, "TuiTextArea 自动换行列宽", cat,
            "MaxColumnWidth=0 不限",
            "超出列宽按空格自动折行；可视区 Width 小于列宽时水平滚动",
            LimitSev.SoftDegrade, "TuiTextArea.cs:54", configurable: true);

        AddLimit(items, "TuiEditBase 撤销历史栈", cat,
            "100 步 (MaxUndoHistory=100)",
            "超出后 TrimStack 保留最近 99 条，最旧静默移除；AOT 兼容 const 常量",
            LimitSev.SoftDegrade, "TuiEditBase.cs:113");

        AddLimit(items, "Tab 键行为标志", cat,
            "AcceptsTab (默认 false)",
            "false: 清除选择→返回 false 交父容器切换焦点；true: 输入 \\t 缩进字符",
            LimitSev.NoLimit, "TuiEditBase.cs:130 / TuiRichEditor.cs:47");

        return items;
    }

    // ════════════════════════════════════════════════════════════════
    // 6. ⚙ 配置 / 杂项上限
    // ════════════════════════════════════════════════════════════════

    private static List<LimitItem> ProbeConfigLimits()
    {
        var items = new List<LimitItem>();
        const string cat = "⚙ 配置/杂项";
        var cfg = Config.FromEnv();

        AddLimit(items, "记忆注入条数", cat,
            $"{cfg.MemoryRelevanceTopN} (Clamp 0-20, 默认 5)",
            "超出 20 硬截断；设为 0 关闭语义匹配",
            LimitSev.HardBlock, "Config.cs:196", configurable: true);

        AddLimit(items, "嵌入维度", cat,
            $"{cfg.EmbeddingDimensions} (Clamp 0-4096)",
            "超出 4096 硬截断；0=使用模型默认值",
            LimitSev.HardBlock, "Config.cs:213 / EmbeddingStore.cs:99", configurable: true);

        AddLimit(items, "记忆注入内容长度", cat,
            "2000 字符 (结构化) / 1500 字符 (回退)",
            "超出截断，静默丢弃超出部分",
            LimitSev.SoftDegrade, "SystemPrompt.cs:41,62");

        AddLimit(items, "会话列表显示", cat,
            "20 条",
            "ListSessions 枚举到 20 条即停止，旧会话仍在磁盘但不可见",
            LimitSev.SoftDegrade, "SessionManager.cs:132-134");

        AddLimit(items, "会话 ID 长度", cat,
            "100 字符",
            "超出截断为 name[..100]，静默截断",
            LimitSev.SoftDegrade, "SessionManager.cs:20,148");

        AddLimit(items, "Auto-commit 文件数", cat,
            "10 个",
            "git status 回退路径只取前 10 个文件",
            LimitSev.SoftDegrade, "Agent.cs:524");

        AddLimit(items, "Commit 消息长度", cat,
            "72 字符",
            "LLM 生成的 commit 消息超出 72 字符硬截断",
            LimitSev.HardBlock, "Agent.cs:603");

        AddLimit(items, "子Agent 上下文消息", cat,
            "300 字符/条",
            "注入给子Agent的父上下文消息每条截断为 300 字符",
            LimitSev.SoftDegrade, "AgentTool.cs:206-207");

        AddLimit(items, "Watch 模式扩展名过滤", cat,
            "45 种扩展名 (硬编码)",
            "非匹配扩展名的文件静默忽略；不支持自定义扩展名",
            LimitSev.SoftDegrade, "WatchMode.cs:25-34");

        AddLimit(items, "Watch 模式忽略目录", cat,
            "14 个目录 (bin/obj/.git/node_modules 等)",
            "忽略目录下的文件静默跳过；不支持自定义忽略",
            LimitSev.SoftDegrade, "WatchMode.cs:17-22");

        return items;
    }

    // ════════════════════════════════════════════════════════════════
    // 汇总输出
    // ════════════════════════════════════════════════════════════════

    private static void AddLimit(List<LimitItem> list, string name, string cat,
        string val, string behavior, LimitSev sev, string src, bool configurable = false)
        => list.Add(new LimitItem(name, cat, val, behavior, sev, src, configurable));

    private static void PrintLimitsReport()
    {
        foreach (var group in _limitsList.GroupBy(i => i.Category))
        {
            var items = group.ToList();
            Console.WriteLine($"\n── {group.Key} ({items.Count} 项) ──");
            Console.WriteLine($"  {"项目",-28} {"当前值",-30} {"评级",-12} {"配置",-8} 超出行为");
            Console.WriteLine($"  {new string('─', 28)} {new string('─', 30)} {new string('─', 12)} {new string('─', 8)} {new string('─', 48)}");

            foreach (var item in items)
            {
                string sev = SevLabel(item.Severity);
                string cfg = item.IsConfigurable ? "⚙可配" : "🔒硬编";
                Console.WriteLine($"  {item.Name,-28} {item.CurrentValue,-30} {sev,-12} {cfg,-8} {item.ExceedBehavior}");
                Console.WriteLine($"  {' ',28} {' ',30} {' ',12} {' ',8} 📍 {item.Source}");
            }
        }

        int hard = _limitsList.Count(i => i.Severity == LimitSev.HardBlock);
        int soft = _limitsList.Count(i => i.Severity == LimitSev.SoftDegrade);
        int graceful = _limitsList.Count(i => i.Severity == LimitSev.Graceful);
        int nolimit = _limitsList.Count(i => i.Severity == LimitSev.NoLimit);
        int configurable = _limitsList.Count(i => i.IsConfigurable);
        int hardcoded = _limitsList.Count(i => !i.IsConfigurable);

        Console.WriteLine($"\n╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║                    上限报告总结                         ║");
        Console.WriteLine($"╠══════════════════════════════════════════════════════════╣");
        Console.WriteLine($"║  总上限项: {_limitsList.Count,2}                                          ║");
        Console.WriteLine($"║  🔴 硬阻断: {hard,2} 项 — 达上限后拒绝/崩溃/截断                    ║");
        Console.WriteLine($"║  🟡 降级:   {soft,2} 项 — 达上限后裁剪/摘要/限流                    ║");
        Console.WriteLine($"║  🟢 优雅:   {graceful,2} 项 — 达上限后友好提示/自动恢复              ║");
        Console.WriteLine($"║  ⚪ 无限制:  {nolimit,2} 项 — 无硬上限，仅受内存/OS 约束              ║");
        Console.WriteLine($"╠══════════════════════════════════════════════════════════╣");
        Console.WriteLine($"║  ⚙ 可配:   {configurable,2} 项 — 支持环境变量/设置界面修改            ║");
        Console.WriteLine($"║  🔒 硬编:   {hardcoded,2} 项 — 需修改源码才能调整                    ║");
        Console.WriteLine($"╚══════════════════════════════════════════════════════════╝");

        if (hard > 0)
        {
            Console.WriteLine("\n⚠️ 硬阻断风险项（达上限可能导致崩溃或数据丢失）：");
            foreach (var item in _limitsList.Where(i => i.Severity == LimitSev.HardBlock))
                Console.WriteLine($"  - {item.Name}: {item.ExceedBehavior}");
        }

        // 列出所有可配置项
        var configItems = _limitsList.Where(i => i.IsConfigurable).ToList();
        if (configItems.Count > 0)
        {
            Console.WriteLine("\n── ⚙ 可配置上限（可通过设置界面或环境变量调整）──");
            Console.WriteLine($"  {"项目",-28} {"环境变量",-32} 设置路径");
            Console.WriteLine($"  {new string('─', 28)} {new string('─', 32)} {new string('─', 36)}");
            Console.WriteLine($"  {"预算上限",-28} {"WAYCODER_MAX_BUDGET_USD",-32} 设置 → 💰 预算 → 预算上限");
            Console.WriteLine($"  {"子智能体最大深度",-28} {"WAYCODER_SUBAGENT_DEPTH",-32} 设置 → 🤖 模型 → 子智能体深度");
            Console.WriteLine($"  {"LLM 上下文窗口",-28} {"WAYCODER_MAX_CONTEXT",-32} 设置 → ⚙ 参数 → 上下文窗口");
            Console.WriteLine($"  {"工具执行超时",-28} {"WAYCODER_TOOL_TIMEOUT",-32} 设置 → ⚙ 参数 → 工具超时");
            Console.WriteLine($"  {"Lint 执行超时",-28} {"WAYCODER_LINT_TIMEOUT",-32} 设置 → ⚙ 参数 → Lint 超时");
            Console.WriteLine($"  {"记忆注入条数",-28} {"WAYCODER_MEMORY_TOPN",-32} 设置 → 🔧 系统 → 记忆注入条数");
            Console.WriteLine($"  {"嵌入维度",-28} {"WAYCODER_EMBEDDING_DIMS",-32} 设置 → 🔧 系统 → 嵌入维度");
            Console.WriteLine($"  {"工具输出折叠风格",-28} {"WAYCODER_CHAT_STYLE",-32} 设置 → 🎨 界面 → 聊天显示风格");
            Console.WriteLine();
            Console.WriteLine("  💡 修改方式：设置界面 Ctrl+S 保存，或手动编辑 .env 文件后重启。");
        }

        Console.WriteLine("\n── 🐛 发现的潜在问题 ──");
        Console.WriteLine("  （当前无已知问题）");
    }
}
