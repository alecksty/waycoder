namespace WayCoder.UI.Cli.Arguments;

/// <summary>启动界面路由（Main 的界面分发结果）。</summary>
public enum StartupRoute
{
    /// <summary>浏览器聊天界面（<c>--web</c>）。</summary>
    Web,

    /// <summary>交互式 CLI 界面（<c>--cli</c>）：逐行读入 → Agent 纯文本回复 → exit/quit 退出。</summary>
    Cli,

    /// <summary>
    /// <c>-p "任务"</c>：走 CLI 的**纯文本输出**执行一次后退出。
    /// 与 <see cref="Cli"/> 同源（同一个 ProcessTextInput），**不带 spinner 动画** ——
    /// 输出可以直接重定向到文件、被脚本/CI 解析（BatchRunner 子进程也走这条）。
    /// </summary>
    CliOneShot,

    /// <summary>
    /// 全屏 TUI —— **不带任何参数启动的默认落点**，也是 <c>-p1</c>~<c>-p0</c> 槽位任务的落点
    /// （槽位任务需要 TUI 的槽位视图与并行工作区）。
    /// </summary>
    Repl,

    /// <summary>一次性执行 + JSON 输出（<c>--json -p</c>，供 IDE/CI 解析，无界面）。</summary>
    OneShotJson,
}

/// <summary>
/// 启动界面分发判据（纯逻辑，无 UI 依赖，主自测直接覆盖）。
///
/// **默认行为规则**：
/// - 不带任何参数 → <see cref="StartupRoute.Repl"/>（全屏 TUI）
/// - <c>-p "任务"</c> → <see cref="StartupRoute.CliOneShot"/>（CLI 纯文本，跑完退出）
/// - <c>-p1</c>~<c>-p0 "任务"</c> → <see cref="StartupRoute.Repl"/>（槽位任务进 TUI）
/// - 显式指定界面 → **指定优先**：<c>--web</c> / <c>--cli</c> / <c>--tui</c>
///
/// 这些此前只存在于 Main 里的一串 if 中，没有任何测试钉住 —— 判据写错（或给某个开关加默认值）
/// 会让界面静默换掉而自测全绿看不出来。
///
/// <c>--tui</c> 的作用是**压过 <c>--cli</c>**（两者同时给时以全屏为准）；它不改变无参数时的结果。
/// <c>--tui -p</c> 的提示词由 Main 提前搬进槽位队列（搬运后 hasPrompt 即 false），故落到 Repl。
/// </summary>
public static class StartupRouter
{
    /// <summary>决定走哪个界面。</summary>
    /// <param name="web"><c>--web</c>：浏览器界面。</param>
    /// <param name="cli"><c>--cli</c>：逐行文本界面。</param>
    /// <param name="tui"><c>--tui</c>：显式要全屏（压过 --cli）。</param>
    /// <param name="json"><c>--json</c> / <c>--output-format json</c>。</param>
    /// <param name="hasPrompt">是否拿到提示词（含从 stdin 管道读到的那份）。</param>
    public static StartupRoute Decide(bool web, bool cli, bool tui, bool json, bool hasPrompt)
    {
        // ① 显式指定的界面优先
        if (web) return StartupRoute.Web;
        if (cli && !tui) return hasPrompt ? StartupRoute.CliOneShot : StartupRoute.Cli;

        // ② 没指定界面：按提示词形态分派（-p = CLI 纯文本，--json = JSON）
        if (hasPrompt) return json ? StartupRoute.OneShotJson : StartupRoute.CliOneShot;

        // ③ 无参数 / --tui / 槽位任务(-p1~-p0) → 全屏 TUI
        return StartupRoute.Repl;
    }
}
