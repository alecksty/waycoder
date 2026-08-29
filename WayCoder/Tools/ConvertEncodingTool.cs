using System.Text;
using WayCoder.UI.Shared;

namespace WayCoder.Tools;

/// <summary>
/// 编码转换工具 —— 把文本文件从一种编码转换成另一种（默认转 UTF-8）。
/// 源编码可自动识别（复用 <see cref="TextEncoding.Detect"/>）或显式指定；目标编码经
/// <see cref="TextEncoding.ResolveEncoding"/> 解析，覆盖市面绝大多数编码。
/// output 省略时原地覆盖转码，指定后写到新路径。
/// </summary>
public class ConvertEncodingTool : ITool
{
    public string Name => "convert_encoding";
    public ToolExecutionMode ExecutionMode => ToolExecutionMode.Exclusive; // 写文件，独占执行

    public string Description =>
        "把文本文件从一种编码转换成另一种（默认转 UTF-8）。支持市面绝大多数编码：" +
        "UTF-8/UTF-8 BOM/UTF-16/UTF-32、简体中文 GB2312/GBK/GB18030、繁体 Big5、" +
        "日文 Shift-JIS/EUC-JP、韩文 EUC-KR/UHC、ISO-8859-1~16、Windows-1250~1258、DOS 437/850 等。" +
        "from_encoding 默认 auto（自动识别 BOM/UTF-8/GB18030），to_encoding 默认 utf-8；" +
        "output 省略时原地覆盖转码。示例：把 GBK 编码的 a.cs 转成 UTF-8 —— file_path=\"a.cs\" from_encoding=\"gbk\" to_encoding=\"utf-8\"。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("file_path", JNode.Object()
                .Set("type", "string")
                .Set("description", "要转换编码的文件路径（文本文件）。"))
            .Set("from_encoding", JNode.Object()
                .Set("type", "string")
                .Set("description", "源编码，默认 auto（自动识别 BOM/UTF-8/GB18030）。可显式指定：utf-8/gbk/gb2312/gb18030/big5/shift-jis/euc-jp/euc-kr/iso-8859-1/windows-1252 等，或代码页数字（如 936/950）。"))
            .Set("to_encoding", JNode.Object()
                .Set("type", "string")
                .Set("description", "目标编码，默认 utf-8。支持同上全部编码；utf-8-bom 输出带 BOM。"))
            .Set("output", JNode.Object()
                .Set("type", "string")
                .Set("description", "输出文件路径，默认覆盖原文件（原地转码）。指定后写到新路径，原文件不动。")))
        .Set("required", JNode.Array().Add("file_path"));

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var filePath = arguments.GetValueOrDefault("file_path")?.ToString() ?? "";
        var from = arguments.GetValueOrDefault("from_encoding")?.ToString();
        var to = arguments.GetValueOrDefault("to_encoding")?.ToString();
        var output = arguments.GetValueOrDefault("output")?.ToString();
        var agentId = arguments.GetValueOrDefault("_agent_id")?.ToString() ?? "main";
        return Task.FromResult(Execute(filePath, from, to, output, agentId));
    }

    private static string Execute(string filePath, string? from, string? to, string? output, string agentId)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return "错误：file_path 不能为空 — 请提供要转换的文件路径。";

        var cwd = CwdContext.Current.Value ?? Directory.GetCurrentDirectory();
        var srcPath = Path.GetFullPath(filePath, cwd);

        // 敏感路径防护（SSH 密钥/云凭据/系统凭据，防提示注入读泄露）
        var sensitive = PathSafety.CheckSensitive(srcPath);
        if (sensitive != null)
            return $"❌ 已阻止：{sensitive}（安全策略：敏感文件读写受保护）";

        if (!File.Exists(srcPath))
            return $"错误：文件不存在 {filePath}";

        // 目标路径：output 省略则原地覆盖
        var dstPath = string.IsNullOrWhiteSpace(output)
            ? srcPath
            : Path.GetFullPath(output, cwd);

        if (!string.Equals(srcPath, dstPath, StringComparison.Ordinal))
        {
            var dstSensitive = PathSafety.CheckSensitive(dstPath);
            if (dstSensitive != null)
                return $"❌ 已阻止：{dstSensitive}（安全策略：敏感文件读写受保护）";
        }

        // 沙箱边界：项目写限（独立于权限模式）
        var sandbox = SandboxManager.CheckWritable(dstPath);
        if (sandbox != null)
            return sandbox;

        // 文件锁：防多 Agent 并发修改冲突
        var lockErr = FileLockManager.TryAcquireOrError(dstPath, agentId, "请等待锁释放或使用其他文件名");
        if (lockErr != null) return lockErr;

        try
        {
            byte[] bytes;
            try { bytes = File.ReadAllBytes(srcPath); }
            catch { return $"错误：无法读取 {filePath}"; }

            if (IsBinaryContent(bytes))
                return $"错误：{filePath} 是二进制文件（检测到 NUL 字节），convert_encoding 只能转换文本文件";

            FileTracker.RecordRead(srcPath);

            // ── 解码：auto 自动识别，否则显式指定编码 ──
            string text;
            string fromName;
            if (string.IsNullOrWhiteSpace(from) || from.Equals("auto", StringComparison.OrdinalIgnoreCase))
            {
                var d = TextEncoding.Detect(bytes);
                text = d.Text;
                fromName = d.EncodingName;
            }
            else
            {
                var enc = TextEncoding.ResolveEncoding(from);
                text = TextEncoding.Decode(bytes, enc);
                fromName = from.Trim();
            }

            // ── 目标编码（默认 UTF-8 无 BOM）──
            var dstEnc = TextEncoding.ResolveEncoding(to);
            var toName = string.IsNullOrWhiteSpace(to) ? "utf-8" : to.Trim();

            var dir = Path.GetDirectoryName(dstPath);
            if (dir != null) Directory.CreateDirectory(dir);
            // 用 WriteAllText（内部 StreamWriter）写回：编码自带 BOM 则写 BOM（UTF-8-BOM/UTF-16/UTF-32），
            // 无 BOM 编码（UTF-8/GBK/Big5 等）不写 BOM。直接 GetBytes + WriteAllBytes 会丢 BOM。
            File.WriteAllText(dstPath, text, dstEnc);
            FileTracker.RecordWrite(dstPath);

            var written = new FileInfo(dstPath).Length;
            var lines = text.Length == 0 ? 0 : text.Count(c => c == '\n') + 1;
            return $"✅ 已转换 {filePath}：{fromName} → {toName}" +
                   $"（{FormatUtil.FormatSize(bytes.Length)} → {FormatUtil.FormatSize(written)}，{lines} 行）";
        }
        catch (Exception ex)
        {
            return $"错误：{ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            FileLockManager.Release(dstPath, agentId);
        }
    }

    /// <summary>检测二进制内容：前 8KB 含 NUL 字节即判定为二进制。</summary>
    private static bool IsBinaryContent(byte[] raw)
    {
        var n = Math.Min(raw.Length, 8192);
        for (int i = 0; i < n; i++)
            if (raw[i] == 0) return true;
        return false;
    }
}
