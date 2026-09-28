namespace WayCoder.Maui.Services;

/// <summary>
/// **编译器行为参数**（Preferences 持久）：优化级别、警告级别、警告当错误、调试输出、浮点/64 位模式。
///
/// <para>
/// 这些值对应 <c>vmltool.config.xml</c> 里的 <c>OptimizationLevel</c> / <c>WarningLevel</c> /
/// <c>WarningsAsErrors</c> / <c>Debug</c> / <c>Float32</c> / <c>Float64</c> / <c>Int64</c> ——
/// 但**手机端从前一个字都不读那份 XML**（只读它的库清单，见 <see cref="VmlLibConfig"/>），
/// 所以这里的取值就是**唯一来源**。
/// </para>
///
/// <para>
/// ⚠ <b>为什么不是"XML 里显式写了就以 XML 为准"</b>：那份 XML 随标准库一起解压，里面**恒有**
/// <c>&lt;OptimizationLevel&gt;0&lt;/OptimizationLevel&gt;</c>、<c>&lt;Float64&gt;hard&lt;/Float64&gt;</c>
/// 这些元素 —— 无从区分"用户显式指定 0"与"出厂默认 0"。按"XML 优先"做，结果就是设置**永远不生效**。
/// （将来若 <c>.vmk</c> 工程文件要支持这几项，规则是 <c>.vmk</c> &gt; 这里。）
/// </para>
///
/// <para>
/// ⚠ <b>默认值</b>（2026-09-28 起）：<b>优化 2 / 警告 0 / 警告当错误 关 / 调试 关 / 浮点 hard</b>。
/// 除优化外其余各项仍等于接入前的行为；<b>优化默认由 0 改成 2</b> 是用户拍板的
/// （「可以开启默认2级优化」）—— 依据是 <c>OptimizationPolicy</c> 里那三个死代码消除缺陷
/// 2026-09-27 已修完，判据 <c>scripts/vml-opt-probe</c> 全绿（O2 把 hello.c 从 68339 条
/// 压到 28 条，且 22 门语言 O0/O2 输出逐字节相同）。
/// </para>
///
/// <para>
/// ⚠ 老用户存过的值优先（<see cref="Pick"/> 读 Preferences 命中就用存值）——
/// 改默认值只影响**从没动过这一项**的人；已经显式选过"关闭"的不会被悄悄改掉。
/// </para>
///
/// <para>
/// ⚠ 编译参数**下一次编译才生效**（与虚拟机那几项同款）—— 设置页的说明行必须写这一点。
/// </para>
/// </summary>
public static class MauiCompileStore
{
    private const string KeyOpt = "vml.compile.opt";
    private const string KeyWarn = "vml.compile.warn";
    private const string KeyWarnErr = "vml.compile.werr";
    private const string KeyDebug = "vml.compile.debug";
    private const string KeyFloat = "vml.compile.float";
    private const string KeyInt64 = "vml.compile.int64";

    /// <summary>
    /// 优化级别候选：**关闭 / 初步 / 中度 / 极致**（用户 2026-09-27 定的四档语义）。
    ///
    /// <list type="bullet">
    /// <item><b>0 关闭</b>：一条指令都不删（接入优化之前的行为；已不是默认档）；</item>
    /// <item><b>1 初步</b>：NOP 消除 + 清掉跳转之后的填充代码。**不删任何函数**，产物大小基本不变；</item>
    /// <item><b>2 中度</b>：加上**死代码消除（可达性分析）** —— 删掉没被调用的库函数。
    ///   这是真正让产物变小的那一档（实测 hello world 69637 → 28 条；俄罗斯方块 74754 → 6282）；</item>
    /// <item><b>3 极致</b>：再加三件安全的小事（删"跳到自己下一条"的 jmp、清数据段、去 `.linked` 声明）。
    ///   ⚠ **不是"更激进的删除"** —— 用户定的规矩是「可以保留多，不能多删除」，
    ///   所以极致档不做任何"赌一把"的分析。</item>
    /// </list>
    ///
    /// <para>
    /// ⚠ 上游那几个 pass（常量折叠/跳转链/死存储/复写传播/窥孔）**不在任何一档里**：
    /// 实测在 O3 打开后 22 门语言的输出全线出错（详见 <c>OptimizationPolicy</c> 的注释）。
    /// </para>
    /// </summary>
    public static readonly int[] OptimizationOptions = [0, 1, 2, 3];

    /// <summary>警告级别候选（对应 <c>WarningLevel</c>：0=不报 / 1=-Wall / 2=-Wextra）。</summary>
    public static readonly int[] WarningOptions = [0, 1, 2];

    /// <summary>
    /// 浮点/64 位模式候选。**没有 <c>soft</c>** —— 软浮点要 <c>softfloat.vml</c>/<c>softdouble.vml</c>/
    /// <c>softint64.vml</c> 三个库，而本平台的 <c>Lib/</c> 里没有它们（枚举注释写明），
    /// 选了只会得到跑不起来的程序。桌面 <c>vmlcli</c> 对 <c>soft</c> 也是**直接报错拒绝**，两边同一口径。
    /// </summary>
    public static readonly string[] NumberModeOptions = ["hard", "none"];

    /// <summary>
    /// 出厂默认优化级别 = <b>2 中度</b>（2026-09-28 用户拍板「可以开启默认2级优化」）。
    /// 单独提成常量是为了让"默认值"只有一处 —— 文档、判据、实现都引它。
    /// </summary>
    public const int DefaultOptimizationLevel = 2;

    /// <summary>优化级别（0 关闭 / 1 初步 / 2 中度 / 3 极致）。**默认 2 = 中度（死代码消除）**。</summary>
    public static int OptimizationLevel
    {
        get => Pick(KeyOpt, DefaultOptimizationLevel, OptimizationOptions);
        set { try { Preferences.Set(KeyOpt, value); } catch { } }
    }

    /// <summary>警告级别（0 不报 / 1 基本 / 2 更多）。**默认 0** —— 与从前一致（从前是全部丢弃）。</summary>
    public static int WarningLevel
    {
        get => Pick(KeyWarn, 0, WarningOptions);
        set { try { Preferences.Set(KeyWarn, value); } catch { } }
    }

    /// <summary>警告当错误（<c>-Werror</c>）。</summary>
    public static bool WarningsAsErrors
    {
        get => Flag(KeyWarnErr);
        set { try { Preferences.Set(KeyWarnErr, value); } catch { } }
    }

    /// <summary>调试输出（前端里的诊断分支 + 编译结束时的警告计数）。</summary>
    public static bool DebugOutput
    {
        get => Flag(KeyDebug);
        set { try { Preferences.Set(KeyDebug, value); } catch { } }
    }

    /// <summary>浮点（float/double）模式。</summary>
    public static string FloatMode
    {
        get => PickString(KeyFloat, "hard");
        set { try { Preferences.Set(KeyFloat, value); } catch { } }
    }

    /// <summary>64 位整数（long/int64）模式。</summary>
    public static string Int64Mode
    {
        get => PickString(KeyInt64, "hard");
        set { try { Preferences.Set(KeyInt64, value); } catch { } }
    }

    /// <summary>
    /// 一次编译用的**快照** —— 编译是异步的（手机上一两分钟），中途别去反复读 Preferences：
    /// 一次编译内的取值必须自洽（否则可能出现"警告级别读到了、调试分支读到的是改之前的值"）。
    /// </summary>
    public static CompileOptions Current => new(
        OptimizationLevel, WarningLevel, WarningsAsErrors, DebugOutput, FloatMode, Int64Mode);

    // ── 文案（显示与取值分离：文案改一个字不该动到存进去的值）──

    /// <summary>
    /// 优化级别显示文本（用户定的四档语义：不优化 / 初步 / 中度 / 极致）。
    ///
    /// ⚠ 文案与 <see cref="OptimizationOptions"/> 一一对应 —— 改档位记得一起改这里，
    /// 否则会出现"下拉框写着中度、实际按初步跑"（本仓头号坑"平行表漂移"）。
    /// </summary>
    public static string OptimizationText(int v) => v switch
    {
        <= 0 => "关闭",
        1 => "初步",
        2 => "中度",
        _ => "极致",
    };

    /// <summary>警告级别显示文本（括号里是等价的命令行开关，便于对照桌面 `vmlcli`）。</summary>
    public static string WarningText(int v) => v switch
    {
        <= 0 => "不报",
        1 => "基本（-Wall）",
        _ => "更多（-Wextra）",
    };

    /// <summary>数值模式显示文本。<c>none</c> 是"遇到浮点/64 位代码直接报错"，不是"优雅降级"。</summary>
    public static string NumberModeText(string m) => m == "none" ? "关闭（遇到就报错）" : "硬件";

    /// <summary>设置页摘要行用的一句话（首页那行由它给，**取值与文案同一处**）。</summary>
    public static string Summary()
        => $"优化 {OptimizationText(OptimizationLevel)} · 警告 {WarningText(WarningLevel)} · 浮点 {NumberModeText(FloatMode)}";

    // ── 读取兜底 ──

    /// <summary>
    /// 读一个候选档：**存的值不在候选表里就回默认**（老配置、手改坏的 Preferences、
    /// 或者我们后来把某个档位从表里去掉了）。直接采信存值的话，Picker 会因为没有匹配项而显示空。
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

    /// <summary>字符串档同款兜底（语义与 <see cref="Pick"/> 一致）。</summary>
    private static string PickString(string key, string fallback)
    {
        try
        {
            var v = Preferences.Get(key, fallback);
            return Array.IndexOf(NumberModeOptions, v) >= 0 ? v : fallback;
        }
        catch { return fallback; }
    }

    private static bool Flag(string key)
    {
        try { return Preferences.Get(key, false); }
        catch { return false; }
    }
}

/// <summary>
/// 一次编译用的编译器行为参数快照（见 <see cref="MauiCompileStore.Current"/>）。
/// 字段名与 <c>CompilerConfig.SetConfig</c> 的键名一一对应，便于在应用点逐条对上。
/// </summary>
public sealed record CompileOptions(
    int OptimizationLevel,
    int WarningLevel,
    bool WarningsAsErrors,
    bool DebugOutput,
    string FloatMode,
    string Int64Mode);
