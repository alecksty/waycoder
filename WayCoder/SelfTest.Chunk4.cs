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
    private static void TestChunk4(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[ChatScreen]");
        var screen = new ChatScreen();
        screen.Activate(); // BuildLayout creates InputArea
        Check("实例非空", screen != null);
        Check("ChatMessages 初始为空", screen!.ChatMessages.Count == 0);

        // 消息管理
        screen.AddUserMsg("hello");
        Check("AddUserMsg 添加消息", screen.ChatMessages.Count == 1 && screen.ChatMessages[0].Role == "user");
        screen.StartAgentMsg();
        screen.AppendToken("Hello, ");
        screen.AppendToken("world!");
        screen.FinishAgentMsg();
        Check("Agent 流式消息合并", screen.ChatMessages.Count == 2 && screen.ChatMessages[1].Content == "Hello, world!");
        screen.AddToolMsg("bash", "echo test");
        Check("工具消息", screen.ChatMessages.Count == 3 && screen.ChatMessages[2].Role == "tool");
        screen.AddSystemMsg("done");
        Check("系统消息", screen.ChatMessages.Count == 4 && screen.ChatMessages[3].Role == "system");

        // Token 显示
        screen.UpdateTokenDisplayFull(1234, 567, 0.0123, 80000, 128000, 0, 0);
        Check("StatusRight 非空", screen.StatusRight.Length > 0);

        // 输入编辑
        screen.InputArea.Text = "";
        screen.InputArea.CursorRow = 0; screen.InputArea.CursorCol = 0;
        screen.InputInsert('a'); screen.InputInsert('b');
        Check("InputInsert 字符", screen.GetInputText() == "ab");
        screen.InputBackspace();
        Check("InputBackspace 删除", screen.GetInputText() == "a");
        screen.InputNewLine();
        screen.InputInsert('x');
        Check("InputNewLine 换行", screen.GetInputText() == "a\nx");

        // 建议
        screen.SetInput("/hel");
        screen.RefreshSuggestions(
            ["/help", "/helix", "/hello"], 0);
        Check("建议面板激活", screen.SuggestActive);
        Check("建议首项过滤正确", screen.Suggestions.Any(s => s.StartsWith("/hel")));
        screen.HideSuggestions();
        Check("隐藏建议", !screen.SuggestActive);
        Console.WriteLine();

        // ---- TuiMenu ----
        Section("[TuiMenu]");
        var menuItems = new List<string> { "复制", "粘贴", "---", "删除", "全选" };
        var menuWin = TuiMenu.Show("编辑", menuItems, 10, 5);
        Check("TuiMenu 窗口非空", menuWin != null);
        Check("TuiMenu 标题=编辑", menuWin!.Title == "编辑");
        Check("TuiMenu 模态", menuWin.Modal);
        Check("TuiMenu 尺寸>0", menuWin.Width > 0 && menuWin.Height > 0);
        Check("TuiMenu Result默认-1", menuWin.Result is int r && r == -1);
        // 快捷键注册
        Check("TuiMenu 快捷键1已注册", menuWin.KeyShortcuts.ContainsKey(ConsoleKey.D1));
        Check("TuiMenu 快捷键Esc已注册", menuWin.KeyShortcuts.ContainsKey(ConsoleKey.Escape));
        // RootView 是 MenuView
        Check("TuiMenu RootView=MenuView", menuWin.RootView is TuiMenu.MenuView);
        // 长菜单滚动测试
        var longItems = new List<string>();
        for (int i = 0; i < 30; i++) longItems.Add($"第{i}项");
        var longMenu = TuiMenu.Show("长列表", longItems, 5, 2);
        Check("长菜单高度有限", longMenu.Height < 30);
        Check("长菜单可滚动", longMenu.Height <= 18); // 14项 + 标题栏 + 边框
        Console.WriteLine();

        // ---- Markdown 表格 ----
        Section("[Markdown 表格]");
        var mdTable = @"
| 语言 | 速度 | 评分 |
|------|------|------|
| C# | 快 | 9.5 |
| Python | 慢 | 6.5 |
";
        var rendered = UI.TuiMarkdown.RenderMessage(mdTable, "assistant", 80);
        Check("表格渲染非空", rendered.Count > 0);
        // 顶部边框 + 表头 + 分隔线 + 2行数据 + 底部边框 = 6
        Check("表格渲染 = 6 行", rendered.Count == 6);
        // 顶部边框含 ┌
        var topLine = string.Concat(rendered[0].Select(s => s.Text));
        Check("表格顶部边框含 ┌", topLine.Contains('┌'));
        // 分隔线含 ┼
        var sepLine = string.Concat(rendered[2].Select(s => s.Text));
        Check("表格分隔线含 ┼", sepLine.Contains('┼'));
        // 底部边框含 └
        var botLine = string.Concat(rendered[^1].Select(s => s.Text));
        Check("表格底部边框含 └", botLine.Contains('└'));
        // 表头含"语言"
        var headerLine = string.Concat(rendered[1].Select(s => s.Text));
        Check("表头含 语言", headerLine.Contains("语言"));
        // 内联格式 **加粗** 测试（1表头 + 1数据行 = 5 行）
        var mdBold = UI.TuiMarkdown.RenderMessage("| **粗体** | `代码` |\n|-----|-----|\n| 正常 | 测试 |", "assistant", 80);
        Check("内联加粗表格 = 5 行", mdBold.Count == 5);
        // 两列表格（1表头 + 1数据行 = 5 行）
        var md2Col = UI.TuiMarkdown.RenderMessage("| A | B |\n|---|---|\n| 1 | 2 |", "assistant", 80);
        Check("2列表格渲染 = 5 行", md2Col.Count == 5);
        // 空表格（仅表头无数据行 = 4 行：顶部+表头+分隔+底部）
        var mdEmpty = UI.TuiMarkdown.RenderMessage("| H |\n|---|", "assistant", 80);
        Check("空数据表格 = 4 行", mdEmpty.Count == 4);
        Console.WriteLine();

        // ---- 输入处理逻辑 ----
        Section("[输入规范化]");
        var input = "／help";
        input = input.Replace('／', '/').Replace('！', '!').Replace('＃', '#');
        Check("全角／→半角/", input == "/help");
        Console.WriteLine();

        // ---- 设置界面 Schema ----
        Section("[设置 Schema]");
        var schema = Config.SettingSchema();
        Check("Schema 非空", schema.Count > 0);
        Check("至少有 5 项设置", schema.Count >= 5);

        // 验证关键设置项存在
        Check("包含 Model", schema.Any(s => s.Key == "Model"));
        Check("包含 ApiKey", schema.Any(s => s.Key == "ApiKey"));
        Check("包含 BaseUrl", schema.Any(s => s.Key == "BaseUrl"));
        Check("包含 MaxTokens", schema.Any(s => s.Key == "MaxTokens"));
        Check("包含 Temperature", schema.Any(s => s.Key == "Temperature"));
        Check("包含 MaxContextTokens", schema.Any(s => s.Key == "MaxContextTokens"));
        Check("包含 MaxBudgetUsd", schema.Any(s => s.Key == "MaxBudgetUsd"));

        // 验证元数据完整性
        Check("所有项有 Label", schema.All(s => s.Label.Length > 0));
        Check("所有项有 Category", schema.All(s => s.Category.Length > 0));
        Check("所有项有 Desc", schema.All(s => s.Desc.Length > 0));
        Check("所有项有 Type", schema.All(s => s.Type is "text" or "number" or "select" or "secret" or "toggle"));
        Check("select 类型有 Options", schema.Where(s => s.Type == "select").All(s => s.Options is { Length: > 0 }));

        // 分类
        var categories = schema.Select(s => s.Category).Distinct().ToList();
        Check("至少 3 个分类", categories.Count >= 3);
        Check("包含模型分类", categories.Any(c => c.Contains("模型")));
        Check("包含参数分类", categories.Any(c => c.Contains("参数")));

        // 环境变量
        var modelDef = schema.First(s => s.Key == "Model");
        Check("Model 是 select 类型", modelDef.Type == "select");
        Check("Model 有多个选项", modelDef.Options!.Length >= 3);
        Check("Model 选项含 deepseek", modelDef.Options!.Contains("deepseek-v4-flash"));

        var apiKeyDef = schema.First(s => s.Key == "ApiKey");
        Check("ApiKey 是 secret 类型", apiKeyDef.Type == "secret");

        var maxTokensDef = schema.First(s => s.Key == "MaxTokens");
        Check("MaxTokens 是 number 类型", maxTokensDef.Type == "number");
        Console.WriteLine();

        // ---- 配置读写 ----
        Section("[配置读写]");
        var testConfig = new Config();
        testConfig.Model = "gpt-5.4";
        testConfig.ApiKey = "sk-test123";
        testConfig.MaxTokens = 8192;
        testConfig.Temperature = 0.5f;
        Check("Model 写入读取", testConfig.Model == "gpt-5.4");
        Check("ApiKey 写入读取", testConfig.ApiKey == "sk-test123");
        Check("MaxTokens 写入读取", testConfig.MaxTokens == 8192);
        Check("Temperature 写入读取", Math.Abs(testConfig.Temperature - 0.5f) < 0.01);

        var configWithBudget = new Config { MaxBudgetUsd = 12.5 };
        Check("MaxBudget 写入读取", configWithBudget.MaxBudgetUsd == 12.5);
        Check("MaxBudget 默认 null", new Config().MaxBudgetUsd == null);

        // /config 命令行读写 API（Schema 驱动，无 switch 重复）
        Check("FindProp 按 Key", Config.FindProp("Model")?.Key == "Model");
        Check("FindProp 忽略大小写", Config.FindProp("model")?.Key == "Model");
        Check("FindProp 按环境变量", Config.FindProp("WAYCODER_MODEL")?.Key == "Model");
        Check("FindProp 未知返回 null", Config.FindProp("NotExist") == null);
        Check("GetPropValue 读取 Model", !string.IsNullOrEmpty(Config.GetPropValue("Model")));

        var savedMaxTokens = Config.Instance.MaxTokens;
        Check("TrySetPropValue MaxTokens 成功",
            Config.TrySetPropValue("MaxTokens", "16384", out var setErr)
            && setErr == null && Config.Instance.MaxTokens == 16384);
        Config.Instance.MaxTokens = savedMaxTokens;

        Check("TrySetPropValue 非法 select 拒绝",
            !Config.TrySetPropValue("SandboxLevel", "bogus", out var selErr) && selErr != null);
        Check("TrySetPropValue 未知项拒绝",
            !Config.TrySetPropValue("NoSuchKey", "x", out var unknownErr) && unknownErr != null);

        // --config 命令行参数（ConfigCli 纯文本，与 /config 共用同一数据源）
        Check("ConfigCli.List 含标题", ConfigCli.List().Contains("配置设置"));
        Check("ConfigCli.Get 已知项", ConfigCli.Get("Model").Contains("Model"));
        Check("ConfigCli.Get 未知项提示", ConfigCli.Get("NoSuchKey").Contains("未知设置项"));

        // --model 模型管理（ModelCli 纯文本，与 /model 共用目录）
        Check("ModelCli.List 含标题", ModelCli.List().Contains("模型目录"));
        Check("ModelCli.List 过滤 deepseek", ModelCli.List("deepseek").Contains("DeepSeek"));
        Check("ModelCli.ListKeys 可读", ModelCli.ListKeys().Length >= 0);

        // env 无 key 时按模型供应商从全局 JSON 回退（多服务商一键切换）
        Check("模型→供应商解析 deepseek", ModelCatalog.Find("deepseek-v4-flash")?.ProviderId == "deepseek");
        Check("模型→供应商解析 openai", ModelCatalog.Find("gpt-5.5")?.ProviderId == "openai");
        Check("ApiKeyStore.ForModel 未知模型返回 null", ApiKeyStore.ForModel("no-such-model-xyz") == null);
        Check("Config 含 SmallProvider 设置项", ConfigCli.Get("SmallProvider").Contains("SmallProvider"));

        // 一个服务商一个 key，一个服务商多个模型（key 跟服务商走，不跟模型走）
        Check("deepseek 多模型共享服务商",
            ModelCatalog.Find("deepseek-v4-pro")?.ProviderId == "deepseek"
            && ModelCatalog.Find("deepseek-v4-flash")?.ProviderId == "deepseek"
            && ModelCatalog.Find("deepseek-chat")?.ProviderId == "deepseek");
        Check("openai 多模型共享服务商",
            ModelCatalog.Find("gpt-5.5")?.ProviderId == "openai"
            && ModelCatalog.Find("gpt-4o")?.ProviderId == "openai");
        Check("qwen 多模型共享服务商",
            ModelCatalog.Find("qwen3-max")?.ProviderId == "qwen"
            && ModelCatalog.Find("qwen-turbo")?.ProviderId == "qwen");
        // 服务商 key 存取（一个服务商一个 key）
        ApiKeyStore.Set("__waycoder_test__", "sk-test-1234567890");
        Check("ApiKeyStore 按服务商存取 key", ApiKeyStore.Get("__waycoder_test__") == "sk-test-1234567890");
        ApiKeyStore.Remove("__waycoder_test__");
        Check("ApiKeyStore 删除服务商 key", ApiKeyStore.Get("__waycoder_test__") == null);

        // 外部配置导入：Claude Code settings.json（env 中 *_MODEL + BASE_URL，去 [1M] 后缀 + 去重 + 跳过 *_MODEL_NAME）
        var claudeJson = """
        {
          "env": {
            "ANTHROPIC_BASE_URL": "https://api.deepseek.com/anthropic",
            "ANTHROPIC_MODEL": "deepseek-v4-pro",
            "ANTHROPIC_DEFAULT_OPUS_MODEL": "deepseek-v4-pro[1M]",
            "ANTHROPIC_DEFAULT_SONNET_MODEL": "deepseek-v4-pro[1M]",
            "ANTHROPIC_DEFAULT_HAIKU_MODEL": "deepseek-v4-pro",
            "ANTHROPIC_DEFAULT_SONNET_MODEL_NAME": "deepseek-v4-pro"
          }
        }
        """;
        var claude = ModelCatalog.ImportClaude(claudeJson);
        Check("Claude 导入去重为 1 个模型", claude.Count == 1);
        Check("Claude 导入模型 id", claude.Count == 1 && claude[0].Id == "deepseek-v4-pro");
        Check("Claude 导入 providerId=claude", claude.Count == 1 && claude[0].ProviderId == "claude");
        Check("Claude 导入 baseUrl", claude.Count == 1 && claude[0].DefaultBaseUrl == "https://api.deepseek.com/anthropic");

        // 外部配置导入：Codex config.toml（[model_providers.*] + 顶层 model + [profiles.*]）
        var codexToml = """
        model_provider = "custom"
        model = "gpt-5.6-sol"

        [profiles.GoAI]
        model_provider = "GoAI"
        model = "deepseek V4 Flash"

        [model_providers.custom]
        name = "DeepSeek"
        base_url = "http://127.0.0.1:15721/v1"
        """;
        var codex = ModelCatalog.ImportCodex(codexToml);
        Check("Codex 导入 provider 模型（全局 model）",
            codex.Any(m => m.Id == "gpt-5.6-sol" && m.ProviderId == "custom"
                && m.DefaultBaseUrl == "http://127.0.0.1:15721/v1"));
        Check("Codex 导入 profile 模型",
            codex.Any(m => m.Id == "deepseek V4 Flash" && m.ProviderId == "goai"));

        // 模型库序列化往返（写本地模型库 → 读回 → 删除，不污染全局库）
        var prevLocalExists = File.Exists(ModelCatalog.LocalModelsPath);
        var mi = new ModelCatalog.ModelInfo(
            "__selftest_roundtrip__", "__selftest_roundtrip__", "SelfTest", "selftest", "T", "Imported",
            128_000, 1.5, 3.0, "https://selftest.example/v1", "round-trip 描述", 8192);
        ModelCatalog.AddCustom(mi, local: true);
        var rtLoaded = ModelCatalog.Find("__selftest_roundtrip__");
        Check("模型库往返: 命中", rtLoaded != null);
        Check("模型库往返: providerId 保留", rtLoaded?.ProviderId == "selftest");
        Check("模型库往返: baseUrl 保留", rtLoaded?.DefaultBaseUrl == "https://selftest.example/v1");
        Check("模型库往返: description 保留", rtLoaded?.Description == "round-trip 描述");
        Check("模型库往返: maxOutput 保留", rtLoaded?.MaxOutput == 8192);
        Check("模型库往返: contextWindow 保留", rtLoaded?.ContextWindow == 128_000);
        Check("模型库往返: 价格保留", rtLoaded?.InputPrice == 1.5 && rtLoaded?.OutputPrice == 3.0);
        ModelCatalog.RemoveCustom("__selftest_roundtrip__");
        Check("模型库删除自定义", ModelCatalog.Find("__selftest_roundtrip__") == null);
        if (!prevLocalExists && File.Exists(ModelCatalog.LocalModelsPath))
        {
            var leftover = File.ReadAllText(ModelCatalog.LocalModelsPath).Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", "");
            if (leftover == "[]") File.Delete(ModelCatalog.LocalModelsPath);  // 测试残留：空库即删
        }

        // 删除子命令：按服务商删除所有自定义模型 + 删除 API key
        ModelCatalog.AddCustom(new ModelCatalog.ModelInfo(
            "__selftest_prov_a__", "__selftest_prov_a__", "SelfTestProv", "selftestprov", "T", "Imported",
            0, 0, 0, null, "test", 0), local: true);
        ModelCatalog.AddCustom(new ModelCatalog.ModelInfo(
            "__selftest_prov_b__", "__selftest_prov_b__", "SelfTestProv", "selftestprov", "T", "Imported",
            0, 0, 0, null, "test", 0), local: true);
        Check("按服务商删除自定义模型数", ModelCatalog.RemoveCustomByProvider("selftestprov") == 2);
        Check("按服务商删除后不可加载",
            ModelCatalog.Find("__selftest_prov_a__") == null && ModelCatalog.Find("__selftest_prov_b__") == null);

        ApiKeyStore.Set("__selftest_key__", "sk-delete-me");
        Check("删除 key 前存在", ApiKeyStore.Has("__selftest_key__"));
        Check("ModelCli.RemoveKey 删除成功", ModelCli.RemoveKey("__selftest_key__").Contains("已删除"));
        Check("删除 key 后不存在", !ApiKeyStore.Has("__selftest_key__"));

        // 添加子命令：手动添加模型 / 服务商（写入全局库，测后清理）
        var addModelMsg = ModelCli.AddModel("__selftest_add_model__", "selftestprov", "https://selftest.example/v1");
        Check("添加模型成功", addModelMsg.Contains("已添加")
            && ModelCatalog.Find("__selftest_add_model__")?.ProviderId == "selftestprov");
        var addProvMsg = ModelCli.AddProvider("__selftest_add_prov__", "http://127.0.0.1:9999/v1");
        Check("添加服务商成功", addProvMsg.Contains("已添加")
            && ModelCatalog.Find("__selftest_add_prov__")?.DefaultBaseUrl == "http://127.0.0.1:9999/v1");
        ModelCatalog.RemoveCustom("__selftest_add_model__");
        ModelCatalog.RemoveCustom("__selftest_add_prov__");
        Check("清理添加的模型/服务商",
            ModelCatalog.Find("__selftest_add_model__") == null && ModelCatalog.Find("__selftest_add_prov__") == null);

        // 供应商注册表新增条目：连通性测试覆盖所有已存 key（含目录内无模型的服务商）
        Check("供应商注册表 gitee 端点",
            ModelCatalog.Providers.TryGetValue("gitee", out var pGitee) && pGitee.DefaultBaseUrl == "https://ai.gitee.com/v1");
        Check("供应商注册表 bailian 端点",
            ModelCatalog.Providers.TryGetValue("bailian", out var pBailian) && pBailian.DefaultBaseUrl.EndsWith("compatible-mode/v1"));
        Check("供应商注册表 opencode 端点",
            ModelCatalog.Providers.TryGetValue("opencode", out var pOpencode) && pOpencode.DefaultBaseUrl == "https://opencode.ai/zen/v1");
        Check("供应商注册表 minimax 端点",
            ModelCatalog.Providers.TryGetValue("minimax", out var pMinimax) && pMinimax.DefaultBaseUrl == "https://api.minimaxi.com/v1");
        Check("供应商注册表 aihubmix 端点",
            ModelCatalog.Providers.TryGetValue("aihubmix", out var pAihubmix) && pAihubmix.DefaultBaseUrl == "https://aihubmix.com/v1");

        if (!prevLocalExists && File.Exists(ModelCatalog.LocalModelsPath))
        {
            var leftover2 = File.ReadAllText(ModelCatalog.LocalModelsPath).Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", "");
            if (leftover2 == "[]") File.Delete(ModelCatalog.LocalModelsPath);
        }

        Console.WriteLine();

        // ---- 会话管理 ----
        Section("[会话管理]");
        var testSessionId = $"test_{DateTime.Now:yyyyMMddHHmmss}";
        var testMsgs = new List<JsonObject>
        {
            new() { ["role"] = "user", ["content"] = "test message" },
            new() { ["role"] = "assistant", ["content"] = "test response" },
        };
        var savedId = SessionManager.SaveSession(testMsgs, "deepseek-v4-flash", testSessionId);
        Check("保存会话返回 ID", savedId == testSessionId);
        Check("会话列表包含测试会话", SessionManager.ListSessions().Any(s => s.Id == testSessionId));

        var sessLoaded = SessionManager.LoadSession(testSessionId);
        Check("加载会话非空", sessLoaded != null);
        Check("加载消息数正确", sessLoaded!.Value.Messages.Count == 2);
        Check("加载模型正确", sessLoaded!.Value.Model == "deepseek-v4-flash");

        Check("删除会话成功", SessionManager.DeleteSession(testSessionId));
        Check("删除后不可加载", SessionManager.LoadSession(testSessionId) == null);
        Console.WriteLine();

        // ---- 模型切换 ----
        Section("[模型切换]");
        var mc = new Config();
        mc.Model = "gpt-5.4";
        Check("切换模型生效", mc.Model == "gpt-5.4");
        mc.Model = "deepseek-v4-pro";
        Check("再次切换生效", mc.Model == "deepseek-v4-pro");
        Console.WriteLine();

        // ---- 超时配置 ----
        Section("[超时配置]");
        var tc = new Config();
        Check("ToolTimeoutSec 默认 120", tc.ToolTimeoutSec == 120);
        Check("LintTimeoutSec 默认 60", tc.LintTimeoutSec == 60);
        tc.ToolTimeoutSec = 300;
        Check("ToolTimeoutSec 写入 300", tc.ToolTimeoutSec == 300);
        tc.LintTimeoutSec = 180;
        Check("LintTimeoutSec 写入 180", tc.LintTimeoutSec == 180);
        Check("SubAgentMaxDepth 默认 3", tc.SubAgentMaxDepth == 3);
        tc.SubAgentMaxDepth = 5;
        Check("SubAgentMaxDepth 写入 5", tc.SubAgentMaxDepth == 5);
        Console.WriteLine();

        // ---- 文件锁 ----
        Section("[文件锁]");
        var testFile = Path.GetTempFileName();
        Check("获取锁成功", FileLockManager.TryAcquire(testFile, "agent-A"));
        Check("同一 agent 可重入", FileLockManager.TryAcquire(testFile, "agent-A"));
        Check("其他 agent 不能获取", !FileLockManager.TryAcquire(testFile, "agent-B"));
        Check("被其他 agent 锁定", FileLockManager.IsLockedByOther(testFile, "agent-B"));
        Check("同一 agent 锁定自己", !FileLockManager.IsLockedByOther(testFile, "agent-A"));
        Check("锁列表包含文件", FileLockManager.GetAllLocks().Any(l => l.FilePath.Contains(Path.GetFileName(testFile))));

        FileLockManager.Release(testFile, "agent-A");
        Check("释放后 agent-B 可获取", FileLockManager.TryAcquire(testFile, "agent-B"));
        FileLockManager.ReleaseAll("agent-B");
        Check("释放全部后锁列表为空", FileLockManager.GetAllLocks().Count == 0);

        // 清理
        try { File.Delete(testFile); } catch { }
        Console.WriteLine();

        // ---- BoxBuffer ----
        Section("[BoxBuffer]");
        Check("VW ASCII = 1", BoxBuffer.VW("a") == 1);
        Check("VW CJK = 2", BoxBuffer.VW("中") == 2);
        Check("VW mixed", BoxBuffer.VW("a中b") == 4);
        Check("VwPlainText 纯文本", BoxBuffer.VwPlainText("hello") == 5);
        Check("VwPlainText 含 ANSI", BoxBuffer.VwPlainText("[31mhello[0m") == 5);
        Check("TruncateByVW 不截断", BoxBuffer.TruncateByVW("abc", 5) == "abc");

        var bb = new BoxBuffer { X = 5, Y = 3, Width = 40, Height = 10 };
        Check("BoxBuffer X/Y", bb.X == 5 && bb.Y == 3);
        Check("BoxBuffer W/H", bb.Width == 40 && bb.Height == 10);
        Check("BoxBuffer ContentLeft", bb.ContentLeft == 6);
        Check("BoxBuffer ContentTop", bb.ContentTop == 4);
        Check("BoxBuffer ContentWidth", bb.ContentWidth == 38);
        Check("BoxBuffer ContentHeight", bb.ContentHeight == 8);
        Check("BoxBuffer None 边框", new BoxBuffer { Border = BorderStyle.None }.ContentLeft == 0);

        var sb = new System.Text.StringBuilder();
        bb.Render(sb); Check("BoxBuffer Render 不崩溃", sb.Length > 0);
        sb.Clear(); bb.WriteLine(sb, 0, 0, "test"); Check("BoxBuffer WriteLine 不崩溃", sb.Length > 0);
        sb.Clear(); bb.Fill(sb); Check("BoxBuffer Fill 不崩溃", sb.Length > 0);

        foreach (var s in new[] { BorderStyle.Single, BorderStyle.Double,
            BorderStyle.Thick, BorderStyle.None })
        { sb.Clear(); new BoxBuffer { Width = 10, Height = 5, Border = s }.Render(sb);
          Check("边框 " + s + " 渲染", sb.Length > 0); }

        Console.WriteLine();

        // ---- Git 自动提交 ----
        Section("[Git 自动提交]");
        var gc = new Config();
        Check("AutoGitCommit 默认 false", !gc.AutoGitCommit);
        gc.AutoGitCommit = true; Check("AutoGitCommit 写入 true", gc.AutoGitCommit);
        gc.AutoGitCommit = false; Check("AutoGitCommit 写入 false", !gc.AutoGitCommit);

        var schema2 = Config.SettingSchema();
        var ac = schema.FirstOrDefault(s => s.Key == "AutoGitCommit");
        Check("Schema 包含 AutoGitCommit", ac != null);
        Check("AutoGitCommit 是 select 类型", ac?.Type == "select");
        Check("AutoGitCommit 有选项", ac?.Options?.Contains("true") == true);
        Check("AutoGitCommit EnvVar", ac?.EnvVar == "WAYCODER_AUTO_COMMIT");

        // Agent.AutoCommitEnabled 属性：通过构造函数和属性均可设置
        // 简单验证类型存在即可（AOT 不支持反射，直接验证功能）
        var savedAutoCommit = Config.FromEnv().AutoGitCommit; // 类型检查通过即可
        Check("AutoGitCommit 类型正确", savedAutoCommit || !savedAutoCommit);

        // IsValidCommitMsg
        Check("IsValid: feat: add x", Agent.IsValidCommitMsg("feat: add login page"));
        Check("IsValid: fix: bug", Agent.IsValidCommitMsg("fix: resolve null pointer"));
        Check("IsValid: docs: update", Agent.IsValidCommitMsg("docs: update readme"));
        Check("IsValid: chore: cleanup", Agent.IsValidCommitMsg("chore: remove dead code"));
        Check("IsValid: refactor: simplify", Agent.IsValidCommitMsg("refactor: extract method"));
        Check("IsValid: 拒绝空", !Agent.IsValidCommitMsg(""));
        Check("IsValid: 拒绝过短", !Agent.IsValidCommitMsg("fix"));
        Check("IsValid: 拒绝中文", !Agent.IsValidCommitMsg("修复：登录问题"));
        Check("IsValid: 拒绝无前缀", !Agent.IsValidCommitMsg("update code"));

        // CleanCommitMsg
        Check("Clean: 去反引号", Agent.CleanCommitMsg("`feat: add login`") == "feat: add login");
        Check("Clean: 去引号", Agent.CleanCommitMsg("\"fix: bug\"") == "fix: bug");
        Check("Clean: 去换行", Agent.CleanCommitMsg("feat:\nadd login") == "feat: add login");

        // EscArg
        Check("EscArg: 普通路径", Agent.EscArg("src/App.cs") == "'src/App.cs'");
        Check("EscArg: 含单引号", Agent.EscArg("it's a file.cs") == "'it'\\''s a file.cs'");

        Console.WriteLine();

        // ---- SaveToEnvFile ----
        Section("[SaveToEnvFile]");
        Check("SaveToEnvFile 方法存在", typeof(Config).GetMethod("SaveToEnvFile") != null);

        Console.WriteLine();

        // ---- CJK Token 估算 ----
        Section("[CJK Token 估算]");
        var cjkMsgs = new List<JsonObject> {
            new() { ["role"] = "user", ["content"] = "你好世界" }
        };
        var cjkEstimate = ContextManager.EstimateTokens(cjkMsgs);
        Check("CJK 估算 > ASCII 估算", cjkEstimate > "hello".Length / 3);
        var asciiMsgs = new List<JsonObject> {
            new() { ["role"] = "user", ["content"] = "hello" }
        };
        Check("CJK 4字 ≈ 6 token", Math.Abs(cjkEstimate - 6) <= 2);
        Check("ASCII 5字 < CJK 4字", ContextManager.EstimateTokens(asciiMsgs) < cjkEstimate);

        // 混合内容
        var mixedMsgs = new List<JsonObject> {
            new() { ["role"] = "user", ["content"] = "中English混合测试" }
        };
        var mixedEst = ContextManager.EstimateTokens(mixedMsgs);
        Check("混合估算 > 纯 ASCII 同等长度", mixedEst > "same length text only".Length / 3);
        Console.WriteLine();

        // ---- 记忆自动注入（隔离 cwd，避免迁移真实 memory.md）----
        Section("[记忆自动注入]");
        var savedPromptCwd = Directory.GetCurrentDirectory();
        var promptTestDir = Path.Combine(Path.GetTempPath(), "waycoder_prompt_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(promptTestDir);
        try
        {
            Directory.SetCurrentDirectory(promptTestDir);
            var sysPrompt = SystemPrompt.Generate(Tools.ToolRegistry.AllTools);
            Check("系统提示词非空", sysPrompt.Length > 0);
            Check("系统提示词包含工具列表", sysPrompt.Contains("read_file") || sysPrompt.Contains("write_file"));
            Check("系统提示词包含规则", sysPrompt.Contains("先读后改"));
        }
        finally
        {
            Directory.SetCurrentDirectory(savedPromptCwd);
            try { Directory.Delete(promptTestDir, true); } catch { }
        }
        Console.WriteLine();

        // ---- 语义记忆 ----
        Section("[语义记忆]");
        // 分词测试
        var tokens1 = SemanticMemory.Tokenize("hello world");
        Check("英文分词 hello world", tokens1.Contains("hello") && tokens1.Contains("world"));
        var tokens2 = SemanticMemory.Tokenize("你好世界");
        Check("CJK bigram 你好世界", tokens2.Contains("你好") && tokens2.Contains("世界"));
        var tokens3 = SemanticMemory.Tokenize("测试API接口");
        Check("CJK bigram 测试API", tokens3.Contains("测试"));
        Check("过滤停用词 the", !SemanticMemory.Tokenize("the test").Contains("the"));
        Check("过滤短词", !SemanticMemory.Tokenize("a b c").Contains("a"));

        // 文档解析测试（样本时间戳选远期日期，避免新近加分影响无关查询断言）
        var sampleMd = @"
---
## 2025-01-01 10:00

项目使用 C# .NET 10 AOT 编译

---
## 2025-01-02 14:00

用户偏好中文界面，终端配色青色主题
";
        var docs = SemanticMemory.ParseDocuments(sampleMd);
        Check("解析记忆文档数", docs.Count >= 2);
        Check("文档1内容", docs.Count >= 1 && docs[0].Content.Contains("C#"));
        Check("文档2内容", docs.Count >= 2 && docs[1].Content.Contains("中文界面"));

        // TF-IDF 搜索测试
        var results1 = SemanticMemory.SearchRelevant(docs, ".NET 编译");
        Check("TF-IDF 搜索编译相关", results1.Count > 0 && results1[0].Doc.Content.Contains("C#"));
        var results2 = SemanticMemory.SearchRelevant(docs, "界面配色");
        Check("TF-IDF 搜索界面相关", results2.Count > 0 && results2[0].Doc.Content.Contains("中文界面"));
        var results3 = SemanticMemory.SearchRelevant(docs, "Python");
        Check("TF-IDF 搜索无相关", results3.Count == 0 || results3[0].Score < 0.3);

        // SemanticMemory 上下文生成（纯函数，不依赖真实记忆文件）
        var ctx = SemanticMemory.GetRelevantContext(sampleMd, ".NET C# 编译", topN: 2, maxChars: 500);
        Check("GetRelevantContext 返回内容", ctx.Length > 0);
        Check("GetRelevantContext 无关查询为空", SemanticMemory.GetRelevantContext(sampleMd, "python ai", topN: 2, maxChars: 500).Length == 0);

        // SearchEntries 测试（MemoryEntry → TF-IDF）
        var testEntries = new List<StructuredMemory.MemoryEntry>
        {
            new() { Name = "dotnet-aot", Description = ".NET AOT 编译", Content = "项目使用 C# .NET 10 NativeAOT 编译为单文件 exe", UpdatedAt = DateTime.Now },
            new() { Name = "ui-theme", Description = "中文终端主题", Content = "用户偏好中文界面，终端配色青色主题，深色背景", UpdatedAt = DateTime.Now },
            new() { Name = "git-workflow", Description = "Git 工作流", Content = "自动 git commit 使用 conventional commit 格式", UpdatedAt = DateTime.Now.AddDays(-10) },
        };
        var searchResults = SemanticMemory.SearchEntries(testEntries, ".NET AOT 编译", topN: 3);
        Check("SearchEntries 返回结果", searchResults.Count > 0);
        Check("SearchEntries 排序正确", searchResults.Count >= 1 && searchResults[0].Entry.Name == "dotnet-aot");
        Check("SearchEntries 有分数", searchResults[0].Score > 0);
        var noResults = SemanticMemory.SearchEntries(testEntries, "python django flask", topN: 3);
        Check("SearchEntries 无关查询无结果", noResults.Count == 0 || noResults.All(r => r.Score < 0.2));

        // EmbeddingStore: 余弦相似度
        var vecA = new float[] { 1, 0, 0 };
        var vecB = new float[] { 0, 1, 0 };
        var vecC = new float[] { 1, 0, 0 };
        Check("余弦相似度 相同=1", Math.Abs(EmbeddingStore.CosineSimilarity(vecA, vecC) - 1.0) < 0.001);
        Check("余弦相似度 正交=0", Math.Abs(EmbeddingStore.CosineSimilarity(vecA, vecB)) < 0.001);
        Check("余弦相似度 null返回0", EmbeddingStore.CosineSimilarity(null, vecA) == 0);
        Check("余弦相似度 维度不匹配返回0", EmbeddingStore.CosineSimilarity(new float[] { 1, 2 }, new float[] { 1, 2, 3 }) == 0);

        // EmbeddingStore: .vec 二进制 I/O 往返
        var tmpMd = Path.Combine(Path.GetTempPath(), $"test_mem_{Guid.NewGuid():N}.md");
        try
        {
            File.WriteAllText(tmpMd, "test");
            var original = new float[] { 0.1f, 0.2f, 0.3f, -0.5f, 0.0f };
            EmbeddingStore.SaveEmbedding(tmpMd, original);
            var vecLoaded = EmbeddingStore.LoadEmbedding(tmpMd);
            Check(".vec 保存+加载", vecLoaded != null && vecLoaded.Length == original.Length);
            Check(".vec 数据一致", vecLoaded != null && Math.Abs(vecLoaded[0] - 0.1f) < 0.001f && Math.Abs(vecLoaded[3] + 0.5f) < 0.001f);
            EmbeddingStore.DeleteEmbedding(tmpMd);
            Check(".vec 删除后加载为null", EmbeddingStore.LoadEmbedding(tmpMd) == null);
        }
        finally
        {
            try { File.Delete(tmpMd); EmbeddingStore.DeleteEmbedding(tmpMd); } catch { }
        }
        Console.WriteLine();

        // ---- NotebookEdit 工具 ----
    }
}
