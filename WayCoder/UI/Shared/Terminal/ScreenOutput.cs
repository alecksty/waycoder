namespace WayCoder.UI.Shared.Terminal;

/// <summary>
/// 判断一段程序输出是**一屏一屏画的全屏程序**，还是**一路往下堆的线性输出**。
///
/// 为什么必须分开：这两类输出的正确呈现方式**互相冲突**——
/// <list type="bullet">
/// <item>**线性**（`ls` / 编译日志 / `grep`）：要**折行 + 回滚缓存**，能往上翻历史。
///   光标定位在这里没有意义（老工具顶多用 `\r` 原地刷新一行）。</item>
/// <item>**全屏**（`nyancat` / `sl` / `nethack` / 一切 curses 程序）：要一个
///   **rows×cols 的网格**，`ESC[H` 是"回到原点重画"、`ESC[2J` 是"清屏"，
///   折行与回滚缓存**都是错的**（折行会把画面搅烂、滚起来会看到半屏残影）。</item>
/// </list>
///
/// 判据只看**光标定位/擦屏类**的 CSI（`H f A B C D G d s u` 与 `J`）——
/// 这些序列在"一路往下堆"的输出里没有意义，出现即说明程序在**按坐标画**。
///
/// ⚠ **刻意不认 `ESC[K`（擦到行尾）**：它是 `\r` 进度条（`[####    ] 40%` 反复重打）的
/// 常客，判成全屏会把普通构建输出也拽进网格模式 —— 那是本仓库最忌讳的"看着像就改行为"。
/// 私有模式（`ESC[?25l` 隐藏光标，终结符是 `l`）同样不认：它单独出现说明不了什么。
/// </summary>
public static class ScreenOutput
{
    /// <summary>认定为"按坐标画"的 CSI 终结符：光标定位 / 移动 / 保存恢复 / 擦屏。</summary>
    private const string CursorFinals = "HfABCDGsudJ";

    /// <summary>
    /// 这段输出是不是全屏程序画的？
    ///
    /// ⚠ 扫描必须**自己走一遍**、不能拿 `String.IndexOf("\x1b[")` 配终结符去猜：
    /// 参数里可能夹着 `?`（私有模式）与分号（`H` 的行;列），而且**序列可能被截断**
    /// （输出是按块拿到的，最后一段可能是半个序列）——截断时按"还没定论"处理，
    /// 不能把半个序列当成命中。
    /// </summary>
    public static bool LooksFullScreen(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return false;

        for (int i = 0; i < raw.Length; i++)
        {
            if (raw[i] != AnsiString.AnsiCharPrefix) continue;
            if (i + 1 >= raw.Length) return false;            // 半个 ESC，没定论
            if (raw[i + 1] != '[') { i++; continue; }         // OSC / 双字符序列，跳过

            // 私有模式（ESC[?…）与扩展协议（ESC[>…）不参与判定
            int j = i + 2;
            if (j < raw.Length && (raw[j] == '?' || raw[j] == '>' || raw[j] == '='))
            {
                i = j;
                continue;
            }

            // 参数区：0x30–0x3F；中间区：0x20–0x2F；终结符：0x40–0x7E
            while (j < raw.Length && raw[j] >= '\x20' && raw[j] <= '\x3f') j++;
            if (j >= raw.Length) return false;                // 序列还没到终结符（被截断）

            char final = raw[j];
            if (final >= '@' && final <= '~' && CursorFinals.IndexOf(final) >= 0)
                return true;

            i = j;
        }
        return false;
    }
}
