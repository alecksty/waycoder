using WayCoder.UI.Tui.Controls;
using WayCoder.UI.TUI;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Screens;

/// <summary>
/// 标记版聊天屏幕 —— 用 tuidemo/chat.tui 声明式布局，复用 ChatScreen 全部交互逻辑。
///
/// 「布局写标记、交互写代码」：本类只做「加载标记 → Find(id) → 赋给基类属性」的接线，
/// 渲染 / 输入 / 滚动 / 对话框 / 槽位 / 会话等逻辑全部由 ChatScreen 基类继承。
///
/// 进入方式：`--tui-chat` 参数或 `WAYCODER_MARKUP_UI=1` 配置开关（测试通过后翻默认）。
/// </summary>
public class MarkupChatScreen : ChatScreen
{
    /// <summary>缓存的标记树：仅首次加载解析，resize 只重排不重解析。</summary>
    private TuiMarkupResult? _markup;

    /// <summary>标记版保留 ChatList 实例（resize 重排不重建）→ 基类跳过消息重灌，改用 RebuildChatItems 按新宽重建。</summary>
    protected override bool BuildLayoutPreservesChatItems => true;

    /// <summary>
    /// 构建布局：首次加载 chat.tui 并接线；后续（resize）只按终端尺寸重排，不重新解析文件。
    /// </summary>
    protected override void BuildLayout()
    {
        if (_markup == null)
        {
            _markup = TuiMarkup.LoadResource("chat.tui");

            TitleBar = _markup.Find<TuiTitleBar>("titleBar") ?? throw Missing("titleBar");
            StatusBar = _markup.Find<TuiStatusBar>("statusBar") ?? throw Missing("statusBar");
            ChatList = _markup.Find<TuiListView>("chatList") ?? throw Missing("chatList");
            InputArea = _markup.Find<TuiTextArea>("inputArea") ?? throw Missing("inputArea");
            InputArea.SyntaxHighlight = true; // 粘贴/输入代码自动语法高亮（Syntax.Detect 内容启发式）
            PromptBar = _markup.Find<TuiPromptBar>("promptBar") ?? throw Missing("promptBar");
            DynamicBar = _markup.Find<TuiDynamicBar>("dynamicBar") ?? throw Missing("dynamicBar");
            InputTopBorder = _markup.Find<TuiSeparator>("inputTopBorder") ?? throw Missing("inputTopBorder");
            InputBotBorder = _markup.Find<TuiSeparator>("inputBotBorder") ?? throw Missing("inputBotBorder");
            ModelInfoRow = _markup.Find<TuiSmartLabel>("modelInfoRow"); // 可空：动态栏放得下模型信息时整行隐藏
            _shortcutRow = _markup.Find<TuiSmartLabel>("shortcutRow"); // 模式栏下方快捷键行
            SuggestPanel = _markup.Find<TuiVBox>("suggestPanel") ?? throw Missing("suggestPanel");
            SidePanel = _markup.Find<TuiSidePanel>("sidePanel") ?? throw Missing("sidePanel");

            RootView = _markup.Screen?.RootView
                       ?? throw new InvalidOperationException("chat.tui 根元素应为 Screen");

            // ── 一次性 code-behind 接线（对应基类 BuildLayout 中代码侧的静态内容）──
            TuiInputHistory.SetPersistPath(Global.GlobalReadConfigPath("input_history.txt"));
            InputArea.OnSubmit = text =>
            {
                if (!string.IsNullOrWhiteSpace(text))
                    OnSubmit?.Invoke(text);
            };
            InputArea.CursorLineBg = 0;
            InputArea.CursorLineFg = TuiTheme.Current.TextAreaFg;
            InputTopBorder.LineColor = TuiTheme.Current.SeparatorFg;
            InputBotBorder.LineColor = TuiTheme.Current.SeparatorFg;
        }

        // 首次与 resize 共用：标记只声明结构，终端尺寸以 TW/TH 为准
        RootView.Width = TW;
        RootView.Height = TH;
        RootView.Layout();
    }

    /// <summary>resize 时按新宽度重建聊天项内容（复用 ChatList 实例，不能走 AddMessage 重灌以免重复）。</summary>
    protected override void RebuildChatItems()
    {
        int w = Math.Max(1, ChatList.Width - 2);
        for (int i = 0; i < ChatList.ItemCount; i++)
            if (ChatList.GetItem(i) is TuiListItem item)
                item.BuildContent(w);
        ChatList.ReLayout();
    }

    private static InvalidOperationException Missing(string id)
        => new($"chat.tui 缺少 id=\"{id}\" 的控件");
}
