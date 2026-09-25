using System.Globalization;
using System.Text;

namespace WayCoder.UI.Shared;

/// <summary>
/// 一次 VML 运行的**状态快照** —— 「VM 状态面板」显示的就是它。
///
/// <para>
/// 字段**只用基元类型与数组**，刻意不引用 <c>VMLRuntime</c>：共享层（<c>UI/Shared</c>）被主工程
/// 与 MAUI 一起编译，而**主工程不引用 VMLRuntime**（引了就没法跑桌面自测）——
/// 与 <see cref="VmlCallRegistry"/>、<see cref="VmlHostRuntime"/> 是同一条约束。
/// 采集那一侧（`VmlUiCalls.CaptureStatus`）负责把 `VmRuntime` 上的活数据填进来。
/// </para>
///
/// <para>
/// <b>为什么值得有这块面板</b>：真机上"游戏卡死"（画面定格、触摸没反应）那次，
/// 查了半小时才靠日志时间戳反推出"程序在固定时刻被超时杀掉了"——
/// 而这些数（PC 卡在哪、还动不动、超时还剩几秒）**本来就摆在手边**，只是没地方看。
/// 面板把"程序是死了还是在等"从推断变成读数。
/// </para>
/// </summary>
public sealed class VmlStatusSnapshot
{
    /// <summary>有没有 VM（没在跑任何程序时面板显示"空闲"）。</summary>
    public bool HasVm { get; set; }

    /// <summary>这次运行是否还在进行（VM 实例还活着）。</summary>
    public bool Running { get; set; }

    /// <summary>运行模式。手机端恒为 `mcu`（安全边界，见 `MauiVml` 的注释）。</summary>
    public string Mode { get; set; } = "mcu";

    /// <summary>特权级：0 = OS / 1 = MCU。</summary>
    public int PrivilegeLevel { get; set; }

    public int Pc { get; set; }

    /// <summary>R0–R15。R12 = 帧指针、R13 = 栈指针、R14 = 链接寄存器、R15 = 返回地址。</summary>
    public int[] Registers { get; set; } = [];

    public float[] FloatRegisters { get; set; } = [];
    public double[] DoubleRegisters { get; set; } = [];
    public long[] LongRegisters { get; set; } = [];

    public bool Zf { get; set; }
    public bool Cf { get; set; }
    public bool Sf { get; set; }

    public bool Fzf { get; set; }
    public bool Fsf { get; set; }
    public bool Fcf { get; set; }
    public bool Fof { get; set; }
    public bool Fuf { get; set; }
    public bool Fdf { get; set; }
    public bool Fif { get; set; }

    public long InstructionsExecuted { get; set; }
    public long SyscallsExecuted { get; set; }
    public int ExitCode { get; set; }

    /// <summary>配置的内存大小（字节）。</summary>
    public int MemoryBytes { get; set; }

    /// <summary>配置的栈大小（字节）。</summary>
    public int StackBytes { get; set; }

    /// <summary>
    /// **内存已用**（字节）：堆高水位（数据段 + 已分配的动态内存）。
    ///
    /// 与 <see cref="MemoryBytes"/>（总量）配对显示成百分比 —— "还剩多少"比"总量多大"有用得多：
    /// 程序撞上总量的那一刻，它已经在报内存错误了。
    /// </summary>
    public int MemoryUsedBytes { get; set; }

    /// <summary>**栈已压**（字节）= 初始栈顶 − 当前 sp。</summary>
    public int StackUsedBytes { get; set; }

    /// <summary>本次运行的超时上限（秒）；0 = 不限时。</summary>
    public int TimeoutSeconds { get; set; }

    /// <summary>
    /// 还剩多少秒被超时杀掉；0 = 不限时或这次运行已结束。
    ///
    /// ⚠ **这是"连续执行"的剩余**，不是墙钟：宿主在等消息/等弹框/等用户输入时会续期
    /// （见 <c>VmRuntime.ResetTimeout</c>）。所以它停在 120 不动 = 程序一直在等待，正常。
    /// </summary>
    public int TimeoutRemainingSeconds { get; set; }

    // ── 宿主/绘图窗口侧（没开窗的程序留默认值）──

    public bool HasScene { get; set; }
    public string? WindowTitle { get; set; }
    public int SceneWidth { get; set; }
    public int SceneHeight { get; set; }

    /// <summary>场景里的图元数（撞 `MaxFigures` 上限时不再涨 —— 那本身是个有用的信号）。</summary>
    public int FigureCount { get; set; }

    /// <summary>`ui_present()` 被调用的次数（= 程序认为自己画了多少帧）。</summary>
    public int FrameNumber { get; set; }

    /// <summary>宿主实际出帧的速率（过去一小段窗口内的平均）。</summary>
    public double Fps { get; set; }

    /// <summary>
    /// 格式化成**等宽对齐**的若干行（= 大窗：全部读数，含四组寄存器）。
    ///
    /// 拆成纯函数是因为它是最容易写错、又最容易测的那部分（十六进制补零、标志位、单位换算），
    /// 而 MAUI 工程不进桌面自测 —— 逻辑留在这里才测得到。
    /// </summary>
    public string[] FormatLines() => FormatLines(detailed: true);

    /// <summary>
    /// 两种窗口形态的格式化。
    ///
    /// <para>
    /// <b>大窗</b>（<paramref name="detailed"/> = true）：现在这一整份 —— 总览、超时、四个特殊寄存器、
    /// R0–R15、F/D/L 三组、标志位、执行量、内存/栈、场景。排查问题要的就是这些。
    /// </para>
    /// <para>
    /// <b>小窗</b>：**只留"一眼看个大概"的那几行**（用户要的：不用显示所有寄存器）。
    /// 判别依据是"这一行能不能回答『程序还在动吗、卡在哪、花了多少』"，够不上的都砍：
    /// R0–R15 / F / D / L 四组寄存器整块去掉（那是大窗的活），绝对值括号、调用次数、
    /// 浮点标志一并去掉。剩 5 行 ≈ 大窗的四成高 —— 浮层盖在游戏画面上时，这个差别就是
    /// "能不能一边玩一边看"。
    /// </para>
    /// <para>
    /// ⚠ **PC / SP 留在小窗里**：它们虽然也是"寄存器"，但那是"卡在哪"的唯一读数
    /// （真机那次排查全靠它），去掉等于把小窗变成好看但没用的东西。
    /// </para>
    /// </summary>
    public string[] FormatLines(bool detailed)
    {
        var lines = new List<string>();

        if (!HasVm)
        {
            lines.Add(detailed ? "VM 状态 · 空闲（没有正在运行的程序）" : "VM 状态 · 空闲");
            return [.. lines];
        }

        if (!detailed) return CompactLines();

        // ① 总览：模式 / 特权级 / 跑没跑
        lines.Add($"VM · {Mode} · 特权 {PrivilegeLevel} · {(Running ? "运行中" : "已结束")}");

        // ② 超时（这项是排查"卡死"的第一读数）
        lines.Add(TimeoutSeconds <= 0
            ? "超时 不限"
            : $"超时 {TimeoutSeconds}s · 剩 {TimeoutRemainingSeconds}s");

        // ③ 四个特殊寄存器（R12=FP / R13=SP / R14=LR / R15=RA，见 VmRuntime 的约定）
        lines.Add($"PC {Hex(Pc)}  SP {Hex(Reg(13))}  FP {Hex(Reg(12))}  LR {Hex(Reg(14))}");

        // ④ 通用寄存器，一行四个
        for (int row = 0; row < 4; row++)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 4; i++)
            {
                int n = row * 4 + i;
                if (i > 0) sb.Append("  ");
                // ⚠ 名字宽度按 **3** 补（`R0` 补成 `R0 `，`R12` 正好三个字符）——
                //   按 2 补的话 R0–R9 是 10 字符、R10–R15 是 11 字符，**同一行里后面几列会整体串位**。
                sb.Append($"R{n,-3} {Hex(Reg(n))}");
            }
            lines.Add(sb.ToString());
        }

        // ⑤ 其余寄存器组（各取前几个 —— 面板不该长到把画面全遮住）
        if (FloatRegisters.Length > 0)
            lines.Add("F  " + Join(FloatRegisters, 4, v => v.ToString("0.###", CultureInfo.InvariantCulture)));
        if (DoubleRegisters.Length > 0)
            lines.Add("D  " + Join(DoubleRegisters, 2, v => v.ToString("0.###", CultureInfo.InvariantCulture)));
        if (LongRegisters.Length > 0)
            lines.Add("L  " + Join(LongRegisters, 2, v => v.ToString(CultureInfo.InvariantCulture)));

        // ⑥ 标志位
        lines.Add($"标志 {Flag("Z", Zf)} {Flag("C", Cf)} {Flag("S", Sf)}"
                  + $" ｜ 浮点 {Flag("Z", Fzf)} {Flag("S", Fsf)} {Flag("C", Fcf)} "
                  + $"{Flag("O", Fof)} {Flag("U", Fuf)} {Flag("D", Fdf)} {Flag("I", Fif)}");

        // ⑦ 执行量与内存
        lines.Add($"指令 {InstructionsExecuted} · 调用 {SyscallsExecuted} · 退出码 {ExitCode}");

        // 占用**百分比在前**（用户要的就是这个数），绝对值跟在括号里当佐证。
        // ⚠ 分母为 0 时 `Percent` 给 `-` 而**不是 100%** —— 100% 会被读成"用满了"。
        lines.Add($"内存 {Percent(MemoryUsedBytes, MemoryBytes)}（{Size1(MemoryUsedBytes)}/{Size(MemoryBytes)}）"
                  + $" · 栈 {Percent(StackUsedBytes, StackBytes)}（{Size1(StackUsedBytes)}/{Size(StackBytes)}）");

        // ⑧ 绘图窗口侧
        if (HasScene)
        {
            lines.Add($"「{WindowTitle}」{SceneWidth}x{SceneHeight} · 图元 {FigureCount}"
                      + $" · 帧 {FrameNumber} · {Fps:0.0}fps");
        }

        return [.. lines];
    }

    /// <summary>
    /// 小窗的正文（见 <see cref="FormatLines(bool)"/> 的说明）。
    ///
    /// 行数的账：闲置 1 行；跑着 3 行（总览 / PC·SP·LR / 标志·指令·内存）；开了窗再 +1 行。
    /// **每一行都得自己站得住** —— 小窗不像大窗那样"多一行只是多一行"，
    /// 多出来的每一行都在抢游戏画面。
    /// </summary>
    private string[] CompactLines()
    {
        var lines = new List<string>();

        // ① 总览：跑没跑、什么模式、超时还剩多少。三项挤一行 —— 小窗里它们是一件事。
        var head = $"VM · {Mode} · 特权 {PrivilegeLevel} · {(Running ? "运行中" : "已结束")}";
        if (TimeoutSeconds > 0) head += $" · 超时剩 {TimeoutRemainingSeconds}s";
        lines.Add(head);

        // ② PC / SP / LR —— **"卡在哪"的那三个数**，小窗里唯一不能省的一组
        lines.Add($"PC {Hex(Pc)} · SP {Hex(Reg(13))} · LR {Hex(Reg(14))}");

        // ③ 执行量 / 占用：标志位只留整数那三个（浮点标志属于"翻寄存器"的活，归大窗）
        lines.Add($"标志 {Flag("Z", Zf)}{Flag("C", Cf)}{Flag("S", Sf)}"
                  + $" · 指令 {InstructionsExecuted} · 退出码 {ExitCode}"
                  + $" · 内存 {Percent(MemoryUsedBytes, MemoryBytes)} · 栈 {Percent(StackUsedBytes, StackBytes)}");

        // ④ 绘图窗口侧：只留"还出不出帧"（标题在窗口上本来就有，不重复占地方）
        if (HasScene)
            lines.Add($"{SceneWidth}x{SceneHeight} · 图元 {FigureCount} · 帧 {FrameNumber} · {Fps:0.0}fps");

        return [.. lines];
    }

    private int Reg(int i) => i >= 0 && i < Registers.Length ? Registers[i] : 0;

    /// <summary>八位十六进制（**大写、补零**）—— 与运行时自己 dump 寄存器的口径一致。</summary>
    private static string Hex(int v) => v.ToString("X8", CultureInfo.InvariantCulture);

    /// <summary>标志位写成 `Z+` / `Z-` —— 比 `Z=True` 短一半，一行放得下七个。</summary>
    private static string Flag(string name, bool v) => name + (v ? "+" : "-");

    private static string Join<T>(T[] values, int count, Func<T, string> fmt)
    {
        var sb = new StringBuilder();
        int n = Math.Min(count, values.Length);
        for (int i = 0; i < n; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(fmt(values[i]));
        }
        return sb.ToString();
    }

    /// <summary>字节数写成 `16M` / `512K` / `N`（面板上比一长串数字好读）。</summary>
    private static string Size(int bytes)
    {
        if (bytes <= 0) return "-";
        if (bytes >= 1024 * 1024) return (bytes / (1024 * 1024)) + "M";
        if (bytes >= 1024) return (bytes / 1024) + "K";
        return bytes.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 带一位小数的尺寸（`3.2M` / `12K`）—— **只有"已用量"用得上**：
    /// 总量是整齐的档位（16M/1M），而用了多少常常是"零点几兆"，取整会让它看着像 3M 或 4M。
    /// </summary>
    private static string Size1(int bytes)
    {
        if (bytes <= 0) return "0";
        if (bytes >= 1024 * 1024)
            return (bytes / (1024.0 * 1024.0)).ToString("0.#", CultureInfo.InvariantCulture) + "M";
        if (bytes >= 1024)
            return (bytes / 1024.0).ToString("0.#", CultureInfo.InvariantCulture) + "K";
        return bytes.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 占用百分比（整数）。分母 ≤ 0 时给 `-`：**绝不能给 100%** ——
    /// 那会被读成"已经用满了"，而实际是"这个数我们不知道"。
    /// </summary>
    private static string Percent(int used, int total)
        => total > 0 ? (used * 100 / total).ToString(CultureInfo.InvariantCulture) + "%" : "-";
}
