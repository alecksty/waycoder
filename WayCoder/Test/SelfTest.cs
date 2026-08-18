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
        ["[UI Lint]"] = "ui",     ["[TuiTableList]"] = "ui",
        ["[TuiMarkup"] = "ui",       ["[CommandPalette 导航"] = "ui",
        ["[对话框 resize]"] = "ui",
        ["[窗口比例缩放]"] = "ui",  ["[窗口位置对齐]"] = "ui",  ["[Flex 布局]"] = "ui",
        // git
        ["[Git]"] = "git",         ["[Git "] = "git",         ["[Git PR]"] = "git",     ["[Git 大"] = "git",
        // config
        ["[配置]"] = "config",     ["[设置 Schema]"] = "config",["[配置读写]"] = "config",["[SaveToEnvFile]"] = "config",
        // memory
        ["[记忆]"] = "memory",     ["[记忆自动注入]"] = "memory",["[语义记忆]"] = "memory",
        // agent
        ["[Agent]"] = "agent",     ["[子智能体]"] = "agent",  ["[权限]"] = "agent",
        ["[权限系统"] = "agent",   ["[权限确认]"] = "agent",
        // review
        ["[代码审查]"] = "review",
        // mcp
        ["[MCP]"] = "mcp",         ["[MCP 环境变量]"] = "mcp",["[MCP HTTP]"] = "mcp",   ["[MCP 缓存]"] = "mcp",
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

        void Section(string title)
        {
            Console.WriteLine(title);
            _secEnabled = filter == null || filter.Any(f => title.StartsWith(f));
        }

        void Check(string name, bool condition)
        {
            if (!_secEnabled) return;
            if (condition) { passed++; Console.WriteLine($"  ✅ {name}"); }
            else { failed++; Console.WriteLine($"  ❌ {name}"); }
        }

        void Fail(string name)
        {
            failed++;
            Console.WriteLine($"  ❌ {name}");
        }

        Console.WriteLine("WayCoder 自测");
        Console.WriteLine("===================\n");

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

        // ---- 结果 ----
        Console.WriteLine($"\n通过: {passed}  失败: {failed}  总计: {passed + failed}");
        Console.WriteLine($"\n通过: {passed}  失败: {failed}  总计: {passed + failed}");
        return failed == 0;
    }
}
