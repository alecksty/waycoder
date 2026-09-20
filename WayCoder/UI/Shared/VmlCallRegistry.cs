namespace WayCoder.UI.Shared;

/// <summary>
/// 四个「通用宿主调用口」的种类 —— **跨语言契约**（号段表在 <see cref="VmlUi"/>：
/// `CallWithInt8` … `CallWithDouble4`）。
///
/// C 侧**没有**"种类"这个概念：它是**绑在 syscall 号上的**（`callwithint8` 发的就是 #577），
/// 所以 `waycoder_ui.h` 里只有调用号（`VML_CALL_*`）与四个包装函数，没有 kind 宏。
///
/// 种类决定了"参数躺在哪一组寄存器里"，而这件事**只有调用口自己知道**：
/// 同一个第 0 号寄存器里放的是 `1` 还是 `1.0f` 的位模式，宿主从寄存器内容上看不出来。
/// 所以种类写在**号**上，而不是当成一个参数传 —— 见 <see cref="VmlUi.CallWithInt8"/> 那段注释。
///
/// ⚠ **只能末尾追加**：数值是编译进 C 程序里的（`asm("SYSCALL #577, …")` 与包装函数），
///    改值等于改 ABI。
/// </summary>
public enum VmlCallCast
{
    /// <summary>`CALLWITHINT8`（#577）：8 个 int 走 R0–R7，返回覆盖 R0。</summary>
    Int8 = 0,
    /// <summary>`CALLWITHFLOAT8`（#578）：8 个 float 走 F0–F7，返回覆盖 F0。</summary>
    Float8 = 1,
    /// <summary>`CALLWITHLONG4`（#579）：4 个 long 走 L0–L3，返回覆盖 L0。</summary>
    Long4 = 2,
    /// <summary>`CALLWITHDOUBLE4`（#580）：4 个 double 走 D0–D3，返回覆盖 D0。</summary>
    Double4 = 3,
}

/// <summary>
/// 一次调用的入参视图 —— 宿主把寄存器现场包成它，实现体按序号取参数。
///
/// **序号从 0 起、不含调用号**：`a.Int(0)` 是程序传的 `v[1]`（`v[0]` 是调用号，已经被吃掉了）。
/// 这样实现体的写法与"这个函数有几个参数"完全对齐，不必每处都记得 -1。
///
/// 刻意**不把参数拷进新数组**：它就是那几个寄存器数组上的一层薄视图（零分配）。
/// 拷贝一份看着更"干净"，但那就等于每次调用都做一次"寄存器 → 参数区"的搬运 ——
/// 而这四个口存在的全部意义就是**不搬运**。
///
/// 取值一律带边界检查（越界返回 0）：参数是程序给的，数组长度是宿主给的，
/// 两者对不上时**最坏是取到 0，绝不是索引越界崩宿主**（与 <see cref="VmlScene"/> 那套
/// "异常参数最多画不出来"同一条规矩）。
/// </summary>
public readonly struct VmlCallArgs
{
    private readonly int[]? _regs;
    private readonly float[]? _floats;
    private readonly double[]? _doubles;
    private readonly long[]? _longs;

    internal VmlCallArgs(VmlCallCast cast, int id,
        int[]? regs, float[]? floats, double[]? doubles, long[]? longs)
    {
        Cast = cast;
        Id = id;
        _regs = regs;
        _floats = floats;
        _doubles = doubles;
        _longs = longs;
    }

    /// <summary>调用号（程序传进来的 `v[0]`）。</summary>
    public int Id { get; }

    /// <summary>本次调用的种类。</summary>
    public VmlCallCast Cast { get; }

    /// <summary>参数个数：`Int8`/`Float8` 是 7，`Long4`/`Double4` 是 3。</summary>
    public int ArgCount => Cast is VmlCallCast.Int8 or VmlCallCast.Float8 ? 7 : 3;

    /// <summary>
    /// 第 <paramref name="i"/> 个参数（**整数域**）—— 给 `Int8`（int）/ `Long4`（long）用。
    /// 浮点种类上调它得到 `float`/`double` 的**位模式**，那不是你要的：`Float8`/`Double4`
    /// 请用 <see cref="Float"/>。越界或种类不符时返回 0。
    /// </summary>
    public long Int(int i)
    {
        if (i < 0 || i >= ArgCount) return 0;
        return Cast switch
        {
            // R0 是调用号，参数从 R1 起 —— 这是 ABI，别改成 R0。
            VmlCallCast.Int8 => At(_regs, i + 1),
            VmlCallCast.Long4 => At(_longs, i + 1),
            _ => 0,
        };
    }

    /// <summary>第 <paramref name="i"/> 个参数（**浮点域**）—— 给 `Float8` / `Double4` 用。越界返回 0。</summary>
    public double Float(int i)
    {
        if (i < 0 || i >= ArgCount) return 0;
        return Cast switch
        {
            VmlCallCast.Float8 => At(_floats, i + 1),
            VmlCallCast.Double4 => At(_doubles, i + 1),
            _ => 0,
        };
    }

    private static long At(int[]? a, int i) => a != null && (uint)i < (uint)a.Length ? a[i] : 0;
    private static long At(long[]? a, int i) => a != null && (uint)i < (uint)a.Length ? a[i] : 0;
    private static double At(float[]? a, int i) => a != null && (uint)i < (uint)a.Length ? a[i] : 0;
    private static double At(double[]? a, int i) => a != null && (uint)i < (uint)a.Length ? a[i] : 0;
}

/// <summary>
/// 一次调用的返回值 —— 覆盖第 0 号寄存器（`R0` / `F0` / `L0` / `D0`）。
///
/// 失败也**走这条路**（不是抛异常、也不是什么都不写）：写回一个**可读的失败码**
/// （见 <see cref="VmlCallRegistry.ErrorNotFound"/> 那一族，取值沿用运行时的
/// `ErrorCodes`），程序那边能自己判断"这个调用没成功"。
/// </summary>
public readonly struct VmlCallResult
{
    internal VmlCallResult(bool ok, long value, double floatValue, string? error)
    {
        Ok = ok;
        Value = value;
        FloatValue = floatValue;
        Error = error;
    }

    /// <summary>是否成功。</summary>
    public bool Ok { get; }
    /// <summary>整数域的结果（`Int8` / `Long4`）；失败时是失败码。</summary>
    public long Value { get; }
    /// <summary>浮点域的结果（`Float8` / `Double4`）；失败时是失败码。</summary>
    public double FloatValue { get; }
    /// <summary>失败原因（可读，宿主日志用）；成功时为 null。</summary>
    public string? Error { get; }
}

/// <summary>
/// **调用号**（id）—— 程序在 `v[0]` 里传的那个数，宿主按它查注册表。
///
/// ## 为什么 id 要有名字
///
/// 它就写在程序的 `v[0] = …` 那一行里，**写错一个数字的后果是"调到别的功能上"**
/// （或者根本没注册），而那既不报错也编译不过关 —— 与 syscall 号撞号是同一类事故。
/// 所以：C 侧有 `VML_CALL_*` 宏、这里有同名常量，**两边同值**（`waycoder_ui.h` 是另一半）。
///
/// ## 本批注册的是"自检族"
///
/// 机制先落地，能力随后按需注册。这一族既是**模板**（照抄它加一个新调用口只要几行），
/// 也是**判据**：它的返回值是"每个参数按位权拼出来的可分辨数"，
/// 于是"某个参数没到 / 参数串位 / 返回值没回到程序"三件事一眼就能看出来。
///
/// ⚠ 与 <see cref="VmlUi.AllNumbers"/> 一样：**只能末尾追加**，不能改值。
/// </summary>
public static class VmlCallIds
{
    /// <summary>`Int8`：7 个 int 的**位权拼接**（a₁ + a₂×10 + a₃×100 + … + a₇×10⁶）。
    /// 传 `1,2,3,4,5,6,7` 得 `7654321` —— 哪一位不对，一眼就看得出是哪个参数。</summary>
    public const int EchoInt = 1;
    /// <summary>`Float8`：同上，浮点域。</summary>
    public const int EchoFloat = 2;
    /// <summary>`Long4`：3 个 long 的位权拼接（a₁ + a₂×1000 + a₃×10⁶）。</summary>
    public const int EchoLong = 3;
    /// <summary>`Double4`：同上，浮点域。</summary>
    public const int EchoDouble = 4;
    /// <summary>`Int8`：宿主种类 —— <see cref="VmlCallRegistry.HostDesktop"/> /
    /// <see cref="VmlCallRegistry.HostMobile"/>。程序据此分支"我是在手机上跑还是在桌面上跑"。</summary>
    public const int HostInfo = 5;
}

/// <summary>
/// 「通用宿主调用口」的**注册表** —— 与 <see cref="VmlJsonApi"/> 同构（按名字/id 注册、
/// 覆盖语义、实现抛异常由这里接住），只是**按 id 而非函数名**分派，且**带类型**。
///
/// ## 分工（三处，缺一不可）
///
/// · **本文件**（`UI/Shared/`，主工程与 MAUI 都编译）：id → 实现的注册表、
///   寄存器 ↔ 参数的读写、异常兜底、失败码。**两端共用这一份** ——
///   分成两份就是本仓库的头号坑（"同一规则两处实现"），而且这类分叉的症状是
///   "手机上对、桌面上错"，最难发现；
/// · **宿主**（`WayCoder.Maui/Services/VmlUiCalls.cs` / `scripts/vmlcli/Program.cs`）：
///   把 VM 的寄存器组喂进来（<see cref="TryHandle"/>），并注册自己那端的实现；
/// · **C 包装**（`Lib/shared/src/vmlui.c` + `Lib/c/waycoder_ui.h`）：把数组装进寄存器、
///   发 syscall。
///
/// ## 加一个"宿主函数"要做什么
///
/// <code>
/// VmlCallRegistry.RegisterInt8(VmlCallIds.MyThing, a =&gt; a.Int(0) * 2);
/// </code>
///
/// 一个能力 = **一行注册**（不用占号、不用加 C 包装、不用重生成 22 门语言的绑定）。
/// 这是它与 `CALLJSON`(#573) 一致的收益；不同的是这条路**一次调用零编解码**。
/// </summary>
public static class VmlCallRegistry
{
    // ── 失败码 ────────────────────────────────────────────────────────────────
    //
    // 取值与 `VMLRuntime/ErrorCodes.cs` **同值**（那里是运行时那份的权威）：
    // 宿主查注册表失败时把它写回第 0 号寄存器，程序据此判断"这个调用没成功"。
    //
    // ⚠ 本文件**不能引用 `VMLRuntime`**：主工程（`WayCoder.csproj`）不引用运行时
    //   （只有 MAUI / vmlcli 引），而自测跑在主工程里 —— 引用它就没法自测了。
    //   所以这里写的是**常量副本**，判据由自测钉住（值变了两边一起红）。

    /// <summary>失败（实现自己报的通用失败也用这个）。= `ErrorCodes.FAILURE`。</summary>
    public const int ErrorFailure = -1;
    /// <summary>参数非法（调用号是负数、种类与注册的不符、参数个数不对）。= `ErrorCodes.INVALID_PARAMETER`。</summary>
    public const int ErrorInvalidParameter = -2;
    /// <summary>这个调用号没人注册。= `ErrorCodes.RESOURCE_NOT_FOUND`。</summary>
    public const int ErrorNotFound = -6;
    /// <summary>实现体自己抛了异常（已被接住、翻成这个码）。= `ErrorCodes.INTERNAL_ERROR`。</summary>
    public const int ErrorInternal = -9;

    // ── 宿主种类（`VML_CALL_HOST_*`）──

    /// <summary>桌面端（`scripts/vmlcli` 脚手架）。</summary>
    public const int HostDesktop = 1;
    /// <summary>手机端（MAUI App）。</summary>
    public const int HostMobile = 2;

    /// <summary>
    /// 可读日志的出口。**必须由宿主接上**（手机 → <c>ErrorLog</c>、桌面 → stderr）——
    /// 本文件不引用任何一个宿主的日志设施。默认什么都不做（自测里就不刷屏了）。
    /// </summary>
    public static Action<string>? Log { get; set; }

    private static readonly Dictionary<int, Entry> Handlers = new();

    private sealed class Entry
    {
        public VmlCallCast Cast;
        public Delegate Handler = null!;
    }

    /// <summary>已注册的调用号（诊断/自测用）。</summary>
    public static IEnumerable<int> Ids => Handlers.Keys;

    /// <summary>已注册条数（自测用）。</summary>
    public static int Count => Handlers.Count;

    /// <summary>注册一个 `Int8` 调用口：7 个 int 参数进、1 个 int 出。</summary>
    public static void RegisterInt8(int id, Func<VmlCallArgs, long> fn)
        => Put(id, VmlCallCast.Int8, fn);

    /// <summary>注册一个 `Float8` 调用口：7 个 float 参数进、1 个 float 出。</summary>
    public static void RegisterFloat8(int id, Func<VmlCallArgs, double> fn)
        => Put(id, VmlCallCast.Float8, fn);

    /// <summary>注册一个 `Long4` 调用口：3 个 long 参数进、1 个 long 出。</summary>
    public static void RegisterLong4(int id, Func<VmlCallArgs, long> fn)
        => Put(id, VmlCallCast.Long4, fn);

    /// <summary>注册一个 `Double4` 调用口：3 个 double 参数进、1 个 double 出。</summary>
    public static void RegisterDouble4(int id, Func<VmlCallArgs, double> fn)
        => Put(id, VmlCallCast.Double4, fn);

    /// <summary>
    /// 注册（或**覆盖**）一个调用号。`null` 实现、负数 id 一律**静默忽略** ——
    /// 注册点在宿主启动路径上，那里抛异常等于"App 起不来"，而少一个函数只是"这个功能不可用"。
    /// （与 <see cref="VmlJsonApi.Register"/> 的 null 防御同一条规矩。）
    /// </summary>
    private static void Put(int id, VmlCallCast cast, Delegate? fn)
    {
        if (fn is null || id < 0) return;
        Handlers[id] = new Entry { Cast = cast, Handler = fn };
    }

    /// <summary>清空（自测用；生产不走）。</summary>
    internal static void ClearForTest()
    {
        Handlers.Clear();
        Log = null;
    }

    /// <summary>
    /// **宿主唯一入口**：认领这四个号，处理掉，返回 true。
    /// 不是这四个号 → 返回 false（宿主必须原样交回运行时，否则会吞掉别的 syscall）。
    ///
    /// 四个寄存器组都传进来（宿主从 `VmRuntime` 上取现成的数组 —— 那是**活引用**，
    /// 写进去就是写进 VM）：
    /// <paramref name="registers"/> 通用整数（`R0`–`R31`）、
    /// <paramref name="floats"/> `F0`–`F15`、<paramref name="doubles"/> `D0`–`D7`、
    /// <paramref name="longs"/> `L0`–`L7`。
    ///
    /// ⚠ **绝不抛**：越界 id、未注册 id、种类不符、实现体抛异常，全部翻成写回值 +
    /// 一条可读日志。宿主 syscall 处理器里抛出去会把 VM 打挂，而程序那边只看到
    /// "窗口没了"（`VmlJsonApi.Invoke` 当初就是这么定的）。
    /// </summary>
    public static bool TryHandle(int syscallNumber, int[] registers,
        float[] floats, double[] doubles, long[] longs)
    {
        if (!VmlUi.TryCallCast(syscallNumber, out var cast)) return false;

        // ⚠ **整个处理过程包在 try 里**：读寄存器、查表、写回都碰数组，
        //   任何一处意外都不该升级成"VM 崩了"。
        try
        {
            // 调用号在四个口里**都是 `R0` 的整数视图**：int8 本来就是 R0；
            // float8/long4/double4 的包装函数另外把 `(int)v[0]` 装进 R0 ——
            // 于是"怎么读 id"在宿主侧只有一种写法（C 包装里那一段是 ABI 的一半）。
            var id = registers.Length > 0 ? registers[0] : -1;
            var args = new VmlCallArgs(cast, id, registers, floats, doubles, longs);
            var result = Invoke(cast, id, args);
            WriteResult(cast, result, registers, floats, doubles, longs);

            if (!result.Ok)
                Log?.Invoke($"[VMLCALL] #{syscallNumber} id={id} 调用失败：{result.Error}");
            return true;
        }
        catch (Exception ex)
        {
            // 走到这里说明是**注册表自己的**问题（不是实现体的 —— 那层在 Invoke 里接住了）
            Log?.Invoke($"[VMLCALL] #{syscallNumber} 宿主内部错误：{ex.Message}");
            WriteResult(cast, Failure(ErrorInternal, ex.Message), registers, floats, doubles, longs);
            return true;
        }
    }

    /// <summary>查表并调用。失败一律翻成失败码，不抛。</summary>
    private static VmlCallResult Invoke(VmlCallCast cast, int id, VmlCallArgs args)
    {
        if (id < 0)
            return Failure(ErrorInvalidParameter, $"调用号是负数：{id}");
        if (!Handlers.TryGetValue(id, out var entry))
            return Failure(ErrorNotFound, $"没有注册这个调用号：{id}（{cast}）");
        if (entry.Cast != cast)
            return Failure(ErrorInvalidParameter,
                $"调用号 {id} 注册的是 {entry.Cast}，被 {cast} 调用了");

        try
        {
            // ⚠ **实现体只调一次**：`Value` 与 `FloatValue` 是同一个结果的两个视图，
            //   写成 `f(args), f(args)` 会让实现体跑两遍 —— 对有副作用的实现体（写文件、
            //   发请求）那是"每调一次做两件事"，而且不报错。
            switch (entry.Handler)
            {
                case Func<VmlCallArgs, long> f:
                {
                    var v = f(args);
                    return new VmlCallResult(true, v, v, null);
                }
                case Func<VmlCallArgs, double> f:
                {
                    var d = f(args);
                    return new VmlCallResult(true, (long)d, d, null);
                }
                default:
                    return Failure(ErrorInternal, "注册表里的实现体类型不对");
            }
        }
        catch (Exception ex)
        {
            // **实现体抛异常不许把 VM 打挂**：接住、记一条带原因的日志、回一个可读码。
            return Failure(ErrorInternal, ex.Message);
        }
    }

    private static VmlCallResult Failure(int code, string reason)
        => new(false, code, code, reason);

    /// <summary>
    /// 把结果写回**第 0 号寄存器**（`R0` / `F0` / `L0` / `D0`）。
    ///
    /// 整数组之外**还要写它的"低位镜像"**，这不是冗余：C 包装的返回路径是
    /// `move [R12-4], R0`（32 位），调用方随后按**自己那个类型的读法**取值 ——
    /// 浮点/长整数/双精度都按 `register 0` 的**类型视图**读（`floatRegisters[0]` /
    /// `longRegisters[0]` / `doubleRegisters[0]`，见 `VMLRuntime.Float.cs` 的
    /// `GetFloatValue/GetLongValue/GetDoubleValue`）。所以两组都要写，
    /// 写法与运行时自己的 `SetFloatValue/SetLongValue/SetDoubleValue` **逐字同源**
    /// （那里对 `regNum < 8` 就是这么镜像的）。少写一组 = "桌面看着对、手机上取到 0"。
    /// </summary>
    private static void WriteResult(VmlCallCast cast, VmlCallResult r,
        int[] registers, float[] floats, double[] doubles, long[] longs)
    {
        switch (cast)
        {
            case VmlCallCast.Int8:
                Set(registers, 0, (int)r.Value);
                break;
            case VmlCallCast.Float8:
            {
                var f = (float)r.FloatValue;
                if (floats.Length > 0) floats[0] = f;
                Set(registers, 0, BitConverter.SingleToInt32Bits(f));
                break;
            }
            case VmlCallCast.Long4:
                if (longs.Length > 0) longs[0] = r.Value;
                Set(registers, 0, (int)(r.Value & 0xFFFFFFFF));
                break;
            case VmlCallCast.Double4:
            {
                if (doubles.Length > 0) doubles[0] = r.FloatValue;
                Set(registers, 0, (int)(BitConverter.DoubleToInt64Bits(r.FloatValue) & 0xFFFFFFFF));
                break;
            }
        }
    }

    private static void Set(int[] a, int i, int v)
    {
        if ((uint)i < (uint)a.Length) a[i] = v;
    }

    /// <summary>
    /// 注册**本批那一族自检调用口**（两端注册同一批 id → 同一套语义）。
    ///
    /// 宿主种类是唯一的差别（<see cref="VmlCallIds.HostInfo"/> 报的那个数）——
    /// 这是**有意为之，不是漏了**：桌面 CLI 是手机端的等价物，但它不是手机，
    /// 与其抄一个会漂的"手机标识"进来，不如如实说"我是桌面脚手架"
    /// （与 `CALLJSON` 的 sysinfo 里 `"version":"(desktop-cli)"` 同一个道理）。
    ///
    /// 实现在**这里**（一份）而不是各宿主一份：两端行为必须逐字相同，
    /// 分开写迟早分叉，而分叉的症状是"手机上对、桌面上错"。
    /// </summary>
    public static void RegisterDefaults(int hostKind)
    {
        // 位权拼接：第 i 个参数占第 i 位（十进制）。任何一位不对都看得出来是哪个参数。
        RegisterInt8(VmlCallIds.EchoInt, a => a.Int(0)
            + a.Int(1) * 10L + a.Int(2) * 100L + a.Int(3) * 1000L
            + a.Int(4) * 10000L + a.Int(5) * 100000L + a.Int(6) * 1000000L);

        RegisterFloat8(VmlCallIds.EchoFloat, a => a.Float(0)
            + a.Float(1) * 10.0 + a.Float(2) * 100.0 + a.Float(3) * 1000.0
            + a.Float(4) * 10000.0 + a.Float(5) * 100000.0 + a.Float(6) * 1000000.0);

        RegisterLong4(VmlCallIds.EchoLong, a => a.Int(0)
            + a.Int(1) * 1000L + a.Int(2) * 1000000L);

        RegisterDouble4(VmlCallIds.EchoDouble, a => a.Float(0)
            + a.Float(1) * 1000.0 + a.Float(2) * 1000000.0);

        RegisterInt8(VmlCallIds.HostInfo, _ => hostKind);
    }
}
