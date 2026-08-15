using System.Text;
using System.Text.Json;
using WayCoder.Tools;
using WayCoder.UI;
using WayCoder.Terminal;
using WayCoder.UI.TuiControls;
using WayCoder.UI.TuiScreens;

namespace WayCoder;

public static partial class SelfTest
{
    private static void TestChunk2(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[Git]");
        var gitTool = new GitTool();
        var gitResult = gitTool.ExecuteAsync(new() { ["command"] = "--version" }).Result;
        Check("git --version 可执行", gitResult.Contains("git version"));
        Check("git push --force 被阻止",
            gitTool.ExecuteAsync(new() { ["command"] = "push --force origin main" }).Result.Contains("已阻止"));
        Console.WriteLine();

        // ---- fetch ----
        Section("[Fetch]");
        var fetchTool = new FetchTool();
        Check("fetch 拒绝非 http URL",
            fetchTool.ExecuteAsync(new() { ["url"] = "ftp://evil.com" }).Result.Contains("错误"));
        Check("fetch 拒绝非法 HTTP 方法",
            fetchTool.ExecuteAsync(new() { ["url"] = "https://example.com", ["method"] = "TRACE" }).Result.Contains("不支持的 HTTP 方法"));
        Check("fetch method 大小写规范化",
            fetchTool.ExecuteAsync(new() { ["url"] = "https://example.com", ["method"] = "trace" }).Result.Contains("不支持的 HTTP 方法"));
        // headers 解析（离线可测，不发起网络）
        var h = FetchTool.ParseHeaders("{\"Authorization\":\"Bearer abc\",\"Content-Type\":\"application/json\"}");
        Check("fetch headers 解析", h != null && h.Count == 2 && h["authorization"] == "Bearer abc");
        Check("fetch headers 空/非法 JSON 返回 null",
            FetchTool.ParseHeaders(null) == null && FetchTool.ParseHeaders("not-json") == null && FetchTool.ParseHeaders("  ") == null);
        Console.WriteLine();

        // ---- sqlite ----
        Section("[Sqlite]");
        var sqliteTool = new SqliteTool();
        try
        {
            var sqliteResult = sqliteTool.ExecuteAsync(new() { ["query"] = "SELECT 1 AS x" }).Result;
            if (sqliteResult.Contains("未找到"))
                Check("sqlite 未安装友好提示", sqliteResult.Contains("sqlite3"));
            else
                Check("sqlite SELECT 查询", sqliteResult.Contains("1") && !sqliteResult.Contains("错误"));
        }
        catch { Fail("sqlite 查询不崩溃"); }
        Check("sqlite 空查询报错",
            sqliteTool.ExecuteAsync(new() { ["query"] = "" }).Result.Contains("错误"));
        Console.WriteLine();

        // ---- test ----
        Section("[Test]");
        var testTool = new TestTool();
        // 解析逻辑（纯离线，不依赖真实测试框架）
        var testPass = TestTool.BuildSummary(0, "100 passed, 0 failed in 5.2s");
        Check("test 解析通过", testPass.Contains("通过") && testPass.Contains("100"));
        var testFail = TestTool.BuildSummary(1, "FAILED tests/test_x.py::test_bar\n2 passed, 1 failed in 3s");
        Check("test 解析失败", testFail.Contains("失败") && testFail.Contains("test_x"));
        var testCounts = TestTool.ExtractCounts("10 passed, 3 failed");
        Check("test 提取 pytest 统计", testCounts.Passed == 10 && testCounts.Failed == 3);
        var testDotnet = TestTool.ExtractCounts("Passed!  - Failed: 0, Passed: 100, Skipped: 2");
        Check("test 提取 dotnet 统计", testDotnet.Passed == 100 && testDotnet.Failed == 0);
        var testNoStats = TestTool.ExtractCounts("no recognizable output here");
        Check("test 无统计返回 -1", testNoStats.Passed == -1 && testNoStats.Failed == -1);
        var testFailures = TestTool.ExtractFailures("FAILED test_a\nError: boom\n2 passed, 1 failed\nFAILED test_b");
        Check("test 提取失败用例（排除摘要）",
            testFailures.Count == 3 && testFailures.Contains("FAILED test_a") && !testFailures.Contains("2 passed, 1 failed"));
        Check("test 空命令报错",
            testTool.ExecuteAsync(new() { ["command"] = "" }).Result.Contains("错误"));
        Console.WriteLine();

        // ---- todo ----
        Section("[Todo]");
        var todoTool = new TodoTool();
        // 先清空，确保干净状态
        todoTool.ExecuteAsync(new() { ["action"] = "clear" }).Wait();
        var createResult = todoTool.ExecuteAsync(new() { ["action"] = "create", ["id"] = "test-1", ["title"] = "测试任务" }).Result;
        Check("todo create", createResult.Contains("创建") && TodoTool.Items.Count == 1);
        var updateResult = todoTool.ExecuteAsync(new() { ["action"] = "update", ["id"] = "test-1", ["status"] = "completed" }).Result;
        Check("todo update", updateResult.Contains("completed") && TodoTool.Items[0].Status == "completed");
        var listResult = todoTool.ExecuteAsync(new() { ["action"] = "list" }).Result;
        Check("todo list", listResult.Contains("✅") && listResult.Contains("测试任务"));
        todoTool.ExecuteAsync(new() { ["action"] = "clear" }).Wait();
        Check("todo clear", TodoTool.Items.Count == 0);
        Console.WriteLine();

        // ---- 权限系统 ----
        Section("[权限]");
        PermissionManager.SetMode("yolo");
        Check("权限 YOLO 模式", PermissionManager.CurrentMode == PermissionManager.Mode.Yolo);
        var permCheck = PermissionManager.CheckAsync("bash", new() { ["command"] = "echo test" }).Result;
        Check("YOLO 模式自动放行", permCheck == true);
        PermissionManager.SetMode("ask");
        Console.WriteLine();

        // ---- 记忆系统（临时目录隔离，避免污染真实 memory.md）----
        Section("[记忆]");
        var savedMemCwd = Directory.GetCurrentDirectory();
        var memTestDir = Path.Combine(Path.GetTempPath(), "waycoder_memtest_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(memTestDir);
        try
        {
            Directory.SetCurrentDirectory(memTestDir);

            var e1 = StructuredMemory.Create("dotnet-aot", "项目使用 .NET 10 AOT 编译", "project", "C# AOT 单文件编译");
            Check("结构化创建", e1.Name == "dotnet-aot");
            Check("结构化读取", StructuredMemory.Get("dotnet-aot")?.Type == "project");
            StructuredMemory.Create("user-pref", "用户偏好中文界面", "user", "终端青色主题");
            Check("结构化搜索", StructuredMemory.Search("中文").Count == 1);
            Check("索引文件存在", File.Exists(StructuredMemory.IndexPath));
            var upd = StructuredMemory.Update("dotnet-aot", content: "C# AOT 单文件 ~8MB");
            Check("结构化更新", upd != null && upd.Content.Contains("8MB"));
            Check("结构化删除", StructuredMemory.Delete("user-pref"));
            Check("结构化计数", StructuredMemory.Count == 1);

            // 共享记忆
            StructuredMemory.SetShared("dotnet-aot", true);
            var sharedEntry = StructuredMemory.Get("dotnet-aot");
            Check("SetShared 标记为共享", sharedEntry?.IsShared == true);
            Check("ListShared 返回共享记忆", StructuredMemory.ListShared().Count == 1);
            StructuredMemory.SetShared("dotnet-aot", false);
            Check("Unshare 取消共享", StructuredMemory.Get("dotnet-aot")?.IsShared == false);
            Check("取消后 ListShared 为空", StructuredMemory.ListShared().Count == 0);
            Check("IsGitRepo 可调用", SharedMemoryManager.IsGitRepo() || !SharedMemoryManager.IsGitRepo());
            SharedMemoryManager.ResetCache();
        }
        finally
        {
            Directory.SetCurrentDirectory(savedMemCwd);
            try { Directory.Delete(memTestDir, true); } catch { }
        }
        Console.WriteLine();

        // ---- 后台任务 ----
        Section("[后台任务]");
        var bgId = BackgroundTaskManager.Start("echo bg_test", 5);
        Check("后台任务启动", bgId > 0);
        System.Threading.Thread.Sleep(1500); // 等任务完成
        var bgOutput = BackgroundTaskManager.GetOutput(bgId);
        Check("后台任务输出", bgOutput.Contains("bg_test"));
        var bgList = BackgroundTaskManager.ListTasks();
        Check("后台任务列表", bgList.Contains("completed") || bgList.Contains("running"));
        BackgroundTaskManager.Cleanup();
        Console.WriteLine();

        // ---- LSP 工具 ----
        Section("[LSP]");
        ITool lspTool = new LspTool();
        Check("lsp 工具名称正确", lspTool.Name == "lsp");
        Check("lsp 有 definition/references/hover/symbols", lspTool.Description.Contains("定义"));
        Check("lsp 支持 14 种语言", LspTool.SupportedServers.Count == 14);
        Check("lsp 含 cpp(clangd)", LspTool.SupportedServers.ContainsKey("cpp") && LspTool.SupportedServers["cpp"].Command == "clangd");
        Check("lsp 含 java(jdtls)", LspTool.SupportedServers.ContainsKey("java") && LspTool.SupportedServers["java"].Command == "jdtls");
        Check("lsp 含 kotlin", LspTool.SupportedServers.ContainsKey("kotlin") && LspTool.SupportedServers["kotlin"].Command == "kotlin-language-server");
        Check("lsp 含 ruby(solargraph)", LspTool.SupportedServers.ContainsKey("ruby") && LspTool.SupportedServers["ruby"].Command == "solargraph");
        Check("lsp 含 php(intelephense)", LspTool.SupportedServers.ContainsKey("php") && LspTool.SupportedServers["php"].Command == "intelephense");
        Check("lsp 含 lua", LspTool.SupportedServers.ContainsKey("lua") && LspTool.SupportedServers["lua"].Command == "lua-language-server");
        Check("lsp 含 bash", LspTool.SupportedServers.ContainsKey("bash") && LspTool.SupportedServers["bash"].Command == "bash-language-server");
        Check("lsp 含 swift", LspTool.SupportedServers.ContainsKey("swift") && LspTool.SupportedServers["swift"].Command == "sourcekit-lsp");
        Check("lsp 含 zig", LspTool.SupportedServers.ContainsKey("zig") && LspTool.SupportedServers["zig"].Command == "zls");
        // 会话缓存复用（离线可测部分：项目根查找 + 会话清理）
        var lspRoot = LspTool.FindProjectRoot(Path.Combine(AppContext.BaseDirectory, "WayCoder.dll"));
        Check("lsp 向上查找项目根", !string.IsNullOrEmpty(lspRoot) && lspRoot.EndsWith("WayCoder"));
        Check("lsp 无标记路径不崩溃",
            !string.IsNullOrEmpty(LspTool.FindProjectRoot(Path.Combine(Path.GetTempPath(), "no_marker_dir", "x.py"))));
        LspTool.ShutdownAllSessions(); // 空态清理不崩溃
        Check("lsp 会话清理不崩溃", true);
        Console.WriteLine();

        // ---- 流式工具执行 (编译期已验证 onToolCall 参数) ----
        // ChatAsync 方法签名已通过 LLM.cs 编译验证，此处确认 LLM 实例可创建
        Section("[LLM 流式]");
        try
        {
            var llmTest = new LLM("test", "sk-test");
            Check("LLM onToolCall 支持 (编译期)", true);
        }
        catch { Fail("LLM onToolCall 支持 (编译期)"); }
        Console.WriteLine();

        // ---- LLM 定价 ----
        Section("[LLM]");
        var llm = new LLM("deepseek-v4-flash", "sk-test");
        // 用反射注入 token 数
        typeof(LLM).GetProperty("TotalPromptTokens")?.SetValue(llm, 1_000_000);
        typeof(LLM).GetProperty("TotalCompletionTokens")?.SetValue(llm, 500_000);
        Check("deepseek-v4-flash 成本 ≈ 0.28", Math.Abs(llm.EstimatedCost!.Value - 0.28) < 0.01);

        var llm2 = new LLM("unknown-model", "sk-test");
        Check("未知模型成本为 null", llm2.EstimatedCost == null);

        // 任务级花费追踪
        typeof(LLM).GetProperty("TotalPromptTokens")?.SetValue(llm, 500_000);
        typeof(LLM).GetProperty("TotalCompletionTokens")?.SetValue(llm, 250_000);
        llm.SnapshotTaskCost();
        // 模拟任务产生了 200K 输入 + 100K 输出
        typeof(LLM).GetProperty("TotalPromptTokens")?.SetValue(llm, 700_000);
        typeof(LLM).GetProperty("TotalCompletionTokens")?.SetValue(llm, 350_000);
        Check("任务 Token 增量 = 200K+100K", llm.TaskPromptTokens == 200_000 && llm.TaskCompletionTokens == 100_000);
        Check("任务花费 ≈ $0.056", llm.TaskCost.HasValue && Math.Abs(llm.TaskCost!.Value - 0.056) < 0.01);
        // 未知模型 TaskCost 为 null
        typeof(LLM).GetProperty("TotalPromptTokens")?.SetValue(llm2, 100_000);
        typeof(LLM).GetProperty("TotalCompletionTokens")?.SetValue(llm2, 50_000);
        llm2.SnapshotTaskCost();
        typeof(LLM).GetProperty("TotalPromptTokens")?.SetValue(llm2, 200_000);
        typeof(LLM).GetProperty("TotalCompletionTokens")?.SetValue(llm2, 100_000);
        Check("未知模型任务花费为 null", llm2.TaskCost == null);
        Check("未知模型任务 Token 增量正确", llm2.TaskPromptTokens == 100_000 && llm2.TaskCompletionTokens == 50_000);

        // ---- LLM 重试/超时配置 ----
        Check("默认重试次数 = 5", Config.Instance.LlmMaxRetries == 5);
        // 全新实例 = 代码默认值，不受 .env 覆盖影响（.env 可合法地把超时调到 600 等）
        var defaultConfig = new Config();
        Check("默认 HTTP 超时 = 300", defaultConfig.LlmHttpTimeoutSec == 300);
        Check("默认连接超时 = 300", defaultConfig.LlmConnectionTimeoutSec == 300);

        // ---- LLM 渐进超时倍率 ----
        Check("TimeoutMultipliers[0] = 1.0", Math.Abs(LLM.TimeoutMultipliers[0] - 1.0) < 0.01);
        Check("TimeoutMultipliers[3] = 3.0", Math.Abs(LLM.TimeoutMultipliers[3] - 3.0) < 0.01);
        Check("TimeoutMultipliers[6] = 8.0", Math.Abs(LLM.TimeoutMultipliers[6] - 8.0) < 0.01);
        Check("GetTimeoutMultiplier(0) = 1.0", Math.Abs(LLM.GetTimeoutMultiplier(0) - 1.0) < 0.01);
        Check("GetTimeoutMultiplier(2) = 2.0", Math.Abs(LLM.GetTimeoutMultiplier(2) - 2.0) < 0.01);
        Check("GetTimeoutMultiplier(7) = 9.0", Math.Abs(LLM.GetTimeoutMultiplier(7) - 9.0) < 0.01);
        Check("GetTimeoutMultiplier(10) = 12.0", Math.Abs(LLM.GetTimeoutMultiplier(10) - 12.0) < 0.01);
        // 第 1 次尝试 (attempt=0): 600*1.0=600s, 第 5 次 (attempt=4): 600*4.0=2400s
        var t1 = 600 * LLM.GetTimeoutMultiplier(0);
        var t5 = 600 * LLM.GetTimeoutMultiplier(4);
        Check($"第1次超时={t1:F0}s", Math.Abs(t1 - 600) < 1);
        Check($"第5次超时={t5:F0}s", Math.Abs(t5 - 2400) < 1);
        Console.WriteLine();

        // ---- 系统提示词 ----
        Section("[系统提示词]");
        var prompt = SystemPrompt.Generate(ToolRegistry.AllTools);
        Check("包含 read_file", prompt.Contains("read_file"));
        Check("包含 edit_file", prompt.Contains("edit_file"));
        Check("包含当前目录", prompt.Contains(Directory.GetCurrentDirectory()));
        // 新结构化区块检查
        Check("包含 critical_rules", prompt.Contains("<critical_rules>"));
        Check("包含 workflow", prompt.Contains("<workflow>"));
        Check("包含 editing_files", prompt.Contains("<editing_files>"));
        Check("包含 exact_matching", prompt.Contains("<exact_matching>"));
        Check("包含 task_completion", prompt.Contains("<task_completion>"));
        Check("包含 error_handling", prompt.Contains("<error_handling>"));
        Check("包含 testing", prompt.Contains("<testing>"));
        Check("包含 code_conventions", prompt.Contains("<code_conventions>"));
        Check("包含 15 条规则", prompt.Contains("15."));
        // 新增 systematic_phases 章节
        Check("包含 systematic_phases", prompt.Contains("<systematic_phases>"));
        Check("包含调查阶段", prompt.Contains("调查"));
        Check("包含分析阶段", prompt.Contains("分析"));
        Check("包含规划阶段", prompt.Contains("规划"));
        Check("包含拆分阶段", prompt.Contains("拆分"));
        Check("包含分工阶段", prompt.Contains("分工"));
        Check("包含执行阶段", prompt.Contains("执行"));
        Check("包含调试阶段", prompt.Contains("调试"));
        Check("包含审核阶段", prompt.Contains("审核"));
        Check("包含提交阶段", prompt.Contains("提交"));
        Check("包含总结阶段", prompt.Contains("总结"));
        Check("包含流水线说明", prompt.Contains("内部流水线"));
        Console.WriteLine();
        Section("[Agent]");
        var agent = new Agent(new LLM("test", "sk-test"));
        agent.Messages.Add(JNode.Object().Set("role", "user").Set("content", "x"));
        agent.Reset();
        Check("Reset 清空消息", agent.Messages.Count == 0);

        var readTool = ToolRegistry.GetTool("read_file")!;
        var agent2 = new Agent(new LLM("test", "sk-test"), [readTool!]);
        Check("工具范围隔离", agent2.ToolByName.Count == 1 && agent2.ToolByName.ContainsKey("read_file"));

        // 优雅暂停标志（Ctrl+Z / /pause）
        var pauseAgent = new Agent(new LLM("test", "sk-test"));
        Check("PauseRequested 默认 false", pauseAgent.PauseRequested == false);
        pauseAgent.PauseRequested = true;
        Check("PauseRequested 可置位", pauseAgent.PauseRequested == true);
        pauseAgent.PauseRequested = false;
        Check("PauseRequested 可复位", pauseAgent.PauseRequested == false);
        Console.WriteLine();

        // ---- JsonHelper ----
        Section("[JSON 辅助]");
        var json = JsonHelper.SerializeArgs(new() { ["k"] = "v", ["n"] = 42 });
        Check("序列化包含键值", json.Contains("\"k\":\"v\"") && json.Contains("\"n\":42"));

        // P1-3 回归：集合/字典/JsonNode 递归序列化（而非 ToString 回显 System.Collections...）
        var listJson = JsonHelper.SerializeArgs(new() { ["tasks"] = new List<object?> { "a", "b", 3 } });
        Check("List 序列化为 JSON 数组", listJson == "{\"tasks\":[\"a\",\"b\",3]}");
        Check("List 不含 ToString 泄漏", !listJson.Contains("System.Collections"));

        var dictJson = JsonHelper.SerializeArgs(new() { ["opts"] = new Dictionary<string, object?> { ["x"] = 1, ["y"] = "z" } });
        Check("字典序列化为 JSON 对象", dictJson == "{\"opts\":{\"x\":1,\"y\":\"z\"}}");
        Check("字典不含 ToString 泄漏", !dictJson.Contains("System.Collections"));

        var nodeJson = JsonHelper.SerializeArgs(new()
        {
            ["arr"] = JNode.Array().Add("m").Add("n"),
            ["obj"] = JNode.Object().Set("deep", true),
        });
        Check("JsonArray/JsonObject 走 ToJsonString", nodeJson.Contains("\"arr\":[\"m\",\"n\"]") && nodeJson.Contains("\"deep\":true"));
        Console.WriteLine();

        // ================================================================
        //  以下为 v0.6.0+ 新增功能的测试
        // ================================================================

        // ---- 权限系统 扩展 ----
        Section("[权限系统 扩展]");
        // Ask 模式: 危险工具需要确认（非交互环境会失败，测边界逻辑）
        PermissionManager.SetMode("ask");
        PermissionManager.Reset();
        Check("SetMode ask → CurrentMode == Ask",
            PermissionManager.CurrentMode == PermissionManager.Mode.Ask);
        PermissionManager.SetMode("auto");
        Check("SetMode auto → CurrentMode == Auto",
            PermissionManager.CurrentMode == PermissionManager.Mode.Auto);
        PermissionManager.SetMode("yolo");
        Check("SetMode yolo → CurrentMode == Yolo",
            PermissionManager.CurrentMode == PermissionManager.Mode.Yolo);
        PermissionManager.SetMode("god");
        Check("SetMode god → CurrentMode == Yolo",
            PermissionManager.CurrentMode == PermissionManager.Mode.Yolo);
        PermissionManager.SetMode("smart");
        Check("SetMode smart → CurrentMode == SmartAuto",
            PermissionManager.CurrentMode == PermissionManager.Mode.SmartAuto);
        PermissionManager.SetMode("smartauto");
        Check("SetMode smartauto → CurrentMode == SmartAuto",
            PermissionManager.CurrentMode == PermissionManager.Mode.SmartAuto);
        PermissionManager.SetMode("auto");
        Check("SetMode auto → CurrentMode == Auto",
            PermissionManager.CurrentMode == PermissionManager.Mode.Auto);
        PermissionManager.SetMode("unknown");
        Check("SetMode unknown → CurrentMode == Ask",
            PermissionManager.CurrentMode == PermissionManager.Mode.Ask);

        // 非危险工具直接放行
        var safeCheck = PermissionManager.CheckAsync("read_file", new() { ["file_path"] = "/tmp/x" }).Result;
        Check("read_file (非危险) 直接放行", safeCheck == true);
        var safeCheck2 = PermissionManager.CheckAsync("glob", new() { ["pattern"] = "*.cs" }).Result;
        Check("glob (非危险) 直接放行", safeCheck2 == true);
        var safeCheck3 = PermissionManager.CheckAsync("grep", new() { ["pattern"] = "x" }).Result;
        Check("grep (非危险) 直接放行", safeCheck3 == true);

        // YOLO 模式：危险工具也放行
        PermissionManager.SetMode("yolo");
        var yoloDanger = PermissionManager.CheckAsync("bash", new() { ["command"] = "echo hi" }).Result;
        Check("YOLO 模式 bash 放行", yoloDanger == true);

        // Reset 清空 Auto 记录
        PermissionManager.SetMode("ask");
        PermissionManager.Reset();
        Check("Reset 后 Ask 模式恢复", PermissionManager.CurrentMode == PermissionManager.Mode.Ask);

        // ---- AutoMode 智能分类器 ----
        Section("[AutoMode 智能分类器]");

        // 风险分级
        Check("read_file → Safe", AutoModeClassifier.Classify("read_file") == AutoModeClassifier.RiskLevel.Safe);
        Check("ls → Safe", AutoModeClassifier.Classify("ls") == AutoModeClassifier.RiskLevel.Safe);
        Check("grep → Safe", AutoModeClassifier.Classify("grep") == AutoModeClassifier.RiskLevel.Safe);
        Check("glob → Safe", AutoModeClassifier.Classify("glob") == AutoModeClassifier.RiskLevel.Safe);
        Check("stat → Safe", AutoModeClassifier.Classify("stat") == AutoModeClassifier.RiskLevel.Safe);
        Check("diff → Safe", AutoModeClassifier.Classify("diff") == AutoModeClassifier.RiskLevel.Safe);
        Check("tree → Safe", AutoModeClassifier.Classify("tree") == AutoModeClassifier.RiskLevel.Safe);
        Check("fetch → Safe", AutoModeClassifier.Classify("fetch") == AutoModeClassifier.RiskLevel.Safe);
        Check("lsp → Safe", AutoModeClassifier.Classify("lsp") == AutoModeClassifier.RiskLevel.Safe);

        Check("write_file → Cautious", AutoModeClassifier.Classify("write_file") == AutoModeClassifier.RiskLevel.Cautious);
        Check("edit_file → Cautious", AutoModeClassifier.Classify("edit_file") == AutoModeClassifier.RiskLevel.Cautious);
        Check("mkdir → Cautious", AutoModeClassifier.Classify("mkdir") == AutoModeClassifier.RiskLevel.Cautious);
        Check("cp → Cautious", AutoModeClassifier.Classify("cp") == AutoModeClassifier.RiskLevel.Cautious);
        Check("mv → Cautious", AutoModeClassifier.Classify("mv") == AutoModeClassifier.RiskLevel.Cautious);

        Check("rm → Dangerous", AutoModeClassifier.Classify("rm") == AutoModeClassifier.RiskLevel.Dangerous);
        Check("bash → Dangerous", AutoModeClassifier.Classify("bash") == AutoModeClassifier.RiskLevel.Dangerous);
        Check("git → Dangerous", AutoModeClassifier.Classify("git") == AutoModeClassifier.RiskLevel.Dangerous);
        Check("kill → Dangerous", AutoModeClassifier.Classify("kill") == AutoModeClassifier.RiskLevel.Dangerous);
        Check("agent → Dangerous", AutoModeClassifier.Classify("agent") == AutoModeClassifier.RiskLevel.Dangerous);

        // 未知工具默认 Dangerous
        Check("unknown_tool → Dangerous", AutoModeClassifier.Classify("unknown_tool") == AutoModeClassifier.RiskLevel.Dangerous);

        // 连续阻止计数
        AutoModeClassifier.Reset();
        Check("初始连续阻止=0", AutoModeClassifier.ConsecutiveDangerousBlocks == 0);

        AutoModeClassifier.RecordDangerousBlock();
        Check("阻止1次=1", AutoModeClassifier.ConsecutiveDangerousBlocks == 1);
        AutoModeClassifier.RecordDangerousBlock();
        Check("阻止2次=2", AutoModeClassifier.ConsecutiveDangerousBlocks == 2);

        // 允许后重置
        AutoModeClassifier.RecordDangerousAllow();
        Check("允许→计数归零", AutoModeClassifier.ConsecutiveDangerousBlocks == 0);

        // 阈值触发
        bool fallbackTriggered = false;
        AutoModeClassifier.FallbackToManualTriggered += () => { fallbackTriggered = true; };
        AutoModeClassifier.RecordDangerousBlock(); // 1
        AutoModeClassifier.RecordDangerousBlock(); // 2
        AutoModeClassifier.RecordDangerousBlock(); // 3 → 触发
        Check("连续3次阻止→触发退回事件", fallbackTriggered);
        Check("触发后计数归零", AutoModeClassifier.ConsecutiveDangerousBlocks == 0);

        // SmartAuto 模式下 Safe 工具放行
        PermissionManager.SetMode("smartauto");
        var smartSafe = PermissionManager.CheckAsync("read_file", new() { ["file_path"] = "/tmp/x" }).Result;
        Check("SmartAuto: read_file 自动放行", smartSafe == true);
        var smartSafe2 = PermissionManager.CheckAsync("ls", new() { ["path"] = "." }).Result;
        Check("SmartAuto: ls 自动放行", smartSafe2 == true);

        // 恢复默认
        PermissionManager.SetMode("ask");
        AutoModeClassifier.Reset();

        // ---- 工作模式管理器 ----
        Section("[工作模式管理器]");

        // 默认模式
        Check("默认模式=Build", WorkModeManager.CurrentMode == WorkMode.Build);

        // 格式输出
        Check("Format(Build) 含🔨", WorkModeManager.Format(WorkMode.Build).Contains("🔨"));
        Check("Format(Plan) 含🧠", WorkModeManager.Format(WorkMode.Plan).Contains("🧠"));
        Check("Format(Review) 含🔍", WorkModeManager.Format(WorkMode.Review).Contains("🔍"));
        Check("Format(Auto) 含🤖", WorkModeManager.Format(WorkMode.Auto).Contains("🤖"));

        // 工具约束：Plan 模式
        Check("Plan: write_file 阻止", WorkModeManager.CheckToolAllowed("write_file", WorkMode.Plan) != null);
        Check("Plan: edit_file 阻止", WorkModeManager.CheckToolAllowed("edit_file", WorkMode.Plan) != null);
        Check("Plan: bash 阻止", WorkModeManager.CheckToolAllowed("bash", WorkMode.Plan) != null);
        Check("Plan: rm 阻止", WorkModeManager.CheckToolAllowed("rm", WorkMode.Plan) != null);
        Check("Plan: git 阻止", WorkModeManager.CheckToolAllowed("git", WorkMode.Plan) != null);
        Check("Plan: agent 阻止", WorkModeManager.CheckToolAllowed("agent", WorkMode.Plan) != null);
        Check("Plan: read_file 允许", WorkModeManager.CheckToolAllowed("read_file", WorkMode.Plan) == null);
        Check("Plan: grep 允许", WorkModeManager.CheckToolAllowed("grep", WorkMode.Plan) == null);
        Check("Plan: lsp 允许", WorkModeManager.CheckToolAllowed("lsp", WorkMode.Plan) == null);

        // 工具约束：Review 模式
        Check("Review: write_file 阻止", WorkModeManager.CheckToolAllowed("write_file", WorkMode.Review) != null);
        Check("Review: bash 阻止", WorkModeManager.CheckToolAllowed("bash", WorkMode.Review) != null);
        Check("Review: agent 允许", WorkModeManager.CheckToolAllowed("agent", WorkMode.Review) == null);
        Check("Review: read_file 允许", WorkModeManager.CheckToolAllowed("read_file", WorkMode.Review) == null);

        // 工具约束：Build 模式全允许
        Check("Build: write_file 允许", WorkModeManager.CheckToolAllowed("write_file", WorkMode.Build) == null);
        Check("Build: bash 允许", WorkModeManager.CheckToolAllowed("bash", WorkMode.Build) == null);
        Check("Build: rm 允许", WorkModeManager.CheckToolAllowed("rm", WorkMode.Build) == null);

        // 工具约束：Auto 模式全允许
        Check("Auto: bash 允许", WorkModeManager.CheckToolAllowed("bash", WorkMode.Auto) == null);
        Check("Auto: write_file 允许", WorkModeManager.CheckToolAllowed("write_file", WorkMode.Auto) == null);

        // 模式切换
        WorkModeManager.SetMode(WorkMode.Plan);
        Check("SetMode→Plan", WorkModeManager.CurrentMode == WorkMode.Plan);
        WorkModeManager.SetMode(WorkMode.Review);
        Check("SetMode→Review", WorkModeManager.CurrentMode == WorkMode.Review);

        // 循环切换
        WorkModeManager.SetMode(WorkMode.Build);
        var m1 = WorkModeManager.CycleNext();
        Check("CycleNext: Build→Plan", m1 == WorkMode.Plan);
        var m2 = WorkModeManager.CycleNext();
        Check("CycleNext: Plan→Review", m2 == WorkMode.Review);
        var m3 = WorkModeManager.CycleNext();
        Check("CycleNext: Review→Auto", m3 == WorkMode.Auto);
        var m4 = WorkModeManager.CycleNext();
        Check("CycleNext: Auto→Build", m4 == WorkMode.Build);

        // ModeChanged 事件
        bool eventFired = false;
        WorkMode received = WorkMode.Build;
        Action<WorkMode> handler = m => { eventFired = true; received = m; };
        WorkModeManager.ModeChanged += handler;
        WorkModeManager.SetMode(WorkMode.Plan);
        Check("ModeChanged 事件触发", eventFired && received == WorkMode.Plan);
        // 清理：移除 handler（AOT 不支持 -= lambda）
        WorkModeManager.SetMode(WorkMode.Build);

        // System Prompt 生成
        var planPrompt = WorkModeManager.GetModePrompt(WorkMode.Plan);
        Check("Plan Prompt 含计划模式", planPrompt.Contains("计划模式"));
        var reviewPrompt = WorkModeManager.GetModePrompt(WorkMode.Review);
        Check("Review Prompt 含审查模式", reviewPrompt.Contains("审查模式"));
        var buildPrompt = WorkModeManager.GetModePrompt(WorkMode.Build);
        Check("Build Prompt 为空", string.IsNullOrEmpty(buildPrompt));

        // 计划审批门（Plan 模式产出计划后弹出审批）—— 纯逻辑判定
        Check("审批门: Plan+计划文本 触发", Agent.ShouldPromptPlanApproval(WorkMode.Plan, 200));
        Check("审批门: Plan+空文本 不触发", !Agent.ShouldPromptPlanApproval(WorkMode.Plan, 0));
        Check("审批门: Build+计划文本 不触发", !Agent.ShouldPromptPlanApproval(WorkMode.Build, 200));
        Check("审批门: Review+计划文本 不触发", !Agent.ShouldPromptPlanApproval(WorkMode.Review, 200));
        Check("审批门: Auto+计划文本 不触发", !Agent.ShouldPromptPlanApproval(WorkMode.Auto, 200));

        // 恢复默认
        WorkModeManager.SetMode(WorkMode.Build);

        // ---- 跨槽位消息传递 ----
        Section("[跨槽位消息]");

        // AgentSlot 初始化
        var testSlot = new AgentSlot();
        Check("新槽位 PendingMessages 为空", testSlot.PendingMessages.Count == 0);

        // 投递消息到非活跃槽位（无 activeScreen）→ 排队
        testSlot.DeliverMessage(0, "测试消息", null, 1);
        Check("Deliver 后 PendingMessages=1", testSlot.PendingMessages.Count == 1);
        Check("Pending 内容匹配", testSlot.PendingMessages[0].Message == "测试消息");
        Check("Pending 来源槽位", testSlot.PendingMessages[0].FromSlot == 0);

        // 投递多条消息
        testSlot.DeliverMessage(2, "第二条消息", null, 1);
        Check("Deliver 后 PendingMessages=2", testSlot.PendingMessages.Count == 2);

        // 刷新队列（没有真实 ChatScreen 时直接清空）
        testSlot.PendingMessages.Clear();
        Check("Clear 后 PendingMessages=0", testSlot.PendingMessages.Count == 0);

        // AgentSlot.Count 常量
        Check("AgentSlot.Count=10", AgentSlot.Count == 10);

        Console.WriteLine();

        // ---- 会话管理 ----
        Section("[会话管理]");

        static List<JNode> MakeMsgs()
        {
            return [
                JNode.Object().Set("role", "user").Set("content", "你好"),
                JNode.Object().Set("role", "assistant").Set("content", "你好！有什么可以帮你的？"),
            ];
        }

        // 保存会话
        var sid = SessionManager.SaveSession(MakeMsgs(), "deepseek-v4-flash", "test-session-001");
        Check("保存会话返回 ID", sid == "test-session-001");

        // 加载会话
        var loaded = SessionManager.LoadSession("test-session-001");
        Check("加载会话成功", loaded != null);
        Check("加载消息数正确", loaded?.Messages.Count == 2);
        Check("加载模型正确", loaded?.Model == "deepseek-v4-flash");

        // 列出会话
        var list = SessionManager.ListSessions();
        Check("会话列表包含测试会话", list.Any(s => s.Id == "test-session-001"));

        // 会话 ID 净化 —— 路径穿越
        var safeId1 = SessionManager.SaveSession(MakeMsgs(), "gpt-5.5", "../../../etc/passwd");
        Check("路径穿越 ID 被净化", safeId1 != "../../../etc/passwd" && !safeId1.Contains(".."));

        // 会话 ID 净化 —— 特殊字符
        var safeId2 = SessionManager.SaveSession(MakeMsgs(), "gpt-5.5", "hello world!@#$");
        Check("特殊字符 ID 被净化", !safeId2.Contains(" ") && !safeId2.Contains("!") && !safeId2.Contains("@") && !safeId2.Contains("#"));

        // 空 sessionId 自动生成
        var autoId = SessionManager.SaveSession(MakeMsgs(), "gpt-5.5", null);
        Check("空 sessionId 自动生成", autoId.StartsWith("session_") && autoId.Length > 10);

        // 加载不存在的会话
        var notFound = SessionManager.LoadSession("nonexistent-session-xyz");
        Check("加载不存在会话返回 null", notFound == null);

        // 清理测试文件
        try
        {
            var sessionsDir = Global.GlobalConfigPath("sessions");
            foreach (var f in Directory.GetFiles(sessionsDir, "test-session-*"))
                File.Delete(f);
        }
        catch { }

        Console.WriteLine();

        // ---- Agent 工作区（F1-F10 槽位） ----
    }
}
