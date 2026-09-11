using System.IO.Compression;
using System.Text;
using System.Text.Json;
using WayCoder.Infra;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.Tui.Screens;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>状态栏路径信息（git 分支探测 + cwd 格式化）测试</summary>
    private static void TestPathStatus(Action<string, bool> Check)
    {
        // 普通仓库（.git 目录）
        var dir = Path.Combine(Path.GetTempPath(), "wc_git_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(Path.Combine(dir, ".git"));
        File.WriteAllText(Path.Combine(dir, ".git", "HEAD"), "ref: refs/heads/main\n");
        Check("路径: 普通仓库分支", PathStatus.TryGetBranch(dir) == "main");

        // detached HEAD → 短哈希
        File.WriteAllText(Path.Combine(dir, ".git", "HEAD"), "a1b2c3d4e5f6789\n");
        Check("路径: detached HEAD 短哈希", PathStatus.TryGetBranch(dir) == "a1b2c3d4");

        // 非 git 仓库
        var noGit = Path.Combine(Path.GetTempPath(), "wc_nogit_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(noGit);
        Check("路径: 非仓库返回 null", PathStatus.TryGetBranch(noGit) == null);

        // worktree（.git 为文件指向 gitdir）
        var wtGitDir = Path.Combine(Path.GetTempPath(), "wc_wtg_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(wtGitDir);
        File.WriteAllText(Path.Combine(wtGitDir, "HEAD"), "ref: refs/heads/feature/x\n");
        var wt = Path.Combine(Path.GetTempPath(), "wc_wt_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(wt);
        File.WriteAllText(Path.Combine(wt, ".git"), "gitdir: " + wtGitDir + "\n");
        Check("路径: worktree 分支", PathStatus.TryGetBranch(wt) == "feature/x");

        // FormatCwd：home 展开（基准 = Global.Home，与 PathStatus.FormatCwd 对齐；
        // 自测框架会把 HomeOverride 指向临时目录，用 UserProfile 作期望值在新实现下永远不成立）
        var home = WayCoder.Global.Home;
        var sub = Path.Combine(home, "Desktop", "proj");
        var expectedHome = "~" + Path.DirectorySeparatorChar + "Desktop" + Path.DirectorySeparatorChar + "proj";
        Check("路径: home 展开为 ~", PathStatus.FormatCwd(sub) == expectedHome);
        Check("路径: 非 home 路径原样", PathStatus.FormatCwd("/usr/local/bin") == "/usr/local/bin");

        try { Directory.Delete(dir, true); } catch { }
        try { Directory.Delete(noGit, true); } catch { }
        try { Directory.Delete(wtGitDir, true); } catch { }
        try { Directory.Delete(wt, true); } catch { }
    }

    /// <summary>AllowedTools 通用白名单参与 FilterTools 决策链（Claude Code --allowedTools 对齐）</summary>
    private static void TestAllowedToolsFilter(Action<string, bool> Check)
    {
        var cfg = Config.Instance;
        var savedAllowed = cfg.AllowedTools;
        var savedBuild = cfg.BuildToolAllowList;
        var savedYolo = cfg.YoloToolAllowList;
        var savedDisabled = cfg.DisabledTools;
        var savedEconomy = cfg.EconomyMode;
        var savedPerm = PermissionManager.CurrentMode;
        try
        {
            var agent = new Agent(new LLM("test", "sk-test"));
            cfg.EconomyMode = EconomyMode.Off;
            cfg.DisabledTools = "";
            cfg.BuildToolAllowList = "";
            cfg.YoloToolAllowList = "";
            PermissionManager.CurrentMode = PermissionManager.Mode.Ask;
            agent.WorkMode = WorkMode.Build;

            cfg.AllowedTools = "read_file,write_file";
            agent.ReapplyToolFilter();
            var names = agent.Tools.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Check("AllowedTools: Build 仅保留通用白名单", names.SetEquals(new[] { "read_file", "write_file" }));

            cfg.AllowedTools = "read_file";
            cfg.BuildToolAllowList = "grep";
            agent.ReapplyToolFilter();
            names = agent.Tools.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Check("AllowedTools: 与 BuildToolAllowList 合并", names.SetEquals(new[] { "read_file", "grep" }));

            cfg.YoloToolAllowList = "bash";
            PermissionManager.CurrentMode = PermissionManager.Mode.Yolo;
            agent.ReapplyToolFilter();
            names = agent.Tools.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Check("AllowedTools: YOLO 覆盖为 YoloToolAllowList 合并", names.SetEquals(new[] { "read_file", "bash" }));

            cfg.AllowedTools = "";
            cfg.BuildToolAllowList = "";
            cfg.YoloToolAllowList = "";
            cfg.DisabledTools = "write_file";
            PermissionManager.CurrentMode = PermissionManager.Mode.Ask;
            agent.ReapplyToolFilter();
            Check("AllowedTools: DisabledTools 仍最后剔除",
                !agent.Tools.Any(t => t.Name.Equals("write_file", StringComparison.OrdinalIgnoreCase)));
        }
        finally
        {
            cfg.AllowedTools = savedAllowed;
            cfg.BuildToolAllowList = savedBuild;
            cfg.YoloToolAllowList = savedYolo;
            cfg.DisabledTools = savedDisabled;
            cfg.EconomyMode = savedEconomy;
            PermissionManager.CurrentMode = savedPerm;
        }
    }

    private static void TestProcessTools(Action<string, bool> Check)
    {
        // ── 进程名安全白名单（防 shell 命令注入）──
        Check("kill: 合法名 node", KillTool.IsSafeProcessName("node"));
        Check("kill: 合法名 dotnet", KillTool.IsSafeProcessName("dotnet"));
        Check("kill: 合法名含空格", KillTool.IsSafeProcessName("Google Chrome"));
        Check("kill: 合法名含点", KillTool.IsSafeProcessName("python3.11"));
        Check("kill: 合法名含连字符/下划线", KillTool.IsSafeProcessName("foo-bar_baz"));
        Check("kill: 空串拒绝", !KillTool.IsSafeProcessName(""));
        Check("kill: 空白拒绝", !KillTool.IsSafeProcessName("   "));
        Check("kill: 分号注入拒绝", !KillTool.IsSafeProcessName("foo; rm -rf /"));
        Check("kill: 管道注入拒绝", !KillTool.IsSafeProcessName("foo|bar"));
        Check("kill: 命令替换拒绝", !KillTool.IsSafeProcessName("foo$(rm -rf /)"));
        Check("kill: 反引号拒绝", !KillTool.IsSafeProcessName("foo`bar`"));
        Check("kill: 重定向拒绝", !KillTool.IsSafeProcessName("foo>bar"));
        Check("kill: 换行注入拒绝", !KillTool.IsSafeProcessName("foo\nbar"));

        // ── kill 工具注入拦截（不执行真实命令，早退返回错误）──
        Check("kill: 非法进程名拦截",
            new KillTool().ExecuteAsync(new() { ["name"] = "foo; rm -rf /" }).Result.Contains("非法字符"));
        Check("kill: 空进程名拦截",
            new KillTool().ExecuteAsync(new() { ["name"] = "" }).Result.Contains("不能为空"));
        Check("kill: 系统关键进程拦截",
            new KillTool().ExecuteAsync(new() { ["name"] = "System" }).Result.Contains("系统关键进程"));
        Check("kill: 缺失参数提示",
            new KillTool().ExecuteAsync(new() { }).Result.Contains("必须指定"));

        // ── ps 工具注入拦截 ──
        Check("ps: 非法进程名拦截",
            new PsTool().ExecuteAsync(new() { ["name"] = "foo; rm -rf /" }).Result.Contains("非法字符"));

        // ── cmd.exe / powershell 启动点的输出解码（v0.96.102）──
        // Windows 上 cmd.exe 及其子命令（tasklist/taskkill）向**重定向管道**写的是系统 OEM 代码页字节
        // （中文系统 GBK），不设解码则中文进程名/提示语全是乱码。实测 `tasklist /NH` 输出含非 ASCII
        // （「微信开发者工具.exe」）、`taskkill` 的「错误: 没有找到进程…」、`powershell` 的中文回显，
        // 三者按 UTF-8 解码**全部失败**。这三处此前漏了 `ProcEncoding.Apply`（CLAUDE.md 既有铁律）。
        if (OperatingSystem.IsWindows())
        {
            var oem = WayCoder.Infra.ProcEncoding.OemEncoding;
            Check("cmd 解码: ps 的 psi 设了 OEM 输出解码",
                PsTool.BuildPsi("cmd.exe", "/c tasklist").StandardOutputEncoding == oem);
            Check("cmd 解码: kill 的 psi 设了 OEM 输出解码",
                KillTool.BuildPsi("cmd.exe", "/c taskkill").StandardOutputEncoding == oem);

            // 端到端：真跑一次 cmd 回显中文，断言解码后拿到原字（含替换字符即失败）
            try
            {
                var psi = PsTool.BuildPsi("cmd.exe", "/c echo 中文测试ABC");
                var r = WayCoder.Infra.ProcUtil.RunAsync(psi, 5000).GetAwaiter().GetResult();
                var outText = r?.Stdout ?? "";
                Check("cmd 解码: cmd 回显中文端到端不乱码",
                    outText.Contains("中文测试ABC") && !outText.Contains('�'));
            }
            catch (Exception ex) { Check($"cmd 解码: 端到端异常 {ex.Message}", false); }
        }
        else
        {
            Check("cmd 解码: 非 Windows 不设解码（/bin/bash 本就 UTF-8）",
                PsTool.BuildPsi("/bin/bash", "-c ps").StandardOutputEncoding is null);
        }
    }

    private static void TestSsgfGuard(Action<string, bool> Check)
    {
        // ── IP 内网/保留地址判断 ──
        Check("SSRF: 127.0.0.1 环回", SsgfGuard.IsPrivateIp("127.0.0.1"));
        Check("SSRF: 10.0.0.1 私网", SsgfGuard.IsPrivateIp("10.0.0.1"));
        Check("SSRF: 172.16.0.1 私网", SsgfGuard.IsPrivateIp("172.16.0.1"));
        Check("SSRF: 172.31.255.255 私网上界", SsgfGuard.IsPrivateIp("172.31.255.255"));
        Check("SSRF: 192.168.1.1 私网", SsgfGuard.IsPrivateIp("192.168.1.1"));
        Check("SSRF: 169.254.169.254 云元数据", SsgfGuard.IsPrivateIp("169.254.169.254"));
        Check("SSRF: 100.64.0.1 CGNAT", SsgfGuard.IsPrivateIp("100.64.0.1"));
        Check("SSRF: 0.0.0.0 保留", SsgfGuard.IsPrivateIp("0.0.0.0"));
        Check("SSRF: 224.0.0.1 组播", SsgfGuard.IsPrivateIp("224.0.0.1"));
        Check("SSRF: ::1 IPv6 环回", SsgfGuard.IsPrivateIp("::1"));
        Check("SSRF: fc00::1 ULA", SsgfGuard.IsPrivateIp("fc00::1"));
        Check("SSRF: fe80::1 链路本地", SsgfGuard.IsPrivateIp("fe80::1"));
        Check("SSRF: 8.8.8.8 公网放行", !SsgfGuard.IsPrivateIp("8.8.8.8"));
        Check("SSRF: 1.1.1.1 公网放行", !SsgfGuard.IsPrivateIp("1.1.1.1"));
        Check("SSRF: 114.114.114.114 公网放行", !SsgfGuard.IsPrivateIp("114.114.114.114"));
        Check("SSRF: 172.15.0.1 边界外公网", !SsgfGuard.IsPrivateIp("172.15.0.1"));
        Check("SSRF: 172.32.0.1 边界外公网", !SsgfGuard.IsPrivateIp("172.32.0.1"));

        // ── URL 校验 ──
        Check("SSRF: 公网 URL 放行", SsgfGuard.CheckUrl("https://example.com/docs").safe);
        Check("SSRF: 内网 IP URL 拦截", !SsgfGuard.CheckUrl("http://127.0.0.1:8080/admin").safe);
        Check("SSRF: 云元数据 URL 拦截", !SsgfGuard.CheckUrl("http://169.254.169.254/latest/meta-data/").safe);
        Check("SSRF: 内网段 URL 拦截", !SsgfGuard.CheckUrl("http://192.168.1.1/").safe);
        Check("SSRF: 10 段 URL 拦截", !SsgfGuard.CheckUrl("http://10.0.0.5:3000/").safe);
        Check("SSRF: localhost 拦截", !SsgfGuard.CheckUrl("http://localhost:3000/").safe);
        Check("SSRF: 内部域名拦截", !SsgfGuard.CheckUrl("http://db.internal/api").safe);
        Check("SSRF: file:// 拦截", !SsgfGuard.CheckUrl("file:///etc/passwd").safe);
        Check("SSRF: ftp:// 拦截", !SsgfGuard.CheckUrl("ftp://example.com/x").safe);
        Check("SSRF: IPv6 环回 URL 拦截", !SsgfGuard.CheckUrl("http://[::1]:8080/").safe);

        // ── 重定向状态码判断 ──
        Check("SSRF: 301 是重定向", SsgfGuard.IsRedirect(301));
        Check("SSRF: 302 是重定向", SsgfGuard.IsRedirect(302));
        Check("SSRF: 303 是重定向", SsgfGuard.IsRedirect(303));
        Check("SSRF: 307 是重定向", SsgfGuard.IsRedirect(307));
        Check("SSRF: 308 是重定向", SsgfGuard.IsRedirect(308));
        Check("SSRF: 200 非重定向", !SsgfGuard.IsRedirect(200));
        Check("SSRF: 404 非重定向", !SsgfGuard.IsRedirect(404));
    }

    private static void TestJsonMode(Action<string, bool> Check)
    {
        // ── 成功结果 ──
        var ok = JsonResult.Build(
            success: true,
            answer: "任务完成",
            error: null,
            model: "deepseek-v4-pro",
            promptTokens: 100,
            completionTokens: 50,
            costUsd: 0.00123,
            durationMs: 2048,
            changedFiles: new[] { "a.cs", "b.cs" });

        Check("JSON: success=true", ok["success"]!.AsBool() == true);
        Check("JSON: answer 透传", ok["answer"]!.AsString() == "任务完成");
        Check("JSON: error 为 null", ok["error"]!.IsNull);
        Check("JSON: model 透传", ok["model"]!.AsString() == "deepseek-v4-pro");
        Check("JSON: usage.total = prompt+completion",
            (int)ok["usage"]!["total_tokens"]!.AsNumber() == 150);
        Check("JSON: cost_usd 保留", Math.Abs(ok["cost_usd"]!.AsNumber() - 0.00123) < 1e-9);
        Check("JSON: duration_ms", (long)ok["duration_ms"]!.AsNumber() == 2048);
        Check("JSON: changed_files 数组", ok["changed_files"] is JNode { Kind: JKind.Array } arr && arr.Count == 2);
        Check("JSON: 序列化可解析", Json.Parse(ok.ToJson()) is JNode { Kind: JKind.Object });

        // ── 失败结果 ──
        var fail = JsonResult.Build(false, "", "请求超时", "m", 0, 0, null, 1, null);
        Check("JSON: success=false", fail["success"]!.AsBool() == false);
        Check("JSON: error 透传", fail["error"]!.AsString() == "请求超时");
        Check("JSON: answer 空串兜底", fail["answer"]!.AsString() == "");
        Check("JSON: cost_usd null", fail["cost_usd"]!.IsNull);
        Check("JSON: changed_files 空数组", fail["changed_files"] is JNode { Kind: JKind.Array } e && e.Count == 0);
    }

    /// <summary>智能重试策略（RetryPolicy）单元测试：异常过滤 + 指数退避 + 重试耗尽。</summary>
    private static void TestRetryPolicy(Action<string, bool> Check)
    {
        // ── ShouldRetry 黑名单（默认禁止的参数/状态异常）──
        Check("Retry: 黑名单拒 ArgumentException",
            !new RetryConfig().ShouldRetry(new ArgumentException("x")));
        Check("Retry: 黑名单拒 OperationCanceledException",
            !new RetryConfig().ShouldRetry(new OperationCanceledException()));
        Check("Retry: 黑名单拒 InvalidOperationException",
            !new RetryConfig().ShouldRetry(new InvalidOperationException()));
        Check("Retry: 默认允许 IOException（瞬时错误）",
            new RetryConfig().ShouldRetry(new IOException("x")));

        // ── 白名单：只重试指定类型 ──
        var whitelist = new RetryConfig
        {
            RetryableExceptions = new HashSet<string> { "System.IO.IOException" },
        };
        Check("Retry: 白名单命中 IOException 重试", whitelist.ShouldRetry(new IOException("x")));
        Check("Retry: 白名单未命中 TimeoutException 不重试",
            !whitelist.ShouldRetry(new TimeoutException("x")));

        // ── RetryAsync：首次成功不重试 ──
        var attempts = 0;
        var ok = RetryPolicy.RetryAsync<int>(() => Task.FromResult(++attempts), new RetryConfig { MaxRetries = 3 })
            .GetAwaiter().GetResult();
        Check("Retry: 首次成功不重试", ok == 1 && attempts == 1);

        // ── RetryAsync：失败 N 次后成功 ──
        var attempts2 = 0;
        var ok2 = RetryPolicy.RetryAsync<int>(() =>
        {
            attempts2++;
            if (attempts2 < 3) throw new IOException("瞬时错误");
            return Task.FromResult(42);
        }, new RetryConfig { MaxRetries = 3, BaseDelayMs = 1, MaxDelayMs = 10 })
            .GetAwaiter().GetResult();
        Check("Retry: 失败 2 次后成功", ok2 == 42 && attempts2 == 3);

        // ── RetryAsync：耗尽重试原样抛出最后一次异常（非 AggregateException）──
        var attempts3 = 0;
        var threw = false;
        try
        {
            RetryPolicy.RetryAsync<int>(() => { attempts3++; throw new IOException("一直失败"); },
                new RetryConfig { MaxRetries = 2, BaseDelayMs = 1, MaxDelayMs = 10 })
                .GetAwaiter().GetResult();
        }
        catch (IOException) { threw = true; }
        Check("Retry: 耗尽重试原样抛出最后一次异常", threw);
        Check("Retry: 耗尽后尝试次数 = MaxRetries+1", attempts3 == 3);

        // ── 指数退避：延迟 100→200→400 递增（禁用 jitter 以确定性断言）──
        var delays = new List<int>();
        try
        {
            RetryPolicy.RetryAsync<int>(() => { throw new IOException("x"); },
                new RetryConfig { MaxRetries = 3, BaseDelayMs = 100, MaxDelayMs = 5000, JitterRatio = 0 },
                (_, _, delay) => delays.Add(delay))
                .GetAwaiter().GetResult();
        }
        catch { }
        Check("Retry: 指数退避 100→200→400", delays.SequenceEqual(new[] { 100, 200, 400 }));

        // ── 对称 jitter：延迟在 ±ratio 范围内抖动（纯函数断言）──
        Check("Retry: jitter 下限 -10%", RetryPolicy.ComputeJitteredDelay(100, 0.1, 0.0) == 90);
        Check("Retry: jitter 中点不变", RetryPolicy.ComputeJitteredDelay(100, 0.1, 0.5) == 100);
        Check("Retry: jitter 上限 +10%", RetryPolicy.ComputeJitteredDelay(100, 0.1, 1.0) == 110);
        Check("Retry: jitter 0 禁用", RetryPolicy.ComputeJitteredDelay(100, 0.0, 0.3) == 100);
        Check("Retry: jitter 负值禁用", RetryPolicy.ComputeJitteredDelay(100, -0.2, 0.3) == 100);
        // 实际重试应产生非确定延迟（在 ±10% 范围内）
        var jitterDelays = new List<int>();
        try
        {
            RetryPolicy.RetryAsync<int>(() => { throw new IOException("x"); },
                new RetryConfig { MaxRetries = 3, BaseDelayMs = 100, MaxDelayMs = 5000, JitterRatio = 0.1 },
                (_, _, delay) => jitterDelays.Add(delay))
                .GetAwaiter().GetResult();
        }
        catch { }
        Check("Retry: jitter 实际延迟在 ±10% 内",
            jitterDelays.Count == 3 &&
            Math.Abs(jitterDelays[0] - 100) <= 10 &&
            Math.Abs(jitterDelays[1] - 200) <= 20 &&
            Math.Abs(jitterDelays[2] - 400) <= 40);

        // ── 无返回值版本 ──
        var ran = false;
        RetryPolicy.RetryAsync(async () => { ran = true; await Task.CompletedTask; }).GetAwaiter().GetResult();
        Check("Retry: 无返回值版本执行", ran);
    }

    /// <summary>工具调用调度器（ToolCallScheduler）：ExecutionMode 分批 + 有界并发 + 工具模式标注。</summary>
    private static void TestToolScheduler(Action<string, bool> Check)
    {
        static ToolCall C(string id, string name) => new(id, name, new Dictionary<string, object?>());

        ToolExecutionMode Mode(string name) =>
            name is "bash" or "write_file" or "edit_file" ? ToolExecutionMode.Exclusive
            : ToolExecutionMode.Parallel;

        // ── Partition 纯逻辑 ──
        var allParallel = new List<ToolCall> { C("1", "read_file"), C("2", "grep"), C("3", "glob") };
        var pAll = ToolCallScheduler.Partition(allParallel, Mode);
        Check("Sched: 全 Parallel 合并为一批", pAll.Count == 1 && pAll[0].Count == 3);

        var allExclusive = new List<ToolCall> { C("1", "bash"), C("2", "write_file") };
        var pExcl = ToolCallScheduler.Partition(allExclusive, Mode);
        Check("Sched: 全 Exclusive 各占一批", pExcl.Count == 2 && pExcl.All(b => b.Count == 1));

        var mixed = new List<ToolCall>
        {
            C("1", "read_file"), C("2", "grep"),
            C("3", "bash"),
            C("4", "read_file"),
            C("5", "edit_file"),
            C("6", "glob"), C("7", "ls"),
        };
        var pMixed = ToolCallScheduler.Partition(mixed, Mode);
        Check("Sched: 混合分批 [[P,P],[E],[P],[E],[P,P]]",
            pMixed.Count == 5 &&
            pMixed[0].Count == 2 && pMixed[0][0].Id == "1" && pMixed[0][1].Id == "2" &&
            pMixed[1].Count == 1 && pMixed[1][0].Id == "3" &&
            pMixed[2].Count == 1 && pMixed[2][0].Id == "4" &&
            pMixed[3].Count == 1 && pMixed[3][0].Id == "5" &&
            pMixed[4].Count == 2 && pMixed[4][0].Id == "6" && pMixed[4][1].Id == "7");

        Check("Sched: 空列表返回空批次", ToolCallScheduler.Partition(new List<ToolCall>(), Mode).Count == 0);

        // 未知工具由调用方 GetExecutionMode 保守按 Exclusive 处理（此处用显式 modeOf 模拟）
        var unknown = new List<ToolCall> { C("1", "nosuch_tool"), C("2", "read_file") };
        var pUnknown = ToolCallScheduler.Partition(unknown,
            n => n == "nosuch_tool" ? ToolExecutionMode.Exclusive : ToolExecutionMode.Parallel);
        Check("Sched: 未知工具保守 Exclusive 独占", pUnknown.Count == 2 && pUnknown[0].Count == 1);

        // ── 有界并发常量 ──
        Check("Sched: 并发上限 1..16", ToolCallScheduler.MaxParallelism > 0 && ToolCallScheduler.MaxParallelism <= 16);

        // ── 实际工具 ExecutionMode 标注 ──
        Check("Sched: bash Exclusive", ToolRegistry.GetTool("bash")!.ExecutionMode == ToolExecutionMode.Exclusive);
        Check("Sched: write_file Exclusive", ToolRegistry.GetTool("write_file")!.ExecutionMode == ToolExecutionMode.Exclusive);
        Check("Sched: edit_file Exclusive", ToolRegistry.GetTool("edit_file")!.ExecutionMode == ToolExecutionMode.Exclusive);
        Check("Sched: agent Exclusive", ToolRegistry.GetTool("agent")!.ExecutionMode == ToolExecutionMode.Exclusive);
        Check("Sched: lsp Exclusive", ToolRegistry.GetTool("lsp")!.ExecutionMode == ToolExecutionMode.Exclusive);
        Check("Sched: read_file Parallel", ToolRegistry.GetTool("read_file")!.ExecutionMode == ToolExecutionMode.Parallel);
        Check("Sched: grep Parallel", ToolRegistry.GetTool("grep")!.ExecutionMode == ToolExecutionMode.Parallel);
        Check("Sched: glob Parallel", ToolRegistry.GetTool("glob")!.ExecutionMode == ToolExecutionMode.Parallel);
        Check("Sched: web_search Parallel", ToolRegistry.GetTool("web_search")!.ExecutionMode == ToolExecutionMode.Parallel);
    }

    /// <summary>工具结果分类器（ToolResultClassifier）：真实错误 vs 用户取消/安全阻止。</summary>
    private static void TestToolResultClassifier(Action<string, bool> Check)
    {
        // ── 真实错误（可重试，注入自恢复提示）──
        Check("Cls: 错误 全角冒号", ToolResultClassifier.IsError("错误：文件不存在"));
        Check("Cls: 错误 半角冒号", ToolResultClassifier.IsError("错误: 参数缺失"));
        Check("Cls: Error 英文", ToolResultClassifier.IsError("Error: permission denied"));
        Check("Cls: ❌ 失败", ToolResultClassifier.IsError("❌ 测试失败（exit 1）"));
        Check("Cls: ❌ 文件锁定", ToolResultClassifier.IsError("❌ 文件被锁定: 忙"));
        Check("Cls: bash 出错", ToolResultClassifier.IsError("运行命令时出错：IOException"));
        Check("Cls: 失败 前缀", ToolResultClassifier.IsError("失败：编译错误"));
        Check("Cls: 前导空白仍识别", ToolResultClassifier.IsError("  错误：x"));

        // ── 中止类（非错误，不注入重试提示）──
        Check("Cls: 用户取消非错误", !ToolResultClassifier.IsError("用户取消了此操作。"));
        Check("Cls: Hook 阻止非错误", !ToolResultClassifier.IsError("操作被 Hook 阻止: 政策"));
        Check("Cls: 危险命令阻止非错误", !ToolResultClassifier.IsError("⚠ 已阻止：强制递归删除"));
        Check("Cls: 沙箱阻止非错误", !ToolResultClassifier.IsError("⛔ 沙箱阻止：越界"));
        Check("Cls: 取消是中止", ToolResultClassifier.IsAbort("用户取消了此操作。"));

        // ── 成功/正常结果（非错误）──
        Check("Cls: 成功非错误", !ToolResultClassifier.IsError("已写入 12 行到 /tmp/a.cs"));
        Check("Cls: 空非错误", !ToolResultClassifier.IsError(null));
        Check("Cls: 空白非错误", !ToolResultClassifier.IsError("   "));
        Check("Cls: 无输出非错误", !ToolResultClassifier.IsError("（无输出）"));
    }

    /// <summary>LRU 缓存（LruCache）单元测试：容量淘汰、LRU 提升、TTL 过期、事件与统计。</summary>
    private static void TestLruCache(Action<string, bool> Check)
    {
        // ── 基本 Put/Get + 命中统计 ──
        var cache = new LruCache<string, int>(3);
        cache.Put("a", 1);
        cache.Put("b", 2);
        Check("LRU: 基本 Get", cache.Get("a") == 1 && cache.Get("b") == 2);
        Check("LRU: 未命中返回默认", cache.Get("missing") == 0);
        Check("LRU: 命中/未命中统计", cache.Hits == 2 && cache.Misses == 1);

        // ── 容量淘汰（淘汰最久未使用）──
        var cache2 = new LruCache<string, int>(2);
        cache2.Put("a", 1);
        cache2.Put("b", 2);
        cache2.Put("c", 3); // 淘汰 a
        Check("LRU: 容量淘汰最旧",
            !cache2.ContainsKey("a") && cache2.ContainsKey("b") && cache2.ContainsKey("c"));
        Check("LRU: 淘汰计数", cache2.Evictions == 1);

        // ── Get 提升 LRU（最近使用不被淘汰）──
        var cache3 = new LruCache<string, int>(2);
        cache3.Put("a", 1);
        cache3.Put("b", 2);
        cache3.Get("a");     // 提升 a 为最近使用
        cache3.Put("c", 3);  // 淘汰 b（而非 a）
        Check("LRU: Get 提升最近使用",
            cache3.ContainsKey("a") && !cache3.ContainsKey("b") && cache3.ContainsKey("c"));

        // ── TTL 过期 ──
        var cache4 = new LruCache<string, int>(3);
        cache4.Put("a", 1, TimeSpan.FromMilliseconds(1));
        System.Threading.Thread.Sleep(20);
        Check("LRU: TTL 过期后 Get 返回默认", cache4.Get("a") == 0);
        Check("LRU: TTL 过期后 ContainsKey false", !cache4.ContainsKey("a"));

        // ── Remove / Clear / OnEvicted 事件 ──
        var evicted = new List<string>();
        var cache5 = new LruCache<string, int>(3);
        cache5.OnEvicted += (k, _) => evicted.Add(k);
        cache5.Put("a", 1);
        cache5.Put("b", 2);
        Check("LRU: Remove 返回 true", cache5.Remove("a"));
        Check("LRU: Remove 不存在的键返回 false", !cache5.Remove("zzz"));
        Check("LRU: Remove 触发 OnEvicted", evicted.Contains("a"));
        cache5.Clear();
        Check("LRU: Clear 清空", cache5.Count == 0);
        Check("LRU: Clear 触发 OnEvicted", evicted.Contains("b"));

        // ── TryGet ──
        var cache6 = new LruCache<string, int>(2);
        cache6.Put("a", 42);
        Check("LRU: TryGet 命中", cache6.TryGet("a", out var v) && v == 42);
        Check("LRU: TryGet 未命中", !cache6.TryGet("missing", out _));

        // ── 容量 ≤0 抛异常 ──
        var threwCap = false;
        try { new LruCache<string, int>(0); } catch (ArgumentOutOfRangeException) { threwCap = true; }
        Check("LRU: 容量 ≤0 抛异常", threwCap);
    }

    /// <summary>短 ID 生成器（IdGenerator）单元测试：字符集、唯一性、slug 格式。</summary>
    private static void TestIdGenerator(Action<string, bool> Check)
    {
        // ── NewId 长度 + 安全字符集 ──
        var id = IdGenerator.NewId(8);
        Check("ID: NewId 长度", id.Length == 8);
        const string safe = "abcdefghjkmnpqrstuvwxyz23456789";
        Check("ID: NewId 字符集安全（无 0/o/1/l/i）", id.All(safe.Contains));

        // ── NewId 唯一性 ──
        var ids = new HashSet<string>();
        for (int i = 0; i < 100; i++) ids.Add(IdGenerator.NewId());
        Check("ID: 100 个 NewId 无重复", ids.Count == 100);

        // ── 长度参数与校验 ──
        Check("ID: NewId 默认长度 8", IdGenerator.NewId().Length == 8);
        Check("ID: NewId 自定义长度 16", IdGenerator.NewId(16).Length == 16);
        var threwLen = false;
        try { IdGenerator.NewId(0); } catch (ArgumentOutOfRangeException) { threwLen = true; }
        Check("ID: NewId 长度 ≤0 抛异常", threwLen);

        // ── NewSlug 格式（形容词-动物-名词）──
        var slug = IdGenerator.NewSlug(3);
        var parts = slug.Split('-');
        Check("ID: NewSlug 3 段", parts.Length == 3);
        Check("ID: NewSlug 全小写字母", slug.All(c => char.IsLower(c) || c == '-'));

        // ── NewSlug 词数 clamp ──
        Check("ID: NewSlug 默认 3 词", IdGenerator.NewSlug().Split('-').Length == 3);
        Check("ID: NewSlug 1 词", IdGenerator.NewSlug(1).Split('-').Length == 1);
        Check("ID: NewSlug 超上限 clamp 到 5", IdGenerator.NewSlug(99).Split('-').Length == 5);
        Check("ID: NewSlug 0 clamp 到 1", IdGenerator.NewSlug(0).Split('-').Length == 1);

        // ── NewPrefixed ──
        var prefixed = IdGenerator.NewPrefixed("wf");
        Check("ID: NewPrefixed 前缀", prefixed.StartsWith("wf_"));
        Check("ID: NewPrefixed 长度 = 前缀+1+6", prefixed.Length == "wf".Length + 1 + 6);
    }

    /// <summary>文件忽略规则（FileIgnoreManager）单元测试：静态忽略 + glob 规则匹配 + 否定/锚定。</summary>
    private static void TestFileIgnoreManager(Action<string, bool> Check)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "wc_ignore_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(tmp);
        try
        {
            // ── 静态忽略（无规则文件，纯 AlwaysIgnore 逻辑）──
            Check("Ignore: node_modules 目录始终忽略",
                FileIgnoreManager.IsIgnored("node_modules/foo.cs", tmp));
            Check("Ignore: dist 目录始终忽略",
                FileIgnoreManager.IsIgnored("dist/app.js", tmp));
            Check("Ignore: .git 目录始终忽略",
                FileIgnoreManager.IsIgnored(".git/HEAD", tmp));
            Check("Ignore: .pyc 扩展名始终忽略",
                FileIgnoreManager.IsIgnored("foo.pyc", tmp));
            Check("Ignore: .dll 扩展名始终忽略",
                FileIgnoreManager.IsIgnored("lib/foo.dll", tmp));
            Check("Ignore: .jpg 扩展名始终忽略",
                FileIgnoreManager.IsIgnored("image.jpg", tmp));
            Check("Ignore: 正常源文件不忽略",
                !FileIgnoreManager.IsIgnored("src/main.cs", tmp));
            Check("Ignore: README 不忽略",
                !FileIgnoreManager.IsIgnored("README.md", tmp));

            // ── 写 .gitignore 规则，测试 glob 匹配 ──
            File.WriteAllText(Path.Combine(tmp, ".gitignore"),
                "# 测试规则\n*.log\nbuild/\nlogs/\n/rootfile.txt\n*.tmp\n!important.log\n");
            FileIgnoreManager.ClearCache();

            Check("Ignore: *.log 匹配任意深度",
                FileIgnoreManager.IsIgnored("app.log", tmp)
                && FileIgnoreManager.IsIgnored("src/deep/app.log", tmp));
            Check("Ignore: 否定规则 !important.log 反转忽略",
                !FileIgnoreManager.IsIgnored("important.log", tmp));
            Check("Ignore: build/ 目录规则匹配",
                FileIgnoreManager.IsIgnored("build/output.txt", tmp));
            Check("Ignore: logs/ 目录规则匹配内容（非 AlwaysIgnore 目录，暴露 // bug）",
                FileIgnoreManager.IsIgnored("logs/output.txt", tmp)
                && FileIgnoreManager.IsIgnored("logs/deep/file.txt", tmp));
            Check("Ignore: logs/ 目录规则不误伤无关文件",
                !FileIgnoreManager.IsIgnored("catalog.txt", tmp));
            Check("Ignore: 锚定 /rootfile.txt 仅匹配根目录",
                FileIgnoreManager.IsIgnored("rootfile.txt", tmp)
                && !FileIgnoreManager.IsIgnored("sub/rootfile.txt", tmp));
            Check("Ignore: *.tmp 扩展名规则匹配",
                FileIgnoreManager.IsIgnored("notes.txt.tmp", tmp));
            Check("Ignore: 未命中规则的文件不忽略",
                !FileIgnoreManager.IsIgnored("main.cs", tmp));

            // ── FilterIgnored 批量过滤 ──
            var kept = FileIgnoreManager.FilterIgnored(
                new[] { "a.cs", "node_modules/x.js", "b.log", "c.pyc", "d.jpg" }, tmp);
            Check("Ignore: FilterIgnored 仅保留非忽略项",
                kept.Count == 1 && kept[0] == "a.cs");

            // ── ShouldSkipDirectory 目录跳过 ──
            Check("Ignore: 跳过 .git 目录",
                FileIgnoreManager.ShouldSkipDirectory(".git", tmp));
            Check("Ignore: 跳过 node_modules 目录",
                FileIgnoreManager.ShouldSkipDirectory("node_modules", tmp));
            Check("Ignore: 跳过隐藏目录 .hidden",
                FileIgnoreManager.ShouldSkipDirectory(".hidden", tmp));
            Check("Ignore: 跳过 build 目录",
                FileIgnoreManager.ShouldSkipDirectory("build", tmp));
            Check("Ignore: 不跳过普通目录 src",
                !FileIgnoreManager.ShouldSkipDirectory("src", tmp));

            // ── ** globstar 零目录匹配 ──
            File.WriteAllText(Path.Combine(tmp, ".gitignore"),
                "/a/**/b\n/**/foo\n/c/**\n");
            FileIgnoreManager.ClearCache();

            Check("Ignore: a/**/b 匹配零目录 a/b",
                FileIgnoreManager.IsIgnored("a/b", tmp));
            Check("Ignore: a/**/b 匹配一级 a/x/b",
                FileIgnoreManager.IsIgnored("a/x/b", tmp));
            Check("Ignore: a/**/b 匹配多级 a/x/y/b",
                FileIgnoreManager.IsIgnored("a/x/y/b", tmp));
            Check("Ignore: a/**/b 不误伤 a/b/c",
                !FileIgnoreManager.IsIgnored("a/b/c", tmp));
            Check("Ignore: a/**/b 不误伤 a/c",
                !FileIgnoreManager.IsIgnored("a/c", tmp));
            Check("Ignore: /**/foo 匹配根 foo",
                FileIgnoreManager.IsIgnored("foo", tmp));
            Check("Ignore: /**/foo 匹配任意深度",
                FileIgnoreManager.IsIgnored("x/foo", tmp)
                && FileIgnoreManager.IsIgnored("x/y/foo", tmp));
            Check("Ignore: /c/** 匹配 c 下所有内容",
                FileIgnoreManager.IsIgnored("c/x.txt", tmp)
                && FileIgnoreManager.IsIgnored("c/d/e.txt", tmp));
        }
        finally
        {
            FileIgnoreManager.ClearCache();
            try { Directory.Delete(tmp, recursive: true); } catch { }
        }
    }

    /// <summary>查找替换工具（FindReplaceTool）单元测试：预览/替换、无效正则回退、错误分支。</summary>
    private static void TestFindReplaceTool(Action<string, bool> Check)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "wc_fr_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(tmp);
        var tool = new FindReplaceTool();
        try
        {
            var file = Path.Combine(tmp, "sample.cs");
            File.WriteAllText(file, "int foo = 1;\nvar foo2 = foo + 1;\n");

            // 空 pattern 报错
            var r0 = tool.ExecuteAsync(new Dictionary<string, object?> {
                ["path"] = tmp, ["pattern"] = "" }).GetAwaiter().GetResult();
            Check("FindReplace: 空 pattern 报错", r0.Contains("pattern 参数不能为空"));

            // 预览模式（不写文件）
            var r1 = tool.ExecuteAsync(new Dictionary<string, object?> {
                ["path"] = tmp, ["pattern"] = "foo", ["replacement"] = "bar", ["dry_run"] = true
            }).GetAwaiter().GetResult();
            Check("FindReplace: 预览输出匹配详情", r1.Contains("foo") && r1.Contains("预览"));
            Check("FindReplace: 预览不写文件", File.ReadAllText(file).Contains("foo"));

            // 实际替换
            var r2 = tool.ExecuteAsync(new Dictionary<string, object?> {
                ["path"] = tmp, ["pattern"] = "foo", ["replacement"] = "bar", ["dry_run"] = false
            }).GetAwaiter().GetResult();
            var replaced = File.ReadAllText(file);
            Check("FindReplace: 实际替换写入", !replaced.Contains("foo") && replaced.Contains("bar"));

            // 无效正则回退为纯文本匹配
            File.WriteAllText(Path.Combine(tmp, "arr.cs"), "var x = arr[0];\n");
            var r3 = tool.ExecuteAsync(new Dictionary<string, object?> {
                ["path"] = tmp, ["pattern"] = "[", ["dry_run"] = true
            }).GetAwaiter().GetResult();
            Check("FindReplace: 无效正则回退纯文本匹配", r3.Contains("arr"));

            // 目录不存在报错
            var r4 = tool.ExecuteAsync(new Dictionary<string, object?> {
                ["path"] = Path.Combine(tmp, "nope"), ["pattern"] = "foo"
            }).GetAwaiter().GetResult();
            Check("FindReplace: 目录不存在报错", r4.Contains("目录不存在"));
        }
        finally
        {
            try { Directory.Delete(tmp, recursive: true); } catch { }
        }
    }

    /// <summary>文件差异对比工具（DiffTool）单元测试：差异行/相同/空文件/不存在。</summary>
    private static void TestDiffTool(Action<string, bool> Check)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "wc_diff_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(tmp);
        var tool = new DiffTool();
        try
        {
            var f1 = Path.Combine(tmp, "a.txt");
            var f2 = Path.Combine(tmp, "b.txt");
            File.WriteAllText(f1, "line1\nline2\nline3\n");
            File.WriteAllText(f2, "line1\nCHANGED\nline3\n");

            // 差异输出
            var r = tool.ExecuteAsync(new Dictionary<string, object?> {
                ["file1"] = f1, ["file2"] = f2 }).GetAwaiter().GetResult();
            Check("Diff: 差异输出含删除行", r.Contains("- 2: line2"));
            Check("Diff: 差异输出含新增行", r.Contains("+ 2: CHANGED"));

            // 相同文件
            var r2 = tool.ExecuteAsync(new Dictionary<string, object?> {
                ["file1"] = f1, ["file2"] = f1 }).GetAwaiter().GetResult();
            Check("Diff: 相同文件提示", r2.Contains("内容相同"));

            // 空文件
            var empty = Path.Combine(tmp, "empty.txt");
            File.WriteAllText(empty, "");
            var r3 = tool.ExecuteAsync(new Dictionary<string, object?> {
                ["file1"] = empty, ["file2"] = empty }).GetAwaiter().GetResult();
            Check("Diff: 空文件提示", r3.Contains("均为空"));

            // 文件不存在
            var r4 = tool.ExecuteAsync(new Dictionary<string, object?> {
                ["file1"] = Path.Combine(tmp, "nope.txt"), ["file2"] = f2 }).GetAwaiter().GetResult();
            Check("Diff: 文件不存在错误", r4.Contains("文件不存在"));
        }
        finally
        {
            try { Directory.Delete(tmp, recursive: true); } catch { }
        }
    }

    /// <summary>目录树工具（TreeTool）单元测试：树生成/深度/隐藏跳过/错误分支。</summary>
    private static void TestTreeTool(Action<string, bool> Check)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "wc_tree_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(tmp);
        var tool = new TreeTool();
        try
        {
            var sub = Path.Combine(tmp, "sub");
            Directory.CreateDirectory(sub);
            File.WriteAllText(Path.Combine(tmp, "a.txt"), "x");
            File.WriteAllText(Path.Combine(sub, "b.cs"), "y");
            File.WriteAllText(Path.Combine(tmp, ".hidden"), "z");

            // 树生成
            var r = tool.ExecuteAsync(new Dictionary<string, object?> {
                ["path"] = tmp, ["depth"] = 3, ["max"] = 100 }).GetAwaiter().GetResult();
            Check("Tree: 输出含子目录", r.Contains("sub"));
            Check("Tree: 输出含文件", r.Contains("a.txt") && r.Contains("b.cs"));
            Check("Tree: 隐藏文件跳过", !r.Contains(".hidden"));

            // 目录不存在
            var r2 = tool.ExecuteAsync(new Dictionary<string, object?> {
                ["path"] = Path.Combine(tmp, "nope") }).GetAwaiter().GetResult();
            Check("Tree: 目录不存在错误", r2.Contains("目录不存在"));

            // 深度限制（depth=1 不递归子目录内容）
            var r3 = tool.ExecuteAsync(new Dictionary<string, object?> {
                ["path"] = tmp, ["depth"] = 1, ["max"] = 100 }).GetAwaiter().GetResult();
            Check("Tree: 深度限制不展开子目录", !r3.Contains("b.cs"));
        }
        finally
        {
            try { Directory.Delete(tmp, recursive: true); } catch { }
        }
    }

    /// <summary>代码片段管理（SnippetStore）单元测试：frontmatter 解析 + 增删查/多词搜索。</summary>
    private static void TestSnippetStore(Action<string, bool> Check)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "wc_snip_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(tmp);
        try
        {
            // 1. 预写 frontmatter 片段，测 Load 解析
            File.WriteAllText(Path.Combine(tmp, "parsed-snippet.md"),
                "---\nname: parsed-snippet\nlanguage: python\ntags: [ml, data]\n---\ndef predict():\n    return 1\n");
            SnippetStore.Load(tmp);
            Check("Snippet: frontmatter 解析 name/body",
                SnippetStore.Get("parsed-snippet", tmp)?.Contains("def predict") == true);

            // 2. Add → Get 往返
            SnippetStore.Add("hello-world", "Console.WriteLine(\"hi\");", "csharp",
                new List<string> { "utility" }, tmp);
            Check("Snippet: Add 后 Get 返回内容",
                SnippetStore.Get("hello-world", tmp)?.Contains("Console.WriteLine") == true);

            // 3. Search 多词 OR
            SnippetStore.Add("string-utils", "static string Trim() {}", "csharp",
                new List<string> { "string", "utility" }, tmp);
            Check("Snippet: Search 按名称命中",
                SnippetStore.Search("string", tmp).Any(s => s.Name == "string-utils"));
            Check("Snippet: Search 命中多个含 utility 标签",
                SnippetStore.Search("utility", tmp).Count >= 2);
            Check("Snippet: Search 无命中返回空",
                SnippetStore.Search("zzz_none", tmp).Count == 0);

            // 4. List
            Check("Snippet: List 含所有片段",
                SnippetStore.List(tmp).Any(s => s.Name == "hello-world"));

            // 5. Delete
            Check("Snippet: Delete 返回 true", SnippetStore.Delete("hello-world", tmp));
            Check("Snippet: Delete 后 Get 返回 null", SnippetStore.Get("hello-world", tmp) == null);
            Check("Snippet: Delete 不存在返回 false", !SnippetStore.Delete("nope", tmp));
        }
        finally
        {
            try { Directory.Delete(tmp, recursive: true); } catch { }
        }
    }

    /// <summary>文件锁管理器（FileLockManager）单元测试：获取/续期/拒绝/过期强占/释放/等待。</summary>
    private static void TestFileLockManager(Action<string, bool> Check)
    {
        var path = Path.Combine(Path.GetTempPath(), "wc_lock_" + Guid.NewGuid().ToString("N")[..6] + ".txt");
        var agentA = "agent-a";
        var agentB = "agent-b";

        // 清理残留
        FileLockManager.ReleaseAll(agentA);
        FileLockManager.ReleaseAll(agentB);

        // 1. 首次获取成功 + 锁信息
        Check("FileLock: 首次获取成功", FileLockManager.TryAcquire(path, agentA));
        Check("FileLock: 获取后有锁信息", FileLockManager.GetLockInfo(path) != null);
        Check("FileLock: 锁持有者正确", FileLockManager.GetLockInfo(path)?.AgentId == agentA);

        // 2. 同 agent 续期成功
        Check("FileLock: 同 agent 续期成功", FileLockManager.TryAcquire(path, agentA));

        // 3. 不同 agent 被拒 + IsLockedByOther
        Check("FileLock: 不同 agent 被拒", !FileLockManager.TryAcquire(path, agentB));
        Check("FileLock: IsLockedByOther 判定", FileLockManager.IsLockedByOther(path, agentB));
        Check("FileLock: 本人不视为其他", !FileLockManager.IsLockedByOther(path, agentA));

        // 4. Release 后其他 agent 可获取
        FileLockManager.Release(path, agentA);
        Check("FileLock: Release 后可被其他获取", FileLockManager.TryAcquire(path, agentB));
        FileLockManager.Release(path, agentB);

        // 5. 不同 agent 释放无效（锁归属不匹配）
        FileLockManager.TryAcquire(path, agentA);
        FileLockManager.Release(path, agentB);
        Check("FileLock: 不同 agent 释放无效", FileLockManager.GetLockInfo(path)?.AgentId == agentA);

        // 6. 过期锁被其他 agent 强制获取（timeout 为负 → 立即过期）
        FileLockManager.Release(path, agentA);
        FileLockManager.TryAcquire(path, agentA, TimeSpan.FromMilliseconds(-1));
        Check("FileLock: 过期锁被其他强占", FileLockManager.TryAcquire(path, agentB));
        FileLockManager.Release(path, agentB);

        // 7. ReleaseAll 释放指定 agent 全部锁
        FileLockManager.TryAcquire(path, agentA);
        FileLockManager.TryAcquire(path + ".2", agentA);
        FileLockManager.ReleaseAll(agentA);
        Check("FileLock: ReleaseAll 清空", FileLockManager.GetAllLocks().Count == 0);

        // 8. GetSummary 空/非空
        Check("FileLock: 无锁摘要为空", FileLockManager.GetSummary() == "");
        FileLockManager.TryAcquire(path, agentA);
        Check("FileLock: 有锁摘要非空", FileLockManager.GetSummary().Contains("文件锁定"));
        FileLockManager.ReleaseAll(agentA);

        // 9. WaitForLockAsync 无锁立即成功
        var ok = FileLockManager.WaitForLockAsync(path, agentA, TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
        Check("FileLock: WaitForLock 无锁成功", ok);

        // 10. Agent.AgentId 默认 + 写工具经 _agent_id 报跨槽位锁冲突（资源锁定报错提醒）
        var probeAgent = new Agent(new LLM("test", "sk-test"));
        Check("FileLock: Agent.AgentId 默认 main", probeAgent.AgentId == "main");

        var crossPath = Path.Combine(Path.GetTempPath(), "wc_cross_" + Guid.NewGuid().ToString("N")[..6] + ".txt");
        FileLockManager.ReleaseAll("F1");
        FileLockManager.ReleaseAll("F2");
        FileLockManager.TryAcquire(crossPath, "F1");
        var crossResult = new WayCoder.Tools.WriteFileTool().ExecuteAsync(new Dictionary<string, object?> {
            ["file_path"] = crossPath,
            ["content"] = "x",
            ["_agent_id"] = "F2",
        }).GetAwaiter().GetResult();
        Check("FileLock: 跨槽位写报锁冲突(F1)", crossResult.Contains("F1") && crossResult.Contains("锁定"));
        FileLockManager.ReleaseAll("F1");
        FileLockManager.ReleaseAll("F2");
        try { File.Delete(crossPath); } catch { }

        // 清理
        FileLockManager.ReleaseAll(agentA);
        FileLockManager.ReleaseAll(agentB);
    }

    /// <summary>跨平台运行器选择（CrossPlatform）单元测试：shell/python 可执行文件与参数标志。</summary>
    private static void TestCrossPlatform(Action<string, bool> Check)
    {
        Check("XPlat: IsWindows 与系统一致", CrossPlatform.IsWindows == OperatingSystem.IsWindows());
        Check("XPlat: ShellExecutable 合法", CrossPlatform.ShellExecutable is "cmd.exe" or "/bin/bash");
        Check("XPlat: PythonExecutable 合法", CrossPlatform.PythonExecutable is "python" or "python3");
        Check("XPlat: ShellArgs 用对标志",
            CrossPlatform.ShellArgs("echo hi").StartsWith(CrossPlatform.IsWindows ? "/c" : "-c"));
        // Unix 分支需对内层引号转义（bash -c 语义），Windows 分支无需转义
        if (!CrossPlatform.IsWindows)
            Check("XPlat: Unix ShellArgs 转义内层引号", CrossPlatform.ShellArgs("echo \"hi\"").Contains("\\\""));
    }

    /// <summary>文件追踪器（FileTracker）单元测试：stale-read 检测 + 先读后改保护 + 删除/禁用。</summary>
    private static void TestFileTracker(Action<string, bool> Check)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "wc_track_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(tmp);
        var f = Path.Combine(tmp, "a.txt");
        var f2 = Path.Combine(tmp, "b.txt");

        FileTracker.Enabled = true;
        FileTracker.Reset();

        try
        {
            // 1. 初始未追踪
            File.WriteAllText(f, "v1");
            Check("FileTrack: 未追踪返回 (false,false)", FileTracker.GetStatus(f) == (false, false));

            // 2. RecordRead 记录哈希
            FileTracker.RecordRead(f);
            Check("FileTrack: RecordRead 后 (true,false)", FileTracker.GetStatus(f) == (true, false));

            // 3. 外部修改检测（哈希变更）
            File.WriteAllText(f, "v2");
            Check("FileTrack: 外部修改后 (true,true)", FileTracker.GetStatus(f) == (true, true));
            var changes = FileTracker.CheckForChanges();
            Check("FileTrack: CheckForChanges 检出", changes.Any(p => Path.GetFileName(p) == "a.txt"));

            // 4. RecordWrite 更新哈希 → 不再 stale
            FileTracker.RecordWrite(f);
            Check("FileTrack: RecordWrite 后 (true,false)", FileTracker.GetStatus(f) == (true, false));

            // 5. 删除检测 + 移除追踪
            File.Delete(f);
            var deleted = FileTracker.CheckForChanges();
            Check("FileTrack: 删除检出", deleted.Any(p => Path.GetFileName(p) == "a.txt"));
            Check("FileTrack: 删除后不再追踪", FileTracker.GetStatus(f) == (false, false));

            // 6. ValidatePreEdit 未读取警告
            File.WriteAllText(f2, "x");
            Check("FileTrack: 未读取编辑警告", FileTracker.ValidatePreEdit(f2)?.Contains("尚未被 read_file") == true);

            // 7. ValidatePreEdit 已读取未修改 → 通过（null）
            FileTracker.RecordRead(f2);
            Check("FileTrack: 已读取未修改通过", FileTracker.ValidatePreEdit(f2) == null);

            // 8. GetChangeWarning 检出外部修改
            File.WriteAllText(f2, "y");
            Check("FileTrack: 变更警告非空", FileTracker.GetChangeWarning()?.Contains("文件变更警告") == true);

            // 9. Enabled=false 短路
            FileTracker.Enabled = false;
            Check("FileTrack: 禁用后 GetStatus 短路", FileTracker.GetStatus(f2) == (false, false));
            FileTracker.Enabled = true;

            // 10. Reset 清空
            FileTracker.Reset();
            Check("FileTrack: Reset 后未追踪", FileTracker.GetStatus(f2) == (false, false));
        }
        finally
        {
            FileTracker.Enabled = true;
            FileTracker.Reset();
            try { Directory.Delete(tmp, recursive: true); } catch { }
        }
    }

    /// <summary>Hook 系统（HooksManager）单元测试：session hook 注册/事件执行/匹配器/输出协议解析。</summary>
    private static void TestHooksManager(Action<string, bool> Check)
    {
        HooksManager.Enabled = true;
        HooksManager.ClearSessionHooks();

        // 1. PreToolUse session hook 阻止（返回 reason）
        var id1 = HooksManager.RegisterSessionHook(HookEvent.PreToolUse,
            ctx => Task.FromResult<HookOutput?>(new HookOutput { Decision = "block", Reason = "测试阻止" }));
        var block = HooksManager.RunPreToolUseAsync("bash", new Dictionary<string, object?> { ["cmd"] = "ls" }).GetAwaiter().GetResult();
        Check("Hook: PreToolUse 阻止返回原因", block?.Contains("测试阻止") == true);

        // 2. 注销后放行
        HooksManager.UnregisterSessionHook(id1);
        Check("Hook: 注销后放行",
            HooksManager.RunPreToolUseAsync("bash", new Dictionary<string, object?>()).GetAwaiter().GetResult() == null);

        // 3. Continue=true 放行
        HooksManager.RegisterSessionHook(HookEvent.PreToolUse,
            ctx => Task.FromResult<HookOutput?>(new HookOutput { Continue = true }));
        Check("Hook: Continue=true 放行",
            HooksManager.RunPreToolUseAsync("bash", new Dictionary<string, object?>()).GetAwaiter().GetResult() == null);
        HooksManager.ClearSessionHooks();

        // 4. PostToolUse 返回 AdditionalContext
        HooksManager.RegisterSessionHook(HookEvent.PostToolUse,
            ctx => Task.FromResult<HookOutput?>(new HookOutput { AdditionalContext = "后处理结果" }));
        Check("Hook: PostToolUse 返回附加上下文",
            HooksManager.RunPostToolUseAsync("bash", new Dictionary<string, object?>(), "ok").GetAwaiter().GetResult() == "后处理结果");
        HooksManager.ClearSessionHooks();

        // 5. Stop 返回 AdditionalContext
        HooksManager.RegisterSessionHook(HookEvent.Stop,
            ctx => Task.FromResult<HookOutput?>(new HookOutput { AdditionalContext = "stop-ctx" }));
        Check("Hook: Stop 返回上下文", HooksManager.RunStopAsync().GetAwaiter().GetResult() == "stop-ctx");
        HooksManager.ClearSessionHooks();

        // 6. 事件隔离：PreToolUse hook 不触发 Stop
        HooksManager.RegisterSessionHook(HookEvent.PreToolUse,
            ctx => Task.FromResult<HookOutput?>(new HookOutput { AdditionalContext = "wrong-event" }));
        Check("Hook: 事件隔离", HooksManager.RunStopAsync().GetAwaiter().GetResult() == null);
        HooksManager.ClearSessionHooks();

        // 7. MatchesPattern（空/* 全匹配、管道、正则、无效正则回退）
        Check("Hook: 空 matcher 全匹配", HooksManager.MatchesPattern("bash", null));
        Check("Hook: * 全匹配", HooksManager.MatchesPattern("bash", "*"));
        Check("Hook: 管道命中", HooksManager.MatchesPattern("bash", "bash|git|rm"));
        Check("Hook: 管道未命中", !HooksManager.MatchesPattern("ls", "bash|git|rm"));
        Check("Hook: 正则命中", HooksManager.MatchesPattern("WriteFile", "^Write"));
        Check("Hook: 正则未命中", !HooksManager.MatchesPattern("ReadFile", "^Write"));
        Check("Hook: 无效正则回退精确命中", HooksManager.MatchesPattern("(", "("));
        Check("Hook: 无效正则回退精确未命中", !HooksManager.MatchesPattern("(", "x"));

        // 8. ParseHookOutput（JSON 协议 / exitCode 2 / 纯文本回退 / 空输出）
        Check("Hook: JSON 解析 Decision",
            HooksManager.ParseHookOutput("{\"Decision\":\"block\",\"Reason\":\"r\"}", 0)?.Decision == "block");
        Check("Hook: exitCode 2 → block", HooksManager.ParseHookOutput("阻止文本", 2)?.Decision == "block");
        Check("Hook: exitCode 2 + JSON 无 decision 仍 block",
            HooksManager.ParseHookOutput("{\"Reason\":\"策略阻止\"}", 2)?.Decision == "block");
        Check("Hook: exitCode 2 + JSON 显式 approve 不强制覆盖",
            HooksManager.ParseHookOutput("{\"Decision\":\"approve\"}", 2)?.Decision == "approve");
        Check("Hook: 纯文本回退", HooksManager.ParseHookOutput("纯文本输出", 0)?.AdditionalContext == "纯文本输出");
        Check("Hook: 空输出 → null", HooksManager.ParseHookOutput("", 0) == null);

        // 9. SnakeCase（PascalCase → snake_case）
        Check("Hook: SnakeCase 常规", HooksManager.SnakeCase("PreToolUse") == "pre_tool_use");
        Check("Hook: SnakeCase 单词", HooksManager.SnakeCase("Stop") == "stop");

        HooksManager.ClearSessionHooks();
    }

    // 手搓 JSON 库（AOT 安全零反射）：解析/DOM/序列化/转义/错误分支

    private static void TestJsonLib(Action<string, bool> Check)
    {
        // 1. 标量解析
        Check("Json: 整数解析", Json.Parse("123")?.AsNumber() == 123);
        Check("Json: 负数解析", Json.Parse("-42.5")?.AsNumber() == -42.5);
        Check("Json: 指数解析", Json.Parse("1e3")?.AsNumber() == 1000);
        Check("Json: true 解析", Json.Parse("true")?.AsBool() == true);
        Check("Json: false 解析", Json.Parse("false")?.AsBool() == false);
        Check("Json: null 解析", Json.Parse("null")?.IsNull == true);
        Check("Json: 字符串解析", Json.Parse("\"hello\"")?.AsString() == "hello");

        // 2. 对象解析
        var obj = Json.Parse("{\"a\":1,\"b\":\"x\",\"c\":true}");
        Check("Json: 对象字段数", obj?.Count == 3);
        Check("Json: 对象取数字", obj?.GetNumber("a") == 1);
        Check("Json: 对象取字符串", obj?.GetString("b") == "x");
        Check("Json: 对象取布尔", obj?.GetBool("c") == true);
        Check("Json: 对象 Has", obj?.Has("a") == true && obj?.Has("z") == false);

        // 3. 数组解析
        var arr = Json.Parse("[1,2,3]");
        Check("Json: 数组长度", arr?.Count == 3);
        Check("Json: 数组下标", arr?.At(1)?.AsNumber() == 2);
        Check("Json: 数组 Items", arr?.Items.Count() == 3);

        // 4. 嵌套
        var nested = Json.Parse("{\"a\":{\"b\":[10,20]}}");
        Check("Json: 嵌套取值", nested?.Get("a")?.Get("b")?.At(1)?.AsNumber() == 20);

        // 5. 转义
        Check("Json: 转义序列", Json.Parse("\"a\\nb\\t\\\"\\\\\"")?.AsString() == "a\nb\t\"\\");
        Check("Json: \\u 中文", Json.Parse("\"\\u4e2d\\u6587\"")?.AsString() == "中文");
        Check("Json: 代理对 emoji", Json.Parse("\"\\ud83d\\ude00\"")?.AsString() == "\U0001F600");

        // 6. 非法输入
        Check("Json: 非法 JSON 拒绝", !Json.TryParse("{bad}", out _));
        Check("Json: 尾随逗号拒绝", !Json.TryParse("[1,]", out _));
        Check("Json: 未闭合对象拒绝", !Json.TryParse("{\"a\":1", out _));
        Check("Json: 空字符串返回 null", Json.Parse("") == null && !Json.TryParse("", out _));

        // 6b. 截断 JSON 健壮性 —— 逐位置截断合法输入，验证解析器不崩溃/不挂起、异常受控
        {
            var truncationSources = new[]
            {
                "{\"a\":1,\"b\":\"hello\",\"c\":[1,2,3],\"d\":{\"e\":true,\"f\":null}}",
                "[1,2,3,4,5]",
                "{\"nested\":{\"deep\":{\"deeper\":[{\"x\":1},{\"y\":2}]}}}",
                "\"a string with escapes \\n\\t\\\" and unicode \\u4e2d\"",
                "{\"unicode\":\"\\ud83d\\ude00\",\"num\":-12.5e3}",
            };

            int tryParseThrew = 0;   // TryParse 抛了异常（不应发生）
            int parseWrongExc = 0;   // Parse 抛了 JsonParseException 之外的异常（不应发生）
            int parseSuccessAtPrefix = 0; // 截断前缀恰好仍是合法 JSON（允许，非错误）

            foreach (var full in truncationSources)
            {
                for (int len = 0; len <= full.Length; len++)
                {
                    var truncated = full[..len];

                    // TryParse 对任何输入（含截断）永不抛异常，只返回 true/false
                    try { Json.TryParse(truncated, out _); }
                    catch { tryParseThrew++; }

                    // Parse 要么成功，要么抛 JsonParseException（受控），绝不抛其它异常类型
                    try { Json.Parse(truncated); parseSuccessAtPrefix++; }
                    catch (JsonParseException) { }
                    catch { parseWrongExc++; }
                }
            }

            Check("Json: 截断输入 TryParse 永不抛异常", tryParseThrew == 0);
            Check("Json: 截断输入 Parse 仅抛 JsonParseException", parseWrongExc == 0);

            // 明确断言的典型截断/畸形样例（TryParse 拒绝、Parse 抛受控异常）
            string[] malformed =
            {
                "{\"a\":",          // 值被截断
                "{\"a\":1,",        // 逗号后缺内容
                "[1,2",             // 数组未闭合
                "[1,2,",            // 逗号后缺内容
                "\"abc",            // 字符串未闭合
                "\"abc\\",          // 转义被截断
                "\"\\u12",          // unicode 转义不完整
                "\"\\ud83d",        // 高代理后缺低代理
                "1.",               // 小数点后缺数字
                "1e",               // 指数后缺数字
                "-",                // 负号后缺数字
                "tru",              // true 被截断
                "fals",             // false 被截断
                "nul",              // null 被截断
                "{\"a\":1}x",       // 根值后多余内容
            };
            int malformedAccepted = 0;
            foreach (var m in malformed)
                if (Json.TryParse(m, out _)) malformedAccepted++;
            Check("Json: 典型畸形输入全部拒绝", malformedAccepted == 0);
        }

        // 7. 序列化往返（数字保真）
        Check("Json: 往返对象", Json.Serialize(Json.Parse("{\"a\":1}")!) == "{\"a\":1}");
        Check("Json: 往返数组", Json.Serialize(Json.Parse("[1,\"x\",true,null]")!) == "[1,\"x\",true,null]");
        Check("Json: 缩进含换行", Json.Serialize(Json.Parse("{\"a\":1}")!, true).Contains('\n'));

        // 8. 序列化转义
        Check("Json: 序列化转义", Json.Serialize(JNode.Str("a\"b\nc")) == "\"a\\\"b\\nc\"");

        // 9. DOM 操作
        var dom = JNode.Object().Set("a", 1).Set("b", "x").Set("a", 2);
        Check("Json: DOM Set 覆盖", dom.Count == 2 && dom.GetNumber("a") == 2);
        var domArr = JNode.Array().Add(1).Add("y");
        Check("Json: DOM Add", domArr.Count == 2 && domArr.At(1)?.AsString() == "y");

        // 9b. JNode.From 类型分派（替代 JsonValue.Create）
        Check("Json: From null", JNode.From(null).IsNull);
        Check("Json: From string", JNode.From("abc").AsString() == "abc");
        Check("Json: From bool", JNode.From(true).AsBool());
        Check("Json: From int", JNode.From(7).AsNumber() == 7);
        Check("Json: From double", JNode.From(3.5).AsNumber() == 3.5);
        Check("Json: From JNode 恒等", ReferenceEquals(JNode.From(domArr), domArr));
        Check("Json: From 序列化", Json.Serialize(JNode.Object().Set("x", JNode.From(1))) == "{\"x\":1}");

        // 10. SerializeValue（无反射）
        Check("Json: SerializeValue null", Json.SerializeValue(null) == "null");
        Check("Json: SerializeValue string", Json.SerializeValue("x") == "\"x\"");
        Check("Json: SerializeValue int", Json.SerializeValue(42) == "42");
        Check("Json: SerializeValue bool", Json.SerializeValue(true) == "true");
        Check("Json: SerializeValue list", Json.SerializeValue(new List<int> { 1, 2 }) == "[1,2]");
        Check("Json: SerializeValue dict", Json.SerializeValue(new Dictionary<string, object?> { ["k"] = 1 }) == "{\"k\":1}");

        // 11. Clone 深拷贝
        var src = Json.Parse("{\"a\":[1,2]}");
        var cp = src?.Clone();
        Check("Json: Clone 深拷贝", cp != null && Json.Serialize(cp) == Json.Serialize(src!));

        // 12. SlotConfig 手搓往返（零反射，替代 JsonSerializer.Deserialize<SlotConfig>）
        var slot = new AgentSlotConfig.SlotConfig
        {
            LargeModel = "deepseek-v4-pro",
            SmallModel = "deepseek-v4-flash",
            BaseUrl = "https://api.deepseek.com",
            ApiKeyProviderId = "deepseek",
            ApiKey = null,
            UseGlobal = false,
        };
        var slotNode = AgentSlotConfig.SlotToNode(slot);
        Check("Json: Slot 键名 PascalCase", slotNode.Has("LargeModel") && !slotNode.Has("largeModel"));
        var slotBack = AgentSlotConfig.SlotFromNode(slotNode);
        Check("Json: Slot 往返 LargeModel", slotBack.LargeModel == "deepseek-v4-pro");
        Check("Json: Slot 往返 SmallModel", slotBack.SmallModel == "deepseek-v4-flash");
        Check("Json: Slot 往返 BaseUrl", slotBack.BaseUrl == "https://api.deepseek.com");
        Check("Json: Slot 往返 ProviderId", slotBack.ApiKeyProviderId == "deepseek");
        Check("Json: Slot 往返 UseGlobal", slotBack.UseGlobal == false);
        Check("Json: Slot null 字段往返", slotBack.ApiKey == null);

        // 序列化往返（模拟保存/加载的 JSON 文本，经手搓 Json 库）
        var slotParsed = AgentSlotConfig.SlotFromNode(Json.Parse(Json.Serialize(slotNode))!);
        Check("Json: Slot 序列化往返", slotParsed.LargeModel == "deepseek-v4-pro" && slotParsed.UseGlobal == false);

        // UseGlobal 缺省 → true（与 JsonSerializer 属性初始化语义一致）
        var defaultSlot = AgentSlotConfig.SlotFromNode(JNode.Object());
        Check("Json: Slot UseGlobal 缺省为 true", defaultSlot.UseGlobal == true);

        // 13. 嵌套缩进美化（FetchTool.PrettyPrintJson 依赖 Json.Serialize(indent)）
        Check("Json: 嵌套缩进美化", Json.Serialize(Json.Parse("{\"a\":[1]}")!, true).Contains("\n  \"a\""));
    }

    // 手搓 XML 库（AOT 安全零反射）：解析/DOM/实体/CDATA/序列化/错误分支

    private static void TestXmlLib(Action<string, bool> Check)
    {
        // 1. 基础元素
        var root = Xml.Parse("<root/>");
        Check("Xml: 空元素", root?.Name == "root" && root?.Children.Count() == 0);
        Check("Xml: 文本内容", Xml.Parse("<a>x</a>")?.InnerText() == "x");

        // 2. 属性
        var attr = Xml.Parse("<a id=\"1\" name='x'/>");
        Check("Xml: 属性双引号", attr?.GetAttr("id") == "1");
        Check("Xml: 属性单引号", attr?.GetAttr("name") == "x");
        Check("Xml: HasAttr", attr?.HasAttr("id") == true && attr?.HasAttr("z") == false);

        // 3. 嵌套
        var nest = Xml.Parse("<a><b>1</b><c>2</c></a>");
        Check("Xml: Find 子元素", nest?.Find("b")?.InnerText() == "1");
        Check("Xml: FindAll 计数", nest?.FindAll("c").Count() == 1);
        Check("Xml: InnerText 递归拼接", nest?.InnerText() == "12");

        // 4. 实体
        Check("Xml: 预定义实体", Xml.Parse("<a>&lt;tag&gt; &amp; &quot; &apos;</a>")?.InnerText() == "<tag> & \" '");
        Check("Xml: 数字字符引用", Xml.Parse("<a>&#65;&#x42;</a>")?.InnerText() == "AB");

        // 5. CDATA
        Check("Xml: CDATA 保留原样", Xml.Parse("<a><![CDATA[<b>raw</b>]]></a>")?.InnerText() == "<b>raw</b>");

        // 6. 声明/注释/DOCTYPE 跳过
        Check("Xml: 声明跳过", Xml.Parse("<?xml version=\"1.0\"?><root/>")?.Name == "root");
        Check("Xml: 注释跳过", Xml.Parse("<!-- c --><root/>")?.Name == "root");
        Check("Xml: DOCTYPE 跳过", Xml.Parse("<!DOCTYPE root><root/>")?.Name == "root");

        // 7. 序列化
        Check("Xml: 空元素序列化", Xml.Serialize(XNode.Element("a")) == "<a/>");
        Check("Xml: 文本转义序列化", Xml.Serialize(XNode.Element("a").AddText("<&>")) == "<a>&lt;&amp;&gt;</a>");
        Check("Xml: 属性转义序列化", Xml.Serialize(XNode.Element("a").Attr("v", "\"q\"")) == "<a v=\"&quot;q&quot;\"/>");
        Check("Xml: 缩进含换行", Xml.Serialize(XNode.Element("a").Add(XNode.Element("b")), true).Contains('\n'));

        // 8. 解析序列化往返
        var xml = "<a id=\"1\"><b>x &amp; y</b><c/></a>";
        Check("Xml: 往返", Xml.Serialize(Xml.Parse(xml)!) == xml);

        // 9. 非法输入
        Check("Xml: 未闭合拒绝", !Xml.TryParse("<a>", out _));
        Check("Xml: 标签不匹配拒绝", !Xml.TryParse("<a></b>", out _));
        Check("Xml: 空返回 null", Xml.Parse("") == null && !Xml.TryParse("", out _));

        // 10. DOM 操作
        var dom = XNode.Element("root").Attr("k", "v").Add(XNode.Element("child"));
        Check("Xml: DOM Attr", dom.GetAttr("k") == "v");
        Check("Xml: DOM Add", dom.Find("child") != null);
    }

}