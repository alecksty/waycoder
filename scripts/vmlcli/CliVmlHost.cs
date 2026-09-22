using System.Diagnostics;
using System.Text;
using VMLRuntime;
using WayCoder.Infra;          // DrawRunner / DrawDocument：与手机端同一条出图链
using WayCoder.UI.Shared;

namespace VmlCli;

/// <summary>
/// **真实的 stderr** —— 宿主自己的诊断走它，**绕开** `Program.RunProgram` 那段
/// 「把 `Console.Error` 接进内存」的重定向。
///
/// ## 为什么必须绕
///
/// `RunProgram` 在 `vm.Run()` 期间把 `Console.Out` 与 `Console.Error` 都换成 `StringWriter`，
/// 那是为了接住**运行时的**诊断（`内存错误(PC=…)` / 标签错误 / 寄存器 dump），
/// 再把它并进返回值。而宿主自己的日志（`[vml-host]` / `[vml-audio]` / `[vml-dlg]`）
/// 也走 `Console.Error`，于是**被一起当成"程序输出"并到了 stdout 上** ——
/// 实测：一次探针运行的画面日志全部出现在 stdout 里。
///
/// 两重后果，都不轻：
///   · **破坏契约**：这个 CLI 的 stdout 是"VML 程序自己的输出"（`scripts/vml-out-probe`
///     等一套判据全靠逐字节比对它），混进脚手架日志等于污染判据；
///   · **自验收失灵**：`scripts/vmlcli-verify/run.sh` 按 stderr 收 `[vml-audio]`，
///     而它跑到了 stdout —— 判据静默变成"一条也没收到"。
///
/// ⚠ 与自测那条既有约定同源（`SelfTest.Report` 的"失败行必须直写真实 stdout"）：
///   **被重定向捕获的流不能用来报"捕获方自己也该看见"的东西**。
///   `Console.SetError` 换的是 `Console.Error` 这个**属性值**，进程真正的 stderr 没变，
///   所以在重定向**之前**抓住那个 writer 就够了。
/// </summary>
internal static class CliErr
{
    private static TextWriter _out = Console.Error;

    /// <summary>在 `Program.RunProgram` 重定向之前调一次（由 <see cref="CliUiCalls"/> 构造时调）。</summary>
    public static void Capture() => _out = Console.Error;

    public static void WriteLine(string line) => _out.WriteLine(line);
}

/// <summary>
/// **桌面端的 VML 宿主** —— <see cref="IVmlHost"/> 的第二个实现（另一个是
/// `WayCoder.Maui/Services/VmlUiCalls.cs` 里的 `MauiVmlHost`）。
///
/// ## 它在补什么
///
/// 在这之前，桌面 CLI 的宿主是 `NullUiCalls` —— **同一批号、全部空操作**。
/// 于是「手机上能跑的游戏在桌面上什么也证明不了」：`ui_win_open` 不做事、
/// `ui_present` 不做事、`ui_wait_msg` 永远等不到消息，任何交互程序在桌面上只剩"跑满超时被杀"。
/// 而桌面存在的全部意义就是**替代模拟器**（手机上验一次一分多钟）。
///
/// 三件事按"一刀一个可验证单元"补：
///   ① **能看见** —— 开窗 + 绘制 + `ui_present` 落成一个真 PNG（<see cref="RenderFrame"/>）；
///   ② **能交互** —— 脚本化输入事件序列（<see cref="StartInputScript"/>）；
///   ③ **有记录** —— 对话框 / 存档 / 音效震动不静默吞掉，要么给可预期的值、要么打一行日志。
///
/// ## 关键的"不造第二份"
///
/// · **逻辑**一行没有：全在 <see cref="VmlHostRuntime"/>（手机端编同一份文件）；
/// · **出图**走 `DrawRunner.Parse` + `ToPng`（手机端 `VmlTool.TryExportFrame` 走的也是它）——
///   不另写渲染器，所以"桌面上渲出来的像素"与"手机上渲出来的像素"是同一个引擎的产物，
///   逐像素判据（像 `Examples/c/draw_colors.c` 那套）才有可能在桌面上做。
///
/// ## 与手机端**有意不同**的地方（都是如实反映，不是偷懒）
///
/// · 没有真窗口：画面落到文件；`ScreenArea()` 报的是 `--screen` 给的数（默认 480×640）。
/// · 没有声卡/马达：音效与震动**打一行机器可读的日志**（`[vml-audio] hz=… ms=…`）。
///   这一条不是"凑合"—— 它正好是最有用的观测点：验证游戏逻辑时，
///   `ui_beep` 的频率序列比截图更能说明"哪一步发生了"。
/// · 对话框用 `--answer` 队列里的脚本化答案（用完给默认值，并**记一行日志**说明用了默认值）。
/// · 存档默认只在本次进程内（`--store <路径>` 才跨运行持久化）。
/// </summary>
internal sealed class CliVmlHost : IVmlHost
{
    private readonly CliHostConfig _cfg;

    /// <summary>共享 syscall 运行时 —— 由 <see cref="CliUiCalls"/> 建好后回填
    /// （`ui_present` 的快照、场景都要从它上面取）。</summary>
    public VmlHostRuntime? Runtime { get; set; }

    public CliVmlHost(CliHostConfig cfg) => _cfg = cfg;

    // ══════════════════════════════════════════════════════════════════════
    // 屏幕
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 可用绘图区。桌面没有"屏幕"，所以这就是 `--screen` 给的那块矩形
    /// （默认 480×640，像素单位 = 手机上的 dp）。
    ///
    /// ⚠ **不是**"拿真实显示器分辨率"：程序是按 `SCR_W/SCR_H` 排版的，
    /// 而桌面验收要的是**可复现**——同一份脚本在谁机器上都该开同样大的窗。
    /// 拿真实屏幕会让"同一程序在 1080p 与 5K 上布局不同"，判据就飘了。
    /// </summary>
    public (int Width, int Height) ScreenArea() => (_cfg.ScreenWidth, _cfg.ScreenHeight);

    /// <summary>方向判定只有 <see cref="VmlUi.OrientationOf"/> 一处实现（两端都调它）。</summary>
    public int Orientation() => VmlUi.OrientationOf(_cfg.ScreenWidth, _cfg.ScreenHeight);

    // ══════════════════════════════════════════════════════════════════════
    // 窗口与绘图
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>这次运行有没有开过窗口（决定退出时要不要出图）。</summary>
    public bool WindowOpened { get; private set; }

    /// <summary>已导出的帧数（`--frames` 目录模式下用）。</summary>
    public int FramesWritten { get; private set; }

    private int _lastPresentVersion;

    public bool OpenWindow(VmlScene scene)
    {
        WindowOpened = true;
        /* ⚠ **种类也要打出来**：桌面没有真窗口，这一行就是"新号解码对不对"的
           唯一判据 —— 电脑屏窗口（`WIN_OPEN_PC` #582）在桌面上与图形窗口**看起来
           完全一样**（都是"开窗 + 场景"），不打这一笔，`Kind`/`NeedKeyboard`
           错了在桌面**一点都看不出来**，要等到真机才知道。 */
        CliErr.WriteLine($"[vml-host] 开窗：\"{scene.Title}\" {scene.Width}×{scene.Height}"
            + $"（种类={scene.Kind}，转屏={scene.Rotation}，"
            + $"手柄={(scene.NeedGamepad ? "要" : "不要")}，键盘={(scene.NeedKeyboard ? "要" : "不要")}）");
        return true;
    }

    public void CloseWindow()
    {
        // 与手机端同一语义：置位之后主循环该退出。窗口"消失"这件事在桌面只是个记录。
        CliErr.WriteLine("[vml-host] 关窗");
    }

    /// <summary>
    /// 每一条绘制调用都会走到这里（一帧几百条），所以**只做轻量判断**：
    /// 只有 `ui_present` 推进了 `PresentVersion` 才出图（`--frames` 目录模式）。
    ///
    /// ⚠ 判据必须是 <see cref="VmlScene.PresentVersion"/> 而**不是** `Version` ——
    /// 后者每个图元都 +1，照它出图会拍到"`ui_clear()` 刚清完、棋子还没画"的半成品
    /// （v0.96.178 那条"俄罗斯方块有时抖动闪烁"的真身）。这条判据在共享层，
    /// 两端一致。
    /// </summary>
    public void SceneChanged(VmlScene scene)
    {
        if (_cfg.FramesDir is null) return;
        var pv = scene.PresentVersion;
        if (pv == _lastPresentVersion) return;

        var dsl = scene.TakePresentedDsl();
        if (dsl is null) return;                 // 程序没调过 ui_present：不猜、不出图
        _lastPresentVersion = pv;

        if (FramesWritten >= _cfg.FramesMax)
        {
            if (FramesWritten == _cfg.FramesMax)
                CliErr.WriteLine($"[vml-host] 帧数达到上限 {_cfg.FramesMax}，后续帧不再落盘"
                    + "（要全部导出就调大 --frames-max）");
            FramesWritten++;                     // 只用来"只提醒一次"，不再写文件
            return;
        }

        Directory.CreateDirectory(_cfg.FramesDir);
        var path = Path.Combine(_cfg.FramesDir, $"frame_{FramesWritten:D4}.png");
        FramesWritten++;
        TryWritePng(dsl, path);
    }

    /// <summary>
    /// **把「最新呈现帧」渲成 PNG** —— 桌面这一刀的产物，也是"能看见"的全部意义。
    ///
    /// 取的顺序与手机端 `VmlTool.TryExportFrame` 一致：
    ///   ① 优先程序自己声明"这一帧画完了"的那份快照（<see cref="VmlScene.PresentedDsl"/>）；
    ///   ② 程序从没调过 `ui_present` 时退回**当前图元**（`BuildDsl()`）——
    ///      严格说那可能是画到一半的，但那类程序本来就没有帧的概念，
    ///      拿最终状态出图正是它想要的（"跑完了，画成什么样"）。
    ///
    /// 返回 false = 没开过窗 / 没东西可画（纯文本程序的正常路径，不报错）。
    /// </summary>
    public bool RenderFrame(string path)
    {
        var scene = Runtime?.Scene();
        if (scene is null) return false;

        var dsl = scene.PresentedDsl ?? (scene.FigureCount > 0 ? scene.BuildDsl() : null);
        if (string.IsNullOrWhiteSpace(dsl)) return false;
        return TryWritePng(dsl, path);
    }

    /// <summary>
    /// 运行结束后兑现 `--frame` / `--frames`（`Program.cs` 在 `vm.Run()` 返回后调一次）。
    ///
    /// **没开过窗也要说一声**：静默不产出文件的话，脚本那边看到的是"文件不存在"，
    /// 而原因可能是"程序是纯文本的"（正常）也可能是"路径写错了"（不正常）——
    /// 这两种必须能分开。
    /// </summary>
    public void EmitRequestedOutputs()
    {
        if (_cfg.FramesDir is not null)
            CliErr.WriteLine($"[vml-host] 共导出 {FramesWritten} 帧到 {Path.GetFullPath(_cfg.FramesDir)}");

        if (_cfg.FramePath is not { } path) return;
        if (!WindowOpened)
        {
            CliErr.WriteLine("[vml-host] 程序没开过绘图窗口 ⇒ --frame 不产出文件（纯文本程序的正常路径）");
            return;
        }
        if (RenderFrame(path)) CliErr.WriteLine($"[vml-host] 画面已写出：{Path.GetFullPath(path)}");
        else CliErr.WriteLine("[vml-host] ⚠ 没有可导出的帧（场景是空的）");
    }

    /// <summary>DSL → PNG 落盘。出图失败**只记一行**（画面是附加信息，不该把整轮判失败）。</summary>
    private static bool TryWritePng(string dsl, string path)
    {
        try
        {
            var doc = DrawRunner.Parse(dsl);
            if (doc.Error is not null)
                CliErr.WriteLine($"[vml-host] ⚠ 这一帧的 DSL 有解析问题：{doc.Error}");
            var png = DrawRunner.ToPng(doc);
            var dir = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(path, png);
            return true;
        }
        catch (Exception ex)
        {
            CliErr.WriteLine($"[vml-host] 导出画面失败：{ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // 对话框：脚本化答案（用完给默认值，**并且说一声**）
    // ══════════════════════════════════════════════════════════════════════

    private int _answerCursor;

    private string? NextAnswer(string what)
    {
        if (_answerCursor < _cfg.Answers.Count) return _cfg.Answers[_answerCursor++];
        CliErr.WriteLine($"[vml-dlg] 答案队列已空，{what} 用默认值"
            + "（要指定就加 --answer，可重复）");
        return null;
    }

    public int DlgMsg(string title, string body, int style)
    {
        var ans = NextAnswer("消息框");
        // 0 = 确定/是，1 = 否/取消（与手机端 `ConfirmAsync` 的 0=是 / 2=否 对齐）
        var result = ans switch
        {
            null => 0,
            "ok" or "yes" or "是" or "确定" or "0" => 0,
            "cancel" or "no" or "否" or "取消" or "1" => 1,
            _ => 0,
        };
        CliErr.WriteLine($"[vml-dlg] msg \"{title}\" \"{body}\" style={style} → {result}"
            + $"{(ans is null ? "" : $"（答案 \"{ans}\"）")}");
        return result;
    }

    public int DlgSelect(string title, string prompt, IReadOnlyList<string> options)
    {
        var ans = NextAnswer("单选");
        var result = ParseIndex(ans, options.Count);
        CliErr.WriteLine($"[vml-dlg] select \"{title}\" [{string.Join(" | ", options)}] → {result}"
            + $"{(ans is null ? "" : $"（答案 \"{ans}\"）")}");
        return result;
    }

    public int DlgMulti(string title, string prompt, IReadOnlyList<string> options)
    {
        var ans = NextAnswer("多选");
        var mask = 0;
        if (ans is not null)
        {
            if (ans is "cancel" or "取消" or "-1") mask = -1;
            else
                foreach (var part in ans.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    var idx = ParseIndex(part.Trim(), options.Count);
                    if (idx >= 0 && idx < 31) mask |= 1 << idx;
                }
        }
        CliErr.WriteLine($"[vml-dlg] multi \"{title}\" [{string.Join(" | ", options)}] → {mask}"
            + $"{(ans is null ? "" : $"（答案 \"{ans}\"）")}");
        return mask;
    }

    public string? DlgInput(string title, string prompt)
    {
        var ans = NextAnswer("输入框");
        if (ans is "cancel" or "取消") ans = null;
        CliErr.WriteLine($"[vml-dlg] input \"{title}\" \"{prompt}\" → {ans ?? "(取消)"}");
        return ans;
    }

    /// <summary>答案文本 → 下标：数字直接用；`cancel`/`-1` 是取消；认不出的按取消处理（响亮地记一行）。</summary>
    private static int ParseIndex(string? s, int count)
    {
        if (s is null) return -1;
        if (s is "cancel" or "取消") return -1;
        if (int.TryParse(s, out var i)) return i >= 0 && i < count ? i : -1;
        CliErr.WriteLine($"[vml-dlg] ⚠ 认不出的答案 \"{s}\"（数字下标 / cancel）—— 按取消处理");
        return -1;
    }

    // ══════════════════════════════════════════════════════════════════════
    // 手感：音效 / 震动
    //
    // 桌面上没有声卡与马达的等价物，但**绝不能静默吞掉**：
    // 合成为一个一行、机器可读的日志。它是最有用的观测点之一 ——
    // 验证游戏逻辑时，`ui_beep` 的频率序列比截图更能说明"哪一步发生了"
    //（本仓真机调试时用同样的办法定位过"连发定时器跑了几拍才被杀"）。
    // ══════════════════════════════════════════════════════════════════════

    public void Tone(int hz, int ms, int wave, int volume)
        => CliErr.WriteLine($"[vml-audio] tone hz={hz} ms={ms} wave={wave} vol={volume}");

    public bool PlayAudio(string fullPath, bool loop)
    {
        CliErr.WriteLine($"[vml-audio] play \"{fullPath}\" loop={(loop ? 1 : 0)}（桌面脚手架不播放，只记录）");
        return true;   // 文件存在性已由共享层判过；这里说"放不了"会把程序的分支带偏
    }

    public void StopAudio() => CliErr.WriteLine("[vml-audio] stop");

    public void SetAudioVolume(int volume) => CliErr.WriteLine($"[vml-audio] volume={volume}");

    public bool Vibrate(int ms, int amplitude)
    {
        CliErr.WriteLine($"[vml-vibrate] ms={ms} amplitude={amplitude}");
        return true;
    }

    public bool VibratePattern(long[] pattern)
    {
        CliErr.WriteLine($"[vml-vibrate] pattern={string.Join(",", pattern)}");
        return true;
    }

    // ══════════════════════════════════════════════════════════════════════
    // 持久化与设备
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 键值存档。默认**只在本次进程内**（不悄悄地往用户目录写文件）；
    /// 要跨运行持久化就 `--store <路径>`（游戏最高分那类才有意义）。
    ///
    /// 结构与手机端 `Preferences` 同一套语义：**键已加过 `vml.` 前缀并清洗**
    /// （见 <see cref="VmlUi.StoreKey"/>），所以不会与宿主自己的设置撞车。
    /// </summary>
    private Dictionary<string, string>? _store;

    private Dictionary<string, string> Store
    {
        get
        {
            if (_store is not null) return _store;
            _store = new Dictionary<string, string>(StringComparer.Ordinal);
            if (_cfg.StorePath is { } path && File.Exists(path))
            {
                try
                {
                    // 复用仓库自己的手写 JSON（AOT 安全、零反射），不为存档再造一个格式。
                    if (Json.TryParse(File.ReadAllText(path), out var node) && node is not null)
                        foreach (var (key, value) in node.Entries)
                            if (value.AsString() is { } s) _store[key] = s;
                }
                catch (Exception ex)
                {
                    CliErr.WriteLine($"[vml-host] ⚠ 读存档失败（按空存档继续）：{ex.Message}");
                }
            }
            return _store;
        }
    }

    private void SaveStore()
    {
        if (_cfg.StorePath is not { } path || _store is null) return;
        try
        {
            var obj = JNode.Object();
            foreach (var kv in _store) obj.Set(kv.Key, kv.Value);
            File.WriteAllText(path, Json.Serialize(obj, indent: true), new UTF8Encoding(false));
        }
        catch (Exception ex)
        {
            CliErr.WriteLine($"[vml-host] ⚠ 写存档失败：{ex.Message}");
        }
    }

    public string? StoreGet(string key) => Store.TryGetValue(key, out var v) ? v : null;

    public void StoreSet(string key, string value) { Store[key] = value; SaveStore(); }

    public void StoreDel(string key) { Store.Remove(key); SaveStore(); }

    public void KeepScreenOn(bool on) => CliErr.WriteLine($"[vml-host] keep-screen-on={on}");

    // ══════════════════════════════════════════════════════════════════════
    // 杂项
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 把相对路径解析成绝对路径。**根 = 源文件所在目录** ——
    /// 与 `RunProgram` 里给 VM 的 `FileSystemRoot` 同源，也就是"这个程序的沙箱"。
    /// （手机上的等价物是 `CwdContext.Root` = workspace；两端用各自那把尺子，
    ///   但"哪把尺子"这件事各端只有一处。）
    /// </summary>
    public string ResolvePath(string relative) => Path.GetFullPath(Path.Combine(_cfg.WorkDir, relative));

    /// <summary>
    /// 桌面端特有的 `CALLJSON` 函数：`version` 与 `sysinfo`。
    ///
    /// 数值来自**桌面环境**，`app`/`version` 报的是"桌面脚手架"而不是 `Global.Version`
    /// —— 这个 CLI 刻意不引用 WayCoder 核心（csproj 里写着只引 vendored 的 `third_party/vml`），
    /// 拿不到那个常量。**这是有意为之，不是漏了**：与其抄一个会漂的版本号进来，
    /// 不如如实说"这是桌面脚手架"。
    /// （`echo` / `screen` 两个语义必须逐字一致的由共享层注册，见 `VmlHostRuntime`。）
    /// </summary>
    public void RegisterJsonHandlers()
    {
        VmlJsonApi.Register("version", _ => JNode.Object()
            .Set("app", "WayCoder")
            .Set("cn", "道码")
            .Set("version", "(desktop-cli)")
            .Set("platform", "desktop"));

        VmlJsonApi.Register("sysinfo", _ =>
        {
            var area = ScreenArea();
            return JNode.Object()
                .Set("app", "WayCoder")
                .Set("version", "(desktop-cli)")
                .Set("platform", "desktop")
                .Set("os", Environment.OSVersion.Platform.ToString())
                .Set("osVersion", Environment.OSVersion.VersionString)
                .Set("deviceModel", "(desktop)")
                .Set("deviceName", "(desktop)")
                .Set("manufacturer", "(desktop)")
                .Set("arch", System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture
                    .ToString().ToLowerInvariant())
                .Set("cpuCount", Environment.ProcessorCount)
                .Set("memoryMb", GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024))
                .Set("deviceId", "(desktop-cli)")
                // ⚠ 屏幕这几项**必须与 `SCR_W`/`SCR_H`/`SCR_ORIENT` 同源**
                //（从前这里写的是死 0，于是同一台机器上"JSON 报的"和"syscall 报的"
                //  两个答案 —— 那正是最难查的一类分叉）。
                .Set("screen", JNode.Object()
                    .Set("w", area.Width)
                    .Set("h", area.Height)
                    .Set("density", 1)
                    .Set("canvasW", area.Width)
                    .Set("canvasH", area.Height))
                .Set("orientation", Orientation());
        });
    }

    public void Log(string message) => CliErr.WriteLine($"[vml-host] {message}");

    // ══════════════════════════════════════════════════════════════════════
    // 脚本化输入
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 起一个投递线程，按脚本给的时间点把输入事件灌进消息队列。
    ///
    /// ## 为什么必须有它
    ///
    /// 手机端程序靠 `ui_wait_msg` / `ui_poll` 收 `VML_MSG_KEYDOWN` 之类（见 `VmlUiProtocol.cs`
    /// 的 `VmlMsgType`）。桌面端没有任何输入源 ⇒ 任何交互程序只能空转，
    /// 「按键有没有接对」「连发/暂停/重开走不走得通」一件都验不了。
    ///
    /// ## 时间基准
    ///
    /// 相对**调用本方法的时刻**（也就是 `vm.Run()` 之前那一瞬）。投递走的是与手机端
    /// UI 线程**同一条路**（<see cref="VmlHostRuntime.Post"/>），VM 线程在队列上阻塞等待 ——
    /// 所以时序语义与真机一致，不需要在 VM 侧加任何东西。
    ///
    /// ⚠ 脚本是**墙钟**驱动的，不是"程序读到哪一步"驱动的：与真机的操作时序同类，
    /// 但对"每帧耗时随机器变"的程序，某条按键落在第几帧上会有抖动。
    /// 要严格确定就把它写成"先等窗口稳定、再发键"的形状（脚本里用大一点的时间差）。
    /// </summary>
    public void StartInputScript()
    {
        if (_cfg.InputEvents is not { Count: > 0 } script || Runtime is null) return;

        var rt = Runtime;
        var clock = Stopwatch.StartNew();
        var thread = new Thread(() =>
        {
            foreach (var ev in script)
            {
                var wait = ev.TimeMs - (int)clock.ElapsedMilliseconds;
                if (wait > 0) Thread.Sleep(wait);
                try { ev.Apply(rt); }
                catch (Exception ex) { CliErr.WriteLine($"[vml-host] 输入事件投递失败：{ex.Message}"); }
            }
        })
        { IsBackground = true, Name = "vml-input-script" };
        thread.Start();
        CliErr.WriteLine($"[vml-host] 输入脚本已装载：{script.Count} 条事件（{_cfg.InputPath}）");
    }
}

/// <summary>
/// 一条脚本化输入事件。
///
/// 事件名与 <see cref="VmlMsgType"/> 一一对应 —— **不另造一套名字**：
/// 程序那边看到的类型就是这些，脚本要是自造一套，"脚本里写 keydown、程序收到 KeyDown"
/// 这层对应关系就又多了一张要人工同步的表。
/// </summary>
internal sealed record CliInputEvent(int TimeMs, string Action, int A, int B)
{
    /// <summary>投递（或执行）这条事件。</summary>
    public void Apply(VmlHostRuntime rt)
    {
        switch (Action)
        {
            case "keydown": rt.PostInput(VmlMsgType.KeyDown, A, B); break;
            case "keyup": rt.PostInput(VmlMsgType.KeyUp, A, B); break;
            case "mousemove": rt.PostInput(VmlMsgType.MouseMove, A, B); break;
            case "mousedown": rt.PostInput(VmlMsgType.MouseDown, A, B); break;
            case "mouseup": rt.PostInput(VmlMsgType.MouseUp, A, B); break;
            case "touchdown": rt.PostInput(VmlMsgType.TouchDown, A, B); break;
            case "touchmove": rt.PostInput(VmlMsgType.TouchMove, A, B); break;
            case "touchup": rt.PostInput(VmlMsgType.TouchUp, A, B); break;
            case "resize": rt.PostInput(VmlMsgType.WindowResize, A, B); break;
            case "orient": rt.PostInput(VmlMsgType.WindowOrient, A, B); break;
            // `close` = 用户点了窗口的返回箭头。**两件事一起做**（置位 + 投消息），
            // 见 `VmlHostRuntime.MarkWindowClosed` —— 只投消息的话 `ui_win_closed()` 仍报 0，
            // 那套 `while (ui_win_closed() == 0)` 的主循环就出不来。
            case "close": rt.MarkWindowClosed(); break;
            // `wait` 只是"空一拍"，不投任何消息（写脚本时用来拉开节奏）。
            case "wait": break;
        }
    }
}

/// <summary>
/// 输入脚本的读入与校验。格式：每行 <c>&lt;毫秒&gt; &lt;事件名&gt; [参数…]</c>，
/// `#`/`;` 起头是注释。
///
/// <code>
/// # 时间(ms)  事件
/// 0     keydown 13      ; 回车
/// 60    keyup 13
/// 500   keydown 37      ; 左
/// 560   keyup 37
/// 900   touchup 100 200
/// 1200  resize 480 800
/// 1500  close
/// </code>
///
/// ⚠ **时间可省略**：省略时沿用上一条的时间（= 同一拍连投）。
/// 键码沿用 Win32 虚拟键值（见 <see cref="VmlKeys"/>）——
/// 与手机屏幕手柄、与桌面物理键盘**同一张表**，脚本里直接写 37 就是左方向键。
/// </summary>
internal static class CliInputScript
{
    private static readonly HashSet<string> Known = new(StringComparer.Ordinal)
    {
        "keydown", "keyup", "mousemove", "mousedown", "mouseup",
        "touchdown", "touchmove", "touchup", "resize", "orient", "close", "wait",
    };

    /// <summary>
    /// 读脚本。**认不出的行一律响亮地报错**（返回 null），不静默跳过 ——
    /// 一条打错的按键（比如把 `keydown` 写成 `keyDown`）静默丢掉的话，
    /// 表现是"程序没反应"，而排查时你会先去怀疑程序。
    /// </summary>
    public static List<CliInputEvent> Load(string path)
    {
        if (!File.Exists(path)) throw new CliArgumentException($"输入脚本不存在：{path}");
        var events = new List<CliInputEvent>();
        var time = 0;
        var lineNo = 0;

        foreach (var raw in File.ReadAllLines(path))
        {
            lineNo++;
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith(';')) continue;

            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            var i = 0;
            if (int.TryParse(parts[0], out var t)) { time = t; i = 1; }
            if (i >= parts.Length)
                throw new CliArgumentException($"{path}:{lineNo} 只有时间没有事件");
            var action = parts[i++].ToLowerInvariant();
            if (!Known.Contains(action))
                throw new CliArgumentException(
                    $"{path}:{lineNo} 认不出的事件 `{action}`；可用：{string.Join(" / ", Known.OrderBy(x => x, StringComparer.Ordinal))}");

            var a = 0; var b = 0;
            // 参数个数就是"这条消息填几个槽"：`close`/`wait` 不填、按键只填 A、
            // 指针与窗口尺寸填 A+B、`orient` 只填 A（B 是预留位，见 VmlMsgType.WindowOrient）。
            var needsArgs = action switch
            {
                "close" or "wait" => 0,
                "keydown" or "keyup" or "orient" => 1,
                _ => 2,
            };
            if (i + needsArgs > parts.Length)
                throw new CliArgumentException($"{path}:{lineNo} 事件 `{action}` 需要 {needsArgs} 个参数");
            if (needsArgs >= 1 && !int.TryParse(parts[i], out a))
                throw new CliArgumentException($"{path}:{lineNo} 参数不是整数：`{parts[i]}`");
            if (needsArgs >= 2 && !int.TryParse(parts[i + 1], out b))
                throw new CliArgumentException($"{path}:{lineNo} 参数不是整数：`{parts[i + 1]}`");

            events.Add(new CliInputEvent(time, action, a, b));
        }
        return events;
    }
}

/// <summary>
/// 宿主相关的那些命令行选项 —— 挂在 `CliOptions` 上（那个类是 `partial`）。
///
/// 为什么拆到这个文件：`Program.cs` 那段 `switch` 是**编译/运行参数**的解析，
/// 而下面这几个只跟"宿主怎么表现"有关。放在一起的话，`Program.cs` 会越来越难读，
/// 而它已经有另一个明确的职责（编译 + 运行流水线，要与 `MauiVml` 逐步对齐）。
/// 认领不了就返回 false，交回主解析器按"未知选项"报错 —— **绝不静默跳过**
/// （本仓的 CLI 铁律：拼错的选项必须响亮地失败，否则用户以为它生效了）。
/// </summary>
internal sealed partial class CliOptions
{
    /// <summary>`--frame <路径>`：运行结束后把最新呈现帧渲成 PNG 写到这里。</summary>
    public string? FramePath { get; private set; }
    /// <summary>`--frames <目录>`：每个 `ui_present` 落一帧（看动画用）。</summary>
    public string? FramesDir { get; private set; }
    /// <summary>`--frames-max N`：帧数上限（默认 120，防死循环程序写满磁盘）。</summary>
    public int FramesMax { get; private set; } = 120;
    /// <summary>`--store <路径>`：跨运行的键值存档（不给就只在本次进程内）。</summary>
    public string? StorePath { get; private set; }
    /// <summary>`--input <路径>`：脚本化输入事件表（见 <see cref="CliInputScript"/>）。</summary>
    public string? InputPath { get; private set; }
    /// <summary>`--input` 读进来的事件表（解析期就装好，见上面那条注释）。</summary>
    public List<CliInputEvent>? InputEvents { get; private set; }
    /// <summary>`--answer <值>`（可重复）：对话框按顺序消费的答案。</summary>
    public List<string> Answers { get; } = new();
    /// <summary>`--screen WxH`：可用绘图区（默认 480×640）。</summary>
    public int ScreenWidth { get; private set; } = 480;
    public int ScreenHeight { get; private set; } = 640;

    /// <summary>认领宿主相关的选项；不是它们就返回 false（交回主解析器报错）。</summary>
    public bool TryParseHostOption(string a, string[] args, ref int i)
    {
        switch (a)
        {
            case "--frame":
                FramePath = Require(args, ref i, "--frame");
                return true;
            case "--frames":
                FramesDir = Require(args, ref i, "--frames");
                return true;
            case "--frames-max":
                FramesMax = ParsePositive(Require(args, ref i, "--frames-max"), "--frames-max");
                return true;
            case "--store":
                StorePath = Require(args, ref i, "--store");
                return true;
            case "--input":
                // ⚠ **在解析期就把脚本读进来并校验**，不是在跑的时候现读：
                //   脚本格式写错（拼错事件名、少一个参数）属于"参数错了"，
                //   就该和 `--modle` 一样当场 `✘ 消息 + 退出码 2`（CLI 铁律①「有错即报错」）。
                //   实测过一次反例：`orient 1`（少一个参数）时 parser 抛在 `RunProgram` 深处，
                //   用户看到的是**一屏 .NET 堆栈**（`Unhandled exception … CliArgumentException`），
                //   既不像参数错误、也看不出是脚本第几行。
                InputPath = Require(args, ref i, "--input");
                InputEvents = CliInputScript.Load(InputPath);
                return true;
            case "--answer":
                Answers.Add(Require(args, ref i, "--answer"));
                return true;
            case "--screen":
            {
                var spec = Require(args, ref i, "--screen");
                var parts = spec.Split('x', 'X');
                if (parts.Length != 2
                    || !int.TryParse(parts[0], out var w) || !int.TryParse(parts[1], out var h)
                    || w <= 0 || h <= 0)
                    throw new CliArgumentException($"--screen 需要 `宽x高`（如 480x640），收到 `{spec}`");
                ScreenWidth = w;
                ScreenHeight = h;
                return true;
            }
            default:
                return false;
        }
    }

    private static int ParsePositive(string text, string name)
        => int.TryParse(text, out var v) && v > 0
            ? v
            : throw new CliArgumentException($"{name} 需要一个正整数，收到 `{text}`");

    /// <summary>把解析结果装成宿主配置（`sourcePath` 决定沙箱根）。</summary>
    public CliHostConfig ToHostConfig(string sourcePath)
    {
        var cfg = new CliHostConfig
        {
            // 沙箱根 = 源文件所在目录：与 `RunProgram` 给 VM 的 `FileSystemRoot` **同源**，
            // 也就是"这个程序的沙箱"。两边各算一份的话，BGM 路径与程序自己 `open()` 的
            // 相对基准就会不是同一个目录。
            WorkDir = Path.GetDirectoryName(Path.GetFullPath(sourcePath)) ?? Directory.GetCurrentDirectory(),
            ScreenWidth = ScreenWidth,
            ScreenHeight = ScreenHeight,
            FramePath = FramePath,
            FramesDir = FramesDir,
            FramesMax = FramesMax,
            StorePath = StorePath,
            InputPath = InputPath,
            InputEvents = InputEvents,
            TimeoutSeconds = TimeoutSeconds,
        };
        cfg.Answers.AddRange(Answers);
        return cfg;
    }
}

/// <summary>桌面宿主的配置（`Program.cs` 从命令行参数装好递进来）。</summary>
internal sealed class CliHostConfig
{
    /// <summary>沙箱根 = 源文件所在目录（与 VM 的 `FileSystemRoot` 同源）。</summary>
    public string WorkDir = ".";
    /// <summary>可用绘图区（`SCR_W`/`SCR_H`）。</summary>
    public int ScreenWidth = 480;
    public int ScreenHeight = 640;
    /// <summary>`--frame <路径>`：运行结束后把最新呈现帧落成 PNG。</summary>
    public string? FramePath;
    /// <summary>`--frames <目录>`：每个 `ui_present` 落一帧（看动画用）。</summary>
    public string? FramesDir;
    /// <summary>`--frames-max N`：帧数上限（防一个死循环程序写满磁盘）。</summary>
    public int FramesMax = 120;
    /// <summary>`--store <路径>`：跨运行的键值存档（不给就只在本次进程内）。</summary>
    public string? StorePath;
    /// <summary>`--input <路径>`：脚本化输入事件表（日志里报路径用）。</summary>
    public string? InputPath;
    /// <summary>`--input` 读进来的事件表（解析期就装好，见 `CliOptions.TryParseHostOption`）。</summary>
    public List<CliInputEvent>? InputEvents;
    /// <summary>`--answer <值>`（可重复）：对话框按顺序消费的答案。</summary>
    public readonly List<string> Answers = new();
    /// <summary>`--timeout` 的秒数 —— 用来给"无限阻塞等待"设兜底上限（见 BlockingWaitLimitMs）。</summary>
    public int TimeoutSeconds = 30;
}

/// <summary>
/// <see cref="ISystemCallHandler"/> 的桌面实现 —— **那层薄壳**（与手机端
/// `WayCoder.Maui/Services/VmlUiCalls.cs` 的 `VmlUiCalls` 一一对应）。
///
/// 逻辑一行没有：全在 <see cref="VmlHostRuntime"/>（两端编同一份文件）。
/// 本类只做两件事：
///   ① 把 500–599 加进运行时的用户态白名单（**漏了这步的现象是
///      「处理器注册了却永远不被调用」**——mcu 模式下 dispatch 顶部先按白名单拒掉）；
///   ② 把 VM 的寄存器递进去，并把 `float8`/`long4`/`double4` 要用的那三组寄存器
///      从运行时上接过去（`int[] registers` 里只有 32 位通用寄存器，
///      64 位值在其中只剩低半截镜像）。
/// </summary>
internal sealed class CliUiCalls : ISystemCallHandler
{
    private readonly CliVmlHost _host;
    private readonly VmlHostRuntime _rt;

    public CliUiCalls(CliHostConfig cfg)
    {
        // **先抓住真实的 stderr**：下一行起，所有宿主日志都走它 ——
        // 再往后 `Program.RunProgram` 会把 `Console.Error` 换成内存缓冲（见 CliErr）。
        CliErr.Capture();
        _host = new CliVmlHost(cfg);
        // 日志出口：本文件不引用任何一个宿主的日志设施（见 VmlCallRegistry.Log 的说明），
        // 桌面端就写 stderr —— 那是"编译/运行日志"该去的地方（stdout 留给程序自己的输出）。
        VmlCallRegistry.Log ??= msg => CliErr.WriteLine(msg);
        // 注册本批那族自检调用口：**与手机端同一批 id、同一份实现**（实现在 UI/Shared 里）
        VmlCallRegistry.RegisterDefaults(VmlCallRegistry.HostDesktop);

        _rt = new VmlHostRuntime(_host) { BlockingWaitLimitMs = Math.Max(1, cfg.TimeoutSeconds) * 1000 };
        _host.Runtime = _rt;
    }

    /// <summary>桌面宿主（`Program.cs` 用它出图 / 装输入脚本）。</summary>
    public CliVmlHost Host => _host;

    /// <summary>共享运行时（诊断用）。</summary>
    public VmlHostRuntime Runtime => _rt;

    static CliUiCalls()
    {
        for (var n = 500; n <= 599; n++) SyscallConstants.UserAllowed.Add(n);
    }

    /// <summary>
    /// `VmRuntime` —— **通用宿主调用口（577–580）需要它**：那四个口的参数躺在
    /// 浮点/长整数/双精度寄存器组里，而那三组**不在** `int[] registers` 这个参数里
    /// （它是 32 位的，装不下 64 位值，只有低 32 位的镜像）。宿主得从运行时上取那三组
    /// 现成的数组（活引用，写进去就是写进 VM）。
    /// </summary>
    public VmRuntime? Vm
    {
        get => _vm;
        set
        {
            _vm = value;
            _rt.FloatRegisters = value?.FloatRegisters;
            _rt.DoubleRegisters = value?.DoubleRegisters;
            _rt.LongRegisters = value?.LongRegisters;
        }
    }
    private VmRuntime? _vm;

    public bool HandleSyscall(int syscallNumber, int[] registers, byte[] memory, ref int pc)
        => _rt.HandleSyscall(syscallNumber, registers, memory);
}
