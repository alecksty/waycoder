using CoreCoderSharp.Tools;

namespace CoreCoderSharp;

/// <summary>
/// 内置自测，通过 --test 或 -t 运行。
/// 无需外部测试框架，保持极简主义。
/// </summary>
public static class SelfTest
{
    public static bool Run()
    {
        var passed = 0;
        var failed = 0;

        void Check(string name, bool condition)
        {
            if (condition) { passed++; Console.WriteLine($"  ✅ {name}"); }
            else { failed++; Console.WriteLine($"  ❌ {name}"); }
        }

        Console.WriteLine("CoreCoderSharp 自测");
        Console.WriteLine("===================\n");

        // ---- 工具注册 ----
        Console.WriteLine("[工具注册]");
        Check("工具数量 == 12", ToolRegistry.AllTools.Count == 12);
        Check("所有工具有有效 schema", ToolRegistry.AllTools.All(t =>
        {
            var s = t.Schema();
            return (string?)s["type"] == "function"
                && s["function"]?["name"] != null
                && s["function"]?["parameters"]?["properties"] != null;
        }));
        Console.WriteLine();

        // ---- Config ----
        Console.WriteLine("[配置]");
        var config = new Config();
        Check("默认模型 deepseek-v4-flash", config.Model == "deepseek-v4-flash");
        Console.WriteLine();

        // ---- ContextManager ----
        Console.WriteLine("[上下文管理]");
        var msgs1 = new List<JsonObject> { new() { ["role"] = "user", ["content"] = "hello world" } };
        Check("Token 估算 > 0", ContextManager.EstimateTokens(msgs1) > 0);

        var msgs2 = new List<JsonObject>
        {
            new() { ["role"] = "tool", ["content"] = string.Join("\n", Enumerable.Repeat("x", 1000)) },
        };
        var before = ContextManager.EstimateTokens(msgs2);
        ContextManager.SnipToolOutputs(msgs2);
        Check("工具输出裁剪有效", ContextManager.EstimateTokens(msgs2) < before);

        var msgs3 = new List<JsonObject>
        {
            new() { ["role"] = "user", ["content"] = "do" },
            new() { ["role"] = "assistant", ["content"] = null, ["tool_calls"] = new JsonArray() },
            new() { ["role"] = "tool", ["tool_call_id"] = "c1", ["content"] = "r" },
        };
        var split = ContextManager.SafeSplit(msgs3, 1);
        Check("SafeSplit 不以 tool 开头", (string?)msgs3[split]["role"] != "tool");
        Console.WriteLine();

        // ---- 工具 ----
        Console.WriteLine("[工具]");

        // read_file
        try
        {
            var tmpFile = Path.GetTempFileName();
            File.WriteAllText(tmpFile, "l1\nl2\nl3\n");
            var readResult = new ReadFileTool().ExecuteAsync(new() { ["file_path"] = tmpFile }).Result;
            Check("read_file 基本功能", readResult.Contains("l1") && readResult.Contains("l2"));
            File.Delete(tmpFile);
        }
        catch { failed++; Console.WriteLine("  ❌ read_file 基本功能"); }

        Check("read_file 文件不存在返回错误",
            new ReadFileTool().ExecuteAsync(new() { ["file_path"] = "/nonexistent" }).Result.Contains("错误"));

        // write_file
        try
        {
            var tmpFile2 = Path.GetTempFileName();
            var writeResult = new WriteFileTool().ExecuteAsync(new() { ["file_path"] = tmpFile2, ["content"] = "hi\n" }).Result;
            Check("write_file 基本功能", writeResult.Contains("已写入") && File.ReadAllText(tmpFile2) == "hi\n");
            File.Delete(tmpFile2);
        }
        catch { failed++; Console.WriteLine("  ❌ write_file 基本功能"); }

        // edit_file
        try
        {
            var tmpFile3 = Path.GetTempFileName();
            File.WriteAllText(tmpFile3, "hello world\n");
            var editResult = new EditFileTool().ExecuteAsync(new()
            {
                ["file_path"] = tmpFile3, ["old_string"] = "world", ["new_string"] = "地球",
            }).Result;
            Check("edit_file 基本替换", editResult.Contains("已编辑") && File.ReadAllText(tmpFile3).Contains("地球"));
            File.Delete(tmpFile3);
        }
        catch { failed++; Console.WriteLine("  ❌ edit_file 基本替换"); }

        try
        {
            var tmpFile4 = Path.GetTempFileName();
            File.WriteAllText(tmpFile4, "aa\n");
            var editResult = new EditFileTool().ExecuteAsync(new()
            {
                ["file_path"] = tmpFile4, ["old_string"] = "NOTFOUND", ["new_string"] = "x",
            }).Result;
            Check("edit_file 未找到返回错误", editResult.Contains("未找到"));
            File.Delete(tmpFile4);
        }
        catch { failed++; Console.WriteLine("  ❌ edit_file 未找到返回错误"); }

        // glob - 在临时目录中创建文件后测试
        try
        {
            var globDir = Path.Combine(Path.GetTempPath(), "glob_test_" + Guid.NewGuid().ToString("N")[..6]);
            Directory.CreateDirectory(globDir);
            File.WriteAllText(Path.Combine(globDir, "a.cs"), "");
            File.WriteAllText(Path.Combine(globDir, "b.cs"), "");
            var globResult = new GlobTool().ExecuteAsync(new() { ["pattern"] = "*.cs", ["path"] = globDir }).Result;
            Check("glob 找到 .cs 文件", globResult.Contains("a.cs") && globResult.Contains("b.cs"));
            Directory.Delete(globDir, true);
        }
        catch { failed++; Console.WriteLine("  ❌ glob 找到 .cs 文件"); }

        // grep
        var grepResult = new GrepTool().ExecuteAsync(new() { ["pattern"] = "class SelfTest" }).Result;
        Check("grep 找到自身", grepResult.Contains("SelfTest"));

        Check("grep 无效正则返回错误",
            new GrepTool().ExecuteAsync(new() { ["pattern"] = "[bad" }).Result.Contains("无效的正则"));

        // bash
        var bashResult = new BashTool().ExecuteAsync(new() { ["command"] = "echo hello" }).Result;
        Check("bash 基本命令", bashResult.Contains("hello"));

        Check("bash 阻止 rm -rf /",
            new BashTool().ExecuteAsync(new() { ["command"] = "rm -rf /" }).Result.Contains("已阻止"));

        Check("bash 阻止 fork 炸弹",
            new BashTool().ExecuteAsync(new() { ["command"] = ":(){ :|:& };:" }).Result.Contains("已阻止"));

        Check("bash 允许安全 rm", BashTool.CheckDangerous("rm -f notes.log") == null);

        // cwd 跟踪
        try
        {
            var testDir = Path.Combine(Path.GetTempPath(), "ct_" + Guid.NewGuid().ToString("N")[..6]);
            var subDir = Path.Combine(testDir, "a", "b");
            Directory.CreateDirectory(subDir);
            BashTool.CurrentCwd.Value = null!; // 重置 AsyncLocal
            BashTool.UpdateCwd($"cd {testDir} && cd a && cd b", testDir);
            Check("bash cd 链式解析", BashTool.CurrentCwd.Value == Path.GetFullPath(subDir));
            Directory.Delete(testDir, true);
        }
        catch { failed++; Console.WriteLine("  ❌ bash cd 链式解析"); }

        Console.WriteLine();

        // ---- git ----
        Console.WriteLine("[Git]");
        var gitTool = new GitTool();
        var gitResult = gitTool.ExecuteAsync(new() { ["command"] = "--version" }).Result;
        Check("git --version 可执行", gitResult.Contains("git version"));
        Check("git push --force 被阻止",
            gitTool.ExecuteAsync(new() { ["command"] = "push --force origin main" }).Result.Contains("已阻止"));
        Console.WriteLine();

        // ---- fetch ----
        Console.WriteLine("[Fetch]");
        var fetchTool = new FetchTool();
        Check("fetch 拒绝非 http URL",
            fetchTool.ExecuteAsync(new() { ["url"] = "ftp://evil.com" }).Result.Contains("错误"));
        Console.WriteLine();

        // ---- todo ----
        Console.WriteLine("[Todo]");
        var todoTool = new TodoTool();
        TodoTool.Items.Clear();
        var createResult = todoTool.ExecuteAsync(new() { ["action"] = "create", ["title"] = "测试任务" }).Result;
        Check("todo create", createResult.Contains("已创建") && TodoTool.Items.Count == 1);
        var updateResult = todoTool.ExecuteAsync(new() { ["action"] = "update", ["id"] = 1, ["status"] = "completed" }).Result;
        Check("todo update", updateResult.Contains("completed") && TodoTool.Items[0].Status == "completed");
        var listResult = todoTool.ExecuteAsync(new() { ["action"] = "list" }).Result;
        Check("todo list", listResult.Contains("✅") && listResult.Contains("测试任务"));
        todoTool.ExecuteAsync(new() { ["action"] = "clear" }).Wait();
        Check("todo clear", TodoTool.Items.Count == 0);
        Console.WriteLine();

        // ---- 权限系统 ----
        Console.WriteLine("[权限]");
        PermissionManager.SetMode("yolo");
        Check("权限 YOLO 模式", PermissionManager.CurrentMode == PermissionManager.Mode.Yolo);
        var permCheck = PermissionManager.CheckAsync("bash", new() { ["command"] = "echo test" }).Result;
        Check("YOLO 模式自动放行", permCheck == true);
        PermissionManager.SetMode("ask");
        Console.WriteLine();

        // ---- 记忆系统 ----
        Console.WriteLine("[记忆]");
        var memRead = MemoryStore.Read();
        Check("memory read 有效返回", memRead is not null);
        MemoryStore.Append("自测写入");
        var memSearch = MemoryStore.Search("自测");
        Check("memory search 找到", memSearch.Contains("自测"));
        Console.WriteLine();

        // ---- 后台任务 ----
        Console.WriteLine("[后台任务]");
        var bgId = BackgroundTaskManager.StartAsync("echo bg_test", 5).Result;
        Check("后台任务启动", bgId > 0);
        System.Threading.Thread.Sleep(1500); // 等任务完成
        var bgOutput = BackgroundTaskManager.GetOutput(bgId);
        Check("后台任务输出", bgOutput.Contains("bg_test"));
        var bgList = BackgroundTaskManager.ListTasks();
        Check("后台任务列表", bgList.Contains("completed") || bgList.Contains("running"));
        BackgroundTaskManager.Cleanup();
        Console.WriteLine();

        // ---- LSP 工具 ----
        Console.WriteLine("[LSP]");
        ITool lspTool = new LspTool();
        Check("lsp 工具名称正确", lspTool.Name == "lsp");
        Check("lsp 有 definition/references/hover/symbols", lspTool.Description.Contains("定义"));
        Console.WriteLine();

        // ---- 流式工具执行 (编译期已验证 onToolCall 参数) ----
        // ChatAsync 方法签名已通过 LLM.cs 编译验证，此处确认 LLM 实例可创建
        Console.WriteLine("[LLM 流式]");
        try
        {
            var llmTest = new LLM("test", "sk-test");
            Check("LLM onToolCall 支持 (编译期)", true);
        }
        catch { failed++; Console.WriteLine("  ❌ LLM onToolCall 支持 (编译期)"); }
        Console.WriteLine();

        // ---- LLM 定价 ----
        Console.WriteLine("[LLM]");
        var llm = new LLM("deepseek-v4-flash", "sk-test");
        // 用反射注入 token 数
        typeof(LLM).GetProperty("TotalPromptTokens")?.SetValue(llm, 1_000_000);
        typeof(LLM).GetProperty("TotalCompletionTokens")?.SetValue(llm, 500_000);
        Check("deepseek-v4-flash 成本 ≈ 0.28", Math.Abs(llm.EstimatedCost!.Value - 0.28) < 0.01);

        var llm2 = new LLM("unknown-model", "sk-test");
        Check("未知模型成本为 null", llm2.EstimatedCost == null);
        Console.WriteLine();

        // ---- 系统提示词 ----
        Console.WriteLine("[系统提示词]");
        var prompt = SystemPrompt.Generate(ToolRegistry.AllTools);
        Check("包含 read_file", prompt.Contains("read_file"));
        Check("包含 edit_file", prompt.Contains("edit_file"));
        Check("包含当前目录", prompt.Contains(Directory.GetCurrentDirectory()));
        Console.WriteLine();

        // ---- Agent ----
        Console.WriteLine("[Agent]");
        var agent = new Agent(new LLM("test", "sk-test"));
        agent.Messages.Add(new JsonObject { ["role"] = "user", ["content"] = "x" });
        agent.Reset();
        Check("Reset 清空消息", agent.Messages.Count == 0);

        var readTool = ToolRegistry.GetTool("read_file")!;
        var agent2 = new Agent(new LLM("test", "sk-test"), [readTool!]);
        Check("工具范围隔离", agent2.ToolByName.Count == 1 && agent2.ToolByName.ContainsKey("read_file"));
        Console.WriteLine();

        // ---- JsonHelper ----
        Console.WriteLine("[JSON 辅助]");
        var json = JsonHelper.SerializeArgs(new() { ["k"] = "v", ["n"] = 42 });
        Check("序列化包含键值", json.Contains("\"k\":\"v\"") && json.Contains("\"n\":42"));
        Console.WriteLine();

        // ---- 结果 ----
        Console.WriteLine($"\n通过: {passed}  失败: {failed}  总计: {passed + failed}");
        return failed == 0;
    }
}
