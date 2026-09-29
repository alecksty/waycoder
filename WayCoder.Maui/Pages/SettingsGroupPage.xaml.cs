using System.Text;
using System.Net.Http;
using System.Text.RegularExpressions;
using WayCoder;
using WayCoder.Infra;
using WayCoder.Maui.Services;
using WayCoder.UI.Shared;

namespace WayCoder.Maui.Pages;

/// <summary>
/// 设置的**二级页** —— 一份 XAML 承载全部分组（大模型/小模型/工具、参数、权限、存储与编辑器、语音），
/// 按 <see cref="Group"/> 只显示对口的那个容器。
///
/// 保存链路复用主工程配置 API（MAUI 已编译 Config/）：ApiKeyStore.Set 存密钥、
/// ConnectionConfig.ApplyModelChoice 切模型、Config.SaveToEnvFile 持久化、AgentService.Reset 重建 Agent。
/// </summary>
[QueryProperty(nameof(Group), "group")]
public partial class SettingsGroupPage : ContentPage
{
    /// <summary>分组 (Id, 标题, 容器)。设 <see cref="Group"/> 时按 <b>Id</b> 显示对应容器。
    /// ⚠ 判定一律按 **Id**，标题只用于显示 —— 原先按中文标题反查
    ///   （`MatchesGroupId` 里写着 `"model" =&gt; title == "模型"`），文案一翻分组路由立刻全断
    ///   （点「模型」卡片什么都不会发生，且零报错）。</summary>
    private IEnumerable<(string Id, string Title, View Box)> Groups()
    {
        yield return ("model", L.Pick("模型", "Model"), GrpModel);
        yield return ("params", L.Pick("参数", "Parameters"), GrpParams);
        yield return ("perm", L.Pick("权限", "Permission"), GrpPerm);
        yield return ("storage", L.Pick("存储", "Storage"), GrpStorage);
        yield return ("editor", L.Pick("编辑器", "Editor"), GrpEditor);
        yield return ("voice", L.Pick("语音", "Voice"), GrpVoice);
        yield return ("vm", L.Pick("虚拟机", "VM"), GrpVm);
        yield return ("compile", L.Pick("编译", "Compile"), GrpCompile);
        // 「全能版」的位置是**有意靠后**的：它是内购，不该在设置页第一屏就推销；
        // 但也不能藏 —— 用户编译不了想用的语言时会主动来找（拦截提示里写了路径）。
        yield return ("full", L.Pick("全能版", "Full Edition"), GrpFull);
    }

    private string _group = "model";

    /// <summary>Shell 路由参数：`settingsgroup?group=params` 之类。</summary>
    public string Group
    {
        get => _group;
        set
        {
            _group = string.IsNullOrEmpty(value) ? "model" : value;
            ApplyGroup();
        }
    }

    /// <summary>
    /// 按 <see cref="Group"/> 只显示对口容器。
    /// ⚠ 先把**全部**设 False 再打开目标那个 —— 若不这样，靠路由参数切到另一个分组时
    /// 上一个分组的容器会留在树上（Shell 会复用页面实例），两个分组叠着显示。
    /// </summary>
    private void ApplyGroup()
    {
        bool matched = false;
        foreach (var (id, title, box) in Groups())
        {
            bool hit = id == _group;
            box.IsVisible = hit;
            if (hit) { Title = title; matched = true; }
        }
        if (!matched)
        {
            GrpModel.IsVisible = true;
            Title = L.Pick("模型", "Model");
        }
    }

    /// <summary>服务商下拉项（展示名 + 内部 id）。</summary>
    private sealed record ProviderOption(string Id, string DisplayName);

    /// <summary>权限模式标签（索引与 <see cref="PermissionManager.Mode"/> 枚举顺序一致）。
    /// 从 <see cref="UiText.PermFull"/> 派生 —— 文案唯一真源，别再手维护一份数组
    /// （此前手机上一套措辞、桌面又一套，同一个 Ask 有 5 种叫法）。</summary>
    private static readonly string[] PermModeLabels =
        Enum.GetValues<PermissionManager.Mode>().Select(UiText.PermFull).ToArray();

    public SettingsGroupPage()
    {
        InitializeComponent();
        ApplyGroup();
    }

    /// <summary>可编辑上限的候选档位（MB）。0 只是占位，实际不允许 0。</summary>
    private static readonly int[] EditorLimitOptions = [1, 2, 4, 8, 16, 32];

    // 保存编码/换行的候选表**不在这里**：挪到 MauiEditorStore.SaveEncodingOptions /
    // SaveNewlineOptions —— 设置页的首页摘要也要用同一份标签，两处各写一遍迟早漂。
    //
    // ⚠ **刻意是表达式体属性，不是 `static readonly` 字段**（2026-09-28 改）：那两份表现在
    //   走 `L.Pick`（标签要跟界面语言），而 `static readonly` 会把值**快照在类型初始化那一刻**
    //   —— 正是 `Lang.cs` 的公理 A3 禁止的形态。当前启动顺序下（`MauiLang.Initialize()` 早于
    //   任何页面构造）看不出问题，但那正是「今天对、明天错」：谁把这两张表的首次访问提前
    //   （比如在 App 构造期预热设置页），语言就被冻死，且**只在某些启动顺序下复现**。
    //   代价是每次访问重建一次数组 —— 只发生在进入设置页/回填控件时，非热路径。
    private static (string Label, Services.MauiEditorStore.SaveEncoding Value)[] EncOptions =>
        Services.MauiEditorStore.SaveEncodingOptions;

    private static (string Label, Services.MauiEditorStore.SaveNewline Value)[] NlOptions =>
        Services.MauiEditorStore.SaveNewlineOptions;

    private bool _loadingEditorSettings;

    /// <summary>把编辑器设置填进控件（只在进入页面时做，避免覆盖用户正在改的值）。</summary>
    private void LoadEditorSettings()
    {
        _loadingEditorSettings = true;
        try
        {
            int mb = (int)(Services.MauiEditorStore.ReadOnlyMaxBytes / (1024 * 1024));
            int idx = Array.IndexOf(EditorLimitOptions, mb);
            if (idx < 0) idx = 1;   // 默认 2MB
            EditorLimitPicker.ItemsSource = EditorLimitOptions.Select(m => $"{m} MB").ToList();
            EditorLimitPicker.SelectedIndex = idx;
            EditorDebugSwitch.IsToggled = Services.MauiEditorStore.ShowDebugHud;
            EditorBubbleCharsEntry.Text = Services.MauiEditorStore.BubbleChars.ToString();
            EditorBubblesSwitch.IsToggled = Services.MauiEditorStore.DefaultExpandBubbles;
            EditorFullWidthSwitch.IsToggled = Services.MauiEditorStore.FullWidthToHalf;

            EditorEncodingPicker.ItemsSource = EncOptions.Select(o => o.Label).ToList();
            EditorEncodingPicker.SelectedIndex = Array.FindIndex(EncOptions,
                o => o.Value == Services.MauiEditorStore.SaveAsEncoding);
            if (EditorEncodingPicker.SelectedIndex < 0) EditorEncodingPicker.SelectedIndex = 0;

            EditorNewlinePicker.ItemsSource = NlOptions.Select(o => o.Label).ToList();
            EditorNewlinePicker.SelectedIndex = Array.FindIndex(NlOptions,
                o => o.Value == Services.MauiEditorStore.SaveAsNewline);
            if (EditorNewlinePicker.SelectedIndex < 0) EditorNewlinePicker.SelectedIndex = 0;
        }
        finally { _loadingEditorSettings = false; }
    }

    private void OnEditorLimitChanged(object? sender, EventArgs e)
    {
        if (_loadingEditorSettings) return;
        int idx = EditorLimitPicker.SelectedIndex;
        if (idx < 0 || idx >= EditorLimitOptions.Length) return;
        Services.MauiEditorStore.SetReadOnlyMaxMB(EditorLimitOptions[idx]);
    }

    private void OnEditorDebugToggled(object? sender, ToggledEventArgs e)
    {
        if (_loadingEditorSettings) return;
        Services.MauiEditorStore.SetDebugHud(e.Value);
    }

    /// <summary>
    /// 气泡每行字数。**失焦与回车都走这一个处理器**（用户可能改完直接点走）。
    ///
    /// 无论输入是否合法，都把框里的值**回写成人话**（夹取后的真实值）——
    /// 不回写的话，用户输入 `5` 之后框里留着 5、实际生效 16，两边对不上，
    /// 而"设置没生效"这种印象最难查。范围由 `MauiEditorStore.ClampBubbleChars` 一处夹取。
    /// </summary>
    private void OnEditorBubbleCharsCompleted(object? sender, EventArgs e)
    {
        if (int.TryParse(EditorBubbleCharsEntry.Text, out int n))
            Services.MauiEditorStore.SetBubbleChars(n);
        EditorBubbleCharsEntry.Text = Services.MauiEditorStore.BubbleChars.ToString();
    }

    private void OnEditorFullWidthToggled(object? sender, ToggledEventArgs e)
    {
        if (_loadingEditorSettings) return;
        Services.MauiEditorStore.SetFullWidthToHalf(e.Value);
    }

    /// <summary>
    /// 诊断气泡的**总开关**（全部展开 / 全部收起）—— 它定的是**默认值**，不是「能不能显示」：
    /// 关掉之后每条错误收成一个小圆点（点圆点仍可单独展开那一条）。
    ///
    /// 与 ✕ 的「单独收起」互相独立：这里**不动** `DiagnosticManager` 里任何一条诊断，
    /// 也不清用户逐条的选择 —— 所以重新打开总开关时，之前用 ✕ 收起的那几条仍然是收起的。
    /// </summary>
    private void OnEditorBubblesToggled(object? sender, ToggledEventArgs e)
    {
        if (_loadingEditorSettings) return;
        Services.MauiEditorStore.SetDefaultExpandBubbles(e.Value);
    }

    private void OnEditorEncodingChanged(object? sender, EventArgs e)
    {
        if (_loadingEditorSettings) return;
        int i = EditorEncodingPicker.SelectedIndex;
        if (i >= 0 && i < EncOptions.Length)
            Services.MauiEditorStore.SetSaveEncoding(EncOptions[i].Value);
    }

    private void OnEditorNewlineChanged(object? sender, EventArgs e)
    {
        if (_loadingEditorSettings) return;
        int i = EditorNewlinePicker.SelectedIndex;
        if (i >= 0 && i < NlOptions.Length)
            Services.MauiEditorStore.SetSaveNewline(NlOptions[i].Value);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadEditorSettings();
        LoadConfig();
        LoadVmSettings();
        LoadCompileSettings();
        LoadFullSettings();
    }

    /// <summary>
    /// 载入「全能版」这一组。
    ///
    /// <para>
    /// ⚠ <b>语言清单是从唯一真源推出来的，不是手打的</b>：全部 = <see cref="VmlFrontendCompilerList.All"/>
    /// （它已经被桌面自测与手机端的注册断言双向钉住），免费那部分 = <see cref="FreeTierPolicy"/>。
    /// 手打一份"22 门语言"的名单在这里，就又多了一张要人记得同步的表 ——
    /// 商店页描述、这里的说明、编译被拦时的提示，三处必须说同一件事。
    /// </para>
    ///
    /// <para>
    /// ⚠ 免费版<b>不显示「优化级别」这一项</b>（由 <c>GrpCompile</c> 那边隐藏）——
    /// 「显示了但暗地里按 0 跑」是用户最容易当成 bug 的形态。这里只说明它属于全能版。
    /// </para>
    /// </summary>
    private void LoadFullSettings()
    {
        bool full = Services.EntitlementStore.IsFull;

        FullStatusLabel.Text = full
            ? L.Pick("✅ 已解锁全能版", "✅ Full Edition unlocked")
            : L.Pick("🔒 未解锁（当前是免费版）", "🔒 Locked (free edition)");

        var free = VmlFrontendCompilerList.All.Where(e => FreeTierPolicy.IsLanguageFree(e.Name))
                                              .Select(e => e.Name).ToList();
        var paid = VmlFrontendCompilerList.All.Where(e => !FreeTierPolicy.IsLanguageFree(e.Name))
                                              .Select(e => e.Name).ToList();

        FullFreeLabel.Text = L.Pick(
            $"免费版包含：代码编辑器、{string.Join(" / ", free)} 语言编译运行、VML 汇编（.vml）编译运行。",
            $"Free edition: the code editor, {string.Join(" / ", free)} compilation, and VML assembly (.vml).");

        FullPaidLabel.Text = L.Pick(
            $"全能版解锁：另外 {paid.Count} 门语言（{string.Join(" / ", paid)}）+ 优化器（O1 / O2 / O3）。",
            $"Full Edition unlocks {paid.Count} more languages ({string.Join(" / ", paid)}) plus the optimizer (O1 / O2 / O3).");

        bool supported = Services.Iap.Supported;
        var price = Services.Iap.PriceText;
        FullBuyBtn.Text = full
            ? L.Pick("已解锁", "Unlocked")
            : price is null
                ? L.Pick($"购买 {Services.EntitlementStore.ProductName}", $"Buy {Services.EntitlementStore.ProductName}")
                : L.Pick($"购买 {Services.EntitlementStore.ProductName} · {price}",
                         $"Buy {Services.EntitlementStore.ProductName} · {price}");
        FullBuyBtn.IsEnabled = supported && !full;
        FullRestoreBtn.Text = L.Pick("恢复购买", "Restore Purchase");
        FullRestoreBtn.IsEnabled = supported && !full;   // 已解锁就不必恢复；换设备时会先是未解锁态

        FullHintLabel.Text = supported
            ? L.Pick("一次性买断，可跨设备恢复（非消耗型内购）。换手机或重装后点「恢复购买」即可找回。",
                     "One-time purchase, restorable across devices. After switching phones or reinstalling, tap Restore Purchase.")
            : L.Pick("本平台没有内购。", "In-app purchase is not available on this platform.");
    }

    // ── 内购两个按钮 ──
    //
    // ⚠ 都是 `async void`（事件签名定死），**必须自己 try/catch 兜住** ——
    //   抛出去就是进程级未处理异常，在 MAUI 的几条路径上还会被先吞掉，
    //   现场只留一行看不懂的日志（本仓 v0.96.171 记过同一个形态）。

    private async void OnBuyClicked(object? sender, EventArgs e) => await RunIapAsync(buy: true);

    private async void OnRestoreClicked(object? sender, EventArgs e) => await RunIapAsync(buy: false);

    private async Task RunIapAsync(bool buy)
    {
        FullBuyBtn.IsEnabled = false;
        FullRestoreBtn.IsEnabled = false;
        try
        {
            var r = buy
                ? await Services.Iap.PurchaseAsync(Services.EntitlementStore.ProductId)
                : await Services.Iap.RestoreAsync();

            // ⚠ 一律**再读一次** IsFull 当结论，别拿 r.Ok 当解锁判据 ——
            //   两者在"恢复购买但一件都没买过"时正好相反（操作成功、资格没有）。
            await DisplayAlertAsync(
                Services.EntitlementStore.IsFull
                    ? L.Pick("全能版", "Full Edition")
                    : L.Pick("购买未完成", "Purchase not completed"),
                r.Message,
                L.Pick("确定", "OK"));
        }
        catch (Exception ex)
        {
            ErrorLog.Warning("[IAP]", $"内购操作异常：{ex}");
            await DisplayAlertAsync(L.Pick("出错了", "Error"), ex.Message, L.Pick("确定", "OK"));
        }
        finally
        {
            LoadFullSettings();
        }
    }

    /// <summary>
    /// 载入「虚拟机」分组的四项（内存 / 栈 / 两个超时）。
    ///
    /// 这些值存在 **Preferences**（<see cref="MauiVmStore"/>）而不是 `config.json` ——
    /// 它们只对**手机端**有意义（桌面 `vmlcli` 走命令行参数），塞进共享配置反而要在两端各解释一遍。
    /// </summary>
    private void LoadVmSettings()
    {
        SelectOption(VmMemoryPicker, MauiVmStore.MemoryOptions, MauiVmStore.MemoryMb, v => $"{v} MB");
        SelectOption(VmStackPicker, MauiVmStore.StackOptions, MauiVmStore.StackKb, v => $"{v} KB");
        SelectOption(VmEditorTimeoutPicker, MauiVmStore.TimeoutOptions, MauiVmStore.EditorTimeoutSec, MauiVmStore.TimeoutText);
        SelectOption(VmShellTimeoutPicker, MauiVmStore.TimeoutOptions, MauiVmStore.ShellTimeoutSec, MauiVmStore.TimeoutText);

        // 超时的语义在 v0.96.438 变了（墙钟 → **连续执行**），设置项旁边必须说清楚，
        // 否则用户会以为"调大了就能让程序跑更久"，而真正的原因是**等输入已经不计时了**。
        VmHintLabel.Text = L.Pick("超时只在程序连续运行、一次都没等待时计时；等消息/等弹框/**触摸与按键**都不算。选「不限」= 不设兜底。", "The timeout counts only continuous execution; waiting for messages, waiting on a dialog, and **touch or key input** all pause it. Choosing \"Unlimited\" means there is no fallback timeout.")
                         + L.Pick("内存与栈改完，下一次运行才生效。", "Memory and stack changes take effect on the next run.");
    }

    /// <summary>
    /// 把候选表填进 Picker 并选中当前值。
    /// **显示文案与取值分离**（索引 → 候选表里的数）—— 直接用字符串当值的话，
    /// "1024 KB" 这种文案一改就会和存储里的数对不上。
    /// </summary>
    private static void SelectOption(Picker picker, int[] options, int current, Func<int, string> label)
    {
        picker.ItemsSource = options.Select(label).ToList();
        var idx = Array.IndexOf(options, current);
        picker.SelectedIndex = idx >= 0 ? idx : 0;
    }

    /// <summary>把四个 Picker 的选择写回 <see cref="MauiVmStore"/>（索引 → 候选表里的值）。</summary>
    private void SaveVmSettings()
    {
        if (VmMemoryPicker.SelectedIndex >= 0)
            MauiVmStore.MemoryMb = MauiVmStore.MemoryOptions[VmMemoryPicker.SelectedIndex];
        if (VmStackPicker.SelectedIndex >= 0)
            MauiVmStore.StackKb = MauiVmStore.StackOptions[VmStackPicker.SelectedIndex];
        if (VmEditorTimeoutPicker.SelectedIndex >= 0)
            MauiVmStore.EditorTimeoutSec = MauiVmStore.TimeoutOptions[VmEditorTimeoutPicker.SelectedIndex];
        if (VmShellTimeoutPicker.SelectedIndex >= 0)
            MauiVmStore.ShellTimeoutSec = MauiVmStore.TimeoutOptions[VmShellTimeoutPicker.SelectedIndex];
    }

    /// <summary>
    /// 载入「编译」分组的六项（优化 / 警告 / 警告当错误 / 调试 / 浮点 / 64 位）。
    ///
    /// 这些值存在 **Preferences**（<see cref="MauiCompileStore"/>）而不是 `config.json` ——
    /// 与虚拟机那几项同一个理由：它们只对**手机端**有意义（桌面 `vmlcli` 走命令行开关）。
    ///
    /// ⚠ 候选表一律取自 `MauiCompileStore`，这里**一个字面量都不写死** ——
    ///   否则"候选表里删掉一档、界面上还留着"这种事没人会发现。
    /// </summary>
    private void LoadCompileSettings()
    {
        // ⚠ 优化器是**全能版**的功能：免费版把这一整块**藏掉**（不是置灰）。
        //
        // 置灰为什么不行：`MauiCompileStore.OptimizationLevel` 在没解锁时恒定返回
        // `OptimizationPolicy.Off`（门收在 getter 一处），而摘要行显示的是**同一个属性** ——
        // 所以界面上会出现"下拉框选着『中度』、摘要写着『优化 关闭』"这种自相矛盾。
        // 与其解释，不如按实际情况不显示它（下方的 `CompileHintLabel` 里有一句话说明去处）。
        CompileOptRow.IsVisible = EntitlementStore.IsFull;

        SelectOption(CompileOptPicker, MauiCompileStore.OptimizationOptions,
            MauiCompileStore.OptimizationLevel, MauiCompileStore.OptimizationText);
        SelectOption(CompileWarnPicker, MauiCompileStore.WarningOptions,
            MauiCompileStore.WarningLevel, MauiCompileStore.WarningText);
        SelectStringOption(CompileFloatPicker, MauiCompileStore.FloatMode);
        SelectStringOption(CompileInt64Picker, MauiCompileStore.Int64Mode);

        // 两个 Switch **不需要"载入中"闸门**：它们不挂 `Toggled`（只在点「保存配置」时读），
        // 所以这里赋值不会触发回写 —— 与虚拟机那四个 Picker 同一模式。
        CompileWarnErrSwitch.IsToggled = MauiCompileStore.WarningsAsErrors;
        CompileDebugSwitch.IsToggled = MauiCompileStore.DebugOutput;

        // 说明行：四件事必须说清，否则用户会按错误的预期去调 ——
        //   ① 什么时候生效（下一次编译，不是下一次运行）；
        //   ② 四档各干什么（尤其"中度才是真正变小的那一档"）；
        //   ③ 优化**不会改变程序行为**（只删确定用不到的东西，实测 22 门语言输出逐字节相同）；
        //   ④ "关闭"是**报错**而不是降级。
        CompileHintLabel.Text =
            L.Pick("改完下一次编译生效。初步只清填充代码；中度会删掉没被调用的库函数（产物大幅变小，", "Takes effect on the next compile. Light only clears padding code; Medium also drops library functions that are never called (the output shrinks a lot, ")
            + L.Pick("实测 hello world 69637→28 条、俄罗斯方块 74754→6282 条）；极致再加几项安全清理。", "measured: hello world 69637→28 instructions, Tetris 74754→6282); Max adds a few more safe cleanups. ")
            + L.Pick("优化只删确定用不到的东西，不改程序行为。", "Optimization only removes what is provably unused, and never changes program behavior. ")
            + L.Pick("「关闭（遇到就报错）」是指遇到浮点 / 64 位代码直接编译报错，不是悄悄降级。", "\"Off (errors out)\" means floating point / 64-bit code fails the compile outright instead of silently degrading.");

        // 免费版没有上面那一项（`CompileOptRow` 已隐藏），得说清它去哪了 ——
        // 否则用户找不着会以为功能被砍了。
        if (!EntitlementStore.IsFull)
            CompileHintLabel.Text += L.Pick("　优化器属于「全能版」，可在上一层的「全能版」里解锁。",
                                            " The optimizer is part of the Full Edition - unlock it under Full Edition.");
    }

    /// <summary>把「编译」分组的六项写回 <see cref="MauiCompileStore"/>（索引 → 候选表里的值）。</summary>
    private void SaveCompileSettings()
    {
        if (CompileOptPicker.SelectedIndex >= 0)
            MauiCompileStore.OptimizationLevel = MauiCompileStore.OptimizationOptions[CompileOptPicker.SelectedIndex];
        if (CompileWarnPicker.SelectedIndex >= 0)
            MauiCompileStore.WarningLevel = MauiCompileStore.WarningOptions[CompileWarnPicker.SelectedIndex];
        if (CompileFloatPicker.SelectedIndex >= 0)
            MauiCompileStore.FloatMode = MauiCompileStore.NumberModeOptions[CompileFloatPicker.SelectedIndex];
        if (CompileInt64Picker.SelectedIndex >= 0)
            MauiCompileStore.Int64Mode = MauiCompileStore.NumberModeOptions[CompileInt64Picker.SelectedIndex];

        MauiCompileStore.WarningsAsErrors = CompileWarnErrSwitch.IsToggled;
        MauiCompileStore.DebugOutput = CompileDebugSwitch.IsToggled;
    }

    /// <summary>
    /// 字符串候选版的 <see cref="SelectOption"/> —— 给数值模式那两项用（档位是 `hard`/`none`
    /// 这种字符串，不是数字）。语义完全一致：**找不到就选第一个**（= 出厂默认档）。
    /// </summary>
    private static void SelectStringOption(Picker picker, string current)
    {
        var options = MauiCompileStore.NumberModeOptions;
        picker.ItemsSource = options.Select(MauiCompileStore.NumberModeText).ToList();
        var idx = Array.IndexOf(options, current);
        picker.SelectedIndex = idx >= 0 ? idx : 0;
    }

    private void LoadConfig()
    {
        var cfg = Config.Instance;

        // VML 游戏画面导出（见 Config.VmlExportFrame 的注释）
        VmlExportSwitch.IsToggled = cfg.VmlExportFrame;
        VmlFrameMaxSideEntry.Text = cfg.VmlFrameMaxSide.ToString();

        // 服务商列表（排除 local/custom，按展示名排序；保留当前服务商）
        var providers = ModelCatalog.Providers
            .Where(kv => kv.Key is not ("local" or "custom"))
            .OrderBy(kv => kv.Value.DisplayName)
            .Select(kv => new ProviderOption(kv.Key, kv.Value.DisplayName))
            .ToList();

        var current = cfg.Provider.ToLowerInvariant();
        if (providers.All(p => p.Id != current))
        {
            var disp = ModelCatalog.ProviderDisplayName(current);
            providers.Insert(0, new ProviderOption(current, disp));
        }

        ProviderPicker.ItemsSource = providers;
        ProviderPicker.SelectedItem = providers.FirstOrDefault(p => p.Id == current) ?? providers.FirstOrDefault();

        ReloadModels(current, cfg.Model);

        BaseUrlEntry.Text = cfg.BaseUrl ?? ModelCatalog.Providers.GetValueOrDefault(current)?.DefaultBaseUrl;
        MaxTokensEntry.Text = cfg.MaxTokens.ToString();
        TemperatureEntry.Text = cfg.Temperature.ToString("F1");

        EconomyPicker.ItemsSource = new List<string> { "off", "auto", "on", "extreme" };
        EconomyPicker.SelectedItem = cfg.EconomyMode.ToString().ToLowerInvariant();

        // 小模型（双模型架构的补全/摘要/压缩侧）：独立服务商/模型/地址/Key
        var smallCurrent = string.IsNullOrEmpty(cfg.SmallProvider) ? current : cfg.SmallProvider.ToLowerInvariant();
        if (providers.All(p => p.Id != smallCurrent))
        {
            var disp = ModelCatalog.ProviderDisplayName(smallCurrent);
            providers.Insert(0, new ProviderOption(smallCurrent, disp));
        }
        SmallProviderPicker.ItemsSource = providers;
        SmallProviderPicker.SelectedItem = providers.FirstOrDefault(p => p.Id == smallCurrent) ?? providers.FirstOrDefault();

        ReloadSmallModels(smallCurrent, cfg.SmallModel);
        SmallBaseUrlEntry.Text = ModelCatalog.Providers.GetValueOrDefault(smallCurrent)?.DefaultBaseUrl;

        // 推理深度（空=模型默认）
        ReasoningPicker.ItemsSource = new List<string> { "", "minimal", "low", "medium", "high", "max" };
        ReasoningPicker.SelectedItem = string.IsNullOrEmpty(cfg.ReasoningEffort) ? "" : cfg.ReasoningEffort;

        // 上下文窗口 + 预算上限
        MaxContextEntry.Text = cfg.MaxContextTokens.ToString();
        BudgetEntry.Text = cfg.MaxBudgetUsd?.ToString("0.##") ?? "";

        // Whisper 语音（空 Key 回退主 Key）
        WhisperModelEntry.Text = cfg.WhisperModel;
        WhisperBaseUrlEntry.Text = cfg.WhisperBaseUrl ?? "";
        WhisperKeyEntry.Text = "";

        // 权限模式（确认轴：Ask/Auto/SmartAuto/Yolo；索引与 enum 顺序一致）
        PermModePicker.ItemsSource = PermModeLabels;
        PermModePicker.SelectedItem = PermModeLabels[(int)PermissionManager.CurrentMode];

        UpdateKeyStatus(current);
        UpdateSmallKeyStatus(smallCurrent);
        RefreshWorkspaceStatus();
    }

    /// <summary>刷新 workspace 存储状态（外部/私有）。</summary>
    private void RefreshWorkspaceStatus()
    {
        var ext = WayCoder.Maui.MauiBootstrap.WorkspaceExternal;
        WorkspaceStatusLabel.Text = ext
            ? L.Pick($"✅ workspace 在外部存储：{WayCoder.Maui.MauiBootstrap.WorkspaceDir}", $"✅ workspace is on external storage: {WayCoder.Maui.MauiBootstrap.WorkspaceDir}")
            : L.Pick($"⚠️ workspace 在 App 私有目录（卸载重装会丢失代码）：\n{WayCoder.Maui.MauiBootstrap.WorkspaceDir}", $"⚠️ workspace is in app-private storage (code is lost on reinstall):\n{WayCoder.Maui.MauiBootstrap.WorkspaceDir}");
        ExternalWorkspaceBtn.Text = ext
            ? L.Pick("🗂 外部存储已启用", "🗂 External storage enabled")
            : L.Pick("🗂 启用外部存储 workspace（卸载重装代码不丢）", "🗂 Enable external workspace (code survives reinstall)");
    }

    /// <summary>启用外部存储 workspace：未授权先跳系统「所有文件访问」设置，授权后自动迁移。</summary>
    private async void OnExternalWorkspaceClicked(object? sender, EventArgs e)
    {
#if ANDROID
        if (!Android.OS.Environment.IsExternalStorageManager)
        {
            try
            {
                // 「所有文件访问」设置页（Android 11+）
                var intent = new Android.Content.Intent(
                    "android.settings.MANAGE_APP_ALL_FILES_ACCESS_PERMISSION",
                    Android.Net.Uri.Parse("package:" + Android.App.Application.Context.PackageName));
                intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                Android.App.Application.Context.StartActivity(intent);
            }
            catch
            {
                // 部分 ROM 无该页面，退到应用详情页
                var intent = new Android.Content.Intent("android.settings.APPLICATION_DETAILS_SETTINGS",
                    Android.Net.Uri.Parse("package:" + Android.App.Application.Context.PackageName));
                intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                Android.App.Application.Context.StartActivity(intent);
            }
            await DisplayAlertAsync(L.Pick("已跳转设置", "Settings opened"), L.Pick("请开启「允许访问所有文件」，返回后点本按钮完成迁移。", "Turn on \"Allow access to all files\", then come back and tap this button to finish the migration."), L.Pick("确定", "OK"));
            return;
        }

        var ok = WayCoder.Maui.MauiBootstrap.TryEnableExternalWorkspace();
        RefreshWorkspaceStatus();
        await DisplayAlertAsync(ok ? L.Pick("已启用", "Enabled") : L.Pick("启用失败", "Could not enable"),
            ok
                ? L.Pick("workspace 已迁移到 sdcard/waycoder/workspace、配置到 sdcard/waycoder/config，卸载重装不丢。重启应用后配置完全生效。", "The workspace moved to sdcard/waycoder/workspace and the config to sdcard/waycoder/config; both survive a reinstall. Restart the app for the config to fully take effect.")
                : L.Pick("无法启用外部存储，请检查权限。", "Could not enable external storage. Please check the permission."), L.Pick("确定", "OK"));
#else
        await DisplayAlertAsync(L.Pick("外部存储", "External storage"), L.Pick("仅 Android 支持。", "Android only."), L.Pick("确定", "OK"));
#endif
    }

    /// <summary>应用模型选择：统一入口切换服务商+模型并刷新 UI。</summary>
    private void ApplyModel(string providerId, string modelId)
    {
        ConnectionConfig.ApplyModelChoice(providerId, modelId, isLarge: true, out _);
        var disp = ModelCatalog.ProviderDisplayName(providerId);
        ProviderPicker.SelectedItem = new ProviderOption(providerId, disp);
        ReloadModels(providerId, modelId);
        UpdateKeyStatus(providerId);
        BaseUrlEntry.Text = ModelCatalog.Providers.GetValueOrDefault(providerId)?.DefaultBaseUrl;
    }

    /// <summary>导入模型：内置目录 / 从配置文件（Claude/OpenCode 等）启发式解析 Key+模型。</summary>
    private async void OnImportModelsClicked(object? sender, EventArgs e)
    {
        // 动作表的**项文本就是判据**（`action` 拿到的是被点中那一项的文本）——
        // 两者绑到同一个局部量上；否则翻成英文后 `== "从配置文件导入"` 永远不成立，且零报错。
        var importFromConfig = L.Pick("从配置文件导入", "Import from config file");
        var importBuiltin = L.Pick("导入内置模型目录", "Import built-in catalog");
        var action = await DisplayActionSheetAsync(L.Pick("导入模型", "Import models"), L.Pick("取消", "Cancel"), null, importFromConfig, importBuiltin);
        if (action == importFromConfig)
            await ImportFromConfigFileAsync();
        else if (action == importBuiltin)
            await DisplayAlertAsync(L.Pick("内置模型", "Built-in models"),
                L.Pick($"内置模型目录已包含 {ModelCatalog.All.Length} 个模型（DeepSeek / Qwen / Zhipu / OpenAI / Anthropic / AIHubMix 等）。在「连接」区选服务商即可使用。", $"The built-in catalog already has {ModelCatalog.All.Length} models (DeepSeek / Qwen / Zhipu / OpenAI / Anthropic / AIHubMix and more). Just pick a provider in the Provider section."), L.Pick("确定", "OK"));
    }

    /// <summary>文件选择器选 Claude/OpenCode 等配置，启发式提取 API Key + 模型并应用。</summary>
    private async Task ImportFromConfigFileAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = L.Pick("选择 Claude/OpenCode 等配置文件", "Choose a Claude/OpenCode config file") });
            if (result == null) return;
            var content = await File.ReadAllTextAsync(result.FullPath);

            // 启发式正则（AOT 安全）：找 API Key 与模型
            var key = Regex.Match(content, "(?i)\"(api_key|apiKey|ANTHROPIC_API_KEY|OPENAI_API_KEY|DEEPSEEK_API_KEY)\"\\s*[:=]\\s*\"([^\"]+)\"").Groups[2].Value;
            var model = Regex.Match(content, "(?i)\"(model|defaultModel)\"\\s*[:=]\\s*\"([^\"]+)\"").Groups[2].Value;

            var applied = false;
            var pid = Config.Instance.Provider;
            if (!string.IsNullOrEmpty(key))
            {
                // 环境变量引用（$VAR / ${VAR}）不是真实 key，跳过导入（防把环境变量当 key 误存）
                if (ApiKeyStore.IsEnvVarRef(key))
                {
                    await DisplayAlertAsync(L.Pick("已跳过", "Skipped"), L.Pick("检测到环境变量引用（$VAR），不是真实 Key，未导入。请填入真实 API Key。", "That looks like an environment variable reference ($VAR), not a real key, so nothing was imported. Paste a real API key instead."), L.Pick("确定", "OK"));
                    return;
                }
                ApiKeyStore.Set(pid, key);
                UpdateKeyStatus(pid);
                applied = true;
            }
            if (!string.IsNullOrEmpty(model) && ModelCatalog.Find(model) is { } mi)
            {
                ApplyModel(mi.ProviderId, mi.Id);
                applied = true;
            }
            await DisplayAlertAsync(applied ? L.Pick("已导入", "Imported") : L.Pick("未识别", "Not recognized"),
                applied ? L.Pick("已从配置文件导入模型 / API Key（请到「保存」确认生效）。", "Model / API key imported from the config file (tap Save to apply).") : L.Pick("未能从该文件识别模型或 API Key。", "Could not find a model or an API key in that file."), L.Pick("确定", "OK"));
        }
        catch (Exception ex) { await DisplayAlertAsync(L.Pick("导入失败", "Import failed"), ex.Message, L.Pick("关闭", "Close")); }
    }

    /// <summary>扫描全部服务商连接可达性（探测 /models 接口，单点 4s 超时；委托核心 ModelCli）。</summary>
    private async void OnScanConnectionsClicked(object? sender, EventArgs e)
    {
        var providers = ModelCli.ResolveScanTargets();

        var results = new List<string>();
        foreach (var p in providers)
        {
            if (string.IsNullOrEmpty(p.Url)) { results.Add(L.Pick($"{p.Name}（{p.Id}）· 无默认地址", $"{p.Name} ({p.Id}) · no default URL")); continue; }
            var (ok, _, _) = await ModelCli.ProbeEndpointAsync(p.Url, ApiKeyStore.Get(p.Id));
            results.Add(L.Pick($"{p.Name}（{p.Id}）· {(ok ? "✅ 可达" : "❌ 不可达")}", $"{p.Name} ({p.Id}) · {(ok ? "✅ reachable" : "❌ unreachable")}"));
        }
        await DisplayActionSheetAsync(L.Pick($"连接扫描（{providers.Count}）", $"Connection scan ({providers.Count})"), L.Pick("关闭", "Close"), null, results.ToArray());
    }

    /// <summary>按服务商刷新模型下拉；可选指定选中模型（含当前模型不在该服务商时的兜底）。</summary>
    private void ReloadModels(string providerId, string? selectModel = null)
    {
        var models = ModelCatalog.ByProvider(providerId).ToList();
        ModelPicker.ItemsSource = models;

        var target = selectModel ?? Config.Instance.Model;
        var match = models.FirstOrDefault(m => string.Equals(m.Id, target, StringComparison.OrdinalIgnoreCase));
        ModelPicker.SelectedItem = match ?? models.FirstOrDefault();
    }

    private void OnProviderChanged(object? sender, EventArgs e)
    {
        if (ProviderPicker.SelectedItem is not ProviderOption opt) return;
        ReloadModels(opt.Id);
        BaseUrlEntry.Text = ModelCatalog.Providers.GetValueOrDefault(opt.Id)?.DefaultBaseUrl;
        UpdateKeyStatus(opt.Id);
    }

    private void UpdateKeyStatus(string providerId)
    {
        var masked = ApiKeyStore.Masked(providerId);
        KeyStatusLabel.Text = masked != null ? L.Pick($"已保存：{masked}", $"Saved: {masked}") : L.Pick("未配置 Key", "No key configured");
    }

    /// <summary>小模型组：按服务商刷新模型下拉（含选中兜底）。</summary>
    private void ReloadSmallModels(string providerId, string? selectModel = null)
    {
        var models = ModelCatalog.ByProvider(providerId).ToList();
        SmallModelPicker.ItemsSource = models;

        var target = selectModel ?? Config.Instance.SmallModel;
        var match = models.FirstOrDefault(m => string.Equals(m.Id, target, StringComparison.OrdinalIgnoreCase));
        SmallModelPicker.SelectedItem = match ?? models.FirstOrDefault();
    }

    private void OnSmallProviderChanged(object? sender, EventArgs e)
    {
        if (SmallProviderPicker.SelectedItem is not ProviderOption opt) return;
        ReloadSmallModels(opt.Id);
        SmallBaseUrlEntry.Text = ModelCatalog.Providers.GetValueOrDefault(opt.Id)?.DefaultBaseUrl;
        UpdateSmallKeyStatus(opt.Id);
    }

    private void UpdateSmallKeyStatus(string providerId)
    {
        var masked = ApiKeyStore.Masked(providerId);
        SmallKeyStatusLabel.Text = masked != null ? L.Pick($"已保存：{masked}", $"Saved: {masked}") : L.Pick("未配置 Key", "No key configured");
    }

    /// <summary>大模型分组测试 Key。</summary>
    private async void OnTestKeyClicked(object? sender, EventArgs e) => await TestKeyFlowAsync(large: true);

    /// <summary>小模型分组测试 Key。</summary>
    private async void OnSmallTestKeyClicked(object? sender, EventArgs e) => await TestKeyFlowAsync(large: false);

    /// <summary>
    /// 测试 API Key 通用流程（大/小模型分组共用）：用输入框（或已存）key 发最小 chat 请求
    /// （max_tokens=1）。有效 → 立即写入 ApiKeyStore 并刷新状态；无效 → 弹错误（含服务端信息）。
    /// </summary>
    private async Task TestKeyFlowAsync(bool large)
    {
        var opt = (large ? ProviderPicker : SmallProviderPicker).SelectedItem as ProviderOption;
        var model = (large ? ModelPicker : SmallModelPicker).SelectedItem as ModelCatalog.ModelInfo;
        var keyEntry = large ? KeyEntry : SmallKeyEntry;
        var baseUrlEntry = large ? BaseUrlEntry : SmallBaseUrlEntry;
        var btn = large ? TestKeyBtn : SmallTestKeyBtn;
        var statusRefresher = large ? new Action<string>(UpdateKeyStatus) : UpdateSmallKeyStatus;

        if (opt == null)
        {
            await DisplayAlertAsync(L.Pick("未选择", "Nothing selected"), L.Pick("请先选择服务商", "Please pick a provider first"), L.Pick("确定", "OK"));
            return;
        }
        var key = keyEntry.Text?.Trim();
        if (string.IsNullOrEmpty(key)) key = ApiKeyStore.Get(opt.Id);
        if (string.IsNullOrEmpty(key))
        {
            await DisplayAlertAsync(L.Pick("无 Key", "No key"), L.Pick("请在输入框填入要测试的 API Key", "Enter the API key you want to test in the field above"), L.Pick("确定", "OK"));
            return;
        }

        var oldText = btn.Text;
        btn.Text = L.Pick("⏳ 测试中…", "⏳ Testing…");
        btn.IsEnabled = false;
        try
        {
            var (ok, message) = await TestKeyCoreAsync(opt.Id, model?.Id ?? "", baseUrlEntry.Text, key);
            if (ok)
            {
                // 有效立即保存：环境变量引用（$VAR）测试会失败，这里双保险跳过
                if (ApiKeyStore.IsEnvVarRef(key))
                {
                    await DisplayAlertAsync(L.Pick("已跳过", "Skipped"), L.Pick("检测到环境变量引用（$VAR），不是真实 Key，未保存。", "That looks like an environment variable reference ($VAR), not a real key, so nothing was saved."), L.Pick("确定", "OK"));
                    keyEntry.Text = "";
                    return;
                }
                ApiKeyStore.Set(opt.Id, key);
                statusRefresher(opt.Id);        // 刷新状态标签
                keyEntry.Text = "";
                await DisplayAlertAsync(L.Pick("✅ Key 有效", "✅ Key is valid"), L.Pick($"服务商 {opt.DisplayName} 的 API Key 验证通过，已自动保存。", $"The API key for {opt.DisplayName} was verified and saved automatically."), L.Pick("确定", "OK"));
            }
            else
            {
                await DisplayAlertAsync(L.Pick("❌ Key 无效", "❌ Invalid key"), message, L.Pick("确定", "OK"));
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(L.Pick("❌ 测试失败", "❌ Test failed"), $"{ex.GetType().Name}: {ex.Message}", L.Pick("确定", "OK"));
        }
        finally
        {
            btn.Text = oldText;
            btn.IsEnabled = true;
        }
    }

    /// <summary>用给定 key 发最小 chat 请求测试有效性。返回 (ok, message)。复用 LLM 端点拼接。</summary>
    private static async Task<(bool Ok, string Message)> TestKeyCoreAsync(
        string providerId, string modelId, string? baseUrlText, string key)
    {
        var baseUrl = string.IsNullOrWhiteSpace(baseUrlText)
            ? ModelCatalog.Providers.GetValueOrDefault(providerId)?.DefaultBaseUrl
            : baseUrlText.Trim();
        // 端点拼接走 LLM 的唯一实现（含 /v1beta/openai 特例）——此前手抄了一份不完整的，
        // Gemini 会被拼成 .../openai/v1/chat/completions → 404 → 误报「Key 无效」
        var endpoint = LLM.ResolveApiEndpoint(baseUrl, "/v1/chat/completions");

        var msgs = JNode.Array();
        msgs.Add(JNode.Object().Set("role", "user").Set("content", "hi"));
        var body = JNode.Object()
            .Set("model", string.IsNullOrEmpty(modelId) ? "gpt-4o-mini" : modelId)
            .Set("max_tokens", 1)
            .Set("messages", msgs);

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
        req.Headers.Add("Authorization", $"Bearer {key}");
        req.Content = new StringContent(body.ToJson(), Encoding.UTF8, "application/json");
        using var resp = await http.SendAsync(req);
        var respBody = await resp.Content.ReadAsStringAsync();

        if (resp.IsSuccessStatusCode) return (true, L.Pick("验证通过", "Verified"));
        var msg = respBody.Length > 300 ? respBody[..300] + "…" : respBody;
        return (false, $"HTTP {(int)resp.StatusCode}\n{msg}");
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (ProviderPicker.SelectedItem is not ProviderOption opt || ModelPicker.SelectedItem is not ModelCatalog.ModelInfo model)
        {
            await DisplayAlertAsync(L.Pick("未选择", "Nothing selected"), L.Pick("请选择服务商与模型", "Please pick a provider and a model"), L.Pick("确定", "OK"));
            return;
        }

        // 1) API Key（非空才写；为空保留原 Key）——合法性校验：只允许字母数字 + `+-_.` 逗号，拒绝环境变量引用
        var key = KeyEntry.Text?.Trim();
        if (!string.IsNullOrEmpty(key))
        {
            if (ApiKeyStore.IsEnvVarRef(key) || !ApiKeyStore.IsValidApiKey(key))
                await DisplayAlertAsync(L.Pick("Key 不合法", "Invalid key"), L.Pick($"只允许英文字母数字 + - _ . ,（{opt.DisplayName} 的 Key 未保存，不要填环境变量引用 $VAR / %VAR%）", $"Only letters, digits and + - _ . , are allowed ({opt.DisplayName} key not saved; don't use $VAR / %VAR% env references)"), L.Pick("确定", "OK"));
            else
                ApiKeyStore.Set(opt.Id, key);
        }
        UpdateKeyStatus(opt.Id);   // 保存后立即刷新「已保存」状态（此前不刷新显示旧 key）

        // 2) 服务商 + 模型 + BaseUrl 统一入口（自动持久化 + 从环境变量导 key）
        var baseUrl = string.IsNullOrWhiteSpace(BaseUrlEntry.Text) ? null : BaseUrlEntry.Text.Trim();
        ConnectionConfig.ApplyModelChoice(opt.Id, model.Id, isLarge: true, out _, baseUrl);

        // 2.5) 小模型分组：独立服务商/模型/地址/Key（同一套 apply + ApiKeyStore）
        if (SmallProviderPicker.SelectedItem is ProviderOption sopt
            && SmallModelPicker.SelectedItem is ModelCatalog.ModelInfo smodel)
        {
            var skey = SmallKeyEntry.Text?.Trim();
            if (!string.IsNullOrEmpty(skey))
            {
                if (ApiKeyStore.IsEnvVarRef(skey) || !ApiKeyStore.IsValidApiKey(skey))
                    await DisplayAlertAsync(L.Pick("Key 不合法", "Invalid key"), L.Pick($"只允许英文字母数字 + - _ . ,（{sopt.DisplayName} 的小模型 Key 未保存）", $"Only letters, digits and + - _ . , are allowed ({sopt.DisplayName} key for the small model not saved)"), L.Pick("确定", "OK"));
                else
                    ApiKeyStore.Set(sopt.Id, skey);
            }
            UpdateSmallKeyStatus(sopt.Id);   // 同样立即刷新
            var sbaseUrl = string.IsNullOrWhiteSpace(SmallBaseUrlEntry.Text) ? null : SmallBaseUrlEntry.Text.Trim();
            ConnectionConfig.ApplyModelChoice(sopt.Id, smodel.Id, isLarge: false, out _, sbaseUrl);
        }

        // VML 游戏画面导出（见 Config.VmlExportFrame / VmlFrameMaxSide）
        Config.Instance.VmlExportFrame = VmlExportSwitch.IsToggled;
        if (int.TryParse(VmlFrameMaxSideEntry.Text, out var vms))
            Config.Instance.VmlFrameMaxSide = Math.Clamp(vms, 128, 4096);

        // 虚拟机参数（内存 / 栈 / 两个运行超时）—— 存 Preferences，不进 config.json
        // ——它们只对手机端有意义（桌面 vmlcli 走命令行参数）。
        SaveVmSettings();

        // 编译参数（优化 / 警告 / 浮点）—— 同样是 Preferences，同样只对手机端有意义
        // （桌面 vmlcli 走命令行开关）。**下一次编译才生效**。
        SaveCompileSettings();

        // 3) 参数
        if (int.TryParse(MaxTokensEntry.Text, out var mt)) Config.Instance.MaxTokens = mt;
        if (float.TryParse(TemperatureEntry.Text, out var tp)) Config.Instance.Temperature = tp;
        if (int.TryParse(MaxContextEntry.Text, out var mct)) Config.Instance.MaxContextTokens = mct;
        Config.Instance.MaxBudgetUsd = double.TryParse(BudgetEntry.Text, out var bud) ? bud : null;
        if (ReasoningPicker.SelectedItem is string re)
            Config.Instance.ReasoningEffort = string.IsNullOrEmpty(re) ? "" : re;
        if (EconomyPicker.SelectedItem is string eco && Enum.TryParse<EconomyMode>(eco, true, out var em))
            Config.Instance.EconomyMode = em;

        // 4) 语音（Whisper 转录；空 Key 回退主 Key）
        Config.Instance.WhisperModel = string.IsNullOrWhiteSpace(WhisperModelEntry.Text) ? "whisper-1" : WhisperModelEntry.Text.Trim();
        Config.Instance.WhisperBaseUrl = string.IsNullOrWhiteSpace(WhisperBaseUrlEntry.Text) ? null : WhisperBaseUrlEntry.Text.Trim();
        var whisperKey = WhisperKeyEntry.Text?.Trim();
        if (!string.IsNullOrEmpty(whisperKey))
            Config.Instance.WhisperApiKey = whisperKey;

        // 4.5) 权限模式（确认轴）
        if (PermModePicker.SelectedIndex >= 0)
            PermissionManager.CurrentMode = (PermissionManager.Mode)PermModePicker.SelectedIndex;
        // 记住工作/权限/经济三种模式（手机无快捷键，下次启动直接恢复）
        Services.MauiModeStore.Save(WorkModeManager.CurrentMode, PermissionManager.CurrentMode, Config.Instance.EconomyMode);

        // 5) 持久化 + 重建 Agent（下次发送按新配置）
        Config.Instance.SaveToEnvFile();
        AgentService.Reset();

        KeyEntry.Text = "";
        await DisplayAlertAsync(L.Pick("已保存", "Saved"), L.Pick($"服务商 {opt.DisplayName} · 模型 {model.DisplayName}", $"Provider {opt.DisplayName} · Model {model.DisplayName}"), L.Pick("确定", "OK"));
    }
}
