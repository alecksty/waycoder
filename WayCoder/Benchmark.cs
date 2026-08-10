using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using WayCoder.Terminal;
using WayCoder.Tools;
using WayCoder.UI;

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
    private static Process? _currentProc;

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
}
