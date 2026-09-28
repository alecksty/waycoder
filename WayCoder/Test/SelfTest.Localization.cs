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
            // 有效期英文侧的单复数（中文无此概念 ⇒ 英文必须自己成形）
            Check("i18n[en]: 有效期剩 1 天用单数 day",
                ApiKeyStore.ExpiryText(DateTime.Today.AddDays(1).ToString("yyyy-MM-dd")).Contains("1 day left"));
            Check("i18n[en]: 有效期剩 3 天用复数 days",
                ApiKeyStore.ExpiryText(DateTime.Today.AddDays(3).ToString("yyyy-MM-dd")).Contains("3 days left"));

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
                // 四个 opt-in 生成器（默认 Build 路径已英文化，这四个只在 Tiny/经济/规划模式下走）。
                // ⚠ **刻意传空工具表**：工具描述是**另一批的活**（还没英文化），传空表才能让判据
                //   落在「我们写的文案」上；否则工具的 `Description` 会把这条护栏变成"永远红"。
                ("Tiny 生成器", SystemPrompt.GenerateTiny([])),
                ("Extreme 生成器", SystemPrompt.GenerateExtreme([])),
                ("Economy 生成器", SystemPrompt.GenerateEconomy([])),
                ("Plan 生成器", SystemPrompt.GeneratePlan([])),
                // Architect **不在这里**：它的成品里注入仓库地图，而地图正文含数据（从项目 markdown
                // 抽出的标题、路径、符号名）—— 整份断言会被自己仓库的文档标题判成漏译（实测踩到）。
                // 它单独断言**模板本体**，见下面那两条。
                ("Architect 英文模板", SystemPrompt.EnglishArchitectTemplateForTest),
                // 有效期展示（ApiKeyStore.ExpiryText）—— 三态各自成形：永久 / 剩 N 天 / 已过期。
                // 它原先硬编码中文，消费方是 --model key 的输出（手机命令行页也能看到）。
                ("ApiKeyStore 有效期·永久", ApiKeyStore.ExpiryText(null)),
                ("ApiKeyStore 有效期·剩 N 天", ApiKeyStore.ExpiryText(DateTime.Today.AddDays(30).ToString("yyyy-MM-dd"))),
                ("ApiKeyStore 有效期·已过期", ApiKeyStore.ExpiryText("2020-01-01")),
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

            // ── ⑤ 公理 A2：拿「文案」当「判据」的地方，中英两份都要认 ──
            // 这是双语化里**唯一会静默损坏功能**的一类：文案翻了、判据没跟 ⇒ 判据恒假，
            // 不报错、不留痕。2026-09-28 实测踩到两处 ——
            //   ① `MauiVml` 的编译失败前缀被翻 ⇒ `WorkReporter` 的 `Contains("编译失败")` 恒假
            //      ⇒ 工作汇报的错误数静默少算；
            //   ② `VmlDiagnostics.Parse` 按字面量剥前缀 ⇒ 英文前缀剥不掉、气泡里多一截。
            // 这条判据从**消费侧**断：英文标记必须与中文标记一样被认出来。
            {
                // ⚠ 消息必须是 `role=assistant` **且带 tool_calls** —— 统计表只在
                //   `TotalActions > 0` 时才渲染（第一版喂了 role=tool 的消息，中英双红，
                //   差点把"判据形状写错"看成"功能真的坏了"）。
                static List<JNode> MsgWith(string content) =>
                [
                    JNode.Object()
                        .Set("role", "assistant")
                        .Set("content", content)
                        .Set("tool_calls", JNode.Array().Add(JNode.Object()
                            .Set("function", JNode.Object().Set("name", "bash").Set("arguments", "{}")))),
                ];

                // ⚠ 判据是「**错误计数 == 1**」，不是「那一行写着中文」——
                //   报告表格行本身也随语言走（`| ❌ 错误 |` / `| ❌ Errors |`），
                //   第一版拿 `Contains("| ❌ 错误 | 1 |")` 断言**英文**报告，
                //   等于要求"英文报告里必须是中文行"，把 A2 这个缺陷焊死了（实测被 agent 抓到）。
                //   正确做法：**各自按自己语言的行文案断言**。
                //
                // ⚠⚠ 而且这条**必须消费真实生产者**（`VmlDiagnostics.Parse`），不能硬编码那个英文串 ——
                //   硬编码的话，将来谁把 `VmlDiagnostics` 的措辞改成 `Compilation failed`，
                //   这条断言照样绿，而 `WorkReporter` 在真实路径上已经瞎了（**判据要盯住接缝，不是盯住字面量**）。
                //   路径：输入**只有宿主前缀**时，剥掉前缀后正文为空 ⇒ 走 `FirstLine` 的兜底
                //   `L.Pick("编译失败", "Compile failed")` —— 那正是 `WorkReporter` 要认的那个串。
                // 生产者 → 消费者**真的串起来**：先让 `VmlDiagnostics` 产出那句话，再喂给 `WorkReporter`。
                // 输入只有宿主前缀时剥掉前缀后正文为空 ⇒ 走 `FirstLine` 的兜底 `L.Pick("编译失败", "Compile failed")`。
                // ⚠ 中英**两侧要各自在对应语言下跑** —— 本段整体跑在 `L.Set(En)` 里，
                //   直接用「中文输入」是取不到中文产物的（`L.Pick` 只看当前语言）。
                //   第一版就是这么写的，中文侧假红了一次。
                var producedEn = VmlDiagnostics.Parse("⚠️ Compile failed: ");
                Check("A2: VmlDiagnostics 在这条路径上确实产出了标记串（否则下面的断言是空转）",
                    producedEn.Count > 0);
                if (producedEn.Count > 0)
                    Check("A2[en]: WorkReporter 认得 VmlDiagnostics **实际产出**的编译失败标记"
                          + $"（产出「{producedEn[0].Message}」；只认中文 ⇒ 英文会话的错误数静默少算）",
                        WorkReporter.Generate(MsgWith(producedEn[0].Message)).Contains("| ❌ Errors | 1 |"));

                L.Set(UiLang.Zh);
                try
                {
                    var producedZh = VmlDiagnostics.Parse("⚠️ 编译失败：");
                    Check("A2[zh]: VmlDiagnostics 在这条路径上产出了中文标记串",
                        producedZh.Count > 0 && producedZh[0].Message == "编译失败");
                    if (producedZh.Count > 0)
                        Check("A2[zh]: WorkReporter 认得 VmlDiagnostics 实际产出的编译失败标记",
                            WorkReporter.Generate(MsgWith(producedZh[0].Message)).Contains("| ❌ 错误 | 1 |"));
                }
                finally { L.Set(UiLang.En); }
            }

            // 教学模式块是**按开关追加**的，单独取一次（默认不开，上面那条走不到它）
            var teach = SystemPrompt.TeachBlockForTest;
            Check("教学模式块[en]: 无中文", !HasCjk(teach));

            // 项目检测的标签与单位词（**是文案**，可断言）：曾漏译成「- 语言: C#」「.vml(746文件)」
            Check("ProjectContext.ToMarkdown()[en]: 无中文",
                !HasCjk(new ProjectInfo { PrimaryLanguage = "C#", Languages = [".cs(1)"] }.ToMarkdown()));

            Check("Prompt[en]: 无残留占位符", !Regex.IsMatch(enPrompt, "__[A-Z_]+__"));

            // ── ④ 工具描述：**模型读的就是这些**，中英混排会削弱指令跟随 ──
            // 判据是「逐个参数都要有」，不是「抽几个看看」：漏一个的后果是英文会话里模型收到
            // 一句中文参数说明 —— 不报错、不留痕，只是变笨。这也是这批改动唯一能自动化的防线
            // （49 个工具散在 50 个文件里，靠人眼过一遍必漏）。
            var notTranslated = new List<string>();
            foreach (var t in tools)
            {
                if (HasCjk(t.Description ?? "")) notTranslated.Add($"{t.Name}.Description");
                var props = t.Parameters["properties"];
                if (props is null) continue;
                foreach (var (pname, pdef) in props.Entries)
                    if (HasCjk(pdef.GetString("description") ?? ""))
                        notTranslated.Add($"{t.Name}.{pname}");
            }
            if (notTranslated.Count > 0)
                Fail($"工具描述[en]: {notTranslated.Count} 处仍含中文 —— 例如 " +
                     string.Join("、", notTranslated.Take(10)));
            else
                Check("工具描述[en]: 全部无中文", true);

            // Architect 两份模板的占位符集合必须**逐一对应**。漏一个的后果是「某段动态内容
            // 在英文下永不注入」—— 不报错、不留痕，只是英文用户看到的提示词少一块。
            static HashSet<string> Placeholders(string t) =>
                Regex.Matches(t, "__[A-Z_]+__").Select(m => m.Value).ToHashSet(StringComparer.Ordinal);
            Check("Architect 模板: 中英占位符集合相等",
                Placeholders(SystemPrompt.EnglishArchitectTemplateForTest).SetEquals(
                    Placeholders(SystemPrompt.ChineseArchitectTemplateForTest)));
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
