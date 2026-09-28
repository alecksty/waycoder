using System.Text.RegularExpressions;
using WayCoder.Tools;
using WayCoder.UI.Shared;

namespace WayCoder;

/// <summary>
/// 双语化的护栏。**桌面自测是这个问题域唯一能自动化的防线** —— MAUI 工程不编进自测，
/// 界面里的每一行中文只能靠人眼发现漏翻，所以凡是能下沉到 <c>WayCoder/</c> 的文案与判定，
/// 都要在这里被钉住。
///
/// <para>
/// 本文件刻意用**黑盒判据**（拿组装后的成品文本断言），而不是去断言某个私有常量：
/// 判据若写在实现细节上，改结构就会失效；写在成品上，模板、注入的片段、拼接分隔符
/// 哪一处漏译都跑不掉。
/// </para>
/// </summary>
public static partial class SelfTest
{
    /// <summary>是否含 CJK 字符（中日韩统一表意文字）。</summary>
    private static bool HasCjk(string s) => Regex.IsMatch(s, "[一-鿿　-〿＀-￯]");

    /// <summary>双语化护栏。</summary>
    private static void TestLocalization(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("双语化：语言判定 / 文案对 / 提示词成品");

        var saved = L.Current;
        try
        {
            // ── ① 语言判定（纯函数，表驱动）──
            // 判据是「**整份列表里有没有中文**」，不是"只看第一个" ——
            // 地区=中国 + 语言=English 是常见组合，只看首位会把这批老用户突然变英文。
            Check("Lang: zh-Hans-CN → 中文", L.FromLanguageTags(["zh-Hans-CN"]) == UiLang.Zh);
            Check("Lang: zh 裸标签 → 中文", L.FromLanguageTags(["zh"]) == UiLang.Zh);
            Check("Lang: zh-Hant → 中文", L.FromLanguageTags(["zh-Hant"]) == UiLang.Zh);
            Check("Lang: zh_CN.UTF-8（Unix LANG 形态）→ 中文",
                L.FromLanguageTags(["zh_CN.UTF-8"]) == UiLang.Zh);
            Check("Lang: 大小写不敏感", L.FromLanguageTags(["ZH-hant"]) == UiLang.Zh);
            Check("Lang: en-US → 英文", L.FromLanguageTags(["en-US"]) == UiLang.En);
            Check("Lang: de-DE → 英文", L.FromLanguageTags(["de-DE"]) == UiLang.En);
            Check("Lang: 首个是 en、后面有 zh → 中文（扫整份列表）",
                L.FromLanguageTags(["en-US", "zh-Hant"]) == UiLang.Zh);
            Check("Lang: 带空白仍识别", L.FromLanguageTags(["  de-DE  "]) == UiLang.En);
            Check("Lang: 空列表 → 维持默认中文", L.FromLanguageTags([]) == UiLang.Zh);
            Check("Lang: null → 维持默认中文", L.FromLanguageTags(null) == UiLang.Zh);
            Check("Lang: 全空白 → 维持默认中文", L.FromLanguageTags(["", "  "]) == UiLang.Zh);
            // 反向：不要把形近的标签误判成中文（zho 是 ISO 639-3，不认；zhx 更不是）
            Check("Lang: 形近标签不误判（zhx → 英文）", L.FromLanguageTags(["zhx"]) == UiLang.En);
            Check("Lang: 空标签不误判", !L.IsChineseTag(""));

            // ── ② 共享文案：中英各自成对且互不串味 ──
            // 中文侧有 CJK、英文侧没有 —— 这条能抓「漏译」（英文分支直接抄了中文）
            // 与「翻串」（中文分支被英文污染）。
            L.Set(UiLang.Zh);
            var zhSamples = new (string Name, string Text)[]
            {
                ("RelativeTime 刚刚", UiText.RelativeTime(DateTime.Now, DateTime.Now)),
                ("RelativeTime 分钟", UiText.RelativeTime(DateTime.Now, DateTime.Now.AddMinutes(-5))),
                ("EconomyName", UiText.EconomyName(EconomyMode.On)),
                ("EconomyShortName", UiText.EconomyShortName(EconomyMode.Extreme)),
                ("PermDisplayName", UiText.PermDisplayName(PermissionManager.Mode.Yolo)),
                ("PermDesc", UiText.PermDesc(PermissionManager.Mode.SmartAuto)),
                ("PermFull", UiText.PermFull(PermissionManager.Mode.Auto)),
                ("PermCompact", UiText.PermCompact(PermissionManager.Mode.Ask)),
                ("PermLabelSpaced", UiText.PermLabelSpaced(PermissionManager.Mode.Yolo)),
                ("BtnAllow", UiText.BtnAllow),
                ("BtnCancel", UiText.BtnCancel),
            };
            foreach (var (name, text) in zhSamples)
                Check($"i18n[zh]: {name} 是中文", HasCjk(text));

            L.Set(UiLang.En);
            var enSamples = new (string Name, string Text)[]
            {
                ("RelativeTime 刚刚", UiText.RelativeTime(DateTime.Now, DateTime.Now)),
                ("RelativeTime 1 分钟（单数）", UiText.RelativeTime(DateTime.Now, DateTime.Now.AddMinutes(-1))),
                ("RelativeTime 5 分钟（复数）", UiText.RelativeTime(DateTime.Now, DateTime.Now.AddMinutes(-5))),
                ("RelativeTime 1 周（单数）", UiText.RelativeTime(DateTime.Now, DateTime.Now.AddDays(-8))),
                ("EconomyName", UiText.EconomyName(EconomyMode.On)),
                ("EconomyShortName", UiText.EconomyShortName(EconomyMode.Extreme)),
                ("PermDisplayName", UiText.PermDisplayName(PermissionManager.Mode.Yolo)),
                ("PermDesc", UiText.PermDesc(PermissionManager.Mode.SmartAuto)),
                ("PermFull", UiText.PermFull(PermissionManager.Mode.Auto)),
                ("PermCompact", UiText.PermCompact(PermissionManager.Mode.Ask)),
                ("PermLabelSpaced", UiText.PermLabelSpaced(PermissionManager.Mode.Yolo)),
                ("BtnAllow", UiText.BtnAllow),
                ("BtnCancel", UiText.BtnCancel),
            };
            foreach (var (name, text) in enSamples)
                Check($"i18n[en]: {name} 无 CJK/全角", !HasCjk(text));

            // 英文单复数：中文档无此概念，英文档必须有（这是"两侧各自成形"的直接判据）
            // ⚠ **必须用同一个 t0 派生 past**：写成 `RelativeTime(Now, Now.AddMinutes(-5))` 的两次
            //   取时钟 —— 第二次略晚 ⇒ `d` 比 5 分钟少一点点 ⇒ `(int)d.TotalMinutes` 截成 4
            //   ⇒ 断言随机器快慢偶发飘红（实测踩到）。测试自己不能是不确定的。
            var t0 = DateTime.Now;
            Check("i18n[en]: 1 minute 单数", UiText.RelativeTime(t0, t0.AddMinutes(-1)).Contains("1 minute ago"));
            Check("i18n[en]: 5 minutes 复数", UiText.RelativeTime(t0, t0.AddMinutes(-5)).Contains("5 minutes ago"));
            Check("i18n[en]: 1 week 单数", UiText.RelativeTime(t0, t0.AddDays(-8)).Contains("1 week ago"));

            // ── ③ 系统提示词成品：英文界面下不得残留任何中文，也不得残留未替换的占位符 ──
            // 这是**最强的一条**：模板、注进去的工作流/规则、教学模式块、Git 状态标签、
            // 以及工具清单那个全角冒号 —— 任何一处漏译都在这里现形。
            var tools = ToolRegistry.AllTools.ToList();

            L.Set(UiLang.Zh);
            var zhPrompt = SystemPrompt.Generate(tools);
            Check("Prompt[zh]: 是中文", HasCjk(zhPrompt));
            Check("Prompt[zh]: 无残留占位符", !Regex.IsMatch(zhPrompt, "__[A-Z_]+__"));

            L.Set(UiLang.En);
            var enPrompt = SystemPrompt.Generate(tools);

            // ⚠ 判据落在「**我们写的文案**」上，不落在「组装后成品的一切字节」上。
            //   成品里会合法地出现中文：Git 提交信息、项目指令正文（AGENT.md/CLAUDE.md）、
            //   记忆正文、文件路径 —— 那些是**数据**，不该也不能翻。
            //   第一版护栏整份断言，于是把它自己的中文提交信息判成了"漏译"（实测踩到）。
            var copySurfaces = new (string Name, string Text)[]
            {
                ("主模板", SystemPrompt.EnglishTemplateForTest),
                ("标准工作流", SystemPrompt.StandardWorkflow),
                ("快速工作流", SystemPrompt.FastModeWorkflow),
                ("标准规则1", SystemPrompt.StandardRule1),
                ("快速规则1", SystemPrompt.FastModeRule1),
                ("子智能体纪律", SystemPrompt.SubAgentDiscipline),
                ("计划模式前缀", WorkModeManager.GetModePrompt(WorkMode.Plan)),
                ("AgentStatus 压缩中", AgentStatusResolver.Resolve(
                    new AgentStatusInput(Busy: true, ToolName: null, Compressing: true, WaitingPermission: false,
                                         WaitingUser: false, WaitingSubagent: false, Mode: WorkMode.Build)).Text),
                ("AgentStatus 思考中", AgentStatusResolver.Resolve(
                    new AgentStatusInput(Busy: true, ToolName: null, Compressing: false, WaitingPermission: false,
                                         WaitingUser: false, WaitingSubagent: false, Mode: WorkMode.Build)).Text),
                ("AgentStatus 空闲", AgentStatusResolver.Resolve(
                    new AgentStatusInput(Busy: false, ToolName: null, Compressing: false, WaitingPermission: false,
                                         WaitingUser: false, WaitingSubagent: false, Mode: WorkMode.Build)).Text),
                ("AgentStatus 计划模式", AgentStatusResolver.Resolve(
                    new AgentStatusInput(Busy: false, ToolName: null, Compressing: false, WaitingPermission: false,
                                         WaitingUser: false, WaitingSubagent: false, Mode: WorkMode.Plan)).Text),
            };
            foreach (var (name, text) in copySurfaces)
            {
                if (HasCjk(text))
                {
                    var line = text.Split('\n').FirstOrDefault(l => HasCjk(l)) ?? "";
                    var head = line.Trim();
                    Fail($"{name}[en]: 不得含中文 —— 首处在：{head[..Math.Min(70, head.Length)]}");
                }
                else
                {
                    Check($"{name}[en]: 无中文", true);
                }
            }

            // 教学模式块是**按开关追加**的，单独取一次（默认不开，上面那条走不到它）
            var teach = SystemPrompt.TeachBlockForTest;
            Check("教学模式块[en]: 无中文", !HasCjk(teach));

            // 项目检测的标签与单位词（**是文案**，可断言）：曾漏译成「- 语言: C#」「.vml(746文件)」
            Check("ProjectContext.ToMarkdown()[en]: 无中文",
                !HasCjk(new ProjectInfo { PrimaryLanguage = "C#", Languages = [".cs(1)"] }.ToMarkdown()));

            Check("Prompt[en]: 无残留占位符", !Regex.IsMatch(enPrompt, "__[A-Z_]+__"));
            Check("Prompt[en]: 含工具名（工具清单确实注入了）",
                tools.Count == 0 || enPrompt.Contains(tools[0].Name, StringComparison.Ordinal));
            Check("Prompt[en]: 含关键区块标记", enPrompt.Contains("<critical_rules>") && enPrompt.Contains("<final_answers>"));
            Check("Prompt[en]: 明确身份", enPrompt.Contains("You are WayCoder"));

            // 反方向：中文成品必须仍含原来的关键串（防"顺手把中文分支也改了"）
            Check("Prompt[zh]: 仍含 <critical_rules>", zhPrompt.Contains("<critical_rules>"));
            Check("Prompt[zh]: 仍含「先读后改」", zhPrompt.Contains("先读后改"));
        }
        finally
        {
            L.Set(saved);
        }
    }
}
