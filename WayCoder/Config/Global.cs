using System.Text;

namespace WayCoder;

/// <summary>
/// 全局常量 —— 应用名称、版本号、开发者信息等，全项目统一引用。
/// </summary>
public static class Global
{
    // ── 文件编码 ──
    /// <summary>UTF-8 无 BOM 编码（源文件写入默认；.NET 的 <see cref="Encoding.UTF8"/> 静态实例带 BOM，Python/严格解析器会拒绝）</summary>
    public static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    /// <summary>写文本文件：原文件带 BOM 则保留，否则写无 BOM UTF-8（write_file/编辑器/批量工具统一走这里，避免源文件被强加 BOM）。</summary>
    public static void WriteAllTextPreserveBom(string path, string content)
        => File.WriteAllText(path, content, FileStartsWithUtf8Bom(path) ? Encoding.UTF8 : Utf8NoBom);

    /// <summary>探测文件是否以 UTF-8 BOM（EF BB BF）开头。</summary>
    static bool FileStartsWithUtf8Bom(string path)
    {
        try
        {
            if (!File.Exists(path)) return false;
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var b = new byte[3];
            if (fs.Read(b, 0, 3) < 3) return false;
            return b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF;
        }
        catch { return false; }
    }

    // ── 文件 IO 通用助手（收敛各处的原子写/建目录/时间戳/清理样板）──
    /// <summary>原子写文本文件：先写 .tmp 再 File.Move 替换，防崩溃/磁盘满留下半截文件覆盖全量数据（无 BOM UTF-8）。</summary>
    public static void WriteAllTextAtomic(string path, string content)
    {
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, content, Utf8NoBom);
        File.Move(tmp, path, overwrite: true);
    }

    /// <summary>写前备份文件：复制为 path.{yyyyMMddHHmmssfff}.bak（重名冲突回退 .{Guid:N}.bak），失败返回 null。
    /// 收敛 DoctorEngine / ModelCatalog 的两份同逻辑 BackupFile。</summary>
    public static string? BackupFile(string path)
    {
        try
        {
            var bak = path + "." + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".bak";
            if (File.Exists(bak)) bak = path + "." + Guid.NewGuid().ToString("N") + ".bak";
            File.Copy(path, bak, overwrite: false);
            return bak;
        }
        catch { return null; }
    }

    /// <summary>确保文件所在目录存在（Directory.CreateDirectory 幂等）。空/无父目录直接返回。</summary>
    public static void EnsureDir(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        }
        catch { /* 建目录失败不崩溃，由后续写文件报错 */ }
    }

    /// <summary>今天日期戳 yyyyMMdd（日志文件名）。</summary>
    public static string TodayStamp() => DateTime.Now.ToString("yyyyMMdd");
    /// <summary>当前时间戳 yyyyMMdd_HHmmss（快照/备份文件名）。</summary>
    public static string NowStamp() => DateTime.Now.ToString("yyyyMMdd_HHmmss");
    /// <summary>可读日志时间戳 yyyy-MM-dd HH:mm:ss。</summary>
    public static string LogStamp() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    /// <summary>清理目录下按 LastWriteTimeUtc 超过保留天数的文件，返回删除数。</summary>
    public static int CleanupOldFiles(string dir, string pattern, int retentionDays)
    {
        if (!Directory.Exists(dir)) return 0;
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        var n = 0;
        try
        {
            foreach (var f in Directory.GetFiles(dir, pattern))
                if (File.GetLastWriteTimeUtc(f) < cutoff) { try { File.Delete(f); n++; } catch { } }
        }
        catch { }
        return n;
    }

    /// <summary>目录下匹配文件数超过上限时删除最旧的多余文件，返回删除数。</summary>
    public static int EnforceMaxFiles(string dir, string pattern, int maxKeep)
    {
        if (!Directory.Exists(dir) || maxKeep <= 0) return 0;
        try
        {
            var files = Directory.GetFiles(dir, pattern)
                .OrderBy(f => File.GetLastWriteTimeUtc(f)).ToList();
            var excess = files.Count - maxKeep;
            for (int i = 0; i < excess; i++) { try { File.Delete(files[i]); } catch { } }
            return Math.Max(0, excess);
        }
        catch { return 0; }
    }

    // ── 显示格式化 ──
    /// <summary>上下文窗口整数 → 简短文本（- / 128K / 1.1M），四端模型表共用的唯一格式。
    /// 规则：≤0 → "-"；≥1_000_000 → 一位小数 M（去尾零，如 1.0M→"1M"、1.05M→"1.1M"）；
    /// ≥1000 → 整数 K；其余 → 原样。小数用 InvariantCulture（固定 "." 小数点，不随地区变 ","）。</summary>
    public static string FormatContext(long ctx) => ctx switch
    {
        <= 0 => "-",
        >= 1_000_000 => (ctx / 1_000_000.0).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "M",
        >= 1000 => (ctx / 1000).ToString(System.Globalization.CultureInfo.InvariantCulture) + "K",
        _ => ctx.ToString(System.Globalization.CultureInfo.InvariantCulture),
    };

    // ── 应用 ──
    /// <summary>应用品牌名（英文）</summary>
    public const string AppName = "WayCoder";
    /// <summary>应用中文名</summary>
    public const string AppNameCN = "道码";
    /// <summary>应用全称</summary>
    public const string AppFullName = "WayCoder 道码·通用编程智能体";
    /// <summary>版本号</summary>
    public const string Version = "v0.96.46";
    /// <summary>应用名 + 版本号</summary>
    public static string AppNameVersion => $"{AppName} {Version} ({AppNameCN})";

    // ── 公司 / 开发者 ──
    /// <summary>公司名称</summary>
    public const string Company = "深圳市探索智能科技有限公司";
    /// <summary>开发者（作者）</summary>
    public const string Developer = "施探宇 (aleck)";
    /// <summary>开发者邮箱</summary>
    public const string Email = "alecksty@163.com";
    /// <summary>联系电话</summary>
    public const string Phone = "+86 186-8039-9436";
    /// <summary>地址</summary>
    public const string Address = "中国 · 深圳";

    // ── 仓库 ──
    /// <summary>Git 仓库地址</summary>
    public const string RepoUrl = "https://gitee.com/aleckstygit/my-coder";
    /// <summary>开源协议</summary>
    public const string License = "MIT";

    // ── 配置目录 ──
    /// <summary>当前配置目录名</summary>
    public const string ConfigDirName = ".waycoder";

    // ════════════════════════════════════════════════════════════════════
    // 资源占用上限（全部编译期常量，防内存/显示/磁盘被无限增长撑爆）
    // 数值由用户统一在此调整；各功能在运行时引用这些常量做截断/裁剪/清理。
    // ════════════════════════════════════════════════════════════════════

    // ── 工具输出 / 后台任务 ──
    /// <summary>Bash 命令流式输出滚动上限（字符）：执行期间超限丢中间保留头尾，防长输出命令撑爆内存。</summary>
    public const int BashOutputMaxChars = 50_000;

    /// <summary>后台任务单任务输出滚动上限（字符）：构建/服务类长任务输出超限保留头尾。</summary>
    public const int MaxBgOutputChars = 50_000;

    /// <summary>已完成后台任务保留个数：超出自动清除最旧，防长期会话内存无限增长。</summary>
    public const int MaxCompletedTasks = 50;

    // ── 消息 / 显示 ──
    /// <summary>单条流式消息内容上限（字符）：一条超长回复/工具输出超过后截断附标记，防 Content 与渲染撑爆。</summary>
    public const int MaxSingleMessageChars = 50_000;

    /// <summary>Agent 消息列表硬上限（条）：token 估算不到阈值但条数过多（LLM 反复回极短消息）时强制压缩。</summary>
    public const int MaxAgentMessagesHard = 300;

    // ── 队列 / 排队 ──
    /// <summary>后台→UI 投递队列上限：渲染慢时流式 token 持续投递会积压，超限丢最旧防内存撑爆。</summary>
    public const int MaxUiQueue = 2000;

    /// <summary>待提交消息排队上限：Agent 长任务期间用户持续输入会无限累积，超限丢最旧。</summary>
    public const int MaxPendingSubmissions = 50;

    /// <summary>跨槽位待投递消息上限：从不激活的槽位会无限累积，超限丢最旧。</summary>
    public const int MaxPendingSlotMessages = 100;

    /// <summary>Watch 模式待处理提示队列上限：突发大量文件变更可积压，超限丢最旧。</summary>
    public const int MaxWatchPrompts = 20;

    // ── 文件集合 ──
    /// <summary>会话内文件追踪集合上限（修改/全会话文件）：超限清空重建，防长期会话无限累积。</summary>
    public const int MaxTrackedFiles = 2000;

    /// <summary>grep/ls 搜索结果上限：超大目录/海量匹配时截断，防结果列表无限增长。</summary>
    public const int MaxGrepResults = 5000;

    /// <summary>grep 匹配行结果上限（条）：达到即截断返回，防宽松正则在超大仓库物化百万条匹配串先爆内存。</summary>
    public const int MaxGrepResultLines = 200;

    // ── 磁盘保留策略（日志 / 轨迹 / 会话 / 审计 / 版本） ──
    /// <summary>错误日志单文件上限（字节）：超限滚动到 error_YYYYMMDD_N.log。</summary>
    public const long MaxLogFileBytes = 10 * 1024 * 1024;

    /// <summary>错误日志单日滚动文件数量上限：超限删最旧（病态 FirstChanceException 时防一天生成上百个 10MB 文件）。</summary>
    public const int MaxRolledLogFiles = 10;

    /// <summary>错误日志保留天数：启动清理 N 天前的 error_*.log。</summary>
    public const int LogRetentionDays = 30;

    /// <summary>DebugLog 会话日志保留天数：Enable 时清理 N 天前的 session_*.log。</summary>
    public const int DebugLogRetentionDays = 7;

    /// <summary>子智能体审计日志单文件上限（字节）：超限滚动到 subagents_N.log。</summary>
    public const long AuditLogFileBytes = 10 * 1024 * 1024;

    /// <summary>运行轨迹保留个数：每次 run 一个文件，超限删最旧。</summary>
    public const int MaxTrajectoryKeep = 100;

    /// <summary>运行轨迹保留天数：删 N 天前的轨迹文件。</summary>
    public const int TrajectoryRetentionDays = 30;

    /// <summary>会话文件保留个数：保存时超限删最旧。</summary>
    public const int MaxSessionsKeep = 200;

    /// <summary>会话文件保留天数：删 N 天前的会话文件。</summary>
    public const int SessionRetentionDays = 30;

    /// <summary>文件版本每文件上限：超限滚动删最旧。</summary>
    public const int FileVersionMaxPerFile = 20;

    /// <summary>文件版本全局总量上限：跨文件删最旧，防「改了很多文件」时磁盘膨胀。</summary>
    public const int FileVersionMaxTotal = 200;

    // ── 各端输入队列 / 数据聚合护栏 ──
    /// <summary>待提交输入队列上限（Web/GUI）：Agent 忙时用户连续输入会无限累积，超限丢最旧。</summary>
    public const int MaxPendingInput = 100;

    /// <summary>Web 非 SSE 客户端槽位绑定上限：只 POST /chat 不建 SSE 连接的 clientId 会永久占用字典与槽位，超限不再绑定新 id。</summary>
    public const int MaxClientSlotEntries = 64;

    /// <summary>LLM 待注入图片每 agentId 上限（张）：连续加图不发消息会累积 base64 内存，超限丢最旧。</summary>
    public const int MaxQueuedImages = 8;

    /// <summary>TodoTool 条数上限：todo 无限加会让 todos.json 无限涨，超限提示先清理。</summary>
    public const int MaxTodos = 500;

    /// <summary>模型目录在线导入数量护栏（总库）：一次导入 7000+ 无护栏会刷爆，超限拒绝该批。</summary>
    public const int MaxImportedModels = 10_000;

    /// <summary>MAUI 会话文件大小上限（字节）：maui_session.txt 超限截尾重写，保尾部窗口。</summary>
    public const long MaxMauiSessionBytes = 2 * 1024 * 1024;

    /// <summary>MAUI 录音文件保留个数：超出删最旧，防 workspace 磁盘无限涨。</summary>
    public const int MaxAudioRecordings = 20;

    /// <summary>默认截图目录保留个数：screenshot 工具默认落盘 ~/.waycoder/screenshots/，超出删最旧，防磁盘无限累积。</summary>
    public const int MaxScreenshotsKeep = 20;


    /// <summary>测试/嵌入式场景可覆写的用户主目录；默认取系统用户目录。</summary>
    public static string? HomeOverride { get; set; }

    /// <summary>当前生效的用户主目录。</summary>
    public static string Home =>
        HomeOverride ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <summary>搜索顺序：新目录优先，旧目录回退</summary>
    public static string[] ConfigDirSearchOrder => [ConfigDirName];

    /// <summary>全局配置路径（~/waycoder/...）</summary>
    public static string GlobalConfigPath(params string[] segments)
    {
        var parts = new string[segments.Length + 1];
        parts[0] = Path.Combine(Home, ConfigDirName);
        Array.Copy(segments, 0, parts, 1, segments.Length);
        return Path.Combine(parts);
    }

    /// <summary>写配置路径：始终返回 .waycoder/ 下路径</summary>
    public static string WriteConfigPath(string cwd, params string[] segments)
    {
        var parts = new string[segments.Length + 1];
        parts[0] = Path.Combine(cwd, ConfigDirName);
        Array.Copy(segments, 0, parts, 1, segments.Length);
        return Path.Combine(parts);
    }

    /// <summary>读配置路径：先试 .waycoder/，回退 .corecoder/，都不存在返回 .waycoder/ 路径</summary>
    public static string ReadConfigPath(string cwd, params string[] segments)
    {
        foreach (var dirName in ConfigDirSearchOrder)
        {
            var parts = new string[segments.Length + 1];
            parts[0] = Path.Combine(cwd, dirName);
            Array.Copy(segments, 0, parts, 1, segments.Length);
            var path = Path.Combine(parts);
            // 检查目录是否存在（对于文件路径，检查父目录）
            var parent = segments.Length > 0 ? Path.GetDirectoryName(path) : path;
            if (parent != null && Directory.Exists(parent)) return path;
        }
        // 都不存在，返回新目录路径
        var fallbackParts = new string[segments.Length + 1];
        fallbackParts[0] = Path.Combine(cwd, ConfigDirName);
        Array.Copy(segments, 0, fallbackParts, 1, segments.Length);
        return Path.Combine(fallbackParts);
    }

    /// <summary>全局读配置路径：~/.waycoder/... 优先，回退 ~/.corecoder/...</summary>
    public static string GlobalReadConfigPath(params string[] segments)
    {
        return ReadConfigPath(Home, segments);
    }

    /// <summary>从 cwd 向上查找存在的配置目录，返回目录名（.waycoder / .corecoder），都不存在返回 null</summary>
    public static string? FindExistingConfigDir(string cwd)
    {
        var dir = cwd;
        while (dir != null)
        {
            foreach (var dirName in ConfigDirSearchOrder)
            {
                if (Directory.Exists(Path.Combine(dir, dirName)))
                    return dirName;
            }
            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
        return null;
    }

    /// <summary>从 cwd 向上查找配置文件（如 mcp_servers.json），返回完整路径，未找到返回 null</summary>
    public static string? FindConfigFileInTree(string cwd, string relativePath)
    {
        var dir = cwd;
        while (dir != null)
        {
            foreach (var dirName in ConfigDirSearchOrder)
            {
                var candidate = Path.Combine(dir, dirName, relativePath);
                if (File.Exists(candidate)) return candidate;
            }
            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
        return null;
    }
}
