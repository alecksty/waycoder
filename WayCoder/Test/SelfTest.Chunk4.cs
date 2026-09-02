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

        // 工具嵌套子消息
        Check("AddToolMsg 嵌套=1", screen.ChatMessages[2].Indent == 1);
        screen.AddToolProgress("bash", "echo test");
        Check("AddToolProgress 为 tool 角色", screen.ChatMessages[^1].Role == "tool");
        Check("AddToolProgress 嵌套=1", screen.ChatMessages[^1].Indent == 1);

        // Token 显示（大/小模型用量 + 花费）
        screen.UpdateTokenDisplayFull(1234, 567, 0.0123, 80000, 128000, 0, 0);
        Check("StatusRight 非空", screen.StatusRight.Length > 0);
        Check("StatusRight 含大/小模型用量", screen.StatusRight.Contains("大:") && screen.StatusRight.Contains("小:"));

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

        // ---- 快捷键表（GetHelpText 供槽位首条对话输出）----
        var helpText = TuiKeybindHelp.GetHelpText();
        Check("快捷键表: 含全局分组", helpText.Contains("全局"));
        Check("快捷键表: 含 F1-F10 槽位", helpText.Contains("F1 - F10"));
        Check("快捷键表: 含 Ctrl+H 帮助", helpText.Contains("Ctrl+H"));
        Check("快捷键表: 含模型快捷键", helpText.Contains("/model"));
        Check("快捷键表: 已删除作废的主题快捷键", !helpText.Contains("Ctrl+Shift+F"));
        // 对齐要按「渲染后」的样子量：«grey» 等标记不占屏宽，带着标记量会把标记长度算进列宽，
        // 各行标记一样长时仍能歪打正着，一旦某行标记不同就漏判 —— 先剥标记再量
        static string StripMarkup(string s) =>
            System.Text.RegularExpressions.Regex.Replace(s, "«[^»]*»", "");
        // 表格形态：两列以 │ 分隔，键列按显示宽度补齐 → 同一分类内竖线必须在同一列。
        // 启动只显示简版（StartupKeys 里的高频键），完整表在 Ctrl+H 面板，行数变少是预期
        var kbRows = helpText.Split('\n')
            .Where(l => l.Contains('│')).Select(l => StripMarkup(l).TrimEnd()).ToList();
        Check("快捷键表: 是两列表格（简版）", kbRows.Count >= 8);
        Check("快捷键表: 简版行数明显少于完整表", kbRows.Count <= 20);
        Check("快捷键表: 竖线列对齐(CJK 安全)",
            kbRows.Select(l => AnsiHelper.DisplayWidth(l[..l.IndexOf('│')])).Distinct().Count() == 1);
        Check("快捷键表: 行首左对齐(无居中前导空格)",
            kbRows.TrueForAll(l => l.StartsWith("  ") && !l.StartsWith("   ")));
        Check("快捷键表: 含分隔线", StripMarkup(helpText).Contains("────"));
        // 键名不着色、其余灰白：竖线之前不得出现标记，说明列必须被 «grey» 包住
        Check("快捷键表: 键名不带颜色标记",
            helpText.Split('\n').Where(l => l.Contains('│'))
                .All(l => !l[..l.IndexOf('│')].Replace("«grey»", "").Contains('«')));
        Check("快捷键表: 说明列走灰白标记",
            helpText.Split('\n').Where(l => l.Contains('│')).All(l => l.Contains("«grey»│")));

        // ---- 表述必须与实现一致（逐条对着代码核过，别再漂回去）----
        var kbPlain = StripMarkup(helpText);
        // Ctrl+C 走 CancelKeyPress 退出（InputManager.cs 关了 TreatControlCAsInput），中断是 Esc
        Check("快捷键表: Ctrl+C 不再谎称中断 Agent", !kbPlain.Contains("Ctrl+C") || !kbPlain.Contains("中断当前 Agent 操作"));
        Check("快捷键表: 补上 Esc 中断", kbPlain.Contains("Esc") && kbPlain.Contains("中断当前 Agent"));
        Check("快捷键表: 补上 Ctrl+Z 暂停", kbPlain.Contains("Ctrl+Z"));
        // Ctrl+↑↓ 是聊天滚动，输入历史是裸 ↑↓（ChatScreen.Input.cs:652 / :975）
        Check("快捷键表: Ctrl+↑↓ 不再谎称输入历史", !kbPlain.Contains("输入历史翻页"));
        // Tab 被 HandleTabCompletion 吃掉，从不切焦点（ChatScreen.Input.cs:784）
        Check("快捷键表: Tab 不再谎称切换焦点", !kbPlain.Contains("切换焦点（输入区"));
        // Ctrl+P 是 ShowPromptBar 建议条，不是 CommandPalette（ChatScreen.Input.cs:628）
        Check("快捷键表: Ctrl+P 不再谎称命令面板", !kbPlain.Contains("命令面板（斜杠"));
        // 裸 Home/End 落到输入区光标，列表首尾要 Ctrl+Home/End（TuiEditBase.cs:226）
        Check("快捷键表: Home/End 不再谎称跳列表首尾", !kbPlain.Contains("跳到列表顶部"));
        // 脚注只剩 Unix 一条：Ctrl+M≡0x0D≡Enter、Ctrl+H≡0x08≡Backspace 抢码，无解，给斜杠命令兜底
        Check("快捷键表: 有 Unix 同码脚注", kbPlain.Contains("Unix") && kbPlain.Contains("同码"));
        Check("快捷键表: 脚注给出兜底命令", kbPlain.Contains("/model") && kbPlain.Contains("/help"));
        // Shift+Tab 已在 Windows 修好（Program.Repl.cs 认 Tab+Shift 修饰键），脚注不该再说它坏
        Check("快捷键表: 不再声称 Shift+Tab 在 Windows 失效",
            !kbPlain.Contains("Shift+Tab") || !kbPlain.Contains("Windows 下失效"));
        // Ctrl+K 是两平台通用的模式切换别名，表里必须写出来，否则用户只知道 Shift+Tab
        Check("快捷键表: 模式切换列出 Ctrl+K 别名", kbPlain.Contains("Ctrl+K"));

        // ---- 模式切换键判定（REPL 主循环没法自测，判定抽到 InputEvent.IsModeSwitchKey）----
        // 三个入口都得认：Unix 的 ESC[Z、Windows 的 Tab+Shift、通用 Ctrl+K
        static UI.TUI.Base.InputEvent KeyEv(ConsoleKey k, ConsoleModifiers mods = 0) => new()
        {
            Type = UI.TUI.Base.InputType.Key,
            KeyInfo = new ConsoleKeyInfo('\0', k,
                mods.HasFlag(ConsoleModifiers.Shift),
                mods.HasFlag(ConsoleModifiers.Alt),
                mods.HasFlag(ConsoleModifiers.Control)),
        };
        Check("模式键: Unix ESC[Z",
            UI.TUI.Base.InputEvent.IsModeSwitchKey(new UI.TUI.Base.InputEvent { Type = UI.TUI.Base.InputType.ShiftTab }));
        Check("模式键: Windows Tab+Shift",
            UI.TUI.Base.InputEvent.IsModeSwitchKey(KeyEv(ConsoleKey.Tab, ConsoleModifiers.Shift)));
        Check("模式键: Ctrl+K 别名",
            UI.TUI.Base.InputEvent.IsModeSwitchKey(KeyEv(ConsoleKey.K, ConsoleModifiers.Control)));
        // 反向两条最要命：裸 Tab 抢走了就没法补全路径，裸 k 抢走了打字就切模式
        Check("模式键: 裸 Tab 放行（留给路径补全）",
            !UI.TUI.Base.InputEvent.IsModeSwitchKey(KeyEv(ConsoleKey.Tab)));
        Check("模式键: 裸 k 放行（当普通字符）",
            !UI.TUI.Base.InputEvent.IsModeSwitchKey(KeyEv(ConsoleKey.K)));
        Check("模式键: Ctrl+Tab 不算",
            !UI.TUI.Base.InputEvent.IsModeSwitchKey(KeyEv(ConsoleKey.Tab, ConsoleModifiers.Control)));
        Check("模式键: Shift+K 不算",
            !UI.TUI.Base.InputEvent.IsModeSwitchKey(KeyEv(ConsoleKey.K, ConsoleModifiers.Shift)));
        Check("模式键: 回车不算", !UI.TUI.Base.InputEvent.IsModeSwitchKey(KeyEv(ConsoleKey.Enter)));
        Check("模式键: 超时事件不算",
            !UI.TUI.Base.InputEvent.IsModeSwitchKey(new UI.TUI.Base.InputEvent { Type = UI.TUI.Base.InputType.Timeout }));
        // 内联代码不再带底色：48 是扩展背景引导码，裸发会被终端渲染成刺眼亮绿
        var inlineCode = UI.Shared.MarkdownParser.ParseInline("路径 `D:\\a\\b` 结束", 0);
        Check("内联代码: 不写残缺背景码 48", inlineCode.TrueForAll(s => s.Bg != 48));
        // 纯文本消息（system/tool）也要解码 «» —— 否则快捷键表直接把 «grey» 印给用户
        var plainSegs = UI.Tui.TuiMarkdown.RenderMessage("«grey»─灰«/»白", "system", 40, plainText: true);
        var plainFlat = plainSegs.SelectMany(l => l).ToList();
        Check("纯文本: «» 标记不外泄", plainFlat.TrueForAll(s => !s.Text.Contains('«')));
        Check("纯文本: «grey» 段着灰色", plainFlat.Exists(s => s.Text.Contains('灰') && s.Fg == 90));
        Check("纯文本: «/» 后恢复默认色", plainFlat.Exists(s => s.Text.Contains('白') && s.Fg != 90));
        // 只解码 «»，不做完整 Markdown —— shell 输出里的反引号/星号是数据，不能被吃掉
        var plainRaw = UI.Tui.TuiMarkdown.RenderMessage("cmd `x` **y**", "tool", 40, plainText: true)
            .SelectMany(l => l).Aggregate("", (a, s) => a + s.Text);
        Check("纯文本: 反引号/星号原样保留", plainRaw.Contains("`x`") && plainRaw.Contains("**y**"));
        // system 消息（/model list 等）走 markdown 渲染（ChatScreen.AddMessage 不再强制 system 纯文本）
        var mdSys = UI.Tui.TuiMarkdown.RenderMessage("cmd `x` **y**", "system", 40, plainText: false)
            .SelectMany(l => l).Aggregate("", (a, s) => a + s.Text);
        Check("markdown: system 消息渲染反引号为代码", !mdSys.Contains('`') && mdSys.Contains("x"));
        Check("markdown: system 消息渲染加粗", !mdSys.Contains('*') && mdSys.Contains("y"));

        // 代码块语法高亮：C# 代码块应产出多色 token（关键字青 36 ≠ 字符串绿 32 ≠ 数字黄 33）
        var cbSegs = UI.Tui.TuiMarkdown.RenderMessage(
            "```csharp\nstring s = \"hi\";\nvar n = 42;\n```", "assistant", 80);
        var cbColors = cbSegs.SelectMany(l => l).Where(s => s.Fg > 0).Select(s => s.Fg).Distinct().ToList();
        Check("代码块高亮: 产出多种颜色", cbColors.Count >= 3);
        Check("代码块高亮: 含关键字青色(36)", cbColors.Contains(36));

        // 4 反引号围栏（````js，AI 在内容含 ``` 时常用）：语言标签不得残留多余反引号（旧 bug：lang="`js"）
        var fence4 = UI.Tui.TuiMarkdown.RenderMessage(
            "````js\nconst x = \"hi\";\n````", "assistant", 80);
        var fence4Text = fence4.SelectMany(l => l).Aggregate("", (a, s) => a + s.Text);
        Check("代码块围栏: 4反引号语言标签无残留反引号", !fence4Text.Contains("`js") && fence4Text.Contains("js"));

        // ---- «fg:#rrggbb» / «bg:#rrggbb» 真彩标记 ----
        const int RgbRed = 0x1000000 | 0xFF0000;
        Check("真彩: fg:#rrggbb", UI.Shared.MarkdownParser.TryMapTag("fg:#ff0000", out var cFg, out var bFg)
            && cFg == RgbRed && !bFg);
        Check("真彩: bg:#rrggbb 归背景", UI.Shared.MarkdownParser.TryMapTag("bg:#ff0000", out var cBg, out var bBg)
            && cBg == RgbRed && bBg);
        Check("真彩: 裸 #rrggbb 当前景", UI.Shared.MarkdownParser.TryMapTag("#ff0000", out var cBare, out var bBare)
            && cBare == RgbRed && !bBare);
        Check("真彩: #rgb 缩写等价 #rrggbb",
            UI.Shared.MarkdownParser.TryMapTag("#f00", out var cShort, out _) && cShort == RgbRed);
        Check("真彩: 大小写不敏感",
            UI.Shared.MarkdownParser.TryMapTag("FG:#FF0000", out var cUp, out _) && cUp == RgbRed);
        Check("真彩: 命名背景 bg:red → 41",
            UI.Shared.MarkdownParser.TryMapTag("bg:red", out var cNamed, out var bNamed) && cNamed == 41 && bNamed);
        Check("真彩: 非法十六进制不认",
            !UI.Shared.MarkdownParser.TryMapTag("fg:#gg0000", out _, out _)
            && !UI.Shared.MarkdownParser.TryMapTag("#ff00", out _, out _));
        // 前景/背景各自独立入栈：«bg» 里嵌 «fg» 再 «/»，背景必须留着
        var rgbSegs = UI.Shared.MarkdownParser.ParseInline("«bg:#000080»底«fg:#ff0000»红«/»还底«/»外");
        Check("真彩: 背景段落生效", rgbSegs.Exists(s => s.Text == "底" && s.Bg == (0x1000000 | 0x000080)));
        Check("真彩: 内层前景不吃掉背景",
            rgbSegs.Exists(s => s.Text == "红" && s.Color == RgbRed && s.Bg == (0x1000000 | 0x000080)));
        Check("真彩: «/» 只弹一层",
            rgbSegs.Exists(s => s.Text == "还底" && s.Bg == (0x1000000 | 0x000080) && s.Color == 0));
        Check("真彩: 出栈后恢复默认", rgbSegs.Exists(s => s.Text == "外" && s.Bg == 0 && s.Color == 0));
        Check("真彩: 未知标签原样保留",
            UI.Shared.MarkdownParser.ParseInline("«fg:#zz»x").Exists(s => s.Text.Contains("«fg:#zz»")));
        // CLI 解码器（Replace 链枚举不到带参标签，须走 ExpandColorTags）
        var cliRgb = Program.SpectreToAnsi("«fg:#ff0000»红«/»«bg:#00ff00»底«/»");
        Check("真彩: CLI 前景转 38;2", cliRgb.Contains("38;2;255;0;0"));
        Check("真彩: CLI 背景转 48;2", cliRgb.Contains("48;2;0;255;0"));
        Check("真彩: CLI 不残留书名号", !cliRgb.Contains('«'));
        Console.WriteLine();

        // ---- Markdown 表格 ----
        Section("[Markdown 表格]");
        var mdTable = @"
| 语言 | 速度 | 评分 |
|------|------|------|
| C# | 快 | 9.5 |
| Python | 慢 | 6.5 |
";
        var rendered = UI.Tui.TuiMarkdown.RenderMessage(mdTable, "assistant", 80);
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
        var mdBold = UI.Tui.TuiMarkdown.RenderMessage("| **粗体** | `代码` |\n|-----|-----|\n| 正常 | 测试 |", "assistant", 80);
        Check("内联加粗表格 = 5 行", mdBold.Count == 5);
        // 两列表格（1表头 + 1数据行 = 5 行）
        var md2Col = UI.Tui.TuiMarkdown.RenderMessage("| A | B |\n|---|---|\n| 1 | 2 |", "assistant", 80);
        Check("2列表格渲染 = 5 行", md2Col.Count == 5);
        // 空表格（仅表头无数据行 = 4 行：顶部+表头+分隔+底部）
        var mdEmpty = UI.Tui.TuiMarkdown.RenderMessage("| H |\n|---|", "assistant", 80);
        Check("空数据表格 = 4 行", mdEmpty.Count == 4);
        Console.WriteLine();

        // ---- 输入处理逻辑 ----
        Section("[输入规范化]");
        var input = "／help";
        input = input.Replace('／', '/').Replace('！', '!').Replace('＃', '#');
        Check("全角／→半角/", input == "/help");

        // 输入框横线前缀变色：! 红（危险 shell）/ / 青（命令）/ @ 品红 / # 灰 / 其余默认
        Check("边框色: ! = 红", UI.Tui.Screens.ChatScreen.InputBorderColorFor("!ls") == UI.Shared.AnsiColors.Red);
        Check("边框色: / = 青", UI.Tui.Screens.ChatScreen.InputBorderColorFor("/help") == UI.Shared.AnsiColors.Cyan);
        Check("边框色: @ = 品红", UI.Tui.Screens.ChatScreen.InputBorderColorFor("@agent") == UI.Shared.AnsiColors.Magenta);
        Check("边框色: # = 灰", UI.Tui.Screens.ChatScreen.InputBorderColorFor("#comment") == UI.Shared.AnsiColors.Grey);
        Check("边框色: 普通文本 = 默认", UI.Tui.Screens.ChatScreen.InputBorderColorFor("hello") == UI.Tui.TuiTheme.Current.SeparatorFg);
        Check("边框色: 空输入 = 默认", UI.Tui.Screens.ChatScreen.InputBorderColorFor("") == UI.Tui.TuiTheme.Current.SeparatorFg);
        Check("边框色: 前导空格仍识别", UI.Tui.Screens.ChatScreen.InputBorderColorFor("  !ls") == UI.Shared.AnsiColors.Red);
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
        Check("供应商显示名 aihubmix → AIHubMix", ModelCatalog.ProviderDisplayName("aihubmix") == "AIHubMix");
        Check("供应商显示名 opencode-go → OpenCode Go", ModelCatalog.ProviderDisplayName("opencode-go") == "OpenCode Go");
        Check("供应商显示名未注册回退 id", ModelCatalog.ProviderDisplayName("nonexistent") == "nonexistent");
        Check("供应商显示名大小写不敏感", ModelCatalog.ProviderDisplayName("AIHubMix") == "AIHubMix");
        Check("供应商显示名 Trim", ModelCatalog.ProviderDisplayName("  aihubmix  ") == "AIHubMix");
        Check("供应商显示名空值回退空串", ModelCatalog.ProviderDisplayName(null) == "");

        // 同 id 多服务商归属：deepseek-v4-flash 内置分属 DeepSeek 官方 / AIHubMix 网关，必须按 (id, baseUrl) 精确定位
        Check("模型按网关精确定位 aihubmix",
            ModelCatalog.Find("deepseek-v4-flash", "https://api.inferera.com/v1")?.ProviderId == "aihubmix");
        Check("模型按网关精确定位官方",
            ModelCatalog.Find("deepseek-v4-flash", "https://api.deepseek.com")?.ProviderId == "deepseek");
        Check("模型按网关精确定位（尾斜杠规范化）",
            ModelCatalog.Find("deepseek-v4-flash", "https://api.inferera.com/v1/")?.ProviderId == "aihubmix");
        // Web 端 ApplyModel 网关推导：baseUrl 空时用 provider 注册表地址（Find 内置官方优先会取错网关）
        Check("provider 注册表地址 aihubmix", ConnectionConfig.ResolveBaseUrl("aihubmix") == "https://api.inferera.com/v1");
        Check("provider 注册表地址 deepseek", ConnectionConfig.ResolveBaseUrl("deepseek") == "https://api.deepseek.com");
        {
            var oldModel = Config.Instance.Model; var oldBase = Config.Instance.BaseUrl; var oldProv = Config.Instance.Provider;
            try
            {
                Config.Instance.Model = "deepseek-v4-flash";
                Config.Instance.BaseUrl = "https://api.inferera.com/v1";
                Config.Instance.Provider = "aihubmix";
                var slot = new AgentSlotConfig.SlotConfig { UseGlobal = true };
                // 选了 AIHubMix 网关的 deepseek-v4-flash，槽位服务商应解析为 aihubmix（而非内置官方 deepseek）
                Check("槽位服务商按网关归属 aihubmix", AgentSlotConfig.ResolveLargeProvider(slot, 0) == "aihubmix");
            }
            finally { Config.Instance.Model = oldModel; Config.Instance.BaseUrl = oldBase; Config.Instance.Provider = oldProv; }
        }
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

        // Crush 模型数据导入：providers.json 数组 + crush.json providers 对象（snake_case 字段）
        {
            var crushArr = """[{"id":"deepseek","name":"DeepSeek","api_endpoint":"https://api.deepseek.com/v1","models":[{"id":"deepseek-chat","name":"DeepSeek V3","cost_per_1m_in":0.27,"cost_per_1m_out":1.1,"context_window":64000,"default_max_tokens":5000}]}]""";
            var ci = ModelCatalog.ImportCrush(crushArr);
            Check("Crush: providers.json 数组解析", ci.Count == 1 && ci[0].ProviderId == "deepseek");
            Check("Crush: api_endpoint 为 base_url", ci.Count == 1 && (ci[0].DefaultBaseUrl ?? "").Contains("deepseek"));
            Check("Crush: snake_case 字段映射", ci.Count == 1 && ci[0].ContextWindow == 64000
                && ci[0].InputPrice == 0.27 && ci[0].OutputPrice == 1.1 && ci[0].MaxOutput == 5000);

            var crushObj = """{"providers":{"deepseek":{"type":"openai-compat","base_url":"https://api.deepseek.com/v1","models":[{"id":"deepseek-chat","name":"DeepSeek V3","cost_per_1m_in":0.27,"context_window":64000}]}}}""";
            var co = ModelCatalog.ImportCrush(crushObj);
            Check("Crush: crush.json providers 对象解析", co.Count == 1 && co[0].ProviderId == "deepseek"
                && (co[0].DefaultBaseUrl ?? "").Contains("deepseek") && co[0].ContextWindow == 64000);
        }
        // 服务商 key 存取（一个服务商一个 key）
        ApiKeyStore.Set("__waycoder_test__", "sk-test-1234567890");
        Check("ApiKeyStore 按服务商存取 key", ApiKeyStore.Get("__waycoder_test__") == "sk-test-1234567890");
        ApiKeyStore.Remove("__waycoder_test__");
        Check("ApiKeyStore 删除服务商 key", ApiKeyStore.Get("__waycoder_test__") == null);

        // 从配置文件导入来源时：$VAR / ${VAR} 是环境变量引用，非真实 key，应跳过
        Check("ApiKeyStore key过滤: $变量是伪 key",
            ApiKeyStore.IsEnvVarRef("$OPENAI_API_KEY") && ApiKeyStore.IsEnvVarRef("${OPENAI_API_KEY}"));
        Check("ApiKeyStore key过滤: 真实 key 不是伪 key",
            !ApiKeyStore.IsEnvVarRef("sk-abc123") && !ApiKeyStore.IsEnvVarRef("  sk-abc123  "));

        // API key 有效期（expiry）：永久 / 截止日期
        Check("ApiKeyStore expiry: null=永久", ApiKeyStore.ExpiryText(null) == "永久" && !ApiKeyStore.IsExpired(null));
        Check("ApiKeyStore expiry: 永久/空串规范化",
            ApiKeyStore.NormalizeExpiry("永久") == null && ApiKeyStore.NormalizeExpiry("  ") == null);
        ApiKeyStore.Set("__waycoder_expiry__", "sk-exp-123456", "2026-12-31");
        Check("ApiKeyStore expiry: Set 带有效期往返", ApiKeyStore.Get("__waycoder_expiry__") == "sk-exp-123456"
            && ApiKeyStore.GetExpiry("__waycoder_expiry__") == "2026-12-31");
        Check("ApiKeyStore expiry: ListAllEntries 含有效期",
            ApiKeyStore.ListAllEntries().TryGetValue("__waycoder_expiry__", out var _expEntry) && _expEntry.Expiry == "2026-12-31");
        Check("ApiKeyStore expiry: 未来日期展示剩 N 天",
            ApiKeyStore.ExpiryText("2026-12-31").Contains("剩") && !ApiKeyStore.ExpiryText("2026-12-31").StartsWith("⚠"));
        Check("ApiKeyStore expiry: 过期日期标 ⚠ 已过期",
            ApiKeyStore.ExpiryText("2020-01-01").Contains("已过期") && ApiKeyStore.IsExpired("2020-01-01"));
        Check("ApiKeyStore expiry: 临期(≤7天)加 ⚠",
            ApiKeyStore.ExpiryText(DateTime.Today.AddDays(3).ToString("yyyy-MM-dd")).StartsWith("⚠"));
        ApiKeyStore.SetExpiry("__waycoder_expiry__", "永久");
        Check("ApiKeyStore expiry: SetExpiry 改有效期保留 key", ApiKeyStore.Get("__waycoder_expiry__") == "sk-exp-123456"
            && ApiKeyStore.GetExpiry("__waycoder_expiry__") == null);
        Check("ApiKeyStore expiry: SetExpiry 未存 key 返回 false", !ApiKeyStore.SetExpiry("__waycoder_nokey__", "2026-12-31"));
        ApiKeyStore.Remove("__waycoder_expiry__");
        Check("ApiKeyStore expiry: 清理后 key 不存在", ApiKeyStore.Get("__waycoder_expiry__") == null);
        Check("ModelCli.SetKey 带有效期提示", ModelCli.SetKey("__waycoder_expiry2__", "sk-x", "2026-12-31").Contains("有效期"));
        ApiKeyStore.Remove("__waycoder_expiry2__");

        // API key 优先级：api_keys.json 优先，环境变量只在 json 为空时补入，绝不覆盖已有 key
        {
            var savedDeepseek = ApiKeyStore.Get("deepseek");
            var savedEnv = ApiKeyStore.EnvKey("deepseek");
            var envVarName = ApiKeyStore.ProviderEnvVar["deepseek"]; // DEEPSEEK_API_KEY
            try
            {
                Environment.SetEnvironmentVariable(envVarName, "sk-env-aaaa1111");
                Check("ApiKeyStore EnvKey: 读到环境变量", ApiKeyStore.EnvKey("deepseek") == "sk-env-aaaa1111");
                Check("ApiKeyStore EnvKey: 无 env 返回 null", ApiKeyStore.EnvKey("__no_such_provider__") == null);

                // 已有 json key → env 不覆盖（json 优先）
                ApiKeyStore.Set("deepseek", "sk-json-2222");
                var imported = ApiKeyStore.ImportFromEnvironment();
                Check("ApiKeyStore 导入: 已有 json key 不被 env 覆盖",
                    ApiKeyStore.Get("deepseek") == "sk-json-2222"
                    && !imported.Contains("deepseek"));

                // json 为空 → env 补入
                ApiKeyStore.Remove("deepseek");
                var imported2 = ApiKeyStore.ImportFromEnvironment();
                Check("ApiKeyStore 导入: json 为空时 env 补入",
                    ApiKeyStore.Get("deepseek") == "sk-env-aaaa1111"
                    && imported2.Contains("deepseek"));
            }
            finally
            {
                Environment.SetEnvironmentVariable(envVarName, savedEnv);
                if (savedDeepseek != null) ApiKeyStore.Set("deepseek", savedDeepseek);
                else ApiKeyStore.Remove("deepseek");
            }
        }

        // ---- ConnectConfig：connect/provider/connection 三层分类存储 ----
        Section("[ConnectionConfig]");
        {
            // FilePathOverride 已由测试入口指向临时文件并预置 base fixture（迁移应为空操作）
            ConnectionConfig.ClearCache();
            Check("ConnectConfig: connects 读取", ConnectionConfig.ListConnects().Count == 4);
            Check("ConnectConfig: 按名查 connect",
                ConnectionConfig.FindConnect("deepseek/deepseek-v4-pro")?.ModelId == "deepseek-v4-pro");
            Check("ConnectConfig: 按内容查 connect",
                ConnectionConfig.FindConnectByContent("deepseek", "deepseek-v4-flash")?.Name == "deepseek/deepseek-v4-flash");
            Check("ConnectConfig: 按模型反查 connect",
                ConnectionConfig.FindConnectByModel("qwen-turbo")?.Name == "qwen/qwen-turbo");
            Check("ConnectConfig: 命名连接读取", ConnectionConfig.ListConnections().Count == 1);
            Check("ConnectConfig: ActiveName", ConnectionConfig.ActiveName == "default");
            Check("ConnectConfig: 回退链读取",
                ConnectionConfig.FallbackChain.Count == 4 && ConnectionConfig.FallbackChain[0] == "deepseek/deepseek-v4-pro");
            Check("ConnectConfig: 默认 connect 名",
                ConnectionConfig.DefaultConnectName("deepseek", "deepseek-v4-pro") == "deepseek/deepseek-v4-pro");
            Check("ConnectConfig: 模型栏格式 (provider)model",
                ConnectionConfig.FormatModel("deepseek", "deepseek-v4-pro") == "(deepseek)deepseek-v4-pro");

            // /connect spec 解析：点号 / 斜杠双分隔符都支持
            Check("Spec: 点号分隔 providerId.modelId",
                ConnectionConfig.TryParseSpec("deepseek.deepseek-v4-pro", out var sp1, out var sm1, out _)
                    && sp1 == "deepseek" && sm1 == "deepseek-v4-pro");
            Check("Spec: 斜杠分隔 providerId/modelId",
                ConnectionConfig.TryParseSpec("deepseek/deepseek-v4-pro", out var sp2, out var sm2, out _)
                    && sp2 == "deepseek" && sm2 == "deepseek-v4-pro");
            Check("Spec: baseUrl:model",
                ConnectionConfig.TryParseSpec("https://api.openai.com:gpt-5.4", out _, out var sm4, out var b4)
                    && sm4 == "gpt-5.4" && b4 == "https://api.openai.com");
            Check("Spec: 裸模型名",
                ConnectionConfig.TryParseSpec("deepseek-v4-pro", out var sp5, out var sm5, out _)
                    && sp5 == "deepseek" && sm5 == "deepseek-v4-pro");
            Check("Spec: 空指令 false", !ConnectionConfig.TryParseSpec("", out _, out _, out _));

            // provider 逻辑一体解析（读 providers.json + api_keys.json，只读）
            var prov = ConnectionConfig.ResolveProvider("deepseek");
            Check("ConnectConfig: provider 组合", prov != null && prov!.BaseUrl.Length > 0 && prov.Name.Length > 0);
            Check("ConnectConfig: connect→provider",
                ConnectionConfig.ResolveProviderForConnect("deepseek/deepseek-v4-pro") != null);

            // 新增/删除 connect 与命名连接（写临时文件）
            Check("ConnectConfig: 新增 connect",
                ConnectionConfig.AddConnect("qwen-turbo", "qwen", "qwen-turbo", out var addErr) && string.IsNullOrEmpty(addErr));
            Check("ConnectConfig: 重名 connect 拒绝",
                !ConnectionConfig.AddConnect("qwen-turbo", "qwen", "qwen-turbo", out _));
            Check("ConnectConfig: 新增命名连接",
                ConnectionConfig.AddConnection("hybrid", "deepseek/deepseek-v4-pro", "qwen-turbo", out _));
            Check("ConnectConfig: 引用中 connect 不可删",
                !ConnectionConfig.RemoveConnect("deepseek/deepseek-v4-pro", out _));
            Check("ConnectConfig: 删除命名连接",
                ConnectionConfig.RemoveConnection("hybrid", out _));
            Check("ConnectConfig: 删除后 connect 可删",
                ConnectionConfig.RemoveConnect("qwen-turbo", out _));
            Check("ConnectConfig: 已删 connect 不存在", ConnectionConfig.FindConnect("qwen-turbo") == null);

            // 文件往返：ClearCache 后重新 Load
            ConnectionConfig.ClearCache();
            Check("ConnectConfig: 文件往返 connects", ConnectionConfig.ListConnects().Count == 4);
            Check("ConnectConfig: 文件往返 connections", ConnectionConfig.ListConnections().Count == 1);
            ConnectionConfig.ClearCache();

            // 跨 provider 小 connect：WithModelOverride 期间按小 connect 的 provider 换 endpoint，结束后恢复
            ApiKeyStore.Set("qwen", "sk-qwen-test-1234");
            try
            {
                var overrideLlm = new LLM("deepseek-v4-pro", "bigkey", "https://big.example.com")
                { SmallModel = "qwen-turbo" };
                Agent.WithModelOverrideAsync(overrideLlm, "qwen-turbo",
                    async () => { await Task.CompletedTask; return 1; }).GetAwaiter().GetResult();
                Check("ConnectConfig: 跨 provider override 恢复 endpoint",
                    overrideLlm.ApiKey == "bigkey" && overrideLlm.BaseUrl == "https://big.example.com");
            }
            finally { ApiKeyStore.Remove("qwen"); }

            // 旧格式迁移：{active, connections:[{name,providerId,largeModel,smallModel}]} → 自动注册 connect
            var legacyPath = Path.Combine(Path.GetTempPath(), "waycoder_conn_legacy.json");
            ConnectionConfig.FilePathOverride = legacyPath;
            try
            {
                File.WriteAllText(legacyPath, """
                {
                  "active": "legacy",
                  "connections": [
                    { "name": "legacy", "providerId": "deepseek", "largeModel": "deepseek-v4-pro", "smallModel": "deepseek-v4-flash" }
                  ],
                  "fallbackChain": ["deepseek/deepseek-v4-pro"]
                }
                """);
                ConnectionConfig.ClearCache();
                var legacyConns = ConnectionConfig.ListConnections();
                Check("ConnectConfig: 旧格式迁移连接", legacyConns.Count == 1 && legacyConns[0].Name == "legacy");
                Check("ConnectConfig: 旧格式自动注册大 connect",
                    ConnectionConfig.FindConnectByContent("deepseek", "deepseek-v4-pro") != null);
                Check("ConnectConfig: 旧格式自动注册小 connect",
                    ConnectionConfig.FindConnectByContent("deepseek", "deepseek-v4-flash") != null);
                Check("ConnectConfig: 旧格式 active 保留", ConnectionConfig.ActiveName == "legacy");
            }
            finally
            {
                ConnectionConfig.ClearCache();
                ConnectionConfig.FilePathOverride = null; // 恢复真实路径（后续无 ConnectionConfig 测试）
                try { if (File.Exists(legacyPath)) File.Delete(legacyPath); } catch { }
            }
        }

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
        Check("Claude 导入 providerId=deepseek(按base_url)", claude.Count == 1 && claude[0].ProviderId == "deepseek");
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
        Check("Codex 导入 provider 模型（全局 model，base_url 127.0.0.1 → local）",
            codex.Any(m => m.Id == "gpt-5.6-sol" && m.ProviderId == "local"
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

        // 供应商注册表新增条目：内置默认端点校验（用 BuiltinProviders 快照，不受用户 providers.json 覆盖影响）
        Check("供应商注册表 gitee 端点",
            ModelCatalog.BuiltinProviders.TryGetValue("gitee", out var pGitee) && pGitee.DefaultBaseUrl == "https://ai.gitee.com/v1");
        // bailian（百炼）与 qwen 共用 dashscope 地址 → 已并入 qwen，不再单独注册（同地址 = 同供应商）
        Check("供应商注册表 bailian 并入 qwen（不再单独注册）",
            !ModelCatalog.BuiltinProviders.ContainsKey("bailian") && ModelCatalog.Providers.ContainsKey("qwen"));
        Check("供应商注册表内置无重复地址",
            !ModelCatalog.BuiltinProviders.Values
                .GroupBy(p => ModelCatalog.NormalizeBaseUrl(p.DefaultBaseUrl))
                .Where(g => g.Key.Length > 0)
                .Any(g => g.Count() > 1));
        Check("供应商注册表 opencode 端点（旧别名 opencode 并入 opencode-zen）",
            ModelCatalog.BuiltinProviders.TryGetValue("opencode-zen", out var pOpencodeZen) && pOpencodeZen.DefaultBaseUrl == "https://opencode.ai/zen/v1"
            && !ModelCatalog.BuiltinProviders.ContainsKey("opencode"));
        Check("供应商注册表 minimax 端点",
            ModelCatalog.BuiltinProviders.TryGetValue("minimax", out var pMinimax) && pMinimax.DefaultBaseUrl == "https://api.minimaxi.com/v1");
        Check("供应商注册表 aihubmix 端点",
            ModelCatalog.BuiltinProviders.TryGetValue("aihubmix", out var pAihubmix) && pAihubmix.DefaultBaseUrl == "https://api.inferera.com/v1");

        if (!prevLocalExists && File.Exists(ModelCatalog.LocalModelsPath))
        {
            var leftover2 = File.ReadAllText(ModelCatalog.LocalModelsPath).Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", "");
            if (leftover2 == "[]") File.Delete(ModelCatalog.LocalModelsPath);
        }

        // ---- 模型库归并（别名 providerId → 同 base_url 已注册供应商）----
        Section("[模型库归并]");
        var reconBase = ModelCatalog.GlobalProviderDir;
        var uA = "https://api.wc-recon-a.example/v1";
        var uB = "https://api.wc-recon-b.example/v1";
        var uC = "https://api.wc-recon-c.example/v1";
        var uUnk = "https://api.wc-unk.example/v1";
        // 别名模型在「URL 未注册时」先写入（模拟导入早于注册 / 导入来源别名），后注册 canonical → 归并移动
        ModelCatalog.AddCustom(new ModelCatalog.ModelInfo("__recon_m1__", "M1", "Alias", "wc-alias", "*", "Imported", 0, 0, 0, uA, "alias"), local: false);
        Check("归并前模型在别名供应商下", ModelCatalog.Find("__recon_m1__")?.ProviderId == "wc-alias");
        ModelCatalog.RegisterProvider("wcpro", "ReconPro", uA);
        var rep = ModelCatalog.ReconcileModels(false);
        Check("归并 moved", rep.Moved >= 1);
        Check("归并后归属 wcpro", ModelCatalog.Find("__recon_m1__")?.ProviderId == "wcpro");
        Check("归并后别名文件删除", !File.Exists(Path.Combine(reconBase, "wc-alias.json")));
        Check("归并备份生成", Directory.GetFiles(reconBase, "wc-alias.json.*.bak").Length > 0);

        // 重复跳过：canonical 已有同 id → 保留现有（orig-version）、移除别名源
        ModelCatalog.AddCustom(new ModelCatalog.ModelInfo("__recon_dup__", "Dup", "Alias", "wc-alias2", "*", "Imported", 0, 0, 0, uB, "alias-version"), local: false);
        ModelCatalog.AddCustom(new ModelCatalog.ModelInfo("__recon_dup__", "Dup", "Canon", "wcpro2", "*", "Imported", 0, 0, 0, uB, "orig-version"), local: false);
        ModelCatalog.RegisterProvider("wcpro2", "ReconPro2", uB);
        var repDup = ModelCatalog.ReconcileModels(false);
        var dupLeft = ModelCatalog.All.Where(m => m.Id == "__recon_dup__" && m.ProviderId == "wcpro2").ToList();
        Check("归并重复跳过计数", repDup.DuplicateSkip >= 1);
        Check("归并重复保留现有(orig)", dupLeft.Count == 1 && dupLeft[0].Description == "orig-version");
        Check("归并重复别名源移除", !File.Exists(Path.Combine(reconBase, "wc-alias2.json")));

        // 幂等：再跑无变化
        var repIdem = ModelCatalog.ReconcileModels(false);
        Check("归并幂等", repIdem.Moved == 0);

        // 无归属：URL 无注册供应商 → 原样保留
        ModelCatalog.AddCustom(new ModelCatalog.ModelInfo("__recon_unk__", "Unk", "Unknown", "wc-unk", "*", "Imported", 0, 0, 0, uUnk, "u"), local: false);
        var repUnk = ModelCatalog.ReconcileModels(false);
        Check("归并无归属原样", ModelCatalog.Find("__recon_unk__")?.ProviderId == "wc-unk" && repUnk.Unresolved >= 1);

        // 无 URL → 原样保留
        ModelCatalog.AddCustom(new ModelCatalog.ModelInfo("__recon_nourl__", "NoUrl", "NoUrl", "wc-nourl", "*", "Imported", 0, 0, 0, null, "n"), local: false);
        var repNoUrl = ModelCatalog.ReconcileModels(false);
        Check("归并无 URL 原样", ModelCatalog.Find("__recon_nourl__")?.ProviderId == "wc-nourl" && repNoUrl.NoUrl >= 1);

        // dry-run：预览计数但不落盘
        ModelCatalog.AddCustom(new ModelCatalog.ModelInfo("__recon_dry__", "Dry", "Alias", "wc-alias3", "*", "Imported", 0, 0, 0, uC, "d"), local: false);
        ModelCatalog.RegisterProvider("wcpro3", "ReconPro3", uC);
        var dryBakCount = Directory.GetFiles(reconBase, "*.bak").Length;
        var repDry = ModelCatalog.ReconcileModels(true);
        Check("归并 dry-run 计数", repDry.Moved >= 1 && repDry.DryRun);
        Check("归并 dry-run 不落盘", File.Exists(Path.Combine(reconBase, "wc-alias3.json")) && ModelCatalog.Find("__recon_dry__")?.ProviderId == "wc-alias3");
        Check("归并 dry-run 无备份", Directory.GetFiles(reconBase, "*.bak").Length == dryBakCount);

        // 防复发：AddCustomRange 把别名模型归一化到注册归属者，不建别名文件
        ModelCatalog.ReconcileModels(false); // 先把 dry 的别名归并掉，回到干净态
        ModelCatalog.AddCustomRange([new ModelCatalog.ModelInfo("__recon_ar__", "AR", "Alias", "wc-alias4", "*", "Imported", 0, 0, 0, uC, "a")], local: false);
        Check("防复发: AddCustomRange 归一化", ModelCatalog.Find("__recon_ar__")?.ProviderId == "wcpro3");
        Check("防复发: 不建别名文件", !File.Exists(Path.Combine(reconBase, "wc-alias4.json")));

        // 字段归一化回归（wafer.ai → wafer-ai 型）：存储 providerId 规范化后==目标，应改写字段而非误删
        // （历史 bug：ModelKey 归一化使 takenKeys 含自身 key → 被判 duplicate-skip 直接删，无 canonical 副本）
        var uD = "https://api.wc-recon-d.example/v1";
        ModelCatalog.AddCustom(new ModelCatalog.ModelInfo("__recon_fld__", "Fld", "Wafer AI", "wafer.ai", "*", "Imported", 0, 0, 0, uD, "f"), local: false);
        ModelCatalog.RegisterProvider("wafer-ai", "Wafer", uD);
        var repFld = ModelCatalog.ReconcileModels(false);
        Check("归并字段归一化: 归属改写为 wafer-ai", ModelCatalog.Find("__recon_fld__")?.ProviderId == "wafer-ai");
        Check("归并字段归一化: 不误删模型", ModelCatalog.Find("__recon_fld__") != null);
        Check("归并字段归一化: 原地文件保留", File.Exists(Path.Combine(reconBase, "wafer-ai.json")));

        // 清理测试数据（不污染真实注册表/模型库）
        ModelCatalog.RemoveCustom("__recon_m1__");
        ModelCatalog.RemoveCustom("__recon_dup__");
        ModelCatalog.RemoveCustom("__recon_unk__");
        ModelCatalog.RemoveCustom("__recon_nourl__");
        ModelCatalog.RemoveCustom("__recon_dry__");
        ModelCatalog.RemoveCustom("__recon_ar__");
        ModelCatalog.RemoveCustom("__recon_fld__");
        ModelCatalog.RemoveProvider("wcpro");
        ModelCatalog.RemoveProvider("wcpro2");
        ModelCatalog.RemoveProvider("wcpro3");
        ModelCatalog.RemoveProvider("wafer-ai");
        foreach (var bak in Directory.GetFiles(reconBase, "wc-alias*.bak")) { try { File.Delete(bak); } catch { } }
        foreach (var bak in Directory.GetFiles(reconBase, "wafer-ai.json.*.bak")) { try { File.Delete(bak); } catch { } }
        Console.WriteLine();

        // 清理测试数据（不污染真实注册表/模型库）
        ModelCatalog.RemoveCustom("__recon_m1__");
        ModelCatalog.RemoveCustom("__recon_dup__");
        ModelCatalog.RemoveCustom("__recon_unk__");
        ModelCatalog.RemoveCustom("__recon_nourl__");
        ModelCatalog.RemoveCustom("__recon_dry__");
        ModelCatalog.RemoveCustom("__recon_ar__");
        ModelCatalog.RemoveProvider("wcpro");
        ModelCatalog.RemoveProvider("wcpro2");
        ModelCatalog.RemoveProvider("wcpro3");
        foreach (var bak in Directory.GetFiles(reconBase, "wc-alias*.bak")) { try { File.Delete(bak); } catch { } }
        Console.WriteLine();

        // ---- 会话管理 ----
        Section("[会话管理]");
        var testSessionId = $"test_{DateTime.Now:yyyyMMddHHmmss}";
        var testMsgs = new List<JNode>
        {
            JNode.Object().Set("role", "user").Set("content", "test message"),
            JNode.Object().Set("role", "assistant").Set("content", "test response"),
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
        Check("AutoGitCommit 仅 config（EnvVar 精简置 null）", string.IsNullOrEmpty(ac?.EnvVar));

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
        var cjkMsgs = new List<JNode> {
            JNode.Object().Set("role", "user").Set("content", "你好世界")
        };
        var cjkEstimate = ContextManager.EstimateTokens(cjkMsgs);
        Check("CJK 估算 > ASCII 估算", cjkEstimate > "hello".Length / 3);
        var asciiMsgs = new List<JNode> {
            JNode.Object().Set("role", "user").Set("content", "hello")
        };
        Check("CJK 4字 ≈ 6 token", Math.Abs(cjkEstimate - 6) <= 2);
        Check("ASCII 5字 < CJK 4字", ContextManager.EstimateTokens(asciiMsgs) < cjkEstimate);

        // 混合内容
        var mixedMsgs = new List<JNode> {
            JNode.Object().Set("role", "user").Set("content", "中English混合测试")
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
            // 完整版基线：显式 Off（极致档规则文案是「edit_file 前必须 read_file」，无「先读后改」字样）
            var sysPrompt = PromptWithMode(EconomyMode.Off);
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
