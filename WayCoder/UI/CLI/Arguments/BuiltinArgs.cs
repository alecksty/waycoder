using WayCoder.Tools;
using WayCoder.UI.Tui;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Cli.Arguments;

// ═══════════════════════════════════════════════════════════════
// 注册入口 —— 应用启动时调用一次
// ═══════════════════════════════════════════════════════════════

public static class BuiltinArgs
{
    static bool _registered;

    /// <summary>注册所有内置 CLI 参数（幂等）。重复名称自动报错。</summary>
    public static void RegisterAll()
    {
        if (_registered) return;
        _registered = true;

        CliArgRegistry.Register(new ModelArg());
        CliArgRegistry.Register(new ConnectArg());
        CliArgRegistry.Register(new BaseUrlArg());
        CliArgRegistry.Register(new ApiKeyArg());
        CliArgRegistry.Register(new PromptArg());
        CliArgRegistry.Register(new ResumeArg());
        CliArgRegistry.Register(new SessionListArg());
        CliArgRegistry.Register(new MaxBudgetArg());
        CliArgRegistry.Register(new MaxRequeueArg());
        CliArgRegistry.Register(new VersionArg());
        CliArgRegistry.Register(new InitArg());
        CliArgRegistry.Register(new YoloArg());
        CliArgRegistry.Register(new OutputFormatArg());
        CliArgRegistry.Register(new PermissionModeArg());
        CliArgRegistry.Register(new AllowedToolsArg());
        CliArgRegistry.Register(new DisallowedToolsArg());
        CliArgRegistry.Register(new SystemPromptArg());
        CliArgRegistry.Register(new SessionArg());
        CliArgRegistry.Register(new WatchArg());
        CliArgRegistry.Register(new TinyArg());
        CliArgRegistry.Register(new EconomyArg());
        CliArgRegistry.Register(new EditArg());
        CliArgRegistry.Register(new UpdateArg());
        CliArgRegistry.Register(new JsonArg());
        CliArgRegistry.Register(new WebArg());
        CliArgRegistry.Register(new TuiArg());
        CliArgRegistry.Register(new CliModeArg());
        CliArgRegistry.Register(new BatchArg());
        CliArgRegistry.Register(new BatchRepoArg());
        CliArgRegistry.Register(new BatchTaskArg());
        CliArgRegistry.Register(new BatchKeepArg());
        CliArgRegistry.Register(new ConfigArg());
        CliArgRegistry.Register(new DebugArg());
        CliArgRegistry.Register(new DebugDumpArg());
        CliArgRegistry.Register(new HelpArg());
        CliArgRegistry.Register(new MaxTurnsArg());
        CliArgRegistry.Register(new AutoCommitArg());
        CliArgRegistry.Register(new McpConfigArg());
        CliArgRegistry.Register(new ThemeArg());
        CliArgRegistry.Register(new QuietArg());
        CliArgRegistry.Register(new NoColorArg());
        CliArgRegistry.Register(new McpArg());
        CliArgRegistry.Register(new KbArg());
        CliArgRegistry.Register(new ResetArg());
        CliArgRegistry.Register(new PurgeArg());
        CliArgRegistry.Register(new ProviderArg());
        CliArgRegistry.Register(new PermitArg());
        CliArgRegistry.Register(new ModeArg());
#if WAYCODER_TEST
        CliArgRegistry.Register(new TestArg());
        CliArgRegistry.Register(new BenchmarkArg());
        CliArgRegistry.Register(new LimitsArg());
#endif
        CliArgRegistry.Register(new ScreenshotArg());
        CliArgRegistry.Register(new WidthProbeArg());
        CliArgRegistry.Register(new SyspromptSizeArg());
#if WAYCODER_TEST
        CliArgRegistry.Register(new TuiDemoArg());
        CliArgRegistry.Register(new TuiAuditArg());
        CliArgRegistry.Register(new TuiMouseArg());
        CliArgRegistry.Register(new DialogShowArg());
#endif
        CliArgRegistry.Register(new GuiArg());
#if WAYCODER_TEST
        CliArgRegistry.Register(new TuiPreviewArg());
        CliArgRegistry.Register(new TuiWatchArg());
        CliArgRegistry.Register(new TuiMarkupDemoArg());
#endif
        CliArgRegistry.Register(new TuiChatArg());
#if WAYCODER_TEST
        CliArgRegistry.Register(new KeypadArg());
#endif
        CliArgRegistry.Register(new ThemeVerifyArg());

        // 槽位任务参数：-pa 共享前缀 + -p1 ~ -p9, -p0(=F10)
        CliArgRegistry.Register(new SlotPromptAllArg());
        for (int n = 0; n <= 9; n++)
            CliArgRegistry.Register(new SlotPromptArg(n));
    }
}
