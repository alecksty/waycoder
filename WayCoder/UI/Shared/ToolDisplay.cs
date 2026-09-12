namespace WayCoder.UI.Shared;

/// <summary>
/// 工具调用的**显示**缩写 —— 只影响界面文案，**绝不影响传给工具的实际参数**
///（真实参数照旧走 <c>ToolCall.Arguments</c>，与这里无关）。
///
/// 背景：工具行原本照搬全部参数，用户实测「路径太长把行撑爆」——
/// `edit_file file_path=C:\a\b\c\d\main.c, old_string=…, new_string=…` 里除了路径全是噪声，
/// 而且 80 字符截断后连路径都被切掉。统一成 `名字(简短参数)`：
///     `edit_file file_path=C:\a\b\c\d\main.c, …` → `edit(main.c)`
///
/// 名字缩短表与路径缩写只在这一处实现，四端（TUI/Web/GUI/MAUI）共用 —— 别在各端各写一份。
/// 注意：**给工具做能力判断时不要用这里的缩写名**（例如 <c>ToolRegistry.IsRawOutput</c> 要真实名），
/// 缩写仅用于显示；本表只覆盖「非 raw 的编辑类工具」，所以按真实名查询的行为不受影响。
/// </summary>
public static class ToolDisplay
{
    /// <summary>工具名 → 显示名（未知工具原样返回）</summary>
    private static readonly Dictionary<string, string> ShortNames = new(StringComparer.Ordinal)
    {
        ["read_file"] = "read",
        ["write_file"] = "write",
        ["edit_file"] = "edit",
        ["multi_edit"] = "edit",
        ["notebook_edit"] = "edit",
        ["find_replace"] = "replace",
    };

    /// <summary>视为「路径类主参」的参数名（按优先级），命中后只显示它</summary>
    private static readonly string[] PathKeys =
    [
        "file_path", "notebook_path", "path", "dir", "directory", "cwd",
        "target", "dest", "destination", "source", "from", "to",
    ];

    /// <summary>显示名：read_file→read / edit_file→edit …（其余原样）</summary>
    public static string ShortName(string? tool)
        => tool != null && ShortNames.TryGetValue(tool, out var s) ? s : (tool ?? "");

    /// <summary>
    /// 路径缩写：**绝对路径只留文件名**；相对路径段数 ≤3 原样，过长则保留末两段（`…/b/main.c`）。
    /// 目标是「最短且还能认出是哪个文件」。
    /// </summary>
    public static string ShortPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        var p = path.Trim();

        // 绝对路径：盘符（C:\ / C:/）、UNC（\\srv\share）、Unix（/…）
        bool absolute = p[0] is '/' or '\\' || (p.Length > 1 && p[1] == ':');
        if (absolute) return FileNameOf(p);

        var segs = p.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segs.Length <= 3) return p;
        return "…/" + segs[^2] + "/" + segs[^1];
    }

    private static string FileNameOf(string p)
    {
        var i = Math.Max(p.LastIndexOf('/'), p.LastIndexOf('\\'));
        return i >= 0 && i + 1 < p.Length ? p[(i + 1)..] : p;
    }

    /// <summary>
    /// 参数摘要（显示用）：命中路径类主参 → 只给缩写后的路径（`main.c`）；
    /// 否则退回紧凑的 `k=v, k=v` 摘要（截断到 maxLen）—— 与轨迹日志用的
    /// <c>Agent.FormatBrief</c> 刻意分开：日志要全量，界面要短。
    /// </summary>
    public static string Brief(IReadOnlyDictionary<string, object?>? args, int maxLen = 60)
    {
        if (args == null || args.Count == 0) return "";

        foreach (var key in PathKeys)
        {
            foreach (var kv in args)
            {
                if (!string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase)) continue;
                // 多值主参（如 multi_edit 的 edits/文件数组）：逐项缩写后用顿号相连
                if (kv.Value is System.Collections.IEnumerable and not string)
                {
                    var many = EnumerateStrings(kv.Value).Select(ShortPath).Where(s => s.Length > 0).ToList();
                    if (many.Count == 0) continue;
                    return Truncate(string.Join("、", many), maxLen);
                }
                var raw = ValueText(kv.Value);
                if (raw.Length == 0) continue;
                return Truncate(ShortPath(raw), maxLen);
            }
        }

        var joined = string.Join(", ", args.Select(kv => $"{kv.Key}={ValueText(kv.Value)}"));
        return Truncate(joined, maxLen);
    }

    /// <summary>工具行文案：`edit(main.c)`（各端工具行统一用它）</summary>
    public static string Line(string? tool, IReadOnlyDictionary<string, object?>? args)
    {
        var brief = Brief(args);
        return brief.Length > 0 ? $"{ShortName(tool)}({brief})" : ShortName(tool);
    }

    private static IEnumerable<string> EnumerateStrings(object? value)
    {
        if (value is System.Collections.IEnumerable en and not string)
            foreach (var item in en)
            {
                var s = ValueText(item);
                if (s.Length > 0) yield return s;
            }
    }

    private static string ValueText(object? value) => value switch
    {
        null => "",
        string s => s,
        JNode node => node.Kind == JKind.String ? (node.AsString() ?? "") : JsonHelper.SerializeValue(node),
        _ => value.ToString() ?? "",
    };

    private static string Truncate(string s, int maxLen)
        => s.Length <= maxLen ? s : ContextManager.TruncateWithEllipsis(s, maxLen, "…");
}
