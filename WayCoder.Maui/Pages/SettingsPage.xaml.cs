using WayCoder;
using WayCoder.Infra;

namespace WayCoder.Maui.Pages;

/// <summary>
/// 设置的**一级页**：只列分组与当前值摘要，点某行进二级页
/// （<see cref="SettingsGroupPage"/>，路由 `settingsgroup?group=xxx`）。
///
/// 摘要一律**现取**（`OnAppearing` 里重算），不缓存 —— 二级页改完返回时
/// 若拿旧值显示，用户会以为「改了没生效」。
/// </summary>
public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshSummaries();
    }

    private void RefreshSummaries()
    {
        var cfg = Config.Instance;

        // ── 模型：大/小 + Key 有没有配（没配 Key 是最常见的「不工作」原因，摘要里必须看得见）
        //
        // ⚠ **必须是一行**（用户定的：详情行只显示一行、多余的省略）。这里原来有个**写死的 `\n`**
        //   把「小模型」独立成第二行 ⇒ 这一张卡比别的都高，整页高矮不齐就是它造成的。
        //   合成一行后**放不下的会被截断**（`SettingsPage.xaml` 的摘要行有 `MaxLines=1`），
        //   所以**顺序按重要性排**：大模型 + 它的 Key 状态在最前，小模型跟在后
        //   （小模型默认就是跟随大模型，看不全不致命；点进二级页有全部）。
        var bigKey = DescribeKey(cfg.Provider);
        var smallKey = DescribeKey(cfg.SmallProvider);
        var small = string.IsNullOrEmpty(cfg.SmallModel) ? L.Pick("未设（跟随大模型）", "Not set (follows main model)") : cfg.SmallModel;
        ModelSummary.Text = L.Pick($"{cfg.Provider} · {cfg.Model}　{bigKey} · 小模型 {small}　{smallKey}", $"{cfg.Provider} · {cfg.Model}  {bigKey} · Small model {small}  {smallKey}");

        // ── 参数
        var ctx = cfg.MaxContextTokens > 0 ? $"{cfg.MaxContextTokens / 1024}K" : L.Pick("默认", "Default");
        var budget = cfg.MaxBudgetUsd is double b ? $"${b:F2}" : L.Pick("不限", "Unlimited");
        var economy = UiText.EconomyName(cfg.EconomyMode);
        ParamsSummary.Text = L.Pick($"上下文 {ctx} · 温度 {cfg.Temperature} · 预算 {budget} · 经济 {economy}", $"Context {ctx} · Temp {cfg.Temperature} · Budget {budget} · Economy {economy}");

        // ── 权限（文案唯一真源在 UiText，别在这儿再写一份措辞）
        PermSummary.Text = UiText.PermFull(PermissionManager.CurrentMode);

        // ── 存储（只管落盘位置，编辑器那半边已经拆成单独一张卡片）
        var where = WayCoder.Maui.MauiBootstrap.WorkspaceExternal
            ? L.Pick("外部存储 ✅", "External storage ✅")
            : L.Pick("App 私有目录 ⚠️", "App private storage ⚠️");
        StorageSummary.Text = L.Pick($"workspace：{where}", $"Workspace: {where}");

        // ── 编辑器：挑几个最常被问的显示。
        // 编码/换行那两项的文案走 MauiEditorStore.NameOf（**与设置页下拉同一份标签**），
        // 不在这里另写一份「UTF-8 无 BOM」之类的短名。
        var mb = (int)(Services.MauiEditorStore.ReadOnlyMaxBytes / (1024 * 1024));
        var full = Services.MauiEditorStore.FullWidthToHalf ? L.Pick("开", "On") : L.Pick("关", "Off");
        EditorSummary.Text = L.Pick(
            $"可编辑上限 {mb}MB · 全角转半角 {full} · " +
            $"保存 {Services.MauiEditorStore.NameOf(Services.MauiEditorStore.SaveAsEncoding)}" +
            $"/{Services.MauiEditorStore.NameOf(Services.MauiEditorStore.SaveAsNewline)}",
            $"Editable limit {mb}MB · Full-width to half-width {full} · " +
            $"Save {Services.MauiEditorStore.NameOf(Services.MauiEditorStore.SaveAsEncoding)}" +
            $"/{Services.MauiEditorStore.NameOf(Services.MauiEditorStore.SaveAsNewline)}");

        // ── 语音
        var wm = string.IsNullOrEmpty(cfg.WhisperModel) ? L.Pick("默认 whisper-1", "Default whisper-1") : cfg.WhisperModel;
        VoiceSummary.Text = wm;

        // ── 虚拟机：摘要那行由 `MauiVmStore.Summary()` 给（**取值与文案同一处** ——
        //    在这里再拼一遍必然出现"摘要写 16M、进去看到的是 8M"）
        VmSummary.Text = Services.MauiVmStore.Summary();

        // ── 编译：同虚拟机，摘要由 `MauiCompileStore.Summary()` 给（取值与文案同一处）
        CompileSummary.Text = Services.MauiCompileStore.Summary();

        // ── 全能版（内购）：标题与摘要都由 `EntitlementStore` 给 ——
        //    这里是**显示**，判据（买没买）在它那边，别在这儿自己判一次。
        FullTitle.Text = Services.EntitlementStore.ProductName;
        FullSummary.Text = Services.EntitlementStore.Summary();

        // ── 关于
        AboutSummary.Text = $"Dolaima {Global.Version}";
    }

    /// <summary>Key 状态一句话。空 → 明确说「未配」，别只留空白让人猜。</summary>
    private static string DescribeKey(string providerId)
    {
        try
        {
            var key = ApiKeyStore.Get(providerId);
            return string.IsNullOrEmpty(key)
                ? L.Pick("未配 Key ⚠️", "No key ⚠️")
                : L.Pick("已配 Key ✅", "Key set ✅");
        }
        catch
        {
            return "";
        }
    }

    private static Task Go(string group) =>
        Shell.Current.GoToAsync($"settingsgroup?group={group}");

    private async void OnModelTapped(object? sender, TappedEventArgs e) => await Go("model");
    private async void OnParamsTapped(object? sender, TappedEventArgs e) => await Go("params");
    private async void OnPermTapped(object? sender, TappedEventArgs e) => await Go("perm");
    private async void OnStorageTapped(object? sender, TappedEventArgs e) => await Go("storage");
    private async void OnEditorTapped(object? sender, TappedEventArgs e) => await Go("editor");
    private async void OnVoiceTapped(object? sender, TappedEventArgs e) => await Go("voice");
    private async void OnVmTapped(object? sender, TappedEventArgs e) => await Go("vm");
    private async void OnCompileTapped(object? sender, TappedEventArgs e) => await Go("compile");
    private async void OnFullTapped(object? sender, TappedEventArgs e) => await Go("full");

    private async void OnAboutTapped(object? sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync("about");
}
