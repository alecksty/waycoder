using WayCoder.UI.Shared;

namespace WayCoder.Maui.Services;

/// <summary>
/// **虚拟机的可调参数**（Preferences 持久）：内存、栈、两个运行超时。
///
/// <para>
/// 出厂值仍取 <see cref="VmlVmDefaults"/>（用户定的 16M / 1M）—— 这里只是把"能不能改"接出来，
/// **不是另立一份真源**：默认值一律从那边派生，改常量时这里跟着走。
/// </para>
///
/// <para>
/// ⚠ **内存与栈改完要下一次运行才生效**（VM 实例是每次运行新建的）。
/// 设置项旁边要写清这一点，否则就是"改了没反应"。
/// </para>
///
/// <para>
/// ⚠ **栈不能超过内存的一半** —— `VmConfig.StackSize` 的 setter 会按 `MemorySize / 2` **静默钳位**
/// （见 <c>VmlVmDefaults</c> 的注释：那是踩过的坑）。所以候选档位刻意配成
/// 「最小内存 4MB、最大栈 2048KB」，无论怎么组合都不会撞上那条静默钳位。
/// </para>
/// </summary>
public static class MauiVmStore
{
    private const string KeyMem = "vml.mem.mb";
    private const string KeyStack = "vml.stack.kb";
    private const string KeyEditorTimeout = "vml.timeout.editor";
    private const string KeyShellTimeout = "vml.timeout.shell";

    /// <summary>内存候选（MB）。下限 4 是为了让最大的栈（2048KB）也不会超过它的一半。</summary>
    public static readonly int[] MemoryOptions = [4, 8, 16, 32, 64, 128];

    /// <summary>栈候选（KB）。上限 2048 = 最小内存 4MB 的一半，正好压在那条静默钳位之下。</summary>
    public static readonly int[] StackOptions = [64, 256, 512, 1024, 2048];

    /// <summary>
    /// 超时候选（秒）。**上限 600 是硬的**：`MauiVml.RunProgram` 里那句
    /// `Math.Clamp(timeoutSeconds, 1, 600)` 就是这个数 —— 候选表超过它只会让用户
    /// "选了 1800、实际跑的是 600"，那比不给选更糟。
    /// </summary>
    /// <summary>
    /// 超时档位。**`0` = 不限时**（`VmRuntime.TimeoutSeconds &lt;= 0` 的约定）。
    ///
    /// <para>
    /// 加这一档是因为 v0.96.440 起"有输入就不计时"（触摸/按键都续期）——
    /// 于是超时只剩一个用途：**兜住失控程序**。而"玩家盯着棋盘想了十分钟、一次没碰屏幕"
    /// 是正常行为，不该被杀；真不想要这个兜底的人现在能自己关掉。
    /// </para>
    /// </summary>
    public static readonly int[] TimeoutOptions = [0, 30, 60, 120, 300, 600];

    public const int DefaultMemoryMb = VmlVmDefaults.MemoryBytes / (1024 * 1024);
    public const int DefaultStackKb = VmlVmDefaults.StackBytes / 1024;

    /// <summary>编辑器里点「运行」跑 VML 程序的超时（秒）。</summary>
    public const int DefaultEditorTimeoutSec = 120;

    /// <summary>命令行页 `vml run` 的超时（秒）。</summary>
    public const int DefaultShellTimeoutSec = 600;

    /// <summary>
    /// VM 内存（MB）。**超时是"连续执行"的**（等输入/等弹框不计时，见 <c>VmRuntime.ResetTimeout</c>），
    /// 所以调大调小的意义与从前不同 —— 现在它只在"程序真的连跑 N 秒没等过"时才生效。
    /// </summary>
    public static int MemoryMb
    {
        get => Pick(KeyMem, DefaultMemoryMb, MemoryOptions);
        set { try { Preferences.Set(KeyMem, value); } catch { } }
    }

    /// <summary>VM 栈（KB）。</summary>
    public static int StackKb
    {
        get => Pick(KeyStack, DefaultStackKb, StackOptions);
        set { try { Preferences.Set(KeyStack, value); } catch { } }
    }

    /// <summary>编辑器运行的超时（秒）。</summary>
    public static int EditorTimeoutSec
    {
        get => Pick(KeyEditorTimeout, DefaultEditorTimeoutSec, TimeoutOptions);
        set { try { Preferences.Set(KeyEditorTimeout, value); } catch { } }
    }

    /// <summary>命令行页运行的超时（秒）。</summary>
    public static int ShellTimeoutSec
    {
        get => Pick(KeyShellTimeout, DefaultShellTimeoutSec, TimeoutOptions);
        set { try { Preferences.Set(KeyShellTimeout, value); } catch { } }
    }

    /// <summary>字节形式，直接喂 <c>VmConfig</c>。</summary>
    public static int MemoryBytes => MemoryMb * 1024 * 1024;

    /// <inheritdoc cref="MemoryBytes"/>
    public static int StackBytes => StackKb * 1024;

    /// <summary>
    /// 读一个候选档：**存的值不在候选表里就回默认**（老配置、手改坏的 Preferences、
    /// 或者我们后来把某个档位从表里去掉了）。直接采信存值的话，
    /// Picker 会因为没有匹配项而显示空 —— 用户看到的是"设置项坏了"。
    /// </summary>
    private static int Pick(string key, int fallback, int[] options)
    {
        try
        {
            var v = Preferences.Get(key, fallback);
            return Array.IndexOf(options, v) >= 0 ? v : fallback;
        }
        catch { return fallback; }
    }

    /// <summary>设置页摘要行用的一句话。</summary>
    /// <summary>超时档位的**显示文本**（唯一实现）—— **`0` 是"不限"，不是"0 秒"**。</summary>
    public static string TimeoutText(int v) => v <= 0 ? "不限" : $"{v} 秒";

    /// <summary>设置页摘要行用的一句话。</summary>
    public static string Summary()
        => $"内存 {MemoryMb}M · 栈 {StackKb}K · 超时 {TimeoutText(EditorTimeoutSec)}/{TimeoutText(ShellTimeoutSec)}";
}
