namespace WayCoder.Tools;

/// <summary>
/// 递归收集文件的**唯一实现**。
///
/// <c>GrepTool</c> / <c>WcTool</c> / <c>FindReplaceTool</c> 此前各写一份「逐目录 try/catch + 深度上限
/// + 跳过垃圾目录」的递归，且各自维护**一张互不相同**的跳过表 —— 三张表都不等于
/// <see cref="FileIgnoreManager"/> 的权威表（25 条）。后果有两类：
/// ① **搜索盲区**：`grep` 会扫 `bin`/`obj`/`.vs`（另两张表跳过）、`find_replace` 会扫 `dist`/`build`
///    （grep 跳过）⇒ 出现「grep 查不到、find_replace 却改得到」这种自相矛盾；
/// ② **性能**：无预算递归（`ProjectInitializer.GlobAny` 用 `SearchOption.AllDirectories`）正是
///    CLAUDE.md 记的「home 下 12~36s」那类坑。
///
/// 现在跳过判断以权威表为**底座**，各工具用 <c>extraSkipDirs</c> 显式追加自己的噪音项
/// （如 grep 的 dist/build）—— 「默认一致、允许显式追加」，不再允许整张表另写一份。
/// </summary>
internal static class FileWalker
{
    /// <summary>深度上限：防符号链接环（<c>ln -s . loop</c>）无限递归 → StackOverflow。</summary>
    internal const int MaxDepth = 64;

    /// <summary>
    /// 递归遍历 <paramref name="root"/>，对每个目录调用 <paramref name="collectInDir"/> 收集文件
    /// （glob / 文本判断等差异留在回调里），结果累积到 <paramref name="results"/>。
    ///
    /// 每个目录独立 try/catch：单个不可访问子目录不再像
    /// <c>Directory.GetFiles(..., AllDirectories)</c> 那样让整棵树搜索失败。
    /// 达到 <paramref name="maxResults"/> 立即停止（先收当前目录，再决定要不要下钻）。
    /// </summary>
    /// <param name="extraSkipDirs">本工具额外跳过的目录名（大小写不敏感）；权威表始终生效。</param>
    internal static void Walk(string root, List<string> results, int maxResults,
        Action<string, List<string>> collectInDir,
        IReadOnlyCollection<string>? extraSkipDirs = null, int depth = 0)
    {
        if (results.Count >= maxResults || depth > MaxDepth) return;

        try
        {
            collectInDir(root, results);
        }
        catch
        {
            // 跳过无法访问的目录（权限/竞态删除）
        }

        if (results.Count >= maxResults) return;

        string[] subDirs;
        try { subDirs = Directory.GetDirectories(root); }
        catch { return; }

        foreach (var sub in subDirs)
        {
            if (results.Count >= maxResults) return;
            if (ShouldSkipDir(sub, extraSkipDirs)) continue;
            Walk(sub, results, maxResults, collectInDir, extraSkipDirs, depth + 1);
        }
    }

    /// <summary>本目录是否跳过：调用方的追加项优先，随后走 <see cref="FileIgnoreManager"/> 权威判定
    /// （内含隐藏目录与 .gitignore/.waycoderignore 规则）。</summary>
    internal static bool ShouldSkipDir(string dirPath, IReadOnlyCollection<string>? extraSkipDirs)
    {
        var name = Path.GetFileName(
            dirPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrEmpty(name)) return false; // 盘根之类：不以名字判断

        if (extraSkipDirs != null)
        {
            foreach (var e in extraSkipDirs)
                if (string.Equals(e, name, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return FileIgnoreManager.ShouldSkipDirectory(dirPath);
    }
}
