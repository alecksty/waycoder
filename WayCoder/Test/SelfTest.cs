using System.Text;
using System.Text.Json;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.Tui.Screens;

namespace WayCoder;

/// <summary>
/// 标记测试模块归属，用于 SelfTest 自动推导 ModuleToSections。
/// 每个 Section("...") 对应的模块由 _sectionModuleMap 字典定义。
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class TestModuleAttribute(string module) : Attribute
{
    public string Module { get; } = module;
}

/// <summary>
/// 内置自测，通过 --test 或 -t 运行。
/// 无需外部测试框架，保持极简主义。
///
/// 新增 Section 时，只需在 _sectionModuleMap 中加一行（section 名 → 模块名），
/// ModuleToSections 自动推导，无需手动维护 switch。
/// </summary>
public static partial class SelfTest
{
    public static bool Run()
    {
        return RunWithFilter(null);
    }

    /// <summary>
    /// /test <模块> — 将测试结果捕获为字符串，返回聊天用文本。
    /// 模块: all | tools | ui | git | config | memory | agent | review | mcp | system
    /// </summary>
    public static string RunToChat(string module)
    {
        // "all" → null（全部），未知模块 → 错误
        HashSet<string>? sections;
        if (module.Equals("all", StringComparison.OrdinalIgnoreCase))
            sections = null;
        else
        {
            sections = ModuleToSections(module);
            if (sections == null)
                return $"❌ 未知模块: {module}\n可用: all, tools, ui, git, config, memory, agent, review, mcp, system";
        }

        var sb = new StringBuilder();
        var originalOut = Console.Out;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var writer = new StringWriter(sb) { NewLine = "\n" };
            Console.SetOut(writer);
            RunWithFilter(sections);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        sw.Stop();
        sb.AppendLine($"\n 耗时: {sw.Elapsed.TotalSeconds:F1}s");
        return sb.ToString();
    }

    /// <summary>
    /// /test <模块> — 按模块运行测试，返回结果摘要。
    /// 模块: all | tools | ui | git | config | memory | agent | review | mcp | system
    /// </summary>
    public static string RunModule(string module)
    {
        var sections = ModuleToSections(module);
        if (sections == null)
            return $"❌ 未知模块: {module}\n可用: all, tools, ui, git, config, memory, agent, review, mcp, system";

        var sb = new StringBuilder();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var ok = RunWithFilter(sections);
        sw.Stop();
        sb.AppendLine(ok ? "✅ 全部通过" : "❌ 存在失败");
        sb.AppendLine($"耗时: {sw.Elapsed.TotalSeconds:F1}s");
        return sb.ToString();
    }

    // ════════════════════════════════════════════════════════════
    // Section 名前缀 → 模块名 映射
    // 新增 Section 只加这里一行即可，ModuleToSections 自动推导。
    // 模块可通过 _moduleIncludes 包含其他模块的 sections（如 tools 包含 git）。
    // ════════════════════════════════════════════════════════════
    static readonly Dictionary<string, string> _sectionModuleMap = new()
    {
        // tools（独立工具测试）
        ["[工具注册"] = "tools",   ["[工具]"] = "tools",   ["[截图抓屏"] = "tools",
        ["[Fetch]"] = "tools",     ["[Todo]"] = "tools",      ["[LSP]"] = "tools",
        ["[Bash "] = "tools",      ["[Lint "] = "tools",      ["[Web "] = "tools",
        ["[Todo 依赖"] = "tools",  ["[工具循环检测"] = "tools", ["[文件修改时间守卫]"] = "tools",
        ["[孤儿工具修复]"] = "tools",
        // ui
        ["[CJK "] = "ui",          ["[语法高亮]"] = "ui",     ["[BoxBuffer]"] = "ui",
        ["[主题系统]"] = "ui",     ["[边框风格]"] = "ui",
        ["[EditorCore 选区锚点]"] = "ui", ["[EditorCore 行尾]"] = "ui", ["[Syntax ANSI 契约]"] = "ui",
        ["[文件编码 BOM]"] = "ui",        ["[文件编码 自动识别]"] = "ui",
        ["[编码转换]"] = "ui",
        ["[InputManager]"] = "ui", ["[ChatScreen主题]"] = "ui",["[TuiMenu]"] = "ui",
        ["[Markdown 表格]"] = "ui",["[TuiTreeView]"] = "ui",  ["[TuiRadioGroup]"] = "ui",
        ["[TuiComboBox]"] = "ui",  ["[TuiSeekBar]"] = "ui",   ["[TuiSeparator]"] = "ui",
        ["[TuiPanel]"] = "ui",     ["[EditorCore]"] = "ui",   ["[TuiRichEditor]"] = "ui",
        ["[EditorScreen]"] = "ui", ["[SettingsScreen]"] = "ui",
        ["[TuiButton]"] = "ui",   ["[TuiCheckbox]"] = "ui", ["[TuiInput]"] = "ui",
        ["[TuiTextArea]"] = "ui", ["[TuiLabel]"] = "ui",    ["[TuiIcon]"] = "ui",
        ["[TuiList]"] = "ui",     ["[TuiListView]"] = "ui", ["[TuiProgress]"] = "ui",
        ["[TuiSpinner]"] = "ui",  ["[TuiStatusBar]"] = "ui",["[TuiTabs]"] = "ui",
        ["[TuiTitleBar]"] = "ui", ["[TuiBanner]"] = "ui",   ["[TuiGrid]"] = "ui",
        ["[TuiWrapPanel]"] = "ui",["[TuiSidePanel]"] = "ui",["[TuiPromptBar]"] = "ui",
        ["[TuiDialog]"] = "ui",   ["[TuiControl]"] = "ui",  ["[TuiView]"] = "ui",
        ["[TuiScreen]"] = "ui",   ["[BoxBuffer]"] = "ui",   ["[AnsiColors]"] = "ui",
        ["[TuiTheme]"] = "ui",    ["[MarkdownRenderer]"] = "ui",
        ["[TuiTable]"] = "ui",    ["[DiffPreview]"] = "ui",  ["[UxHelper]"] = "ui",
        ["[终端实测宽度]"] = "ui", ["[设置对话框构建]"] = "ui",
        ["[UI Lint]"] = "ui",     ["[TuiTableList]"] = "ui",
        ["[TuiMarkup"] = "ui",       ["[CommandPalette 导航"] = "ui",
        ["[对话框 resize]"] = "ui",
        ["[窗口比例缩放]"] = "ui",  ["[窗口位置对齐]"] = "ui",  ["[Flex 布局]"] = "ui",
        ["[TuiMouse]"] = "ui",
        // git
        ["[Git]"] = "git",         ["[Git "] = "git",         ["[Git PR]"] = "git",     ["[Git 大"] = "git",
        // config
        ["[配置]"] = "config",     ["[设置 Schema]"] = "config",["[配置读写]"] = "config",["[SaveToEnvFile]"] = "config",
        ["[模型库归并]"] = "config",
        // memory
        ["[记忆]"] = "memory",     ["[记忆自动注入]"] = "memory",["[语义记忆]"] = "memory",
        // agent
        ["[Agent]"] = "agent",     ["[子智能体]"] = "agent",  ["[权限]"] = "agent",
        ["[权限系统"] = "agent",   ["[权限确认]"] = "agent", ["[AutoMode 智能分类器]"] = "agent",
        // review
        ["[代码审查]"] = "review",
        // mcp
        ["[MCP]"] = "mcp",         ["[MCP 环境变量]"] = "mcp",["[MCP HTTP]"] = "mcp",   ["[MCP 缓存]"] = "mcp",
        ["[MCP 目录]"] = "mcp",
        // system
        ["[LLM]"] = "system",      ["[系统提示词]"] = "system",["[JSON 辅助]"] = "system",
        ["[模型回退]"] = "system", ["[调试日志]"] = "system",  ["[项目检测]"] = "system",
        ["[上下文管理]"] = "system",["[预算系统]"] = "system",  ["[Hooks]"] = "system",
        ["[自定义命令]"] = "system",["[输入规范化]"] = "system",["[命令别名]"] = "system",
        ["[错误自恢复]"] = "system",["[Token 性能统计]"] = "system",["[HTTP 代理]"] = "system",
        ["[Sub-Agent"] = "system", ["[Tab 路径补全]"] = "system",["[输入历史]"] = "system",
        ["[模型热键切换]"] = "system",["[对话导出]"] = "system",["[最近文件]"] = "system",
        ["[会话管理]"] = "system", ["[会话 + 检查点]"] = "system",["[编辑器 Lint]"] = "system",
        ["[会话详情"] = "system", ["[CLI 参数"] = "system",
        ["[Lint 解析:"] = "system",["[Lint 诊断:"] = "system", ["[配置: EditorLint]"] = "system",
        ["[语法: 诊断背景色]"] = "system",["[诊断: Severity]"] = "system",["[诊断: Diagnostic]"] = "system",
        ["[Doctor]"] = "system", ["[沙箱管理]"] = "system",
    };

    // 模块包含关系（如 tools 测试也跑 git 的工具测试）
    static readonly Dictionary<string, string[]> _moduleIncludes = new()
    {
        ["tools"] = ["tools", "git"], // tools 模块包含 git 工具测试
    };

    /// <summary>
    /// 模块名 → Section 前缀集合。新增 section 只需修改 _sectionModuleMap。
    /// </summary>
    static HashSet<string>? ModuleToSections(string module)
    {
        if (module.Equals("all", StringComparison.OrdinalIgnoreCase)) return null;
        var names = _moduleIncludes.TryGetValue(module, out var includes)
            ? includes : new[] { module };
        var set = new HashSet<string>();
        foreach (var name in names)
        {
            foreach (var prefix in _sectionModuleMap.Where(kv => kv.Value == name).Select(kv => kv.Key))
                set.Add(prefix);
        }
        return set.Count > 0 ? set : null;
    }

    private static bool RunWithFilter(HashSet<string>? filter)
    {
        var passed = 0;
        var failed = 0;
        var _secEnabled = true;

        // ── 计时：统计每条 Check / 每个 Section 的耗时，末尾输出最慢项，便于定位慢测试去优化 ──
        // Check 收到的是调用方已算好的 bool，无法在 Check 内部包住被测逻辑；
        // 故用「上一次计时点（Section 起始 或 上一条 Check）到本条 Check」的间隔近似本条测试耗时
        // （含其前置 setup，仍能准确定位慢点）。Section 耗时独立按段累计，不受 Check 门控影响。
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var sectionAccum = new Dictionary<string, double>();  // Section 名 → 累计耗时 ms
        var itemTimes = new List<(string Name, double Ms)>(); // 单条测试耗时 ms
        string currentSection = "";
        double sectionStartMs = 0;
        double lastPointMs = 0;

        void Section(string title)
        {
            var now = sw.Elapsed.TotalMilliseconds;
            if (!string.IsNullOrEmpty(currentSection))
                sectionAccum[currentSection] = sectionAccum.GetValueOrDefault(currentSection) + (now - sectionStartMs);
            currentSection = title;
            sectionStartMs = now;
            lastPointMs = now; // 新段起始：避免把跨段 / 被过滤段的耗时算进某条 Check

            Console.WriteLine(title);
            _secEnabled = filter == null || filter.Any(f => title.StartsWith(f));
        }

        void Check(string name, bool condition)
        {
            if (!_secEnabled) return;
            var now = sw.Elapsed.TotalMilliseconds;
            var gap = now - lastPointMs;
            lastPointMs = now;
            if (gap >= 0.5) itemTimes.Add(($"{currentSection} ▸ {name}", gap));

            if (condition) { passed++; Console.WriteLine($"  ✅ {name}"); }
            else { failed++; Console.WriteLine($"  ❌ {name}"); }
        }

        void Fail(string name)
        {
            if (!_secEnabled) return;
            var now = sw.Elapsed.TotalMilliseconds;
            var gap = now - lastPointMs;
            lastPointMs = now;
            if (gap >= 0.5) itemTimes.Add(($"{currentSection} ▸ {name}", gap));

            failed++;
            Console.WriteLine($"  ❌ {name}");
        }

        Console.WriteLine("WayCoder 自测");
        Console.WriteLine("===================\n");

        // 自测全程隔离全局配置目录，避免 SessionManager/CheckpointManager 等写真实用户目录。
        var savedHomeOverride = Global.HomeOverride;
        var testHome = Path.Combine(Path.GetTempPath(), "waycoder_selftest_home_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(testHome);
        Global.HomeOverride = testHome;

        try
        {
        // ---- ConnectConfig 测试隔离：重定向 connections.json 到临时文件并预置完整新格式，
        //      避免迁移写真实 ~/.waycoder/connections.json 与 config.json/.env ----
        var connTmpPath = Path.Combine(Path.GetTempPath(), "waycoder_conn_selftest.json");
        ConnectionConfig.FilePathOverride = connTmpPath;
        ConnectionConfig.ClearCache();
        try
        {
            File.WriteAllText(connTmpPath, """
            {
              "active": "default",
              "connects": [
                { "name": "deepseek/deepseek-v4-pro", "providerId": "deepseek", "modelId": "deepseek-v4-pro" },
                { "name": "deepseek/deepseek-v4-flash", "providerId": "deepseek", "modelId": "deepseek-v4-flash" },
                { "name": "qwen/qwen-turbo", "providerId": "qwen", "modelId": "qwen-turbo" },
                { "name": "zhipu/glm-4-flash", "providerId": "zhipu", "modelId": "glm-4-flash" }
              ],
              "connections": [
                { "name": "default", "big": "deepseek/deepseek-v4-pro", "small": "deepseek/deepseek-v4-flash" }
              ],
              "fallbackChain": ["deepseek/deepseek-v4-pro", "deepseek/deepseek-v4-flash", "qwen/qwen-turbo", "zhipu/glm-4-flash"]
            }
            """);
        }
        catch { }

        // ---- 工具注册 ----
        TestChunk1(Section, Check, Fail);

        TestChunk2(Section, Check, Fail);

        TestChunk3(Section, Check, Fail);

        TestChunk4(Section, Check, Fail);

        TestChunk5(Section, Check, Fail);

        TestChunk6(Section, Check, Fail);

        TestChunk7(Section, Check, Fail);

        TestChunk8(Section, Check, Fail);

        TestChunk9(Section, Check, Fail);

        TestChunk10(Section, Check, Fail);

        TestChunk11(Section, Check, Fail);

        TestChunk12(Section, Check, Fail);

        TestChunk13(Section, Check, Fail);

        TestChunk14(Section, Check, Fail);

        TestChunk15(Section, Check, Fail);

        TestChunk16(Section, Check, Fail);

        TestChunk17(Section, Check, Fail);

        TestChunk18(Section, Check, Fail);

        // 清理 ConnectConfig 测试隔离
        ConnectionConfig.FilePathOverride = null;
        ConnectionConfig.ClearCache();
        try { if (File.Exists(connTmpPath)) File.Delete(connTmpPath); } catch { }
        }
        finally
        {
            Global.HomeOverride = savedHomeOverride;
            try { Directory.Delete(testHome, true); } catch { }
        }

        // ---- 结果 ----
        Console.WriteLine($"\n通过: {passed}  失败: {failed}  总计: {passed + failed}");

        // 结算最后一个 Section 的耗时
        var endMs = sw.Elapsed.TotalMilliseconds;
        if (!string.IsNullOrEmpty(currentSection))
            sectionAccum[currentSection] = sectionAccum.GetValueOrDefault(currentSection) + (endMs - sectionStartMs);
        sw.Stop();

        // ── 最慢项报告 ──
        Console.WriteLine("\n── 最慢 Section（耗时降序，前 5）──");
        var topSections = sectionAccum.OrderByDescending(kv => kv.Value).Take(5).ToList();
        if (topSections.Count == 0) Console.WriteLine("  (无)");
        foreach (var (name, ms) in topSections)
            Console.WriteLine($"  {FmtMs(ms)}  {name}");

        Console.WriteLine("\n── 最慢测试项（耗时降序，前 10，≥0.5ms）──");
        var topItems = itemTimes.OrderByDescending(t => t.Ms).Take(10).ToList();
        if (topItems.Count == 0) Console.WriteLine("  (全部 < 0.5ms)");
        foreach (var (name, ms) in topItems)
            Console.WriteLine($"  {FmtMs(ms)}  {name}");

        return failed == 0;
    }

    /// <summary>耗时格式化：≥100ms 整毫秒，否则保留 1 位小数</summary>
    private static string FmtMs(double ms) =>
        ms >= 100 ? $"{ms,7:F0}ms" : $"{ms,7:F1}ms";
}
