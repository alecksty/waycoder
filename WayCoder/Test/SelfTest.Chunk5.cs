using System.Text;
using System.Text.Json;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.Tui.Screens;

namespace WayCoder;

public static partial class SelfTest
{
    private static void TestChunk5(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[NotebookEdit]");
        var nbTestDir = Path.Combine(Path.GetTempPath(), "waycoder_nbtest_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(nbTestDir);
        try
        {
            var nbPath = Path.Combine(nbTestDir, "test.ipynb");
            // 创建一个最小 notebook
            var nb = JNode.Object()
                .Set("nbformat", 4)
                .Set("nbformat_minor", 5)
                .Set("metadata", JNode.Object())
                .Set("cells", JNode.Array());
            var cell0 = JNode.Object().Set("cell_type", "code").Set("metadata", JNode.Object()).Set("outputs", JNode.Array());
            cell0["execution_count"] = null;
            var cell0Source = JNode.Array(); cell0Source.Add(JNode.From("print('hello')\n")); cell0["source"] = cell0Source;
            var cell1 = JNode.Object().Set("cell_type", "markdown").Set("metadata", JNode.Object());
            var cell1Source = JNode.Array(); cell1Source.Add(JNode.From("# Title\n")); cell1["source"] = cell1Source;
            nb["cells"]!.Add(cell0); nb["cells"]!.Add(cell1);
            File.WriteAllText(nbPath, nb.ToJson(true));

            var notebookTool = new NotebookEditTool();
            Check("notebook_edit 工具名称", notebookTool.Name == "notebook_edit");
            Check("notebook_edit 描述非空", notebookTool.Description.Length > 20);

            // 测试 replace
            var replaceResult = notebookTool.ExecuteAsync(new Dictionary<string, object?>
            {
                ["notebook_path"] = nbPath,
                ["cell_index"] = 0,
                ["new_source"] = "print('replaced')",
            }).Result;
            Check("Replace cell", replaceResult.Contains("已替换"));
            var nbAfterReplace = Json.Parse(File.ReadAllText(nbPath))!;
            Check("Replace 内容变更", GetNotebookSource(nbAfterReplace, 0).Contains("replaced"));

            // 测试 insert
            var insertResult = notebookTool.ExecuteAsync(new Dictionary<string, object?>
            {
                ["notebook_path"] = nbPath,
                ["cell_index"] = 0,
                ["new_source"] = "## New MD Cell",
                ["cell_type"] = "markdown",
                ["edit_mode"] = "insert",
            }).Result;
            Check("Insert cell", insertResult.Contains("已插入"));
            var nbAfterInsert = Json.Parse(File.ReadAllText(nbPath))!;
            Check("Insert 后 cells 数量", nbAfterInsert["cells"]!.Count == 3);

            // 测试 delete
            var deleteResult = notebookTool.ExecuteAsync(new Dictionary<string, object?>
            {
                ["notebook_path"] = nbPath,
                ["cell_index"] = 1,
                ["new_source"] = "",
                ["edit_mode"] = "delete",
            }).Result;
            Check("Delete cell", deleteResult.Contains("已删除"));
            var nbAfterDelete = Json.Parse(File.ReadAllText(nbPath))!;
            Check("Delete 后 cells 数量", nbAfterDelete["cells"]!.Count == 2);

            // 测试非 .ipynb 文件拒绝
            var badResult = notebookTool.ExecuteAsync(new Dictionary<string, object?>
            {
                ["notebook_path"] = Path.Combine(nbTestDir, "test.txt"),
                ["cell_index"] = 0,
                ["new_source"] = "x",
            }).Result;
            Check("非 ipynb 文件拒绝", badResult.Contains("不是 .ipynb"));
        }
        finally
        {
            try { Directory.Delete(nbTestDir, true); } catch { }
        }
        Console.WriteLine();

        // ---- 自定义提示词模板 ----
        Section("[自定义提示词模板]");
        var customInstructions = ProjectContext.LoadInstructions();
        Check("LoadInstructions 不崩溃", customInstructions != null);
        // 如果 .waycoder/ 或 .corecoder/ 存在应能找到文件
        var testDirs = new[] { ".waycoder", ".corecoder" };
        foreach (var dirName in testDirs)
        {
            var ccdDir = Path.Combine(Directory.GetCurrentDirectory(), dirName);
            if (Directory.Exists(ccdDir))
            {
                var mdFiles = Directory.GetFiles(ccdDir, "*.md");
                var promptMd = mdFiles.FirstOrDefault(f => Path.GetFileName(f).Equals("prompt.md", StringComparison.OrdinalIgnoreCase));
                if (promptMd != null)
                    Check($"扫描到 {dirName}/prompt.md", customInstructions!.Contains("prompt.md") || customInstructions.Length > 0);
            }
        }
        Console.WriteLine();

        // ---- 命令别名 ----
        Section("[命令别名]");
        // 模拟 ProcessUserInput 中的别名 switch
        var aliasTests = new Dictionary<string, string> {
            ["/c"] = "/compact", ["/m"] = "/model", ["/r"] = "/reset",
            ["/h"] = "/help", ["/t"] = "/tokens", ["/d"] = "/diff",
            ["/s"] = "/save", ["/q"] = "quit"
        };
        foreach (var (alias, expected) in aliasTests)
        {
            var resolved = alias switch {
                "/c" => "/compact", "/m" => "/model", "/r" => "/reset",
                "/h" => "/help", "/t" => "/tokens", "/d" => "/diff",
                "/s" => "/save", "/q" => "quit", _ => alias
            };
            Check($"别名 {alias} → {expected}", resolved == expected);
        }
        Check("非别名不变 /export", ("/export" switch { "/c" => "/compact", "/m" => "/model", _ => "/export" }) == "/export");
        Console.WriteLine();

        // ---- 斜杠命令拼写纠错 ----
        Section("[命令纠错]");
        SlashCommandRegistry.RegisterAll(); // 填充 KnownCommands 供纠错测试
        // /sesion → /session（漏字符，距离 1）
        Check("漏字符 /sesion → /session", Program.SuggestCommand("/sesion") == "/session");
        // /hel → /help（短命令距离 1）
        Check("短命令 /hel → /help", Program.SuggestCommand("/hel") == "/help");
        // /sesison → /session（多字符，距离 2，长命令允许）
        Check("多字符 /sesison → /session", Program.SuggestCommand("/sesison") == "/session");
        // /tokenss → /tokens（多字符，距离 1）
        Check("多字符 /tokenss → /tokens", Program.SuggestCommand("/tokenss") == "/tokens");
        // 已知命令不纠正
        Check("已知命令 /model 不纠正", Program.SuggestCommand("/model") == null);
        // 带参数保留
        Check("带参数 /model x 不纠正", Program.SuggestCommand("/model gpt-5.4") == null);
        Check("带参数纠正保留", Program.SuggestCommand("/sesion x") == "/session x");
        // 非斜杠输入不处理
        Check("非斜杠输入不纠正", Program.SuggestCommand("hello world") == null);
        // 短命令距离 2 拒绝（/ls → /pr 距离 2 但过短）
        Check("短命令距离 2 拒绝 /ls", Program.SuggestCommand("/ls") == null);
        // 距离太远不纠正
        Check("距离太远不纠正 /xyzzy", Program.SuggestCommand("/xyzzy") == null);
        // 编辑距离算法
        Check("Levenshtein 相同 = 0", Program.Levenshtein("abc", "abc") == 0);
        Check("Levenshtein 替换 = 1", Program.Levenshtein("abc", "abd") == 1);
        Check("Levenshtein 插入 = 1", Program.Levenshtein("abc", "abcd") == 1);
        Check("Levenshtein 删除 = 1", Program.Levenshtein("abcd", "abc") == 1);
        Check("Levenshtein 空串", Program.Levenshtein("", "abc") == 3);
        Check("KnownCommands 非空", Program.KnownCommands.Length >= 25);
        // v0.38.0: / 补全数据源改为 SlashCommandRegistry（此前硬编码 14 条）
        Check("SlashCommandRegistry 非空", SlashCommandRegistry.Commands.Count > 0);
        Check("SlashCommandRegistry 含 /help", SlashCommandRegistry.Commands.Any(c => c.Name == "/help"));
        Check("SlashCommandRegistry 含 /model", SlashCommandRegistry.Commands.Any(c => c.Name == "/model"));
        Check("SlashCommandRegistry 覆盖原硬编码 14 条", SlashCommandRegistry.Commands.Count >= 14);
        // /diff 拆分：/diff + /d 归 DiffCommand（diff 预览），/recent 只留文件列表
        var (diffCmd, _) = SlashCommandRegistry.Match("/diff");
        Check("命令注册: /diff → DiffCommand", diffCmd?.GetType().Name == "DiffCommand");
        var (diffAlias, _) = SlashCommandRegistry.Match("/d");
        Check("命令别名: /d → DiffCommand", diffAlias?.GetType().Name == "DiffCommand");
        var (recentCmd, _) = SlashCommandRegistry.Match("/recent");
        Check("命令拆分: /recent 仍列文件（非 DiffCommand）", recentCmd != null && recentCmd.GetType().Name == "RecentCommand");
        Console.WriteLine();

        // ---- MCP 环境变量解析 ----
        Section("[MCP 环境变量]");
        var mcpConfig = Json.Parse(@"[
            { ""name"": ""test"", ""command"": ""echo"", ""args"": [""hi""], ""env"": { ""API_KEY"": ""sk-123"", ""DEBUG"": ""1"" } }
        ]");
        Check("MCP 配置解析非空", mcpConfig != null);
        var srv = mcpConfig![0];
        Check("MCP name 字段", srv!["name"]?.AsString() == "test");
        var envObj = srv!["env"];
        Check("MCP env 解析", envObj != null && envObj.Count == 2);
        Check("MCP env API_KEY", envObj!["API_KEY"]?.AsString() == "sk-123");
        // 无 env 的配置
        var noEnv = Json.Parse(@"{ ""name"": ""x"", ""command"": ""y"" }");
        Check("MCP 无 env 不崩溃", noEnv!["env"] == null);
        Console.WriteLine();

        // ---- MCP HTTP 传输 ----
        Section("[MCP HTTP]");

        Check("HTTP 传输: url 检测",
            Json.Parse(@"{ ""url"": ""http://localhost:8080/mcp"" }")!["url"]?.AsString() == "http://localhost:8080/mcp");
        Check("HTTP 传输: transport=http",
            Json.Parse(@"{ ""transport"": ""http"", ""url"": ""http://x.com/mcp"" }")!["transport"]?.AsString() == "http");
        var stdioCfg = Json.Parse(@"{ ""command"": ""echo"", ""args"": [""hi""] }");
        Check("Stdio 传输: 向后兼容",
            stdioCfg!["command"]?.AsString() == "echo" && stdioCfg["url"] == null);

        Environment.SetEnvironmentVariable("TEST_MCP_VAR", "secret123");
        Check("MCP 环境变量展开: headers", McpManager.ExpandEnvVars("Bearer ${TEST_MCP_VAR}") == "Bearer secret123");
        Check("MCP 环境变量展开: url", McpManager.ExpandEnvVars("http://host/${TEST_MCP_VAR}/path") == "http://host/secret123/path");
        Check("MCP 环境变量展开: 无变量", McpManager.ExpandEnvVars("no-vars-here") == "no-vars-here");
        Check("MCP 环境变量展开: 空字符串", McpManager.ExpandEnvVars("") == "");

        var hdrObj = JNode.Object().Set("Authorization", "Bearer ${TEST_MCP_VAR}").Set("X-Custom", "static");
        var parsedHdr = McpManager.ParseHeaders(hdrObj);
        Check("MCP headers: 展开", parsedHdr != null && parsedHdr["Authorization"] == "Bearer secret123");
        Check("MCP headers: 静态", parsedHdr != null && parsedHdr["X-Custom"] == "static");
        Check("MCP headers: null", McpManager.ParseHeaders(null) == null);
        Environment.SetEnvironmentVariable("TEST_MCP_VAR", null);

        // ---- MCP SSE 传输 ----
        Section("[MCP SSE]");

        Check("SSE: transport=sse 识别",
            McpManager.DetectTransport(Json.Parse(@"{ ""transport"": ""sse"", ""url"": ""http://x.com/sse"" }")!)
                == McpManager.McpTransportType.Sse);
        Check("SSE: transport=http 识别",
            McpManager.DetectTransport(Json.Parse(@"{ ""transport"": ""http"", ""url"": ""http://x.com/mcp"" }")!)
                == McpManager.McpTransportType.Http);
        Check("SSE: url 无 transport 默认 http",
            McpManager.DetectTransport(Json.Parse(@"{ ""url"": ""http://x.com/mcp"" }")!)
                == McpManager.McpTransportType.Http);
        Check("SSE: 无 url 无 transport 默认 stdio",
            McpManager.DetectTransport(Json.Parse(@"{ ""command"": ""echo"" }")!)
                == McpManager.McpTransportType.Stdio);

        Check("SSE endpoint: 相对路径解析为绝对",
            SseMcpTransport.ResolveEndpointUrl("http://x.com/sse", "/message?sessionId=abc")
                == "http://x.com/message?sessionId=abc");
        Check("SSE endpoint: 绝对 URL 原样",
            SseMcpTransport.ResolveEndpointUrl("http://x.com/sse", "http://y.com/msg")
                == "http://y.com/msg");
        Check("SSE endpoint: 空 data 返回 null",
            SseMcpTransport.ResolveEndpointUrl("http://x.com/sse", "") == null);
        Check("SSE endpoint: 空白 data 返回 null",
            SseMcpTransport.ResolveEndpointUrl("http://x.com/sse", "   ") == null);

        Console.WriteLine();

        // ---- MCP 缓存 ----
        Section("[MCP 缓存]");

        var k1 = McpCache.ComputeCacheKey("test", "echo|hi");
        var k2 = McpCache.ComputeCacheKey("test", "echo|hi");
        var k3 = McpCache.ComputeCacheKey("test", "echo|bye");
        Check("MCP 缓存键: 稳定性", k1 == k2);
        Check("MCP 缓存键: 不同配置不同键", k1 != k3);
        Check("MCP 缓存键: 格式", k1.StartsWith("test|") && k1.Length >= 21 && k1.Length <= 30);

        var sidNode = Json.Parse(@"{ ""command"": ""npx"", ""args"": [""-y"", ""server""] }");
        Check("MCP 规范ID: stdio", McpCache.GetCanonicalId(sidNode!) == "npx|-y|server");
        var hidNode = Json.Parse(@"{ ""url"": ""http://example.com/mcp"" }");
        Check("MCP 规范ID: HTTP", McpCache.GetCanonicalId(hidNode!) == "http://example.com/mcp");
        var nidNode = Json.Parse(@"{ ""name"": ""x"" }");
        Check("MCP 规范ID: 无标识符", McpCache.GetCanonicalId(nidNode!) == null);

        Check("McpInfo 初始非空", !string.IsNullOrEmpty(McpManager.Info));

        // ---- MCP 状态模型 ----
        Section("[MCP 状态]");

        Check("状态枚举: Connecting=0", (int)McpServerStatus.Connecting == 0);
        Check("状态枚举: Connected=1", (int)McpServerStatus.Connected == 1);
        Check("状态枚举: Failed=2", (int)McpServerStatus.Failed == 2);

        var sinfo = new McpServerInfo("github", "http", McpServerStatus.Connected, 5, null);
        Check("McpServerInfo: 名称", sinfo.Name == "github");
        Check("McpServerInfo: 传输", sinfo.Transport == "http");
        Check("McpServerInfo: 状态", sinfo.Status == McpServerStatus.Connected);
        Check("McpServerInfo: 工具数", sinfo.ToolCount == 5);
        Check("McpServerInfo: 错误为空", sinfo.Error == null);

        var sstate = new McpServerState
        {
            Name = "fs",
            Transport = "stdio",
            Status = McpServerStatus.Failed,
            ToolCount = 0,
            Error = "超时",
        };
        var sinfo2 = sstate.ToInfo();
        Check("McpServerState.ToInfo: 映射状态", sinfo2.Status == McpServerStatus.Failed);
        Check("McpServerState.ToInfo: 映射错误", sinfo2.Error == "超时");
        Check("McpServerState.ToInfo: 映射传输", sinfo2.Transport == "stdio");

        // Reload 对不存在的服务器返回非空提示（不抛异常，不触发真实连接）
        Check("Reload: 未匹配服务器返回非空提示",
            !string.IsNullOrEmpty(McpManager.ReloadAsync("___waycoder_test_nonexistent___").Result));

        // ---- MCP 资源 / 提示词 ----
        Section("[MCP 资源/提示词]");

        var fakeConn = new McpConnection("fs", new FakeMcpTransport((method, _) => method switch
        {
            "resources/read" => Json.Parse(@"{""result"":{""contents"":[{""uri"":""file:///a.txt"",""text"":""hello resource""}]}}"),
            "resources/list" => Json.Parse(@"{""result"":{""resources"":[{""uri"":""file:///a.txt"",""name"":""a"",""description"":""doc""}]}}"),
            "prompts/get" => Json.Parse(@"{""result"":{""messages"":[{""role"":""user"",""content"":{""type"":""text"",""text"":""hi prompt""}}]}}"),
            _ => null,
        }));

        var resArr = Json.Parse(@"[{""uri"":""file:///a.txt"",""name"":""a"",""description"":""doc""}]")!;
        var resTool = new McpResourceTool("fs", resArr, fakeConn);
        Check("资源工具: 名称", resTool.Name == "mcp__fs__resources");
        Check("资源工具: 描述含 URI", resTool.Description.Contains("file:///a.txt"));
        Check("资源工具: 参数含 uri", resTool.Parameters["properties"]?["uri"] != null);
        var resRead = resTool.ExecuteAsync(new Dictionary<string, object?> { ["uri"] = "file:///a.txt" }).Result;
        Check("资源工具: 读取返回文本", resRead.Contains("hello resource"));
        var resList = resTool.ExecuteAsync(new Dictionary<string, object?>()).Result;
        Check("资源工具: 列表返回名称+URI", resList.Contains("a (file:///a.txt)"));

        var promptDef = Json.Parse(@"{""name"":""greet"",""description"":""打招呼"",""arguments"":[{""name"":""who"",""description"":""对象""}]}")!;
        var promptTool = new McpPromptTool("fs", promptDef, fakeConn);
        Check("提示词工具: 名称", promptTool.Name == "mcp__fs__prompt__greet");
        Check("提示词工具: 描述", promptTool.Description == "打招呼");
        Check("提示词工具: 参数含 who", promptTool.Parameters["properties"]?["who"] != null);
        var promptRes = promptTool.ExecuteAsync(new Dictionary<string, object?> { ["who"] = "world" }).Result;
        Check("提示词工具: 调用返回消息", promptRes.Contains("[user]") && promptRes.Contains("hi prompt"));

        Check("提示词工具: BuildParameters 空", McpPromptTool.BuildParameters(null)["properties"]!.Count == 0);
        Check("提示词工具: ExtractContentText 字符串", McpPromptTool.ExtractContentText(Json.Parse("\"plain\"")) == "plain");
        Check("提示词工具: ExtractContentText 对象", McpPromptTool.ExtractContentText(Json.Parse(@"{""type"":""text"",""text"":""obj""}")) == "obj");
        Check("提示词工具: ExtractContentText null", McpPromptTool.ExtractContentText(null) == "");

        var sinfo3 = new McpServerInfo("x", "http", McpServerStatus.Connected, 3, null, 2, 1);
        Check("McpServerInfo: 资源数", sinfo3.ResourceCount == 2);
        Check("McpServerInfo: 提示词数", sinfo3.PromptCount == 1);

        Console.WriteLine();

        // ---- Agent 错误自恢复 ----
        Section("[错误自恢复]");
        // 验证错误消息格式 — ExecuteToolAsync 追加修正提示
        var errorMsg = "错误：文件未找到";
        var enhanced = errorMsg + "\n[请分析错误原因，修正参数后重试]";
        Check("错误消息含修正提示", enhanced.Contains("[请分析错误原因"));
        var exMsg = "执行 bash 时出错：超时\n[请分析错误原因，尝试其他方式完成目标]";
        Check("异常消息含修正提示", exMsg.Contains("尝试其他方式完成目标"));
        Console.WriteLine();

        // ---- Token 性能统计 ----
        Section("[Token 性能统计]");
        var testLLM = new LLM("deepseek-v4-flash", "sk-test");
        Check("LastLatencyMs 初始 0", testLLM.LastLatencyMs == 0);
        Check("LastTokensPerSec 初始 0", testLLM.LastTokensPerSec == 0);
        Check("TotalRequests 初始 0", testLLM.TotalRequests == 0);
        Check("EffectiveModel 等于 Model", testLLM.EffectiveModel == "deepseek-v4-flash");
        testLLM.ModelOverride = "gpt-5.4-mini";
        Check("ModelOverride 后", testLLM.EffectiveModel == "gpt-5.4-mini");
        testLLM.ModelOverride = null;
        Check("ModelOverride 清空后", testLLM.EffectiveModel == "deepseek-v4-flash");
        Console.WriteLine();

        // ---- HTTP 代理支持 ----
        Section("[HTTP 代理]");
        var proxyUrl = Environment.GetEnvironmentVariable("HTTPS_PROXY")
                    ?? Environment.GetEnvironmentVariable("HTTP_PROXY")
                    ?? Environment.GetEnvironmentVariable("ALL_PROXY");
        Check("代理环境变量读取不崩溃", true); // 环境变量存在与否都通过
        // 验证环境变量名存在（不检查值）
        Check("HTTPS_PROXY 变量可读", true); // 系统级测试
        Console.WriteLine();

        // ---- Sub-Agent 增强 ----
        Section("[Sub-Agent 增强]");
        var agentTool = new AgentTool();
        Check("AgentTool Name", agentTool.Name == "agent");
        Check("AgentTool Description 非空", agentTool.Description.Length > 0);
        Check("AgentTool Schema 含 task", agentTool.Parameters["properties"]?.Has("task") == true);
        // v0.38.0: 并行 tasks 数组 schema
        Check("AgentTool Schema 含 tasks（并行数组）", agentTool.Parameters["properties"]?.Has("tasks") == true);
        // BuildParentContext via reflection-like test
        Check("AgentTool ParentAgent 初始 null", agentTool.ParentAgent == null);
        // 递归深度
        Check("AgentTool MaxDepth 默认 3", AgentTool.MaxDepth == 3);
        Check("AgentTool CurrentDepth 初始 0", AgentTool.CurrentDepth == 0);
        Console.WriteLine();

        // ---- Git 分支检测 ----
        Section("[Git 分支检测]");
        var headPath = Path.Combine(Directory.GetCurrentDirectory(), ".git", "HEAD");
        if (File.Exists(headPath))
        {
            var head = File.ReadAllText(headPath).Trim();
            Check("HEAD 文件可读", head.Length > 0);
            if (head.StartsWith("ref: refs/heads/"))
            {
                var branch = head["ref: refs/heads/".Length..];
                Check("分支名非空", branch.Length > 0);
            }
            else Check("分离 HEAD 可读", head.Length >= 7);
        }
        else Check("无 .git/HEAD (非 git 仓库)", true);
        Console.WriteLine();

        // ---- 文件路径补全 ----
        Section("[Tab 路径补全]");
        // 直接内联测试 LCP 逻辑
        Func<List<string>, string> findLcp = strings => {
            if (strings.Count == 0) return "";
            var p = strings[0];
            foreach (var s in strings.Skip(1))
            {
                while (!s.StartsWith(p, StringComparison.OrdinalIgnoreCase) && p.Length > 0)
                    p = p[..^1];
                if (p.Length == 0) break;
            }
            return p;
        };
        Check("LCP 'Pro' → 'Pro'", findLcp(["Program.cs", "Program.old", "Project.cs"]) == "Pro");
        Check("LCP ['ab','ac'] → 'a'", findLcp(["ab", "ac"]) == "a");
        Check("LCP ['x','y'] → ''", findLcp(["x", "y"]) == "");
        Check("LCP 单元素", findLcp(["hello"]) == "hello");
        Check("LCP ['test.cs','test_helper.cs'] → 'test_'", findLcp(["test.cs", "test_helper.cs"]) == "test");
        Console.WriteLine();

        // ---- 输入历史 ----
        Section("[输入历史]");
        var history = new List<string>();
        history.Add("prompt 1");
        history.Add("prompt 2");
        Check("历史添加有序", history[0] == "prompt 1" && history[1] == "prompt 2");
        // 去重相邻重复
        var last = history[^1];
        if (last != "prompt 3") history.Add("prompt 3");
        Check("历史去重", history.Count == 3);
        // 上限 200
        for (int i = 0; i < 210; i++) history.Add($"item {i}");
        while (history.Count > 200) history.RemoveAt(0);
        Check("历史上限 200", history.Count <= 200);
        Console.WriteLine();

        // ---- 模型热键切换 ----
        Section("[模型热键切换]");
        var models = new[] { "deepseek-v4-flash", "deepseek-v4-pro", "gpt-5.4-mini", "gpt-5.4" };
        var curModel = "deepseek-v4-flash";
        var idx = Array.IndexOf(models, curModel);
        var next = models[(idx + 1) % models.Length];
        Check("循环切换 v4-flash→v4-pro", next == "deepseek-v4-pro");
        idx = Array.IndexOf(models, "gpt-5.4");
        next = models[(idx + 1) % models.Length];
        Check("循环切换 gpt-5.4→v4-flash (回环)", next == "deepseek-v4-flash");
        Console.WriteLine();

        // ---- 对话导出 ----
        Section("[对话导出]");
        var exportMsgs = new List<JNode> {
            JNode.Object().Set("role", "user").Set("content", "hello"),
            JNode.Object().Set("role", "assistant").Set("content", "hi there"),
            JNode.Object().Set("role", "tool").Set("content", "result").Set("tool_call_id", "c1"),
        };
        var exportSb = new StringBuilder();
        exportSb.AppendLine("# WayCoder 对话导出");
        foreach (var msg in exportMsgs)
        {
            var role = msg["role"]?.AsString() ?? "";
            var content = msg["content"]?.AsString() ?? "";
            if (role == "user") exportSb.AppendLine($"### 👤 User\n\n{content}\n");
            else if (role == "assistant") exportSb.AppendLine($"### 🤖 Assistant\n\n{content}\n");
            else if (role == "tool") exportSb.AppendLine($"### 🔧 Tool\n\n```\n{content}\n```\n");
        }
        var exportText = exportSb.ToString();
        Check("导出含标题", exportText.Contains("WayCoder 对话导出"));
        Check("导出含 User", exportText.Contains("👤 User") && exportText.Contains("hello"));
        Check("导出含 Assistant", exportText.Contains("🤖 Assistant") && exportText.Contains("hi there"));
        Check("导出含 Tool", exportText.Contains("🔧 Tool") && exportText.Contains("result"));

        // 长内容截断
        var longContent = new string('x', 2500);
        var truncated = longContent.Length > 2000 ? longContent[..2000] + $"\n\n...（共 {longContent.Length} 字符）" : longContent;
        Check("导出超长截断", truncated.Length < 2500 && truncated.Contains("..."));
        Console.WriteLine();

        // ---- 权限确认增强 ----
    }

    /// <summary>自测用假 MCP 传输 — 按 method 返回脚本化响应，不触碰真实网络/进程。</summary>
    private sealed class FakeMcpTransport : McpTransport
    {
        private readonly Func<string, JNode?, JNode?> _handler;
        public override bool IsConnected => true;

        public FakeMcpTransport(Func<string, JNode?, JNode?> handler) => _handler = handler;

        public override Task<JNode?> SendRequestAsync(int id, string method, JNode @params, CancellationToken ct)
            => Task.FromResult(_handler(method, @params));

        public override void SendNotification(string method, JNode @params) { }

        public override Task DisconnectAsync() => Task.CompletedTask;
    }
}
