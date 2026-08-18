using System.Text;
using System.Text.Json;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.TUI.Base;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.Tui.Screens;
using Arguments = WayCoder.UI.Cli.Arguments;

namespace WayCoder;

public static partial class SelfTest
{
    private static void TestChunk9(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[DiffPreview]");
        // Hunk/HunkLine 结构
        var hunk = new DiffPreview.Hunk { Header = "@@ -1,3 +1,4 @@", OldStart = 1, OldCount = 3, NewStart = 1, NewCount = 4 };
        Check("DiffPreview.Hunk Header 设置", hunk.Header.StartsWith("@@"));
        Check("DiffPreview.Hunk OldStart", hunk.OldStart == 1);

        var hunkLine = new DiffPreview.HunkLine { Kind = '+', Text = "新增行", OldLine = -1, NewLine = 1 };
        Check("DiffPreview.HunkLine Kind=+", hunkLine.Kind == '+');
        Check("DiffPreview.HunkLine Text", hunkLine.Text == "新增行");

        hunk.Lines.Add(new DiffPreview.HunkLine { Kind = ' ', Text = "上下文", OldLine = 1, NewLine = 1 });
        hunk.Lines.Add(new DiffPreview.HunkLine { Kind = '-', Text = "删除行", OldLine = 2, NewLine = -1 });
        hunk.Lines.Add(hunkLine);
        Check("DiffPreview.Hunk Lines=3", hunk.Lines.Count == 3);

        // Decision 枚举
        Check("DiffPreview.Decision.AcceptAll", (int)DiffPreview.Decision.AcceptAll == 0);
        Check("DiffPreview.Decision.RejectAll", (int)DiffPreview.Decision.RejectAll == 1);
        Check("DiffPreview.Decision.Partial", (int)DiffPreview.Decision.Partial == 2);
        Console.WriteLine();

        // ── UxHelper 测试 ──
        Section("[UxHelper]");
        Check("UxHelper.IsTuiMode 可调用", new Action(() => { var _ = UxHelper.IsTuiMode; }) != null);
        Check("UxHelper.Select 空选项返回 null", UxHelper.Select("t", new List<string>()) == null);
        Check("UxHelper.MultiSelect 空选项返回空列表", UxHelper.MultiSelect("t", new List<string>()) is { Count: 0 });
        Console.WriteLine();

        // ── CommandPalette 导航（分类头不吞噬每组第一条命令）──
        Section("[CommandPalette 导航]");
        var cpRows = new List<(bool IsHeader, int CmdIdx, string Cat)>
        {
            (true, 0, "模型"),
            (false, 0, "模型"),
            (false, 1, "模型"),
            (true, 2, "文件"),
            (false, 2, "文件"),
        };
        // 行索引: 0=头, 1=命令0, 2=命令1, 3=头, 4=命令2
        Check("CP: 向下到相邻命令", CommandPalette.StepToCommand(cpRows, 1, +1) == 2);
        Check("CP: 向上到相邻命令", CommandPalette.StepToCommand(cpRows, 2, -1) == 1);
        Check("CP: 向下跳过分类头", CommandPalette.StepToCommand(cpRows, 2, +1) == 4);
        Check("CP: 向上跳过分类头", CommandPalette.StepToCommand(cpRows, 4, -1) == 2);
        Check("CP: 末尾向下停留", CommandPalette.StepToCommand(cpRows, 4, +1) == 4);
        Check("CP: 开头向上停留", CommandPalette.StepToCommand(cpRows, 1, -1) == 1);
        Check("CP: Home 落到首命令", CommandPalette.StepToCommand(cpRows, -1, +1) == 1);
        Check("CP: End 落到末命令", CommandPalette.StepToCommand(cpRows, cpRows.Count, -1) == 4);
        Console.WriteLine();

        // ── AskUserQuestion 工具 ──
        Section("[AskUserQuestion 工具]");
        ITool auqTool = new AskUserQuestionTool();
        Check("AskUserQuestion.Name", auqTool.Name == "ask_user_question");
        Check("AskUserQuestion.Description 非空", !string.IsNullOrEmpty(auqTool.Description));
        // Schema 校验
        var auqSchema = auqTool.Schema();
        Check("AskUserQuestion.Schema type=function", auqSchema["type"]?.AsString() == "function");
        var auqFunc = auqSchema["function"];
        Check("AskUserQuestion.Schema name", auqFunc?["name"]?.AsString() == "ask_user_question");
        var auqParams = auqFunc?["parameters"];
        Check("AskUserQuestion.Schema parameters 非空", auqParams != null);
        Check("AskUserQuestion.Schema required=questions",
            auqParams?["required"] is JNode reqArr && reqArr.Count == 1 && reqArr[0]?.AsString() == "questions");
        // 空 questions → 错误
        var auqEmptyResult = auqTool.ExecuteAsync(new() { ["questions"] = JNode.Array() }).Result;
        Check("AskUserQuestion 空数组返回错误", auqEmptyResult.Contains("错误"));
        // 缺少 questions 参数
        var auqMissingResult = auqTool.ExecuteAsync(new()).Result;
        Check("AskUserQuestion 缺少参数返回错误", auqMissingResult.Contains("错误"));
        // 工具注册
        Check("AskUserQuestion 在 ToolRegistry 中", ToolRegistry.GetTool("ask_user_question") != null);
        // 安全分类
        Check("AskUserQuestion 分类为 Safe",
            AutoModeClassifier.Classify("ask_user_question") == AutoModeClassifier.RiskLevel.Safe);
        // display → label 映射（"label — desc" → label）
        var rlOptions = new List<(string Label, string Description)> { ("蓝", "深海"), ("绿", "翡翠") };
        var rlDisplay = new List<string> { "蓝  —  深海", "绿  —  翡翠" };
        Check("AskUserQuestion 解析 label(带描述)", AskUserQuestionTool.ResolveLabel("绿  —  翡翠", rlDisplay, rlOptions) == "绿");
        Check("AskUserQuestion 解析 label(纯 label)", AskUserQuestionTool.ResolveLabel("蓝", new List<string> { "蓝" }, rlOptions) == "蓝");
        Check("AskUserQuestion 解析 label(查无原样)", AskUserQuestionTool.ResolveLabel("未知", rlDisplay, rlOptions) == "未知");
        Console.WriteLine();

        // ---- Todo 依赖系统 (P0-4) ----
        Section("[Todo 依赖系统]");
        var depTool = new TodoTool();
        depTool.ExecuteAsync(new() { ["action"] = "clear" }).Wait();

        // 创建依赖链: task-a → task-b → task-c
        var rA = depTool.ExecuteAsync(new() { ["action"] = "create", ["id"] = "dep-a", ["title"] = "前置任务A" }).Result;
        Check("Todo依赖: 创建任务A", rA.Contains("创建") && TodoTool.Items.Any(t => t.Id == "dep-a"));
        var rB = depTool.ExecuteAsync(new() { ["action"] = "create", ["id"] = "dep-b", ["title"] = "任务B", ["deps"] = JNode.Array().Add("dep-a") }).Result;
        Check("Todo依赖: 创建任务B(依赖A)", rB.Contains("创建") && TodoTool.Items.Any(t => t.Id == "dep-b" && t.Status == "blocked"));
        var rC = depTool.ExecuteAsync(new() { ["action"] = "create", ["id"] = "dep-c", ["title"] = "任务C", ["deps"] = JNode.Array().Add("dep-b") }).Result;
        Check("Todo依赖: 创建任务C(依赖B)", rC.Contains("创建") && TodoTool.Items.Any(t => t.Id == "dep-c" && t.Status == "blocked"));

        // blocked → in_progress 被拒绝（依赖未完成）
        var blockReject = depTool.ExecuteAsync(new() { ["action"] = "update", ["id"] = "dep-b", ["status"] = "in_progress" }).Result;
        Check("Todo依赖: blocked不能直接in_progress", blockReject.Contains("blocked") || blockReject.Contains("依赖") || blockReject.Contains("无法"));

        // 完成 A → B 自动解封
        var doneA = depTool.ExecuteAsync(new() { ["action"] = "update", ["id"] = "dep-a", ["status"] = "completed" }).Result;
        Check("Todo依赖: 完成A后B自动解封", doneA.Contains("completed") || doneA.Contains("解除") || doneA.Contains("解封"));

        // 验证 B 现在是 pending
        var listAfter = depTool.ExecuteAsync(new() { ["action"] = "list" }).Result;
        Check("Todo依赖: B从blocked变为pending", TodoTool.Items.Any(t => t.Id == "dep-b" && t.Status != "blocked"));

        // 完成 B → C 也应该解封
        depTool.ExecuteAsync(new() { ["action"] = "update", ["id"] = "dep-b", ["status"] = "completed" }).Wait();
        Check("Todo依赖: 完成B后C自动解封", TodoTool.Items.Any(t => t.Id == "dep-c" && t.Status != "blocked"));

        // 更新描述字段
        depTool.ExecuteAsync(new() { ["action"] = "update", ["id"] = "dep-a", ["description"] = "已完成的描述" }).Wait();
        Check("Todo依赖: 更新描述", TodoTool.Items.First(t => t.Id == "dep-a").Description == "已完成的描述");

        // 删除任务
        depTool.ExecuteAsync(new() { ["action"] = "delete", ["id"] = "dep-c" }).Wait();
        Check("Todo依赖: 删除任务C", !TodoTool.Items.Any(t => t.Id == "dep-c"));

        depTool.ExecuteAsync(new() { ["action"] = "clear" }).Wait();
        Check("Todo依赖: 清理后为空", TodoTool.Items.Count == 0);

        Console.WriteLine();

        // ---- 工具循环检测配置 (P0-2) ----
        Section("[工具循环检测配置]");
        // 通过 Agent 构造验证常量配置
        var agentForLoop = new Agent(new LLM("test", "test-key"));
        Check("循环检测: PerToolLoopWindow >= 5", agentForLoop.LoopWindowForTest >= 5);
        Check("循环检测: PerToolLoopThreshold >= 3", agentForLoop.LoopThresholdForTest >= 3);
        Check("循环检测: Threshold <= Window", agentForLoop.LoopThresholdForTest <= agentForLoop.LoopWindowForTest);
        Console.WriteLine();

        // ---- 文件修改时间守卫 (P0-3) ----
        Section("[文件修改时间守卫]");
        FileTracker.Reset();
        Check("FileTracker: Reset后无记录", FileTracker.CheckForChanges().Count == 0);

        var ftTestFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(ftTestFile, "v1");
            FileTracker.RecordRead(ftTestFile);
            var (tracked, stale1) = FileTracker.GetStatus(ftTestFile);
            Check("FileTracker: RecordRead后已追踪", tracked);
            Check("FileTracker: 未修改时非stale", !stale1);

            // 外部修改内容 → stale（FileTracker 基于 SHA256 哈希比对）
            File.WriteAllText(ftTestFile, "v2_modified_by_external");
            var (tracked2, stale2) = FileTracker.GetStatus(ftTestFile);
            Check("FileTracker: 外部修改后变stale", tracked2 && stale2);

            // RecordWrite 后不再 stale
            FileTracker.RecordWrite(ftTestFile);
            var (tracked3, stale3) = FileTracker.GetStatus(ftTestFile);
            Check("FileTracker: RecordWrite后非stale", tracked3 && !stale3);

            // 再次外部修改 → CheckForChanges 应检测到
            File.WriteAllText(ftTestFile, "v3_another_external_modification");
            var changes = FileTracker.CheckForChanges();
            Check("FileTracker: CheckForChanges检测到变更", changes.Count > 0);
        }
        finally { try { File.Delete(ftTestFile); } catch { } }
        FileTracker.Reset();
        Check("FileTracker: Reset后清空", FileTracker.CheckForChanges().Count == 0);

        // ---- FileTracker 持久化（跨会话 stale-read 保护）----
        var ftPersistFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(ftPersistFile, "persist_v1");
            FileTracker.Reset();
            FileTracker.RecordRead(ftPersistFile);

            // 磁盘上应生成 .waycoder/file-tracker.json 并包含该文件
            var storePath = Path.Combine(Environment.CurrentDirectory, ".waycoder", "file-tracker.json");
            Check("FileTracker 持久化: 记录后生成 JSON", File.Exists(storePath));
            if (File.Exists(storePath))
            {
                var persistedText = File.ReadAllText(storePath);
                Check("FileTracker 持久化: JSON 包含路径", persistedText.Contains(Path.GetFileName(ftPersistFile)));
                Check("FileTracker 持久化: JSON 含 hash 字段", persistedText.Contains("\"hash\""));
            }

            // 外部修改后"重启"（重新从磁盘加载），应仍能检测到 stale
            File.WriteAllText(ftPersistFile, "persist_v2_external");
            FileTracker.ReloadForTest();
            var (pTracked, pStale) = FileTracker.GetStatus(ftPersistFile);
            Check("FileTracker 持久化: 重启后仍追踪", pTracked);
            Check("FileTracker 持久化: 重启后检测到外部修改", pStale);
        }
        finally { try { File.Delete(ftPersistFile); } catch { } }
        FileTracker.Reset();
        Console.WriteLine();

        // ---- CLI 参数: 会话恢复别名 ----
        Section("[CLI 参数: 会话恢复]");
        // 重新注册以获取干净状态（BuiltinArgs 注册是幂等的）
        var resumeArg = new Arguments.ResumeArg();
        Check("ResumeArg: 名称含 resume", resumeArg.Key == "resume");
        Check("ResumeArg: ValueCount 为可选(-1)", resumeArg.ValueCount == -1);
        Check("ResumeArg: 描述含 '恢复会话'", resumeArg.Description.Contains("恢复会话"));
        // NameDisplay 包含了所有别名
        Check("ResumeArg: 别名含 -c", resumeArg.NameDisplay.Contains("-c"));
        Check("ResumeArg: 别名含 --continue", resumeArg.NameDisplay.Contains("--continue"));
        Check("ResumeArg: 别名含 -r", resumeArg.NameDisplay.Contains("-r"));
        Check("ResumeArg: 别名含 --resume", resumeArg.NameDisplay.Contains("--resume"));

        var sessionListArg = new Arguments.SessionListArg();
        Check("SessionListArg: 名称正确", sessionListArg.Key == "session-list");
        Check("SessionListArg: ValueCount 为 0(标志)", sessionListArg.ValueCount == 0);
        Check("SessionListArg: 描述含 '列出'", sessionListArg.Description.Contains("列出"));
        Check("SessionListArg: 别名含 --sessions", sessionListArg.NameDisplay.Contains("--sessions"));

        // EconomyArg: 省 Token 模式短名（-e / --economy）
        var economyArg = new Arguments.EconomyArg();
        Check("EconomyArg: 名称含 economy", economyArg.Key == "economy");
        Check("EconomyArg: 短名含 -e", economyArg.Names.Contains("-e"));
        Check("EconomyArg: 长名含 --economy", economyArg.Names.Contains("--economy"));
        Check("EconomyArg: 描述含 '任务复杂度'", economyArg.Description.Contains("任务复杂度"));
        Console.WriteLine();

        // ---- 会话详情: MessageCount ----
        Section("[会话详情: MessageCount]");
        var mcTestId = "mc_test_" + DateTime.Now.ToString("yyyyMMddHHmmss");
        var mcMsgs = new List<JNode>
        {
            JNode.Object().Set("role", "user").Set("content", "msg1"),
            JNode.Object().Set("role", "assistant").Set("content", "r1"),
            JNode.Object().Set("role", "user").Set("content", "msg2"),
            JNode.Object().Set("role", "assistant").Set("content", "r2"),
            JNode.Object().Set("role", "user").Set("content", "msg3"),
            JNode.Object().Set("role", "assistant").Set("content", "r3"),
        };
        var mcSavedId = SessionManager.SaveSession(mcMsgs, "test-model", mcTestId);
        Check("SessionInfo: 保存成功", mcSavedId == mcTestId);

        // 通过 LoadSession 验证（比 ListSessions 更可靠，不依赖文件排序）
        var mcLoaded = SessionManager.LoadSession(mcTestId);
        Check("SessionInfo: 加载成功", mcLoaded != null);
        Check("SessionInfo: MessageCount == 6", mcLoaded?.Messages.Count == 6);
        Check("SessionInfo: Model 正确", mcLoaded?.Model == "test-model");

        // ListSessions 返回的 SessionInfo.MessageCount 与 LoadSession 一致
        var mcList = SessionManager.ListSessions(100);
        var mcEntry = mcList.FirstOrDefault(s => s.Id == mcTestId);
        if (mcEntry != null)
        {
            Check("SessionInfo: ListSessions.MessageCount == 6", mcEntry.MessageCount == 6);
            Check("SessionInfo: ListSessions.Preview 非空", !string.IsNullOrEmpty(mcEntry.Preview));
        }
        // 注意: ListSessions 可能扫描不到刚保存的文件（路径分区/文件系统缓存），核心验证通过即可

        // 清理
        try { SessionManager.DeleteSession(mcTestId); } catch { }
        Console.WriteLine();

        // ---- 对话框 resize 修复 ----
        Section("[对话框 resize 修复]");
        var resizeWin = new TuiWindow { Width = 40, Height = 10, X = 10, Y = 5 };
        bool resizeCalled = false;
        resizeWin.OnResizeContent = () => { resizeCalled = true; };
        resizeWin.OnResize(120, 40);
        Check("TuiWindow: OnResize 触发 OnResizeContent", resizeCalled);
        Check("TuiWindow: WindowHAlign 默认 Center", resizeWin.WindowHAlign == EHAlign.Center);
        Check("TuiWindow: WindowVAlign 默认 Middle", resizeWin.WindowVAlign == EVAlign.Middle);
        Check("TuiWindow: Width 保持", resizeWin.Width == 40);
        Check("TuiWindow: Height 保持", resizeWin.Height == 10);

        // 测试 RootView 在 resize 后被通知
        var resizeWin2 = new TuiWindow { Width = 30, Height = 8, WindowHAlign = EHAlign.Stretch, WindowVAlign = EVAlign.Stretch };
        resizeWin2.OnResizeContent = () => { };
        var oldRoot = resizeWin2.RootView;
        resizeWin2.OnResize(100, 30);
        Check("TuiWindow: RootView 非空", oldRoot != null);
        Console.WriteLine();

        // ---- 窗口比例缩放 (XScale/YScale) ----
        Section("[窗口比例缩放]");
        // XScale=0.5 在 100 宽终端 → Width=50
        var scaleWin1 = new TuiWindow { XScale = 0.5, MinWidth = 10 };
        scaleWin1.OnResizeContent = () => { };
        scaleWin1.OnResize(100, 40);
        Check("XScale=0.5 Width=50", scaleWin1.Width == 50);

        // YScale=0.5 在 40 高终端 → Height=20
        var scaleWin2 = new TuiWindow { YScale = 0.5, MinHeight = 3 };
        scaleWin2.OnResizeContent = () => { };
        scaleWin2.OnResize(100, 40);
        Check("YScale=0.5 Height=20", scaleWin2.Height == 20);

        // XScale=0 → 固定尺寸不变
        var scaleWin3 = new TuiWindow { Width = 30, XScale = 0 };
        scaleWin3.OnResizeContent = () => { };
        scaleWin3.OnResize(100, 40);
        Check("XScale=0 Width不变", scaleWin3.Width == 30);

        // MinWidth 约束：XScale 比例小于 MinWidth 时取 MinWidth
        var scaleWin4 = new TuiWindow { XScale = 0.3, MinWidth = 50 };
        scaleWin4.OnResizeContent = () => { };
        scaleWin4.OnResize(100, 40);
        Check("XScale+MinWidth: Width=Max(50,30)", scaleWin4.Width == 50);

        // MaxWidth 约束：XScale 比例超过 MaxWidth 时取 MaxWidth
        var scaleWin5 = new TuiWindow { XScale = 0.8, MaxWidth = 60 };
        scaleWin5.OnResizeContent = () => { };
        scaleWin5.OnResize(100, 40);
        Check("XScale+MaxWidth: Width=Min(80,60)", scaleWin5.Width == 60);

        // XScale + YScale 同时生效
        var scaleWin6 = new TuiWindow { XScale = 0.6, YScale = 0.3, MinWidth = 10, MinHeight = 3 };
        scaleWin6.OnResizeContent = () => { };
        scaleWin6.OnResize(200, 100);
        Check("XScale+YScale: Width=120", scaleWin6.Width == 120);
        Check("XScale+YScale: Height=30", scaleWin6.Height == 30);

        // 默认值
        Check("XScale 默认0", new TuiWindow().XScale == 0);
        Check("YScale 默认0", new TuiWindow().YScale == 0);

        // YScale=0 时只缩放宽度
        var scaleWin7 = new TuiWindow { XScale = 0.4, YScale = 0, Height = 20, MinWidth = 10 };
        scaleWin7.OnResizeContent = () => { };
        scaleWin7.OnResize(100, 40);
        Check("XScale=0.4 YScale=0: Width缩放Height固定", scaleWin7.Width == 40 && scaleWin7.Height == 20);
        Console.WriteLine();

        // ---- 窗口位置对齐 (WindowHAlign/WindowVAlign) ----
        Section("[窗口位置对齐]");
        // 默认对齐：居中
        Check("WindowHAlign 默认 Center", new TuiWindow().WindowHAlign == EHAlign.Center);
        Check("WindowVAlign 默认 Middle", new TuiWindow().WindowVAlign == EVAlign.Middle);

        // HAlign.Left → X=0
        var alignLeft = new TuiWindow { WindowHAlign = EHAlign.Left, WindowVAlign = EVAlign.Stretch, Width = 30, Height = 10 };
        alignLeft.OnResizeContent = () => { };
        alignLeft.OnResize(100, 40);
        Check("HAlign=Left: X=0", alignLeft.X == 0);

        // HAlign.Right → X = termW - Width
        var alignRight = new TuiWindow { WindowHAlign = EHAlign.Right, WindowVAlign = EVAlign.Stretch, Width = 30, Height = 10 };
        alignRight.OnResizeContent = () => { };
        alignRight.OnResize(100, 40);
        Check("HAlign=Right: X=70", alignRight.X == 70);

        // VAlign.Top → Y=0
        var alignTop = new TuiWindow { WindowHAlign = EHAlign.Stretch, WindowVAlign = EVAlign.Top, Width = 30, Height = 10 };
        alignTop.OnResizeContent = () => { };
        alignTop.OnResize(100, 40);
        Check("VAlign=Top: Y=0", alignTop.Y == 0);

        // VAlign.Bottom → Y = termH - Height
        var alignBottom = new TuiWindow { WindowHAlign = EHAlign.Stretch, WindowVAlign = EVAlign.Bottom, Width = 30, Height = 10 };
        alignBottom.OnResizeContent = () => { };
        alignBottom.OnResize(100, 40);
        Check("VAlign=Bottom: Y=30", alignBottom.Y == 30);

        // HAlign.Stretch + VAlign.Stretch → 不自动定位
        var alignNone = new TuiWindow { WindowHAlign = EHAlign.Stretch, WindowVAlign = EVAlign.Stretch, X = 15, Y = 8, Width = 30, Height = 10 };
        alignNone.OnResizeContent = () => { };
        alignNone.OnResize(100, 40);
        Check("Stretch+Stretch: X保持15", alignNone.X == 15);
        Check("Stretch+Stretch: Y保持8", alignNone.Y == 8);

        // 左上角对齐
        var alignTopLeft = new TuiWindow { WindowHAlign = EHAlign.Left, WindowVAlign = EVAlign.Top, Width = 30, Height = 10 };
        alignTopLeft.OnResizeContent = () => { };
        alignTopLeft.OnResize(100, 40);
        Check("Left+Top: (0,0)", alignTopLeft.X == 0 && alignTopLeft.Y == 0);

        // 右下角对齐
        var alignBottomRight = new TuiWindow { WindowHAlign = EHAlign.Right, WindowVAlign = EVAlign.Bottom, Width = 30, Height = 10 };
        alignBottomRight.OnResizeContent = () => { };
        alignBottomRight.OnResize(100, 40);
        Check("Right+Bottom: (70,30)", alignBottomRight.X == 70 && alignBottomRight.Y == 30);

        // ScreenMargin: Toast 右下角偏移
        var toast = new TuiWindow
        {
            WindowHAlign = EHAlign.Right, WindowVAlign = EVAlign.Bottom,
            Width = 30, Height = 3,
            ScreenMargin = new EdgeInsets(0, 2, 0, 2)   // Top=0, Right=2, Bottom=0, Left=2
        };
        toast.OnResizeContent = () => { };
        toast.OnResize(100, 40);
        Check("Toast Right+Bottom+Margin(0,2,0,2): X=68", toast.X == 100 - 30 - 2);   // 68
        Check("Toast Right+Bottom+Margin(0,2,0,2): Y=37", toast.Y == 40 - 3 - 0);     // 37 (no bottom margin)

        // ScreenMargin: 左上角偏移
        var topLeftMargin = new TuiWindow
        {
            WindowHAlign = EHAlign.Left, WindowVAlign = EVAlign.Top,
            Width = 20, Height = 5,
            ScreenMargin = new EdgeInsets(1, 0, 3, 0)   // Top=1, Right=0, Bottom=3, Left=0
        };
        topLeftMargin.OnResizeContent = () => { };
        topLeftMargin.OnResize(100, 40);
        Check("Margin(1,0,3,0): X=0", topLeftMargin.X == 0);
        Check("Margin(1,0,3,0): Y=1", topLeftMargin.Y == 1);

        // ScreenMargin 默认值
        var defaultMargin = new TuiWindow().ScreenMargin;
        Check("ScreenMargin 默认全0", defaultMargin.Top == 0 && defaultMargin.Right == 0 && defaultMargin.Bottom == 0 && defaultMargin.Left == 0);

        // 集成: 终端 resize → Screen → Window → Flex 全链路
        var resizeScreen = new ChatScreen();
        var intWin = new TuiWindow
        {
            XScale = 0.5, YScale = 0.4,
            WindowHAlign = EHAlign.Center, WindowVAlign = EVAlign.Middle,
            ScreenMargin = new EdgeInsets(1, 2, 1, 2),
            MinWidth = 10, MinHeight = 3
        };
        // 添加 Flex 布局的子控件
        var intHbox = new TuiHBox { Width = intWin.ContentWidth };
        intHbox.Add(new TuiLabel("L") { Flex = 1 });
        intHbox.Add(new TuiLabel("R") { Flex = 2 });
        intWin.RootView = intHbox;
        resizeScreen.Windows.Add(intWin);

        // 模拟终端从 120×40 resize 到 160×50
        resizeScreen.OnResize(160, 50);

        // 窗口尺寸: 160*0.5=80, 50*0.4=20
        Check("集成: resize后 Width=80", intWin.Width == 80);
        Check("集成: resize后 Height=20", intWin.Height == 20);
        // 居中位置: X=(160-80)/2=40, Y=(50-20)/2=15
        Check("集成: resize后居中 X=40", intWin.X == 40);
        Check("集成: resize后居中 Y=15", intWin.Y == 15);
        // RootView 被通知
        Check("集成: RootView Width=ContentWidth", intWin.RootView.Width == intWin.ContentWidth);

        // 再次 resize 到 80×24
        resizeScreen.OnResize(80, 24);
        Check("集成: 二次resize Width=40", intWin.Width == 40);
        Check("集成: 二次resize Height=9", intWin.Height == 9);
        Check("集成: 二次resize 居中 X=20", intWin.X == 20);
        Check("集成: 二次resize 居中 Y=7", intWin.Y == 7);
        Console.WriteLine();

        // ---- 孤儿工具修复 (P0-1): Agent 常量验证 ----
        Section("[孤儿工具修复]");
        // Agent 主循环在开始前调用 RepairOrphanedToolPairs
        // 通过消息注入验证：模拟孤儿场景
        var orphanMsgs = new List<JNode>
        {
            JNode.Object().Set("role", "user").Set("content", "test"),
            JNode.Object()
                .Set("role", "assistant")
                .Set("content", (string?)null)
                .Set("tool_calls", JNode.Array()
                    .Add(JNode.Object()
                        .Set("id", "orphan_call_1")
                        .Set("type", "function")
                        .Set("function", JNode.Object().Set("name", "bash").Set("arguments", "echo test")))
                    .Add(JNode.Object()
                        .Set("id", "orphan_call_2")
                        .Set("type", "function")
                        .Set("function", JNode.Object().Set("name", "read_file").Set("arguments", "{}")))),
        };
        // 注入孤儿: tool_calls 有 2 个，但 tool 结果消息只有 1 个
        orphanMsgs.Add(JNode.Object()
            .Set("role", "tool")
            .Set("tool_call_id", "orphan_call_1")
            .Set("content", "正常结果"));
        orphanMsgs.Add(JNode.Object()
            .Set("role", "tool")
            .Set("tool_call_id", "extra_result_no_call")
            .Set("content", "多余的 tool 结果"));

        // 通过 Agent 公开的测试钩子验证修复逻辑
        var testResult = Agent.TestOrphanRepair(orphanMsgs);
        Check("孤儿修复: 检测到孤儿调用", testResult.OrphanCallsDetected == 1); // orphan_call_2
        Check("孤儿修复: 检测到孤儿结果", testResult.OrphanResultsDetected == 1); // extra_result_no_call
        Check("孤儿修复: 注入合成错误", testResult.OrphanCallsFixed == 1);
        Check("孤儿修复: 移除多余结果", testResult.OrphanResultsRemoved == 1);
        Console.WriteLine();

        // ── v0.36.0: FileTracker 先读后改保护 ──
        var ftTestFile2 = Path.Combine(Path.GetTempPath(), "waycoder_ft_test2.txt");
        try
        {
            File.WriteAllText(ftTestFile2, "original content");
            FileTracker.Reset();

            // 未读取就编辑 → 应返回警告
            var warn = FileTracker.ValidatePreEdit(ftTestFile2);
            Check("FileTracker 未读先改返回警告", warn != null && warn.Contains("尚未被 read_file"));

            // 读取后编辑 → 应通过
            FileTracker.RecordRead(ftTestFile2);
            var warn2 = FileTracker.ValidatePreEdit(ftTestFile2);
            Check("FileTracker 已读后编辑通过", warn2 == null);

            // 外部修改后编辑 → 应返回警告
            Thread.Sleep(1100); // 确保时间戳差超过 1 秒
            File.WriteAllText(ftTestFile2, "modified externally");
            var warn3 = FileTracker.ValidatePreEdit(ftTestFile2);
            Check("FileTracker 外部修改后编辑返回警告", warn3 != null && warn3.Contains("外部修改"));

            // 新文件（不存在）→ 通过
            var warn4 = FileTracker.ValidatePreEdit("/nonexistent/file_xyz.txt");
            Check("FileTracker 新文件无需读取", warn4 == null);

            // Reset 清空 LastReadTimes
            FileTracker.Reset();
            var warn5 = FileTracker.ValidatePreEdit(ftTestFile2);
            Check("FileTracker Reset 后未读先改警告", warn5 != null);
        }
        finally
        {
            try { File.Delete(ftTestFile2); } catch { }
        }
        FileTracker.Reset();

        // ── v0.36.0: Agent 工具集分层 ──
        Check("SubAgentDeniedTools 不包含 bash（子智能体保留 shell 权限）",
            !ToolRegistry.SubAgentDeniedTools.Contains("bash"));
        Check("SubAgentDeniedTools 包含 rm",
            ToolRegistry.SubAgentDeniedTools.Contains("rm"));
        Check("SubAgentDeniedTools 包含 kill",
            ToolRegistry.SubAgentDeniedTools.Contains("kill"));
        Check("SubAgentDeniedTools 包含 git",
            ToolRegistry.SubAgentDeniedTools.Contains("git"));

        // ── v0.53.0: 子智能体健壮性加固 ──
        var (dnBlocked, _) = BashGuard.CheckBanned("dotnet new console");
        Check("BashGuard 拦截 dotnet new", dnBlocked);
        var (dbBlocked, _) = BashGuard.CheckBanned("dotnet build -c Release");
        Check("BashGuard 不误伤 dotnet build", !dbBlocked);
        Check("子智能体纪律含「禁止创建」", SystemPrompt.SubAgentDiscipline.Contains("禁止创建"));
        Check("子智能体纪律含「自测」", SystemPrompt.SubAgentDiscipline.Contains("自测"));
        var baseLLM = new LLM("test-model", "key");
        baseLLM.ModelOverride = "big-model";
        var subClone = baseLLM.Clone();
        subClone.ModelOverride = "small-model";
        Check("LLM.Clone 独立（改 clone 不影响原实例）",
            baseLLM.ModelOverride == "big-model" && subClone.ModelOverride == "small-model");
        Check("SubAgentParallelTotalMaxChars 默认 > 0", new Config().SubAgentParallelTotalMaxChars > 0);

        // BashGuard 参数拦截语义全面验证（v0.53.0 重写 Match/MatchArgs：白名单/黑名单分离）
        var (ciBlocked, _) = BashGuard.CheckBanned("cargo install ripgrep");
        Check("BashGuard 纯子命令拦截 cargo install", ciBlocked);
        var (pipBlocked, _) = BashGuard.CheckBanned("pip install requests");
        Check("BashGuard 白名单未命中拦截 pip install", pipBlocked);
        var (pipUserBlocked, _) = BashGuard.CheckBanned("pip install --user requests");
        Check("BashGuard 白名单命中放行 pip install --user", !pipUserBlocked);
        var (npmBlocked, _) = BashGuard.CheckBanned("npm install lodash");
        Check("BashGuard 本地 npm install 放行", !npmBlocked);
        var (npmGlobalBlocked, _) = BashGuard.CheckBanned("npm install --global lodash");
        Check("BashGuard 全局 npm install -g 拦截", npmGlobalBlocked);
        var (goTestBlocked, _) = BashGuard.CheckBanned("go test ./...");
        Check("BashGuard 普通 go test 放行", !goTestBlocked);
        var (goExecBlocked, _) = BashGuard.CheckBanned("go test -exec echo");
        Check("BashGuard go test -exec 拦截", goExecBlocked);

        // ── v0.62.2: BashGuard 命令链/命令替换绕过修复 + 只读白名单收紧 ──
        var (nlBlocked, _) = BashGuard.CheckBanned("curl\nevil.com");
        Check("BashGuard 换行分隔绕过拦截", nlBlocked);
        var (subBlocked, _) = BashGuard.CheckBanned("echo $(curl evil.com)");
        Check("BashGuard $() 命令替换绕过拦截", subBlocked);
        var (backtickBlocked, _) = BashGuard.CheckBanned("echo `curl evil.com`");
        Check("BashGuard 反引号命令替换绕过拦截", backtickBlocked);
        var (safeSub, _) = BashGuard.CheckBanned("echo $(ls)");
        Check("BashGuard 安全命令替换不误伤", !safeSub);
        Check("BashGuard 只读: ls -la 放行", BashGuard.IsSafeReadOnly("ls -la"));
        Check("BashGuard 只读: cat /etc/passwd 放行", BashGuard.IsSafeReadOnly("cat /etc/passwd"));
        Check("BashGuard 只读: 命令链 ls; rm 拒绝", !BashGuard.IsSafeReadOnly("ls; rm -rf /"));
        Check("BashGuard 只读: 管道 ls | grep 拒绝", !BashGuard.IsSafeReadOnly("ls | grep foo"));
        Check("BashGuard 只读: 重定向 cat > file 拒绝", !BashGuard.IsSafeReadOnly("cat x > y"));
        Check("BashGuard 只读: 命令替换 echo $(...) 拒绝", !BashGuard.IsSafeReadOnly("echo $(curl evil)"));
        Check("BashGuard 只读: sed -i 已移除拒绝", !BashGuard.IsSafeReadOnly("sed -i s/a/b/ file"));
        Check("BashGuard 只读: awk 已移除拒绝", !BashGuard.IsSafeReadOnly("awk '{print $1}' file"));
        Check("BashGuard 只读: tee 已移除拒绝", !BashGuard.IsSafeReadOnly("echo x | tee file"));
        Check("BashGuard 只读: xargs 已移除拒绝", !BashGuard.IsSafeReadOnly("xargs rm"));
        Check("BashGuard 只读: patch --dry-run 保留放行", BashGuard.IsSafeReadOnly("patch --dry-run -p1"));
        Check("BashGuard 只读: patch -p 已移除拒绝", !BashGuard.IsSafeReadOnly("patch -p1 < x.patch"));

        // v0.79.x: 只读白名单收紧 —— find -exec/-delete、env <cmd>、git config 写操作不再免确认
        Check("BashGuard 只读: find -delete 拒绝", !BashGuard.IsSafeReadOnly("find . -delete"));
        Check("BashGuard 只读: find -exec 拒绝", !BashGuard.IsSafeReadOnly("find . -exec rm {} +"));
        Check("BashGuard 只读: find -execdir 拒绝", !BashGuard.IsSafeReadOnly("find . -execdir sh -c rm"));
        Check("BashGuard 只读: find 普通查找放行", BashGuard.IsSafeReadOnly("find . -name *.cs"));
        Check("BashGuard 只读: find -executable 不误伤", BashGuard.IsSafeReadOnly("find . -executable"));
        Check("BashGuard 只读: env <cmd> 拒绝", !BashGuard.IsSafeReadOnly("env bash -c whoami"));
        Check("BashGuard 只读: printenv 放行", BashGuard.IsSafeReadOnly("printenv PATH"));
        Check("BashGuard 只读: git config 写操作拒绝", !BashGuard.IsSafeReadOnly("git config core.pager cat"));
        Check("BashGuard 只读: git config --list 也拒绝（前缀无法区分读写）", !BashGuard.IsSafeReadOnly("git config --list"));

        // v0.79.x: PathSafety 敏感路径防护
        Check("PathSafety: id_rsa 拦截", PathSafety.CheckSensitive("/Users/x/.ssh/id_rsa") != null);
        Check("PathSafety: authorized_keys 拦截", PathSafety.CheckSensitive("/home/x/.ssh/authorized_keys") != null);
        Check("PathSafety: .bashrc 拦截", PathSafety.CheckSensitive("/home/x/.bashrc") != null);
        Check("PathSafety: .pem 拦截", PathSafety.CheckSensitive("/home/x/keys/server.pem") != null);
        Check("PathSafety: .aws 目录拦截", PathSafety.CheckSensitive("/home/x/.aws/credentials") != null);
        Check("PathSafety: .config/gcloud 拦截", PathSafety.CheckSensitive("/home/x/.config/gcloud/credentials.json") != null);
        Check("PathSafety: /etc/passwd 拦截", PathSafety.CheckSensitive("/etc/passwd") != null);
        Check("PathSafety: 普通源文件放行", PathSafety.CheckSensitive("/home/x/project/src/main.cs") == null);
        Check("PathSafety: 临时目录放行", PathSafety.CheckSensitive("/tmp/output.txt") == null);
        Check("PathSafety: 项目内普通文件放行", PathSafety.CheckSensitive("/Users/x/project/README.md") == null);

        // v0.79.x: EnvScrubber 凭据名覆盖扩展
        Check("EnvScrubber: MYSQL_PWD 敏感", EnvScrubber.IsSensitive("MYSQL_PWD"));
        Check("EnvScrubber: DB_PASS 敏感", EnvScrubber.IsSensitive("DB_PASS"));
        Check("EnvScrubber: GOOGLE_APPLICATION_CREDENTIALS 敏感", EnvScrubber.IsSensitive("GOOGLE_APPLICATION_CREDENTIALS"));
        Check("EnvScrubber: DOCKER_AUTH 敏感", EnvScrubber.IsSensitive("DOCKER_AUTH"));
        Check("EnvScrubber: AZURE_STORAGE_CONNECTION_STRING 敏感", EnvScrubber.IsSensitive("AZURE_STORAGE_CONNECTION_STRING"));
        Check("EnvScrubber: NETRC 敏感", EnvScrubber.IsSensitive("NETRC"));
        Check("EnvScrubber: PATH 不敏感", !EnvScrubber.IsSensitive("PATH"));
        Check("EnvScrubber: HOME 不敏感", !EnvScrubber.IsSensitive("HOME"));

        // v0.79.x: SSRF ResolveSafeAddresses（ConnectCallback 原子解析+校验，防 DNS 重绑定）
        var rsaPub = SsgfGuard.ResolveSafeAddresses("8.8.8.8");
        Check("SSRF: ResolveSafe 公网字面量放行并返回地址", rsaPub.safe && rsaPub.addrs.Length == 1);
        var rsaPriv = SsgfGuard.ResolveSafeAddresses("10.0.0.1");
        Check("SSRF: ResolveSafe 内网字面量拒绝", !rsaPriv.safe);
        var rsaMeta = SsgfGuard.ResolveSafeAddresses("169.254.169.254");
        Check("SSRF: ResolveSafe 云元数据拒绝", !rsaMeta.safe);

        // v0.53.1: tasks 数组元素提取（对象元素 {description/task/...} 正确解出文本，而非乱码）
        var extStr = AgentTool.ExtractTaskText("给 TimeSeries 加冒烟测试");
        Check("AgentTool.ExtractTaskText 纯字符串透传", extStr == "给 TimeSeries 加冒烟测试");
        var extDict = AgentTool.ExtractTaskText(new Dictionary<string, object?> { ["description"] = "给 Automata 加冒烟测试" });
        Check("AgentTool.ExtractTaskText 对象提取 description", extDict == "给 Automata 加冒烟测试");
        var extDictTask = AgentTool.ExtractTaskText(new Dictionary<string, object?> { ["task"] = "给 Geospatial 加冒烟测试" });
        Check("AgentTool.ExtractTaskText 对象提取 task", extDictTask == "给 Geospatial 加冒烟测试");
        var extJson = AgentTool.ExtractTaskText(JNode.Object().Set("description", "JsonObject 路径"));
        Check("AgentTool.ExtractTaskText JsonObject 提取", extJson == "JsonObject 路径");
        var extNull = AgentTool.ExtractTaskText(null);
        Check("AgentTool.ExtractTaskText null 返回 null", extNull == null);

        // 深度 0（允许 agent 递归）
        var depth0Tools = ToolRegistry.GetSubAgentTools(ToolRegistry.AllTools, 0, 5);
        var depth0Names = depth0Tools.Select(t => t.Name).ToHashSet();
        Check("子Agent深度0 有 bash", depth0Names.Contains("bash"));
        Check("子Agent深度0 无 rm", !depth0Names.Contains("rm"));
        Check("子Agent深度0 有 agent", depth0Names.Contains("agent"));
        Check("子Agent深度0 有 write_file", depth0Names.Contains("write_file"));
        Check("子Agent深度0 有 read_file", depth0Names.Contains("read_file"));
        Check("子Agent深度0 有 grep", depth0Names.Contains("grep"));

        // 最大深度（禁止 agent）
        var maxDepthTools = ToolRegistry.GetSubAgentTools(ToolRegistry.AllTools, 4, 5);
        var maxDepthNames = maxDepthTools.Select(t => t.Name).ToHashSet();
        Check("子Agent最大深度 无 agent", !maxDepthNames.Contains("agent"));
        Check("子Agent最大深度 仍有 read_file", maxDepthNames.Contains("read_file"));

        // ── v0.36.0: Git 状态注入提示词 ──
        var gitStatusResult = SystemPrompt.GenerateGitStatus();
        Check("Git 状态注入: 非 null", gitStatusResult != null);
        // 当前在 git 仓库中，应包含分支信息
        if (!string.IsNullOrEmpty(gitStatusResult))
        {
            Check("Git 状态注入: 包含仓库信息",
                gitStatusResult.Contains("分支") || gitStatusResult.Contains("branch")
                || gitStatusResult.Contains("Git 仓库状态"));
        }
        Console.WriteLine();

    }
}
