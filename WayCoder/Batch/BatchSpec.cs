using System.Text;
using System.Text.Json.Nodes;

namespace WayCoder;

/// <summary>
/// 批量任务引擎 —— 单个任务：一个仓库 + 一段任务描述。
/// 仓库可为远程 URL 或本地路径，每个任务在独立的克隆副本（worktree 隔离）中执行。
/// </summary>
public sealed class BatchJob
{
    /// <summary>仓库地址（远程 URL 或本地路径）。</summary>
    public string Repo { get; init; } = "";
    /// <summary>要执行的任务描述（作为一次性 prompt 传给子进程）。</summary>
    public string Task { get; init; } = "";
    /// <summary>可选显示名（缺省从仓库地址提取）。</summary>
    public string? Name { get; init; }
    /// <summary>可选分支（git clone -b）。</summary>
    public string? Branch { get; init; }

    /// <summary>用于目录命名/报告显示的名字（Name 优先，回退仓库名提取）。</summary>
    public string DisplayName =>
        string.IsNullOrWhiteSpace(Name) ? BatchSpec.SanitizeName(Repo) : Name;
}

/// <summary>
/// 批量任务清单 + JSON 解析。多仓库并行处理的核心数据结构。
/// </summary>
public sealed class BatchSpec
{
    public List<BatchJob> Jobs { get; } = new();
    public int MaxParallel { get; set; } = 4;
    public int TimeoutSec { get; set; } = 1800;
    public bool KeepResults { get; set; } = false;

    public const int MinParallel = 1;
    public const int MaxParallelLimit = 16;
    public const int MinTimeoutSec = 60;
    public const int MaxTimeoutSec = 36000;

    /// <summary>用法示例（写入 --batch 帮助）。</summary>
    public const string JsonTemplate = """
{
  "maxParallel": 4,
  "timeoutSec": 1800,
  "keepResults": false,
  "tasks": [
    { "repo": "https://github.com/org/repo1", "task": "修复登录 bug", "name": "repo1", "branch": "main" },
    { "repo": "/本地/路径/repo2", "task": "补充单元测试" }
  ]
}
""";

    /// <summary>解析 JSON 字符串为清单。失败时 error 非空、返回 null。</summary>
    public static BatchSpec? Parse(string json, out string error)
    {
        error = "";
        JsonNode? root;
        try { root = JsonNode.Parse(json); }
        catch (Exception ex) { error = $"JSON 解析失败: {ex.Message}"; return null; }

        if (root is not JsonObject obj) { error = "JSON 顶层必须是对象"; return null; }

        var spec = new BatchSpec();
        if (obj["maxParallel"] is JsonValue mp && mp.TryGetValue<int>(out var mpv))
            spec.MaxParallel = Math.Clamp(mpv, MinParallel, MaxParallelLimit);
        if (obj["timeoutSec"] is JsonValue ts && ts.TryGetValue<int>(out var tsv))
            spec.TimeoutSec = Math.Clamp(tsv, MinTimeoutSec, MaxTimeoutSec);
        if (obj["keepResults"] is JsonValue kr && kr.TryGetValue<bool>(out var krv))
            spec.KeepResults = krv;

        if (obj["tasks"] is not JsonArray arr) { error = "缺少 tasks 数组"; return null; }
        foreach (var item in arr)
        {
            if (item is not JsonObject jobObj) continue;
            var repo = jobObj["repo"]?.GetValue<string>()?.Trim() ?? "";
            var task = jobObj["task"]?.GetValue<string>() ?? "";
            if (string.IsNullOrWhiteSpace(repo) || string.IsNullOrWhiteSpace(task)) continue;
            spec.Jobs.Add(new BatchJob
            {
                Repo = repo,
                Task = task,
                Name = jobObj["name"]?.GetValue<string>(),
                Branch = jobObj["branch"]?.GetValue<string>(),
            });
        }

        if (spec.Jobs.Count == 0) { error = "tasks 数组为空或缺少 repo/task 字段"; return null; }
        return spec;
    }

    /// <summary>从「仓库列表 + 共享任务」构建清单（--batch-repo 多个 + --batch-task）。</summary>
    public static BatchSpec FromRepos(IEnumerable<string> repos, string task, int maxParallel = 4)
    {
        var spec = new BatchSpec { MaxParallel = Math.Clamp(maxParallel, MinParallel, MaxParallelLimit) };
        foreach (var repo in repos)
        {
            if (string.IsNullOrWhiteSpace(repo)) continue;
            spec.Jobs.Add(new BatchJob { Repo = repo.Trim(), Task = task });
        }
        return spec;
    }

    /// <summary>从仓库路径/URL 提取安全的目录名（去除 .git、非法字符）。</summary>
    public static string SanitizeName(string repo)
    {
        var s = repo.Trim();
        if (s.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            s = s[..^4];
        // 取最后一段路径（处理 URL / 反斜杠 / 冒号）
        var segments = s.Split('/', '\\', ':');
        s = segments.LastOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? s;
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
            sb.Append(char.IsLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '_');
        var result = sb.ToString().Trim('_', '.');
        return string.IsNullOrEmpty(result) ? "repo" : result;
    }

    /// <summary>判断仓库是远程 URL 还是本地路径。</summary>
    public static bool IsRemoteUrl(string repo) =>
        repo.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || repo.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        || repo.StartsWith("git@", StringComparison.OrdinalIgnoreCase)
        || repo.StartsWith("ssh://", StringComparison.OrdinalIgnoreCase)
        || repo.StartsWith("git://", StringComparison.OrdinalIgnoreCase);
}
