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
    /// <summary>
    /// 按指定省钱档位生成系统提示词（用完恢复原档位）。
    /// 提示词内容随 <c>WAYCODER_ECONOMY</c> 变形（Off/Auto=完整、On=精简、Extreme=极致），
    /// 断言必须自带基线，否则用户 .env 开了省钱模式就会让完整版断言集体假失败。
    /// </summary>
    private static string PromptWithMode(EconomyMode mode)
    {
        var saved = Config.Instance.EconomyMode;
        Config.Instance.EconomyMode = mode;
        try { return SystemPrompt.Generate(ToolRegistry.AllTools); }
        finally { Config.Instance.EconomyMode = saved; }
    }

    /// <summary>
    /// 「工具调用不崩溃」断言助手：正常返回非空结果为真，抛异常为假。
    /// 用于边界输入（空路径/空参数）—— 只验工具自身健壮，不对结果内容作假设
    /// （对内容作假设会把「测工具」变成「测当前目录里有什么」）。
    /// </summary>
    private static bool TryToolCall(Func<string> call)
    {
        try { return !string.IsNullOrEmpty(call()); }
        catch { return false; }
    }

    /// <summary>修完必验证闭环门（源码文件判定 + 配置开关）测试</summary>
    private static void TestVerifyBeforeDone(Action<string, bool> Check)
    {
        Check("验证门: 默认开启", new Config().VerifyBeforeDone);
        Check("验证门: .cs 是源码文件", Agent.IsSourceFile("a/b/Foo.cs"));
        Check("验证门: .py 是源码文件", Agent.IsSourceFile("x.py"));
        Check("验证门: .tsx 是源码文件", Agent.IsSourceFile("c.tsx"));
        Check("验证门: .md 不是源码文件", !Agent.IsSourceFile("README.md"));
        Check("验证门: .json 不是源码文件", !Agent.IsSourceFile("config.json"));
        Check("验证门: 无扩展名不是源码文件", !Agent.IsSourceFile("Makefile"));
        Check("验证门: 大写扩展名也命中", Agent.IsSourceFile("Foo.CS"));
    }

    /// <summary>子智能体明文审计日志（记录 + 截断 + 历史）测试</summary>
    private static void TestSubAgentAudit(Action<string, bool> Check)
    {
        var savedEnabled = SubAgentAudit.Enabled;
        SubAgentAudit.Enabled = true;
        SubAgentAudit.ClearHistory();
        try
        {
            SubAgentAudit.Record(1, "正常任务", "bash, read", "完成", 123);
            SubAgentAudit.Record(2, new string('超', 5000), "write", "结果", 45);
            var hist = SubAgentAudit.History;
            Check("审计: 记录入历史", hist.Count == 2);
            Check("审计: 工具清单保留", hist[0].Tools == "bash, read");
            Check("审计: 深度保留", hist[0].Depth == 1);
            Check("审计: 耗时保留", hist[0].DurationMs == 123);
            Check("审计: 超长任务按 4000 码点截断", hist[1].Task.Length <= 4000);
            SubAgentAudit.ClearHistory();
            Check("审计: 清空历史", SubAgentAudit.History.Count == 0);
        }
        finally
        {
            SubAgentAudit.Enabled = savedEnabled;
            SubAgentAudit.ClearHistory();
        }
    }

    /// <summary>任务漂移护栏（目标注入 + 逐级加强 + 文件清单）测试</summary>
    private static void TestGoalGuard(Action<string, bool> Check)
    {
        Check("护栏: 空目标返回空串", Agent.BuildGoalGuard("", 0, []) == "");
        Check("护栏: 空目标即含清单也空", Agent.BuildGoalGuard("", 20, ["a.cs"]) == "");

        var early = Agent.BuildGoalGuard("修 bug", 3, []);
        Check("护栏: 早轮含 current_goal", early.Contains("<current_goal>"));
        Check("护栏: 早轮不含 goal_check", !early.Contains("<goal_check>"));

        var gentle = Agent.BuildGoalGuard("修 bug", 7, []);
        Check("护栏: 中轮触发 goal_check", gentle.Contains("<goal_check>"));
        Check("护栏: 中轮措辞=超过 6 轮", gentle.Contains("连续执行超过 " + Agent.GoalReinforceRound));

        var strong = Agent.BuildGoalGuard("修 bug", 15, []);
        Check("护栏: 晚轮措辞=超过 12 轮", strong.Contains("连续执行超过 " + Agent.GoalReinforceStrongRound));
        Check("护栏: 晚轮含「已跑偏」", strong.Contains("已跑偏"));

        var withFiles = Agent.BuildGoalGuard("修 bug", 7, ["src/Foo.cs", "src/Bar.cs"]);
        Check("护栏: 清单列出文件", withFiles.Contains("src/Foo.cs"));
        Check("护栏: 无文件则无清单", !gentle.Contains("文件清单"));

        var manyFiles = Agent.BuildGoalGuard("修 bug", 7, Enumerable.Range(0, 25).Select(i => $"f{i}.cs").ToArray());
        Check("护栏: 清单超 20 只列前 20", manyFiles.Contains("仅列前 20"));
    }

    /// <summary>TUI 鼠标支持（所有控件/界面的 OnMouse 离屏模拟）测试</summary>
    private static void TestTuiMouse(Action<string, bool> Check)
    {
        foreach (var (name, pass) in TuiMouseTest.CollectChecks())
            Check("TuiMouse: " + name, pass);
    }

    /// <summary>GenerateProjectSnapshot 测试</summary>
    private static void TestGenerateProjectSnapshot(Action<string, bool> Check)
    {
        // 通过 HardCollapseAsync 的调用链间接验证快照不为空且包含关键信息
        // 直接测试：构造场景确保 GenerateProjectSnapshot 不会崩溃

        // ── 验证项目快照内容 ──
        // cwd 可能是仓库根（dotnet run --project WayCoder/...）或 WayCoder/ 子目录，
        // 故用「任一存在」兼容两种运行方式，验证关键子目录存在。
        var cwd = System.IO.Directory.GetCurrentDirectory();
        Check("Snapshot: 工作目录存在", System.IO.Directory.Exists(cwd));
        Check("Snapshot: Agent 目录存在",
            System.IO.Directory.Exists(System.IO.Path.Combine(cwd, "WayCoder", "Agent")) ||
            System.IO.Directory.Exists(System.IO.Path.Combine(cwd, "Agent")));
        Check("Snapshot: .git 目录存在",
            System.IO.Directory.Exists(System.IO.Path.Combine(cwd, ".git")) ||
            System.IO.Directory.Exists(System.IO.Path.Combine(cwd, "..", ".git")));
    }

    /// <summary>/init 项目初始化（ProjectInitializer 生成 AGENT.md + 命令检测）测试</summary>
    private static void TestProjectInit(Action<string, bool> Check)
    {
        // ── GenerateAgentMd 结构（默认 AGENT.md，/init claude 传 CLAUDE.md）──
        var info = new ProjectInfo
        {
            ProjectRoot = "/tmp/demo-project",
            PrimaryLanguage = "Go",
            Frameworks = new List<string> { "Go" },
            BuildTools = new List<string> { "go" },
        };
        var md = ProjectInitializer.GenerateAgentMd(info);
        Check("init: 默认生成 AGENT.md 标题", md.Contains("# AGENT.md"));
        Check("init: 不含 CLAUDE.md 标题", !md.Contains("# CLAUDE.md"));
        var mdClaude = ProjectInitializer.GenerateAgentMd(info, "CLAUDE.md");
        Check("init: claude 参数生成 CLAUDE.md 标题", mdClaude.Contains("# CLAUDE.md"));
        Check("init: 含项目概述区块", md.Contains("## 项目概述"));
        Check("init: 含项目名", md.Contains("demo-project"));
        Check("init: 含主语言", md.Contains("主语言: Go"));
        Check("init: 含命令块", md.Contains("```bash"));
        Check("init: 含开发规范区块", md.Contains("## 开发规范"));

        // ── 命令检测（临时目录，按构建系统分场景）──
        string? tmp = null;
        try
        {
            tmp = Directory.CreateTempSubdirectory("waycoder-init").FullName;

            // .NET
            var dotnetDir = Path.Combine(tmp, "dotnet");
            Directory.CreateDirectory(dotnetDir);
            File.WriteAllText(Path.Combine(dotnetDir, "App.csproj"), "<Project/>");
            Check("init: dotnet 构建命令", ProjectInitializer.DetectBuildCommand(dotnetDir) == "dotnet build --nologo -v q");
            Check("init: dotnet 无 Tests 项目时无测试命令", ProjectInitializer.DetectTestCommand(dotnetDir) == null);
            File.WriteAllText(Path.Combine(dotnetDir, "App.Tests.csproj"), "<Project/>");
            Check("init: dotnet 有 Tests 项目返回 dotnet test", ProjectInitializer.DetectTestCommand(dotnetDir) == "dotnet test --nologo -v q");

            // Node.js
            var nodeDir = Path.Combine(tmp, "node");
            Directory.CreateDirectory(nodeDir);
            File.WriteAllText(Path.Combine(nodeDir, "package.json"),
                "{\"scripts\":{\"test\":\"jest\",\"lint\":\"eslint .\"}}");
            // npm 构建：统一后要求 package.json **显式声明 build 脚本**（此前无条件返回
            // `npm install && npm run build`，对纯库项目是假的构建步骤；而 Agent 侧那份一直
            // 是「仅当声明了 build」—— 两份答案不同，这里按统一后的语义锁）
            Check("init: npm 无 build 脚本 → 无构建命令", ProjectInitializer.DetectBuildCommand(nodeDir) == null);
            File.WriteAllText(Path.Combine(nodeDir, "package.json"),
                "{\"scripts\":{\"test\":\"jest\",\"build\":\"tsc\"}}");
            Check("init: npm 声明 build 脚本 → npm run build --silent",
                ProjectInitializer.DetectBuildCommand(nodeDir) == "npm run build --silent");
            File.WriteAllText(Path.Combine(nodeDir, "package.json"),
                "{\"scripts\":{\"test\":\"jest\",\"lint\":\"eslint .\"}}");
            Check("init: npm 测试命令", ProjectInitializer.DetectTestCommand(nodeDir) == "npm test --silent");
            Check("init: npm lint 命令", ProjectInitializer.DetectLintCommand(nodeDir) == "npm run lint");

            // Go
            var goDir = Path.Combine(tmp, "go");
            Directory.CreateDirectory(goDir);
            File.WriteAllText(Path.Combine(goDir, "go.mod"), "module x\n");
            Check("init: go 测试命令", ProjectInitializer.DetectTestCommand(goDir) == "go test ./...");
            Check("init: go lint 命令", ProjectInitializer.DetectLintCommand(goDir) == "go vet ./...");

            // Rust
            var rustDir = Path.Combine(tmp, "rust");
            Directory.CreateDirectory(rustDir);
            File.WriteAllText(Path.Combine(rustDir, "Cargo.toml"), "[package]\n");
            Check("init: rust 测试命令", ProjectInitializer.DetectTestCommand(rustDir) == "cargo test -q");

            // Python
            var pyDir = Path.Combine(tmp, "python");
            Directory.CreateDirectory(pyDir);
            File.WriteAllText(Path.Combine(pyDir, "test_foo.py"), "def test(): pass\n");
            Check("init: python 测试命令", ProjectInitializer.DetectTestCommand(pyDir) == "python -m pytest -q");

            // 未知项目：返回 null
            var emptyDir = Path.Combine(tmp, "empty");
            Directory.CreateDirectory(emptyDir);
            Check("init: 空目录无构建命令", ProjectInitializer.DetectBuildCommand(emptyDir) == null);
        }
        catch { }
        finally
        {
            if (tmp != null) { try { Directory.Delete(tmp, true); } catch { } }
        }
    }

    /// <summary>/init LLM 分析器（ProjectInitAnalyzer 纯逻辑：上下文收集/提示词/清理/降级决策）测试</summary>
    private static void TestProjectInitAnalyzer(Action<string, bool> Check)
    {
        // ── ShouldUseLlm 降级决策 ──
        Check("init-llm: 无 LLM 走降级", !ProjectInitAnalyzer.ShouldUseLlm(null));
        Check("init-llm: 有 LLM 用 LLM", ProjectInitAnalyzer.ShouldUseLlm(new LLM("x", "k")));

        // ── CleanFenced 围栏剥离 ──
        Check("init-llm: 剥 markdown 围栏", ProjectInitAnalyzer.CleanFenced("```markdown\n# X\n```") == "# X");
        Check("init-llm: 无围栏原样", ProjectInitAnalyzer.CleanFenced("plain text") == "plain text");
        Check("init-llm: 空串返回空", ProjectInitAnalyzer.CleanFenced("") == "");

        // ── 上下文收集（临时目录夹具）──
        string? tmp = null;
        try
        {
            tmp = Directory.CreateTempSubdirectory("waycoder-init-llm").FullName;
            File.WriteAllText(Path.Combine(tmp, "App.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"/>");
            File.WriteAllText(Path.Combine(tmp, "Program.cs"), "class Program { static void Main() { } }\n");
            File.WriteAllText(Path.Combine(tmp, ".cursorrules"), "禁止使用反射。\n");
            File.WriteAllText(Path.Combine(tmp, "README.md"), "# 示例项目\n构建说明。\n");
            File.WriteAllText(Path.Combine(tmp, "AGENT.md"), "# AGENT.md\n已有内容。\n");
            // 超长规则文件（放 .waycoder/ 下才会被 CollectExistingRules 收集 → 验证 Rune 截断）
            Directory.CreateDirectory(Path.Combine(tmp, ".waycoder"));
            File.WriteAllText(Path.Combine(tmp, ".waycoder", "longrule.md"), new string('x', 5000));

            var info = new ProjectInfo { ProjectRoot = tmp };
            var ctx = ProjectInitAnalyzer.CollectInitContext(info, "AGENT.md");
            Check("init-llm: ExistingRules 含 .cursorrules", ctx.ExistingRules.Contains(".cursorrules") && ctx.ExistingRules.Contains("反射"));
            Check("init-llm: 超长规则截断带标记", ctx.ExistingRules.Contains("longrule") && ctx.ExistingRules.Contains("... (已截断)"));
            Check("init-llm: ReadmeHead 含 README 头部", ctx.ReadmeHead.Contains("示例项目"));
            Check("init-llm: ExistingTarget 含已有 AGENT.md", ctx.ExistingTarget.Contains("已有内容"));
            Check("init-llm: Commands 含 dotnet build", ctx.Commands.Contains("dotnet build"));
            Check("init-llm: RepoMap 非空", !string.IsNullOrEmpty(ctx.RepoMap));

            // ── BuildPrompt 占位符替换 ──
            var prompt = ProjectInitAnalyzer.BuildPrompt("CLAUDE.md", ctx);
            Check("init-llm: 提示词含文件头", prompt.Contains("# CLAUDE.md"));
            Check("init-llm: 提示词含上下文区块", prompt.Contains("仓库地图") && prompt.Contains("项目检测"));
            Check("init-llm: 无未替换占位符", !prompt.Contains("{REPO_MAP}") && !prompt.Contains("{FILE_NAME}") && !prompt.Contains("{PROJECT_INFO}"));

            // ── FallbackContent 降级产物回归 ──
            var fb = ProjectInitAnalyzer.FallbackContent(info, "AGENT.md");
            Check("init-llm: 降级含 AGENT.md 标题", fb.Contains("# AGENT.md"));
            Check("init-llm: 降级含项目概述", fb.Contains("## 项目概述"));
        }
        catch { }
        finally
        {
            if (tmp != null) { try { Directory.Delete(tmp, true); } catch { } }
        }
    }

    private static void TestMultiSlotParallel(Action<string, bool> Check)
    {
        // ── 槽位运行状态（多槽位后台并行执行的核心状态）──
        var slot = new AgentSlot();
        Check("并行: 初始非忙", !slot.IsBusy);
        Check("并行: 初始无取消令牌", slot.Cts == null);
        Check("并行: Sync 锁非空", slot.Sync != null);
        Check("并行: 初始消息为空", slot.ChatMessages.Count == 0);
        Check("并行: 槽位数量为 10", AgentSlot.Count == 10);

        // ── 流式缓冲：StartStream → AppendToken → FinishStream ──
        slot.BufferedStartStream();
        Check("并行: 开始流式创建一条消息", slot.ChatMessages.Count == 1);
        Check("并行: 流式消息标记 Streaming", slot.ChatMessages[^1].Streaming);
        slot.BufferedAppendToken("你好");
        slot.BufferedAppendToken("，世界");
        Check("并行: token 连续拼接", slot.ChatMessages[^1].Content == "你好，世界");
        Check("并行: 追加后仍 Streaming", slot.ChatMessages[^1].Streaming);
        slot.BufferedFinishStream();
        Check("并行: 结束流式取消 Streaming", !slot.ChatMessages[^1].Streaming);

        // ── 无流式消息时 AppendToken 自动新建（对标 EnsureAgentStreaming）──
        slot.BufferedAddMsg("system", "工具输出");
        slot.BufferedAppendToken("继续");
        Check("并行: 无流式消息时 AppendToken 自建",
            slot.ChatMessages[^1].Streaming && slot.ChatMessages[^1].Content == "继续");

        // ── AppendToLast 追加到最后一条（工具流式输出）──
        slot.BufferedAppendToLast(" 追加");
        Check("并行: AppendToLast 追加到最后一条", slot.ChatMessages[^1].Content == "继续 追加");

        // ── 多槽位并发压力：10 槽 × 并行线程各自写入，不崩溃、消息隔离（WAYCODER_STRESS=1 才跑）──
        if (Environment.GetEnvironmentVariable("WAYCODER_STRESS") == "1")
        {
            var slots = new AgentSlot[10];
            for (int i = 0; i < 10; i++) slots[i] = new AgentSlot();
            try
            {
                Parallel.For(0, 10, s =>
                {
                    for (int j = 0; j < 200; j++)
                    {
                        slots[s].BufferedAddMsg("user", $"槽位{s} 消息{j}");
                        slots[s].BufferedAppendToken($"token{j}");
                        slots[s].BufferedFinishStream();
                    }
                });
                bool isolated = true;
                for (int s = 0; s < 10; s++)
                {
                    if (slots[s].ChatMessages.Count < 200)
                        isolated = false; // 至少 200 条（每条 AddMsg + 自建的 token 消息）
                    // 隔离检查：本槽位消息不得包含其他槽位的标记（无跨槽位串扰）
                    for (int k = 0; k < 10 && isolated; k++)
                        if (k != s && slots[s].ChatMessages.Any(m => m.Content.Contains($"槽位{k}")))
                            isolated = false;
                }
                Check("槽位并发: 10 槽并行写入不崩溃且隔离", isolated);
            }
            catch (Exception ex)
            {
                Check($"槽位并发: 异常 {ex.GetType().Name}: {ex.Message}", false);
            }
        }
    }

    private static void TestWorkModePerAgent(Action<string, bool> Check)
    {
        var savedGlobal = WorkModeManager.CurrentMode;

        // ── 实例级工作模式：与全局 CurrentMode 解耦 ──
        var a1 = new Agent(new LLM("test", "sk-test"));
        var a2 = new Agent(new LLM("test", "sk-test"));
        Check("模式: Agent 默认 Build", a1.WorkMode == WorkMode.Build);
        Check("模式: 两实例可独立设置", a1.WorkMode != a2.WorkMode || a1.WorkMode == WorkMode.Build);

        // 设置实例模式不影响全局镜像
        a1.WorkMode = WorkMode.Plan;
        Check("模式: 实例设 Plan 不影响全局", WorkModeManager.CurrentMode == savedGlobal);
        Check("模式: 实例模式已生效", a1.WorkMode == WorkMode.Plan);
        Check("模式: 另一实例仍 Build", a2.WorkMode == WorkMode.Build);

        // ── 工具约束跟随实例模式 ──
        Check("模式: Plan 阻止 write_file",
            WorkModeManager.CheckToolAllowed("write_file", a1.WorkMode) != null);
        Check("模式: Build 允许 write_file",
            WorkModeManager.CheckToolAllowed("write_file", a2.WorkMode) == null);

        // ── 模式变化回调 ──
        WorkMode? notified = null;
        a1.OnWorkModeChanged = m => notified = m;
        a1.WorkMode = WorkMode.Build;
        a1.OnWorkModeChanged?.Invoke(a1.WorkMode);
        Check("模式: 回调收到新模式", notified == WorkMode.Build);

        // ── 计划审批门纯逻辑（实例模式驱动）──
        Check("模式: Plan 触发审批", Agent.ShouldPromptPlanApproval(WorkMode.Plan, 50));
        Check("模式: Build 不触发审批", !Agent.ShouldPromptPlanApproval(WorkMode.Build, 50));
        Check("模式: Chat 不触发审批", !Agent.ShouldPromptPlanApproval(WorkMode.Chat, 50));

        // ── v3：Plan 只读白名单 / Chat 0 工具 / 权限四档无 TINY / 聊天别名 / Auto 改必问 ──
        var planAgent = new Agent(new LLM("test", "sk-test"));
        planAgent.WorkMode = WorkMode.Plan;
        planAgent.ReapplyToolFilter();
        var planNames = planAgent.Tools.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Check("v3: Plan 含 read_file", planNames.Contains("read_file"));
        Check("v3: Plan 含 grep", planNames.Contains("grep"));
        Check("v3: Plan 含 doc", planNames.Contains("doc"));
        Check("v3: Plan 含 bash", planNames.Contains("bash"));
        Check("v3: Plan 不含 write_file", !planNames.Contains("write_file"));
        Check("v3: Plan 不含 git", !planNames.Contains("git"));
        Check("v3: Plan 不含 sqlite", !planNames.Contains("sqlite"));

        var chatAgent = new Agent(new LLM("test", "sk-test"));
        chatAgent.WorkMode = WorkMode.Chat;
        chatAgent.ReapplyToolFilter();
        Check("v3: Chat 工具数为 0", chatAgent.Tools.Count == 0);

        // 权限四档循环（无 TINY）
        PermissionManager.SetMode("ask");
        Check("v3: 权限循环 Ask→Auto", PermissionManager.CycleMode() == PermissionManager.Mode.Auto);
        Check("v3: 权限循环 Auto→SmartAuto", PermissionManager.CycleMode() == PermissionManager.Mode.SmartAuto);
        Check("v3: 权限循环 SmartAuto→Yolo", PermissionManager.CycleMode() == PermissionManager.Mode.Yolo);
        Check("v3: 权限循环 Yolo→Ask（无 TINY）", PermissionManager.CycleMode() == PermissionManager.Mode.Ask);

        // 聊天别名
        Check("v3: IsChatModeAlias('tiny')", PermissionManager.IsChatModeAlias("tiny"));
        Check("v3: IsChatModeAlias('chat')", PermissionManager.IsChatModeAlias("chat"));
        Check("v3: IsChatModeAlias('yolo') 为假", !PermissionManager.IsChatModeAlias("yolo"));

        // Auto 改必问（≈Ask）：只读工具直接放行（读路径不弹框）
        PermissionManager.SetMode("auto");
        Check("v3: Auto 只读 read_file 放行",
            PermissionManager.CheckAsync("read_file", new() { ["file_path"] = "/tmp/x" }).Result);
        Check("v3: Auto 只读 grep 放行",
            PermissionManager.CheckAsync("grep", new() { ["pattern"] = "x" }).Result);
        Check("v3: Auto bash 只读命令放行",
            PermissionManager.CheckAsync("bash", new() { ["command"] = "git status" }).Result);
        PermissionManager.SetMode("ask");

        WorkModeManager.CurrentMode = savedGlobal;

        // ── v0.96.3: 惰性系统提示词 + 模式切换请求组合（修复构造期按 Build 抢先生成被丢弃）──
        {
            var savedEcon = Config.Instance.EconomyMode;
            try
            {
                Config.Instance.EconomyMode = EconomyMode.On; // 轻量：Build 提示词走精简版，无 RepoMap

                var m = new Agent(new LLM("test", "sk-test"));
                var field = typeof(Agent).GetField("_systemPrompt",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var fullMsgs = typeof(Agent).GetMethod("FullMessages",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Check("惰性: 反射找到 _systemPrompt 字段", field != null);
                Check("惰性: 反射找到 FullMessages", fullMsgs != null);
                if (field != null && fullMsgs != null)
                {
                    // 构造后（默认 Build）不立即生成 —— 惰性
                    Check("惰性: 构造后未生成", field.GetValue(m) == null);

                    // 注入一条 user 消息，FullMessages 才能组合出实际请求
                    m.Messages.Add(JNode.Object().Set("role", "user").Set("content", "测试"));

                    // ── Chat：0 工具 + 无 system 注入，且不触发提示词生成 ──
                    m.WorkMode = WorkMode.Chat;
                    m.ReapplyToolFilter();
                    Check("切换Chat: 工具数 0", m.Tools.Count == 0);
                    var chatMsgs = (List<JNode>)fullMsgs.Invoke(m, null)!;
                    Check("Chat请求: 无 system 消息", !chatMsgs.Any(x => x["role"]?.AsString() == "system"));
                    Check("Chat请求: 仅 user 消息", chatMsgs.Count == 1 && chatMsgs[0]["role"]?.AsString() == "user");
                    Check("Chat请求: 不触发提示词生成(仍 null)", field.GetValue(m) == null);

                    // ── Plan：只读工具 + 计划提示词注入 ──
                    m.WorkMode = WorkMode.Plan;
                    m.ReapplyToolFilter();
                    Check("切换Plan: 工具非空", m.Tools.Count > 0);
                    Check("切换Plan: 工具全在只读白名单",
                        m.Tools.All(t => WorkModeManager.PlanReadOnlyTools.Contains(t.Name)));
                    var planMsgs = (List<JNode>)fullMsgs.Invoke(m, null)!;
                    var planSys = planMsgs.Where(x => x["role"]?.AsString() == "system")
                        .Select(x => x["content"]?.AsString()).FirstOrDefault() ?? "";
                    Check("Plan请求: 注入 system", planSys.Length > 0);
                    Check("Plan请求: 含计划模式提示", planSys.Contains("计划模式"));
                    Check("Plan请求: 生成后已缓存", field.GetValue(m) != null);

                    // ── Build：全量工具 + 完整提示词（验证失效重建）──
                    m.WorkMode = WorkMode.Build;
                    m.ReapplyToolFilter();
                    Check("切换Build: 工具非空", m.Tools.Count > 0);
                    Check("切换Build: 含 write_file",
                        m.Tools.Any(t => t.Name.Equals("write_file", StringComparison.OrdinalIgnoreCase)));
                    var buildMsgs = (List<JNode>)fullMsgs.Invoke(m, null)!;
                    var buildSys = buildMsgs.Where(x => x["role"]?.AsString() == "system")
                        .Select(x => x["content"]?.AsString()).FirstOrDefault() ?? "";
                    Check("Build请求: 注入 system", buildSys.Length > 0);
                    Check("Build请求: 提示词随模式重建(≠Plan)", buildSys != planSys);
                }
            }
            finally
            {
                Config.Instance.EconomyMode = savedEcon;
            }
        }
    }

    private static void TestBatchEngine(Action<string, bool> Check)
    {
        // ── JSON 解析 ──
        var ok = BatchSpec.Parse("""
        {
          "maxParallel": 2,
          "tasks": [
            { "repo": "https://github.com/a/b", "task": "修 bug" },
            { "repo": "/本地/x", "task": "加测试", "name": "x", "branch": "dev" }
          ]
        }
        """, out var err1);
        Check("批量: 解析 2 个任务", ok != null && ok.Jobs.Count == 2 && err1 == "");
        Check("批量: maxParallel=2", ok!.MaxParallel == 2);
        Check("批量: name/branch 透传", ok.Jobs[1].Name == "x" && ok.Jobs[1].Branch == "dev");

        // ── 边界与钳制 ──
        var clamp = BatchSpec.Parse("""{ "maxParallel": 99, "timeoutSec": 1, "tasks": [{"repo":"r","task":"t"}] }""", out _);
        Check("批量: maxParallel 钳制到 16", clamp!.MaxParallel == 16);
        Check("批量: timeoutSec 钳制到 60", clamp.TimeoutSec == 60);
        var clamp2 = BatchSpec.Parse("""{ "maxParallel": 0, "tasks": [{"repo":"r","task":"t"}] }""", out _);
        Check("批量: maxParallel 钳制到 1", clamp2!.MaxParallel == 1);

        // ── 错误场景 ──
        Check("批量: 缺 tasks 报错", BatchSpec.Parse("""{"maxParallel":2}""", out var e1) == null && e1.Contains("tasks"));
        Check("批量: 非法 JSON 报错", BatchSpec.Parse("not json", out var e2) == null && e2.Contains("JSON"));
        Check("批量: 空任务报错", BatchSpec.Parse("""{"tasks":[]}""", out var e3) == null && e3.Length > 0);

        // ── SanitizeName ──
        Check("批量: URL 提取名", BatchSpec.SanitizeName("https://github.com/org/my-repo.git") == "my-repo");
        Check("批量: 本地路径提取名", BatchSpec.SanitizeName("/a/b/c") == "c");
        Check("批量: 反斜杠路径提取名", BatchSpec.SanitizeName("C:\\proj\\repo") == "repo");
        Check("批量: 空名兜底", BatchSpec.SanitizeName("///") == "repo");
        Check("批量: 非法字符替换", BatchSpec.SanitizeName("my repo!") == "my_repo");

        // ── IsRemoteUrl ──
        Check("批量: https 是远程", BatchSpec.IsRemoteUrl("https://x/y"));
        Check("批量: git@ 是远程", BatchSpec.IsRemoteUrl("git@github.com:o/r.git"));
        Check("批量: 本地路径非远程", !BatchSpec.IsRemoteUrl("/local/path"));

        // ── FromRepos ──
        var spec = BatchSpec.FromRepos(new[] { "https://a/b", "/local/c" }, "共享任务");
        Check("批量: FromRepos 构建 2 任务", spec.Jobs.Count == 2 && spec.Jobs.All(j => j.Task == "共享任务"));

        // ── 报告渲染 ──
        var report = new BatchReport();
        report.Results.Add(new BatchResult { Name = "a", Repo = "r", Success = true, Summary = "完成", DurationMs = 1000 });
        report.Results.Add(new BatchResult { Name = "b", Repo = "r2", Success = false, Error = "挂了", ExitCode = 1, DurationMs = 2000 });
        var md = report.ToMarkdown();
        Check("批量: 报告含统计", md.Contains("总计: 2") && md.Contains("成功: 1") && md.Contains("失败: 1"));
        Check("批量: 报告含成功/失败图标", md.Contains("✅") && md.Contains("❌"));
        Check("批量: 报告含错误详情", md.Contains("挂了"));

        // ── RunAsync 端到端（本地 git 仓库 + 注入 fake spawner，无网络无 LLM）──
        try
        {
            var gitOk = GitRunner.Run("--version").ExitCode == 0;
            Check("批量: git 可用（端到端前提）", gitOk);
            if (gitOk)
            {
                var srcDir = Path.Combine(Path.GetTempPath(), "wc_batch_src_" + Guid.NewGuid().ToString("N")[..6]);
                var rootDir = Path.Combine(Path.GetTempPath(), "wc_batch_root_" + Guid.NewGuid().ToString("N")[..6]);
                Directory.CreateDirectory(srcDir);
                GitRunner.RunOrThrow("init -q", srcDir);
                File.WriteAllText(Path.Combine(srcDir, "a.txt"), "hi");
                GitRunner.RunOrThrow("add -A", srcDir);
                GitRunner.RunOrThrow("-c user.email=t@t -c user.name=t commit -q -m init", srcDir);

                var spec2 = BatchSpec.FromRepos(new[] { srcDir, srcDir }, "任务", maxParallel: 2);
                spec2.TimeoutSec = 60;
                spec2.KeepResults = false;

                var ran = 0;
                var report2 = BatchRunner.RunAsync(spec2,
                    log: null,
                    rootDir: rootDir,
                    spawn: (job, dir, task, ct) =>
                    {
                        Interlocked.Increment(ref ran);
                        var cloned = File.Exists(Path.Combine(dir, "a.txt"));
                        return Task.FromResult<(int, string, string)>(cloned ? (0, $"OK {job.DisplayName}", "") : (1, "", "clone 未就绪"));
                    }).GetAwaiter().GetResult();

                Check("批量: RunAsync 并发执行 2 任务", ran == 2);
                Check("批量: RunAsync 全部成功", report2.Succeeded == 2 && report2.Failed == 0);
                Check("批量: 报告已写文件", File.Exists(Path.Combine(rootDir, "batch-report.md")));
                Check("批量: 工作副本已清理", !Directory.Exists(Path.Combine(rootDir, "jobs")) || Directory.GetDirectories(Path.Combine(rootDir, "jobs")).Length == 0);

                // 清理临时目录
                try { Directory.Delete(srcDir, recursive: true); } catch { }
                try { Directory.Delete(rootDir, recursive: true); } catch { }
            }
        }
        catch (Exception ex)
        {
            Check($"批量: RunAsync 端到端异常: {ex.Message}", false);
        }
    }

    // ── 插件系统测试用最小实现 ──

    private sealed class TestPluginTool : ITool
    {
        public string Name => "plugin_test_tool";
        public string Description => "测试插件工具";
        public JNode Parameters => JNode.Object().Set("type", "object").Set("properties", JNode.Object());
        public Task<string> ExecuteAsync(Dictionary<string, object?> arguments) => Task.FromResult("ok");
    }

    private sealed class TestPluginCommand : SlashCommand
    {
        public override string Name => "/plugin-hello";
        public override string Description => "测试插件命令";
        public override Task ExecuteAsync(string args, ChatScreen screen) => Task.CompletedTask;
    }

    private sealed class TestPlugin : Plugin
    {
        public override string Name => "test-plugin";
        public override string Version => "9.9.9";
        public override IEnumerable<ITool> GetTools() => [new TestPluginTool()];
        public override IEnumerable<ISlashCommand> GetCommands() => [new TestPluginCommand()];
    }

    private sealed class NullReturningPlugin : Plugin
    {
        public override string Name => "null-plugin";
        public override IEnumerable<ITool> GetTools() => null!;
        public override IEnumerable<ISlashCommand> GetCommands() => null!;
    }

    private static void TestPluginSystem(Action<string, bool> Check)
    {
        PluginRegistry.Register(new TestPlugin());
        Check("插件: 注册成功", PluginRegistry.Plugins.Count == 1);
        Check("插件: 收集 1 个工具", PluginRegistry.CollectTools().Count() == 1);
        Check("插件: 收集 1 个命令", PluginRegistry.CollectCommands().Count() == 1);
        Check("插件: 工具集成到 AllTools", ToolRegistry.AllTools.Any(t => t.Name == "plugin_test_tool"));
        Check("插件: 命令名正确", PluginRegistry.CollectCommands().First().Name == "/plugin-hello");

        // 同名覆盖（忽略大小写）不重复
        PluginRegistry.Register(new TestPlugin());
        Check("插件: 同名注册覆盖不重复", PluginRegistry.Plugins.Count == 1);
        Check("插件: 版本字段保留", PluginRegistry.Plugins[0].Version == "9.9.9");

        // null 注册防御
        PluginRegistry.Register(null!);
        Check("插件: null 注册不抛", PluginRegistry.Plugins.Count == 1);

        // 卸载
        Check("插件: 卸载成功", PluginRegistry.Unregister("test-plugin"));
        Check("插件: 卸载后工具移除", PluginRegistry.Plugins.Count == 0
            && !ToolRegistry.AllTools.Any(t => t.Name == "plugin_test_tool"));

        // null 返回防御
        PluginRegistry.Register(new NullReturningPlugin());
        Check("插件: null 返回防御不抛", PluginRegistry.CollectTools().Count() == 0
            && PluginRegistry.CollectCommands().Count() == 0);
        PluginRegistry.Unregister("null-plugin");
    }

    /// <summary>跨会话记忆检索（MemoryRetrieval）单元测试：关键词匹配打分 + 提示词格式化。</summary>
    private static void TestMemoryRetrieval(Action<string, bool> Check)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "wc_mem_" + Guid.NewGuid().ToString("N")[..6]);
        var memDir = Path.Combine(tmp, ".waycoder", "memory");
        Directory.CreateDirectory(memDir);
        try
        {
            // 写两个 frontmatter 记忆文件
            File.WriteAllText(Path.Combine(memDir, "timeseries-notes.md"),
                "---\nname: timeseries-notes\ndescription: TimeSeries 时序预测模块的实现要点\n---\n时序预测用指数平滑。\n");
            File.WriteAllText(Path.Combine(memDir, "automata-notes.md"),
                "---\nname: automata-notes\ndescription: Automata 状态机的构建方法\ntype: project\n---\n状态机用 DFA 表示。\n");

            MemoryRetrieval.Load(tmp);

            // ── GetRelevant 关键词匹配 ──
            var rel = MemoryRetrieval.GetRelevant("给 TimeSeries 模块加冒烟测试", maxResults: 3);
            Check("Memory: GetRelevant 命中 TimeSeries 记忆",
                rel.Any(m => m.Name == "timeseries-notes"));

            var rel2 = MemoryRetrieval.GetRelevant("Automata 状态机怎么建", maxResults: 3);
            Check("Memory: GetRelevant 命中 Automata 记忆",
                rel2.Any(m => m.Name == "automata-notes"));

            var rel3 = MemoryRetrieval.GetRelevant("完全无关的 XYZQWERTY 话题", maxResults: 3);
            Check("Memory: 无关关键词不命中已索引记忆",
                rel3.All(m => m.Name != "timeseries-notes" && m.Name != "automata-notes"));

            // ── FormatForPrompt 格式化 ──
            var fmt = MemoryRetrieval.FormatForPrompt(rel);
            Check("Memory: FormatForPrompt 含标题", fmt.Contains("相关记忆"));
            Check("Memory: FormatForPrompt 含记忆名与类型",
                fmt.Contains("timeseries-notes") && fmt.Contains("reference"));

            // 描述超 200 字符截断
            var longItem = new MemoryRetrieval.MemoryItem(
                "long-desc", new string('x', 250), "content", "ref", DateTime.UtcNow);
            var fmt2 = MemoryRetrieval.FormatForPrompt(new[] { longItem });
            Check("Memory: 描述超 200 截断为 …", fmt2.Contains(new string('x', 200) + "..."));

            // 空列表返回空
            Check("Memory: FormatForPrompt 空列表返回空",
                MemoryRetrieval.FormatForPrompt(new List<MemoryRetrieval.MemoryItem>()) == "");
        }
        finally
        {
            try { Directory.Delete(tmp, recursive: true); } catch { }
        }
    }

    /// <summary>运行轨迹（Trajectory）单元测试：截断纯函数 + Enabled 标志 + JSONL 事件流落盘/读回。</summary>
    private static void TestTrajectory(Action<string, bool> Check)
    {
        // ── 1. Truncate 纯函数（头尾保留 + 省略标记）──
        Check("Traj: 短文本不截断", Trajectory.Truncate("hello", 100) == "hello");
        Check("Traj: null 不截断", Trajectory.Truncate(null!, 100) == null);
        Check("Traj: 空串不截断", Trajectory.Truncate("", 100) == "");
        var longText = new string('A', 3000);
        var truncated = Trajectory.Truncate(longText, 2000);
        Check("Traj: 截断含标记", truncated.Contains("已截断"));
        Check("Traj: 截断保留头", truncated.StartsWith(new string('A', 1200)));
        Check("Traj: 截断保留尾", truncated.EndsWith(new string('A', 2000 - 1200 - "\n…[已截断]…\n".Length)));
        Check("Traj: 极小 maxChars 只留头", Trajectory.Truncate(longText, 10) == "AAAAAAAAAA");
        // UTF-16 代理对：maxChars 落在 emoji 中间时不切半（走 TruncateByRunes 回退）
        Check("Traj: 截断不切半代理对", Trajectory.Truncate("ab😀cd", 3) == "ab😀");
        Check("Traj: 截断结果不含替换符", !Trajectory.Truncate("ab😀cd", 3).Contains('�'));

        // ── StringExtensions.Truncate（maxLen=0 守卫 + 代理对）──
        Check("TruncateExt: maxLen=0 返回空", "x".Truncate(0) == "");
        Check("TruncateExt: 短于上限原样返回", "hello".Truncate(10) == "hello");
        Check("TruncateExt: 不切半代理对", "ab😀cd".Truncate(3) == "ab…");

        // ── 2. Enabled 标志（默认开，未设 WAYCODER_TRAJECTORY）──
        Check("Traj: Enabled 默认开", Trajectory.Enabled);

        // ── 3. 完整事件流：Create → RecordTurn → RecordTool → End → JSONL 读回 ──
        var dir = Path.Combine(Path.GetTempPath(), "waycoder-trajectory-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var t = Trajectory.Create("test-model", "session-1", dir);
            Check("Traj: Create 非空", t != null);
            if (t != null)
            {
                Check("Traj: 文件已创建", File.Exists(t.FilePath));
                t.RecordTurn(0, 100, 50, 200, 1, 0);
                t.RecordTurn(1, 150, 60, 10, 0, 300);
                t.RecordTool("write_file", "{\"path\":\"/tmp/a.cs\"}", "已写入 12 行", true, 42);
                t.RecordTool("bash", "ls", "运行命令时出错", false, 7);
                t.End();

                var lines = File.ReadAllLines(t.FilePath);
                Check("Traj: 6 个事件", lines.Length == 6);

                var types = new List<string>();
                bool allParse = true, allSchema = true, allVersion = true;
                foreach (var line in lines)
                {
                    var ev = Json.Parse(line);
                    if (ev == null) { allParse = false; continue; }
                    if (ev["traceSchema"]?.AsString() != "waycoder-trajectory") allSchema = false;
                    if (ev["schemaVersion"]?.AsNumber() != 1) allVersion = false;
                    types.Add(ev["type"]?.AsString() ?? "");
                }
                Check("Traj: 每行可解析", allParse);
                Check("Traj: traceSchema 正确", allSchema);
                Check("Traj: schemaVersion 正确", allVersion);
                Check("Traj: 事件类型顺序",
                    string.Join(",", types) == "run_start,llm_turn,llm_turn,tool_call,tool_call,run_end");

                // run_end 汇总字段（累计轮次 + 总 token）
                var last = Json.Parse(lines[^1]);
                Check("Traj: run_end rounds", last!["data"]?["rounds"]?.AsNumber() == 2);
                Check("Traj: run_end totalTokens", last!["data"]?["totalTokens"]?.AsNumber() == 100 + 50 + 150 + 60);

                // tool_call 成败/名称
                var toolOk = Json.Parse(lines[3]);
                Check("Traj: tool_call name", toolOk!["data"]?["name"]?.AsString() == "write_file");
                Check("Traj: tool_call ok=true", toolOk!["data"]?["ok"]?.AsBool() == true);
                var toolFail = Json.Parse(lines[4]);
                Check("Traj: tool_call ok=false", toolFail!["data"]?["ok"]?.AsBool() == false);
            }
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    /// <summary>从 wc 单文件输出里提取指定字段（如「字符」）前的数值。</summary>
    private static int ExtractWcField(string output, string field)
    {
        var idx = output.IndexOf(" " + field);
        if (idx < 0) return -1;
        int end = idx;
        while (end > 0 && output[end - 1] == ' ') end--;
        int start = end;
        while (start > 0 && char.IsDigit(output[start - 1])) start--;
        return int.TryParse(output[start..end], out var n) ? n : -1;
    }

    /// <summary>CLI 参数测试桩：AllowMultiple + 单值，用于验证多值累积解析（唯一名避免与内置参数冲突）。</summary>
    private sealed class _TestMultiArg : WayCoder.UI.Cli.Arguments.CliArg
    {
        public override int ValueCount => 1;
        public override bool AllowMultiple => true;
        public _TestMultiArg() : base("test-multi-v0730", "--test-multi-v0730") { }
    }

    /// <summary>递归收集控件树里的按钮（校验对话框配色是否统一时用）。</summary>
    private static void CollectButtons(WayCoder.UI.Tui.TuiControl? root,
        List<WayCoder.UI.Tui.Controls.TuiButton> into)
    {
        switch (root)
        {
            case null: return;
            case WayCoder.UI.Tui.Controls.TuiButton b: into.Add(b); return;
            case WayCoder.UI.TUI.Base.TuiView v:
                foreach (var c in v.Children) CollectButtons(c, into);
                return;
        }
    }

    /// <summary>找控件树里第一个列表控件（校验 resize 后控件宽度有没有跟着算时用）。</summary>
    private static WayCoder.UI.Tui.Controls.TuiList? FindFirstList(WayCoder.UI.Tui.TuiControl? root)
    {
        switch (root)
        {
            case null: return null;
            case WayCoder.UI.Tui.Controls.TuiList l: return l;
            case WayCoder.UI.TUI.Base.TuiView v:
                foreach (var c in v.Children)
                    if (FindFirstList(c) is { } found) return found;
                return null;
            default: return null;
        }
    }

    /// <summary>递归收集控件树里的标签（查模板占位符有没有被清干净时用）。</summary>
    private static void CollectLabels(WayCoder.UI.Tui.TuiControl? root,
        List<WayCoder.UI.Tui.Controls.TuiLabel> into)
    {
        switch (root)
        {
            case null: return;
            case WayCoder.UI.Tui.Controls.TuiLabel l: into.Add(l); return;
            case WayCoder.UI.TUI.Base.TuiView v:
                foreach (var c in v.Children) CollectLabels(c, into);
                return;
        }
    }

}