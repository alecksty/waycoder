namespace WayCoder.Tools;

/// <summary>文件操作辅助 —— CpTool/MvTool 复制的递归目录复制（含深度防环）收敛。</summary>
public static class FileOps
{
    /// <summary>
    /// 递归复制目录（含子目录与文件）。深度上限 &gt;64 抛错（防符号链接环无限递归 → StackOverflow）。
    /// </summary>
    public static void CopyDirectory(string srcDir, string destDir, bool overwrite, int depth = 0)
    {
        if (depth > 64) throw new IOException("目录层级过深（>64 层），已中止");
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.GetFiles(srcDir))
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), overwrite);
        foreach (var dir in Directory.GetDirectories(srcDir))
            CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)), overwrite, depth + 1);
    }
}
