namespace WayCoder.UI.Shared;

/// <summary>问卷选项 —— 纯数据，不依赖任何 TUI 控件类型（<c>UI/Shared/</c> 在 MAUI 也编译）。</summary>
/// <param name="Label">选项文本</param>
/// <param name="Description">一行说明（渲染在选项右侧固定列）</param>
/// <param name="IsOther">是否为「其他」项：选中回车后转入自定义输入（复用输入框），而非直接作答</param>
public sealed record SurveyOption(string Label, string? Description = null, bool IsOther = false);

/// <summary>
/// 行内问卷的一道题（一「页」或一「步」）。
///
/// 四端形态统一由「题目数 × MultiSelect × 是否显示标签页」决定：
///   · 1 题 + 单选 → 权限确认那种「多选一」
///   · 1 题 + 多选 → 勾选若干项（Space 勾选，Enter 提交）
///   · 多题 + ShowTabs → 横向多页（←→/Tab 翻页，页头是标签行）
///   · 多题 + 无标签 → 分步骤（Enter 逐步推进，页头是「步骤 k/n」）
///
/// 放在 <c>UI/Shared/</c> 而不是 <c>UI/TUI/Controls/</c>：<c>Tools/AskUserQuestionTool</c> 要用它，
/// 而 MAUI 排除了 <c>UI/TUI/**</c> 却编译 <c>Tools/**</c> —— 放错了就只在 MAUI 上编译失败。
/// </summary>
public sealed record SurveyQuestion(
    string Title,                      // 短标题（标签行/步骤行显示）
    string Question,                   // 完整问题（聊天流里已展示，这里备用）
    List<SurveyOption> Options,
    bool MultiSelect = false,
    bool AllowOther = true,            // 选项末尾自动追加「其他（自行输入）」——竞品默认有，模型给的选项未必覆盖用户想法
    bool AllowSkip = true);            // 单选页末尾再追加「跳过此题」——多选页空选本身即跳过，不重复占行

/// <summary>
/// 问卷结果：<see cref="Picks"/> 是每题选中的选项索引（-1 = 选了「其他」，答案文本在 <see cref="Others"/>），
/// <see cref="Others"/> 与题目一一对应（null = 该题没用「其他」）。两表等长。
/// </summary>
public sealed record SurveyResult(List<List<int>> Picks, List<string?> Others);
