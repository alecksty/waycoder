using WayCoder.UI.Shared;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>
    /// 绘图接口的护栏 —— 先是**号段查重**，后面会陆续加"刷子/渐变"那批（见
    /// `docs/VML宿主接口.md` §5 的加接口步骤）。
    ///
    /// ## 为什么第一条断言是查重
    ///
    /// syscall 号是**在 `VmlUi` 里一个个 `public const int Xxx = 5xx;` 加上去的**，
    /// 而 AOT 禁反射 ⇒ 运行时枚举不出来 ⇒ 没有这张清单时，"两个特性抢同一个号"
    /// 只能靠人眼比对。它的症状不是报错，而是**一个功能静默变成另一个功能**：
    /// `HandleSyscall` 的 `switch` 遇到两个相同 `case` 时**先写的那个赢**
    /// （后写的那个是**永远不可达**的代码，编译器只给一条 CS0162 警告），
    /// 于是新加的功能"调了没反应"、或者更糟——跑的是旧那个的语义。
    ///
    /// 这与本仓反复踩的「平行表」是同一族问题，但危害更大：平行表漂了是"两边显示不一样"，
    /// 号撞了是"功能整个不见了"。所以这条断言放在新一批接口动工**之前**先落。
    /// </summary>
    /// <summary>
    /// 定位 `WayCoder/UI/Shared/VmlUiProtocol.cs`（交叉比对那条自测要读它的源码文本）。
    ///
    /// 写法照 `SelfTest.Chunk10.FindHelpDir()`：**从当前目录逐级上溯**去找一个已知路径。
    /// 不用 `AppContext.BaseDirectory` —— 那是 `bin/…`，源码不在那儿。
    /// </summary>
    private static string? FindVmlUiProtocolSrc()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (var i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            var probe = Path.Combine(dir.FullName, "WayCoder", "UI", "Shared", "VmlUiProtocol.cs");
            if (File.Exists(probe)) return probe;
        }
        return null;
    }

    /// <summary>
    /// 从文件全文里**切出 `VmlUi` 类那一段**（`public static class VmlUi` 到下一个顶层类之前）。
    ///
    /// ⚠ 切段是必须的：同一个文件里 `VmlUiLimits` 也定义了一批 5xx 常量
    /// （`LandscapeSideChromeDp = 518`、`MaxPolyPoints = 512`），而它们**不是 syscall 号**、
    /// 本就不该出现在 `AllNumbers` 里。不切段的话这条自测会把它们报成"漏登记"，成了误报。
    ///
    /// ⚠⚠ **必须先把行尾归一化成 LF**（2026-09-28 修）：下面两个判据都是拿带 `\n` 的字面量
    /// 去 `IndexOf` 的，而本仓 `core.autocrlf=true` + `.gitattributes: * text=auto`
    /// ⇒ **Windows 检出的工作区文件是 CRLF**，`"public static class VmlUi\n"` 根本匹配不到
    /// （CRLF 下 `VmlUi` 后面跟的是 `\r`），函数返回空串 ⇒ 紧接着「切得出正文」那条断言
    /// 直接红，且 `declared` 为空 ⇒ **63 个号全被报成「源码里没有定义」**。
    /// 这套自测的**真实失败信息完全指错了地方**（看着像号段登记漏了，其实是行尾）。
    /// 之所以长期没暴露：CI 跑在 ubuntu 上（LF 检出），只有 Windows 本机才会红。
    /// </summary>
    private static string VmlUiClassBody(string text)
    {
        // 归一化行尾 —— 判据只针对内容，不该被检出的行尾形态左右
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');
        const string head = "public static class VmlUi\n";
        var start = text.IndexOf(head, StringComparison.Ordinal);
        if (start < 0) return "";
        var bodyStart = start + head.Length;
        var end = text.IndexOf("\npublic static class ", bodyStart, StringComparison.Ordinal);
        return end < 0 ? text[bodyStart..] : text[bodyStart..end];
    }

    private static void TestChunk26(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("VML 宿主接口：号段查重");

        var all = VmlUi.AllNumbers;
        Check($"号清单非空（实得 {all.Length} 个）", all.Length > 0);

        // ① 同一个号出现两次 —— 后加的那个功能会被先加的抢走，且**编译期只给警告**
        var dupes = all.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Check(dupes.Count == 0
                ? "号段无重复"
                : $"号段无重复（重复：{string.Join(", ", dupes)}）",
            dupes.Count == 0);

        // ② 都在 500–599，且都被 Handles/ReservedRange 认领 —— 号段外的号会被内置 syscall 接走
        var outside = all.Where(n => !VmlUi.Handles(n)).ToList();
        Check(outside.Count == 0
                ? "所有号都落在 500–599（Handles 认领）"
                : $"所有号都落在 500–599（越界：{string.Join(", ", outside)}）",
            outside.Count == 0);

        var reserved = new HashSet<int>(VmlUi.ReservedRange());
        var notReserved = all.Where(n => !reserved.Contains(n)).ToList();
        Check(notReserved.Count == 0
                ? "所有号都在 ReservedRange 里（否则运行时会以「权限不足」拒掉）"
                : $"所有号都在 ReservedRange 里（缺：{string.Join(", ", notReserved)}）",
            notReserved.Count == 0);

        // ③ 反证：这道网**真的会响**。把清单临时改成一个必然撞号的形状，
        //    断言查重逻辑抓得到 —— 「不响的自测比没有更糟」是本仓记过的一条。
        var probe = all.Append(all[0]).ToArray();
        var probeDupes = probe.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Check("反证：清单里塞一个重复号，查重能抓到", probeDupes.Count == 1 && probeDupes[0] == all[0]);

        // ④ **清单 ↔ 源码定义 的交叉比对** —— 上面三条全在"表内自洽"，**查不出"少一个"**。
        //
        //    而这个坑已经踩过**三次**：`DrawTextEx(581)`、`SetVAlign(586)`、`GfxState(595)`
        //    （最后一个连注释都把它记了两遍，还是漏了第三次）。
        //    危害不是"少登记本身"，而是**那道查重网漏掉了这个号**：它与别的号撞了也查不出来，
        //    症状是一个功能静默变成另一个功能（`switch` 里先写的 `case` 赢）。
        //
        //    AOT 禁反射 ⇒ 运行时枚举不出常量 ⇒ 唯一的办法是**读源码文本**做比对。
        //    ⚠ 只扫 `VmlUi` 那一段（理由见 `VmlUiClassBody` 的注释）。
        var srcPath = FindVmlUiProtocolSrc();
        Check("号段: 找得到 VmlUiProtocol.cs（交叉比对要读源码）", srcPath != null);
        if (srcPath != null)
        {
            var body = VmlUiClassBody(File.ReadAllText(srcPath));
            Check("号段: 切得出 VmlUi 类正文（切段判据没被别处改动带偏）", body.Length > 1000);

            var declared = System.Text.RegularExpressions.Regex
                .Matches(body, @"public const int \w+ = (\d+);")
                .Select(m => int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture))
                .Where(n => n is >= 500 and <= 599)
                .ToHashSet();
            var listed = new HashSet<int>(all);

            // ① 源码里定义了、却没登记进清单 ⇒ 查重网漏掉它
            var missing = declared.Except(listed).OrderBy(n => n).ToList();
            Check(missing.Count == 0
                    ? $"号段: 源码里定义的 5xx 常量都已登记（{declared.Count} 个）"
                    : $"号段: 源码里定义的 5xx 常量都已登记（**漏登记**：{string.Join(", ", missing)}）",
                missing.Count == 0);

            // ② 清单里有、源码里却没有 ⇒ 这条更严重：那个号根本没有实现，纯占位
            var extra = listed.Except(declared).OrderBy(n => n).ToList();
            Check(extra.Count == 0
                    ? "号段: 清单里的每一项在源码里都有定义"
                    : $"号段: 清单里的每一项在源码里都有定义（**无定义**：{string.Join(", ", extra)}）",
                extra.Count == 0);

            // ③ 反证：这道网**真的会响**。人为"漏登记"一个号，比对必须抓到。
            //    （"不响的自测比没有更糟"—— 本仓记过的一条。）
            var probeMissing = declared.Where(n => n != declared.Min()).ToHashSet();
            Check("反证：漏登记一个号，交叉比对能抓到",
                declared.Except(probeMissing).Count() == 1);
        }

        TestUiDocSync(Section, Check);

        _ = Fail;

        TestStrokeStyleCompat(Section, Check);
        TestStrokeGradient(Section, Check);
        TestTextGradient(Section, Check);
        TestBrushModel(Section, Check);
        TestCallPorts(Section, Check);
    }

    /// <summary>
    /// **号段文档同步** —— `third_party/vml/Lib/README-ui.md` 与 `VmlUi.AllNumbers` 对表。
    ///
    /// ## 为什么这条必须有
    ///
    /// 那个文件是接口号的第 **4** 份抄本（另三份：`VmlUiProtocol.cs` 本体、
    /// `docs/VML宿主接口.md`、`help/vml/ui.md`），而它是**给程序作者看的那一份** ——
    /// C 头文件 `waycoder_ui.h` 的注释里就写着「号段表见 `Lib/README-ui.md`」。
    ///
    /// 它漂过一次，而且**三种错同时犯**：列着 3 个**已经被合并掉的号**
    /// （561 `MSG_WAIT` / 562 `MSG_COUNT` / 564 `TIMER_KILL` —— v0.96.483 把七个号
    /// 并成了 `MSG`(#560) 一个号 + 操作码）、漏了 **36 个活着的号**、示例里还在用老号。
    /// **照它写程序不会报错，只会"调了没反应"** —— 宿主 switch 落到兜底、返回 -1。
    ///
    /// ## 判据的形状（为什么只查这两个方向）
    ///
    /// · **正向**（活号 → 文档）：`AllNumbers` 里每个号都必须在文档**表格行**里出现。
    ///   漏一个就红 —— 这是"文档过期"的主症状。
    /// · **反向**（文档 → 活号）：表格行里出现的号都必须是活号。**只扫表格行、不扫正文** ——
    ///   文档里**该**提到死号（"561/562/564 已不存在"那句警告是刻意留的），
    ///   扫正文会把那句警告报成错，于是逼着后人把警告删掉，正好抹掉最有用的信息。
    ///
    /// ⚠ **它只能保证"号没漏"，保证不了"参数写得对"** —— 参数以 `VmlUiProtocol.cs`
    /// 的注释为准（唯一事实源）。这是刻意的：文档抄参数必然漂，抄"有哪些、叫什么"才有护栏价值。
    /// </summary>
    private static void TestUiDocSync(Action<string> Section, Action<string, bool> Check)
    {
        Section("VML 宿主接口：号段文档同步（Lib/README-ui.md）");

        var docPath = FindRepoFile("third_party", "vml", "Lib", "README-ui.md");
        Check("号段文档: 找得到 Lib/README-ui.md", docPath != null);
        if (docPath == null) return;

        var text = File.ReadAllText(docPath);

        // ── 表格行里出现的号（含 `| 577–580 |` 这种区间写法）──
        //
        //    ⚠ 区间必须展开：那一行只写了 577，578/579/580 一个数字都不出现，
        //      不展开的话正向检查会把它们三个报成"文档漏了"，成了误报。
        //    ⚠ `\*{0,2}` 是给 `| **560** |` 这种加粗写法留的：加粗是排版，
        //      不该让号"看不见" —— 第一版没留，于是把 `MSG`(560) 与 `TIMER`(563)
        //      报成漏了（而它们就在表里，还是最要紧的两个）。**判据要容得下排版。**
        var documented = new HashSet<int>();
        foreach (System.Text.RegularExpressions.Match m in
                 System.Text.RegularExpressions.Regex.Matches(text,
                     @"^\|\s*\*{0,2}(\d{3})\*{0,2}\s*(?:[–-]\s*\*{0,2}(\d{3})\*{0,2}\s*)?\|",
                     System.Text.RegularExpressions.RegexOptions.Multiline))
        {
            var lo = int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            if (!m.Groups[2].Success)
            {
                if (lo is >= 500 and <= 599) documented.Add(lo);
                continue;
            }
            var hi = int.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
            for (var n = lo; n <= hi && n <= 599; n++)
                if (n >= 500) documented.Add(n);
        }

        Check($"号段文档: 扫得出号（实得 {documented.Count} 个）", documented.Count > 30);

        var live = new HashSet<int>(VmlUi.AllNumbers);
        Check($"号段文档: 活号清单非空（实得 {live.Count} 个）", live.Count > 30);

        // ① 正向：活号必须有文档
        var undocumented = live.Except(documented).OrderBy(n => n).ToList();
        Check(undocumented.Count == 0
                ? $"号段文档: {live.Count} 个活号在 README-ui.md 里都有"
                : $"号段文档: **漏了 {undocumented.Count} 个号**（{string.Join(", ", undocumented)}）"
                  + " —— 照它写程序会静默失效，补进 `Lib/README-ui.md` 的表格",
            undocumented.Count == 0);

        // ② 反向：文档表格里不许有死号（正文里的历史说明不算，理由见方法注释）
        var dead = documented.Except(live).OrderBy(n => n).ToList();
        Check(dead.Count == 0
                ? "号段文档: 表格里的号都是活号"
                : $"号段文档: 表格里列着**已经不存在的号**（{string.Join(", ", dead)}）"
                  + " —— 照着调只会返回 -1、什么都不发生",
            dead.Count == 0);

        // ③ 反证：这道网**真的会响**（"不响的自测比没有更糟"）。
        //    人为从活号里去掉一个，正向比对必须抓到它。
        var probe = live.Where(n => n != live.Min()).ToHashSet();
        Check("反证：活号少一个，文档比对能抓到",
            live.Except(probe).Count() == 1 && live.Except(probe).First() == live.Min());
    }

    /// <summary>
    /// 从当前目录逐级上溯，按**路径分段**拼出一个仓库内文件的绝对路径。
    ///
    /// 按分段拼而不是写死 `"third_party/vml/Lib/README-ui.md"`：Windows 上分隔符不同，
    /// 而 `Path.Combine` 在两种平台上都对。找不到返回 null —— **由用例自己报红**，
    /// 静默跳过就是"这条测试永远不生效"。
    /// </summary>
    private static string? FindRepoFile(params string[] segments)
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (var i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            var probe = Path.Combine(new[] { dir.FullName }.Concat(segments).ToArray());
            if (File.Exists(probe)) return probe;
        }
        return null;
    }

    /// <summary>
    /// **通用宿主调用口**（577–580）—— 号段、契约值、注册表、参数读写、失败码。
    ///
    /// 这一批的判据有个特点：**宿主侧那一半（id → 实现、寄存器 ↔ 参数、返回值写回哪一组）
    /// 可以完全离线自测** —— 它只跟四个数组打交道，不需要真 VM。
    /// 另一半（C 包装把数组装进寄存器）在 `Lib/shared/src/vmlui.c` 里，
    /// 由 `scripts/vmlcli` 的端到端例程验（`Examples/c/callports.c`）。
    ///
    /// 所以这里**手工摆四个数组**当寄存器组用 —— 它们就是 VM 的 `registers` /
    /// `floatRegisters` / `doubleRegisters` / `longRegisters`，形状与语义一模一样。
    /// </summary>
    private static void TestCallPorts(Action<string> Section, Action<string, bool> Check)
    {
        Section("VML 宿主接口：通用调用口（577–580）");

        // ── ① 号段：四个号必须登记在案（漏登记 → 查重网漏掉新号）──
        var set = new HashSet<int>(VmlUi.AllNumbers);
        Check("四个调用口都在号清单里",
            set.Contains(VmlUi.CallWithInt8) && set.Contains(VmlUi.CallWithFloat8)
            && set.Contains(VmlUi.CallWithLong4) && set.Contains(VmlUi.CallWithDouble4));

        // 号值本身是**跨语言契约**（C 头文件的包装函数把号写在内联汇编里），钉死。
        Check("号值 = 577/578/579/580",
            VmlUi.CallWithInt8 == 577 && VmlUi.CallWithFloat8 == 578
            && VmlUi.CallWithLong4 == 579 && VmlUi.CallWithDouble4 == 580);

        Check("号 → 种类映射正确",
            VmlUi.TryCallCast(577, out var c1) && c1 == VmlCallCast.Int8
            && VmlUi.TryCallCast(578, out var c2) && c2 == VmlCallCast.Float8
            && VmlUi.TryCallCast(579, out var c3) && c3 == VmlCallCast.Long4
            && VmlUi.TryCallCast(580, out var c4) && c4 == VmlCallCast.Double4);

        // **反向**：不是这四个号必须返回 false —— 否则会把别的 syscall 吞掉
        //（`TryHandle` 由宿主无条件调用，返回 true 就等于"我处理了"）。
        Check("非本批号不认领（573/576/999）",
            !VmlUi.TryCallCast(573, out _) && !VmlUi.TryCallCast(576, out _) && !VmlUi.TryCallCast(999, out _));

        // ── ② 种类与调用号的数值也是契约（C 侧只有调用号宏 `VML_CALL_*` —— 种类是**绑在号上**的）──
        Check("种类值 = Int8/Float8/Long4/Double4 = 0/1/2/3",
            (int)VmlCallCast.Int8 == 0 && (int)VmlCallCast.Float8 == 1
            && (int)VmlCallCast.Long4 == 2 && (int)VmlCallCast.Double4 == 3);
        Check("自检族调用号 = 1..5",
            VmlCallIds.EchoInt == 1 && VmlCallIds.EchoFloat == 2 && VmlCallIds.EchoLong == 3
            && VmlCallIds.EchoDouble == 4 && VmlCallIds.HostInfo == 5);

        // 失败码与 `VMLRuntime/ErrorCodes` **同值**（那边是权威）。本文件不能引用运行时
        //（主工程不引 VMLRuntime，而自测跑在主工程里），所以这里钉字面量 ——
        // 值一旦被改，两端程序读到的"失败"就变了，那正是这套码存在的意义。
        Check("失败码 = -1/-2/-6/-9（与 ErrorCodes 同值）",
            VmlCallRegistry.ErrorFailure == -1 && VmlCallRegistry.ErrorInvalidParameter == -2
            && VmlCallRegistry.ErrorNotFound == -6 && VmlCallRegistry.ErrorInternal == -9);

        // ── ③ 注册表：四个种类各走一遍（合成寄存器组）──
        // 先清空再注册自己那几条：注册表是**静态**的，别的用例（或将来某段启动代码）
        // 注册过什么会影响判定 —— 不隔离的话"类型不符"那条可能因为别人占了号而假绿。
        VmlCallRegistry.ClearForTest();
        Check("清空后注册表为空", VmlCallRegistry.Count == 0);

        VmlCallRegistry.RegisterInt8(101, a => a.Int(0) + a.Int(1) * 10L + a.Int(6) * 100L);
        VmlCallRegistry.RegisterFloat8(102, a => a.Float(0) + a.Float(6) * 10.0);
        VmlCallRegistry.RegisterLong4(103, a => a.Int(0) + a.Int(2) * 1000L);
        VmlCallRegistry.RegisterDouble4(104, a => a.Float(0) + a.Float(2) * 1000.0);
        Check("四类各注册一条", VmlCallRegistry.Count == 4);

        int[] regs;
        float[] fr;
        double[] dr;
        long[] lr;

        // Int8：R0=id R1..R7=参数 → 返回值覆盖 R0
        regs = new int[32]; fr = new float[16]; dr = new double[8]; lr = new long[8];
        regs[0] = 101; regs[1] = 7; regs[2] = 8; regs[7] = 9;
        Check("int8 认领（TryHandle 返回 true）",
            VmlCallRegistry.TryHandle(577, regs, fr, dr, lr));
        Check("int8 参数按位取对、返回值覆盖 R0（7 + 8×10 + 9×100 = 987）", regs[0] == 987);

        // Float8：F0=id F1..F7=参数 → 返回值覆盖 F0（**同时**镜像进 R0 的位模式）
        regs = new int[32]; fr = new float[16]; dr = new double[8]; lr = new long[8];
        fr[0] = 102; fr[1] = 1.5f; fr[7] = 2.0f;
        regs[0] = 102;                        // C 包装同时把 id 的整数视图放进 R0
        Check("float8 认领", VmlCallRegistry.TryHandle(578, regs, fr, dr, lr));
        Check("float8 参数取对、返回值覆盖 F0（1.5 + 2×10 = 21.5）", Math.Abs(fr[0] - 21.5f) < 1e-4);
        Check("float8 返回值同时镜像进 R0（C 调用方按浮点位模式读）",
            regs[0] == BitConverter.SingleToInt32Bits(21.5f));

        // Long4：L0=id L1..L3=参数 → 返回值覆盖 L0（+ R0 低 32 位镜像）
        regs = new int[32]; fr = new float[16]; dr = new double[8]; lr = new long[8];
        lr[0] = 103; lr[1] = 5; lr[3] = 6;
        regs[0] = 103;
        Check("long4 认领", VmlCallRegistry.TryHandle(579, regs, fr, dr, lr));
        Check("long4 参数取对、返回值覆盖 L0（5 + 6×1000 = 6005）", lr[0] == 6005);
        Check("long4 返回值镜像 R0 低 32 位", regs[0] == 6005);

        // Double4：D0=id D1..D3=参数 → 返回值覆盖 D0（+ R0 位模式低半镜像）
        regs = new int[32]; fr = new float[16]; dr = new double[8]; lr = new long[8];
        dr[0] = 104; dr[1] = 0.25; dr[3] = 3.0;
        regs[0] = 104;
        Check("double4 认领", VmlCallRegistry.TryHandle(580, regs, fr, dr, lr));
        Check("double4 参数取对、返回值覆盖 D0（0.25 + 3×1000 = 3000.25）",
            Math.Abs(dr[0] - 3000.25) < 1e-9);
        Check("double4 返回值镜像 R0（低 32 位位模式）",
            regs[0] == (int)(BitConverter.DoubleToInt64Bits(3000.25) & 0xFFFFFFFF));

        // ── ④ 失败路径：**一个都不许崩**，而且要给出可读码 ──
        // 未注册的号
        regs = new int[32]; fr = new float[16]; dr = new double[8]; lr = new long[8];
        regs[0] = 9999;
        VmlCallRegistry.TryHandle(577, regs, fr, dr, lr);
        Check("未注册的调用号 → -6（RESOURCE_NOT_FOUND）", regs[0] == VmlCallRegistry.ErrorNotFound);

        // 类型不符：102 注册的是 Float8，却被 int8 口调用
        regs = new int[32]; fr = new float[16]; dr = new double[8]; lr = new long[8];
        regs[0] = 102;
        VmlCallRegistry.TryHandle(577, regs, fr, dr, lr);
        Check("种类不符 → -2（INVALID_PARAMETER）", regs[0] == VmlCallRegistry.ErrorInvalidParameter);

        // 负数调用号
        regs = new int[32]; fr = new float[16]; dr = new double[8]; lr = new long[8];
        regs[0] = -5;
        VmlCallRegistry.TryHandle(577, regs, fr, dr, lr);
        Check("负数调用号 → -2", regs[0] == VmlCallRegistry.ErrorInvalidParameter);

        // 实现体抛异常：**必须被接住**（抛出去 = VM 被打挂，程序只看到"窗口没了"）
        VmlCallRegistry.RegisterInt8(105, _ => throw new InvalidOperationException("实现体自己炸了"));
        regs = new int[32]; fr = new float[16]; dr = new double[8]; lr = new long[8];
        regs[0] = 105;
        var threw = false;
        try { VmlCallRegistry.TryHandle(577, regs, fr, dr, lr); }
        catch { threw = true; }
        Check("实现体抛异常被接住（TryHandle 不抛）", !threw);
        Check("实现体抛异常 → -9（INTERNAL_ERROR）", regs[0] == VmlCallRegistry.ErrorInternal);

        // 数组比应有的短（宿主给错了）也不许越界崩 —— 参数一律取 0，返回值仍写进存在的那个槽
        //（期望值是 0 而不是 101：`R0` 是**调用号槽**，返回值覆盖它，所以读到的是
        //  "参数全 0 时的结果" 0 —— 这正好也把"返回值覆盖 R0"再钉一遍）
        var tinyRegs = new int[1] { 101 };
        var ok = true;
        try { VmlCallRegistry.TryHandle(577, tinyRegs, new float[1], new double[1], new long[1]); }
        catch { ok = false; }
        Check("寄存器数组短得离谱也不抛", ok);
        Check("短数组下参数按 0 处理，返回值仍写回 R0（0）", tinyRegs[0] == 0);

        // ── ⑤ 收尾：把注册表恢复成"生产用"的样子，免得上面的用例污染后面 ──
        VmlCallRegistry.ClearForTest();
        VmlCallRegistry.RegisterDefaults(VmlCallRegistry.HostDesktop);
        Check("RegisterDefaults 之后自检族可用（宿主信息 = 桌面）",
            TryCall(VmlUi.CallWithInt8, VmlCallIds.HostInfo) == VmlCallRegistry.HostDesktop);
        Check("自检族 EchoInt：1..7 → 7654321", TryCall(VmlUi.CallWithInt8,
            VmlCallIds.EchoInt, 1, 2, 3, 4, 5, 6, 7) == 7654321);

        // 收尾后表里是 5 条（自检族）。
        Check("自检族共 5 条", VmlCallRegistry.Count == 5);

        // 小工具：把 7 个 int 参数按 int8 口发一次，返回 R0。
        static long TryCall(int syscallNumber, int id, params int[] args)
        {
            var r = new int[32];
            r[0] = id;
            for (var i = 0; i < args.Length && i < 7; i++) r[i + 1] = args[i];
            VmlCallRegistry.TryHandle(syscallNumber, r, new float[16], new double[8], new long[8]);
            return r[0];
        }
    }

    /// <summary>
    /// **刷子模型的参数防护** —— 这批号是"一个号 + 操作码"，程序传进来的
    /// 种类/槽位/形状码**全是裸整数**，写错一个就是未定义行为。
    ///
    /// 仓库既有的规矩是「**让异常参数最多画不出来，绝不崩**」（见 `VmlScene` 那一堆
    /// `InCoordRange` / `Dim` / `MaxPolyPoints`(见 `VmlUiLimits`)）。这一批同样按这条写，但
    /// **写的时候没有自测钉住** —— 于是补在这里。
    ///
    /// 判据刻意只断言两类，因为这两类才是"崩"的来源：
    /// ① **不抛**（越界索引、除零、空引用）；
    /// ② **越界的东西真的被挡住了**（不是"没崩就算过"—— 静默画错也是错）。
    /// </summary>
    private static void TestBrushModel(Action<string> Section, Action<string, bool> Check)
    {
        Section("绘图 DSL：刷子模型的参数防护");

        // ── 形状码 ──
        var s = new VmlScene();
        Check("未知形状码 → false（不抛）", !s.AddShape(999, 1, 2, 3, 4, 5, 6, 7));
        Check("负形状码 → false", !s.AddShape(-1, 1, 2, 3, 4, 5, 6, 7));

        // 坐标越界整条丢弃：屏幕外的东西本来也看不见，丢掉还省内存（与既有策略一致）
        Check("坐标超出 ±100 万 → 丢弃",
            !s.AddShape(VmlShape.Rect, VmlScene.CoordLimit + 1, 0, 10, 10, 0, 0, 0));
        Check("合法坐标 → 受理", s.AddShape(VmlShape.Rect, 0, 0, 10, 10, 0, 0, 0));

        // 角数钳位 —— 与 DSL 侧 StarCommand/RegularCommand 的判据同源
        Check("星形角数 < 2 被抬到 2",
            s.AddStar(10, 10, 20, 8, 0, 0) && s.BuildDsl().Contains("star 10 10 20 8 2 0"));
        Check("星形角数 > 4096 被压到 4096",
            s.AddStar(10, 10, 20, 8, 99999, 0) && s.BuildDsl().Contains("star 10 10 20 8 4096 0"));
        Check("正多边形边数 < 3 被抬到 3",
            s.AddRegular(10, 10, 20, 1, 0) && s.BuildDsl().Contains("regular 10 10 20 3 0"));
        Check("旋转角归一到 [0,360)", s.AddStar(10, 10, 20, 8, 5, -90)
            && s.BuildDsl().Contains("star 10 10 20 8 5 270"));

        // ── 样式槽 ──
        Check("未知样式槽 → false（不抛）", !s.SetStyle(99, unchecked((int)0xFF00FF00), 0, 0, 0, 0));
        Check("SetStyle 正常槽 → true", s.SetStyle(VmlStyleSlot.Fill, unchecked((int)0xFF00FF00), 0, 0, 0, 0));

        // 句柄越界一律当"没有"，绝不能拿它去索引刷子表
        Check("越界句柄 → BrushToken 为 null", s.BrushToken(999) == null);
        Check("句柄 0 → null（0 是「没有」）", s.BrushToken(0) == null);
        Check("未知槽位不产生刷子", s.BrushToken(-5) == null);

        // 渐变刷子给了"画笔"槽（本批还不支持）→ **退化到起始色并记警告**，不崩、不静默
        var s2 = new VmlScene();
        var gb = s2.AddGradientBrush(radial: false, 0xFFFF0000, 0xFF0000FF, 0, 0, 1000, 0);
        Check("渐变刷子句柄有效", gb >= 1 && s2.BrushToken(gb)!.StartsWith('@'));
        // v0.96.306 起画笔槽**收渐变**（描边渐变已落地）—— 从前这里是"退化成起始色"。
        Check("渐变刷子给了画笔槽 → 受理并**保留渐变**",
            s2.SetStyle(VmlStyleSlot.Pen, gb, 3, 0, 0, 0) && s2.PenToken!.StartsWith('@'));

        // ── 刷子表上限 ──
        var s3 = new VmlScene();
        var last = 0;
        for (var i = 0; i < VmlScene.MaxBrushes + 10; i++) last = s3.AddSolidBrush((uint)(0xFF000000 + i));
        Check("刷子表满了之后返回 0（不越界、不崩）", last == 0);
        Check($"上限就是 {VmlScene.MaxBrushes}",
            s3.BrushToken(VmlScene.MaxBrushes) != null && s3.BrushToken(VmlScene.MaxBrushes + 1) == null);

        // 同色复用：循环里反复造不该把表撑爆
        var s4 = new VmlScene();
        var h1 = s4.AddSolidBrush(0xFF123456);
        var h2 = s4.AddSolidBrush(0xFF123456);
        Check("同色刷子复用同一个句柄", h1 == h2 && h1 >= 1);

        // ── 画笔的箭头：改发 `arrow` 指令（DSL 里是另一条指令，几何在 ArrowCommand.Head）──
        var s5 = new VmlScene();
        s5.SetStyle(VmlStyleSlot.Pen, unchecked((int)0xFF00FF00), 3, 0, 0, VmlArrow.End);
        s5.AddShape(VmlShape.Line, 10, 20, 90, 60, 0, 0, 0);
        Check("画笔带箭头 → 发的是 arrow 而不是 line",
            s5.BuildDsl().Contains("arrow 10 20 90 60") && !s5.BuildDsl().Contains("line 10 20"));

        var s6 = new VmlScene();
        s6.SetStyle(VmlStyleSlot.Pen, unchecked((int)0xFF00FF00), 3, 0, 0, VmlArrow.None);
        s6.AddShape(VmlShape.Line, 10, 20, 90, 60, 0, 0, 0);
        Check("画笔不带箭头 → 仍然是 line", s6.BuildDsl().Contains("line 10 20 90 60"));
    }

    /// <summary>
    /// **描边（画笔）渐变** —— 「边框一个渐变、填充一个渐变」。
    ///
    /// 改动集中在三处，判据也就分三层：
    /// ① **解析**：第二个 `@id` 进描边槽；描边类图元（只有一个颜色位）的裸 `@id` 也是描边槽；
    ///    `path` 是老特例（裸 `@id` 一直是填充），所以那条走显式 `stroke @id`。
    /// ② **光栅**：逐段粗线四边形 + 逐像素从刷子取色。**归一化盒用原几何的盒**是本条最难发现的一处 ——
    ///    拿被线宽撑大的轮廓盒去归一化，渐变会整体偏半个线宽，而"偏一点"肉眼看不出来。
    /// ③ **SVG**：四条只描边的指令从前各自硬写 `ColorUtil.ToHex(f.Stroke)`，
    ///    给它们配渐变描边会**光栅对、SVG 黑**。这里把两个出口都钉住。
    ///
    /// 判据用的是**能分辨对错**的那一种：起点像素取「纯起始色」而不是"接近起始色"，
    /// 因为归一化盒错了正好会让它变成 7% 的插值色（R=235 而不是 251）——
    /// 断言写成"偏红"就两边都过，等于没测。
    /// </summary>
    private static void TestStrokeGradient(Action<string> Section, Action<string, bool> Check)
    {
        Section("绘图 DSL：描边（画笔）渐变");

        static DrawDocument Doc(string body) => DrawRunner.Parse("canvas 200 200 #ffffff\n" + body);
        static DrawFigure? One(string body)
        {
            var d = Doc(body);
            return d.Figures.Count == 1 ? d.Figures[0] : null;
        }

        // ── ① 解析 ──

        // 兼容红线：**一个**裸 @id 仍进填充槽（既有 10 条指令共用的位置约定）
        var a = One("rect 0 0 10 10 @gf");
        Check("兼容：单个 @id 仍进填充槽", a?.GradientRef == "gf" && a?.StrokeGradientRef == null);

        // 第二个 @id 从前只是把第一个覆盖掉（= 被静默丢弃），所以这个槽位不碰任何既有语义
        var b = One("rect 0 0 10 10 @gf @gs 3");
        Check("第二个 @id → 描边槽", b?.GradientRef == "gf" && b?.StrokeGradientRef == "gs");

        // 描边类图元只有一个颜色位（就是描边本身），裸 @id 只能是描边刷子
        foreach (var (kind, dsl) in new[]
        {
            ("line", "line 0 0 10 10 @g"),
            ("arrow", "arrow 0 0 10 10 @g"),
            ("polyline", "polyline 0 0 10 0 10 10 @g"),
        })
        {
            var f = One(dsl);
            Check($"{kind}: 裸 @id → 描边槽（不是填充槽）",
                f?.StrokeGradientRef == "g" && f?.GradientRef == null);
        }

        // path 是老特例：裸 @id 一直是**填充**（既有语义，不能动）⇒ 描边走显式关键字
        var p1 = One("path \"M 0 0 L 10 0 L 10 10 Z\" @g");
        Check("path 兼容：裸 @id 仍是填充（红线）", p1?.GradientRef == "g" && p1?.StrokeGradientRef == null);
        var p2 = One("path \"M 0 0 L 10 0 L 10 10 Z\" stroke @g");
        Check("path: 显式 stroke @id → 描边槽", p2?.StrokeGradientRef == "g" && p2?.GradientRef == null);

        // 同一份渐变可以被两个槽同时引用（解析不是"二选一"）
        var dup = Doc("gradient g linear #ff0000 #0000ff\nrect 0 0 10 10 @g @g 3").Figures[0];
        Check("填充与描边可引用同一份渐变（两边都解析出来）",
            dup.Gradient != null && dup.StrokeGradient != null
            && ReferenceEquals(dup.Gradient, dup.StrokeGradient));

        // 悬空引用退化成纯色（与填充槽的老行为一致，不崩）
        // ⚠ 必须带上一条真的渐变定义：`DrawRunner.Parse` 的解析循环是
        // `if (doc.Gradients.Count > 0)` 整体的 —— 一条都没定义时它整段跳过，
        // 那时 `GradientRef` 原样留着（这是既有行为，不是本条要测的东西）。
        var dangling = Doc("gradient g linear #ff0000 #0000ff\nrect 0 0 10 10 @nosuch @alsono 3").Figures[0];
        Check("悬空的描边渐变引用 → 退化成纯色",
            dangling.StrokeGradientRef == null && dangling.StrokeGradient == null);

        // ── ② 光栅 ──

        var doc = Doc("gradient g linear #ff0000 #0000ff\nline 10 10 50 10 @g 6");
        var img = PngDecoder.Decode(DrawRunner.ToPng(doc));

        // 线从 x=10 到 x=50、粗 6（butt 端帽 ⇒ 四边形恰好落在 [10,50]）。
        // 归一化盒 = 几何盒 (10,10)-(50,10)、跨度 40，所以 x=10 的像素中心 10.5 → t=0.0125。
        var left = img.ColorAt(10, 10);
        var right = img.ColorAt(49, 10);
        Check($"光栅渐变描边：左端≈起始色（实得 #{R(left):X2}{G(left):X2}{B(left):X2}）",
            R(left) >= 248 && B(left) <= 8);
        Check($"光栅渐变描边：右端≈终止色（实得 #{R(right):X2}{G(right):X2}{B(right):X2}）",
            B(right) >= 248 && R(right) <= 8);
        Check("光栅渐变描边：中点确在两者之间（不是被钳成一端）",
            R(img.ColorAt(30, 10)) is > 100 and < 160 && B(img.ColorAt(30, 10)) is > 100 and < 160);

        // **归一化盒**判据（这是本条最容易错的地方）：若误用"被线宽撑大的轮廓盒"（7..53、跨度 46），
        // 左端会变成 t=0.076 ⇒ R≈235。所以断言必须卡在 248 以上才分辨得开。
        Check("光栅渐变描边：起点是纯色 ⇒ 归一化盒用的是几何盒而非轮廓盒", R(left) >= 248);

        // 闭合图形也要能画（走 DrawFill.Stroke，与开放描边是两条路）
        var ring = PngDecoder.Decode(DrawRunner.ToPng(
            Doc("gradient g linear #ff0000 #0000ff\nrect 20 20 60 40 @nofill @g 8")));
        var rl = ring.ColorAt(21, 40);
        Check($"闭合形状的渐变描边（rect 左边框≈起始色，实得 R={R(rl)}）", R(rl) >= 240 && B(rl) <= 20);

        // ── ③ SVG ──

        var svg = DrawRunner.ToSvg(Doc("gradient g linear #ff0000 #0000ff\nline 10 10 50 10 @g 6"));
        Check("SVG: 线的描边引用渐变（url(#g)）", svg.Contains("stroke=\"url(#g)\""));
        Check("SVG: defs 里确实有这份渐变", svg.Contains("linearGradient id=\"g\""));
        var svgRect = DrawRunner.ToSvg(Doc("gradient g linear #ff0000 #0000ff\nrect 0 0 10 10 @nofill @g 3"));
        Check("SVG: 闭合形状的描边也走 url(#g)", svgRect.Contains("stroke=\"url(#g)\""));
        // 真正的判据是"描边渐变**不能**漏进填充槽" —— 而不是填充具体是什么色
        // （`@nofill` 是个不存在的 id，解析后退化回 rect 的默认填充）。
        Check("SVG: 描边渐变不漏进填充槽（fill 不是 url(#g)）",
            !svgRect.Contains("fill=\"url(#g)\""));

        // ── ④ 矢量后端：**轮廓化填掉**，不回退光栅 ──
        //    平台没有 SetStrokePaint，但描边就是个填充多边形 ⇒ 展成轮廓再填。
        //    （v0.96.306~308 那两版是"标记画不了 ⇒ 整窗回退"，画面正确但慢；
        //      而且回退只翻标志位时静态程序会永久停在残缺帧上，见 v0.96.308。）

        var vt = new RecordingVectorTarget();
        var fig = Doc("gradient g linear #ff0000 #0000ff\nline 10 10 50 10 @g 6").Figures[0];
        DrawVector.Stroke(vt, fig.Args, fig);
        Check("矢量: 渐变描边走轮廓化填充（不回退光栅）", vt.Unsupported.Count == 0 && vt.Fills.Count == 1);
        Check("矢量: 一次 FillShape 装下全部块（不是逐块建路径）",
            vt.Fills.Count == 1 && vt.Fills[0].Subpaths.Count >= 1 && vt.Fills[0].Gradient != null);

        // ── ⑤ 端到端：**C 层够得到的那个入口**一直钉到解析器 ──
        //    VML 程序调的是 `ui_set_pen(刷子句柄,…)` + `ui_draw_line(...)`，
        //    落到 VmlScene 就是"画笔槽 + 画线"。这条断言跨过了**发射**与**解析**两层的接缝 ——
        //    本仓最常见的故障正是"两边各自都对、中间的字符串对不上"。
        var s7 = new VmlScene();
        var gb2 = s7.AddGradientBrush(radial: false, 0xFFFF0000, 0xFF0000FF, 0, 0, 1000, 0);
        s7.SetStyle(VmlStyleSlot.Pen, gb2, 4, 0, 0, 0);
        s7.AddShape(VmlShape.Line, 10, 10, 50, 10, 0, 0, 0);
        var e2e = DrawRunner.Parse(s7.BuildDsl());
        var line = e2e.Figures.FirstOrDefault(x => x.Kind is "line" or "arrow");
        Check("端到端：画笔槽的渐变刷子 → DSL → 解析回描边渐变",
            line?.StrokeGradient != null && line.StrokeGradientRef != null);
        Check("端到端：VmlScene 发射的画笔 token 是刷子引用（@_b…）而不是退化色",
            s7.PenToken != null && s7.PenToken.StartsWith('@'));

        // 形状那一路（走 StyleTail 的位置约定：第一个 token = 填充、第二个 = 描边）
        var s8 = new VmlScene();
        var gb3 = s8.AddGradientBrush(radial: false, 0xFFFF0000, 0xFF0000FF, 0, 0, 1000, 0);
        s8.SetStyle(VmlStyleSlot.Fill, unchecked((int)0xFF00FF00), 0, 0, 0, 0);
        s8.SetStyle(VmlStyleSlot.Pen, gb3, 3, 0, 0, 0);
        s8.AddShape(VmlShape.Rect, 10, 10, 60, 40, 0, 0, 0);
        var e2e2 = DrawRunner.Parse(s8.BuildDsl());
        // 半径 0 ⇒ 发 `rect`（分流判据是**半径 > 0**，不是"高 > 0"）
        var rectFig = e2e2.Figures.FirstOrDefault(x => x.Kind == "rect");
        Check($"端到端：形状的填充纯色 + 描边渐变各就各位（实得 {rectFig?.Kind ?? "null"}）",
            rectFig?.StrokeGradient != null && rectFig.Fill == 0xFF00FF00
            && rectFig.StrokeGradientRef != null && rectFig.GradientRef == null);

        // 反过来：半径 > 0 ⇒ 发 `roundrect`。两条分支都要被走到才说明分流是对的
        // （从前判据写成"高 > 0"⇒ 恒真 ⇒ `rect` 那条**永远是死代码**）。
        var s9 = new VmlScene();
        s9.SetStyle(VmlStyleSlot.Fill, unchecked((int)0xFF00FF00), 0, 0, 0, 0);
        s9.AddShape(VmlShape.Rect, 10, 10, 60, 40, 12, 0, 0);
        Check("形状码分流：半径 > 0 → roundrect",
            DrawRunner.Parse(s9.BuildDsl()).Figures.Any(x => x.Kind == "roundrect"));

        // 绕向一致性：非零环绕靠它成立（各块绕向若不同，重叠处会被挖空成洞）。
        static double Shoelace(IReadOnlyList<double> p)
        {
            double s = 0;
            for (int i = 0; i + 1 < p.Count; i += 2)
            {
                int j = (i + 2) % p.Count;
                s += p[i] * p[j + 1] - p[j] * p[i + 1];
            }
            return s;
        }
        Check("矢量: 各块绕向一致（非零环绕才是并集，否则重叠处被挖空）",
            vt.Fills[0].Subpaths.All(sp => Shoelace(sp) > 0));

        // **刷子矩形必须是原几何的盒**，不是被线宽撑大的轮廓盒 ——
        // 这是渐变位置唯一的判据：偏半个线宽肉眼看不出来。
        // 几何 (10,10)-(50,10)、线宽 6 ⇒ 轮廓盒 y 是 7..13，几何盒是 10..10。
        var bx = vt.Fills[0].Box;
        Check($"矢量: 刷子矩形用的是原几何盒（实得 y {bx?.MinY}..{bx?.MaxY}）",
            bx != null && Math.Abs(bx.Value.MinY - 10) < 1e-6 && Math.Abs(bx.Value.MaxY - 10) < 1e-6
            && Math.Abs(bx.Value.MinX - 10) < 1e-6 && Math.Abs(bx.Value.MaxX - 50) < 1e-6);

        // round 端帽的圆也要进同一批子路径（它们与杆重叠，靠非零规则相加）
        var vt3 = new RecordingVectorTarget();
        var round = Doc("gradient g linear #ff0000 #0000ff\nline 10 10 50 10 @g 6 round").Figures[0];
        DrawVector.Stroke(vt3, round.Args, round);
        var pieces = vt3.Fills.Count > 0 ? vt3.Fills[0].Subpaths.Count : 0;
        Check($"矢量: round 端帽的圆也进了同一批子路径（实得 {pieces} 块，应 ≥ 3）", pieces >= 3);

        // 反证：纯色描边**不走这条路**（仍走 StrokePolyline，不建填充）
        var vt2 = new RecordingVectorTarget();
        var plain = Doc("line 10 10 50 10 #ff0000 6").Figures[0];
        DrawVector.Stroke(vt2, plain.Args, plain);
        Check("反证：纯色描边仍走 StrokePolyline（不误入轮廓化路径）",
            vt2.Unsupported.Count == 0 && vt2.Fills.Count == 0 && vt2.Strokes.Count == 1);
    }

    /// <summary>
    /// **文字渐变** —— 「文字也可以使用渐变」。
    ///
    /// ## 归一化按**整个文本块**，不是逐个字形
    ///
    /// 逐字形归一化会让每个字都自己红→蓝，一眼看去是"花的"；要的是"这一行从红到蓝"。
    /// 盒由 `DrawGeo.TextBlockBox` 给（**唯一真源**，SVG 那一半将来要用同一个盒）。
    ///
    /// ## 判据为什么是"红像素和蓝像素都存在"
    ///
    /// 这条路径有**两种**失败形态，且都不会报错：
    /// ① 渐变没接上 ⇒ 回退成 `f.Fill`（黑），画出来是黑的；
    /// ② 盒算错 ⇒ 整行落在渐变的一端，全是同色。
    /// "红 R>120&&B<80 的像素 > 5 个" + "蓝 B>120&&R<80 的像素 > 5 个" 两条一起
    /// 把这两种都挡住。对照组（纯色文字）必须**两条都不成立** —— 少了对照组，
    /// 一条"任何情况下都返回 true"的判据也能过。
    /// </summary>
    private static void TestTextGradient(Action<string> Section, Action<string, bool> Check)
    {
        Section("绘图 DSL：文字渐变");

        // ── TextBlockBox：唯一真源 ──
        var fig = DrawRunner.Parse("canvas 400 100 #ffffff\ntext 200 40 \"hi\" 20 #ffffff middle").Figures[0];
        var box = DrawGeo.TextBlockBox(fig, 40);
        Check($"TextBlockBox: middle 锚点向左退半个宽度（实得 x={box.X}）",
            Math.Abs(box.X - 180) < 1e-6 && Math.Abs(box.W - 40) < 1e-6);
        var figEnd = DrawRunner.Parse("canvas 400 100 #ffffff\ntext 200 40 \"hi\" 20 #ffffff end").Figures[0];
        Check("TextBlockBox: end 锚点向左退一个宽度",
            Math.Abs(DrawGeo.TextBlockBox(figEnd, 40).X - 160) < 1e-6);
        var figMulti = DrawRunner.Parse("canvas 400 100 #ffffff\ntext 0 10 \"a\\nb\\nc\" 20 #ffffff start").Figures[0];
        Check("TextBlockBox: 三行的高度 = 2×行距 + 一个 em",
            Math.Abs(DrawGeo.TextBlockBox(figMulti, 10).H - (20 * 1.3 * 2 + 20)) < 1e-6);

        // ── 光栅：红蓝像素都要出现 ──
        static int CountR(uint c) => R(c);
        static int CountB(uint c) => B(c);
        static (int Red, int Blue) Scan(RasterImage img, int x0, int y0, int x1, int y1)
        {
            int red = 0, blue = 0;
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    var c = img.ColorAt(x, y);
                    if (A(c) < 128) continue;
                    if (CountR(c) > 120 && CountB(c) < 80) red++;
                    else if (CountB(c) > 120 && CountR(c) < 80) blue++;
                }
            return (red, blue);
        }

        var doc = DrawRunner.Parse(
            "canvas 300 80 #ffffff\ngradient g linear #ff0000 #0000ff\ntext 20 20 \"WWWWWW\" 44 @g start");
        var img = PngDecoder.Decode(DrawRunner.ToPng(doc));
        var (red, blue) = Scan(img, 0, 0, 300, 80);
        Check($"光栅文字渐变: 左端有红像素（实得 {red} 个）", red > 5);
        Check($"光栅文字渐变: 右端有蓝像素（实得 {blue} 个）", blue > 5);

        // 对照组：纯色文字**不该**同时出现红与蓝（少了这条，判据永真也能过）
        var solid = PngDecoder.Decode(DrawRunner.ToPng(DrawRunner.Parse(
            "canvas 300 80 #ffffff\ntext 20 20 \"WWWWWW\" 44 #ff0000 start")));
        var (sred, sblue) = Scan(solid, 0, 0, 300, 80);
        Check($"对照: 纯色文字只有红、没有蓝（实得 红={sred} 蓝={sblue}）", sred > 5 && sblue == 0);

        // ── 未知字体族名：兜底到可用字体之后，渐变**仍然**生效 ──
        //
        // ⚠ **这条测不到 5×7 点阵那条路**，别读成"5×7 也验过了"。
        //    `TrueTypeFont.Resolve` 对不认识的族名有一个"候选里随便挑一个能加载的"兜底，
        //    所以 `no-such-font-xyz` 照样拿到真字体 —— 露馅的是**像素数与 TrueType 那条一模一样**
        //    （690/680 对 690/680），两条断言本该是不同路径。
        //    5×7 只在"一个字体都加载不出来"时才走，DSL 层造不出那个环境
        //    ⇒ **那条路目前没有自测覆盖**（代码支持渐变，`Canvas.DrawText` 也接了采样器，但没验过）。
        var fallback = PngDecoder.Decode(DrawRunner.ToPng(DrawRunner.Parse(
            "canvas 300 80 #ffffff\ngradient g linear #ff0000 #0000ff\ntext 20 20 \"WWWWWW\" 44 @g start no-such-font-xyz")));
        var (fred, fblue) = Scan(fallback, 0, 0, 300, 80);
        Check($"未知族名兜底后渐变仍生效（实得 红={fred} 蓝={fblue}）", fred > 5 && fblue > 5);

        // ── 矢量后端：字形没有路径 API ⇒ 如实标记，让宿主整窗回退光栅 ──
        var vt = new RecordingVectorTarget();
        var tfig = DrawRunner.Parse("canvas 300 80 #ffffff\ngradient g linear #ff0000 #0000ff\ntext 20 20 \"hi\" 44 @g start").Figures[0];
        DrawVector.Text(vt, tfig);
        Check("矢量: 文字渐变 → MarkUnsupported（平台文字 API 只吃纯色）",
            vt.Unsupported.Contains("text-gradient"));

        // ── 端到端：C 层够得到的入口（`ui_set_text_brush(渐变刷子)` + `ui_draw_text`）──
        var st = new VmlScene();
        var tgb = st.AddGradientBrush(radial: false, 0xFFFF0000, 0xFF0000FF, 0, 0, 1000, 0);
        st.SetStyle(VmlStyleSlot.Text, tgb, 0, 0, 0, 0);
        st.AddShapeText(20, 30, "hi");
        var tdoc = DrawRunner.Parse(st.BuildDsl());
        var tfig2 = tdoc.Figures.FirstOrDefault(x => x.Kind == "text");
        Check("端到端：文字槽的渐变刷子 → DSL → 解析回文字渐变",
            tfig2?.Gradient != null && tfig2.GradientRef != null);
        Check("端到端：发射的 token 是刷子引用（@_b…）而不是退化色",
            st.TextBrushColor != 0);   // 向后兼容的读取口仍给得出一个色（渐变的起始色）

        // 兼容回归：老的 `AddText(…, uint, …)` 与 `SetFont` 那条路**一字没变**
        var sPlain = new VmlScene();
        sPlain.FontSize = 20;
        sPlain.FontColor = 0xFF00FF00;
        sPlain.AddShapeText(10, 10, "hi");
        var pdoc = DrawRunner.Parse(sPlain.BuildDsl());
        var pfig = pdoc.Figures.FirstOrDefault(x => x.Kind == "text");
        Check("兼容：没设文字刷子时仍跟随 SetFont 的颜色",
            pfig?.Gradient == null && pfig?.Fill == 0xFF00FF00);

        // 兼容：`AddText` 的 uint 重载仍然可用（公开 API，不能被 string 版顶掉）
        var sDirect = new VmlScene();
        sDirect.AddText(5, 5, "ab", 0xFF123456, 18, 0);
        var ddoc = DrawRunner.Parse(sDirect.BuildDsl());
        var dfig = ddoc.Figures.FirstOrDefault(x => x.Kind == "text");
        Check("兼容：AddText(uint color) 重载未被 string 版顶掉",
            dfig?.Gradient == null && dfig?.Fill == 0xFF123456);

        // ── SVG：文字渐变必须走**专用**那份 userSpaceOnUse 定义 ──
        //
        // 为什么不能与形状共用：形状用 objectBoundingBox，而文字的盒在 SVG 里由渲染器
        // **按字形墨迹**算 —— 与我们 TextBlockBox（行高×行数 + 最长行宽）必然不等，
        // PNG 与 SVG 的渐变位置会差一截。
        var svgDoc = DrawRunner.Parse(
            "canvas 300 80 #ffffff\ngradient g linear #ff0000 #0000ff\ntext 20 20 \"WWWWWW\" 44 @g start");
        var svg = DrawRunner.ToSvg(svgDoc);
        Check("SVG: 文字渐变用 userSpaceOnUse", svg.Contains("gradientUnits=\"userSpaceOnUse\""));
        Check("SVG: 文字 fill 引用的是文字专用 id（不是形状那个 g）",
            svg.Contains("fill=\"url(#tg0)\"") && !svg.Contains("fill=\"url(#g)\""));
        // 计划里的判据：x1 必须等于 TextBlockBox 的左缘（线性渐变默认几何就是"从左到右"）
        var tbox = DrawParse.TextBox(svgDoc.Figures[0]);
        Check($"SVG: 渐变 x1 落在文字盒左缘（盒 X={tbox.X}）",
            svg.Contains($"x1=\"{tbox.X:0.###}\" y1=\"{tbox.Y:0.###}\""));
        Check("SVG: 渐变 x2 落在文字盒右缘（跨度 = 最长行宽）",
            svg.Contains($"x2=\"{tbox.X + tbox.W:0.###}\" y2=\"{tbox.Y:0.###}\""));

        // 同一份渐变被**两个**文字图元引用 ⇒ 各发一份（盒不同）、id 不能撞
        var two = DrawRunner.ToSvg(DrawRunner.Parse(
            "canvas 300 120 #ffffff\ngradient g linear #ff0000 #0000ff\n"
            + "text 10 10 \"AA\" 30 @g start\ntext 10 60 \"BBBB\" 30 @g start"));
        Check("SVG: 两个文字图元各拿一个 id（tg0 / tg1，不撞）",
            two.Contains("id=\"tg0\"") && two.Contains("id=\"tg1\"")
            && two.Contains("fill=\"url(#tg0)\"") && two.Contains("fill=\"url(#tg1)\""));
        // ⚠ 这里第一版写成 `Matches(...).Count == 2` —— 那只数了**个数**，
        //    跟标签说的"x2 不同"根本不是一回事（两个相等的 x2 也能过）。
        //    断言写错和实现写错一样会让人以为验过了，所以改成真比较。
        // ⚠ 取的是**文字那两份 def 里的** x2：`<defs>` 里还有一份共享的 `g`（objectBoundingBox、
        //    x2="1"），那是**既有行为**——所有 `gradient` 定义都无条件发出，不管有没有人引用。
        //    不按 id 收窄的话会把它也算进来（第一版就是，看到的三个值里有它）。
        var x2s = System.Text.RegularExpressions.Regex
            .Matches(two, "<linearGradient id=\"tg[0-9]+\"[^>]*x2=\"([0-9.]+)\"")
            .Select(m => m.Groups[1].Value).ToList();
        Check($"SVG: 两个文字图元的渐变跨度不同（实得 {string.Join(" / ", x2s)}）",
            x2s.Count == 2 && x2s[0] != x2s[1]);

        // 兼容性：用户**真的**把渐变起名叫 `tg0` 时不能撞 ——
        // DSL 里 `gradient tg0 linear …` 合法，撞上就是一个 SVG 里两个同名 id、
        // `url(#tg0)` 指哪个由渲染器说了算（"平时没事、别人起个名就坏"那类隐患）。
        var clash = DrawRunner.ToSvg(DrawRunner.Parse(
            "canvas 300 80 #ffffff\ngradient tg0 linear #ff0000 #0000ff\ntext 20 20 \"WW\" 30 @tg0 start"));
        // 用户占了 `tg0` ⇒ 文字那份应当排到 `tg1`，**并且文字真的引用 tg1**。
        // （第一版这里写成 `!Contains("tg1")` —— 与前半句自相矛盾：tg1 正是避让后该用的那个。）
        Check("SVG: 文字专用 id 避开用户已用的渐变名（文字改引用 tg1）",
            clash.Contains("id=\"tg0\"") && clash.Contains("id=\"tg1\"")
            && clash.Contains("fill=\"url(#tg1)\"")
            && System.Text.RegularExpressions.Regex.Matches(clash, "id=\"tg[0-9]+\"").Count == 2);

        // 回归守卫：**形状**那份渐变仍然不带 gradientUnits（= objectBoundingBox），
        // 别把文字的改动漏进形状 —— 那会让所有形状渐变的基准跟着变。
        var shapeSvg = DrawRunner.ToSvg(DrawRunner.Parse(
            "canvas 100 100 #ffffff\ngradient g linear #ff0000 #0000ff\nrect 10 10 50 30 @g"));
        Check("回归: 形状渐变仍是 objectBoundingBox（不带 gradientUnits）",
            !shapeSvg.Contains("gradientUnits") && shapeSvg.Contains("fill=\"url(#g)\""));

        // 反证：纯色文字**不该**被这条误伤（否则所有文字都会把整窗拖回光栅）
        var vt2 = new RecordingVectorTarget();
        var plain = DrawRunner.Parse("canvas 300 80 #ffffff\ntext 20 20 \"hi\" 44 #ff0000 start").Figures[0];
        DrawVector.Text(vt2, plain);
        Check("反证：纯色文字不受影响（不会误触发整窗回退）",
            vt2.Unsupported.Count == 0 && vt2.Texts.Count == 1);
    }

    /// <summary>
    /// **描边类图元解析的兼容性主闸**。
    ///
    /// `line` / `arrow` / `polyline` 以前各写了一份逐字相同的样式循环，现在收口成
    /// `DrawParse.ParseStrokeStyle` 一处。收口本身必须**逐字段零变化** ——
    /// 这几条断言就是那个"零"：把每条指令解析出来的
    /// `Stroke / StrokeWidth / LineCap / Dashed / Fill / GradientRef / FontFamily / Args` 全部钉住。
    ///
    /// 为什么值得单列一批：这三条是 `draw` 工具（AI 直接写 DSL）与桌面导出共用的一面，
    /// 解析漂了不会报错，只会"画出来的东西和写的不一样"。
    /// </summary>
    private static void TestStrokeStyleCompat(Action<string> Section, Action<string, bool> Check)
    {
        Section("绘图 DSL：描边类解析（收口前后必须逐字段不变）");

        // 把一条图元的全部样式字段打成一个字符串 —— 断言一条字符串比断言八个字段好读，
        // 而且**漏掉某个字段**本身就会被看见（那一格永远是默认值）。
        static string Dump(string dsl)
        {
            var doc = DrawRunner.Parse("canvas 200 200 #ffffff\n" + dsl);
            if (doc.Figures.Count != 1) return $"图元数={doc.Figures.Count} err={doc.Error}";
            var f = doc.Figures[0];
            var args = string.Join(",", f.Args.Select(v => v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)));
            return $"{f.Kind} args=[{args}] stroke=#{f.Stroke:X8} w={f.StrokeWidth:0.###} cap={f.LineCap} dash={f.Dashed} "
                 + $"fill=#{f.Fill:X8} gref={f.GradientRef ?? "-"} font={f.FontFamily} size={f.FontSize:0.###} anchor={f.Anchor}";
        }

        // ① line：裸线 / 颜色 + 线宽 / 线帽 + 虚线
        Check("line 默认", Dump("line 0 0 10 10") ==
            "line args=[0,0,10,10] stroke=#FF000000 w=1 cap=butt dash=False fill=#FF000000 gref=- font=sans-serif size=14 anchor=start");
        Check("line 颜色+线宽", Dump("line 0 0 10 10 #ff0000 5") ==
            "line args=[0,0,10,10] stroke=#FFFF0000 w=5 cap=butt dash=False fill=#FF000000 gref=- font=sans-serif size=14 anchor=start");
        Check("line 线帽+虚线", Dump("line 0 0 10 10 #f00 2 round dash") ==
            "line args=[0,0,10,10] stroke=#FFFF0000 w=2 cap=round dash=True fill=#FF000000 gref=- font=sans-serif size=14 anchor=start");

        // ② arrow：与 line 同一套样式段
        Check("arrow 颜色", Dump("arrow 0 0 10 10 #00ff00") ==
            "arrow args=[0,0,10,10] stroke=#FF00FF00 w=1 cap=butt dash=False fill=#FF000000 gref=- font=sans-serif size=14 anchor=start");

        // ③ polyline：几何**变长**，样式段从第 N 个 token 起（与 line/arrow 的起点不同）
        Check("polyline 颜色+线宽+线帽", Dump("polyline 0 0 10 0 10 10 #0000ff 3 square") ==
            "polyline args=[0,0,10,0,10,10] stroke=#FF0000FF w=3 cap=square dash=False fill=#FF000000 gref=- font=sans-serif size=14 anchor=start");

        // ④ 填充形状走的是另一套（第一个颜色=填充、第二个=描边）—— 顺手一起钉住，
        //    免得将来"统一到 ParseStyle"时把描边类的单色语义搞混
        Check("rect 双色（填充+描边）", Dump("rect 0 0 10 10 #ff0000 #00ff00 3") ==
            "rect args=[0,0,10,10] stroke=#FF00FF00 w=3 cap=butt dash=False fill=#FFFF0000 gref=- font=sans-serif size=14 anchor=start");
        Check("rect 渐变填充", Dump("rect 0 0 10 10 @g #00ff00 3") ==
            "rect args=[0,0,10,10] stroke=#FF00FF00 w=3 cap=butt dash=False fill=#FF000000 gref=g font=sans-serif size=14 anchor=start");

        // ⑤ text：`@id` 必须进**渐变引用**，绝不能落进"其余裸词=字体族名"的兜底
        var textDump = Dump("text 10 20 \"hi\" 24 #ffffff middle");
        Check("text 常规（颜色/字号/锚点）", textDump ==
            "text args=[10,20] stroke=#00000000 w=1 cap=butt dash=False fill=#FFFFFFFF gref=- font=sans-serif size=24 anchor=middle");
        var textGrad = Dump("text 10 20 \"hi\" 24 @g1");
        Check("text @id → 渐变引用（不是字体族名）", textGrad.Contains("gref=g1"));
        Check("text @id 不再污染字体族", textGrad.Contains("font=sans-serif"));
    }
}
