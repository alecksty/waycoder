using System.Diagnostics;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.Tui.Screens;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>
    /// 思考折叠（<see cref="ThinkStreamParser"/> 纯逻辑 + <see cref="ChatScreen"/> 状态机）
    /// 与聊天区行数上限（<see cref="Config.MaxChatLines"/>）。
    ///
    /// 折叠的收益不在「少显示几行」，而在**正文彻底不进渲染层**：
    /// 一段 50K 字符的推理 ≈ 上千行，留在 <c>TuiListView</c> 里就是每次 ReLayout/滚动都要付的钱。
    /// </summary>
    private static void TestChunk20(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        // ══ 解析器纯逻辑（规则单源 = Web app.js handleToken，此处逐条钉住）══
        Section("[思考流解析(ThinkStreamParser)]");

        static (List<string> Body, List<string> Think, int Starts, int Ends) Run(params string?[] tokens)
        {
            var p = new ThinkStreamParser();
            var body = new List<string>();
            var think = new List<string>();
            int starts = 0, ends = 0;
            foreach (var t in tokens)
                p.Feed(t, body.Add, think.Add, () => starts++, () => ends++);
            return (body, think, starts, ends);
        }

        var r1 = Run("你好");
        Check("思考流: 纯正文直通", r1.Body.Count == 1 && r1.Body[0] == "你好" && r1.Think.Count == 0);

        // 标准一圈：LLM 发 "\n«dim»" + 推理 + "«/»\n" + 答案
        var r2 = Run("\n«dim»", "我在推理", "…想完了", "«/»\n", "答案");
        Check("思考流: 推理进思考路", string.Join("", r2.Think) == "我在推理…想完了");
        Check("思考流: 正文不含推理", !string.Join("", r2.Body).Contains("我在推理")
            && string.Join("", r2.Body).Contains("答案"));
        Check("思考流: 开始/结束各触发一次", r2.Starts == 1 && r2.Ends == 1);

        // 块内嵌套标记（服务端超长时注入 «orange3»…«/»）不得被当成思考收尾 —— 逐层配对。
        // 分步喂：只在**内层 `«/»` 到达后、外层 `«/»` 到达前**检查「还没结束」才说明得了问题。
        {
            var p3 = new ThinkStreamParser();
            var body3 = new List<string>();
            var think3 = new List<string>();
            int starts3 = 0, ends3 = 0;
            p3.Feed("«dim»", body3.Add, think3.Add, () => starts3++, () => ends3++);
            p3.Feed("前半", body3.Add, think3.Add);
            p3.Feed("\n«orange3»… 思考内容过长，显示窗口受限«/»", body3.Add, think3.Add, null, () => ends3++);
            Check("思考流: 块内嵌套标记不提前收尾", ends3 == 0 && p3.InThinking);
            Check("思考流: 嵌套标记留在推理正文里配对", string.Join("", think3).Contains("«orange3»"));
            p3.Feed("后半", body3.Add, think3.Add);
            p3.Feed("«/»", body3.Add, think3.Add, null, () => ends3++);
            Check("思考流: 嵌套层归零才算结束",
                ends3 == 1 && starts3 == 1 && string.Join("", think3).Contains("后半"));
        }

        // ⚠ 块外的 «/» 必须原样保留：它是**所有** «» 标记的统一结束符，剥掉会让渲染器失配
        var r4 = Run("«cyan»正文«/»");
        Check("思考流: 块外 «/» 原样进正文", string.Join("", r4.Body) == "«cyan»正文«/»");

        // 标记与正文同 token
        var r5 = Run("答案«/»\n");
        Check("思考流: 同 token 里的标记被切开归位", string.Join("", r5.Body) == "答案«/»\n"
            && r5.Ends == 0 && r5.Starts == 0);

        // 已在思考块内再遇 «dim»：消费掉但不叠加层数（对齐 Web ensureThink 的早退）
        var r6 = Run("«dim»", "«dim»", "推理", "«/»");
        Check("思考流: 重复 «dim» 不叠加层数", r6.Starts == 1 && r6.Ends == 1
            && string.Join("", r6.Think) == "推理");

        // 异常中断后 Reset：下一轮 «dim» 必须能重新开启（否则整轮推理都漏进正文）
        {
            var p = new ThinkStreamParser();
            var body = new List<string>();
            var think = new List<string>();
            int starts = 0;
            p.Feed("«dim»推理", body.Add, think.Add, () => starts++);
            p.Reset();
            p.Feed("«dim»第二轮", body.Add, think.Add, () => starts++);
            Check("思考流: Reset 后能重新开启", starts == 2 && string.Join("", think).Contains("第二轮"));
        }

        // 显式数组：`Run(null)` 会被解析成「整个 params 数组为 null」而不是「一个 null 元素」
        Check("思考流: 空/null token 不出错",
            Run("").Body.Count == 0 && Run(new string?[] { null }).Starts == 0);

        // markupOpeners 与 Web 的正则 /«(?!\/)[^«»]*»/g 对齐
        Check("思考流: markupOpeners 只数开启标记",
            ThinkStreamParser.CountMarkupOpeners("«red»x«/»") == 1
            && ThinkStreamParser.CountMarkupOpeners("«a»«b»«/»«/»") == 2
            && ThinkStreamParser.CountMarkupOpeners("无标记") == 0);

        // ══ ChatScreen 状态机 ══
        Section("[思考折叠(ChatScreen)]");
        {
            var savedLines = Config.Instance.MaxChatLines;
            var savedMsgs = Config.Instance.MaxChatMessages;
            var savedTokens = Config.Instance.MaxChatTokens;
            var scr = new ChatScreen();
            scr.Activate();
            scr.ChatList.Width = 80;
            scr.ChatList.Height = 12;
            try
            {
                Config.Instance.MaxChatLines = 0;   // 本段只测折叠，行数裁剪单独测
                Config.Instance.MaxChatTokens = 0;
                Config.Instance.MaxChatMessages = 0;

                scr.StartAgentMsg();
                scr.AppendToken("\n«dim»");
                scr.AppendToken("我在推理");

                Check($"思考折叠: 思考中为标题+正文两行 (实际 {scr.ChatList.ItemCount})",
                    scr.ChatList.ItemCount == 2);
                var titleItem = scr.ChatList.GetItem(0) as TuiListItem;
                Check("思考折叠: 首行是「思考中」标题",
                    titleItem is { Role: "think" } && titleItem.MarkdownContent.Contains("思考中"));
                var bodyItem = scr.ChatList.GetItem(1) as TuiListItem;
                Check("思考折叠: 次行是推理正文",
                    bodyItem is { Role: "think" } && bodyItem.MarkdownContent == "我在推理");

                // 关键：思考消息必须插在流式 assistant 消息**之前**（AppendToken 依赖 [^1] 是流式 assistant）
                Check("思考折叠: 思考消息插在 assistant 之前（否则后续 token 被静默丢弃）",
                    scr.ChatMessages.Count == 2
                    && scr.ChatMessages[0].Role == "think"
                    && scr.ChatMessages[1] is { Role: "assistant", Streaming: true });

                scr.AppendToken("«/»\n");
                Check($"思考折叠: 定稿后正文行移除 (剩 {scr.ChatList.ItemCount} 项)", scr.ChatList.ItemCount == 1);
                var folded = scr.ChatList.GetItem(0) as TuiListItem;
                Check("思考折叠: 标题改写「已思考 N 秒」", folded?.MarkdownContent.StartsWith("💭 已思考") == true);
                Check("思考折叠: 正文留在内存供点开", folded?.DetailText == "我在推理");
                Check("思考折叠: 耗时已记录（≥1 秒）", folded?.ThinkingSeconds >= 1);

                // 折叠后正文继续：必须另起 assistant 项（不能写进「已思考」那行）
                scr.AppendToken("答案在此");
                var answer = scr.ChatList.GetItem(scr.ChatList.ItemCount - 1) as TuiListItem;
                Check("思考折叠: 思考后的正文另起 assistant 项",
                    answer is { Role: "assistant" } && answer.MarkdownContent == "答案在此");
                // 用 Contains 而非相等：`«/»\n` 的尾随换行也算正文片段（历史里留一个换行，无害）
                Check("思考折叠: 正文进了流式 ChatMsg（未被丢弃）",
                    scr.ChatMessages[^1] is { Role: "assistant" } tail && tail.Content.Contains("答案在此"));
            }
            finally
            {
                Config.Instance.MaxChatLines = savedLines;
                Config.Instance.MaxChatMessages = savedMsgs;
                Config.Instance.MaxChatTokens = savedTokens;
            }
            scr.Deactivate();
        }

        // 中断兜底：思考没收到 «/» 就被中断（取消/超时/异常），FinishAgentMsg 必须就地折叠
        {
            var scr = new ChatScreen();
            scr.Activate();
            scr.ChatList.Width = 80;
            scr.ChatList.Height = 12;
            scr.StartAgentMsg();
            scr.AppendToken("«dim»想一半");
            Check("思考折叠: 中断前正文行在位", scr.ChatList.ItemCount == 2);
            scr.FinishAgentMsg();
            Check($"思考折叠: 中断后强制折叠 (剩 {scr.ChatList.ItemCount} 项)", scr.ChatList.ItemCount == 1);
            Check("思考折叠: 中断也保住已推理内容",
                (scr.ChatList.GetItem(0) as TuiListItem)?.DetailText == "想一半");
            scr.Deactivate();
        }

        // 模型只思考没出正文（推理为空）：整块撤掉，不留一行「已思考」噪音
        {
            var scr = new ChatScreen();
            scr.Activate();
            scr.ChatList.Width = 80;
            scr.ChatList.Height = 12;
            scr.StartAgentMsg();
            scr.AppendToken("«dim»");
            scr.AppendToken("«/»");
            Check($"思考折叠: 无推理内容不留空行 (剩 {scr.ChatList.ItemCount} 项)", scr.ChatList.ItemCount == 0);
            Check("思考折叠: 无内容的思考消息也没进 ChatMessages",
                scr.ChatMessages.All(m => m.Role != "think"));
            scr.Deactivate();
        }

        // 工具消息到来：思考就地定稿（Web/MAUI 同规则）
        {
            var scr = new ChatScreen();
            scr.Activate();
            scr.ChatList.Width = 80;
            scr.ChatList.Height = 12;
            scr.StartAgentMsg();
            scr.AppendToken("«dim»推理内容");
            scr.FinishAgentMsg();
            scr.AddToolProgress("read_file", "main.c");
            var toolTitle = scr.ChatList.GetItem(scr.ChatList.ItemCount - 1) as TuiListItem;
            Check("思考折叠: 工具行接在思考行之后",
                toolTitle is { Role: "tool" } && scr.ChatList.ItemCount == 2);
            scr.Deactivate();
        }

        // ══ 槽位缓冲路径（非活跃槽位的 token 不进控件树，只落 ChatMsg；切回来重建）══
        Section("[思考折叠(槽位缓冲)]");
        {
            var slot = new AgentSlot();
            slot.BufferedStartStream();
            slot.BufferedAppendToken("\n«dim»");
            slot.BufferedAppendToken("槽位推理");
            slot.BufferedAppendToken("«/»\n");
            slot.BufferedAppendToken("槽位答案");

            Check("槽位缓冲: 思考消息 + 流式 assistant（顺序正确）",
                slot.ChatMessages.Count == 2
                && slot.ChatMessages[0] is { Role: "think" } tm && tm.ThinkingSeconds >= 1
                && slot.ChatMessages[0].Reasoning == "槽位推理"
                && slot.ChatMessages[1] is { Role: "assistant" } am && am.Content.Contains("槽位答案"));
            Check("槽位缓冲: 正文里没有裸标记（«dim»/«/» 已消费）",
                slot.ChatMessages.All(m => !(m.Content ?? "").Contains("«dim»")));

            var scr = new ChatScreen();
            scr.Activate();
            scr.ChatList.Width = 80;
            scr.ChatList.Height = 12;
            slot.RestoreTo(scr);
            Check($"槽位重建: 思考行折成一行（项数={scr.ChatList.ItemCount}）", scr.ChatList.ItemCount == 2);
            var rebuilt = scr.ChatList.GetItem(0) as TuiListItem;
            Check("槽位重建: 显示「已思考」而非裸推理",
                rebuilt is { Role: "think" } && rebuilt.MarkdownContent.Contains("已思考"));
            Check("槽位重建: 正文仍在（可点开）", rebuilt?.DetailText == "槽位推理");
            scr.Deactivate();
        }

        // 切走槽位时：进行中的思考必须就地定稿（否则切回来是一条永远「思考中」的死行）
        {
            var scr = new ChatScreen();
            scr.Activate();
            scr.ChatList.Width = 80;
            scr.ChatList.Height = 12;
            scr.StartAgentMsg();
            scr.AppendToken("«dim»进行中的思考");
            var slot2 = new AgentSlot();
            slot2.SaveFrom(scr);
            Check("槽位保存: 切走时进行中的思考被定稿",
                slot2.ChatMessages.Any(m => m.Role == "think" && m.ThinkingSeconds >= 1
                    && !m.Content.Contains("思考中")));
            scr.Deactivate();
        }

        // ══ 行数上限 ══
        Section("[聊天行数上限(MaxChatLines)]");
        {
            var savedLines = Config.Instance.MaxChatLines;
            var savedMsgs = Config.Instance.MaxChatMessages;
            var savedTokens = Config.Instance.MaxChatTokens;
            try
            {
                Config.Instance.MaxChatMessages = 0;   // 只留行数一条判据
                Config.Instance.MaxChatTokens = 0;
                Config.Instance.MaxChatLines = 100;    // 低水位 = 80

                var scr = new ChatScreen();
                scr.Activate();
                scr.ChatList.Width = 80;
                scr.ChatList.Height = 12;
                for (int i = 0; i < 400; i++)
                    scr.AddMessage($"行数测试消息 {i}", "user");

                Check($"行数上限: 裁到低水位内 (ContentHeight={scr.ChatList.ContentHeight} ≤ 80)",
                    scr.ChatList.ContentHeight <= 80);
                var last = scr.ChatList.GetItem(scr.ChatList.ItemCount - 1) as TuiListItem;
                Check("行数上限: 最新消息保留", last?.MarkdownContent.Contains("行数测试消息 399") == true);
                Check("行数上限: 至少留一项（不删空）", scr.ChatList.ItemCount >= 1);
                scr.Deactivate();

                // 单项自己就超上限：不能把它也删了（否则屏幕全空）
                var scr2 = new ChatScreen();
                scr2.Activate();
                scr2.ChatList.Width = 80;
                scr2.ChatList.Height = 12;
                Config.Instance.MaxChatLines = 50;
                scr2.AddMessage(string.Join("\n", Enumerable.Range(0, 200).Select(i => $"超长单条第 {i} 行")), "assistant");
                Check($"行数上限: 单项超限时不清空 (项数={scr2.ChatList.ItemCount})", scr2.ChatList.ItemCount == 1);
                scr2.Deactivate();
            }
            finally
            {
                Config.Instance.MaxChatLines = savedLines;
                Config.Instance.MaxChatMessages = savedMsgs;
                Config.Instance.MaxChatTokens = savedTokens;
            }
        }

        // ══ 满行数下的追加与翻页（用户点名要测：会不会越聊越卡）══
        Section("[聊天区性能(满行数)]");
        {
            var savedLines = Config.Instance.MaxChatLines;
            var savedMsgs = Config.Instance.MaxChatMessages;
            var savedTokens = Config.Instance.MaxChatTokens;
            try
            {
                Config.Instance.MaxChatMessages = 0;
                Config.Instance.MaxChatTokens = 0;
                Config.Instance.MaxChatLines = 500;

                var scr = new ChatScreen();
                scr.Activate();
                scr.ChatList.Width = 80;
                scr.ChatList.Height = 20;
                for (int i = 0; i < 600; i++)
                    scr.AddMessage($"历史消息 {i}", "user");
                Check($"性能: 600 条后行数被钳住 (ContentHeight={scr.ChatList.ContentHeight} ≤ 400)",
                    scr.ChatList.ContentHeight <= 400);

                var sw = Stopwatch.StartNew();
                for (int i = 0; i < 200; i++)
                    scr.AddMessage($"追加消息 {i}", "user");
                long addMs = sw.ElapsedMilliseconds;

                sw.Restart();
                for (int i = 0; i < 200; i++)
                {
                    scr.ChatList.ScrollUp(10);
                    scr.ChatList.ScrollDown(10);
                }
                long scrollMs = sw.ElapsedMilliseconds;

                // 翻页的真实代价在**渲染**上（标脏本身很便宜，重建一屏才是开销）
                sw.Restart();
                for (int i = 0; i < 50; i++)
                {
                    scr.ChatList.ScrollUp(5);
                    var frame = new System.Text.StringBuilder();
                    scr.Render(frame);
                }
                long renderMs = sw.ElapsedMilliseconds;

                // 阈值留足余量（实测值 ×~5），只拦「量级退化」——防的是越聊越卡，不是几十毫秒的抖动
                Check($"性能: 满行数下追加 200 条 {addMs}ms < 5000", addMs < 5000);
                Check($"性能: 满行数下翻页 400 次 {scrollMs}ms < 2000", scrollMs < 2000);
                Check($"性能: 满行数下滚动+渲染 50 帧 {renderMs}ms < 3000", renderMs < 3000);
                scr.Deactivate();
            }
            finally
            {
                Config.Instance.MaxChatLines = savedLines;
                Config.Instance.MaxChatMessages = savedMsgs;
                Config.Instance.MaxChatTokens = savedTokens;
            }
        }

        // ══ 详情窗口取正文（剥标记，与 Web stripMarkupTags 同规则）══
        Section("[思考详情(标记剥离)]");
        Check("思考详情: 剥掉颜色标记留原文",
            WayCoder.UI.Tui.Custom.ThinkDetail.StripMarkup("«red»错误«/»但«bold»这里«/»正常") == "错误但这里正常");
        Check("思考详情: 无标记原样返回", WayCoder.UI.Tui.Custom.ThinkDetail.StripMarkup("纯文本") == "纯文本");
        Check("思考详情: 未闭合的 « 原样保留",
            WayCoder.UI.Tui.Custom.ThinkDetail.StripMarkup("半截«标记") == "半截«标记");
        Check("思考详情: 空串不崩", WayCoder.UI.Tui.Custom.ThinkDetail.StripMarkup("") == "");
    }
}
