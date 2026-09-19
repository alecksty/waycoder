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

        _ = Fail;
    }
}
